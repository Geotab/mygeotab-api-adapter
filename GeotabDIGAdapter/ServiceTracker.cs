using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Caches;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.Logging;
using NLog;
using Polly;
using Polly.Retry;

namespace GeotabDIGAdapter
{
    /// <summary>
    /// A class that helps manage <typeparamref name="T"/> information for all <see cref="DIGAdapterService"/>s.
    /// </summary>
    public class ServiceTracker<T> : IServiceTracker<T> where T : class, IDbOServiceTracking, new()
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        bool cacheIsStale = false;
        bool isUpdating = false;
        readonly SemaphoreSlim cacheReloadLock = new(1, 1);

        readonly IDateTimeHelper dateTimeHelper;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<T> dbOServiceTrackingEntityPersister;
        readonly IGenericGenericDbObjectCache<T, AdapterGenericDbObjectCache<T>> dbOServiceTrackingObjectCache;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context;

        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <inheritdoc/>
        public bool IsUpdating { get => isUpdating; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceTracker{T}"/> class.
        /// </summary>
        public ServiceTracker(
            IDateTimeHelper dateTimeHelper,
            IExceptionHelper exceptionHelper,
            IGenericEntityPersister<T> dbOServiceTrackingEntityPersister,
            IGenericGenericDbObjectCache<T, AdapterGenericDbObjectCache<T>> dbOServiceTrackingObjectCache,
            IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context)
        {
            this.dateTimeHelper = dateTimeHelper;
            this.exceptionHelper = exceptionHelper;
            this.dbOServiceTrackingEntityPersister = dbOServiceTrackingEntityPersister;
            this.dbOServiceTrackingObjectCache = dbOServiceTrackingObjectCache;
            this.context = context;

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            InitializeDbOServiceTrackingListAsync().Wait();

            Id = Guid.NewGuid().ToString();
            logger.Debug($"{nameof(ServiceTracker<T>)} [Id: {Id}] created.");
        }

        /// <inheritdoc/>
        public async Task<PrerequisiteServiceOperationCheckResult> CheckOperationOfPrerequisiteServicesAsync(List<DIGAdapterService> prerequisiteServices, bool includeCheckForWhetherServicesHaveProcessedAnyData = false)
        {
            var allPrerequisiteServicesRunning = true;
            var servicesNeverRun = new List<DIGAdapterService>();
            var servicesNotRunning = new List<DIGAdapterService>();
            var servicesWithNoDataProcessed = new List<DIGAdapterService>();

            foreach (var prerequisiteService in prerequisiteServices)
            {
                var serviceHasBeenRun = await ServiceHasBeenRunAsync(prerequisiteService);
                if (serviceHasBeenRun == false)
                {
                    allPrerequisiteServicesRunning = false;
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

            var result = new PrerequisiteServiceOperationCheckResult
            {
                AllPrerequisiteServicesRunning = allPrerequisiteServicesRunning,
                ServicesNeverRun = servicesNeverRun,
                ServicesNotRunning = servicesNotRunning,
                ServicesWithNoDataProcessed = servicesWithNoDataProcessed,
                RecommendedDelayBeforeNextCheck = TimeSpan.FromSeconds(10),
                ServicesNeverRunStatement = servicesNeverRun.Count > 0 ? $"Services never run: {string.Join(", ", servicesNeverRun)}" : string.Empty,
                ServicesNotRunningStatement = servicesNotRunning.Count > 0 ? $"Services not running: {string.Join(", ", servicesNotRunning)}" : string.Empty,
                ServicesWithNoDataProcessedStatement = servicesWithNoDataProcessed.Count > 0 ? $"Services with no data processed: {string.Join(", ", servicesWithNoDataProcessed)}" : string.Empty
            };
            return result;
        }

        /// <inheritdoc/>
        public async Task<List<T>> GetDbOServiceTrackingListAsync()
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var result = await dbOServiceTrackingObjectCache.GetObjectsAsync();
            return result;
        }

        /// <inheritdoc/>
        public async Task<T> GetDbOServiceTrackingRecordAsync(DIGAdapterService digAdapterService)
        {
            await ReloadDbOServiceTrackingObjectCacheIfStaleAsync();
            var dbOServiceTracking = await dbOServiceTrackingObjectCache.GetObjectAsync(digAdapterService.ToString());
            return dbOServiceTracking;
        }

        /// <inheritdoc/>
        public async Task InitializeDbOServiceTrackingListAsync()
        {
            await dbOServiceTrackingObjectCache.InitializeAsync(Databases.AdapterDatabase);

            // Make sure that a OServiceTracking record exists for each of the services. For any that don't have a record (e.g. when the application is run for the first time), create a new record.
            var dbOServiceTrackingsToPersist = new List<T>();
            var services = Enum.GetValues(typeof(DIGAdapterService));
            foreach (var service in services)
            {
                var existingService = await dbOServiceTrackingObjectCache.GetObjectAsync(service.ToString());
                if (existingService == null)
                {
                    T newDbOServiceTracking = new()
                    {
                        DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                        ServiceId = service.ToString()!,
                        RecordLastChangedUtc = DateTime.UtcNow
                    };
                    dbOServiceTrackingsToPersist.Add(newDbOServiceTracking);
                }
            }

            if (dbOServiceTrackingsToPersist.Count != 0)
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
        }

        /// <summary>
        /// Persists the <paramref name="dbOServiceTrackingsToPersist"/> to database.
        /// </summary>
        /// <param name="context">The <see cref="AdapterDatabaseUnitOfWorkContext"/> to use.</param>
        /// <param name="dbOServiceTrackingsToPersist">The list of <typeparamref name="T"/> entities to persist to database.</param>
        /// <returns></returns>
        async Task PersistDbOServiceTrackingRecordsToDatabaseAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context, List<T> dbOServiceTrackingsToPersist)
        {
            if (dbOServiceTrackingsToPersist.Count != 0)
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    await WaitIfUpdatingAsync();
                    isUpdating = true;
                    await dbOServiceTrackingEntityPersister.PersistEntitiesToDatabaseAsync(context, dbOServiceTrackingsToPersist, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    isUpdating = false;
                }
                cacheIsStale = true;
            }
        }

