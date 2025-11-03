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
        const int DefaultResultsLimit = 50000;
        const int DefaultTimeoutSeconds = 30;

        /// <summary>
        /// The default results limit for the GetFeed method of the Geotab API.
        /// </summary>
        int GetFeedResultLimitDefault { get; }

        /// <summary>
        /// The default results limit for the GetFeed method of the Geotab API when querying for <see cref="Device"/> objects.
        /// </summary>
        int GetFeedResultLimitDevice { get; }

        /// <summary>
        /// The default results limit for the GetFeed method of the Geotab API when querying for <see cref="MediaFile"/> objects.
        /// </summary>
        int GetFeedResultLimitMediaFile { get; }

        /// <summary>
        /// The default results limit for the GetFeed method of the Geotab API when querying for <see cref="Route"/> objects.
        /// </summary>
        int GetFeedResultLimitRoute { get; }

        /// <summary>
        /// The default results limit for the GetFeed method of the Geotab API when querying for <see cref="Rule"/> objects.
        /// </summary>
        int GetFeedResultLimitRule { get; }

        /// <summary>
        /// The default results limit for the GetFeed method of the Geotab API when querying for <see cref="User"/> objects.
        /// </summary>
        int GetFeedResultLimitUser { get; }

        /// <summary>
        /// The default results limit for the GetFeed method of the Geotab API when querying for <see cref="Zone"/> objects.
        /// </summary>
        int GetFeedResultLimitZone { get; }

        /// <summary>
        /// A <see cref="API"/> instance to be used for interfacing with the MyGeotab platform.
        /// </summary>
        API MyGeotabAPI { get; }

        /// <summary>
        /// Indicates whether the <see cref="MyGeotabAPI"/> is authenticated.
        /// </summary>
        bool MyGeotabAPIIsAuthenticated { get; }

        /// <summary>
        /// Authenticates the <see cref="MyGeotabAPI"/> object.
        /// </summary>
        /// <param name="userName">The MyGeotab user name.</param>
        /// <param name="password">The MyGeotab password.</param>
        /// <param name="database">The MyGeotab database.</param>
        /// <param name="server">The MyGeotab server.</param>
        /// <param name="requestTimeoutSeconds">The timeout, in seconds, for <see cref="API"/> requests.</param>
        /// <returns></returns>
        /// <exception cref="MyGeotabConnectionException"></exception>
        Task AuthenticateMyGeotabApiAsync(string userName, string password, string database, string server, int requestTimeoutSeconds = DefaultTimeoutSeconds);

        /// <summary>
        /// Returns a list of <see cref="Entity"/>s of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Entity"/> to be retrieved.</typeparam>
        /// <param name="search">A <see cref="Search"/> to be applied in order to return a specific <see cref="Entity"/> or set thereof. If <c>null</c>, all entities of the subject type are returned.</param>
        /// <param name="timeoutSeconds">The maximum number of seconds that the <see cref="System.Threading.Tasks.Task"/> can take to be completed before it is deemed that there is a MyGeotab connectivity issue and a <see cref="MyGeotabConnectionException"/> will be thrown.</param>
        /// <returns>A list of objects of the subject <see cref="Type"/>.</returns>
        Task<IList<T>> GetAsync<T>(Search search = null, int timeoutSeconds = DefaultTimeoutSeconds) where T : class, IEntity;

        /// <summary>
        /// Returns a batch of data of the specified <see cref="Entity"/> type starting at the specified <c>fromDate</c>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Entity"/> to be retrieved.</typeparam>
        /// <param name="fromDate">The starting ("seed") date to use when making the first <c>GetFeed</c> call. All new data that has arrived after this date will be returned in this call, up to a maximum of <c>resultsLimit</c> data records. The <see cref="FeedResult{T}"/> returned by the feed method will contain the highest version for subsequent calls.</param>
        /// <param name="resultsLimit">The maximum number of records to return. The default and the maximum value is 50,000 unless otherwise indicated in the <see href="https://geotab.github.io/sdk/software/guides/concepts/#result-limits">MyGeotab SDK documentation</see>.</param>
        /// <param name="timeoutSeconds">The maximum number of seconds that the <see cref="System.Threading.Tasks.Task"/> can take to be completed before it is deemed that there is a MyGeotab connectivity issue and a <see cref="MyGeotabConnectionException"/> will be thrown.</param>
        /// <returns></returns>
        Task<FeedResult<T>> GetFeedAsync<T>(DateTime? fromDate = null, int resultsLimit = DefaultResultsLimit, int timeoutSeconds = DefaultTimeoutSeconds) where T : class, IEntity;

        /// <summary>
        /// Returns a batch of data of the specified <see cref="Entity"/> type starting at the specified <c>fromVersion</c>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Entity"/> to be retrieved.</typeparam>
        /// <param name="fromVersion">Last retrieved version. All new data that has arrived after this version will be returned in this call, up to a maximum of <c>resultsLimit</c> data records. The <see cref="FeedResult{T}"/> returned by the feed method will contain the highest version for subsequent calls. When starting a new feed, if this value is not provided, the call will return only the <c>toVersion</c> (last version in the system).</param>
        /// <param name="resultsLimit">The maximum number of records to return. The default and the maximum value is 50,000 unless otherwise indicated in the <see href="https://geotab.github.io/sdk/software/guides/concepts/#result-limits">MyGeotab SDK documentation</see>.</param>
        /// <param name="timeoutSeconds">The maximum number of seconds that the <see cref="System.Threading.Tasks.Task"/> can take to be completed before it is deemed that there is a MyGeotab connectivity issue and a <see cref="MyGeotabConnectionException"/> will be thrown.</param>
        /// <returns></returns>
        Task<FeedResult<T>> GetFeedAsync<T>(long? fromVersion, int resultsLimit = DefaultResultsLimit, int timeoutSeconds = DefaultTimeoutSeconds) where T : class, IEntity;

        /// <summary>
        /// Returns the <see cref="VersionInformation"/> associated with the MyGeotab server that the <see cref="API"/> is connected to.
        /// </summary>
        /// <param name="timeoutSeconds">The maximum number of seconds that the <see cref="System.Threading.Tasks.Task"/> can take to be completed before it is deemed that there is a MyGeotab connectivity issue and a <see cref="MyGeotabConnectionException"/> will be thrown.</param>
        /// <returns>The <see cref="VersionInformation"/> associated with the MyGeotab server that the <see cref="API"/> is connected to.</returns>
        /// <exception cref="MyGeotabConnectionException"></exception>
        Task<VersionInformation> GetVersionInformationAsync(int timeoutSeconds = DefaultTimeoutSeconds);

        /// <summary>
        /// Modifies the <paramref name="entity"/> in the MyGeotab database.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Entity"/> to be modified.</typeparam>
        /// <param name="entity">The <see cref="Entity"/> to be modified.</param>
        /// <param name="timeoutSeconds">The maximum number of seconds that the <see cref="System.Threading.Tasks.Task"/> can take to be completed before it is deemed that there is a MyGeotab connectivity issue and a <see cref="MyGeotabConnectionException"/> will be thrown.</param>
        Task SetAsync<T>(T entity, int timeoutSeconds = DefaultTimeoutSeconds) where T : class, IEntity;
    }
}
