using NLog;
using System.Collections.Concurrent;

namespace MyGeotabAPIAdapter.DIGAPI
{
    /// <summary>
    /// Tracks rate limits for a specific DIG API method using a sliding window algorithm.
    /// </summary>
    sealed class DIGAPIMethodRateLimitTracker
    {
        readonly string methodName;
        readonly int maxRequestsPerMinute;
        readonly Logger logger;

        // Sliding window tracking:
        readonly ConcurrentQueue<DateTime> minuteWindowRequestTimestamps = new();
        readonly SemaphoreSlim rateLimitLock = new(1, 1);

        // Backoff tracking for 429 responses:
        DateTime? backoffUntil;
        readonly Lock backoffLock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DIGAPIMethodRateLimitTracker"/> class.
        /// </summary>
        /// <param name="methodName">The name of the DIG API method with which this tracker is associated.</param>
        /// <param name="maxRequestsPerMinute">The maximum number of requests allowed per minute.</param>
        /// <param name="logger">The logger instance to use for logging.</param>
        public DIGAPIMethodRateLimitTracker(string methodName, int maxRequestsPerMinute, Logger logger)
        {
            this.methodName = methodName;
            this.maxRequestsPerMinute = maxRequestsPerMinute;
            this.logger = logger;
        }

        /// <summary>
        /// Waits until a request can be made without exceeding rate limits for the subject method. This method should be called before each API request.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when a request can be made for the subject method.</returns>
        public async Task WaitForPermitAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Check if we're in a backoff period from a 429 response.
                TimeSpan? backoffRemaining = GetBackoffRemaining();
                if (backoffRemaining.HasValue && backoffRemaining.Value > TimeSpan.Zero)
                {
                    logger.Debug($"[{methodName}] rate limiter in backoff period. Waiting {backoffRemaining.Value.TotalSeconds:F1} seconds.");
                    await Task.Delay(backoffRemaining.Value, cancellationToken);
                    continue;
                }

                await rateLimitLock.WaitAsync(cancellationToken);
                try
                {
                    var now = DateTime.UtcNow;
                    CleanupOldTimestamps(now);

                    // Calculate delay needed to stay within limits.
                    var delay = CalculateRequiredDelay(now);

                    if (delay > TimeSpan.Zero)
                    {
                        logger.Debug($"[{methodName}] rate limiter delaying request by {delay.TotalSeconds:F1}s to stay within limits.");
                        await Task.Delay(delay, cancellationToken);
                        now = DateTime.UtcNow;
                        CleanupOldTimestamps(now);
                    }

                    // Record this request and return.
                    minuteWindowRequestTimestamps.Enqueue(now);
                    return;
                }
                finally
                {
                    rateLimitLock.Release();
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Records that a rate limit (429) response was received for the subject method, triggering a backoff period.
        /// </summary>
        /// <param name="retryAfterSeconds">The number of seconds to wait before retrying, from the Retry-After header.</param>
        public void RecordRateLimitResponse(int retryAfterSeconds)
        {
            lock (backoffLock)
            {
                // Add a small buffer to the retry-after time.
                var backoffDuration = TimeSpan.FromSeconds(retryAfterSeconds + 1);
                backoffUntil = DateTime.UtcNow.Add(backoffDuration);
                logger.Warn($"[{methodName}] rate limit (429) response received. Backing off for {backoffDuration.TotalSeconds:F0} seconds until {backoffUntil.Value:HH:mm:ss} UTC.");
            }
        }

        /// <summary>
        /// Gets current statistics about the rate limiter state for the subject method.
        /// </summary>
        /// <returns>Statistics about requests and limits for the subject method.</returns>
        public DIGAPIRateLimiterMethodStatistics GetStatistics()
        {
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);
            var timestamps = minuteWindowRequestTimestamps.ToArray();

            return new DIGAPIRateLimiterMethodStatistics
            {
                MethodName = methodName,
                IsRateLimited = true,
                RequestsInLastMinute = timestamps.Count(t => t >= oneMinuteAgo),
                MaxRequestsPerMinute = maxRequestsPerMinute,
                IsInBackoff = GetBackoffRemaining().HasValue,
                BackoffRemainingSeconds = GetBackoffRemaining()?.TotalSeconds ?? 0
            };
        }

        /// <summary>
        /// Gets the remaining backoff time, if any.
        /// </summary>
        /// <returns>The remaining backoff time, or null if not in backoff.</returns>
        TimeSpan? GetBackoffRemaining()
        {
            lock (backoffLock)
            {
                if (backoffUntil.HasValue && backoffUntil.Value > DateTime.UtcNow)
                {
                    return backoffUntil.Value - DateTime.UtcNow;
                }
                backoffUntil = null;
                return null;
            }
        }

        /// <summary>
        /// Cleans up old timestamps from the tracking window.
        /// </summary>
        /// <param name="now">The current UTC time.</param>
        void CleanupOldTimestamps(DateTime now)
        {
            var oneMinuteAgo = now.AddMinutes(-1);
            while (minuteWindowRequestTimestamps.TryPeek(out var oldest) && oldest < oneMinuteAgo)
            {
                minuteWindowRequestTimestamps.TryDequeue(out _);
            }
        }

        /// <summary>
        /// Calculates the required delay before the next request can be made, based on the per-minute rate limit.
        /// </summary>
        /// <param name="now">The current timestamp used to evaluate rate limits.</param>
        /// <returns>A <see cref="TimeSpan"/> representing the amount of time to wait before making the next request. Returns <see cref="TimeSpan.Zero"/> if no delay is required.</returns>
        TimeSpan CalculateRequiredDelay(DateTime now)
        {
            var oneMinuteAgo = now.AddMinutes(-1);
            var timestamps = minuteWindowRequestTimestamps.ToArray();
            var requestsInLastMinute = timestamps.Count(t => t >= oneMinuteAgo);

            if (requestsInLastMinute >= maxRequestsPerMinute)
            {
                var oldestInMinute = timestamps.Where(t => t >= oneMinuteAgo).Min();
                var delay = oldestInMinute.AddMinutes(1) - now;
                if (delay > TimeSpan.Zero)
                {
                    logger.Debug($"[{methodName}] per-minute limit reached ({requestsInLastMinute}/{maxRequestsPerMinute}). Need to wait {delay.TotalSeconds:F1}s.");
                    return delay;
                }
            }

            return TimeSpan.Zero;
        }
    }
}