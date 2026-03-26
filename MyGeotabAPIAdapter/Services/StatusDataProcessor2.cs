using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Caches;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityMappers;
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
    /// A <see cref="BackgroundService"/> that extracts <see cref="StatusData"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database.
    /// </summary>
    class StatusDataProcessor2 : BackgroundService
    {
        bool feedVersionRollbackRequired = false;

        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<StatusDataProcessor2> awaiter;
        readonly IDbStatusData2DbEntityMetadata2EntityMapper dbStatusData2DbEntityMetadata2EntityMapper;
        readonly IExceptionHelper exceptionHelper;
        readonly IForeignKeyServiceDependencyMap statusDataForeignKeyServiceDependencyMap;
        readonly IGenericEntityPersister<DbEntityMetadata2> dbEntityMetadata2EntityPersister;
        readonly IGenericEntityPersister<DbStatusData2> dbStatusData2EntityPersister;
        readonly IGenericEntityPersister<DbStatusDataLocation2> dbStatusDataLocation2EntityPersister;
        readonly IGenericGeotabGUIDCacheableDbObjectCache2<DbDiagnosticId2, AdapterDatabaseUnitOfWorkContext> dbDiagnosticId2ObjectCache;
        readonly IGenericGeotabObjectFeeder<StatusData> statusDataGeotabObjectFeeder;
        readonly IGeotabDeviceFilterer geotabDeviceFilterer;
        readonly IGeotabDiagnosticFilterer geotabDiagnosticFilterer;
        readonly IGeotabIdConverter geotabIdConverter;
        readonly IGeotabStatusDataDbStatusData2ObjectMapper geotabStatusDataDbStatusData2ObjectMapper;
        readonly IMinimumIntervalSampler<StatusData> minimumIntervalSampler;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;
        readonly IUnknownDiagnosticIdTracker unknownDiagnosticIdTracker;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusDataProcessor2"/> class.
        /// </summary>
        public StatusDataProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<StatusDataProcessor2> awaiter, IDbStatusData2DbEntityMetadata2EntityMapper dbStatusData2DbEntityMetadata2EntityMapper, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbEntityMetadata2> dbEntityMetadata2EntityPersister, IGenericEntityPersister<DbStatusData2> dbStatusData2EntityPersister, IGenericEntityPersister<DbStatusDataLocation2> dbStatusDataLocation2EntityPersister, IGenericGeotabGUIDCacheableDbObjectCache2<DbDiagnosticId2, AdapterDatabaseUnitOfWorkContext> dbDiagnosticId2ObjectCache, IGeotabDeviceFilterer geotabDeviceFilterer, IGeotabDiagnosticFilterer geotabDiagnosticFilterer, IGeotabIdConverter geotabIdConverter, IGenericGeotabObjectFeeder<StatusData> statusDataGeotabObjectFeeder, IGeotabStatusDataDbStatusData2ObjectMapper geotabStatusDataDbStatusData2ObjectMapper, IMinimumIntervalSampler<StatusData> minimumIntervalSampler, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IUnknownDiagnosticIdTracker unknownDiagnosticIdTracker, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.dbStatusData2DbEntityMetadata2EntityMapper = dbStatusData2DbEntityMetadata2EntityMapper;
            this.exceptionHelper = exceptionHelper;
            this.dbEntityMetadata2EntityPersister = dbEntityMetadata2EntityPersister;
            this.dbStatusData2EntityPersister = dbStatusData2EntityPersister;
            this.dbStatusDataLocation2EntityPersister = dbStatusDataLocation2EntityPersister;
            this.dbDiagnosticId2ObjectCache = dbDiagnosticId2ObjectCache;
            this.geotabDeviceFilterer = geotabDeviceFilterer;
            this.geotabDiagnosticFilterer = geotabDiagnosticFilterer;
            this.geotabIdConverter = geotabIdConverter;
            this.statusDataGeotabObjectFeeder = statusDataGeotabObjectFeeder;
            this.geotabStatusDataDbStatusData2ObjectMapper = geotabStatusDataDbStatusData2ObjectMapper;
            this.minimumIntervalSampler = minimumIntervalSampler;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.unknownDiagnosticIdTracker = unknownDiagnosticIdTracker;

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            // Setup the foreign key service dependency map.
            statusDataForeignKeyServiceDependencyMap = new ForeignKeyServiceDependencyMap(
                [
                    new ForeignKeyServiceDependency("FK_StatusData2_Devices2", AdapterService.DeviceProcessor2),
                    new ForeignKeyServiceDependency("FK_StatusData2_DiagnosticIds2", AdapterService.DiagnosticProcessor2)
                ]
            );
        }

        /// <summary>
        /// Iteratively executes the business logic until the service is stopped.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.StatusDataFeedIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait if necessary.
                var prerequisiteServices = new List<AdapterService>
                {
                    AdapterService.DatabaseMaintenanceService2,
                    AdapterService.DeviceProcessor2,
                    AdapterService.DiagnosticProcessor2
                };
                await awaiter.WaitForPrerequisiteServicesIfNeededAsync(prerequisiteServices, stoppingToken);
                await awaiter.WaitForDatabaseMaintenanceCompletionIfNeededAsync(stoppingToken);
                var connectivityRestored = await awaiter.WaitForConnectivityRestorationIfNeededAsync(stoppingToken);
                if (connectivityRestored == true)
                {
                    feedVersionRollbackRequired = true;
                    connectivityRestored = false;
                }

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        await InitializeOrUpdateCachesAsync(cancellationTokenSource);

                        var dbOServiceTracking = await serviceTracker.GetStatusDataService2InfoAsync();

                        // Initialize the Geotab object feeder.
                        if (statusDataGeotabObjectFeeder.IsInitialized == false)
                        {
                            await statusDataGeotabObjectFeeder.InitializeAsync(cancellationTokenSource, adapterConfiguration.StatusDataFeedIntervalSeconds, myGeotabAPIHelper.GetFeedResultLimitDefault, (long?)dbOServiceTracking.LastProcessedFeedVersion);
                        }

                        // If this is the first iteration after a connectivity disruption, roll-back the LastFeedVersion of the GeotabObjectFeeder to the last processed feed version that was committed to the database and set the LastFeedRetrievalTimeUtc to DateTime.MinValue to start processing without further delay.
                        if (feedVersionRollbackRequired == true)
                        {
                            statusDataGeotabObjectFeeder.Rollback(dbOServiceTracking.LastProcessedFeedVersion);
                            feedVersionRollbackRequired = false;
                        }

                        // Get a batch of StatusData objects from Geotab.
                        await statusDataGeotabObjectFeeder.GetFeedDataBatchAsync(cancellationTokenSource);
                        stoppingToken.ThrowIfCancellationRequested();

                        // Process any returned StatusDatas.
                        var statusDatas = statusDataGeotabObjectFeeder.GetFeedResultDataValuesList();
                        var dbStatusData2sToPersist = new List<DbStatusData2>();
                        var dbStatusDataLocation2sToPersist = new List<DbStatusDataLocation2>();
                        var statusData2DbEntityMetadata2sToPersist = new List<DbEntityMetadata2>();
                        if (statusDatas.Count != 0)
                        {
                            // Apply tracked device filter and/or tracked diagnostic filter and/or interval sampling (if configured in appsettings.json) and then map the StatusDatas to DbStatusData2s.
                            var filteredStatusDatas = await geotabDeviceFilterer.ApplyDeviceFilterAsync(cancellationTokenSource, statusDatas);
                            filteredStatusDatas = await geotabDiagnosticFilterer.ApplyDiagnosticFilterAsync(cancellationTokenSource, filteredStatusDatas);
                            filteredStatusDatas = await minimumIntervalSampler.ApplyMinimumIntervalAsync(cancellationTokenSource, filteredStatusDatas);

                            // Map the filtered StatusDatas to DbStatusData2s.
                            // Phase 1: Attempt cache lookup for all records; defer any with unknown diagnostics.
                            var deferredStatusDatas = new List<StatusData>();
                            foreach (var statusData in filteredStatusDatas)
                            {
                                var statusDataDiagnosticDbId = await TryResolveDiagnosticDbIdAsync(statusData.Diagnostic.Id);
                                if (statusDataDiagnosticDbId == null)
                                {
                                    deferredStatusDatas.Add(statusData);
                                    continue;
                                }
                                MapStatusDataToDbEntities(statusData, (long)statusDataDiagnosticDbId, dbStatusData2sToPersist, dbStatusDataLocation2sToPersist);
                            }

                            // Phase 2: If any records were deferred, force a DB cache refresh and retry.
                            if (deferredStatusDatas.Count > 0)
                            {
                                logger.Debug($"{deferredStatusDatas.Count} {nameof(StatusData)} record(s) deferred due to unknown diagnostic IDs. Forcing DB cache refresh and retrying.");
                                await dbDiagnosticId2ObjectCache.UpdateAsync(true);

                                var stillUnresolvedDiagnosticIdStrings = new List<string>();
                                foreach (var statusData in deferredStatusDatas)
                                {
                                    var statusDataDiagnosticId = statusData.Diagnostic.Id;
                                    var statusDataDiagnosticDbId = await TryResolveDiagnosticDbIdAsync(statusDataDiagnosticId);
                                    if (statusDataDiagnosticDbId == null)
                                    {
                                        logger.Warn($"Could not process {nameof(StatusData)} with GeotabId '{statusData.Id}' because a {nameof(DbDiagnosticId2)} with a {nameof(DbDiagnosticId2.GeotabId)} matching the {nameof(StatusData.Diagnostic)}.{nameof(StatusData.Diagnostic.Id)} '{statusDataDiagnosticId}' could not be found (after forced cache refresh). Record will be permanently skipped for this batch.");
                                        stillUnresolvedDiagnosticIdStrings.Add(geotabIdConverter.ToGuidString(statusDataDiagnosticId));
                                        continue;
                                    }
                                    logger.Debug($"{nameof(StatusData)} with GeotabId '{statusData.Id}' resolved on retry after forced DB cache refresh.");
                                    MapStatusDataToDbEntities(statusData, (long)statusDataDiagnosticDbId, dbStatusData2sToPersist, dbStatusDataLocation2sToPersist);
                                }

                                // Register still-unresolved diagnostic IDs for out-of-cycle DiagnosticProcessor2 sync.
                                if (stillUnresolvedDiagnosticIdStrings.Count > 0)
                                {
                                    unknownDiagnosticIdTracker.RegisterUnknownDiagnosticIdStrings(stillUnresolvedDiagnosticIdStrings);
                                }
                            }
                        }

                        stoppingToken.ThrowIfCancellationRequested();

                        if (dbStatusData2sToPersist.Count != 0)
                        {
                            statusData2DbEntityMetadata2sToPersist = dbStatusData2DbEntityMetadata2EntityMapper.CreateEntities(dbStatusData2sToPersist);
                        }

                        // Persist changes to database.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    // DbStatusData2, DbStatusDataLocation2 and DbEntityMetadata2:
                                    await dbStatusData2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbStatusData2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    await dbStatusDataLocation2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbStatusDataLocation2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    await dbEntityMetadata2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, statusData2DbEntityMetadata2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    // DbOServiceTracking (for StatusDataProcessor2):
                                    if (dbStatusData2sToPersist.Count != 0)
                                    {
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.StatusDataProcessor2, statusDataGeotabObjectFeeder.LastFeedRetrievalTimeUtc, statusDataGeotabObjectFeeder.LastFeedVersion);
                                    }
                                    else
                                    {
                                        // No StatusDatas were returned, but the OServiceTracking record for this service still needs to be updated to show that the service is operating.
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.StatusDataProcessor2, DateTime.UtcNow);
                                    }

                                    // Commit transactions:
                                    await adapterUOW.CommitAsync();
                                }
                                catch (Exception ex)
                                {
                                    feedVersionRollbackRequired = true;
                                    await adapterUOW.RollBackAsync();
                                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                                    throw;
                                }
                            }
                        }, new Context());

                        // Clear FeedResultData.
                        statusDataGeotabObjectFeeder.FeedResultData.Clear();
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
                    var exceptionToAnalyze = ex.InnerException ?? ex;
                    if (ForeignKeyExceptionHelper.IsForeignKeyViolationException(exceptionToAnalyze))
                    {
                        var violatedConstraint = ForeignKeyExceptionHelper.GetConstraintNameFromException(exceptionToAnalyze);
                        if (!string.IsNullOrEmpty(violatedConstraint) && statusDataForeignKeyServiceDependencyMap.TryGetDependency(violatedConstraint, out AdapterService prerequisiteService))
                        {
                            await awaiter.WaitForPrerequisiteServiceToProcessEntitiesAsync(prerequisiteService, stoppingToken);
                            // After waiting, this iteration's attempt is considered "handled" by waiting. The next iteration will be the actual retry of the operation.
                            logger.Debug($"Iteration handling for FK violation on '{violatedConstraint}' complete (waited for {prerequisiteService}). Ready for next iteration.");
                        }
                        else
                        {
                            // FK violation occurred, but constraint name not found OR not included in the dependency map.
                            string reason = string.IsNullOrEmpty(violatedConstraint) ? "constraint name not extractable" : $"constraint '{violatedConstraint}' not included in tripForeignKeyServiceDependencyMap";
                            exceptionHelper.LogException(ex, NLogLogLevelName.Fatal, $"{DefaultErrorMessagePrefix} Unhandled FK violation: {reason}.");
                            stateMachine.HandleException(ex, NLogLogLevelName.Fatal);
                        }
                    }
                    else
                    {
                        // Not an FK violation. Treat as fatal.
                        exceptionHelper.LogException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
                        stateMachine.HandleException(ex, NLogLogLevelName.Fatal);
                    }
                }

                // If the feed is up-to-date, add a delay equivalent to the configured interval.
                if (statusDataGeotabObjectFeeder.FeedCurrent == true)
                {
                    // Add a delay equivalent to the configured interval.
                    await awaiter.WaitForConfiguredIntervalAsync(delayTimeSpan, DelayIntervalType.Feed, stoppingToken);
                }
            }
        }

        /// <summary>
        /// Initializes and/or updates any caches used by this class.
        /// </summary>
        /// <returns></returns>
        async Task InitializeOrUpdateCachesAsync(CancellationTokenSource cancellationTokenSource)
        {
            var dbObjectCacheInitializationAndUpdateTasks = new List<Task>();

            // Update the in-memory cache of database objects that correspond with Geotab objects.
            if (dbDiagnosticId2ObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbDiagnosticId2ObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }

            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);
        }

        /// <summary>
        /// Maps a <see cref="StatusData"/> object to <see cref="DbStatusData2"/> and <see cref="DbStatusDataLocation2"/> entities and adds them to the respective lists.
        /// </summary>
        void MapStatusDataToDbEntities(StatusData statusData, long diagnosticDbId, List<DbStatusData2> dbStatusData2sToPersist, List<DbStatusDataLocation2> dbStatusDataLocation2sToPersist)
        {
            var statusDataDeviceId = geotabIdConverter.ToLong(statusData.Device.Id);
            var dbStatusData2 = geotabStatusDataDbStatusData2ObjectMapper.CreateEntity(statusData, statusDataDeviceId, diagnosticDbId);
            dbStatusData2sToPersist.Add(dbStatusData2);

            DbStatusDataLocation2 dbStatusDataLocation2 = new()
            {
                id = dbStatusData2.id,
                DeviceId = dbStatusData2.DeviceId,
                DateTime = dbStatusData2.DateTime,
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                LongLatProcessed = false,
                RecordLastChangedUtc = dbStatusData2.RecordCreationTimeUtc
            };
            dbStatusDataLocation2sToPersist.Add(dbStatusDataLocation2);
        }

        /// <summary>
        /// Attempts to resolve the database ID for a diagnostic from the <see cref="dbDiagnosticId2ObjectCache"/>. Returns <c>null</c> if the diagnostic is not found in the cache.
        /// </summary>
        async Task<long?> TryResolveDiagnosticDbIdAsync(Id diagnosticId)
        {
            var diagnosticGeotabIdType = geotabIdConverter.GetGeotabIdType(diagnosticId);
            if (diagnosticGeotabIdType == GeotabIdType.GuidId)
            {
                var diagnosticGuid = geotabIdConverter.ToGuid(diagnosticId);
                return await dbDiagnosticId2ObjectCache.GetObjectIdByGeotabGUIDAsync(diagnosticGuid);
            }
            else if (diagnosticGeotabIdType == GeotabIdType.NamedGuidId || diagnosticGeotabIdType == GeotabIdType.ShimId)
            {
                var diagnosticGuidString = geotabIdConverter.ToGuidString(diagnosticId);
                return await dbDiagnosticId2ObjectCache.GetObjectIdByGeotabGUIDStringAsync(diagnosticGuidString);
            }
            return null;
        }

        /// <summary>
        /// Starts the current <see cref="StatusDataProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.StatusDataProcessor2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.StatusDataProcessor2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
            stateMachine.RegisterService(nameof(StatusDataProcessor2), adapterConfiguration.EnableStatusDataFeed);

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.EnableStatusDataFeed == true)
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
        /// Stops the current <see cref="StatusDataProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            stateMachine.RegisterService(nameof(StatusDataProcessor2), false);

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
