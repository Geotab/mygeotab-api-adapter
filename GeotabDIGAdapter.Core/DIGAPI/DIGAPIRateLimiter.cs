using NLog;
using System.Collections.Concurrent;

namespace MyGeotabAPIAdapter.DIGAPI
{
    /// <summary>
    /// A rate limiter for DIG API calls. Based on DIG API <see href="https://docs.google.com/document/d/1aRPIDz7d49BEqEID_ZLjtrhwAuHacWO1WhTMUVUXwMI/edit?pli=1&tab=t.0#heading=h.7gsa4og8wue5">rate limits</see>:
    /// - Authentication endpoints (/authentication/authenticate, /authentication/refresh-token, /authentication/revoke-token): 5 requests per minute each
    /// - Note: /records and /invalidrecords endpoints are NOT subject to the same rate limiting.
    /// </summary>
    public class DIGAPIRateLimiter : IDIGAPIRateLimiter
    {
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        // Rate limit configuration from DIG API documentation (authentication endpoints only):
        const int DefaultRequestsPerMinute = 5;
        const double SafetyMarginPercent = 0.95;

        // Authentication endpoint method names that are subject to rate limiting.
        public const string MethodName_Authenticate = "Authenticate";
        public const string MethodName_RefreshToken = "RefreshToken";
        public const string MethodName_RevokeToken = "RevokeToken";

        // Non-rate-limited endpoint method names.
        public const string MethodName_PostRecords = "PostRecords";
        public const string MethodName_GetInvalidRecords = "GetInvalidRecords";

        static readonly HashSet<string> RateLimitedMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            MethodName_Authenticate,
            MethodName_RefreshToken,
            MethodName_RevokeToken
        };

        readonly int requestsPerMinute;
        readonly int effectiveRequestsPerMinute;

        // Per-method tracking using a concurrent dictionary (only for rate-limited methods).
        readonly ConcurrentDictionary<string, DIGAPIMethodRateLimitTracker> methodTrackers = new(StringComparer.OrdinalIgnoreCase);

        // Global lock for creating new trackers.
        readonly Lock trackerCreationLock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DIGAPIRateLimiter"/> class with default rate limits.
        /// </summary>
        public DIGAPIRateLimiter() : this(DefaultRequestsPerMinute)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DIGAPIRateLimiter"/> class with custom rate limits.
        /// </summary>
        /// <param name="requestsPerMinute">Maximum requests allowed per minute per authentication method.</param>
        public DIGAPIRateLimiter(int requestsPerMinute)
        {
            this.requestsPerMinute = requestsPerMinute > 0 ? requestsPerMinute : DefaultRequestsPerMinute;

            // Apply safety margin.
            effectiveRequestsPerMinute = Math.Max(1, (int)(this.requestsPerMinute * SafetyMarginPercent));

            logger.Info($"{nameof(DIGAPIRateLimiter)} initialized. Authentication endpoint limits: {this.requestsPerMinute}/minute (effective: {effectiveRequestsPerMinute}). Non-auth endpoints ({MethodName_PostRecords}, {MethodName_GetInvalidRecords}) are not rate-limited.");
        }

        /// <summary>
        /// Determines if the specified method is subject to rate limiting.
        /// </summary>
        /// <param name="methodName">The name of the API method.</param>
        /// <returns>True if the method is rate-limited; otherwise, false.</returns>
        static bool IsRateLimitedMethod(string methodName)
        {
            return RateLimitedMethods.Contains(methodName);
        }

        /// <summary>
        /// Gets or creates a rate limit tracker for the specified method.
        /// </summary>
        DIGAPIMethodRateLimitTracker GetOrCreateTracker(string methodName)
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

                tracker = new DIGAPIMethodRateLimitTracker(methodName, effectiveRequestsPerMinute, logger);
                methodTrackers[methodName] = tracker;
                logger.Debug($"Created rate limit tracker for method '{methodName}'.");
                return tracker;
            }
        }

        /// <inheritdoc/>
        public DIGAPIRateLimiterStatistics GetStatistics()
        {
            var methodStats = new Dictionary<string, DIGAPIRateLimiterMethodStatistics>();

            foreach (var kvp in methodTrackers)
            {
                methodStats[kvp.Key] = kvp.Value.GetStatistics();
            }

            return new DIGAPIRateLimiterStatistics
            {
                MaxRequestsPerMinute = requestsPerMinute,
                EffectiveRequestsPerMinute = effectiveRequestsPerMinute,
                RateLimitedMethods = [.. RateLimitedMethods],
                MethodStatistics = methodStats
            };
        }

        /// <inheritdoc/>
        public DIGAPIRateLimiterMethodStatistics GetStatistics(string methodName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            if (methodTrackers.TryGetValue(methodName, out var tracker))
            {
                return tracker.GetStatistics();
            }

            // Return empty statistics for unknown or non-rate-limited methods
            return new DIGAPIRateLimiterMethodStatistics
            {
                MethodName = methodName,
                RequestsInLastMinute = 0,
                MaxRequestsPerMinute = IsRateLimitedMethod(methodName) ? effectiveRequestsPerMinute : 0,
                IsRateLimited = IsRateLimitedMethod(methodName),
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

            // Record rate limit responses even for methods we don't normally track,
            // in case the API behavior changes or we receive an unexpected 429.
            var tracker = GetOrCreateTracker(methodName);
            tracker.RecordRateLimitResponse(retryAfterSeconds);
        }

        /// <inheritdoc/>
        public async Task WaitForPermitAsync(string methodName, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            // Only apply rate limiting to authentication endpoints
            if (!IsRateLimitedMethod(methodName))
            {
                logger.Trace($"[{methodName}] Not a rate-limited endpoint, proceeding immediately.");
                return;
            }

            var tracker = GetOrCreateTracker(methodName);
            await tracker.WaitForPermitAsync(cancellationToken);
        }
    }
}