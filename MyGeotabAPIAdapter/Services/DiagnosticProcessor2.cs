using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Caches;
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
    /// A <see cref="BackgroundService"/> that extracts <see cref="Diagnostic"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class DiagnosticProcessor2 : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<DiagnosticProcessor2> awaiter;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericGeotabGUIDCacheableDbObjectCache2<DbDiagnosticId2, AdapterDatabaseUnitOfWorkContext> dbDiagnosticId2ObjectCache;
        readonly IGenericGeotabGUIDCacheableDbObjectCache2<DbDiagnostic2, AdapterDatabaseUnitOfWorkContext> dbDiagnostic2ObjectCache;
        readonly IGenericEntityPersister<DbDiagnostic2> dbDiagnostic2EntityPersister;
        readonly IGenericEntityPersister<DbDiagnosticId2> dbDiagnosticId2EntityPersister;
        readonly IGenericGeotabObjectCacher<Diagnostic> diagnosticGeotabObjectCacher;
        readonly IGeotabDiagnosticDbDiagnostic2ObjectMapper geotabDiagnosticDbDiagnostic2ObjectMapper;
        readonly IGeotabDiagnosticDbDiagnosticId2ObjectMapper geotabDiagnosticDbDiagnosticId2ObjectMapper;
        readonly IGeotabIdConverter geotabIdConverter;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticProcessor2"/> class.
        /// </summary>
        public DiagnosticProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<DiagnosticProcessor2> awaiter, IExceptionHelper exceptionHelper, IGenericGeotabGUIDCacheableDbObjectCache2<DbDiagnosticId2, AdapterDatabaseUnitOfWorkContext> dbDiagnosticId2ObjectCache, IGenericGeotabGUIDCacheableDbObjectCache2<DbDiagnostic2, AdapterDatabaseUnitOfWorkContext> dbDiagnostic2ObjectCache, IGenericEntityPersister<DbDiagnostic2> dbDiagnostic2EntityPersister, IGenericEntityPersister<DbDiagnosticId2> dbDiagnosticId2EntityPersister, IGeotabDiagnosticDbDiagnostic2ObjectMapper geotabDiagnosticDbDiagnostic2ObjectMapper, IGeotabDiagnosticDbDiagnosticId2ObjectMapper geotabDiagnosticDbDiagnosticId2ObjectMapper, IGeotabIdConverter geotabIdConverter, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericGeotabObjectCacher<Diagnostic> diagnosticGeotabObjectCacher, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.exceptionHelper = exceptionHelper;
            this.dbDiagnosticId2ObjectCache = dbDiagnosticId2ObjectCache;
            this.dbDiagnostic2ObjectCache = dbDiagnostic2ObjectCache;
            this.dbDiagnostic2EntityPersister = dbDiagnostic2EntityPersister;
            this.dbDiagnosticId2EntityPersister = dbDiagnosticId2EntityPersister;
            this.geotabDiagnosticDbDiagnostic2ObjectMapper = geotabDiagnosticDbDiagnostic2ObjectMapper;
            this.geotabDiagnosticDbDiagnosticId2ObjectMapper = geotabDiagnosticDbDiagnosticId2ObjectMapper;
            this.geotabIdConverter = geotabIdConverter;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.diagnosticGeotabObjectCacher = diagnosticGeotabObjectCacher;

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
            var delayTimeSpan = TimeSpan.FromMinutes(adapterConfiguration.DiagnosticCacheUpdateIntervalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait if necessary.
                var prerequisiteServices = new List<AdapterService> { };
                await awaiter.WaitForPrerequisiteServicesIfNeededAsync(prerequisiteServices, stoppingToken);
                await awaiter.WaitForDatabaseMaintenanceCompletionIfNeededAsync(stoppingToken);
                await awaiter.WaitForConnectivityRestorationIfNeededAsync(stoppingToken);

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        await InitializeOrUpdateCachesAsync(cancellationTokenSource);

                        var dbDiagnostic2sToPersist = new List<DbDiagnostic2>();
                        var newDbDiagnostic2sToPersistDictionary = new Dictionary<string, Common.DatabaseWriteOperationType>();
                        var dbDiagnosticId2sToPersist = new List<DbDiagnosticId2>();
                        var newDbDiagnosticId2sToPersistDictionary = new Dictionary<string, Common.DatabaseWriteOperationType>();
                        // Only propagate the cache to database if the cache has been updated since the last time it was propagated to database.
                        if (diagnosticGeotabObjectCacher.LastUpdatedTimeUTC > diagnosticGeotabObjectCacher.LastPropagatedToDatabaseTimeUtc)
                        {
                            DateTime recordChangedTimestampUtc = DateTime.UtcNow;

                            // Find any diagnostics that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                            var dbDiagnostic2s = await dbDiagnostic2ObjectCache.GetObjectsAsync();
                            foreach (var dbDiagnostic2 in dbDiagnostic2s)
                            {
                                if (dbDiagnostic2.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                                {
                                    bool diagnosticExistsInCache = diagnosticGeotabObjectCacher.GeotabObjectCache.ContainsKey(Id.Create(dbDiagnostic2.GeotabId));
                                    if (!diagnosticExistsInCache)
                                    {
                                        logger.Debug($"Diagnostic '{dbDiagnostic2.GeotabId}' no longer exists in MyGeotab and is being marked as deleted.");
                                        dbDiagnostic2.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                        dbDiagnostic2.RecordLastChangedUtc = recordChangedTimestampUtc;
                                        dbDiagnostic2.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                        dbDiagnostic2sToPersist.Add(dbDiagnostic2);
                                    }
                                }
                            }

                            // Iterate through the in-memory cache of Geotab Diagnostic objects.
                            foreach (var diagnostic in diagnosticGeotabObjectCacher.GeotabObjectCache.Values)
                            {
                                // Get the underlying GUID (in string format due to the presence of ShimIds with Diagnostics) of the Geotab Diagnostic object.
                                string diagnosticGeotabGuid = geotabIdConverter.ToGuidString(diagnostic.Id);

                                // Try to find the existing database record for the cached diagnostic.
                                //var diagnosticGeotabGuid = await dbDiagnostic2ObjectCache.GetObjectGeotabGUIDByGeotabIdAsync(diagnostic.Id.ToString());
                                var dbDiagnostic2 = await dbDiagnostic2ObjectCache.GetObjectByGeotabGUIDAsync(diagnosticGeotabGuid);
                                if (dbDiagnostic2 != null)
                                {
                                    // The DbDiagnostic2 has already been added to the database. Update it if required.
                                    if (geotabDiagnosticDbDiagnostic2ObjectMapper.EntityRequiresUpdate(dbDiagnostic2, diagnostic))
                                    {
                                        DbDiagnostic2 updatedDbDiagnostic2 = geotabDiagnosticDbDiagnostic2ObjectMapper.UpdateEntity(dbDiagnostic2, diagnostic);
                                        dbDiagnostic2sToPersist.Add(updatedDbDiagnostic2);
                                    }

                                    // Check whether the diagnostic Id has changed and, if so, add a new DbDiagnosticId2.
                                    var dbDiagnosticId2s = await dbDiagnosticId2ObjectCache.GetObjectsAsync(diagnosticGeotabGuid, diagnostic.Id.ToString());
                                    if ( dbDiagnosticId2s.Any() == false)
                                    {
                                        var newDbDiagnosticId2 = geotabDiagnosticDbDiagnosticId2ObjectMapper.CreateEntity(diagnostic);
                                        dbDiagnosticId2sToPersist.Add(newDbDiagnosticId2);
                                    }
                                }
                                else
                                {
                                    // The diagnostic has not yet been added to the database.
                                    
                                    // Create a DbDiagnostic2, set its properties and add it to the cache. Also create a corresponding DbDiagnosticId2.
                                    var newDbDiagnostic2 = geotabDiagnosticDbDiagnostic2ObjectMapper.CreateEntity(diagnostic);
                                    var newDbDiagnosticId2 = geotabDiagnosticDbDiagnosticId2ObjectMapper.CreateEntity(diagnostic);

                                    // DbDiagnostic2: There may be multiple records for the same entity in the batch of entities retrieved from Geotab. If there are, make sure that duplicates are set to be updated instead of inserted.
                                    if (newDbDiagnostic2sToPersistDictionary.ContainsKey(newDbDiagnostic2.GeotabId))
                                    {
                                        newDbDiagnostic2.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                    }

                                    dbDiagnostic2sToPersist.Add(newDbDiagnostic2);
                                    if (newDbDiagnostic2.DatabaseWriteOperationType == Common.DatabaseWriteOperationType.Insert)
                                    {
                                        newDbDiagnostic2sToPersistDictionary.Add(newDbDiagnostic2.GeotabId, newDbDiagnostic2.DatabaseWriteOperationType);
                                    }

                                    // DbDiagnosticId2: There may be multiple records for the same entity in the batch of entities retrieved from Geotab. If there are, make sure that duplicates are set to be updated instead of inserted.
                                    if (newDbDiagnosticId2sToPersistDictionary.ContainsKey(newDbDiagnosticId2.GeotabId))
                                    {
                                        newDbDiagnosticId2.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                    }

                                    dbDiagnosticId2sToPersist.Add(newDbDiagnosticId2);
                                    if (newDbDiagnosticId2.DatabaseWriteOperationType == Common.DatabaseWriteOperationType.Insert)
                                    {
                                        newDbDiagnosticId2sToPersistDictionary.Add(newDbDiagnosticId2.GeotabId, newDbDiagnosticId2.DatabaseWriteOperationType);
                                    }
                                }
                            }

                            stoppingToken.ThrowIfCancellationRequested();
                        }
                        else
                        {
                            logger.Debug($"Diagnostic cache in database is up-to-date.");
                        }

                        // Persist changes to database.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    // NOTE: DbDiagnostic2 entities must be upserted before DbDiagnosticId2 entities, so these need to be awaited in order vs. run in parallel.

                                    // DbDiagnostic2:
                                    await dbDiagnostic2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbDiagnostic2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    // DbDiagnosticId2:
                                    await dbDiagnosticId2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbDiagnosticId2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    // DbOServiceTracking:
                                    await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DiagnosticProcessor2, DateTime.UtcNow);

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

                        // If there were any changes, force the DbDiagnostic2 and DbDiagnosticId2 caches to be updated so that the changes are immediately available to other consumers. Run tasks in parallel.
                        var dbDiagnosticCacheUpdateTasks = new List<Task>();
                        if (dbDiagnostic2sToPersist.Any())
                        {
                            dbDiagnosticCacheUpdateTasks.Add(dbDiagnostic2ObjectCache.UpdateAsync(true));
                        }
                        if (dbDiagnostic2sToPersist.Any())
                        {
                            dbDiagnosticCacheUpdateTasks.Add(dbDiagnosticId2ObjectCache.UpdateAsync(true));
                        }
                        await Task.WhenAll(dbDiagnosticCacheUpdateTasks);
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

                // Add a delay equivalent to the configured interval.
                await awaiter.WaitForConfiguredIntervalAsync(delayTimeSpan, DelayIntervalType.Update, stoppingToken);
            }
        }

        /// <summary>
        /// Initializes and/or updates any caches used by this class.
        /// </summary>
        /// <returns></returns>
        async Task InitializeOrUpdateCachesAsync(CancellationTokenSource cancellationTokenSource)
        {
            var dbObjectCacheInitializationAndUpdateTasks = new List<Task>();

            // Update the in-memory cache of Geotab objects obtained via API from the MyGeotab database.
            if (diagnosticGeotabObjectCacher.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(diagnosticGeotabObjectCacher.InitializeAsync(cancellationTokenSource, adapterConfiguration.DiagnosticCacheIntervalDailyReferenceStartTimeUTC, adapterConfiguration.DiagnosticCacheUpdateIntervalMinutes, adapterConfiguration.DiagnosticCacheRefreshIntervalMinutes, myGeotabAPIHelper.GetFeedResultLimitDefault, true));
            }
            else
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(diagnosticGeotabObjectCacher.UpdateGeotabObjectCacheAsync(cancellationTokenSource));
            }

            // Update the in-memory cache of database objects that correspond with Geotab objects.
            if (dbDiagnostic2ObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbDiagnostic2ObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }
            if (dbDiagnosticId2ObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbDiagnosticId2ObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }

            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);
        }

        /// <summary>
        /// Starts the current <see cref="DiagnosticProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.DiagnosticProcessor2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DiagnosticProcessor2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
                stateMachine.RegisterService(nameof(DiagnosticProcessor2), adapterConfiguration.EnableDiagnosticCache);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableDiagnosticCache == true)
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
        /// Stops the current <see cref="DiagnosticProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(DiagnosticProcessor2), false);
            }

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
