using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;
using NLog;
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
    /// A generic class that maintains an in-memory cache of <see cref="IIdCacheableDbEntity"/>s.
    /// </summary>
    /// <typeparam name="T">The type of entity being cached.</typeparam>
    public class GenericDbObjectCache<T> : IGenericDbObjectCache<T> where T : class, IIdCacheableDbEntity
    {
        static string CurrentClassName { get => $"{nameof(GenericDbObjectCache<T>)}<{typeof(T).Name}>"; }

        protected readonly ConcurrentDictionary<long, T> objectCache = new();
        readonly ConcurrentDictionary<string, long> geotabIdCache = new();

        int cacheUpdateIntervalMinutes = 5;
        Databases database;
        IBaseRepository2<T> dbEntityRepo;
        readonly DateTime defaultDateTime = DateTime.ParseExact("1912/06/23", "yyyy/MM/dd", CultureInfo.InvariantCulture);
        readonly SemaphoreSlim initializationLock = new(1, 1);
        bool isInitialized;
        bool isUpdating = false;
        DateTime lastUpdated;
        readonly SemaphoreSlim updateLock = new(1, 1);

        readonly IDateTimeHelper dateTimeHelper;
        readonly UnitOfWorkContext context;
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
        /// Initializes a new instance of the <see cref="GenericDbObjectCache{T}"/> class.
        /// </summary>
        public GenericDbObjectCache(IDateTimeHelper dateTimeHelper, UnitOfWorkContext context)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            lastUpdated = defaultDateTime;
            this.dateTimeHelper = dateTimeHelper;

            this.context = context;
            logger.Debug($"{nameof(UnitOfWorkContext)} [Id: {context.Id}] associated with {CurrentClassName}.");

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// *** UNIT TESTING CONSTRUCTOR *** This constructor is designed for unit testing only. It accepts an additional <see cref="IBaseRepository2{T}"/> input, which may be a test class. Under normal operation, a <see cref="BaseRepository2{T}"/> is created and used within this class when required. Under normal operation, the other constructor should be used in conjunction with Dependency Injection.
        /// </summary>
        public GenericDbObjectCache(IDateTimeHelper dateTimeHelper, UnitOfWorkContext context, IBaseRepository2<T> testDbEntityRepo)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            lastUpdated = defaultDateTime;
            this.dateTimeHelper = dateTimeHelper;

            this.context= context;
            logger.Debug($"{nameof(UnitOfWorkContext)} [Id: {context.Id}] associated with {CurrentClassName}.");

            dbEntityRepo = testDbEntityRepo;

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task<T> GetObjectAsync(long id)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await UpdateAsync();
            await WaitIfUpdatingAsync();

            if (objectCache.TryGetValue(id, out T obj))
            {
                return obj;
            }
            return default;
        }

        /// <inheritdoc/>
        public async Task<T> GetObjectAsync(string geotabId)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

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
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await UpdateAsync();
            await WaitIfUpdatingAsync();

            if (geotabIdCache.TryGetValue(geotabId, out long id))
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
            var objectEnumerator = objectCache.GetEnumerator();
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
            var objectEnumerator = objectCache.GetEnumerator();
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
                        dbEntityRepo = new BaseRepository2<T>(context);
                    }

                    isUpdating = true;
                    using (var uow = context.CreateUnitOfWork(database))
                    {
                        // Get any dbEntities that have been updated since the last time this GenericDbObjectCache was updated. If this is the first time this method has been called since application startup, all dbEntities will be loaded into the cache.
                        var dbEntites = await dbEntityRepo.GetAllAsync(cancellationTokenSource, null, lastUpdated);

                        foreach (var dbEntity in dbEntites)
                        {
                            // If the dbEntity is already in the objectCache, remove it so that it can be replaced with the new one.
                            if (objectCache.ContainsKey(dbEntity.id))
                            {
                                if (!objectCache.TryRemove(dbEntity.id, out T retrievedValue))
                                {
                                    throw new Exception($"Unable to remove {typeof(T).Name} with id \"{dbEntity.id}\" from objectCache.");
                                }
                            }

                            // Add the dbEntity to the objectCache.
                            if (!objectCache.TryAdd(dbEntity.id, dbEntity))
                            {
                                throw new Exception($"Unable to add {typeof(T).Name} with id \"{dbEntity.id}\" to objectCache.");
                            }

                            // Add or update the geotabIdCache.
                            _ = geotabIdCache.AddOrUpdate(dbEntity.GeotabId, dbEntity.id,
                                (geotabId, existingEntityId) =>
                                {
                                // If this delegate is invoked, then the key already exists. Validate against duplicates to ensure we don't add another item to the geotabIdCache that has the same GeotabId, but a different Id.
                                if (dbEntity.id != existingEntityId)
                                    {
                                        throw new ArgumentException($"A {typeof(T).Name} with GeotabId \"{dbEntity.GeotabId}\" already exists objectCache. Duplicates are not allowed. The existing {typeof(T).Name} has an Id of \"{existingEntityId}\". The {typeof(T).Name} with the same GeotabId that cannot be added to the objectCache has an Id of \"{dbEntity.id}\".");
                                    }
                                // Nothing to do here, since the Id is the only updatable property.
                                return existingEntityId;
                                });
                        }
                    }
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
        /// Checks whether the <see cref="InitializeAsync(UnitOfWorkContext, Databases)"/> has been invoked since the current class instance was created and throws an <see cref="InvalidOperationException"/> if initialization has not been performed.
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
