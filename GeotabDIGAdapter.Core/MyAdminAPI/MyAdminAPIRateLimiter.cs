using NLog;
using System.Collections.Concurrent;

namespace MyGeotabAPIAdapter.MyAdminAPI
{
    /// <summary>
    /// A thread-safe rate limiter for MyAdmin API calls implementing a sliding window algorithm. Based on MyAdmin API <see href="https://developers.geotab.com/myAdmin/guides/rateLimits/">rate limits</see>:
    /// - 5,000 requests per minute per method
    /// - 100,000 requests per day per method
    /// </summary>
    public class MyAdminAPIRateLimiter : IMyAdminAPIRateLimiter
    {
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        // Rate limit configuration from MyAdmin API documentation:
        const int DefaultRequestsPerMinute = 5000;
        const int DefaultRequestsPerDay = 100000;
        const double SafetyMarginPercent = 0.95;

        readonly int requestsPerMinute;
        readonly int requestsPerDay;
        readonly int effectiveRequestsPerMinute;
        readonly int effectiveRequestsPerDay;

        // Per-method tracking using a concurrent dictionary.
        readonly ConcurrentDictionary<string, MyAdminAPIMethodRateLimitTracker> methodTrackers = new(StringComparer.OrdinalIgnoreCase);

        // Global lock for creating new trackers.
        readonly Lock trackerCreationLock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MyAdminAPIRateLimiter"/> class with default rate limits.
        /// </summary>
        public MyAdminAPIRateLimiter() : this(DefaultRequestsPerMinute, DefaultRequestsPerDay)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MyAdminAPIRateLimiter"/> class with custom rate limits.
        /// </summary>
        /// <param name="requestsPerMinute">Maximum requests allowed per minute per method.</param>
        /// <param name="requestsPerDay">Maximum requests allowed per day per method.</param>
        public MyAdminAPIRateLimiter(int requestsPerMinute, int requestsPerDay)
        {
            this.requestsPerMinute = requestsPerMinute > 0 ? requestsPerMinute : DefaultRequestsPerMinute;
            this.requestsPerDay = requestsPerDay > 0 ? requestsPerDay : DefaultRequestsPerDay;

            // Apply safety margin.
            effectiveRequestsPerMinute = (int)(this.requestsPerMinute * SafetyMarginPercent);
            effectiveRequestsPerDay = (int)(this.requestsPerDay * SafetyMarginPercent);

            logger.Info($"{nameof(MyAdminAPIRateLimiter)} initialized with limits per method: {this.requestsPerMinute}/minute (effective: {effectiveRequestsPerMinute}), {this.requestsPerDay}/day (effective: {effectiveRequestsPerDay}).");
        }

        /// <summary>
        /// Gets or creates a rate limit tracker for the specified method.
        /// </summary>
        MyAdminAPIMethodRateLimitTracker GetOrCreateTracker(string methodName)
        {
            if (methodTrackers.TryGetValue(methodName, out var tracker))
            {
                return tracker;
            }

            lock (trackerCreationLock)
            {
                // Double-check after acquiring lock
                if (methodTrackers.TryGetValue(methodName, out tracker))
                {
                    return tracker;
                }

                tracker = new MyAdminAPIMethodRateLimitTracker(methodName, effectiveRequestsPerMinute, effectiveRequestsPerDay, logger);
                methodTrackers[methodName] = tracker;
                logger.Debug($"Created rate limit tracker for method '{methodName}'.");
                return tracker;
            }
        }

        /// <inheritdoc/>
        public MyAdminAPIRateLimiterStatistics GetStatistics()
        {
            var methodStats = new Dictionary<string, MyAdminAPIRateLimiterMethodStatistics>();

            foreach (var kvp in methodTrackers)
            {
                methodStats[kvp.Key] = kvp.Value.GetStatistics();
            }

            return new MyAdminAPIRateLimiterStatistics
            {
                MaxRequestsPerMinute = requestsPerMinute,
                MaxRequestsPerDay = requestsPerDay,
                EffectiveRequestsPerMinute = effectiveRequestsPerMinute,
                EffectiveRequestsPerDay = effectiveRequestsPerDay,
                MethodStatistics = methodStats
            };
        }

        /// <inheritdoc/>
        public MyAdminAPIRateLimiterMethodStatistics GetStatistics(string methodName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            if (methodTrackers.TryGetValue(methodName, out var tracker))
            {
                return tracker.GetStatistics();
            }

            // Return empty statistics for unknown methods
            return new MyAdminAPIRateLimiterMethodStatistics
            {
                MethodName = methodName,
                RequestsInLastMinute = 0,
                RequestsInLastDay = 0,
                MaxRequestsPerMinute = effectiveRequestsPerMinute,
                MaxRequestsPerDay = effectiveRequestsPerDay,
                IsInBackoff = false,
                BackoffRemainingSeconds = 0
            };
        }

        /// <inheritdoc/>
        public void RecordRateLimitResponse(string methodName)
        {
            // Default backoff of 60 seconds if no Retry-After header provided
            RecordRateLimitResponse(methodName, 60);
        }

        /// <inheritdoc/>
        public void RecordRateLimitResponse(string methodName, int retryAfterSeconds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            var tracker = GetOrCreateTracker(methodName);
            tracker.RecordRateLimitResponse(retryAfterSeconds);
        }

        /// <inheritdoc/>
        public async Task WaitForPermitAsync(string methodName, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            var tracker = GetOrCreateTracker(methodName);
            await tracker.WaitForPermitAsync(cancellationToken);
        }
    }
}