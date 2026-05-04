namespace MyGeotabAPIAdapter.MyAdminAPI
{
    /// <summary>
    /// Contains statistics for a specific MyAdmin API method.
    /// </summary>
    public class MyAdminAPIRateLimiterMethodStatistics
    {
        /// <summary>
        /// The name of the API method.
        /// </summary>
        public string MethodName { get; init; }

        /// <summary>
        /// The number of requests made in the last minute.
        /// </summary>
        public int RequestsInLastMinute { get; init; }

        /// <summary>
        /// The number of requests made in the last day.
        /// </summary>
        public int RequestsInLastDay { get; init; }

        /// <summary>
        /// The maximum requests allowed per minute.
        /// </summary>
        public int MaxRequestsPerMinute { get; init; }

        /// <summary>
        /// The maximum requests allowed per day.
        /// </summary>
        public int MaxRequestsPerDay { get; init; }

        /// <summary>
        /// Indicates whether the rate limiter is currently in backoff mode.
        /// </summary>
        public bool IsInBackoff { get; init; }

        /// <summary>
        /// The remaining seconds in backoff mode, if applicable.
        /// </summary>
        public double BackoffRemainingSeconds { get; init; }

        /// <summary>
        /// The percentage of the per-minute rate limit currently utilized.
        /// </summary>
        public double MinuteUtilizationPercent => MaxRequestsPerMinute > 0 ? (double)RequestsInLastMinute / MaxRequestsPerMinute * 100 : 0;

        /// <summary>
        /// The percentage of the per-day rate limit currently utilized.
        /// </summary>
        public double DayUtilizationPercent => MaxRequestsPerDay > 0 ? (double)RequestsInLastDay / MaxRequestsPerDay * 100 : 0;
    }
}
