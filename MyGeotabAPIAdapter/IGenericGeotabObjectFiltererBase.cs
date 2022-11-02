using Geotab.Checkmate.ObjectModel;
using System.Collections.Concurrent;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a generic class that builds and maintains a ConcurrentDictionary of Geotab <see cref="T"/> objects corresponding with a supplied list of Geotab <see cref="Entity"/> Ids. Intended to be injected (via Dependency Injection) into type-specific classes that are used to filter lists of other types of Geotab <see cref="Entity"/> objects that are related to the subject <see cref="T"/> objects in the <see cref="GeotabObjectsToFilterOn"/>. 
    /// </summary>
    /// <typeparam name="T">The type of <see cref="Entity"/> to comprise the <see cref="GeotabObjectsToFilterOn"/>.</typeparam>
    internal interface IGenericGeotabObjectFiltererBase<T> where T : Entity
    {
        /// <summary>
        /// The list of <see cref="T"/> Geotab objects corresponding with the list of idsOfGeotabObjectsToFilterOn supplied in the <see cref="InitializeAsync(CancellationTokenSource, string)"/> method.
        /// </summary>
        ConcurrentDictionary<Id, T> GeotabObjectsToFilterOn { get; }

        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Indicates whether the <see cref="InitializeAsync(CancellationTokenSource, string)"/> method has been invoked since the current class instance was created.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// If not already initialized, initializes the current <see cref="IGenericGeotabObjectFiltererBase{T}"/> instance and populates the <see cref="GeotabObjectsToFilterOn"/> ConcurrentDictionary.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="idsOfGeotabObjectsToFilterOn">The comma-separated list of <see cref="T.Id"/>s representing corresponding Geotab <see cref="T"/> objects to be loaded into the <see cref="GeotabObjectsToFilterOn"/> ConcurrentDictionary.</param>
        /// <param name="cacheIntervalDailyReferenceStartTimeUTC">Sets the <see cref="CacheIntervalDailyReferenceStartTimeUTC"/> property value.</param>
        /// <param name="cacheUpdateIntervalMinutes">Sets the <see cref="CacheUpdateIntervalMinutes"/> property value.</param>
        /// <param name="cacheRefreshIntervalMinutes">Sets the <see cref="CacheRefreshIntervalMinutes"/> property value.</param>
        /// <param name="feedResultsLimit">Sets the <see cref="FeedResultsLimit"/> property value.</param>
        /// <param name="useGetFeed">Sets the <see cref="UseGetFeed"/> property value.</param>
        /// <returns></returns>
        Task InitializeAsync(CancellationTokenSource cancellationTokenSource, string idsOfGeotabObjectsToFilterOn, DateTime cacheIntervalDailyReferenceStartTimeUTC, int cacheUpdateIntervalMinutes, int cacheRefreshIntervalMinutes, int feedResultsLimit, bool useGetFeed);
    }
}
