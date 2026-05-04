namespace MyGeotabAPIAdapter.MyAdminAPI
{
    /// <summary>
    /// Contains overall statistics for the MyAdmin API rate limiter.
    /// </summary>
    public class MyAdminAPIRateLimiterStatistics
    {
        /// <summary>
        /// The maximum requests allowed per minute per method.
        /// </summary>
        public int MaxRequestsPerMinute { get; init; }

        /// <summary>
        /// The maximum requests allowed per day per method.
        /// </summary>
        public int MaxRequestsPerDay { get; init; }

        /// <summary>
        /// The effective requests per minute (after safety margin).
        /// </summary>
        public int EffectiveRequestsPerMinute { get; init; }

        /// <summary>
        /// The effective requests per day (after safety margin).
        /// </summary>
        public int EffectiveRequestsPerDay { get; init; }

        /// <summary>
        /// Per-method statistics.
        /// </summary>
        public Dictionary<string, MyAdminAPIRateLimiterMethodStatistics> MethodStatistics { get; init; } = [];
    }
}
