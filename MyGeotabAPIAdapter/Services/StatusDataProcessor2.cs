using Geotab.Checkmate.ObjectModel.Engine;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Add_Ons.VSS;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Configuration.Add_Ons.VSS;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Caches;
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
        readonly IGenericEntityPersister<DbOVDSServerCommand> dbOVDSServerCommandEntityPersister;
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
        readonly IVSSConfiguration vssConfiguration;
        readonly IVSSObjectMapper vssObjectMapper;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusDataProcessor2"/> class.
        /// </summary>
        public StatusDataProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<StatusDataProcessor2> awaiter, IDbStatusData2DbEntityMetadata2EntityMapper dbStatusData2DbEntityMetadata2EntityMapper, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbEntityMetadata2> dbEntityMetadata2EntityPersister, IGenericEntityPersister<DbStatusData2> dbStatusData2EntityPersister, IGenericEntityPersister<DbStatusDataLocation2> dbStatusDataLocation2EntityPersister, IGenericEntityPersister<DbOVDSServerCommand> dbOVDSServerCommandEntityPersister, IGenericGeotabGUIDCacheableDbObjectCache2<DbDiagnosticId2, AdapterDatabaseUnitOfWorkContext> dbDiagnosticId2ObjectCache, IGeotabDeviceFilterer geotabDeviceFilterer, IGeotabDiagnosticFilterer geotabDiagnosticFilterer, IGeotabIdConverter geotabIdConverter, IGenericGeotabObjectFeeder<StatusData> statusDataGeotabObjectFeeder, IGeotabStatusDataDbStatusData2ObjectMapper geotabStatusDataDbStatusData2ObjectMapper, IMinimumIntervalSampler<StatusData> minimumIntervalSampler, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IVSSConfiguration vssConfiguration, IVSSObjectMapper vssObjectMapper, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.dbStatusData2DbEntityMetadata2EntityMapper = dbStatusData2DbEntityMetadata2EntityMapper;
            this.exceptionHelper = exceptionHelper;
            this.dbEntityMetadata2EntityPersister = dbEntityMetadata2EntityPersister;
            this.dbStatusData2EntityPersister = dbStatusData2EntityPersister;
            this.dbStatusDataLocation2EntityPersister = dbStatusDataLocation2EntityPersister;
            this.dbOVDSServerCommandEntityPersister = dbOVDSServerCommandEntityPersister;
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
            this.vssConfiguration = vssConfiguration;
            this.vssObjectMapper = vssObjectMapper;

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

                        // For VSS Add-On: Adjust (i.e. reduce) the FeedResultsLimit for StatusData records if they are configured to be output as OVDS server commands.
                        var getFeedResultsLimit = myGeotabAPIHelper.GetFeedResultLimitDefault;
                        if (vssConfiguration.EnableVSSAddOn == true && vssConfiguration.OutputStatusDataToOVDS == true)
                        {
                            getFeedResultsLimit = vssConfiguration.StatusDataFeedResultsLimitWhenOutputtingStatusDataToOVDS;
                        }

                        // Initialize the Geotab object feeder.
                        if (statusDataGeotabObjectFeeder.IsInitialized == false)
                        {
                            await statusDataGeotabObjectFeeder.InitializeAsync(cancellationTokenSource, adapterConfiguration.StatusDataFeedIntervalSeconds, getFeedResultsLimit, (long?)dbOServiceTracking.LastProcessedFeedVersion);
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

                        // Determine StatusData output option.
                        VSSOutputOptions statusDataOutputOption = vssConfiguration.GetVSSOutputOptionForStatusData();

                        // Process any returned StatusDatas.
                        var statusDatas = statusDataGeotabObjectFeeder.GetFeedResultDataValuesList();
                        var dbStatusData2sToPersist = new List<DbStatusData2>();
                        var dbStatusDataLocation2sToPersist = new List<DbStatusDataLocation2>();
                        var statusData2DbEntityMetadata2sToPersist = new List<DbEntityMetadata2>();
                        var dbOVDSServerCommandsToPersist = new List<DbOVDSServerCommand>();
                        if (statusDatas.Count != 0)
                        {
                            // Apply tracked device filter and/or tracked diagnostic filter and/or interval sampling (if configured in appsettings.json) and then map the StatusDatas to DbStatusData2s.
                            var filteredStatusDatas = await geotabDeviceFilterer.ApplyDeviceFilterAsync(cancellationTokenSource, statusDatas);
                            filteredStatusDatas = await geotabDiagnosticFilterer.ApplyDiagnosticFilterAsync(cancellationTokenSource, filteredStatusDatas);
                            filteredStatusDatas = await minimumIntervalSampler.ApplyMinimumIntervalAsync(cancellationTokenSource, filteredStatusDatas);

                            // Map the filtered StatusDatas to DbStatusData2s.
                            foreach (var statusData in filteredStatusDatas)
                            {
                                var statusDataDeviceId = geotabIdConverter.ToLong(statusData.Device.Id);

                                // Get the id of the record in the DiagnosticIds2 database table that corresponds with the statusData.Diagnostic. Diagnostic Ids are mostly GUIDs, but there may be some that are "ShimIds" that don't have an underlying GUID. For efficiency, where possible, the dbDiagnosticId2ObjectCache is queried using the GUID version of the Id. Otherwise, the "GuidString" is used.
                                var statusDataDiagnostic = statusData.Diagnostic;
                                var statusDataDiagnosticId = statusDataDiagnostic.Id;
                                var statusDataDiagnosticGeotabIdType = geotabIdConverter.GetGeotabIdType(statusDataDiagnosticId);
                                long? statusDataDiagnosticDbId = null;
                                if (statusDataDiagnosticGeotabIdType == GeotabIdType.GuidId)
                                {
                                    var statusDataDiagnosticGuid = geotabIdConverter.ToGuid(statusDataDiagnosticId);
                                    statusDataDiagnosticDbId = await dbDiagnosticId2ObjectCache.GetObjectIdByGeotabGUIDAsync(statusDataDiagnosticGuid);
                                }
                                else if (statusDataDiagnosticGeotabIdType == GeotabIdType.NamedGuidId || statusDataDiagnosticGeotabIdType == GeotabIdType.ShimId)
                                {
                                    var statusDataDiagnosticGuidString = geotabIdConverter.ToGuidString(statusDataDiagnosticId);
                                    statusDataDiagnosticDbId = await dbDiagnosticId2ObjectCache.GetObjectIdByGeotabGUIDStringAsync(statusDataDiagnosticGuidString);
                                }
  
                                if (statusDataDiagnosticDbId == null)
                                {
                                    logger.Warn($"Could not process {nameof(StatusData)} with GeotabId {statusData.Id}' because a {nameof(DbDiagnosticId2)} with a {nameof(DbDiagnosticId2.GeotabId)} matching the {nameof(StatusData.Diagnostic.Id)} could not be found.");
                                    continue;
                                }

                                var dbStatusData2 = geotabStatusDataDbStatusData2ObjectMapper.CreateEntity(statusData, statusDataDeviceId, (long)statusDataDiagnosticDbId);
                                dbStatusData2sToPersist.Add(dbStatusData2);

                                DbStatusDataLocation2 dbStatusDataLocation2 = new()
                                { 
                                    id = dbStatusData2.id,
                                    DeviceId = dbStatusData2.DeviceId,
                                    DateTime = dbStatusData2.DateTime,
                                    DatabaseWriteOperationType  = Common.DatabaseWriteOperationType.Insert,
                                    LongLatProcessed = false,
                                    RecordLastChangedUtc = dbStatusData2.RecordCreationTimeUtc
                                };
                                dbStatusDataLocation2sToPersist.Add(dbStatusDataLocation2);
                            }

                            // Generate DbOVDSServerCommands if dictated by the configured VSSOutputOption.
                            if (statusDataOutputOption == VSSOutputOptions.DbOVDSServerCommandOnly || statusDataOutputOption == VSSOutputOptions.AdapterRecordAndDbOVDSServerCommand)
                            {
                                dbOVDSServerCommandsToPersist = vssObjectMapper.GetDbOVDSServerSetCommands(filteredStatusDatas);
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
                                    if (statusDataOutputOption == VSSOutputOptions.AdapterRecordOnly || statusDataOutputOption == VSSOutputOptions.AdapterRecordAndDbOVDSServerCommand)
                                    {
                                        await dbStatusData2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbStatusData2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                        await dbStatusDataLocation2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbStatusDataLocation2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                        await dbEntityMetadata2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, statusData2DbEntityMetadata2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);
                                    }

                                    // DbOVDSServerCommands:
                                    if (statusDataOutputOption == VSSOutputOptions.DbOVDSServerCommandOnly || statusDataOutputOption == VSSOutputOptions.AdapterRecordAndDbOVDSServerCommand)
                                    {
                                        await dbOVDSServerCommandEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbOVDSServerCommandsToPersist, cancellationTokenSource, Logging.LogLevel.Info);
                                    }

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
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(StatusDataProcessor2), adapterConfiguration.EnableStatusDataFeed);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableStatusDataFeed == true)
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
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(StatusDataProcessor2), false);
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
            if (vssConfiguration.EnableVSSAddOn == true && vssConfiguration.OutputStatusDataToOVDS == true && vssConfiguration.IsInitialized == false)
            {
                await vssConfiguration.InitializeAsync(AppContext.BaseDirectory, vssConfiguration.VSSPathMapFileName);
            }
        }
    }
}