        /// <summary>
        /// Reloads the <see cref="dbOServiceTrackingObjectCache"/> if it has been flagged as stale.
        /// </summary>
        /// <returns></returns>
        async Task ReloadDbOServiceTrackingObjectCacheIfStaleAsync()
        {
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
        }

        /// <inheritdoc/>
        public async Task<bool> ServiceHasBeenRunAsync(DIGAdapterService digAdapterService)
        {
            var dbOServiceTracking = await GetDbOServiceTrackingRecordAsync(digAdapterService);
            return dbOServiceTracking.EntitiesHaveBeenProcessed;
        }

        /// <inheritdoc/>
        public async Task<bool> ServiceHasProcessedDataAsync(DIGAdapterService digAdapterService)
        {
            var dbOServiceTracking = await GetDbOServiceTrackingRecordAsync(digAdapterService);
            if (dbOServiceTracking.LastProcessedFeedVersion != null)
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public async Task<bool> ServiceHasProcessedDataSinceAsync(DIGAdapterService digAdapterService, DateTime sinceDateTime)
        {
            var dbOServiceTracking = await GetDbOServiceTrackingRecordAsync(digAdapterService);
            var entitiesLastProcessedUtc = dbOServiceTracking.EntitiesLastProcessedUtc;

            if (entitiesLastProcessedUtc.HasValue && entitiesLastProcessedUtc > sinceDateTime)
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public async Task<bool> ServiceIsRunningAsync(DIGAdapterService digAdapterService)
        {
            const int CutoffDays = 2;
            var dbOServiceTracking = await GetDbOServiceTrackingRecordAsync(digAdapterService);
            var processorLastProcessedEntitiesUtc = (DateTime)dbOServiceTracking.EntitiesLastProcessedUtc!;
            var processorIsRunning = !dateTimeHelper.TimeIntervalHasElapsed(processorLastProcessedEntitiesUtc, DateTimeIntervalType.Days, CutoffDays);
            return processorIsRunning;
        }

#nullable enable
        /// <inheritdoc/>
        public async Task UpdateDbOServiceTrackingRecordAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context, DIGAdapterService digAdapterService, DateTime? entitiesLastProcessedUtc = null, long? lastProcessedFeedVersion = null)
        {
            var dbOServiceTrackingToUpdate = await GetDbOServiceTrackingRecordAsync(digAdapterService);

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

            var dbOServiceTrackingsToUpdate = new List<T>
            {
                dbOServiceTrackingToUpdate
            };
            await PersistDbOServiceTrackingRecordsToDatabaseAsync(context, dbOServiceTrackingsToUpdate);
        }
#nullable disable

#nullable enable
        /// <inheritdoc/>
        public async Task UpdateDbOServiceTrackingRecordAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context, DIGAdapterService digAdapterService, string? adapterVersion = null, string? adapterMachineName = null)
        {
            var dbOServiceTrackingToUpdate = await GetDbOServiceTrackingRecordAsync(digAdapterService);

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

            var dbOServiceTrackingsToUpdate = new List<T>
            {
                dbOServiceTrackingToUpdate
            };
            await PersistDbOServiceTrackingRecordsToDatabaseAsync(context, dbOServiceTrackingsToUpdate);
        }
#nullable disable

        /// <inheritdoc/>
        public async Task UpdateDbOServiceTrackingRecordWithMetricsAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context, DIGAdapterService digAdapterService, DateTime entitiesLastProcessedUtc, int lastBatchSize, int successCount, int failureCount)
        {
            var dbOServiceTrackingToUpdate = await GetDbOServiceTrackingRecordAsync(digAdapterService);

            dbOServiceTrackingToUpdate.EntitiesLastProcessedUtc = entitiesLastProcessedUtc;

            // Cast to DbGdaOServiceTracking to access DIG-specific metrics properties
            if (dbOServiceTrackingToUpdate is DbGdaOServiceTracking gdaTracking)
            {
                gdaTracking.LastBatchSize = lastBatchSize;
                gdaTracking.SuccessCount = (gdaTracking.SuccessCount ?? 0) + successCount;
                gdaTracking.FailureCount = (gdaTracking.FailureCount ?? 0) + failureCount;
            }

            dbOServiceTrackingToUpdate.RecordLastChangedUtc = DateTime.UtcNow;
            dbOServiceTrackingToUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

            var dbOServiceTrackingsToUpdate = new List<T>
            {
                dbOServiceTrackingToUpdate
            };
            await PersistDbOServiceTrackingRecordsToDatabaseAsync(context, dbOServiceTrackingsToUpdate);
        }

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
