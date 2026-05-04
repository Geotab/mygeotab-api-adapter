using MyGeotabAPIAdapter.Logging;
using NLog;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;

namespace MyGeotabAPIAdapter.MyAdminAPI
{
    /// <summary>
    /// A helper class for enhancing application resiliency through the provision of policies that can provide for things such as setting timeouts and retries when executing MyAdmin API calls.
    /// </summary>
    public static class MyAdminAPIResilienceHelper
    {
        static string DefaultErrorMessagePrefix { get => $"{nameof(MyAdminAPIResilienceHelper)} process caught an exception"; }

        // Known MyAdmin API connectivity-related exception detail constants.
        public const string MyAdminConnectionExceptionMessage_HttpRequestException = "HttpRequestException";
        public const string MyAdminConnectionExceptionMessage_ServiceUnavailable = "Service temporarily unavailable";
        public const string MyAdminTimeoutExceptionMessage_OperationTimedOut = "The operation has timed out.";

        // Session expiry-related exception message constant.
        public const string MyAdminSessionExpiredExceptionMessage = "SessionExpiredException";

        // Rate limit-related exception message constants.
        public const string MyAdminRateLimitExceptionMessage_TooManyRequests = "429";
        public const string MyAdminRateLimitExceptionMessage_RateLimitExceeded = "rate limit";

        // MyAdminException message constant.
        public const string MyAdminApiErrorExceptionMessage = "MyAdminException";

        // Non-retryable HTTP status/error message constants (fail fast per MyAdmin Exceptions Practices).
        public const string MyAdminNonRetryableMessage_BadRequest = "Bad Request";
        public const string MyAdminNonRetryableMessage_Unauthorized = "Unauthorized";
        public const string MyAdminNonRetryableMessage_Forbidden = "Forbidden";
        public const string MyAdminNonRetryableMessage_StatusCode400 = "400";
        public const string MyAdminNonRetryableMessage_StatusCode401 = "401";
        public const string MyAdminNonRetryableMessage_StatusCode403 = "403";
        // Non-retryable MyAdminException business error code constants.
        public const string MyAdminNonRetryableMessage_IllegalFieldValue = "illegal_field_value";
        public const string MyAdminNonRetryableMessage_DuplicateEntityError = "duplicate_entity_error";
        // Non-retryable whitelisted exception type message constants (per MYA WHITELISTED_TYPES).
        public const string MyAdminNonRetryableMessage_SecurityException = "SecurityException";
        public const string MyAdminNonRetryableMessage_DuplicateException = "DuplicateException";
        public const string MyAdminNonRetryableMessage_InvalidDataException = "InvalidDataException";
        public const string MyAdminNonRetryableMessage_UserAuthenticationException = "UserAuthenticationException";

        /// <summary>
        /// The maximum allowed number of retries for each MyAdmin API call.
        /// </summary>
        public static int MaxRetries { get => 1000000; } // Essentially unlimited.

        /// <summary>
        /// The maximum number of re-authentication attempts when session expires.
        /// </summary>
        public static int MaxReauthenticationRetries { get => 1000000; }

        /// <summary>
        /// The maximum number of retries for rate limit (429) responses.
        /// </summary>
        public static int MaxRateLimitRetries { get => 1000000; }

        /// <summary>
        /// The maximum allowed timeout in seconds for each MyAdmin API call.
        /// </summary>
        public static int MaxTimeoutSeconds { get => 3600; } // 1 hour

        /// <summary>
        /// The minimum allowed timeout in seconds for each MyAdmin API call.
        /// </summary>
        public static int MinTimeoutSeconds { get => 10; }

        // Polly context variable names:
        public static string PollyContextKeyIsExceptionTimeoutRelated { get => "IsExceptionTimeoutRelated"; }
        public static string PollyContextKeyRetryAttemptNumber { get => "RetryAttemptNumber"; }
        public static string PollyContextKeyRetryAttemptTimeoutTimeSpan { get => "RetryAttemptTimeoutTimeSpan"; }
        public static string PollyContextKeyMethodName { get => "MethodName"; }

