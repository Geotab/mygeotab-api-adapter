using Geotab.Checkmate.ObjectModel;
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
    /// A <see cref="BackgroundService"/> that extracts <see cref="ZoneType"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class ZoneTypeProcessor2 : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<ZoneTypeProcessor2> awaiter;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericGenericDbObjectCache<DbZoneType2, AdapterGenericDbObjectCache<DbZoneType2>> dbZoneType2ObjectCache;
        readonly IGenericEntityPersister<DbZoneType2> dbZoneType2EntityPersister;
        readonly IGenericGeotabObjectCacher<ZoneType> zoneTypeGeotabObjectCacher;
        readonly IGeotabZoneTypeDbZoneType2ObjectMapper geotabZoneTypeDbZoneType2ObjectMapper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoneTypeProcessor2"/> class.
        /// </summary>
        public ZoneTypeProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<ZoneTypeProcessor2> awaiter, IExceptionHelper exceptionHelper, IGenericGenericDbObjectCache<DbZoneType2, AdapterGenericDbObjectCache<DbZoneType2>> dbZoneType2ObjectCache, IGenericEntityPersister<DbZoneType2> dbZoneType2EntityPersister, IGeotabZoneTypeDbZoneType2ObjectMapper geotabZoneTypeDbZoneType2ObjectMapper, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericGeotabObjectCacher<ZoneType> zoneTypeGeotabObjectCacher, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.exceptionHelper = exceptionHelper;
            this.dbZoneType2ObjectCache = dbZoneType2ObjectCache;
            this.dbZoneType2EntityPersister = dbZoneType2EntityPersister;
            this.geotabZoneTypeDbZoneType2ObjectMapper = geotabZoneTypeDbZoneType2ObjectMapper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.zoneTypeGeotabObjectCacher = zoneTypeGeotabObjectCacher;

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
            var delayTimeSpan = TimeSpan.FromMinutes(adapterConfiguration.ZoneTypeCacheUpdateIntervalMinutes);

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

                        var dbZoneType2sToPersist = new List<DbZoneType2>();
                        var newDbZoneType2sToPersistDictionary = new Dictionary<string, Common.DatabaseWriteOperationType>();
                        // Only propagate the cache to database if the cache has been updated since the last time it was propagated to database.
                        if (zoneTypeGeotabObjectCacher.LastUpdatedTimeUTC > zoneTypeGeotabObjectCacher.LastPropagatedToDatabaseTimeUtc)
                        {
                            DateTime recordChangedTimestampUtc = DateTime.UtcNow;

                            // Find any zoneTypes that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                            var dbZoneType2s = await dbZoneType2ObjectCache.GetObjectsAsync();
                            foreach (var dbZoneType2 in dbZoneType2s)
                            {
                                if (dbZoneType2.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                                {
                                    bool zoneTypeExistsInCache = zoneTypeGeotabObjectCacher.GeotabObjectCache.ContainsKey(Id.Create(dbZoneType2.GeotabId));
                                    if (!zoneTypeExistsInCache)
                                    {
                                        logger.Debug($"ZoneType '{dbZoneType2.GeotabId}' no longer exists in MyGeotab and is being marked as deleted.");
                                        dbZoneType2.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                        dbZoneType2.RecordLastChangedUtc = recordChangedTimestampUtc;
                                        dbZoneType2.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                        dbZoneType2sToPersist.Add(dbZoneType2);
                                    }
                                }
                            }

                            // Iterate through the in-memory cache of Geotab ZoneType objects.
                            foreach (var zoneType in zoneTypeGeotabObjectCacher.GeotabObjectCache.Values)
                            {
                                // Try to find the existing database record for the cached zoneType.                                
                                var dbZoneType2 = await dbZoneType2ObjectCache.GetObjectAsync(zoneType.Id.ToString());
                                if (dbZoneType2 != null)
                                {
                                    // The zoneType has already been added to the database.
                                    if (geotabZoneTypeDbZoneType2ObjectMapper.EntityRequiresUpdate(dbZoneType2, zoneType))
                                    {
                                        DbZoneType2 updatedDbZoneType2 = geotabZoneTypeDbZoneType2ObjectMapper.UpdateEntity(dbZoneType2, zoneType);
                                        dbZoneType2sToPersist.Add(updatedDbZoneType2);
                                    }
                                }
                                else
                                {
                                    // The zoneType has not yet been added to the database. Create a DbZoneType2, set its properties and add it to the cache.
                                    var newDbZoneType2 = geotabZoneTypeDbZoneType2ObjectMapper.CreateEntity(zoneType);

                                    // There may be multiple records for the same entity in the batch of entities retrieved from Geotab. If there are, make sure that duplicates are set to be updated instead of inserted.
                                    if (newDbZoneType2sToPersistDictionary.ContainsKey(newDbZoneType2.GeotabId))
                                    {
                                        newDbZoneType2.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                    }

                                    dbZoneType2sToPersist.Add(newDbZoneType2);
                                    if (newDbZoneType2.DatabaseWriteOperationType == Common.DatabaseWriteOperationType.Insert)
                                    {
                                        newDbZoneType2sToPersistDictionary.Add(newDbZoneType2.GeotabId, newDbZoneType2.DatabaseWriteOperationType);
                                    }
                                }
                            }

                            stoppingToken.ThrowIfCancellationRequested();
                        }
                        else
                        {
                            logger.Debug($"ZoneType cache in database is up-to-date.");
                        }

                        // Persist changes to database.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    // DbZoneType2:
                                    await dbZoneType2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbZoneType2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    // DbOServiceTracking:
                                    await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.ZoneTypeProcessor2, DateTime.UtcNow);

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

                        // If there were any changes, force the DbZoneType2 cache to be updated so that the changes are immediately available to other consumers.

                        if (dbZoneType2sToPersist.Any())
                        {
                            await dbZoneType2ObjectCache.UpdateAsync(true);
                        }
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
            if (zoneTypeGeotabObjectCacher.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(zoneTypeGeotabObjectCacher.InitializeAsync(cancellationTokenSource, adapterConfiguration.ZoneTypeCacheIntervalDailyReferenceStartTimeUTC, adapterConfiguration.ZoneTypeCacheUpdateIntervalMinutes, adapterConfiguration.ZoneTypeCacheRefreshIntervalMinutes, myGeotabAPIHelper.GetFeedResultLimitDefault, false));
            }
            else
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(zoneTypeGeotabObjectCacher.UpdateGeotabObjectCacheAsync(cancellationTokenSource));
            }

            // Update the in-memory cache of database objects that correspond with Geotab objects.
            if (dbZoneType2ObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbZoneType2ObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }

            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);
        }

        /// <summary>
        /// Starts the current <see cref="ZoneTypeProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.ZoneTypeProcessor2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.ZoneTypeProcessor2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
                stateMachine.RegisterService(nameof(ZoneTypeProcessor2), adapterConfiguration.EnableZoneTypeCache);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableZoneTypeCache == true)
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
        /// Stops the current <see cref="ZoneTypeProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(ZoneTypeProcessor2), false);
            }

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
