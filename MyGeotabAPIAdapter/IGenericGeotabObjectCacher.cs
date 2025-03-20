using Geotab.Checkmate.ObjectModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a generic class that handles processing of Geotab <see cref="Entity"/>s that can be classified as "reference data" that should be cached locally for optimal efficiency. These object types are typically user-generated in the Geotab system, remain relatively static and can be updated from time to time. As such, it is necessary to periodically update and refresh the local cache to maintain synchronicity. Examples include <see cref="User"/>, <see cref="Device"/> and <see cref="Zone"/>.
    /// </summary>
    /// <typeparam name="T">The type of Geotab <see cref="Entity"/> to be processed by this <see cref="IGenericGeotabObjectCacher{T}"/> instance.</typeparam>
    internal interface IGenericGeotabObjectCacher<T> where T : Entity
    {
        const int DefaultFeedResultsLimit = 5000;

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the subject cache.
        /// </summary>
        DateTime CacheIntervalDailyReferenceStartTimeUTC { get; set; }

        /// <summary>
        /// The interval, in minutes, used to determine whether the <see cref="GeotabObjectCache"/> cache needs to be refreshed (i.e. purged and fully re-populated to remove items that have been deleted).
        /// </summary>
        int CacheRefreshIntervalMinutes { get; set; }

        /// <summary>
        /// The interval, in minutes, used to determine whether the <see cref="GeotabObjectCache"/> cache needs to be updated (i.e. items added and/or updated).
        /// </summary>
        int CacheUpdateIntervalMinutes { get; set; }

        /// <summary>
        /// The results limit to be supplied to the GetFeed() method for the subject <see cref="Entity"/> type.
        /// </summary>
        int FeedResultsLimit { get; set; }

        /// <summary>
        /// The cache of Geotab <see cref="Entity"/>s.
        /// </summary>
        ConcurrentDictionary<Id, T> GeotabObjectCache { get; }

        /// <summary>
        /// A subset of items in the <see cref="GeotabObjectCache"/> that have been added or updated in the last cache update. This is intended to be used for incremental updates to the database.
        /// </summary>
        IEnumerable<T> GeotabObjectsChangedInLastUpdate { get; }

        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Indicates whether the <see cref="InitializeAsync(CancellationTokenSource, DateTime, int, int, int, bool)"/> method has been invoked since the current class instance was created.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// The last <see cref="CacheOperationType"/> that was executed on the <see cref="GeotabObjectCache"/>.
        /// </summary>
        CacheOperationType LastCacheOperationType { get; set; }

        /// <summary>
        /// The FeedVersion of the latest <see cref="GeotabObjectCache"/>; applies only if the <see cref="GeotabObjectCache"/> is populated via the GetFeed() method.
        /// </summary>
        long? LastFeedVersion { get; set; }

        /// <summary>
        /// The last time when the <see cref="GeotabObjectCache"/> was propagated to the database.
        /// </summary>
        DateTime LastPropagatedToDatabaseTimeUtc { get; set; }

        /// <summary>
        /// The last time when the <see cref="GeotabObjectCache"/> was refreshed (i.e. purged and fully re-populated to remove items that have been deleted).
        /// </summary>
        DateTime LastRefreshedTimeUTC { get; }

        /// <summary>
        /// The last time when the <see cref="GeotabObjectCache"/> was updated (i.e. items added and/or updated).
        /// </summary>
        DateTime LastUpdatedTimeUTC { get; }

        /// <summary>
        /// If <c>true</c>, a GetFeed call will be used for data retrieval. If <c>false</c>, a Get call will be used instead. GetFeed should be used whenever an <see cref="Entity"/> type is supported by a data feed.
        /// </summary>
        bool UseGetFeed { get; set; }

        /// <summary>
        /// Indicates whether the cache of Geotab <see cref="Entity"/>s maintained by the current <see cref="IGenericGeotabObjectCacher<T>"/> instance contains any items.
        /// </summary>
        /// <returns></returns>
        bool Any();

        /// <summary>
        /// Determines what <see cref="CacheOperationType"/> is to be performed with regard to the <see cref="GeotabObjectCache"/> based on the <see cref="CacheIntervalDailyReferenceStartTimeUTC"/>, <see cref="CacheUpdateIntervalMinutes"/> and <see cref="CacheRefreshIntervalMinutes"/> property values.
        /// </summary>
        /// <returns></returns> 
        CacheOperationType GetRequiredCacheOperationType();

        /// <summary>
        /// If not already initialized, initializes the current <see cref="IGenericGeotabObjectCacher<T>"/> instance and executes the <see cref="UpdateGeotabObjectCacheAsync"/> method.
        /// </summary>
        /// <paramref name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</paramref>
        /// <param name="cacheIntervalDailyReferenceStartTimeUTC">Sets the <see cref="CacheIntervalDailyReferenceStartTimeUTC"/> property value.</param>
        /// <param name="cacheUpdateIntervalMinutes">Sets the <see cref="CacheUpdateIntervalMinutes"/> property value.</param>
        /// <param name="cacheRefreshIntervalMinutes">Sets the <see cref="CacheRefreshIntervalMinutes"/> property value.</param>
        /// <param name="feedResultsLimit">Sets the <see cref="FeedResultsLimit"/> property value.</param>
        /// <param name="useGetFeed">Sets the <see cref="UseGetFeed"/> property value.</param>
        /// <returns></returns>
        Task InitializeAsync(CancellationTokenSource cancellationTokenSource, DateTime cacheIntervalDailyReferenceStartTimeUTC, int cacheUpdateIntervalMinutes, int cacheRefreshIntervalMinutes, int feedResultsLimit = DefaultFeedResultsLimit, bool useGetFeed = true);

        /// <summary>
        /// Indicates whether the current <see cref="IGenericGeotabObjectCacher<T>"/> is in the process of being updated.
        /// </summary>
        bool IsUpdating { get; }

        /// <summary>
        /// Updates the cache of Geotab <see cref="Entity"/>s maintained by the current <see cref="IGenericGeotabObjectCacher<T>"/> instance.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns>Asynchronous task</returns>
        Task UpdateGeotabObjectCacheAsync(CancellationTokenSource cancellationTokenSource);

        /// <summary>
        /// Waits until <see cref="IsUpdating"/> is <c>false</c>. Intended for use by methods that enumerate and retrieve cached objects.
        /// </summary>
        /// <returns></returns>
        Task WaitIfUpdatingAsync();
    }

    /// <summary>
    /// Types of operations related to cache processing.
    /// </summary>
    enum CacheOperationType { None, Refresh, Update }
}
