namespace MyGeotabAPIAdapter.DIGAPI
{
    /// <summary>
    /// Interface for a helper class to assist in working with the DIG API.
    /// </summary>
    public interface IDIGAPIHelper
    {
        /// <summary>
        /// The default timeout in seconds for DIG API requests.
        /// </summary>
        const int DefaultTimeoutSeconds = 30;

        /// <summary>
        /// Indicates whether the DIG API session is authenticated.
        /// </summary>
        bool DIGAPIIsAuthenticated { get; }

        /// <summary>
        /// Gets the current bearer token, if authenticated.
        /// </summary>
        DIGToken? CurrentBearerToken { get; }

        /// <summary>
        /// Gets the current refresh token, if authenticated.
        /// </summary>
        DIGToken? CurrentRefreshToken { get; }

        /// <summary>
        /// Authenticates with the DIG API.
        /// </summary>
        /// <param name="digApiEndpoint">The DIG API endpoint (e.g., "https://dig.geotab.com:443").</param>
        /// <param name="username">The DIG username.</param>
        /// <param name="password">The DIG password.</param>
        /// <param name="requestTimeoutSeconds">The timeout, in seconds, for API requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AuthenticateDIGAPIAsync(string digApiEndpoint, string username, string password, int requestTimeoutSeconds = DefaultTimeoutSeconds);

        /// <summary>
        /// Refreshes the current tokens using the existing bearer and refresh tokens.
        /// </summary>
        /// <param name="requestTimeoutSeconds">The timeout, in seconds, for API requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RefreshTokensAsync(int requestTimeoutSeconds = DefaultTimeoutSeconds);

        /// <summary>
        /// Revokes the current tokens.
        /// </summary>
        /// <param name="requestTimeoutSeconds">The timeout, in seconds, for API requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RevokeTokensAsync(int requestTimeoutSeconds = DefaultTimeoutSeconds);

        /// <summary>
        /// Posts records to the DIG API.
        /// </summary>
        /// <param name="records">The records to post.</param>
        /// <param name="requestTimeoutSeconds">The timeout, in seconds, for API requests.</param>
        /// <returns>A <see cref="PostRecordsResult"/>.</returns>
        Task<PostRecordsResult> PostRecordsAsync(IList<object> records, int requestTimeoutSeconds = DefaultTimeoutSeconds);

        /// <summary>
        /// Gets the current rate limiter statistics.
        /// </summary>
        /// <returns>A <see cref="DIGAPIRateLimiterStatistics"/> object.</returns>
        DIGAPIRateLimiterStatistics GetRateLimiterStatistics();

        /// <summary>
        /// Gets the current rate limiter statistics for a specific method.
        /// </summary>
        /// <param name="methodName">The name of the API method.</param>
        /// <returns>A <see cref="DIGAPIRateLimiterMethodStatistics"/> object.</returns>
        DIGAPIRateLimiterMethodStatistics GetRateLimiterStatistics(string methodName);

        /// <summary>
        /// Retrieves invalid records from the DIG API.
        /// </summary>
        /// <param name="nextResultKey">The cursor value to continue pagination. Null for the first request.</param>
        /// <param name="numberOfResults">The maximum number of records to retrieve per request. Default is 1000, max is 50000.</param>
        /// <param name="requestTimeoutSeconds">The timeout, in seconds, for API requests.</param>
        /// <returns>A <see cref="GetInvalidRecordsResult"/>.</returns>
        Task<GetInvalidRecordsResult> GetInvalidRecordsAsync(int? nextResultKey = null, int numberOfResults = 1000, int requestTimeoutSeconds = DefaultTimeoutSeconds);
    }
}