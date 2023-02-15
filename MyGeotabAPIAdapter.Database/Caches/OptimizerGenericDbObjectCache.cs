﻿using MyGeotabAPIAdapter.Database.DataAccess;
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
    public class OptimizerGenericDbObjectCache<T> : IGenericDbObjectCache<T> where T : class, IIdCacheableDbEntity
    {
        // Obtain the type parameter type (for logging purposes).
        readonly Type typeParameterType = typeof(T);

        static string CurrentClassName { get => $"{nameof(OptimizerGenericDbObjectCache<T>)}<{typeof(T).Name}>"; }

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
        readonly SemaphoreSlim updateLock = new(1, 1);

        readonly IDateTimeHelper dateTimeHelper;
        readonly IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> context;
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
        /// Initializes a new instance of the <see cref="OptimizerGenericDbObjectCache{T}"/> class.
        /// </summary>
        public OptimizerGenericDbObjectCache(IDateTimeHelper dateTimeHelper, IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> context)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            lastUpdated = defaultDateTime;
            this.dateTimeHelper = dateTimeHelper;

            this.context = context;
            logger.Debug($"{nameof(IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext>)} [Id: {context.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// *** UNIT TESTING CONSTRUCTOR *** This constructor is designed for unit testing only. It accepts an additional <see cref="IBaseRepository{T}"/> input, which may be a test class. Under normal operation, a <see cref="BaseRepository{T}"/> is created and used within this class when required. Under normal operation, the other constructor should be used in conjunction with Dependency Injection.
        /// </summary>
        public OptimizerGenericDbObjectCache(IDateTimeHelper dateTimeHelper, IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> context, IBaseRepository<T> testDbEntityRepo)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            lastUpdated = defaultDateTime;
            this.dateTimeHelper = dateTimeHelper;

            this.context = context;
            logger.Debug($"{nameof(IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext>)} [Id: {context.Id}] associated with {CurrentClassName}.");

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

            return objectCacheCurrentValuesList;
        }

        /// <inheritdoc/>
        public async Task<List<T>> GetObjectsAsync(DateTime changedSince)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await UpdateAsync();
            await WaitIfUpdatingAsync();

            var results = objectCacheCurrentValuesList.Where(cachedObject => cachedObject.LastUpsertedUtc > changedSince).ToList();

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

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(bool ForceUpdate = false)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Abort if the configured execution interval has not elapsed since the last time this method was executed AND ForceUpdate is false.
            if (!dateTimeHelper.TimeIntervalHasElapsed(lastUpdated, DateTimeIntervalType.Minutes, cacheUpdateIntervalMinutes) && !ForceUpdate)
            {
                return;
            }

            await updateLock.WaitAsync();
            try
            {
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
                            // Get any dbEntities that have been updated since the last time this GenericDbObjectCache was updated. If this is the first time this method has been called since application startup, all dbEntities will be loaded into the cache.
                            var dbEntities = await dbEntityRepo.GetAllAsync(cancellationTokenSource, null, lastUpdated);
                            var objectCacheUpdated = false;

                            foreach (var dbEntity in dbEntities)
                            {
                                objectCacheUpdated = true;

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
        /// Checks whether the <see cref="InitializeAsync(Databases)"/> method has been invoked since the current class instance was created and throws an <see cref="InvalidOperationException"/> if initialization has not been performed.
        /// </summary>
        void ValidateInitialized()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (isInitialized == false && isInternalUpdate == false)
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
