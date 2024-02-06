﻿using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Caches;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.Logging;
using NLog;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A class that helps manage <see cref="DbOServiceTracking"/> information for all <see cref="AdapterService"/>s.
    /// </summary>
    internal class ServiceTracker : IServiceTracker
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        bool cacheIsStale = false;
        bool isUpdating = false;
        readonly SemaphoreSlim cacheReloadLock = new(1, 1);

        readonly IDateTimeHelper dateTimeHelper;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbOServiceTracking> dbOServiceTrackingEntityPersister;
        readonly IGenericGenericDbObjectCache<DbOServiceTracking, AdapterGenericDbObjectCache<DbOServiceTracking>> dbOServiceTrackingObjectCache;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context;

        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <inheritdoc/>
        public bool IsUpdating { get => isUpdating; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceTracker"/> class.
        /// </summary>
        public ServiceTracker(IDateTimeHelper dateTimeHelper, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbOServiceTracking> dbOServiceTrackingEntityPersister, IGenericGenericDbObjectCache<DbOServiceTracking, AdapterGenericDbObjectCache<DbOServiceTracking>> dbOServiceTrackingObjectCache, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.dateTimeHelper = dateTimeHelper;
            this.exceptionHelper = exceptionHelper;
            this.dbOServiceTrackingEntityPersister = dbOServiceTrackingEntityPersister;
            this.dbOServiceTrackingObjectCache = dbOServiceTrackingObjectCache;
            this.context = context;

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            InitializeDbOServiceTrackingListAsync().Wait();

            Id = Guid.NewGuid().ToString();
            logger.Debug($"{nameof(ServiceTracker)} [Id: {Id}] created.");
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task<PrerequisiteServiceOperationCheckResult> CheckOperationOfPrerequisiteServicesAsync(List<AdapterService> prerequisiteServices, bool includeCheckForWhetherServicesHaveProcessedAnyData = false)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var allPrerequisiteServicesRunning = true;
            var servicesNeverRun = new List<AdapterService>();
            var servicesNotRunning = new List<AdapterService>();
            var servicesWithNoDataProcessed = new List<AdapterService>();

            foreach (var prerequisiteService in prerequisiteServices)
            {
                var serviceHasBeenRun = await ServiceHasBeenRunAsync(prerequisiteService);
                if (serviceHasBeenRun == false)
                {
                    servicesNeverRun.Add(prerequisiteService);
                }

                var serviceIsRunning = await ServiceIsRunningAsync(prerequisiteService);
                if (serviceIsRunning == false)
                {
                    allPrerequisiteServicesRunning = false;
                    servicesNotRunning.Add(prerequisiteService);
                }

                if (includeCheckForWhetherServicesHaveProcessedAnyData == true)
                {
                    var serviceHasProcessedData = await ServiceHasProcessedDataAsync(prerequisiteService);
                    if (serviceHasProcessedData == false)
                    {
                        servicesWithNoDataProcessed.Add(prerequisiteService);
                    }
                }
            }

            var result = new PrerequisiteServiceOperationCheckResult(allPrerequisiteServicesRunning, servicesNeverRun, servicesNotRunning, servicesWithNoDataProcessed);
            return result;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetBinaryDataServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.BinaryDataProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetChargeEventServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.ChargeEventProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<List<DbOServiceTracking>> GetDbOServiceTrackingListAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var result = await dbOServiceTrackingObjectCache.GetObjectsAsync();
            return result;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetDbOServiceTrackingRecordAsync(AdapterService adapterService)
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(adapterService.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetDebugDataServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.DebugDataProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetDeviceServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.DeviceProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetDeviceStatusInfoServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.DeviceStatusInfoProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetDiagnosticServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.DiagnosticProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetDriverChangeServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.DriverChangeProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetDutyStatusAvailabilityServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.DutyStatusAvailabilityProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetDVIRLogServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.DVIRLogProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetExceptionEventServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.ExceptionEventProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetFaultDataServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.FaultDataProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetLogRecordServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.LogRecordProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetRuleServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.RuleProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetStatusDataServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.StatusDataProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetTripServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.TripProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetUserServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.UserProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetZoneServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.ZoneProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOServiceTracking> GetZoneTypeServiceInfoAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(AdapterService.ZoneTypeProcessor.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task InitializeDbOServiceTrackingListAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await dbOServiceTrackingObjectCache.InitializeAsync(Databases.AdapterDatabase);

            // Make sure that a OServiceTracking record exists for each of the services. For any that don't have a record (e.g. when the application is run for the first time), create a new record.
            var dbOServiceTrackingsToPersist = new List<DbOServiceTracking>();
            var services = Enum.GetValues(typeof(AdapterService));
            foreach (var service in services)
            {
                var existingService = await dbOServiceTrackingObjectCache.GetObjectAsync(service.ToString());
                if (existingService == null)
                {
                    DbOServiceTracking newDbOServiceTracking = new()
                    {
                        DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                        ServiceId = service.ToString(),
                        RecordLastChangedUtc = DateTime.UtcNow
                    };
                    dbOServiceTrackingsToPersist.Add(newDbOServiceTracking);
                }
            }

            if (dbOServiceTrackingsToPersist.Any())
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                    {
                        using (var uow = context.CreateUnitOfWork(Databases.AdapterDatabase))
                        {
                            try
                            {
                                await PersistDbOServiceTrackingRecordsToDatabaseAsync(context, dbOServiceTrackingsToPersist);

                                // Commit transactions:
                                await uow.CommitAsync();
                            }
                            catch (Exception ex)
                            {
                                exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                                await uow.RollBackAsync();
                                throw;
                            }
                        }
                    }, new Context());
                }
                await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Persists the <paramref name="dbOServiceTrackingsToPersist"/> to database.
        /// </summary>
        /// <param name="context">The <see cref="AdapterDatabaseUnitOfWorkContext"/> to use.</param>
        /// <param name="dbOServiceTrackingsToPersist">The list of <see cref="DbOServiceTracking"/> entities to persist to database.</param>
        /// <returns></returns>
        async Task PersistDbOServiceTrackingRecordsToDatabaseAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context, List<DbOServiceTracking> dbOServiceTrackingsToPersist)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (dbOServiceTrackingsToPersist.Any())
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    await WaitIfUpdatingAsync();
                    isUpdating = true;
                    await dbOServiceTrackingEntityPersister.PersistEntitiesToDatabaseAsync(context, dbOServiceTrackingsToPersist, cancellationTokenSource, Logging.LogLevel.Debug);
                    isUpdating = false;
                }
                cacheIsStale = true;
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Reloads the <see cref="dbOServiceTrackingObjectCache"/> if it has been flagged as stale.
        /// </summary>
        /// <returns></returns>
        async Task ReloadDbOServiceTrackingObjectCacheIfStaleAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Abort if cache is current.
            if (cacheIsStale == false)
            {
                return;
            }

            await cacheReloadLock.WaitAsync();
            try
            {
                if (cacheIsStale == true)
                {
                    await dbOServiceTrackingObjectCache.UpdateAsync(true);
                    cacheIsStale = false;
                }
            }
            finally
            {
                cacheReloadLock.Release();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task<bool> ServiceHasBeenRunAsync(AdapterService adapterService)
        {
            var dbOServiceTracking = await GetDbOServiceTrackingRecordAsync(adapterService);
            return dbOServiceTracking.EntitiesHaveBeenProcessed;
        }

        /// <inheritdoc/>
        public async Task<bool> ServiceHasProcessedDataAsync(AdapterService adapterService)
        {
            var dbOServiceTracking = await GetDbOServiceTrackingRecordAsync(adapterService);
            if (dbOServiceTracking.LastProcessedFeedVersion != null)
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public async Task<bool> ServiceIsRunningAsync(AdapterService adapterService)
        {
            const int CutoffDays = 2;
            var dbOServiceTracking = await GetDbOServiceTrackingRecordAsync(adapterService);
            var processorLastProcessedEntitiesUtc = (DateTime)dbOServiceTracking.EntitiesLastProcessedUtc;
            var processorIsRunning = !dateTimeHelper.TimeIntervalHasElapsed(processorLastProcessedEntitiesUtc, DateTimeIntervalType.Days, CutoffDays);
            return processorIsRunning;
        }

#nullable enable
        /// <inheritdoc/>
        public async Task UpdateDbOServiceTrackingRecordAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context, AdapterService adapterService, DateTime? entitiesLastProcessedUtc = null, long? lastProcessedFeedVersion = null)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");
            var dbOServiceTrackingToUpdate = await GetDbOServiceTrackingRecordAsync(adapterService);

            if (entitiesLastProcessedUtc != null)
            {
                dbOServiceTrackingToUpdate.EntitiesLastProcessedUtc = entitiesLastProcessedUtc;
            }

            if (lastProcessedFeedVersion != null)
            {
                dbOServiceTrackingToUpdate.LastProcessedFeedVersion = lastProcessedFeedVersion;
            }

            dbOServiceTrackingToUpdate.RecordLastChangedUtc = DateTime.UtcNow;
            dbOServiceTrackingToUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

            var dbOServiceTrackingsToUpdate = new List<DbOServiceTracking>
            {
                dbOServiceTrackingToUpdate
            };
            await PersistDbOServiceTrackingRecordsToDatabaseAsync(context, dbOServiceTrackingsToUpdate);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
#nullable disable

#nullable enable
        /// <inheritdoc/>
        public async Task UpdateDbOServiceTrackingRecordAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context, AdapterService adapterService, string? adapterVersion = null, string? adapterMachineName = null)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");
            var dbOServiceTrackingToUpdate = await GetDbOServiceTrackingRecordAsync(adapterService);

            if (adapterVersion != null)
            {
                dbOServiceTrackingToUpdate.AdapterVersion = adapterVersion;
            }

            if (adapterMachineName != null)
            {
                dbOServiceTrackingToUpdate.AdapterMachineName = adapterMachineName;
            }

            dbOServiceTrackingToUpdate.RecordLastChangedUtc = DateTime.UtcNow;
            dbOServiceTrackingToUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

            var dbOServiceTrackingsToUpdate = new List<DbOServiceTracking>
            {
                dbOServiceTrackingToUpdate
            };
            await PersistDbOServiceTrackingRecordsToDatabaseAsync(context, dbOServiceTrackingsToUpdate);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
#nullable disable

        /// <inheritdoc/>
        public async Task WaitIfUpdatingAsync()
        {
            while (isUpdating)
            {
                await Task.Delay(25);
            }
        }
    }
}
