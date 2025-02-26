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
    /// A <see cref="BackgroundService"/> that extracts <see cref="Device"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class DeviceProcessor2 : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<DeviceProcessor2> awaiter;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericGenericDbObjectCache<DbDevice2, AdapterGenericDbObjectCache<DbDevice2>> dbDevice2ObjectCache;
        readonly IGenericEntityPersister<DbDevice2> dbDevice2EntityPersister;
        readonly IGenericGeotabObjectCacher<Device> deviceGeotabObjectCacher;
        readonly IGeotabDeviceDbDevice2ObjectMapper geotabDeviceDbDevice2ObjectMapper;
        readonly IGeotabIdConverter geotabIdConverter;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

         /// <summary>
        /// Initializes a new instance of the <see cref="DeviceProcessor2"/> class.
        /// </summary>
        public DeviceProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<DeviceProcessor2> awaiter, IExceptionHelper exceptionHelper, IGenericGenericDbObjectCache<DbDevice2, AdapterGenericDbObjectCache<DbDevice2>> dbDevice2ObjectCache, IGenericEntityPersister<DbDevice2> dbDevice2EntityPersister, IGeotabDeviceDbDevice2ObjectMapper geotabDeviceDbDevice2ObjectMapper, IGeotabIdConverter geotabIdConverter, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericGeotabObjectCacher<Device> deviceGeotabObjectCacher, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.exceptionHelper = exceptionHelper;
            this.dbDevice2ObjectCache = dbDevice2ObjectCache;
            this.dbDevice2EntityPersister = dbDevice2EntityPersister;
            this.geotabDeviceDbDevice2ObjectMapper = geotabDeviceDbDevice2ObjectMapper;
            this.geotabIdConverter = geotabIdConverter;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.deviceGeotabObjectCacher = deviceGeotabObjectCacher;

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
            var delayTimeSpan = TimeSpan.FromMinutes(adapterConfiguration.DeviceCacheUpdateIntervalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait if necessary.
                var prerequisiteServices = new List<AdapterService> {};
                await awaiter.WaitForPrerequisiteServicesIfNeededAsync(prerequisiteServices, stoppingToken);
                await awaiter.WaitForDatabaseMaintenanceCompletionIfNeededAsync(stoppingToken);
                await awaiter.WaitForConnectivityRestorationIfNeededAsync(stoppingToken);

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        await InitializeOrUpdateCachesAsync(cancellationTokenSource);

                        var dbDevice2sToPersist = new List<DbDevice2>();
                        var newDbDevice2sToPersistDictionary = new Dictionary<long, Common.DatabaseWriteOperationType>();
                        // Only propagate the cache to database if the cache has been updated since the last time it was propagated to database.
                        if (deviceGeotabObjectCacher.LastUpdatedTimeUTC > deviceGeotabObjectCacher.LastPropagatedToDatabaseTimeUtc)
                        {
                            DateTime recordChangedTimestampUtc = DateTime.UtcNow;

                            // Find any devices that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                            var dbDevice2s = await dbDevice2ObjectCache.GetObjectsAsync();
                            foreach (var dbDevice2 in dbDevice2s)
                            {
                                if (dbDevice2.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                                {
                                    bool deviceExistsInCache = deviceGeotabObjectCacher.GeotabObjectCache.ContainsKey(Id.Create(dbDevice2.GeotabId));
                                    if (!deviceExistsInCache)
                                    {
                                        logger.Debug($"Device '{dbDevice2.GeotabId}' no longer exists in MyGeotab and is being marked as deleted.");
                                        dbDevice2.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                        dbDevice2.RecordLastChangedUtc = recordChangedTimestampUtc;
                                        dbDevice2.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                        dbDevice2sToPersist.Add(dbDevice2);
                                    }
                                }
                            }

                            // Iterate through the in-memory cache of Geotab Device objects.
                            foreach (var device in deviceGeotabObjectCacher.GeotabObjectCache.Values)
                            {
                                // Try to find the existing database record for the cached device.
                                long deviceId = geotabIdConverter.ToLong(device.Id);
                                var dbDevice2 = await dbDevice2ObjectCache.GetObjectAsync(deviceId);
                                if (dbDevice2 != null)
                                {
                                    // The device has already been added to the database.
                                    if (geotabDeviceDbDevice2ObjectMapper.EntityRequiresUpdate(dbDevice2, device))
                                    {
                                        DbDevice2 updatedDbDevice2 = geotabDeviceDbDevice2ObjectMapper.UpdateEntity(dbDevice2, device);
                                        dbDevice2sToPersist.Add(updatedDbDevice2);
                                    }
                                }
                                else
                                {
                                    // The device has not yet been added to the database. Create a DbDevice2, set its properties and add it to the cache.
                                    var newDbDevice2 = geotabDeviceDbDevice2ObjectMapper.CreateEntity(device);

                                    // There may be multiple records for the same entity in the batch of entities retrieved from Geotab. If there are, make sure that duplicates are set to be updated instead of inserted.
                                    if (newDbDevice2sToPersistDictionary.ContainsKey(newDbDevice2.id))
                                    {
                                        newDbDevice2.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                    }

                                    dbDevice2sToPersist.Add(newDbDevice2);
                                    if (newDbDevice2.DatabaseWriteOperationType == Common.DatabaseWriteOperationType.Insert)
                                    {
                                        newDbDevice2sToPersistDictionary.Add(newDbDevice2.id, newDbDevice2.DatabaseWriteOperationType);
                                    }
                                }
                            }

                            stoppingToken.ThrowIfCancellationRequested();
                        }
                        else
                        {
                            logger.Debug($"Device cache in database is up-to-date.");
                        }

                        // Persist changes to database.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    // DbDevice2:
                                    await dbDevice2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbDevice2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    // DbOServiceTracking:
                                    await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DeviceProcessor2, DateTime.UtcNow);

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

                        // If there were any changes, force the DbDevice2 cache to be updated so that the changes are immediately available to other consumers.
                        if (dbDevice2sToPersist.Any())
                        {
                            await dbDevice2ObjectCache.UpdateAsync(true);
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
            if (deviceGeotabObjectCacher.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(deviceGeotabObjectCacher.InitializeAsync(cancellationTokenSource, adapterConfiguration.DeviceCacheIntervalDailyReferenceStartTimeUTC, adapterConfiguration.DeviceCacheUpdateIntervalMinutes, adapterConfiguration.DeviceCacheRefreshIntervalMinutes, myGeotabAPIHelper.GetFeedResultLimitDevice, true));
            }
            else
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(deviceGeotabObjectCacher.UpdateGeotabObjectCacheAsync(cancellationTokenSource));
            }

            // Update the in-memory cache of database objects that correspond with Geotab objects.
            if (dbDevice2ObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbDevice2ObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }

            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);
        }

        /// <summary>
        /// Starts the current <see cref="DeviceProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.DeviceProcessor2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DeviceProcessor2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
                stateMachine.RegisterService(nameof(DeviceProcessor2), adapterConfiguration.EnableDeviceCache);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableDeviceCache == true)
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
        /// Stops the current <see cref="DeviceProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(DeviceProcessor2), false);
            }

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
