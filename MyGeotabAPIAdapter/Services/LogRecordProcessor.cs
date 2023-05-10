using Geotab.Checkmate.ObjectModel;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Add_Ons.VSS;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Configuration.Add_Ons.VSS;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Database.Models.Add_Ons.VSS;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.GeotabObjectMappers;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.MyGeotabAPI;
using NLog;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that extracts <see cref="LogRecord"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class LogRecordProcessor : BackgroundService
    {
        bool feedVersionRollbackRequired = false;

        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment adapterEnvironment;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbLogRecord> dbLogRecordEntityPersister;
        readonly IGenericEntityPersister<DbOVDSServerCommand> dbOVDSServerCommandEntityPersister;
        readonly IGenericGeotabObjectFeeder<LogRecord> logRecordGeotabObjectFeeder;
        readonly IGeotabDeviceFilterer geotabDeviceFilterer;
        readonly IGeotabLogRecordDbLogRecordObjectMapper geotabLogRecordDbLogRecordObjectMapper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IPrerequisiteServiceChecker prerequisiteServiceChecker;
        readonly IServiceTracker serviceTracker;
        readonly IStateMachine stateMachine;
        readonly IVSSConfiguration vssConfiguration;
        readonly IVSSObjectMapper vssObjectMapper;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogRecordProcessor"/> class.
        /// </summary>
        public LogRecordProcessor(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment adapterEnvironment, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbLogRecord> dbLogRecordEntityPersister, IGenericEntityPersister<DbOVDSServerCommand> dbOVDSServerCommandEntityPersister, IGeotabDeviceFilterer geotabDeviceFilterer, IGenericGeotabObjectFeeder<LogRecord> logRecordGeotabObjectFeeder, IGeotabLogRecordDbLogRecordObjectMapper geotabLogRecordDbLogRecordObjectMapper, IMyGeotabAPIHelper myGeotabAPIHelper, IPrerequisiteServiceChecker prerequisiteServiceChecker, IServiceTracker serviceTracker, IStateMachine stateMachine, IVSSConfiguration vssConfiguration, IVSSObjectMapper vssObjectMapper, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.exceptionHelper = exceptionHelper;
            this.dbLogRecordEntityPersister = dbLogRecordEntityPersister;
            this.dbOVDSServerCommandEntityPersister = dbOVDSServerCommandEntityPersister;
            this.geotabDeviceFilterer = geotabDeviceFilterer;
            this.logRecordGeotabObjectFeeder = logRecordGeotabObjectFeeder;
            this.geotabLogRecordDbLogRecordObjectMapper = geotabLogRecordDbLogRecordObjectMapper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.prerequisiteServiceChecker = prerequisiteServiceChecker;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.vssConfiguration = vssConfiguration;
            this.vssObjectMapper = vssObjectMapper;

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Iteratively executes the business logic until the service is stopped.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            while (!stoppingToken.IsCancellationRequested)
            {
                await WaitForPrerequisiteServicesIfNeededAsync(stoppingToken);

                // Abort if waiting for connectivity restoration.
                if (stateMachine.CurrentState == State.Waiting)
                {
                    feedVersionRollbackRequired = true;
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        var dbOServiceTracking = await serviceTracker.GetLogRecordServiceInfoAsync();

                        // For VSS Add-On: Adjust (i.e. reduce) the FeedResultsLimit for LogRecords if they are configured to be output as OVDS server commands.
                        var getFeedResultsLimit = myGeotabAPIHelper.GetFeedResultLimitDefault;
                        if (vssConfiguration.EnableVSSAddOn == true && vssConfiguration.OutputLogRecordsToOVDS == true)
                        {
                            getFeedResultsLimit = vssConfiguration.LogRecordFeedResultsLimitWhenOutputtingLogRecordsToOVDS;
                        }

                        // Initialize the Geotab object feeder.
                        if (logRecordGeotabObjectFeeder.IsInitialized == false)
                        {
                            await logRecordGeotabObjectFeeder.InitializeAsync(cancellationTokenSource, adapterConfiguration.LogRecordFeedIntervalSeconds, getFeedResultsLimit, (long?)dbOServiceTracking.LastProcessedFeedVersion);
                        }

                        // If this is the first iteration after a connectivity disruption, roll-back the LastFeedVersion of the GeotabObjectFeeder to the last processed feed version that was committed to the database and set the LastFeedRetrievalTimeUtc to DateTime.MinValue to start processing without further delay.
                        if (feedVersionRollbackRequired == true)
                        {
                            logRecordGeotabObjectFeeder.LastFeedVersion = dbOServiceTracking.LastProcessedFeedVersion;
                            logRecordGeotabObjectFeeder.LastFeedRetrievalTimeUtc = DateTime.MinValue;
                            feedVersionRollbackRequired = false;
                        }

                        // Get a batch of LogRecord objects from Geotab.
                        await logRecordGeotabObjectFeeder.GetFeedDataBatchAsync(cancellationTokenSource);
                        stoppingToken.ThrowIfCancellationRequested();

                        // Determine LogRecord output option.
                        VSSOutputOptions logRecordOutputOption = vssConfiguration.GetVSSOutputOptionForLogRecords();

                        // Process any returned LogRecords.
                        var logRecords = logRecordGeotabObjectFeeder.GetFeedResultDataValuesList();
                        var dbLogRecordsToPersist = new List<DbLogRecord>();
                        var dbOVDSServerCommandsToPersist = new List<DbOVDSServerCommand>();
                        if (logRecords.Count > 0)
                        {
                            // Apply tracked device filter (if configured in appsettings.json) and then map the LogRecords to DbLogRecords.
                            var filteredLogRecords = await geotabDeviceFilterer.ApplyDeviceFilterAsync(cancellationTokenSource, logRecords);
                            dbLogRecordsToPersist = geotabLogRecordDbLogRecordObjectMapper.CreateEntities(filteredLogRecords);

                            // Generate DbOVDSServerCommands if dictated by the configured VSSOutputOption.
                            if (logRecordOutputOption == VSSOutputOptions.DbOVDSServerCommandOnly || logRecordOutputOption == VSSOutputOptions.AdapterRecordAndDbOVDSServerCommand)
                            {
                                dbOVDSServerCommandsToPersist = vssObjectMapper.GetDbOVDSServerSetCommands(filteredLogRecords);
                            }
                        }

                        stoppingToken.ThrowIfCancellationRequested();

                        // Persist changes to database.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    // DbLogRecord:
                                    if (logRecordOutputOption == VSSOutputOptions.AdapterRecordOnly || logRecordOutputOption == VSSOutputOptions.AdapterRecordAndDbOVDSServerCommand)
                                    {
                                        await dbLogRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbLogRecordsToPersist, cancellationTokenSource, Logging.LogLevel.Info);
                                    }

                                    // DbOVDSServerCommands:
                                    if (logRecordOutputOption == VSSOutputOptions.DbOVDSServerCommandOnly || logRecordOutputOption == VSSOutputOptions.AdapterRecordAndDbOVDSServerCommand)
                                    {
                                        await dbOVDSServerCommandEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbOVDSServerCommandsToPersist, cancellationTokenSource, Logging.LogLevel.Info);
                                    }

                                    // DbOServiceTracking (for LogRecordProcessor):
                                    if (dbLogRecordsToPersist.Count > 0)
                                    {
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.LogRecordProcessor, logRecordGeotabObjectFeeder.LastFeedRetrievalTimeUtc, logRecordGeotabObjectFeeder.LastFeedVersion);
                                    }
                                    else
                                    {
                                        // No LogRecords were returned, but the OServiceTracking record for this service still needs to be updated to show that the service is operating.
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.LogRecordProcessor, DateTime.UtcNow);
                                    }

                                    // Commit transactions:
                                    await adapterUOW.CommitAsync();
                                }
                                catch (Exception ex)
                                {
                                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                                    await adapterUOW.RollBackAsync();
                                    throw;
                                }
                            }
                        }, new Context());

                        // Clear FeedResultData.
                        logRecordGeotabObjectFeeder.FeedResultData.Clear();
                    }

                    logger.Trace($"Completed iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");
                }
                catch (OperationCanceledException)
                {
                    string errorMessage = $"{CurrentClassName} process cancelled.";
                    logger.Warn(errorMessage);
                    throw new Exception(errorMessage);
                }
                catch (AdapterDatabaseConnectionException databaseConnectionException)
                {
                    HandleException(databaseConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                }
                catch (MyGeotabConnectionException myGeotabConnectionException)
                {
                    HandleException(myGeotabConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                }
                catch (Exception ex)
                {
                    // If an exception hasn't been handled to this point, log it and kill the process.
                    HandleException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
                }

                // If the feed is up-to-date, add a delay equivalent to the configured update interval.
                if (logRecordGeotabObjectFeeder.FeedCurrent == true)
                {
                    var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.LogRecordFeedIntervalSeconds);
                    logger.Info($"{CurrentClassName} pausing for the configured feed interval ({delayTimeSpan}).");
                    await Task.Delay(delayTimeSpan, stoppingToken);
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Generates and logs an error message for the supplied <paramref name="exception"/>. If the <paramref name="exception"/> is connectivity-related, the <see cref="stateMachine"/> will have its <see cref="IStateMachine.CurrentState"/> and <see cref="IStateMachine.Reason"/> set accordingly. If the value supplied for <paramref name="logLevel"/> is <see cref="NLogLogLevelName.Fatal"/>, the current process will be killed.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <param name="logLevel">The <see cref="LogLevel"/> to be used when logging the error message.</param>
        /// <param name="errorMessagePrefix">The start of the error message, which will be followed by the <see cref="Exception.Message"/>, <see cref="Exception.Source"/> and <see cref="Exception.StackTrace"/>.</param>
        /// <returns></returns>
        void HandleException(Exception exception, NLogLogLevelName logLevel, string errorMessagePrefix)
        {
            exceptionHelper.LogException(exception, logLevel, errorMessagePrefix);
            if (exception is AdapterDatabaseConnectionException)
            {
                stateMachine.SetState(State.Waiting, StateReason.AdapterDatabaseNotAvailable);
            }
            else if (exception is MyGeotabConnectionException)
            {
                stateMachine.SetState(State.Waiting, StateReason.MyGeotabNotAvailable);
            }

            if (logLevel == NLogLogLevelName.Fatal)
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        /// Starts the current <see cref="LogRecordProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.LogRecordProcessor, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.LogRecordProcessor, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
                        await adapterUOW.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                        await adapterUOW.RollBackAsync();
                        throw;
                    }
                }
            }, new Context());

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.EnableLogRecordFeed == true)
            {
                logger.Info($"******** STARTING SERVICE: {CurrentClassName}");
                await base.StartAsync(cancellationToken);
            }
            else
            {
                logger.Warn($"******** WARNING - SERVICE DISABLED: The {CurrentClassName} service has not been enabled and will NOT be started.");
            }
        }

        /// <summary>
        /// Stops the current <see cref="LogRecordProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Checks whether any prerequisite services have been run and are currently running. If any of prerequisite services have not yet been run or are not currently running, details will be logged and this service will pause operation, repeating this check intermittently until all prerequisite services are running.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        async Task WaitForPrerequisiteServicesIfNeededAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var prerequisiteServices = new List<AdapterService>
            {
                AdapterService.DeviceProcessor
            };

            await prerequisiteServiceChecker.WaitForPrerequisiteServicesIfNeededAsync(CurrentClassName, prerequisiteServices, cancellationToken);

            // If outputting to VSS, ensure that the VSSConfiguration is initialized. This is to allow the current service to operate independently of the OVDSClientWorker.
            if (vssConfiguration.EnableVSSAddOn == true && vssConfiguration.OutputLogRecordsToOVDS == true && vssConfiguration.IsInitialized == false)
            {
                await vssConfiguration.InitializeAsync(AppContext.BaseDirectory, vssConfiguration.VSSPathMapFileName);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
