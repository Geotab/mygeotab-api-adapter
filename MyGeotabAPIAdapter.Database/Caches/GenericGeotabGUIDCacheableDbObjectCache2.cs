#nullable enable
using Microsoft.VisualStudio.Threading;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.MyGeotabAPI;
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
    /// A generic class that maintains an in-memory cache of <see cref="IGeotabGUIDCacheableDbEntity"/>s.
    /// </summary>
    /// <typeparam name="T1">The type of entity being cached.</typeparam>
    /// <typeparam name="T2">The type of <see cref="IDatabaseUnitOfWorkContext"/> to use.</typeparam>
    public class GenericGeotabGUIDCacheableDbObjectCache2<T1, T2> : IGenericGeotabGUIDCacheableDbObjectCache2<T1, T2> where T1 : class, IGeotabGUIDCacheableDbEntity where T2 : IDatabaseUnitOfWorkContext
    {
        static string CurrentClassName { get => $"{nameof(GenericGeotabGUIDCacheableDbObjectCache<T1>)}<{typeof(T1).Name}>"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        protected readonly ConcurrentDictionary<long, T1> dbObjectFromDbIdCache = new();
        readonly ConcurrentDictionary<Guid, long> dbIdFromGeotabGuidCache = new();
        readonly ConcurrentDictionary<string, long> dbIdFromGeotabGuidStringCache = new();
        readonly ConcurrentDictionary<string, string> geotabGuidStringFromGeotabIdCache = new();

        int cacheUpdateIntervalMinutes = 5;
        Databases database;
        IBaseRepository<T1> dbEntityRepo;
        readonly DateTime defaultDateTime = DateTime.ParseExact("1912/06/23", "yyyy/MM/dd", CultureInfo.InvariantCulture);
        readonly SemaphoreSlim initializationLock = new(1, 1);
        bool isInitialized;
        bool isUpdating = false;
        DateTime lastUpdated;
        readonly AsyncReaderWriterLock asyncReaderWriterLock = new(null);

        readonly IDateTimeHelper dateTimeHelper;
        readonly IGeotabIdConverter geotabIdConverter;
        readonly T2 context;
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
        public GenericGeotabGUIDCacheableDbObjectCache2(IDateTimeHelper dateTimeHelper, IGeotabIdConverter geotabIdConverter, T2 context)
        {
            lastUpdated = defaultDateTime;
            this.dateTimeHelper = dateTimeHelper;
            this.geotabIdConverter = geotabIdConverter;

            this.context = context;
            logger.Debug($"{nameof(T2)} [Id: {context.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);
        }

        /// <summary>
        /// *** UNIT TESTING CONSTRUCTOR *** This constructor is designed for unit testing only. It accepts an additional <see cref="IBaseRepository{T}"/> input, which may be a test class. Under normal operation, a <see cref="BaseRepository{T}"/> is created and used within this class when required. Under normal operation, the other constructor should be used in conjunction with Dependency Injection.
        /// </summary>
        public GenericGeotabGUIDCacheableDbObjectCache2(IDateTimeHelper dateTimeHelper, T2 context, IBaseRepository<T1> testDbEntityRepo)
        {
            lastUpdated = defaultDateTime;
            this.dateTimeHelper = dateTimeHelper;

            this.context = context;
            logger.Debug($"{nameof(T2)} [Id: {context.Id}] associated with {CurrentClassName}.");

            dbEntityRepo = testDbEntityRepo;
        }

        /// <inheritdoc/>
        public async Task<T1?> GetObjectAsync(long id)
        {
            await UpdateAsync();
            using (await asyncReaderWriterLock.ReadLockAsync())
            {
                if (dbObjectFromDbIdCache.TryGetValue(id, out T1 obj))
                {
                    return obj;
                }
            }
            return default;
        }

        /// <inheritdoc/>
        public async Task<T1?> GetObjectByGeotabGUIDAsync(string geotabGUIDString)
        {
            if (string.IsNullOrEmpty(geotabGUIDString))
            { 
                return default;
            }

            var objectId = await GetObjectIdByGeotabGUIDStringAsync(geotabGUIDString);
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
            await UpdateAsync();
            using (await asyncReaderWriterLock.ReadLockAsync())
            {
                if (geotabGuidStringFromGeotabIdCache.TryGetValue(geotabId, out string geotabGUID))
                {
                    return geotabGUID;
                }
            }
            return default;
        }

        /// <inheritdoc/>
        public async Task<long?> GetObjectIdByGeotabGUIDAsync(Guid geotabGUID)
        {
            await UpdateAsync();
            using (await asyncReaderWriterLock.ReadLockAsync())
            {
                if (dbIdFromGeotabGuidCache.TryGetValue(geotabGUID, out long id))
                {
                    return id;
                }
            }
            return default;
        }

        /// <inheritdoc/>
        public async Task<long?> GetObjectIdByGeotabGUIDStringAsync(string geotabGUIDString)
        {
            await UpdateAsync();
            using (await asyncReaderWriterLock.ReadLockAsync())
            {
                if (dbIdFromGeotabGuidStringCache.TryGetValue(geotabGUIDString, out long id))
                {
                    return id;
                }
            }
            return default;
        }

        /// <inheritdoc/>
        public async Task<List<T1>> GetObjectsAsync()
        {
            await UpdateAsync();
            using (await asyncReaderWriterLock.ReadLockAsync())
            {
                var results = new List<T1>();
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
        }

        /// <inheritdoc/>
        public async Task<List<T1>> GetObjectsAsync(DateTime changedSince)
        {
            await UpdateAsync();
            using (await asyncReaderWriterLock.ReadLockAsync())
            {
                var results = new List<T1>();
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
        }

        /// <inheritdoc/>
        public async Task<List<T1>> GetObjectsAsync(string geotabGUIDString, string geotabId)
        {
            await UpdateAsync();
            using (await asyncReaderWriterLock.ReadLockAsync())
            {
                var results = new List<T1>();
                var objectEnumerator = dbObjectFromDbIdCache.GetEnumerator();
                objectEnumerator.Reset();
                while (objectEnumerator.MoveNext())
                {
                    var currentKVP = objectEnumerator.Current;
                    var currentObject = currentKVP.Value;
                    if (currentObject.GeotabGUIDString == geotabGUIDString && currentObject.GeotabId == geotabId)
                    {
                        results.Add(currentObject);
                    }
                }
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
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(bool ForceUpdate = false)
        {
            // Abort if the configured execution interval has not elapsed since the last time this method was executed AND ForceUpdate is false.
            if (!dateTimeHelper.TimeIntervalHasElapsed(lastUpdated, DateTimeIntervalType.Minutes, cacheUpdateIntervalMinutes) && !ForceUpdate)
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
            IEnumerable<T1>? dbEntities = null;

            // First query the database for any updated entities.
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                if (dbEntityRepo == null)
                {
                    dbEntityRepo = new BaseRepository<T1>(context);
                }

                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                {
                    using (var uow = context.CreateUnitOfWork(database))
                    {
                        // Get any dbEntities that have been updated since the last time this GenericGeotabGUIDCacheableDbObjectCache was updated. If this is the first time this method has been called since application startup, all dbEntities will be loaded into the cache.
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
                    // If the dbEntity is already in the dbObjectFromDbIdCache, remove it so that it can be replaced with the new one.
                    if (dbObjectFromDbIdCache.ContainsKey(dbEntity.id))
                    {
                        if (!dbObjectFromDbIdCache.TryRemove(dbEntity.id, out T1 retrievedValue))
                        {
                            throw new Exception($"Unable to remove {typeof(T1).Name} with id \"{dbEntity.id}\" from dbObjectFromDbIdCache.");
                        }
                    }

                    // Add the dbEntity to the dbObjectFromDbIdCache.
                    if (!dbObjectFromDbIdCache.TryAdd(dbEntity.id, dbEntity))
                    {
                        throw new Exception($"Unable to add {typeof(T1).Name} with id \"{dbEntity.id}\" to dbObjectFromDbIdCache.");
                    }

                    // Add or update the dbIdFromGeotabGuidCache if the entity has an Id that can actually be converted to GUID.
                    dbEntity.GeotabGUID = geotabIdConverter.TryToGuid(dbEntity.GeotabId);
                    if (dbEntity.GeotabGUID != null)
                    {
                        _ = dbIdFromGeotabGuidCache.AddOrUpdate((Guid)dbEntity.GeotabGUID, dbEntity.id,
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
                    }

                    // Add or update the dbIdFromGeotabGuidStringCache.
                    _ = dbIdFromGeotabGuidStringCache.AddOrUpdate(dbEntity.GeotabGUIDString, dbEntity.id,
                        (geotabGUIDString, existingEntityId) =>
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
                    _ = geotabGuidStringFromGeotabIdCache.AddOrUpdate(dbEntity.GeotabId, dbEntity.GeotabGUIDString,
                        (geotabID, existingEntityGeotabGUID) =>
                        {
                            // If this delegate is invoked, then the key already exists. Validate against duplicates to ensure we don't add another item to the geotabGuidFromGeotabIdCache that has the same GeotabId, but a different GeotabGUID.
                            if (dbEntity.GeotabGUIDString != existingEntityGeotabGUID)
                            {
                                throw new ArgumentException($"A {typeof(T1).Name} with GeotabId \"{dbEntity.GeotabId}\" already exists in the geotabGuidFromGeotabIdCache. Duplicates are not allowed. The existing {typeof(T1).Name} has a GeotabGUID of \"{existingEntityGeotabGUID}\". The {typeof(T1).Name} with the same GeotabId that cannot be added to the geotabGuidFromGeotabIdCache has a GeotabGUID of \"{dbEntity.GeotabGUIDString}\".");
                            }
                            // Nothing to do here, since the GeotabGUID is the only updatable property.
                            return existingEntityGeotabGUID;
                        });
                }
            }

            lastUpdated = DateTime.UtcNow;
            isUpdating = false;
        }

        /// <summary>
        /// Checks whether the <see cref="InitializeAsync(Databases)"/> has been invoked since the current class instance was created and throws an <see cref="InvalidOperationException"/> if initialization has not been performed.
        /// </summary>
        void ValidateInitialized()
        {
            if (isInitialized == false)
            {
                throw new InvalidOperationException($"The current {CurrentClassName} has not been initialized. The {nameof(InitializeAsync)} method must be called before other methods can be invoked.");
            }
        }
    }
}
