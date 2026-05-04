namespace MyGeotabAPIAdapter.MyAdminAPI
{
    /// <summary>
    /// Interface for a thread-safe rate limiter for MyAdmin API calls implementing a sliding window algorithm. Based on MyAdmin API <see href="https://developers.geotab.com/myAdmin/guides/rateLimits/">rate limits</see>:
    /// - 5,000 requests per minute per method
    /// - 100,000 requests per day per method
    /// </summary>
    public interface IMyAdminAPIRateLimiter
    {
        /// <summary>
        /// Gets current statistics about the rate limiter state for all methods.
        /// </summary>
        /// <returns>Statistics about requests and limits per method.</returns>
        MyAdminAPIRateLimiterStatistics GetStatistics();

        /// <summary>
        /// Gets current statistics about the rate limiter state for a specific method.
        /// </summary>
        /// <param name="methodName">The name of the API method.</param>
        /// <returns>Statistics about requests and limits for the specified method.</returns>
        MyAdminAPIRateLimiterMethodStatistics GetStatistics(string methodName);

        /// <summary>
        /// Records that a rate limit (429) response was received with no Retry-After header. Uses a default backoff period.
        /// </summary>
        /// <param name="methodName">The name of the API method that received the rate limit response.</param>
        void RecordRateLimitResponse(string methodName);

        /// <summary>
        /// Records that a rate limit (429) response was received for a specific method, triggering a backoff period.
        /// </summary>
        /// <param name="methodName">The name of the API method that received the rate limit response.</param>
        /// <param name="retryAfterSeconds">The number of seconds to wait before retrying, from the Retry-After header.</param>
        void RecordRateLimitResponse(string methodName, int retryAfterSeconds);

        /// <summary>
        /// Waits until a request can be made without exceeding rate limits for the specified method. This method should be called before each API request.
        /// </summary>
        /// <param name="methodName">The name of the API method being called (e.g., "ProvisionDevice", "Authenticate").</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when a request can be made.</returns>
        Task WaitForPermitAsync(string methodName, CancellationToken cancellationToken = default);
    }
}