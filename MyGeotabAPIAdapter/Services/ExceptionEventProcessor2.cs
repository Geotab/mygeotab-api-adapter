using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Exceptions;
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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that extracts <see cref="ExceptionEvent"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class ExceptionEventProcessor2 : BackgroundService
    {
        bool feedVersionRollbackRequired = false;

        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<ExceptionEventProcessor2> awaiter;
        readonly IBaseRepository<DbStgExceptionEvent2> dbStgExceptionEvent2Repo;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbStgExceptionEvent2> dbStgExceptionEvent2EntityPersister;
        readonly IGenericGeotabObjectFeeder<ExceptionEvent> exceptionEventGeotabObjectFeeder;
        readonly IGeotabDeviceFilterer geotabDeviceFilterer;
        readonly IGeotabIdConverter geotabIdConverter;
        readonly IGeotabExceptionEventDbStgExceptionEvent2ObjectMapper geotabExceptionEventDbStgExceptionEvent2ObjectMapper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionEventProcessor2"/> class.
        /// </summary>
        public ExceptionEventProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<ExceptionEventProcessor2> awaiter, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbStgExceptionEvent2> dbStgExceptionEvent2EntityPersister, IGenericGeotabObjectFeeder<ExceptionEvent> exceptionEventGeotabObjectFeeder, IGeotabDeviceFilterer geotabDeviceFilterer, IGeotabIdConverter geotabIdConverter, IGeotabExceptionEventDbStgExceptionEvent2ObjectMapper geotabExceptionEventDbStgExceptionEvent2ObjectMapper, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.exceptionHelper = exceptionHelper;
            this.dbStgExceptionEvent2EntityPersister = dbStgExceptionEvent2EntityPersister;
            this.exceptionEventGeotabObjectFeeder = exceptionEventGeotabObjectFeeder;
            this.geotabDeviceFilterer = geotabDeviceFilterer;
            this.geotabIdConverter = geotabIdConverter;
            this.geotabExceptionEventDbStgExceptionEvent2ObjectMapper = geotabExceptionEventDbStgExceptionEvent2ObjectMapper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;

            dbStgExceptionEvent2Repo = new BaseRepository<DbStgExceptionEvent2>(adapterContext);

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
            const string MergeFunctionSQL_Postgres = @"SELECT public.""spMerge_stg_ExceptionEvents2""();";
            const string MergeProcedureSQL_SQLServer = @"EXEC [dbo].[spMerge_stg_ExceptionEvents2];";
            const string TruncateStagingTableSQL_Postgres = @"TRUNCATE TABLE public.""stg_ExceptionEvents2"";";
            const string TruncateStagingTableSQL_SQLServer = @"TRUNCATE TABLE [dbo].[stg_ExceptionEvents2];";

            MethodBase methodBase = MethodBase.GetCurrentMethod();
            var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.ExceptionEventFeedIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait if necessary.
                var prerequisiteServices = new List<AdapterService>
                {
                    AdapterService.DeviceProcessor2,
                    AdapterService.RuleProcessor2,
                    AdapterService.UserProcessor2
                };
                await awaiter.WaitForPrerequisiteServicesIfNeededAsync(prerequisiteServices, stoppingToken);
                await awaiter.WaitForDatabaseMaintenanceCompletionIfNeededAsync(stoppingToken);
                await awaiter.WaitForConnectivityRestorationIfNeededAsync(stoppingToken);

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        var dbOServiceTracking = await serviceTracker.GetExceptionEventService2InfoAsync();

                        // Initialize the Geotab object feeder.
                        if (exceptionEventGeotabObjectFeeder.IsInitialized == false)
                        {
                            await exceptionEventGeotabObjectFeeder.InitializeAsync(cancellationTokenSource, adapterConfiguration.ExceptionEventFeedIntervalSeconds, myGeotabAPIHelper.GetFeedResultLimitDefault, (long?)dbOServiceTracking.LastProcessedFeedVersion);
                        }

                        // If this is the first iteration after a connectivity disruption, roll-back the LastFeedVersion of the GeotabObjectFeeder to the last processed feed version that was committed to the database and set the LastFeedRetrievalTimeUtc to DateTime.MinValue to start processing without further delay.
                        if (feedVersionRollbackRequired == true)
                        {
                            exceptionEventGeotabObjectFeeder.LastFeedVersion = dbOServiceTracking.LastProcessedFeedVersion;
                            exceptionEventGeotabObjectFeeder.LastFeedRetrievalTimeUtc = DateTime.MinValue;
                            feedVersionRollbackRequired = false;
                        }

                        // Get a batch of ExceptionEvent objects from Geotab.
                        await exceptionEventGeotabObjectFeeder.GetFeedDataBatchAsync(cancellationTokenSource);
                        stoppingToken.ThrowIfCancellationRequested();

                        // Process any returned ExceptionEvents.
                        var exceptionEvents = exceptionEventGeotabObjectFeeder.GetFeedResultDataValuesList();
                        var dbStgExceptionEvent2sToPersist = new List<DbStgExceptionEvent2>();
                        if (exceptionEvents.Count != 0)
                        {
                            // Apply tracked device filter and/or tracked diagnostic filter (if configured in appsettings.json).
                            var filteredExceptionEvents = await geotabDeviceFilterer.ApplyDeviceFilterAsync(cancellationTokenSource, exceptionEvents);

                            // Map the ExceptionEvent objects to DbExceptionEvent2 entities.
                            foreach (var exceptionEvent in filteredExceptionEvents)
                            {
                                long? exceptionEventDeviceId = null;
                                if (exceptionEvent.Device != null && exceptionEvent.Device.Id != null)
                                {
                                    exceptionEventDeviceId = geotabIdConverter.ToLong(exceptionEvent.Device.Id);
                                }

                                long? exceptionEventDriverId = null;
                                if (exceptionEvent.Driver != null && exceptionEvent.Driver.Id != null && exceptionEvent.Driver.GetType() != typeof(UnknownDriver))
                                {
                                    exceptionEventDriverId = geotabIdConverter.ToLong(exceptionEvent.Driver.Id);
                                }

                                if (exceptionEventDeviceId == null)
                                {
                                    logger.Warn($"Could not process {nameof(ExceptionEvent)} with GeotabId {exceptionEvent.Id}' because its {nameof(ExceptionEvent.Device)} is null.");
                                    continue;
                                }

                                var dbStgExceptionEvent2 = geotabExceptionEventDbStgExceptionEvent2ObjectMapper.CreateEntity(exceptionEvent, (long)exceptionEventDeviceId, exceptionEventDriverId);
                                dbStgExceptionEvent2sToPersist.Add(dbStgExceptionEvent2);
                            }
                        }

                        stoppingToken.ThrowIfCancellationRequested();

                        // Persist changes to database. Step 1: Persist the DbStgExceptionEvent2 entities.
                        if (dbStgExceptionEvent2sToPersist.Count != 0)
                        {
                            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                            {
                                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                {
                                    try
                                    {
                                        // Truncate staging table in case it contains any data:
                                        var sql = adapterContext.ProviderType switch
                                        {
                                            ConnectionInfo.DataAccessProviderType.PostgreSQL => TruncateStagingTableSQL_Postgres,
                                            ConnectionInfo.DataAccessProviderType.SQLServer => TruncateStagingTableSQL_SQLServer,
                                            _ => throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.")
                                        };
                                        await dbStgExceptionEvent2Repo.ExecuteAsync(sql, null, cancellationTokenSource, true, adapterContext);

                                        // DbStgExceptionEvent2:
                                        await dbStgExceptionEvent2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbStgExceptionEvent2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

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
                        }

                        // Persist changes to database. Step 2: Merge the DbStgExceptionEvent2 entities into the DbExceptionEvent2 table and update the DbOServiceTracking table.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    if (dbStgExceptionEvent2sToPersist.Count != 0)
                                    {
                                        // Build the SQL statement to execute the merge procedure.
                                        var sql = adapterContext.ProviderType switch
                                        {
                                            ConnectionInfo.DataAccessProviderType.PostgreSQL => MergeFunctionSQL_Postgres,
                                            ConnectionInfo.DataAccessProviderType.SQLServer => MergeProcedureSQL_SQLServer,
                                            _ => throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.")
                                        };

                                        // Execute the merge procedure.
                                        await dbStgExceptionEvent2Repo.ExecuteAsync(sql, null, cancellationTokenSource);
                                    }

                                    // DbOServiceTracking:
                                    if (dbStgExceptionEvent2sToPersist.Count != 0)
                                    {
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.ExceptionEventProcessor2, exceptionEventGeotabObjectFeeder.LastFeedRetrievalTimeUtc, exceptionEventGeotabObjectFeeder.LastFeedVersion);
                                    }
                                    else
                                    {
                                        // No ExceptionEvents were returned, but the OServiceTracking record for this service still needs to be updated to show that the service is operating.
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.ExceptionEventProcessor2, DateTime.UtcNow);
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
                        exceptionEventGeotabObjectFeeder.FeedResultData.Clear();
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
                if (exceptionEventGeotabObjectFeeder.FeedCurrent == true)
                {
                    await awaiter.WaitForConfiguredIntervalAsync(delayTimeSpan, DelayIntervalType.Feed, stoppingToken);
                }
            }
        }

        /// <summary>
        /// Starts the current <see cref="ExceptionEventProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.ExceptionEventProcessor2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.ExceptionEventProcessor2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
                stateMachine.RegisterService(nameof(ExceptionEventProcessor2), adapterConfiguration.EnableExceptionEventFeed);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableExceptionEventFeed == true)
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
        /// Stops the current <see cref="ExceptionEventProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(ExceptionEventProcessor2), false);
            }

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
