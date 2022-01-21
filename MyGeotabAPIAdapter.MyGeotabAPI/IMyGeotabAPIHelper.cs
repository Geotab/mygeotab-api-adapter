using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.MyGeotabAPI
{
    /// <summary>
    /// Interface for a helper class to assist in working with the MyGeotab API (.NET API wrapper - <see cref="Geotab.Checkmate.ObjectModel"/>).
    /// </summary>
    public interface IMyGeotabAPIHelper
    {
        /// <summary>
        /// Returns a list of <see cref="Entity"/>s of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Entity"/> to be retrieved.</typeparam>
        /// <param name="myGeotabApi">An authenticated MyGeotab <see cref="API"/> object.</param>
        /// <param name="search">A <see cref="Search"/> to be applied in order to return a specific <see cref="Entity"/> or set thereof. If <c>null</c>, all entities of the subject type are returned.</param>
        /// <param name="timeoutSeconds">The maximum number of seconds that the <see cref="System.Threading.Tasks.Task"/> can take to be completed before it is deemed that there is a MyGeotab connectivity issue and a <see cref="MyGeotabConnectionException"/> will be thrown.</param>
        /// <returns>A list of objects of the subject <see cref="Type"/>.</returns>
        Task<IList<T>> GetAsync<T>(API myGeotabApi, Search search, int timeoutSeconds);

        /// <summary>
        /// Returns a batch of data of the specified <see cref="Entity"/> type starting at the specified <c>fromDate</c>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Entity"/> to be retrieved.</typeparam>
        /// <param name="myGeotabApi">An authenticated MyGeotab <see cref="API"/> object.</param>
        /// <param name="fromDate">The starting ("seed") date to use when making the first <c>GetFeed</c> call. All new data that has arrived after this date will be returned in this call, up to a maximum of <c>resultsLimit</c> data records. The <see cref="FeedResult{T}"/> returned by the feed method will contain the highest version for subsequent calls.</param>
        /// <param name="resultsLimit">The maximum number of records to return. The default and the maximum value is 50,000 unless otherwise indicated in the <see href="https://geotab.github.io/sdk/software/guides/concepts/#result-limits">MyGeotab SDK documentation</see>.</param>
        /// <param name="timeoutSeconds">The maximum number of seconds that the <see cref="System.Threading.Tasks.Task"/> can take to be completed before it is deemed that there is a MyGeotab connectivity issue and a <see cref="MyGeotabConnectionException"/> will be thrown.</param>
        /// <returns></returns>
        Task<FeedResult<T>> GetFeedAsync<T>(API myGeotabApi, DateTime? fromDate, int resultsLimit, int timeoutSeconds) where T : Entity;

        /// <summary>
        /// Returns a batch of data of the specified <see cref="Entity"/> type starting at the specified <c>fromVersion</c>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Entity"/> to be retrieved.</typeparam>
        /// <param name="myGeotabApi">An authenticated MyGeotab <see cref="API"/> object.</param>
        /// <param name="fromVersion">Last retrieved version. All new data that has arrived after this version will be returned in this call, up to a maximum of <c>resultsLimit</c> data records. The <see cref="FeedResult{T}"/> returned by the feed method will contain the highest version for subsequent calls. When starting a new feed, if this value is not provided, the call will return only the <c>toVersion</c> (last version in the system).</param>
        /// <param name="resultsLimit">The maximum number of records to return. The default and the maximum value is 50,000 unless otherwise indicated in the <see href="https://geotab.github.io/sdk/software/guides/concepts/#result-limits">MyGeotab SDK documentation</see>.</param>
        /// <param name="timeoutSeconds">The maximum number of seconds that the <see cref="System.Threading.Tasks.Task"/> can take to be completed before it is deemed that there is a MyGeotab connectivity issue and a <see cref="MyGeotabConnectionException"/> will be thrown.</param>
        /// <returns></returns>
        Task<FeedResult<T>> GetFeedAsync<T>(API myGeotabApi, long fromVersion, int resultsLimit, int timeoutSeconds) where T : Entity;

        /// <summary>
        /// Modifies the <paramref name="entity"/> in the MyGeotab database.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Entity"/> to be modified.</typeparam>
        /// <param name="myGeotabApi">An authenticated MyGeotab <see cref="API"/> object.</param>
        /// <param name="entity">The <see cref="Entity"/> to be modified.</param>
        /// <param name="timeoutSeconds">The maximum number of seconds that the <see cref="System.Threading.Tasks.Task"/> can take to be completed before it is deemed that there is a MyGeotab connectivity issue and a <see cref="MyGeotabConnectionException"/> will be thrown.</param>
        /// <returns>A list of objects of the subject <see cref="Type"/>.</returns>
        Task<object> SetAsync<T>(API myGeotabApi, T entity, int timeoutSeconds);
    }
}
