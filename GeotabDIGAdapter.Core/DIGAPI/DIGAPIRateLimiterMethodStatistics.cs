namespace MyGeotabAPIAdapter.DIGAPI
{
    /// <summary>
    /// Contains statistics for a specific DIG API method.
    /// </summary>
    public class DIGAPIRateLimiterMethodStatistics
    {
        /// <summary>
        /// The name of the API method.
        /// </summary>
        public string MethodName { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether this method is subject to rate limiting.
        /// </summary>
        public bool IsRateLimited { get; set; }

        /// <summary>
        /// The number of requests made in the last minute.
        /// </summary>
        public int RequestsInLastMinute { get; set; }

        /// <summary>
        /// The maximum requests allowed per minute (0 if not rate-limited).
        /// </summary>
        public int MaxRequestsPerMinute { get; set; }

        /// <summary>
        /// Indicates whether the rate limiter is currently in backoff mode.
        /// </summary>
        public bool IsInBackoff { get; set; }

        /// <summary>
        /// The remaining seconds in backoff mode, if applicable.
        /// </summary>
        public double BackoffRemainingSeconds { get; set; }

        /// <summary>
        /// The percentage of the per-minute rate limit currently utilized.
        /// </summary>
        public double MinuteUtilizationPercent => MaxRequestsPerMinute > 0 ? (double)RequestsInLastMinute / MaxRequestsPerMinute * 100 : 0;
    }
}