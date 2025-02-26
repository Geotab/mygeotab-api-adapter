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
    /// A <see cref="BackgroundService"/> that extracts <see cref="Zone"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class ZoneProcessor2 : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<ZoneProcessor2> awaiter;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericGenericDbObjectCache<DbZone2, AdapterGenericDbObjectCache<DbZone2>> dbZone2ObjectCache;
        readonly IGenericEntityPersister<DbZone2> dbZone2EntityPersister;
        readonly IGenericGeotabObjectCacher<Zone> zoneGeotabObjectCacher;
        readonly IGeotabIdConverter geotabIdConverter;
        readonly IGeotabZoneDbZone2ObjectMapper geotabZoneDbZone2ObjectMapper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoneProcessor2"/> class.
        /// </summary>
        public ZoneProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<ZoneProcessor2> awaiter, IExceptionHelper exceptionHelper, IGenericGenericDbObjectCache<DbZone2, AdapterGenericDbObjectCache<DbZone2>> dbZone2ObjectCache, IGenericEntityPersister<DbZone2> dbZone2EntityPersister, IGeotabZoneDbZone2ObjectMapper geotabZoneDbZone2ObjectMapper, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericGeotabObjectCacher<Zone> zoneGeotabObjectCacher, IGeotabIdConverter geotabIdConverter, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.exceptionHelper = exceptionHelper;
            this.dbZone2ObjectCache = dbZone2ObjectCache;
            this.dbZone2EntityPersister = dbZone2EntityPersister;
            this.geotabZoneDbZone2ObjectMapper = geotabZoneDbZone2ObjectMapper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.zoneGeotabObjectCacher = zoneGeotabObjectCacher;
            this.geotabIdConverter = geotabIdConverter;

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
            var delayTimeSpan = TimeSpan.FromMinutes(adapterConfiguration.ZoneCacheUpdateIntervalMinutes);

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

                        var dbZone2sToPersist = new List<DbZone2>();
                        var newDbZone2sToPersistDictionary = new Dictionary<long, Common.DatabaseWriteOperationType>();
                        // Only propagate the cache to database if the cache has been updated since the last time it was propagated to database.
                        if (zoneGeotabObjectCacher.LastUpdatedTimeUTC > zoneGeotabObjectCacher.LastPropagatedToDatabaseTimeUtc)
                        {
                            DateTime recordChangedTimestampUtc = DateTime.UtcNow;

                            // Find any zones that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                            var dbZone2s = await dbZone2ObjectCache.GetObjectsAsync();
                            foreach (var dbZone2 in dbZone2s)
                            {
                                if (dbZone2.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                                {
                                    bool zoneExistsInCache = zoneGeotabObjectCacher.GeotabObjectCache.ContainsKey(Id.Create(dbZone2.GeotabId));
                                    if (!zoneExistsInCache)
                                    {
                                        logger.Debug($"Zone '{dbZone2.GeotabId}' no longer exists in MyGeotab and is being marked as deleted.");
                                        dbZone2.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                        dbZone2.RecordLastChangedUtc = recordChangedTimestampUtc;
                                        dbZone2.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                        dbZone2sToPersist.Add(dbZone2);
                                    }
                                }
                            }

                            // Iterate through the in-memory cache of Geotab Zone objects.
                            foreach (var zone in zoneGeotabObjectCacher.GeotabObjectCache.Values)
                            {
                                // Try to find the existing database record for the cached zone.
                                long zoneId = geotabIdConverter.ToLong(zone.Id);
                                var dbZone2 = await dbZone2ObjectCache.GetObjectAsync(zoneId);
                                if (dbZone2 != null)
                                {
                                    // The zone has already been added to the database.
                                    if (geotabZoneDbZone2ObjectMapper.EntityRequiresUpdate(dbZone2, zone))
                                    {
                                        DbZone2 updatedDbZone2 = geotabZoneDbZone2ObjectMapper.UpdateEntity(dbZone2, zone);
                                        dbZone2sToPersist.Add(updatedDbZone2);
                                    }
                                }
                                else
                                {
                                    // The zone has not yet been added to the database. Create a DbZone2, set its properties and add it to the cache.
                                    var newDbZone2 = geotabZoneDbZone2ObjectMapper.CreateEntity(zone);

                                    // There may be multiple records for the same entity in the batch of entities retrieved from Geotab. If there are, make sure that duplicates are set to be updated instead of inserted.
                                    if (newDbZone2sToPersistDictionary.ContainsKey(newDbZone2.id))
                                    {
                                        newDbZone2.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                    }

                                    dbZone2sToPersist.Add(newDbZone2);
                                    if (newDbZone2.DatabaseWriteOperationType == Common.DatabaseWriteOperationType.Insert)
                                    {
                                        newDbZone2sToPersistDictionary.Add(newDbZone2.id, newDbZone2.DatabaseWriteOperationType);
                                    }
                                }
                            }

                            stoppingToken.ThrowIfCancellationRequested();
                        }
                        else
                        {
                            logger.Debug($"Zone cache in database is up-to-date.");
                        }

                        // Persist changes to database.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    // DbZone2:
                                    await dbZone2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbZone2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    // DbOServiceTracking:
                                    await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.ZoneProcessor2, DateTime.UtcNow);

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

                        // If there were any changes, force the DbZone2 cache to be updated so that the changes are immediately available to other consumers.
                        if (dbZone2sToPersist.Any())
                        {
                            await dbZone2ObjectCache.UpdateAsync(true);
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
            if (zoneGeotabObjectCacher.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(zoneGeotabObjectCacher.InitializeAsync(cancellationTokenSource, adapterConfiguration.ZoneCacheIntervalDailyReferenceStartTimeUTC, adapterConfiguration.ZoneCacheUpdateIntervalMinutes, adapterConfiguration.ZoneCacheRefreshIntervalMinutes, myGeotabAPIHelper.GetFeedResultLimitZone, true));
            }
            else
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(zoneGeotabObjectCacher.UpdateGeotabObjectCacheAsync(cancellationTokenSource));
            }

            // Update the in-memory cache of database objects that correspond with Geotab objects.
            if (dbZone2ObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbZone2ObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }

            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);
        }

        /// <summary>
        /// Starts the current <see cref="ZoneProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.ZoneProcessor2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.ZoneProcessor2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
                stateMachine.RegisterService(nameof(ZoneProcessor2), adapterConfiguration.EnableZoneCache);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableZoneCache == true)
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
        /// Stops the current <see cref="ZoneProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(ZoneProcessor2), false);
            }

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
