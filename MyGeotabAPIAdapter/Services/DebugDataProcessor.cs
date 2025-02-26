using Geotab.Checkmate.ObjectModel;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
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
    /// A <see cref="BackgroundService"/> that extracts <see cref="DebugData"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class DebugDataProcessor : BackgroundService
    {
        bool feedVersionRollbackRequired = false;

        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking> adapterEnvironment;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbDebugData> dbDebugDataEntityPersister;
        readonly IGenericGeotabObjectFeeder<DebugData> debugDataGeotabObjectFeeder;
        readonly IGeotabDeviceFilterer geotabDeviceFilterer;
        readonly IGeotabDebugDataDbDebugDataObjectMapper geotabDebugDataDbDebugDataObjectMapper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IPrerequisiteServiceChecker<DbOServiceTracking> prerequisiteServiceChecker;
        readonly IServiceTracker<DbOServiceTracking> serviceTracker;
        readonly IStateMachine<DbMyGeotabVersionInfo> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugDataProcessor"/> class.
        /// </summary>
        public DebugDataProcessor(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking> adapterEnvironment, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbDebugData> dbDebugDataEntityPersister, IGenericGeotabObjectFeeder<DebugData> debugDataGeotabObjectFeeder, IGeotabDeviceFilterer geotabDeviceFilterer, IGeotabDebugDataDbDebugDataObjectMapper geotabDebugDataDbDebugDataObjectMapper, IMyGeotabAPIHelper myGeotabAPIHelper, IPrerequisiteServiceChecker<DbOServiceTracking> prerequisiteServiceChecker, IServiceTracker<DbOServiceTracking> serviceTracker, IStateMachine<DbMyGeotabVersionInfo> stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.exceptionHelper = exceptionHelper;
            this.dbDebugDataEntityPersister = dbDebugDataEntityPersister;
            this.debugDataGeotabObjectFeeder = debugDataGeotabObjectFeeder;
            this.geotabDeviceFilterer = geotabDeviceFilterer;
            this.geotabDebugDataDbDebugDataObjectMapper = geotabDebugDataDbDebugDataObjectMapper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.prerequisiteServiceChecker = prerequisiteServiceChecker;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;

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
                        var dbOServiceTracking = await serviceTracker.GetDebugDataServiceInfoAsync();

                        // Initialize the Geotab object feeder.
                        if (debugDataGeotabObjectFeeder.IsInitialized == false)
                        {
                            await debugDataGeotabObjectFeeder.InitializeAsync(cancellationTokenSource, adapterConfiguration.DebugDataFeedIntervalSeconds, myGeotabAPIHelper.GetFeedResultLimitDefault, dbOServiceTracking.LastProcessedFeedVersion);
                        }

                        // If this is the first iteration after a connectivity disruption, roll-back the LastFeedVersion of the GeotabObjectFeeder to the last processed feed version that was committed to the database and set the LastFeedRetrievalTimeUtc to DateTime.MinValue to start processing without further delay.
                        if (feedVersionRollbackRequired == true)
                        {
                            debugDataGeotabObjectFeeder.LastFeedVersion = dbOServiceTracking.LastProcessedFeedVersion;
                            debugDataGeotabObjectFeeder.LastFeedRetrievalTimeUtc = DateTime.MinValue;
                            feedVersionRollbackRequired = false;
                        }

                        // Get a batch of DebugData objects from Geotab.
                        await debugDataGeotabObjectFeeder.GetFeedDataBatchAsync(cancellationTokenSource);
                        stoppingToken.ThrowIfCancellationRequested();

                        // Process any returned DebugDatas.
                        var debugDatas = debugDataGeotabObjectFeeder.GetFeedResultDataValuesList();
                        var dbDebugDatasToPersist = new List<DbDebugData>();
                        if (debugDatas.Any())
                        {
                            // Apply tracked device filter (if configured in appsettings.json).
                            var filteredDebugDatas = await geotabDeviceFilterer.ApplyDeviceFilterAsync(cancellationTokenSource, debugDatas);

                            // Map the DebugData objects to DbDebugData entities.
                            dbDebugDatasToPersist = geotabDebugDataDbDebugDataObjectMapper.CreateEntities(filteredDebugDatas);

                            stoppingToken.ThrowIfCancellationRequested();
                        }

                        // Persist changes to database.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    // DbDebugData:
                                    await dbDebugDataEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbDebugDatasToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    // DbOServiceTracking:
                                    if (dbDebugDatasToPersist.Any())
                                    {
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DebugDataProcessor, debugDataGeotabObjectFeeder.LastFeedRetrievalTimeUtc, debugDataGeotabObjectFeeder.LastFeedVersion);
                                    }
                                    else
                                    {
                                        // No DebugDatas were returned, but the OServiceTracking record for this service still needs to be updated to show that the service is operating.
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DebugDataProcessor, DateTime.UtcNow);
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
                        debugDataGeotabObjectFeeder.FeedResultData.Clear();
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
                if (debugDataGeotabObjectFeeder.FeedCurrent == true)
                {
                    var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.DebugDataFeedIntervalSeconds);
                    logger.Info($"{CurrentClassName} pausing for the configured feed interval ({delayTimeSpan}).");
                    await Task.Delay(delayTimeSpan, stoppingToken);
                }
            }
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
        /// Starts the current <see cref="DebugDataProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.DebugDataProcessor, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DebugDataProcessor, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
            if (adapterConfiguration.UseDataModel2 == false && adapterConfiguration.EnableDebugDataFeed == true)
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
        /// Stops the current <see cref="DebugDataProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
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
            var prerequisiteServices = new List<AdapterService>
            {
                AdapterService.DeviceProcessor,
                AdapterService.UserProcessor
            };

            await prerequisiteServiceChecker.WaitForPrerequisiteServicesIfNeededAsync(CurrentClassName, prerequisiteServices, cancellationToken);
        }
    }
}
