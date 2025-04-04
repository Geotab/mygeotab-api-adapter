using Geotab.Checkmate.ObjectModel;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Add_Ons.VSS;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Configuration.Add_Ons.VSS;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityMappers;
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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that extracts <see cref="LogRecord"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class LogRecordProcessor2 : BackgroundService
    {
        bool feedVersionRollbackRequired = false;

        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<LogRecordProcessor2> awaiter;
        readonly IDbLogRecord2DbEntityMetadata2EntityMapper dbLogRecord2DbEntityMetadata2EntityMapper;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbEntityMetadata2> dbEntityMetadata2EntityPersister;
        readonly IGenericEntityPersister<DbLogRecord2> dbLogRecord2EntityPersister;
        readonly IGenericEntityPersister<DbOVDSServerCommand> dbOVDSServerCommandEntityPersister;
        readonly IGenericGeotabObjectFeeder<LogRecord> logRecordGeotabObjectFeeder;
        readonly IGeotabDeviceFilterer geotabDeviceFilterer;
        readonly IGeotabLogRecordDbLogRecord2ObjectMapper geotabLogRecordDbLogRecord2ObjectMapper;
        readonly IMinimumIntervalSampler<LogRecord> minimumIntervalSampler;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;
        readonly IVSSConfiguration vssConfiguration;
        readonly IVSSObjectMapper vssObjectMapper;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogRecordProcessor"/> class.
        /// </summary>
        public LogRecordProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<LogRecordProcessor2> awaiter, IDbLogRecord2DbEntityMetadata2EntityMapper dbLogRecord2DbEntityMetadata2EntityMapper, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbEntityMetadata2> dbEntityMetadata2EntityPersister, IGenericEntityPersister<DbLogRecord2> dbLogRecord2EntityPersister, IGenericEntityPersister<DbOVDSServerCommand> dbOVDSServerCommandEntityPersister, IGeotabDeviceFilterer geotabDeviceFilterer, IGenericGeotabObjectFeeder<LogRecord> logRecordGeotabObjectFeeder, IGeotabLogRecordDbLogRecord2ObjectMapper geotabLogRecordDbLogRecord2ObjectMapper, IMinimumIntervalSampler<LogRecord> minimumIntervalSampler, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IVSSConfiguration vssConfiguration, IVSSObjectMapper vssObjectMapper, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.dbLogRecord2DbEntityMetadata2EntityMapper = dbLogRecord2DbEntityMetadata2EntityMapper;
            this.exceptionHelper = exceptionHelper;
            this.dbEntityMetadata2EntityPersister = dbEntityMetadata2EntityPersister;
            this.dbLogRecord2EntityPersister = dbLogRecord2EntityPersister;
            this.dbOVDSServerCommandEntityPersister = dbOVDSServerCommandEntityPersister;
            this.geotabDeviceFilterer = geotabDeviceFilterer;
            this.logRecordGeotabObjectFeeder = logRecordGeotabObjectFeeder;
            this.geotabLogRecordDbLogRecord2ObjectMapper = geotabLogRecordDbLogRecord2ObjectMapper;
            this.minimumIntervalSampler = minimumIntervalSampler;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.vssConfiguration = vssConfiguration;
            this.vssObjectMapper = vssObjectMapper;

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);
        }

        /// <summary>
        /// Iteratively executes the business logic until the service is stopped.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.LogRecordFeedIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait if necessary.
                var prerequisiteServices = new List<AdapterService>
                {
                    AdapterService.DeviceProcessor2
                };
                await awaiter.WaitForPrerequisiteServicesIfNeededAsync(prerequisiteServices, stoppingToken);
                await awaiter.WaitForDatabaseMaintenanceCompletionIfNeededAsync(stoppingToken);
                await awaiter.WaitForConnectivityRestorationIfNeededAsync(stoppingToken);
                await WaitForVSSConfigurationIfNeededAsync(stoppingToken);

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        var dbOServiceTracking = await serviceTracker.GetLogRecordService2InfoAsync();

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
                        var dbLogRecord2sToPersist = new List<DbLogRecord2>();
                        var logRecord2DbEntityMetadata2sToPersist = new List<DbEntityMetadata2>();
                        var dbOVDSServerCommandsToPersist = new List<DbOVDSServerCommand>();
                        if (logRecords.Count != 0)
                        {
                            // Apply tracked device filter and/or interval sampling (if configured in appsettings.json) and then map the LogRecords to DbLogRecord2s.
                            var filteredLogRecords = await geotabDeviceFilterer.ApplyDeviceFilterAsync(cancellationTokenSource, logRecords);
                            filteredLogRecords = await minimumIntervalSampler.ApplyMinimumIntervalAsync(cancellationTokenSource, filteredLogRecords);
                            dbLogRecord2sToPersist = geotabLogRecordDbLogRecord2ObjectMapper.CreateEntities(filteredLogRecords);
                            logRecord2DbEntityMetadata2sToPersist = dbLogRecord2DbEntityMetadata2EntityMapper.CreateEntities(dbLogRecord2sToPersist);

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
                                    // DbLogRecord2 and DbEntityMetadata2:
                                    if (logRecordOutputOption == VSSOutputOptions.AdapterRecordOnly || logRecordOutputOption == VSSOutputOptions.AdapterRecordAndDbOVDSServerCommand)
                                    {
                                        await dbLogRecord2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbLogRecord2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                        await dbEntityMetadata2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, logRecord2DbEntityMetadata2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);
                                    }

                                    // DbOVDSServerCommands:
                                    if (logRecordOutputOption == VSSOutputOptions.DbOVDSServerCommandOnly || logRecordOutputOption == VSSOutputOptions.AdapterRecordAndDbOVDSServerCommand)
                                    {
                                        await dbOVDSServerCommandEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbOVDSServerCommandsToPersist, cancellationTokenSource, Logging.LogLevel.Info);
                                    }

                                    // DbOServiceTracking (for LogRecordProcessor):
                                    if (dbLogRecord2sToPersist.Count != 0)
                                    {
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.LogRecordProcessor2, logRecordGeotabObjectFeeder.LastFeedRetrievalTimeUtc, logRecordGeotabObjectFeeder.LastFeedVersion);
                                    }
                                    else
                                    {
                                        // No LogRecords were returned, but the OServiceTracking record for this service still needs to be updated to show that the service is operating.
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.LogRecordProcessor2, DateTime.UtcNow);
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
                    exceptionHelper.LogException(databaseConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    stateMachine.HandleException(databaseConnectionException, NLogLogLevelName.Error);
                }
                catch (MyGeotabConnectionException myGeotabConnectionException)
                {
                    exceptionHelper.LogException(myGeotabConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    stateMachine.HandleException(myGeotabConnectionException, NLogLogLevelName.Error);

                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
                    stateMachine.HandleException(ex, NLogLogLevelName.Fatal);
                }

                // If the feed is up-to-date, add a delay equivalent to the configured interval.
                if (logRecordGeotabObjectFeeder.FeedCurrent == true)
                {
                    // Add a delay equivalent to the configured interval.
                    await awaiter.WaitForConfiguredIntervalAsync(delayTimeSpan, DelayIntervalType.Feed, stoppingToken);
                }
            }
        }

        /// <summary>
        /// Starts the current <see cref="LogRecordProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.LogRecordProcessor, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.LogRecordProcessor2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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

            // Register this service with the StateMachine. Set mustPauseForDatabaseMaintenance to true if the service is enabled or false otherwise.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(LogRecordProcessor2), adapterConfiguration.EnableLogRecordFeed);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableLogRecordFeed == true)
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
        /// Stops the current <see cref="LogRecordProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(LogRecordProcessor2), false);
            }

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// If outputting to VSS, ensure that the VSSConfiguration is initialized. This is to allow the current service to operate independently of the OVDSClientWorker.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        async Task WaitForVSSConfigurationIfNeededAsync(CancellationToken cancellationToken)
        {
            if (vssConfiguration.EnableVSSAddOn == true && vssConfiguration.OutputLogRecordsToOVDS == true && vssConfiguration.IsInitialized == false)
            {
                await vssConfiguration.InitializeAsync(AppContext.BaseDirectory, vssConfiguration.VSSPathMapFileName);
            }
        }
    }
}
