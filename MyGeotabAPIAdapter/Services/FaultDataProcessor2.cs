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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that extracts <see cref="FaultData"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class FaultDataProcessor2 : BackgroundService
    {
        bool feedVersionRollbackRequired = false;

        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<FaultDataProcessor2> awaiter;
        readonly IDbFaultData2DbEntityMetadata2EntityMapper dbFaultData2DbEntityMetadata2EntityMapper;
        readonly IExceptionHelper exceptionHelper;
        readonly IForeignKeyServiceDependencyMap faultDataForeignKeyServiceDependencyMap;
        readonly IGenericEntityPersister<DbEntityMetadata2> dbEntityMetadata2EntityPersister;
        readonly IGenericEntityPersister<DbFaultData2> dbFaultData2EntityPersister;
        readonly IGenericEntityPersister<DbFaultDataLocation2> dbFaultDataLocation2EntityPersister;
        readonly IGenericGeotabGUIDCacheableDbObjectCache2<DbDiagnosticId2, AdapterDatabaseUnitOfWorkContext> dbDiagnosticId2ObjectCache;
        readonly IGenericGeotabObjectFeeder<FaultData> faultDataGeotabObjectFeeder;
        readonly IGenericGeotabObjectHydrator<Controller> controllerGeotabObjectHydrator;
        readonly IGenericGeotabObjectHydrator<FailureMode> failureModeGeotabObjectHydrator;
        readonly IGeotabDeviceFilterer geotabDeviceFilterer;
        readonly IGeotabDiagnosticFilterer geotabDiagnosticFilterer;
        readonly IGeotabFaultDataDbFaultData2ObjectMapper geotabFaultDataDbFaultData2ObjectMapper;
        readonly IGeotabIdConverter geotabIdConverter;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="FaultDataProcessor2"/> class.
        /// </summary>
        public FaultDataProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<FaultDataProcessor2> awaiter, IDbFaultData2DbEntityMetadata2EntityMapper dbFaultData2DbEntityMetadata2EntityMapper, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbEntityMetadata2> dbEntityMetadata2EntityPersister, IGenericEntityPersister<DbFaultData2> dbFaultData2EntityPersister, IGenericEntityPersister<DbFaultDataLocation2> dbFaultDataLocation2EntityPersister, IGenericGeotabGUIDCacheableDbObjectCache2<DbDiagnosticId2, AdapterDatabaseUnitOfWorkContext> dbDiagnosticId2ObjectCache, IGenericGeotabObjectFeeder<FaultData> faultDataGeotabObjectFeeder, IGenericGeotabObjectHydrator<Controller> controllerGeotabObjectHydrator, IGenericGeotabObjectHydrator<FailureMode> failureModeGeotabObjectHydrator, IGeotabDeviceFilterer geotabDeviceFilterer, IGeotabDiagnosticFilterer geotabDiagnosticFilterer, IGeotabFaultDataDbFaultData2ObjectMapper geotabFaultDataDbFaultData2ObjectMapper, IGeotabIdConverter geotabIdConverter, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.dbFaultData2DbEntityMetadata2EntityMapper = dbFaultData2DbEntityMetadata2EntityMapper;
            this.exceptionHelper = exceptionHelper;
            this.dbEntityMetadata2EntityPersister = dbEntityMetadata2EntityPersister;
            this.dbFaultData2EntityPersister = dbFaultData2EntityPersister;
            this.dbFaultDataLocation2EntityPersister = dbFaultDataLocation2EntityPersister;
            this.dbDiagnosticId2ObjectCache = dbDiagnosticId2ObjectCache;
            this.faultDataGeotabObjectFeeder = faultDataGeotabObjectFeeder;
            this.controllerGeotabObjectHydrator = controllerGeotabObjectHydrator;
            this.failureModeGeotabObjectHydrator = failureModeGeotabObjectHydrator;
            this.geotabDeviceFilterer = geotabDeviceFilterer;
            this.geotabDiagnosticFilterer = geotabDiagnosticFilterer;
            this.geotabFaultDataDbFaultData2ObjectMapper = geotabFaultDataDbFaultData2ObjectMapper;
            this.geotabIdConverter = geotabIdConverter;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            // Setup the foreign key service dependency map.
            faultDataForeignKeyServiceDependencyMap = new ForeignKeyServiceDependencyMap(
                [
                    new ForeignKeyServiceDependency("FK_FaultData2_Devices2", AdapterService.DeviceProcessor2),
                    new ForeignKeyServiceDependency("FK_FaultData2_DiagnosticIds2", AdapterService.DiagnosticProcessor2),
                    new ForeignKeyServiceDependency("FK_FaultData2_Users2", AdapterService.UserProcessor2)
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
            var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.FaultDataFeedIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait if necessary.
                var prerequisiteServices = new List<AdapterService> 
                {
                    AdapterService.ControllerProcessor2,
                    AdapterService.DeviceProcessor2,
                    AdapterService.DiagnosticProcessor2,
                    AdapterService.FailureModeProcessor2,
                    AdapterService.UserProcessor2
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

                        var dbOServiceTracking = await serviceTracker.GetFaultDataService2InfoAsync();

                        // Initialize the Geotab object feeder.
                        if (faultDataGeotabObjectFeeder.IsInitialized == false)
                        {
                            await faultDataGeotabObjectFeeder.InitializeAsync(cancellationTokenSource, adapterConfiguration.FaultDataFeedIntervalSeconds, myGeotabAPIHelper.GetFeedResultLimitDefault, (long?)dbOServiceTracking.LastProcessedFeedVersion);
                        }

                        // If this is the first iteration after a connectivity disruption, roll-back the LastFeedVersion of the GeotabObjectFeeder to the last processed feed version that was committed to the database and set the LastFeedRetrievalTimeUtc to DateTime.MinValue to start processing without further delay.
                        if (feedVersionRollbackRequired == true)
                        {
                            faultDataGeotabObjectFeeder.Rollback(dbOServiceTracking.LastProcessedFeedVersion);
                            feedVersionRollbackRequired = false;
                        }

                        // Get a batch of FaultData objects from Geotab.
                        await faultDataGeotabObjectFeeder.GetFeedDataBatchAsync(cancellationTokenSource);
                        stoppingToken.ThrowIfCancellationRequested();

                        // Process any returned FaultDatas.
                        var faultDatas = faultDataGeotabObjectFeeder.GetFeedResultDataValuesList();
                        var dbFaultData2sToPersist = new List<DbFaultData2>();
                        var dbFaultDataLocation2sToPersist = new List<DbFaultDataLocation2>();
                        var faultData2DbEntityMetadata2sToPersist = new List<DbEntityMetadata2>();
                        if (faultDatas.Count != 0)
                        {
                            // Apply tracked device filter and/or tracked diagnostic filter (if configured in appsettings.json).
                            var filteredFaultDatas = await geotabDeviceFilterer.ApplyDeviceFilterAsync(cancellationTokenSource, faultDatas);
                            filteredFaultDatas = await geotabDiagnosticFilterer.ApplyDiagnosticFilterAsync(cancellationTokenSource, filteredFaultDatas);

                            // Hydrate child objects.
                            foreach (var filteredFaultData in filteredFaultDatas)
                            {
                                var hydratedController = controllerGeotabObjectHydrator.HydrateEntity(filteredFaultData.Controller, NoController.Value);
                                filteredFaultData.Controller = hydratedController;
                                var hydratedFailureMode = failureModeGeotabObjectHydrator.HydrateEntity(filteredFaultData.FailureMode, NoFailureMode.Value);
                                filteredFaultData.FailureMode = hydratedFailureMode;
                            }

                            // Map the FaultData objects to DbFaultData2 entities.
                            foreach (var faultData in filteredFaultDatas)
                            {
                                long? faultDataDeviceId = null;
                                if (faultData.Device != null && faultData.Device.Id != null)
                                {
                                    faultDataDeviceId = geotabIdConverter.ToLong(faultData.Device.Id);
                                }

                                long? faultDataDismissUserId = null;
                                if (faultData.DismissUser != null && faultData.DismissUser.Id != null && faultData.DismissUser.GetType() != typeof(NoUser))
                                {
                                    faultDataDismissUserId = geotabIdConverter.ToLong(faultData.DismissUser.Id);
                                }

                                // Get the id of the record in the DiagnosticIds2 database table that corresponds with the faultData.Diagnostic. Diagnostic Ids are mostly GUIDs, but there may be some that are "ShimIds" that don't have an underlying GUID. For efficiency, where possible, the dbDiagnosticId2ObjectCache is queried using the GUID version of the Id. Otherwise, the "GuidString" is used.
                                var faultDataDiagnostic = faultData.Diagnostic;
                                var faultDataDiagnosticId = faultDataDiagnostic.Id;
                                var faultDataDiagnosticGeotabIdType = geotabIdConverter.GetGeotabIdType(faultDataDiagnosticId);
                                long? faultDataDiagnosticDbId = null;
                                if (faultDataDiagnosticGeotabIdType == GeotabIdType.GuidId)
                                {
                                    var faultDataDiagnosticGuid = geotabIdConverter.ToGuid(faultDataDiagnosticId);
                                    faultDataDiagnosticDbId = await dbDiagnosticId2ObjectCache.GetObjectIdByGeotabGUIDAsync(faultDataDiagnosticGuid);
                                }
                                else if (faultDataDiagnosticGeotabIdType == GeotabIdType.NamedGuidId || faultDataDiagnosticGeotabIdType == GeotabIdType.ShimId)
                                {
                                    var faultDataDiagnosticGuidString = geotabIdConverter.ToGuidString(faultDataDiagnosticId);
                                    faultDataDiagnosticDbId = await dbDiagnosticId2ObjectCache.GetObjectIdByGeotabGUIDStringAsync(faultDataDiagnosticGuidString);
                                }

                                if (faultDataDeviceId == null)
                                {
                                    logger.Warn($"Could not process {nameof(FaultData)} with GeotabId {faultData.Id}' because its {nameof(FaultData.Device)} is null.");
                                    continue;
                                }

                                if (faultDataDiagnosticDbId == null)
                                {
                                    logger.Warn($"Could not process {nameof(FaultData)} with GeotabId {faultData.Id}' because a {nameof(DbDiagnosticId2)} with a {nameof(DbDiagnosticId2.GeotabId)} matching the {nameof(FaultData.Diagnostic.Id)} could not be found.");
                                    continue;
                                }

                                var dbFaultData2 = geotabFaultDataDbFaultData2ObjectMapper.CreateEntity(faultData, (long)faultDataDeviceId, (long)faultDataDiagnosticDbId, faultDataDismissUserId);
                                dbFaultData2sToPersist.Add(dbFaultData2);

                                DbFaultDataLocation2 dbFaultDataLocation2 = new()
                                { 
                                    id = dbFaultData2.id,
                                    DeviceId = dbFaultData2.DeviceId,
                                    DateTime = dbFaultData2.DateTime,
                                    DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                                    LongLatProcessed = false,
                                    RecordLastChangedUtc = dbFaultData2.RecordCreationTimeUtc
                                };
                                dbFaultDataLocation2sToPersist.Add(dbFaultDataLocation2);
                            }
                        }

                        stoppingToken.ThrowIfCancellationRequested();

                        if (dbFaultData2sToPersist.Count != 0)
                        {
                            faultData2DbEntityMetadata2sToPersist = dbFaultData2DbEntityMetadata2EntityMapper.CreateEntities(dbFaultData2sToPersist);
                        }

                        // Persist changes to database.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    // DbFaultData2, DbFaultDataLocation2 and DbEntityMetadata2:
                                    await dbFaultData2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbFaultData2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    await dbFaultDataLocation2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbFaultDataLocation2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    await dbEntityMetadata2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, faultData2DbEntityMetadata2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    // DbOServiceTracking:
                                    if (dbFaultData2sToPersist.Count != 0)
                                    {
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.FaultDataProcessor2, faultDataGeotabObjectFeeder.LastFeedRetrievalTimeUtc, faultDataGeotabObjectFeeder.LastFeedVersion);
                                    }
                                    else
                                    {
                                        // No FaultDatas were returned, but the OServiceTracking record for this service still needs to be updated to show that the service is operating.
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.FaultDataProcessor2, DateTime.UtcNow);
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
                        faultDataGeotabObjectFeeder.FeedResultData.Clear();
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
                        if (!string.IsNullOrEmpty(violatedConstraint) && faultDataForeignKeyServiceDependencyMap.TryGetDependency(violatedConstraint, out AdapterService prerequisiteService))
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
                if (faultDataGeotabObjectFeeder.FeedCurrent == true)
                {
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
        /// Starts the current <see cref="FaultDataProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.FaultDataProcessor2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.FaultDataProcessor2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
                stateMachine.RegisterService(nameof(FaultDataProcessor2), adapterConfiguration.EnableFaultDataFeed);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableFaultDataFeed == true)
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
        /// Stops the current <see cref="FaultDataProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(FaultDataProcessor2), false);
            }

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
