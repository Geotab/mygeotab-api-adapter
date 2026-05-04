namespace MyGeotabAPIAdapter.DIGAPI
{
    /// <summary>
    /// Interface for a rate limiter for DIG API calls. Based on DIG API <see href="https://docs.google.com/document/d/1aRPIDz7d49BEqEID_ZLjtrhwAuHacWO1WhTMUVUXwMI/edit?pli=1&tab=t.0#heading=h.7gsa4og8wue5">rate limits</see>:
    /// - Authentication endpoints (/authentication/authenticate, /authentication/refresh-token, /authentication/revoke-token): 5 requests per minute each
    /// - Note: /records and /invalidrecords endpoints are NOT subject to the same rate limiting.
    /// </summary>
    public interface IDIGAPIRateLimiter
    {
        /// <summary>
        /// Gets the current rate limiter statistics.
        /// </summary>
        /// <returns>A <see cref="DIGAPIRateLimiterStatistics"/> object.</returns>
        DIGAPIRateLimiterStatistics GetStatistics();

        /// <summary>
        /// Gets the current rate limiter statistics for a specific method.
        /// </summary>
        /// <param name="methodName">The name of the API method.</param>
        /// <returns>A <see cref="DIGAPIRateLimiterMethodStatistics"/> object.</returns>
        DIGAPIRateLimiterMethodStatistics GetStatistics(string methodName);

        /// <summary>
        /// Records a rate limit (429) response for the specified method with a default backoff.
        /// </summary>
        /// <param name="methodName">The name of the API method.</param>
        void RecordRateLimitResponse(string methodName);

        /// <summary>
        /// Records a rate limit (429) response for the specified method with a specific Retry-After value.
        /// </summary>
        /// <param name="methodName">The name of the API method.</param>
        /// <param name="retryAfterSeconds">The number of seconds to wait before retrying.</param>
        void RecordRateLimitResponse(string methodName, int retryAfterSeconds);

        /// <summary>
        /// Waits for a permit to make an API call for the specified method.
        /// For non-authentication endpoints, this returns immediately.
        /// </summary>
        /// <param name="methodName">The name of the API method being called.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when a request can be made.</returns>
        Task WaitForPermitAsync(string methodName, CancellationToken cancellationToken = default);
    }
}