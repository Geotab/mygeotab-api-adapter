namespace MyGeotabAPIAdapter.DIGAPI
{
    /// <summary>
    /// Contains overall statistics for the DIG API rate limiter.
    /// </summary>
    public class DIGAPIRateLimiterStatistics
    {
        /// <summary>
        /// The maximum requests allowed per minute for rate-limited (authentication) endpoints.
        /// </summary>
        public int MaxRequestsPerMinute { get; set; }

        /// <summary>
        /// The effective requests per minute (after safety margin) for rate-limited endpoints.
        /// </summary>
        public int EffectiveRequestsPerMinute { get; set; }

        /// <summary>
        /// The list of method names that are subject to rate limiting.
        /// </summary>
        public List<string> RateLimitedMethods { get; set; } = [];

        /// <summary>
        /// Per-method statistics.
        /// </summary>
        public Dictionary<string, DIGAPIRateLimiterMethodStatistics> MethodStatistics { get; set; } = [];
    }
}