        /// <summary>
        /// Creates an <see cref="AsyncPolicyWrap"/> that includes rate limiting, session expiry handling, retry, and timeout policies for MyAdmin API calls.
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy.</typeparam>
        /// <param name="configuredTimeoutSeconds">The initial timeout in seconds for the first retry attempt.</param>
        /// <param name="exceptionHelper">The <see cref="MyAdminExceptionHelper"/> to be used for logging.</param>
        /// <param name="logger">The logger to be used for logging.</param>
        /// <param name="reauthenticateAsync">The async function to call for re-authentication when session expires.</param>
        /// <param name="rateLimiter">The rate limiter to use for proactive rate limiting.</param>
        /// <returns>An <see cref="AsyncPolicyWrap"/> with rate limiting and session expiry handling.</returns>
        public static AsyncPolicyWrap CreateAsyncPolicyWrapForMyAdminAPICallsWithReauthentication<TException>(
            int configuredTimeoutSeconds,
            IMyAdminExceptionHelper exceptionHelper,
            Logger logger,
            Func<Task> reauthenticateAsync,
            IMyAdminAPIRateLimiter? rateLimiter = null) where TException : Exception
        {
            var asyncRateLimitRetryPolicy = CreateAsyncRetryPolicyForRateLimiting<TException>(exceptionHelper, logger, rateLimiter);
            var asyncSessionExpiryRetryPolicy = CreateAsyncRetryPolicyForSessionExpiry<TException>(exceptionHelper, logger, reauthenticateAsync);
            var asyncRetryPolicyForTimedOut = CreateAsyncRetryPolicyForMyAdminAPICallsTimedOut<TException>(exceptionHelper, logger);
            var asyncRetryPolicyForNonTimedOut = CreateAsyncRetryPolicyForMyAdminAPICallsNotTimedOut<TException>(exceptionHelper, logger);
            var asyncTimeoutPolicy = CreateAsyncTimeoutPolicyForMyAdminAPICalls(configuredTimeoutSeconds);

            // Rate limit policy is outermost so it can handle 429s and wait before any retry.
            return Policy.WrapAsync(asyncRateLimitRetryPolicy, asyncSessionExpiryRetryPolicy, asyncRetryPolicyForTimedOut, asyncRetryPolicyForNonTimedOut, asyncTimeoutPolicy);
        }

        /// <summary>
        /// Creates a Polly <see cref="Context"/> with the method name set for rate limiting purposes.
        /// </summary>
        /// <param name="methodName">The name of the API method being called.</param>
        /// <returns>A <see cref="Context"/> with the method name set.</returns>
        public static Context CreateContextWithMethodName(string methodName)
        {
            var context = new Context();
            context[PollyContextKeyMethodName] = methodName;
            return context;
        }

        /// <summary>
        /// Gets the method name from a Polly context, or returns a default value if not set.
        /// </summary>
        /// <param name="context">The Polly context.</param>
        /// <returns>The method name or "Unknown" if not set.</returns>
        static string GetMethodNameFromContext(Context context)
        {
            if (context != null && context.TryGetValue(PollyContextKeyMethodName, out var methodName) && methodName is string name)
            {
                return name;
            }
            return "Unknown";
        }

        /// <summary>
        /// Creates an <see cref="AsyncRetryPolicy"/> for handling rate limit (429) responses. Uses exponential backoff with the Retry-After header when available.
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy.</typeparam>
        /// <param name="exceptionHelper">The <see cref="MyAdminExceptionHelper"/> to be used for logging details.</param>
        /// <param name="logger">The logger to be used for logging any exception and retry-related information.</param>
        /// <param name="rateLimiter">The rate limiter to notify of 429 responses.</param>
        /// <returns></returns>
        static AsyncRetryPolicy CreateAsyncRetryPolicyForRateLimiting<TException>(
            IMyAdminExceptionHelper exceptionHelper,
            Logger logger,
            IMyAdminAPIRateLimiter? rateLimiter) where TException : Exception
        {
            return Policy
                .Handle<TException>(exception =>
                    exception.Message.Contains(MyAdminRateLimitExceptionMessage_TooManyRequests, StringComparison.OrdinalIgnoreCase) ||
                    exception.Message.Contains(MyAdminRateLimitExceptionMessage_RateLimitExceeded, StringComparison.OrdinalIgnoreCase))
                .WaitAndRetryAsync(
                    retryCount: MaxRateLimitRetries,
                    sleepDurationProvider: (retryAttempt, exception, context) =>
                    {
                        // Try to parse Retry-After from exception message or use exponential backoff
                        int retryAfterSeconds = ParseRetryAfterFromException(exception) ?? GetExponentialBackoffSeconds(retryAttempt);

                        // Notify the rate limiter of the 429 response (with method name from context)
                        if (rateLimiter != null)
                        {
                            var methodName = GetMethodNameFromContext(context);
                            rateLimiter.RecordRateLimitResponse(methodName, retryAfterSeconds);
                        }

                        return TimeSpan.FromSeconds(retryAfterSeconds);
                    },
                    onRetryAsync: async (exception, sleepDuration, attemptNumber, context) =>
                    {
                        exceptionHelper.LogException(exception, NLogLogLevelName.Warn, DefaultErrorMessagePrefix);
                        var methodName = GetMethodNameFromContext(context);
                        logger.Warn($"[{methodName}] {nameof(MyAdminAPIResilienceHelper)} caught rate limit (429) exception: '{exception.Message}'. Waiting {sleepDuration.TotalSeconds:F0} seconds before retry (attempt {attemptNumber} of {MaxRateLimitRetries}).");

                        // Log rate limiter statistics if available
                        if (rateLimiter != null)
                        {
                            var stats = rateLimiter.GetStatistics(methodName);
                            logger.Debug($"[{methodName}] Rate limiter stats - Requests in last minute: {stats.RequestsInLastMinute}/{stats.MaxRequestsPerMinute}, " +
                                        $"Requests in last day: {stats.RequestsInLastDay}/{stats.MaxRequestsPerDay}");
                        }

                        await Task.CompletedTask;
                    }
                );
        }

