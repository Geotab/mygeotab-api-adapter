#nullable enable
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;
using NLog;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Caches
{
    /// <summary>
    /// A generic class that maintains an in-memory cache of <see cref="IGeotabGUIDCacheableDbEntity"/>s.
    /// </summary>
    /// <typeparam name="T">The type of entity being cached.</typeparam>
    public class GenericGeotabGUIDCacheableDbObjectCache<T> : IGenericGeotabGUIDCacheableDbObjectCache<T> where T : class, IGeotabGUIDCacheableDbEntity
    {
        static string CurrentClassName { get => $"{nameof(GenericGeotabGUIDCacheableDbObjectCache<T>)}<{typeof(T).Name}>"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        protected readonly ConcurrentDictionary<long, T> dbObjectFromDbIdCache = new();
        readonly ConcurrentDictionary<string, long> dbIdFromGeotabGuidCache = new();
        readonly ConcurrentDictionary<string, string> geotabGuidFromGeotabIdCache = new();

        int cacheUpdateIntervalMinutes = 5;
        Databases database;
        IBaseRepository<T> dbEntityRepo;
        readonly DateTime defaultDateTime = DateTime.ParseExact("1912/06/23", "yyyy/MM/dd", CultureInfo.InvariantCulture);
        readonly SemaphoreSlim initializationLock = new(1, 1);
        bool isInitialized;
        bool isUpdating = false;
        DateTime lastUpdated;
        readonly SemaphoreSlim updateLock = new(1, 1);

        readonly IDateTimeHelper dateTimeHelper;
        readonly OptimizerDatabaseUnitOfWorkContext context;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public int CacheUpdateIntervalMinutes
        {
            get => cacheUpdateIntervalMinutes;
            set => cacheUpdateIntervalMinutes = value;
        }

        /// <inheritdoc/>
        public DateTime DefaultDateTime { get => defaultDateTime; }

        /// <inheritdoc/>
        public bool IsInitialized
        {
            get
            {
                // The dbObjectFromDbIdCache may be empty if the source was not yet populated when the cache was initialized (e.g. if the API Adapter and Data Optimizer were started at the same time, or in some situations based on combinations of appsettings.json values). If this is the case, we want to re-initialize to capture the first batch of data that may have subsequently come-in.
                if (dbObjectFromDbIdCache.IsEmpty && isUpdating == false)
                {
                    isInitialized = false;
                }
                return isInitialized;
            }
        }

        /// <inheritdoc/>
        public bool IsUpdating { get => isUpdating; }

        /// <inheritdoc/>
        public DateTime LastUpdated { get => lastUpdated; }

        /// <inheritdoc/>
        public bool Any()
        {
            return !dbObjectFromDbIdCache.IsEmpty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericGeotabGUIDCacheableDbObjectCache{T}"/> class.
        /// </summary>
        public GenericGeotabGUIDCacheableDbObjectCache(IDateTimeHelper dateTimeHelper, OptimizerDatabaseUnitOfWorkContext context)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            lastUpdated = defaultDateTime;
            this.dateTimeHelper = dateTimeHelper;

            this.context = context;
            logger.Debug($"{nameof(OptimizerDatabaseUnitOfWorkContext)} [Id: {context.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(MaxRetries, logger);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// *** UNIT TESTING CONSTRUCTOR *** This constructor is designed for unit testing only. It accepts an additional <see cref="IBaseRepository{T}"/> input, which may be a test class. Under normal operation, a <see cref="BaseRepository{T}"/> is created and used within this class when required. Under normal operation, the other constructor should be used in conjunction with Dependency Injection.
        /// </summary>
        public GenericGeotabGUIDCacheableDbObjectCache(IDateTimeHelper dateTimeHelper, OptimizerDatabaseUnitOfWorkContext context, IBaseRepository<T> testDbEntityRepo)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            lastUpdated = defaultDateTime;
            this.dateTimeHelper = dateTimeHelper;

            this.context = context;
            logger.Debug($"{nameof(OptimizerDatabaseUnitOfWorkContext)} [Id: {context.Id}] associated with {CurrentClassName}.");

            dbEntityRepo = testDbEntityRepo;

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task<T?> GetObjectAsync(long id)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await UpdateAsync();
            await WaitIfUpdatingAsync();

            if (dbObjectFromDbIdCache.TryGetValue(id, out T obj))
            {
                return obj;
            }
            return default;
        }

        /// <inheritdoc/>
        public async Task<T?> GetObjectByGeotabGUIDAsync(string geotabGUID)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var objectId = await GetObjectIdByGeotabGUIDAsync(geotabGUID);
            if (objectId != null)
            {
                var obj = await GetObjectAsync((long)objectId);
                return obj;
            }
            return default;
        }

        /// <inheritdoc/>
        public async Task<string?> GetObjectGeotabGUIDByGeotabIdAsync(string geotabId)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await UpdateAsync();
            await WaitIfUpdatingAsync();

            if (geotabGuidFromGeotabIdCache.TryGetValue(geotabId, out string geotabGUID))
            {
                return geotabGUID;
            }
            return default;
        }

        /// <inheritdoc/>
        public async Task<long?> GetObjectIdByGeotabGUIDAsync(string geotabGUID)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await UpdateAsync();
            await WaitIfUpdatingAsync();

            if (dbIdFromGeotabGuidCache.TryGetValue(geotabGUID, out long id))
            {
                return id;
            }
            return default;
        }

        /// <inheritdoc/>
        public async Task<List<T>> GetObjectsAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await UpdateAsync();
            await WaitIfUpdatingAsync();

            var results = new List<T>();
            var objectEnumerator = dbObjectFromDbIdCache.GetEnumerator();
            objectEnumerator.Reset();
            while (objectEnumerator.MoveNext())
            {
                var currentKVP = objectEnumerator.Current;
                var currentObject = currentKVP.Value;
                results.Add(currentObject);
            }
            return results;
        }

        /// <inheritdoc/>
        public async Task<List<T>> GetObjectsAsync(DateTime changedSince)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await UpdateAsync();
            await WaitIfUpdatingAsync();

            var results = new List<T>();
            var objectEnumerator = dbObjectFromDbIdCache.GetEnumerator();
            objectEnumerator.Reset();
            while (objectEnumerator.MoveNext())
            {
                var currentKVP = objectEnumerator.Current;
                var currentObject = currentKVP.Value;
                if (currentObject.LastUpsertedUtc >= changedSince)
                {
                    results.Add(currentObject);
                }
            }
            return results;
        }

        /// <inheritdoc/>
        public async Task<List<T>> GetObjectsAsync(string geotabGUID, string geotabId)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await UpdateAsync();
            await WaitIfUpdatingAsync();

            var results = new List<T>();
            var objectEnumerator = dbObjectFromDbIdCache.GetEnumerator();
            objectEnumerator.Reset();
            while (objectEnumerator.MoveNext())
            {
                var currentKVP = objectEnumerator.Current;
                var currentObject = currentKVP.Value;
                if (currentObject.GeotabGUID == geotabGUID && currentObject.GeotabId == geotabId)
                {
                    results.Add(currentObject);
                }
            }
            return results;
        }

        /// <inheritdoc/>
        public async Task InitializeAsync(Databases database)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await initializationLock.WaitAsync();
            try
            {
                if (isInitialized == false)
                {
                    this.database = database;
                    isInitialized = true;
                    await UpdateAsync(true);
                }
                else
                {
                    logger.Debug($"The current {CurrentClassName} has already been initialized.");
                }
            }
            finally
            {
                initializationLock.Release();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(bool ForceUpdate = false)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await updateLock.WaitAsync();
            try
            {
                // Abort if the configured execution interval has not elapsed since the last time this method was executed AND ForceUpdate is false.
                if (!dateTimeHelper.TimeIntervalHasElapsed(lastUpdated, DateTimeIntervalType.Minutes, cacheUpdateIntervalMinutes) && !ForceUpdate)
                {
                    return;
                }

                ValidateInitialized();

                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    if (dbEntityRepo == null)
                    {
                        dbEntityRepo = new BaseRepository<T>(context);
                    }

                    isUpdating = true;
                    await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                    {
                        using (var uow = context.CreateUnitOfWork(database))
                        {
                            // Get any dbEntities that have been updated since the last time this GenericGeotabGUIDCacheableDbObjectCache was updated. If this is the first time this method has been called since application startup, all dbEntities will be loaded into the cache.
                            var dbEntities = await dbEntityRepo.GetAllAsync(cancellationTokenSource, null, lastUpdated);

                            foreach (var dbEntity in dbEntities)
                            {
                                // If the dbEntity is already in the dbObjectFromDbIdCache, remove it so that it can be replaced with the new one.
                                if (dbObjectFromDbIdCache.ContainsKey(dbEntity.id))
                                {
                                    if (!dbObjectFromDbIdCache.TryRemove(dbEntity.id, out T retrievedValue))
                                    {
                                        throw new Exception($"Unable to remove {typeof(T).Name} with id \"{dbEntity.id}\" from dbObjectFromDbIdCache.");
                                    }
                                }

                                // Add the dbEntity to the dbObjectFromDbIdCache.
                                if (!dbObjectFromDbIdCache.TryAdd(dbEntity.id, dbEntity))
                                {
                                    throw new Exception($"Unable to add {typeof(T).Name} with id \"{dbEntity.id}\" to dbObjectFromDbIdCache.");
                                }

                                // Add or update the dbIdFromGeotabGuidCache.
                                _ = dbIdFromGeotabGuidCache.AddOrUpdate(dbEntity.GeotabGUID, dbEntity.id,
                                    (geotabGUID, existingEntityId) =>
                                    {
                                        // If this delegate is invoked, then the key already exists. This would be the case when a new KnownId is assigned to a Diagnostic. If this is the case, use the higher of the two Id values since the new one will have been added to the database table later than the original one and we'd logically want to have any new data (e.g. StatusData or FaultData) tied to the newer KnownId going forward.
                                        if (dbEntity.id != existingEntityId)
                                        {
                                            return dbEntity.id;
                                        }
                                        // Nothing to do here, since the Id is the only updatable property.
                                        return existingEntityId;
                                    });

                                // Add or update to geotabGuidFromGeotabIdCache.
                                _ = geotabGuidFromGeotabIdCache.AddOrUpdate(dbEntity.GeotabId, dbEntity.GeotabGUID,
                                    (geotabID, existingEntityGeotabGUID) =>
                                    {
                                        // If this delegate is invoked, then the key already exists. Validate against duplicates to ensure we don't add another item to the geotabGuidFromGeotabIdCache that has the same GeotabId, but a different GeotabGUID.
                                        if (dbEntity.GeotabGUID != existingEntityGeotabGUID)
                                        {
                                            throw new ArgumentException($"A {typeof(T).Name} with GeotabId \"{dbEntity.GeotabId}\" already exists in the geotabGuidFromGeotabIdCache. Duplicates are not allowed. The existing {typeof(T).Name} has a GeotabGUID of \"{existingEntityGeotabGUID}\". The {typeof(T).Name} with the same GeotabId that cannot be added to the geotabGuidFromGeotabIdCache has a GeotabGUID of \"{dbEntity.GeotabGUID}\".");
                                        }
                                        // Nothing to do here, since the GeotabGUID is the only updatable property.
                                        return existingEntityGeotabGUID;
                                    });
                            }
                        }
                    }, new Context());
                    isUpdating = false;
                }
                lastUpdated = DateTime.UtcNow;
            }
            finally
            {
                updateLock.Release();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Checks whether the <see cref="InitializeAsync(OptimizerDatabaseUnitOfWorkContext, Databases)"/> has been invoked since the current class instance was created and throws an <see cref="InvalidOperationException"/> if initialization has not been performed.
        /// </summary>
        void ValidateInitialized()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (isInitialized == false)
            {
                throw new InvalidOperationException($"The current {CurrentClassName} has not been initialized. The {nameof(InitializeAsync)} method must be called before other methods can be invoked.");
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
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
