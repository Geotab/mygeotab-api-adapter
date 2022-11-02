using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.MyGeotabAPI;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A generic class that handles processing of Geotab <see cref="Entity"/>s that can be classified as "reference data" that should be cached locally for optimal efficiency. These object types are typically user-generated in the Geotab system, remain relatively static and can be updated from time to time. As such, it is necessary to periodically update and refresh the local cache to maintain synchronicity. Examples include <see cref="User"/>, <see cref="Device"/> and <see cref="Zone"/>.
    /// </summary>
    /// <typeparam name="T">The type of Geotab <see cref="Entity"/> to be processed by this <see cref="GenericGeotabObjectCacher{T}"/> instance.</typeparam>
    internal class GenericGeotabObjectCacher<T> : IGenericGeotabObjectCacher<T> where T : Entity
    {
        // Obtain the type parameter type (for logging purposes).
        readonly Type typeParameterType = typeof(T);

        static string CurrentClassName { get => $"{nameof(GenericGeotabObjectCacher<T>)}<{typeof(T).Name}>"; }

        // According to <see href="https://geotab.github.io/sdk/software/api/reference/#M:Geotab.Checkmate.Database.DataStore.GetFeed1">GetFeed(...)</see>, 5000 is the lowest feed result limit; thus, it is used here as the default, but should be overridden with the correct limit for the subject entity type.
        const int DefaultFeedResultsLimit = 5000;
        const int MinFeedIntervalMilliseconds = 1010;
        const string DuplicateKeyExceptionMessagePrefix = "An item with the same key has already been added. Key:";

        readonly SemaphoreSlim initializationLock = new(1, 1);
        bool isInitialized;
        bool isInternalUpdate;
        bool isUpdating = false;
        readonly SemaphoreSlim updateLock = new(1, 1);

        int feedResultsLimit = DefaultFeedResultsLimit;
        bool useGetFeed = true;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IDateTimeHelper dateTimeHelper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public DateTime CacheIntervalDailyReferenceStartTimeUTC { get; set; }

        /// <inheritdoc/>
        public int CacheRefreshIntervalMinutes { get; set; }

        /// <inheritdoc/>
        public int CacheUpdateIntervalMinutes { get; set; }

        /// <inheritdoc/>
        public int FeedResultsLimit
        {
            get => feedResultsLimit;
            set => feedResultsLimit = value;
        }

        /// <inheritdoc/>
        public ConcurrentDictionary<Id, T> GeotabObjectCache { get; private set; }

        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <inheritdoc/>
        public bool IsInitialized
        {
            get
            {
                if (GeotabObjectCache.IsEmpty)
                {
                    isInitialized = false;
                }
                return isInitialized;
            }
        }

        /// <inheritdoc/>
        public bool IsUpdating { get => isUpdating; }

        /// <inheritdoc/>
        public long? LastFeedVersion { get; set; }

        /// <inheritdoc/>
        public DateTime LastPropagatedToDatabaseTimeUtc { get; set; }

        /// <inheritdoc/>
        public DateTime LastRefreshedTimeUTC { get; private set; }

        /// <inheritdoc/>
        public DateTime LastUpdatedTimeUTC { get; private set; }

        /// <inheritdoc/>
        public bool UseGetFeed
        {
            get => useGetFeed;
            set => useGetFeed = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericGeotabObjectCacher{T}"/> class.
        /// </summary>
        public GenericGeotabObjectCacher(IAdapterConfiguration adapterConfiguration, IDateTimeHelper dateTimeHelper, IMyGeotabAPIHelper myGeotabAPIHelper)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.adapterConfiguration = adapterConfiguration;
            this.dateTimeHelper = dateTimeHelper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            GeotabObjectCache = new ConcurrentDictionary<Id, T>();

            Id = Guid.NewGuid().ToString();
            logger.Debug($"{nameof(GenericGeotabObjectCacher<T>)}<{typeParameterType}> [Id: {Id}] created.");
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public bool Any()
        {
            return !GeotabObjectCache.IsEmpty;
        }

        /// <inheritdoc/>
        public CacheOperationType GetRequiredCacheOperationType()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name} for type '{typeParameterType.Name}'");

            // By default, no cache operation is required, unless the following logic determines an update or refresh is needed.
            var requiredCacheOperationType = CacheOperationType.None;

            if (isInitialized == false)
            {
                // If the subject cache has not yet been retrieved since application startup, it must be refreshed - regardless of update or refresh interval settings.
                logger.Debug($"Initial '{typeParameterType.Name}' cache retrieval not yet executed - refresh required.");
                requiredCacheOperationType = CacheOperationType.Refresh;
            }
            else
            {
                // Initialize the cache interval reference DateTime by using the current date combined with the TimeOfDay from the "CacheIntervalDailyReferenceStartTimeUTC" configured for the current cache entity type.
                var currentDateTimeUTC = DateTime.UtcNow;
                var cacheIntervalReferenceDateTimeUTC = currentDateTimeUTC.Date + CacheIntervalDailyReferenceStartTimeUTC.TimeOfDay;

                // If the calculated cache interval reference time has not yet been reached in the current day, set the cache interval reference DateTime to the same time on the previous day.
                if (currentDateTimeUTC <= cacheIntervalReferenceDateTimeUTC)
                {
                    cacheIntervalReferenceDateTimeUTC = cacheIntervalReferenceDateTimeUTC.AddDays(-1);
                }

                // Calculate the next DateTime that the subject cache should be REFRESHED. Do so by starting with the cache interval reference DateTime and adding the configured refresh interval repeatedly until the calculated DateTime exceeds the DateTime at which the cache was last refreshed.
                var nextRequiredCacheRefreshDateTimeUTC = cacheIntervalReferenceDateTimeUTC;
                while (nextRequiredCacheRefreshDateTimeUTC < LastRefreshedTimeUTC)
                {
                    nextRequiredCacheRefreshDateTimeUTC = nextRequiredCacheRefreshDateTimeUTC.AddMinutes((double)CacheRefreshIntervalMinutes);
                }

                if (currentDateTimeUTC > nextRequiredCacheRefreshDateTimeUTC)
                {
                    // If the current DateTime is greater than the calculated next DateTime that the subject cache should be refreshed, then a cache REFRESH is required.
                    logger.Debug($"Required refresh time of '{nextRequiredCacheRefreshDateTimeUTC:yyyy-MM-dd HH:mm:ss 'GMT'}' has passed for '{typeParameterType.Name}' cache - refresh required.");
                    requiredCacheOperationType = CacheOperationType.Refresh;
                }
                else
                {
                    logger.Debug($"Required refresh time of '{nextRequiredCacheRefreshDateTimeUTC:yyyy-MM-dd HH:mm:ss 'GMT'}' has not yet passed for '{typeParameterType.Name}' cache - refresh not required.");
                    // Given that a cache refresh is not required, determine whether the cache needs to be UPDATED. Calculate the next DateTime that the subject cache should be UPDATED. Do so by starting with the cache interval reference DateTime and adding the configured update interval repeatedly until the calculated DateTime exceeds the DateTime at which the cache was last updated.
                    var nextRequiredCacheUpdateDateTimeUTC = cacheIntervalReferenceDateTimeUTC;
                    while (nextRequiredCacheUpdateDateTimeUTC < LastUpdatedTimeUTC)
                    {
                        nextRequiredCacheUpdateDateTimeUTC = nextRequiredCacheUpdateDateTimeUTC.AddMinutes((double)CacheUpdateIntervalMinutes);
                    }

                    // If the current DateTime is greater than the calculated next DateTime that the subject cache should be updated, then a cache UPDATE is required.
                    if (currentDateTimeUTC > nextRequiredCacheUpdateDateTimeUTC)
                    {
                        logger.Debug($"Required update time of '{nextRequiredCacheUpdateDateTimeUTC:yyyy-MM-dd HH:mm:ss 'GMT'}' has passed for '{typeParameterType.Name}' cache - update required.");
                        requiredCacheOperationType = CacheOperationType.Update;
                    }
                    else
                    {
                        logger.Debug($"Required update time of '{nextRequiredCacheUpdateDateTimeUTC:yyyy-MM-dd HH:mm:ss 'GMT'}' has not yet passed for '{typeParameterType.Name}' cache - update not required.");
                    }
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name} for type '{typeParameterType.Name}'");

            return requiredCacheOperationType;
        }

        /// <inheritdoc/>
        public async Task InitializeAsync(CancellationTokenSource cancellationTokenSource, DateTime cacheIntervalDailyReferenceStartTimeUTC, int cacheUpdateIntervalMinutes, int cacheRefreshIntervalMinutes, int feedResultsLimit = DefaultFeedResultsLimit, bool useGetFeed = true)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await initializationLock.WaitAsync();
            try
            {
                if (isInitialized == false)
                {
                    CacheIntervalDailyReferenceStartTimeUTC = cacheIntervalDailyReferenceStartTimeUTC;
                    CacheUpdateIntervalMinutes = cacheUpdateIntervalMinutes;
                    CacheRefreshIntervalMinutes = cacheRefreshIntervalMinutes;
                    FeedResultsLimit = feedResultsLimit;
                    UseGetFeed = useGetFeed;
                    isInternalUpdate = true;
                    await UpdateGeotabObjectCacheAsync(cancellationTokenSource);
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
        public async Task UpdateGeotabObjectCacheAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name} for type '{typeParameterType.Name}'");

            await updateLock.WaitAsync();
            try
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                ValidateInitialized();
                isUpdating = true;

                // Process cache based on required cache operation type:
                CacheOperationType requiredCacheOperationType = GetRequiredCacheOperationType();
                if (requiredCacheOperationType == CacheOperationType.None)
                {
                    logger.Debug($"{typeParameterType.Name} cache not updated; {CacheUpdateIntervalMinutes} minutes have not passed since last update.");
                }
                else
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // If a cache refresh is required, clear the existing cache so that an entirely new set of records can be captured.
                    if (requiredCacheOperationType == CacheOperationType.Refresh)
                    {
                        logger.Debug($"{typeParameterType.Name} cache refresh required.");
                        GeotabObjectCache.Clear();
                        LastFeedVersion = 0;
                        logger.Info($"{typeParameterType.Name} cache cleared.");
                    }

                    // Populate the cache, adding new items and updating existing items with their changed counterparts from the database.  Repeat execution of the GetFeedAsync method until no more results are returned to ensure that the cache is complete and up-to-date.
                    FeedResult<T> feedResult = null;
                    bool keepGoing = true;
                    int cacheRecordsAdded = 0;

                    DateTime lastApiCallTimeUtc = DateTime.MinValue;
                    while (keepGoing == true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (dateTimeHelper.TimeIntervalHasElapsed(lastApiCallTimeUtc, DateTimeIntervalType.Milliseconds, MinFeedIntervalMilliseconds))
                        {
                            if (UseGetFeed == true)
                            {
                                // Use the GetFeed method for data retrieval.
                                logger.Debug($"Calling GetFeedAsync<{typeParameterType.Name}>.");
                                feedResult = await myGeotabAPIHelper.GetFeedAsync<T>(LastFeedVersion, FeedResultsLimit, adapterConfiguration.TimeoutSecondsForMyGeotabTasks);
                                LastFeedVersion = feedResult.ToVersion;
                                logger.Debug($"GetFeedAsync<{typeParameterType.Name}> returned with {feedResult.Data.Count} records.");
                            }
                            else
                            {
                                // Use the Get method for data retrieval.
                                logger.Debug($"Calling GetAsync<{typeParameterType.Name}>.");
                                feedResult = new FeedResult<T>
                                {
                                    Data = await myGeotabAPIHelper.GetAsync<T>(null, adapterConfiguration.TimeoutSecondsForMyGeotabTasks)
                                };
                                logger.Debug($"GetAsync<{typeParameterType.Name}> returned with {feedResult.Data.Count} records.");
                            }
                            lastApiCallTimeUtc = DateTime.UtcNow;
                            if (feedResult == null)
                            {
                                logger.Warn($"{methodBase.ReflectedType.Name}.{methodBase.Name} of type '{ typeParameterType.Name}' produced zero results indicating a serious problem with the subject type. Investigation required.");
                                break;
                            }

                            // Process the retrieved Geotab objects.
                            foreach (T feedResultItem in feedResult.Data)
                            {
                                // If the cache is being updated and it already contains the current object, remove the object from the cache; it will be replaced with the new version later.
                                if (requiredCacheOperationType == CacheOperationType.Update && GeotabObjectCache.ContainsKey(feedResultItem.Id))
                                {
                                    if (!GeotabObjectCache.TryRemove(feedResultItem.Id, out T retrievedValue))
                                    {
                                        throw new Exception($"Unable to remove {typeParameterType.Name} with id \"{feedResultItem.Id}\" from GeotabObjectCache.");
                                    }
                                }

                                // ZoneStop Rules should not be added to the cache if ZoneStops are not being tracked.
                                bool excludeItemFromCache = false;
                                if (typeParameterType == typeof(Rule) && adapterConfiguration.TrackZoneStops == false)
                                {
                                    var rule = feedResultItem as Rule;
                                    if (rule.BaseType == ExceptionRuleBaseType.ZoneStop)
                                    {
                                        excludeItemFromCache = true;
                                    }
                                }

                                // Add the object to the GeotabObjectCache.
                                if (excludeItemFromCache == false)
                                {
                                    if (!GeotabObjectCache.TryAdd(feedResultItem.Id, feedResultItem))
                                    {
                                        throw new Exception($"Unable to add {typeParameterType.Name} with id \"{feedResultItem.Id}\" to GeotabObjectCache.");
                                    }
                                    cacheRecordsAdded += 1;
                                }
                            }
                            if (feedResult.Data.Count < FeedResultsLimit)
                            {
                                keepGoing = false;
                            }
                        }
                    }
                    DateTime currentDateTime = DateTime.UtcNow;
                    // The LastUpdatedTimeUtc is always updated because a refresh encompasses everyting that is done during an update.
                    LastUpdatedTimeUTC = currentDateTime;
                    logger.Info($"{typeParameterType.Name} cache updated with {cacheRecordsAdded} records added.");
                    if (requiredCacheOperationType == CacheOperationType.Refresh)
                    {
                        LastRefreshedTimeUTC = currentDateTime;
                        logger.Info($"{typeParameterType.Name} cache refresh completed.");
                    }
                }
                isUpdating = false;
            }
            finally
            {
                updateLock.Release();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name} for type '{typeParameterType.Name}'");
        }

        /// <summary>
        /// Checks whether the <see cref="InitializeAsync(CancellationTokenSource, DateTime, int, int, int, bool)"/> method has been invoked since the current class instance was created and throws an <see cref="InvalidOperationException"/> if initialization has not been performed.
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