        /// <summary>
        /// Attempts to parse a Retry-After value from an exception message.
        /// </summary>
        /// <param name="exception">The exception to parse.</param>
        /// <returns>The retry-after seconds if found, otherwise null.</returns>
        static int? ParseRetryAfterFromException(Exception exception)
        {
            // Look for patterns like "Retry-After: 60" or "retry after 60 seconds"
            var message = exception.Message;

            // Try to find "Retry-After:" header pattern
            var retryAfterIndex = message.IndexOf("Retry-After:", StringComparison.OrdinalIgnoreCase);
            if (retryAfterIndex >= 0)
            {
                var valueStart = retryAfterIndex + "Retry-After:".Length;
                var remaining = message[valueStart..].TrimStart();
                var numberStr = new string(remaining.TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(numberStr, out var seconds))
                {
                    return seconds;
                }
            }

            return null;
        }

        /// <summary>
        /// Calculates exponential backoff seconds for rate limit retries.
        /// </summary>
        /// <param name="retryAttempt">The current retry attempt number.</param>
        /// <returns>The number of seconds to wait.</returns>
        static int GetExponentialBackoffSeconds(int retryAttempt)
        {
            // Exponential backoff: 5, 10, 20, 40, 60, 60, 60...
            const int BaseDelaySeconds = 5;
            const int MaxDelaySeconds = 60;

            var delay = BaseDelaySeconds * (int)Math.Pow(2, retryAttempt - 1);
            return Math.Min(delay, MaxDelaySeconds);
        }

