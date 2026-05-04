using NLog;
using System.Collections.Concurrent;

namespace MyGeotabAPIAdapter.MyAdminAPI
{
    /// <summary>
    /// Internal class to track rate limits for a single API method.
    /// </summary>
    sealed class MyAdminAPIMethodRateLimitTracker
    {
        readonly string methodName;
        readonly int maxRequestsPerMinute;
        readonly int maxRequestsPerDay;
        readonly Logger logger;

        // Sliding window tracking - separate queues for minute and day windows:
        readonly ConcurrentQueue<DateTime> minuteWindowRequestTimestamps = new();
        readonly ConcurrentQueue<DateTime> dayWindowRequestTimestamps = new();
        readonly SemaphoreSlim rateLimitLock = new(1, 1);

        // Backoff tracking for 429 responses:
        DateTime? backoffUntil;
        readonly Lock backoffLock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MyAdminAPIMethodRateLimitTracker"/> class.
        /// </summary>
        /// <param name="methodName">The name of the MyAdmin method with which this tracker is associated.</param>
        /// <param name="maxRequestsPerMinute">The maximum number of requests allowed per minute.</param>
        /// <param name="maxRequestsPerDay">The maximum number of requests allowed per day.</param>
        /// <param name="logger">The logger instance to use for logging.</param>
        public MyAdminAPIMethodRateLimitTracker(string methodName, int maxRequestsPerMinute, int maxRequestsPerDay, Logger logger)
        {
            this.methodName = methodName;
            this.maxRequestsPerMinute = maxRequestsPerMinute;
            this.maxRequestsPerDay = maxRequestsPerDay;
            this.logger = logger;
        }

        /// <summary>
        /// Waits until a request can be made without exceeding rate limits for the subject method. This method should be called before each API request.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when a request can be made for the subject method.</returns>
        public async Task WaitForPermitAsync(CancellationToken cancellationToken)
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

                    // Record this request in both windows and return.
                    minuteWindowRequestTimestamps.Enqueue(now);
                    dayWindowRequestTimestamps.Enqueue(now);
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
        public MyAdminAPIRateLimiterMethodStatistics GetStatistics()
        {
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);
            var oneDayAgo = now.AddDays(-1);

            var minuteTimestamps = minuteWindowRequestTimestamps.ToArray();
            var dayTimestamps = dayWindowRequestTimestamps.ToArray();

            return new MyAdminAPIRateLimiterMethodStatistics
            {
                MethodName = methodName,
                RequestsInLastMinute = minuteTimestamps.Count(t => t >= oneMinuteAgo),
                RequestsInLastDay = dayTimestamps.Count(t => t >= oneDayAgo),
                MaxRequestsPerMinute = maxRequestsPerMinute,
                MaxRequestsPerDay = maxRequestsPerDay,
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
        /// Cleans up old timestamps from the tracking windows.
        /// </summary>
        /// <param name="now">The current UTC time.</param>
        void CleanupOldTimestamps(DateTime now)
        {
            var oneMinuteAgo = now.AddMinutes(-1);
            var oneDayAgo = now.AddDays(-1);

            // Clean minute window.
            while (minuteWindowRequestTimestamps.TryPeek(out var oldest) && oldest < oneMinuteAgo)
            {
                minuteWindowRequestTimestamps.TryDequeue(out _);
            }

            // Clean day window.
            while (dayWindowRequestTimestamps.TryPeek(out var oldest) && oldest < oneDayAgo)
            {
                dayWindowRequestTimestamps.TryDequeue(out _);
            }
        }

        /// <summary>
        /// Calculates the required delay before the next request can be made, based on per-minute and per-day rate limits.
        /// </summary>
        /// <remarks>This method enforces both per-minute and per-day request limits. If either limit has been reached, it returns the longer delay required to comply with both constraints. Callers should wait for the returned duration before issuing another request to avoid exceeding rate limits.</remarks>
        /// <param name="now">The current timestamp used to evaluate rate limits.</param>
        /// <returns>A <see cref="TimeSpan"/> representing the amount of time to wait before making the next request. Returns <see cref="TimeSpan.Zero"/> if no delay is required.</returns>
        TimeSpan CalculateRequiredDelay(DateTime now)
        {
            var delayForMinute = TimeSpan.Zero;
            var delayForDay = TimeSpan.Zero;

            // Check per-minute limit.
            var oneMinuteAgo = now.AddMinutes(-1);
            var minuteTimestamps = minuteWindowRequestTimestamps.ToArray();
            var requestsInLastMinute = minuteTimestamps.Count(t => t >= oneMinuteAgo);

            if (requestsInLastMinute >= maxRequestsPerMinute)
            {
                var oldestInMinute = minuteTimestamps.Where(t => t >= oneMinuteAgo).Min();
                var calculatedDelay = oldestInMinute.AddMinutes(1) - now;
                delayForMinute = calculatedDelay > TimeSpan.Zero ? calculatedDelay : TimeSpan.Zero;
                if (delayForMinute > TimeSpan.Zero)
                {
                    logger.Debug($"[{methodName}] per-minute limit reached ({requestsInLastMinute}/{maxRequestsPerMinute}). Need to wait {delayForMinute.TotalSeconds:F1}s.");
                }
            }

            // Check per-day limit.
            var oneDayAgo = now.AddDays(-1);
            var dayTimestamps = dayWindowRequestTimestamps.ToArray();
            var requestsInLastDay = dayTimestamps.Count(t => t >= oneDayAgo);

            if (requestsInLastDay >= maxRequestsPerDay)
            {
                var oldestInDay = dayTimestamps.Where(t => t >= oneDayAgo).Min();
                var calculatedDelay = oldestInDay.AddDays(1) - now;
                delayForDay = calculatedDelay > TimeSpan.Zero ? calculatedDelay : TimeSpan.Zero;
                if (delayForDay > TimeSpan.Zero)
                {
                    logger.Warn($"[{methodName}] per-day limit reached ({requestsInLastDay}/{maxRequestsPerDay}). Need to wait {delayForDay.TotalHours:F1} hours.");
                }
            }

            // Return the longer of the two delays (both are guaranteed >= Zero)
            return delayForMinute > delayForDay ? delayForMinute : delayForDay;
        }
    }
}
