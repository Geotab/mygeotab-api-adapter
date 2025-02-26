using Microsoft.VisualStudio.Threading;
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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Caches
{
    /// <summary>
    /// A generic class that maintains an in-memory cache of <see cref="IIdCacheableDbEntity"/>s.
    /// </summary>
    /// <typeparam name="T">The type of entity being cached.</typeparam>
    public class AdapterGenericDbObjectCache<T> : IGenericDbObjectCache<T> where T : class, IIdCacheableDbEntity
    {
        // Obtain the type parameter type (for logging purposes).
        readonly Type typeParameterType = typeof(T);

        static string CurrentClassName { get => $"{nameof(AdapterGenericDbObjectCache<T>)}<{typeof(T).Name}>"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        protected readonly ConcurrentDictionary<long, T> objectCache = new();
        readonly ConcurrentDictionary<string, long> geotabIdCache = new();
        List<T> objectCacheCurrentValuesList = new();

        int cacheUpdateIntervalMinutes = 1;
        Databases database;
        IBaseRepository<T> dbEntityRepo;
        readonly DateTime defaultDateTime = DateTime.ParseExact("1912/06/23", "yyyy/MM/dd", CultureInfo.InvariantCulture);
        readonly SemaphoreSlim initializationLock = new(1, 1);
        bool isInitialized;
        bool isInternalUpdate;
        bool isUpdating = false;
        DateTime lastUpdated;
        readonly AsyncReaderWriterLock asyncReaderWriterLock = new(null);

        readonly IDateTimeHelper dateTimeHelper;
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public int CacheUpdateIntervalMinutes
        {
            get => cacheUpdateIntervalMinutes;
            set => cacheUpdateIntervalMinutes = value;
        }

        /// <inheritdoc/>
        public int Count { get => objectCache.Count; }

        /// <inheritdoc/>
        public DateTime DefaultDateTime { get => defaultDateTime; }

        /// <inheritdoc/>
        public bool IsInitialized
        {
            get
            {
                // The objectCache may be empty if the source was not yet populated when the cache was initialized (e.g. if the API Adapter and Data Optimizer were started at the same time, or in some situations based on combinations of appsettings.json values). If this is the case, we want to re-initialize to capture the first batch of data that may have subsequently come-in.
                if (objectCache.IsEmpty)
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
            return !objectCache.IsEmpty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterGenericDbObjectCache{T}"/> class.
        /// </summary>
        public AdapterGenericDbObjectCache(IDateTimeHelper dateTimeHelper, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context)
        {
            lastUpdated = defaultDateTime;
            this.dateTimeHelper = dateTimeHelper;

            this.context = context;
            logger.Debug($"{nameof(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext>)} [Id: {context.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);
        }

        /// <summary>
        /// *** UNIT TESTING CONSTRUCTOR *** This constructor is designed for unit testing only. It accepts an additional <see cref="IBaseRepository{T}"/> input, which may be a test class. Under normal operation, a <see cref="BaseRepository{T}"/> is created and used within this class when required. Under normal operation, the other constructor should be used in conjunction with Dependency Injection.
        /// </summary>
        public AdapterGenericDbObjectCache(IDateTimeHelper dateTimeHelper, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context, IBaseRepository<T> testDbEntityRepo)
        {
            lastUpdated = defaultDateTime;
            this.dateTimeHelper = dateTimeHelper;

            this.context = context;
            logger.Debug($"{nameof(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext>)} [Id: {context.Id}] associated with {CurrentClassName}.");

            dbEntityRepo = testDbEntityRepo;
        }

        /// <inheritdoc/>
        public async Task<T?> GetObjectAsync(long id)
        {
            await UpdateAsync();
            using (await asyncReaderWriterLock.ReadLockAsync())
            {
                if (objectCache.TryGetValue(id, out T obj))
                {
                    return obj;
                }
            }
            return default;
        }

        /// <inheritdoc/>
        public async Task<T?> GetObjectAsync(string geotabId)
        {
            var objectId = await GetObjectIdAsync(geotabId);
            if (objectId != null)
            {
                var obj = await GetObjectAsync((long)objectId);
                return obj;
            }
            return default;
        }

        /// <inheritdoc/>
        public async Task<long?> GetObjectIdAsync(string geotabId)
        {
            await UpdateAsync();
            using (await asyncReaderWriterLock.ReadLockAsync())
            {
                if (geotabIdCache.TryGetValue(geotabId, out long id))
                {
                    return id;
                }
            }
            return default;
        }

        /// <inheritdoc/>
        public async Task<List<T>> GetObjectsAsync()
        {
            await UpdateAsync();
            using (await asyncReaderWriterLock.ReadLockAsync())
            {
                return objectCacheCurrentValuesList;
            }
        }

        /// <inheritdoc/>
        public async Task<List<T>> GetObjectsAsync(DateTime changedSince)
        {
            await UpdateAsync();
            using (await asyncReaderWriterLock.ReadLockAsync())
            {
                var results = objectCacheCurrentValuesList.Where(cachedObject => cachedObject.LastUpsertedUtc > changedSince).ToList();
                return results;
            }
        }

        /// <inheritdoc/>
        public async Task InitializeAsync(Databases database)
        {
            await initializationLock.WaitAsync();
            try
            {
                if (isInitialized == false)
                {
                    isInternalUpdate = true;
                    this.database = database;
                    await UpdateAsync(true);
                    isInternalUpdate = false;
                    isInitialized = true;
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
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(bool forceUpdate = false, IList<T> deletedItemsToRemoveFromCache = null)
        {
            using (await asyncReaderWriterLock.WriteLockAsync())
            {
                // Remove any items from the cache, if necessary.
                if (deletedItemsToRemoveFromCache != null && deletedItemsToRemoveFromCache.Any())
                {
                    var removedObjectCacheItemsCount = 0;
                    var removedGeotabIdCacheItemsCount = 0;
                    foreach (var itemToRemove in deletedItemsToRemoveFromCache)
                    {
                        if (objectCache.TryRemove(itemToRemove.id, out T retrievedObject))
                        {
                            removedObjectCacheItemsCount++;
                        }
                        if (geotabIdCache.TryRemove(itemToRemove.GeotabId, out long retrievedGeotabId))
                        {
                            removedGeotabIdCacheItemsCount++;
                        }
                    }
                    logger.Info($"{CurrentClassName} removed {removedObjectCacheItemsCount} item(s) from its object cache and {removedGeotabIdCacheItemsCount} item(s) from its GeotabId cache.");
                }
            }

            // Abort if the configured execution interval has not elapsed since the last time this method was executed AND ForceUpdate is false.
            if (!dateTimeHelper.TimeIntervalHasElapsed(lastUpdated, DateTimeIntervalType.Minutes, cacheUpdateIntervalMinutes) && !forceUpdate)
            {
                return;
            }

            ValidateInitialized();

            // Abort if this UpdateAsync method is already in the process of being executed (e.g. called via one of the Get methods).
            if (isUpdating)
            {
                return;
            }

            isUpdating = true;

            // Perform cache update using database entity repository.
            var objectCacheUpdated = false;
            IEnumerable<T>? dbEntities = null;

            // First query the database for any updated entities.
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                if (dbEntityRepo == null)
                {
                    dbEntityRepo = new BaseRepository<T>(context);
                }

                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                {
                    using (var uow = context.CreateUnitOfWork(database))
                    {
                        // Get any dbEntities that have been updated since the last time this GenericDbObjectCache was updated. If this is the first time this method has been called since application startup, all dbEntities will be loaded into the cache.
                        dbEntities = await dbEntityRepo.GetAllAsync(cancellationTokenSource, null, lastUpdated);
                    }
                }, new Context());
            }

            // If no updated entities were found in the database, don't continue.
            if (dbEntities == null || dbEntities.Any() == false)
            {
                isUpdating = false;
                return;
            }

            // Updated entities were found in the database. Update the objectCache with them.
            using (await asyncReaderWriterLock.WriteLockAsync())
            {
                foreach (var dbEntity in dbEntities)
                {
                    // If the dbEntity is already in the objectCache, remove it so that it can be replaced with the new one.
                    if (objectCache.ContainsKey(dbEntity.id))
                    {
                        if (!objectCache.TryRemove(dbEntity.id, out T retrievedValue))
                        {
                            throw new Exception($"Unable to remove {typeParameterType.Name} with id \"{dbEntity.id}\" from objectCache.");
                        }
                    }

                    // Add the dbEntity to the objectCache.
                    if (!objectCache.TryAdd(dbEntity.id, dbEntity))
                    {
                        throw new Exception($"Unable to add {typeParameterType.Name} with id \"{dbEntity.id}\" to objectCache.");
                    }

                    // Add or update the geotabIdCache.
                    _ = geotabIdCache.AddOrUpdate(dbEntity.GeotabId, dbEntity.id,
                        (geotabId, existingEntityId) =>
                        {
                            // If this delegate is invoked, then the key already exists. Validate against duplicates to ensure we don't add another item to the geotabIdCache that has the same GeotabId, but a different Id.
                            if (dbEntity.id != existingEntityId)
                            {
                                throw new ArgumentException($"A {typeParameterType.Name} with GeotabId \"{dbEntity.GeotabId}\" already exists objectCache. Duplicates are not allowed. The existing {typeParameterType.Name} has an Id of \"{existingEntityId}\". The {typeParameterType.Name} with the same GeotabId that cannot be added to the objectCache has an Id of \"{dbEntity.id}\".");
                            }
                            // Nothing to do here, since the Id is the only updatable property.
                            return existingEntityId;
                        });
                }
                objectCacheUpdated = true;

                // If there were any updates, build the objectCacheCurrentValuesList so that it doesn't need to be built unnecessarily every time the GetObjectsAsync() method is executed.
                if (objectCacheUpdated == true)
                {
                    objectCacheCurrentValuesList = new List<T>();
                    var objectEnumerator = objectCache.GetEnumerator();
                    objectEnumerator.Reset();
                    while (objectEnumerator.MoveNext())
                    {
                        var currentKVP = objectEnumerator.Current;
                        var currentObject = currentKVP.Value;
                        objectCacheCurrentValuesList.Add(currentObject);
                    }
                }
            }

            lastUpdated = DateTime.UtcNow;
            isUpdating = false;
        }

        /// <summary>
        /// Checks whether the <see cref="InitializeAsync(Databases)"/> method has been invoked since the current class instance was created and throws an <see cref="InvalidOperationException"/> if initialization has not been performed.
        /// </summary>
        void ValidateInitialized()
        {
            if (isInitialized == false && isInternalUpdate == false)
            {
                throw new InvalidOperationException($"The current {CurrentClassName} has not been initialized. The {nameof(InitializeAsync)} method must be called before other methods can be invoked.");
            }
        }
    }
}