        /// <summary>
        /// Determines whether the specified exception represents a non-retryable error that should fail fast.
        /// Per the MyAdmin Exceptions Practices document, 400, 401, and 403 errors should not be retried
        /// unless the underlying input or credentials are changed. Programming errors (ArgumentException,
        /// InvalidOperationException) are also non-retryable.
        /// </summary>
        /// <param name="exception">The exception to evaluate.</param>
        /// <returns><c>true</c> if the exception is non-retryable; otherwise, <c>false</c>.</returns>
        public static bool IsNonRetryableException(Exception exception)
        {
            // Programming errors should never be retried.
            if (exception is ArgumentException || exception is InvalidOperationException)
            {
                return true;
            }

            // Check for non-retryable HTTP status codes, business error patterns, and whitelisted
            // exception types in the exception message. Per MYA WHITELISTED_TYPES guidance,
            // SecurityException, ArgumentException, DuplicateException, InvalidDataException,
            // and UserAuthenticationException should not be retried.
            var message = exception.Message;
            if (message.Contains(MyAdminNonRetryableMessage_BadRequest, StringComparison.OrdinalIgnoreCase)
                || message.Contains(MyAdminNonRetryableMessage_Unauthorized, StringComparison.OrdinalIgnoreCase)
                || message.Contains(MyAdminNonRetryableMessage_Forbidden, StringComparison.OrdinalIgnoreCase)
                || message.Contains(MyAdminNonRetryableMessage_StatusCode400)
                || message.Contains(MyAdminNonRetryableMessage_StatusCode401)
                || message.Contains(MyAdminNonRetryableMessage_StatusCode403)
                || message.Contains(MyAdminNonRetryableMessage_IllegalFieldValue, StringComparison.OrdinalIgnoreCase)
                || message.Contains(MyAdminNonRetryableMessage_DuplicateEntityError, StringComparison.OrdinalIgnoreCase)
                || message.Contains(MyAdminNonRetryableMessage_SecurityException)
                || message.Contains(MyAdminNonRetryableMessage_DuplicateException)
                || message.Contains(MyAdminNonRetryableMessage_InvalidDataException)
                || message.Contains(MyAdminNonRetryableMessage_UserAuthenticationException))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates an <see cref="AsyncRetryPolicy"/> for exceptions that are NOT timeout-related.
        /// A staggered delay is implemented between successive retry attempts.
        /// Non-retryable exceptions (400, 401, 403, business errors, programming errors) are excluded
        /// per the MyAdmin Exceptions Practices "Fail Fast" guidance.
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy.</typeparam>
        /// <param name="exceptionHelper">The <see cref="MyAdminExceptionHelper"/> to be used for logging details.</param>
        /// <param name="logger">The logger to be used for logging any exception and retry-related information.</param>
        /// <returns></returns>
        static AsyncRetryPolicy CreateAsyncRetryPolicyForMyAdminAPICallsNotTimedOut<TException>(IMyAdminExceptionHelper exceptionHelper, Logger logger) where TException : Exception
        {
            return Policy
                .Handle<TException>(exception =>
                    exception.Message.Contains(MyAdminTimeoutExceptionMessage_OperationTimedOut) == false
                    && exception.Message.Contains(MyAdminConnectionExceptionMessage_ServiceUnavailable) == false
                    && exception.Message.Contains(MyAdminSessionExpiredExceptionMessage) == false
                    && exception.Message.Contains(MyAdminApiErrorExceptionMessage) == false
                    && !exception.Message.Contains(MyAdminRateLimitExceptionMessage_TooManyRequests, StringComparison.OrdinalIgnoreCase)
                    && !exception.Message.Contains(MyAdminRateLimitExceptionMessage_RateLimitExceeded, StringComparison.OrdinalIgnoreCase)
                    && !IsNonRetryableException(exception))
                .WaitAndRetryAsync(
                    retryCount: MaxRetries,
                    sleepDurationProvider: (retryAttempt) =>
                    {
                        int sleepDurationSeconds;
                        if (retryAttempt < 2)
                        {
                            sleepDurationSeconds = 1;
                        }
                        else if (retryAttempt < 11)
                        {
                            sleepDurationSeconds = 10;
                        }
                        else if (retryAttempt < 21)
                        {
                            sleepDurationSeconds = 30;
                        }
                        else if (retryAttempt < 31)
                        {
                            sleepDurationSeconds = 60;
                        }
                        else if (retryAttempt < 41)
                        {
                            sleepDurationSeconds = 300;
                        }
                        else
                        {
                            sleepDurationSeconds = 600;
                        }
                        return TimeSpan.FromSeconds(sleepDurationSeconds);
                    },
                    onRetry: (exception, sleepDuration, attemptNumber, context) =>
                    {
                        context[PollyContextKeyIsExceptionTimeoutRelated] = false;
                        exceptionHelper.LogException(exception, NLogLogLevelName.Warn, DefaultErrorMessagePrefix);
                        var callTimeout = context.TryGetValue(PollyContextKeyRetryAttemptTimeoutTimeSpan, out var timeout) ? (TimeSpan)timeout : TimeSpan.Zero;
                        context[PollyContextKeyRetryAttemptNumber] = attemptNumber;
                        var methodName = GetMethodNameFromContext(context);
                        logger.Warn($"[{methodName}] {nameof(MyAdminAPIResilienceHelper)} caught exception of type '{exception.GetType().Name}' with message '{exception.Message}'. MyAdmin API call timeout was {callTimeout}. Retrying in {sleepDuration} (attempt {attemptNumber} of {MaxRetries}).");
                    }
                );
        }

        /// <summary>
        /// Creates an <see cref="AsyncRetryPolicy"/> for exceptions that ARE timeout-related.
        /// There is a one second delay before each retry attempt.
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy.</typeparam>
        /// <param name="exceptionHelper">The <see cref="MyAdminExceptionHelper"/> to be used for logging details.</param>
        /// <param name="logger">The logger to be used for logging any exception and retry-related information.</param>
        /// <returns></returns>
        static AsyncRetryPolicy CreateAsyncRetryPolicyForMyAdminAPICallsTimedOut<TException>(IMyAdminExceptionHelper exceptionHelper, Logger logger) where TException : Exception
        {
            return Policy
                .Handle<TException>(exception =>
                    exception.Message.Contains(MyAdminTimeoutExceptionMessage_OperationTimedOut) == true)
                .Or<TException>(exception =>
                    exception.Message.Contains(MyAdminConnectionExceptionMessage_ServiceUnavailable))
                .WaitAndRetryAsync(
                    retryCount: MaxRetries,
                    sleepDurationProvider: (retryAttempt) => TimeSpan.FromSeconds(1),
                    onRetry: (exception, sleepDuration, attemptNumber, context) =>
                    {
                        context[PollyContextKeyIsExceptionTimeoutRelated] = true;
                        exceptionHelper.LogException(exception, NLogLogLevelName.Warn, DefaultErrorMessagePrefix);
                        var callTimeout = context.TryGetValue(PollyContextKeyRetryAttemptTimeoutTimeSpan, out var timeout) ? (TimeSpan)timeout : TimeSpan.Zero;
                        context[PollyContextKeyRetryAttemptNumber] = attemptNumber;
                        var methodName = GetMethodNameFromContext(context);
                        logger.Warn($"[{methodName}] {nameof(MyAdminAPIResilienceHelper)} caught exception of type '{exception.GetType().Name}' with message '{exception.Message}'. MyAdmin API call timeout was {callTimeout}. Retrying in {sleepDuration} (attempt {attemptNumber} of {MaxRetries}).");
                    }
                );
        }

        /// <summary>
        /// Creates an <see cref="AsyncRetryPolicy"/> that handles MyAdmin SessionExpiredException by re-authenticating and retrying.
        /// There is a one second delay before each retry attempt.
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy.</typeparam>
        /// <param name="exceptionHelper">The <see cref="MyAdminExceptionHelper"/> to be used for logging details.</param>
        /// <param name="logger">The logger to be used for logging any exception and retry-related information.</param>
        /// <param name="reauthenticateAsync">The async function to call for re-authentication when session expires.</param>
        /// <returns></returns>
        static AsyncRetryPolicy CreateAsyncRetryPolicyForSessionExpiry<TException>(IMyAdminExceptionHelper exceptionHelper, Logger logger, Func<Task> reauthenticateAsync) where TException : Exception
        {
            return Policy
                .Handle<TException>(exception =>
                    exception.Message.Contains(MyAdminSessionExpiredExceptionMessage) == true)
                .WaitAndRetryAsync(
                    retryCount: MaxReauthenticationRetries,
                    sleepDurationProvider: (retryAttempt) => TimeSpan.FromSeconds(1),
                    onRetryAsync: async (exception, sleepDuration, attemptNumber, context) =>
                    {
                        exceptionHelper.LogException(exception, NLogLogLevelName.Warn, DefaultErrorMessagePrefix);
                        var methodName = GetMethodNameFromContext(context);
                        logger.Warn($"[{methodName}] {nameof(MyAdminAPIResilienceHelper)} detected MyAdmin session expiry: '{exception.Message}'. Attempting re-authentication (attempt {attemptNumber} of {MaxReauthenticationRetries}).");

                        try
                        {
                            await reauthenticateAsync();
                            logger.Info("MyAdmin re-authentication successful. Retrying original operation.");
                        }
                        catch (Exception reauthException)
                        {
                            logger.Error($"Re-authentication failed: {reauthException.Message}");
                            throw;
                        }
                    }
                );
        }

        /// <summary>
        /// Creates an <see cref="AsyncTimeoutPolicy"/> with dynamic timeout calculation based on retry context.
        /// </summary>
        /// <param name="initialTimeoutSeconds">The initial timeout in seconds to be applied to the first attempt.</param>
        static AsyncTimeoutPolicy CreateAsyncTimeoutPolicyForMyAdminAPICalls(int initialTimeoutSeconds)
        {
            initialTimeoutSeconds = GetValidatedInitialTimeoutSeconds(initialTimeoutSeconds);

            return Policy.TimeoutAsync(context =>
            {
                int attemptNumber = 0;
                if (context.ContainsKey(PollyContextKeyRetryAttemptNumber))
                {
                    attemptNumber = (int)context[PollyContextKeyRetryAttemptNumber];
                }

                bool isExceptionTimeoutRelated = false;
                if (context.ContainsKey(PollyContextKeyIsExceptionTimeoutRelated))
                {
                    isExceptionTimeoutRelated = (bool)context[PollyContextKeyIsExceptionTimeoutRelated];
                }

                int timeoutSeconds;
                if (isExceptionTimeoutRelated)
                {
                    timeoutSeconds = GetRetrySecondsForCurrentAttemptTimedOut(initialTimeoutSeconds, attemptNumber);
                }
                else
                {
                    timeoutSeconds = GetRetrySecondsForCurrentAttemptNotTimedOut(attemptNumber);
                }

                var retryAttemptTimeoutTimeSpan = TimeSpan.FromSeconds(timeoutSeconds);
                context[PollyContextKeyRetryAttemptTimeoutTimeSpan] = retryAttemptTimeoutTimeSpan;
                return retryAttemptTimeoutTimeSpan;
            });
        }

        /// <summary>
        /// Creates an <see cref="AsyncPolicyWrap"/> that includes retry and timeout policies for MyAdmin API calls.
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy.</typeparam>
        /// <param name="configuredTimeoutSeconds">The initial timeout in seconds for the first retry attempt.</param>
        /// <param name="exceptionHelper">The <see cref="MyAdminExceptionHelper"/> to be used for logging.</param>
        /// <param name="logger">The logger to be used for logging.</param>
        /// <returns></returns>
        public static AsyncPolicyWrap CreateAsyncPolicyWrapForMyAdminAPICalls<TException>(int configuredTimeoutSeconds, IMyAdminExceptionHelper exceptionHelper, Logger logger) where TException : Exception
        {
            var asyncRetryPolicyForTimedOut = CreateAsyncRetryPolicyForMyAdminAPICallsTimedOut<TException>(exceptionHelper, logger);
            var asyncRetryPolicyForNonTimedOut = CreateAsyncRetryPolicyForMyAdminAPICallsNotTimedOut<TException>(exceptionHelper, logger);
            var asyncTimeoutPolicy = CreateAsyncTimeoutPolicyForMyAdminAPICalls(configuredTimeoutSeconds);
            return Policy.WrapAsync(asyncRetryPolicyForTimedOut, asyncRetryPolicyForNonTimedOut, asyncTimeoutPolicy);
        }

        /// <summary>
        /// Returns a calculated timeout value for non-timeout-related exceptions.
        /// </summary>
        static int GetRetrySecondsForCurrentAttemptNotTimedOut(int attemptNumber)
        {
            const int TimeoutSecondsForAttemptRange1 = 30;
            const int TimeoutSecondsForAttemptRange2 = 300;
            int TimeoutSecondsForAttemptRange3 = MaxTimeoutSeconds;

            if (attemptNumber < 1)
            {
                attemptNumber = 1;
            }

            if (attemptNumber < 4)
            {
                return TimeoutSecondsForAttemptRange1;
            }
            if (attemptNumber < 7)
            {
                return TimeoutSecondsForAttemptRange2;
            }
            return TimeoutSecondsForAttemptRange3;
        }

        /// <summary>
        /// Returns a calculated timeout value for timeout-related exceptions.
        /// </summary>
        static int GetRetrySecondsForCurrentAttemptTimedOut(int initialTimeoutSeconds, int attemptNumber)
        {
            if (attemptNumber < 1)
            {
                attemptNumber = 1;
            }

            if (initialTimeoutSeconds < MinTimeoutSeconds)
            {
                initialTimeoutSeconds = MinTimeoutSeconds;
            }
            if (initialTimeoutSeconds >= MaxTimeoutSeconds)
            {
                return MaxTimeoutSeconds;
            }
            int retrySecondsForCurrentAttempt = initialTimeoutSeconds * attemptNumber;
            if (retrySecondsForCurrentAttempt > MaxTimeoutSeconds)
            {
                return MaxTimeoutSeconds;
            }
            return retrySecondsForCurrentAttempt;
        }

        /// <summary>
        /// Ensures that the timeout value falls within the allowed range.
        /// </summary>
        static int GetValidatedInitialTimeoutSeconds(int initialTimeoutSeconds)
        {
            if (initialTimeoutSeconds < MinTimeoutSeconds)
            {
                return MinTimeoutSeconds;
            }
            if (initialTimeoutSeconds > MaxTimeoutSeconds)
            {
                return MaxTimeoutSeconds;
            }
            return initialTimeoutSeconds;
        }
    }
}