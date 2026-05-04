using MyGeotabAPIAdapter.Logging;
using NLog;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;

namespace MyGeotabAPIAdapter.DIGAPI
{
    /// <summary>
    /// A helper class for enhancing application resiliency through the provision of policies that can provide for things such as setting timeouts and retries when executing DIG API calls.
    /// </summary>
    public static class DIGAPIResilienceHelper
    {
        static string DefaultErrorMessagePrefix { get => $"{nameof(DIGAPIResilienceHelper)} process caught an exception"; }

        // Known DIG API connectivity-related exception detail constants.
        public const string DIGConnectionExceptionMessage_HttpRequestException = "HttpRequestException";
        public const string DIGConnectionExceptionMessage_ServiceUnavailable = "Service is unavailable";
        public const string DIGTimeoutExceptionMessage_OperationTimedOut = "The operation has timed out.";

        // Token expiry-related exception message constants.
        // 401 = Unauthorized (token expired), 403 = Forbidden (token already refreshed/not found)
        public const string DIGTokenExpiredExceptionMessage = "401";
        public const string DIGTokenInvalidExceptionMessage = "403";

        // Rate limit-related exception message constants.
        public const string DIGRateLimitExceptionMessage_TooManyRequests = "429";
        public const string DIGRateLimitExceptionMessage_RateLimitExceeded = "rate limit";

        // Non-retryable client error exception message constants (should not be retried).
        public const string DIGBadRequestExceptionMessage = "400";
        public const string DIGPayloadTooLargeExceptionMessage = "413";

        // Non-retryable deserialization error type name (should not be retried — response is malformed).
        static readonly Type JsonExceptionType = typeof(System.Text.Json.JsonException);

        /// <summary>
        /// The maximum allowed number of retries for each DIG API call.
        /// </summary>
        public static int MaxRetries { get => 1000000; } // Essentially unlimited.

        /// <summary>
        /// The maximum number of re-authentication attempts when token expires.
        /// </summary>
        public static int MaxReauthenticationRetries { get => 1000000; }

        /// <summary>
        /// The maximum number of retries for rate limit (429) responses.
        /// </summary>
        public static int MaxRateLimitRetries { get => 1000000; }

        /// <summary>
        /// The maximum allowed timeout in seconds for each DIG API call.
        /// </summary>
        public static int MaxTimeoutSeconds { get => 3600; } // 1 hour

        /// <summary>
        /// The minimum allowed timeout in seconds for each DIG API call.
        /// </summary>
        public static int MinTimeoutSeconds { get => 10; }

        // Polly context variable names:
        public static string PollyContextKeyIsExceptionTimeoutRelated { get => "IsExceptionTimeoutRelated"; }
        public static string PollyContextKeyRetryAttemptNumber { get => "RetryAttemptNumber"; }
        public static string PollyContextKeyRetryAttemptTimeoutTimeSpan { get => "RetryAttemptTimeoutTimeSpan"; }
        public static string PollyContextKeyMethodName { get => "MethodName"; }

        /// <summary>
        /// Creates an <see cref="AsyncPolicyWrap"/> that includes rate limiting, token expiry handling, retry, and timeout policies for DIG API calls.
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy.</typeparam>
        /// <param name="configuredTimeoutSeconds">The initial timeout in seconds for the first retry attempt.</param>
        /// <param name="exceptionHelper">The <see cref="DIGExceptionHelper"/> to be used for logging.</param>
        /// <param name="logger">The logger to be used for logging.</param>
        /// <param name="reauthenticateAsync">The async function to call for re-authentication when token expires.</param>
        /// <param name="rateLimiter">The rate limiter to use for proactive rate limiting.</param>
        /// <returns>An <see cref="AsyncPolicyWrap"/> with rate limiting and token expiry handling.</returns>
        public static AsyncPolicyWrap CreateAsyncPolicyWrapForDIGAPICallsWithReauthentication<TException>(
            int configuredTimeoutSeconds,
            DIGExceptionHelper exceptionHelper,
            Logger logger,
            Func<Task> reauthenticateAsync,
            IDIGAPIRateLimiter? rateLimiter = null) where TException : Exception
        {
            var asyncRateLimitRetryPolicy = CreateAsyncRetryPolicyForRateLimiting<TException>(exceptionHelper, logger, rateLimiter);
            var asyncTokenExpiryRetryPolicy = CreateAsyncRetryPolicyForTokenExpiry<TException>(exceptionHelper, logger, reauthenticateAsync);
            var asyncRetryPolicyForTimedOut = CreateAsyncRetryPolicyForDIGAPICallsTimedOut<TException>(exceptionHelper, logger);
            var asyncRetryPolicyForNonTimedOut = CreateAsyncRetryPolicyForDIGAPICallsNotTimedOut<TException>(exceptionHelper, logger);
            var asyncTimeoutPolicy = CreateAsyncTimeoutPolicyForDIGAPICalls(configuredTimeoutSeconds);

            // Rate limit policy is outermost so it can handle 429s and wait before any retry.
            return Policy.WrapAsync(asyncRateLimitRetryPolicy, asyncTokenExpiryRetryPolicy, asyncRetryPolicyForTimedOut, asyncRetryPolicyForNonTimedOut, asyncTimeoutPolicy);
        }

        /// <summary>
        /// Creates an <see cref="AsyncPolicyWrap"/> that includes retry and timeout policies for DIG API calls (without re-authentication).
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy.</typeparam>
        /// <param name="configuredTimeoutSeconds">The initial timeout in seconds for the first retry attempt.</param>
        /// <param name="exceptionHelper">The <see cref="DIGExceptionHelper"/> to be used for logging.</param>
        /// <param name="logger">The logger to be used for logging.</param>
        /// <returns></returns>
        public static AsyncPolicyWrap CreateAsyncPolicyWrapForDIGAPICalls<TException>(int configuredTimeoutSeconds, DIGExceptionHelper exceptionHelper, Logger logger) where TException : Exception
        {
            var asyncRetryPolicyForTimedOut = CreateAsyncRetryPolicyForDIGAPICallsTimedOut<TException>(exceptionHelper, logger);
            var asyncRetryPolicyForNonTimedOut = CreateAsyncRetryPolicyForDIGAPICallsNotTimedOut<TException>(exceptionHelper, logger);
            var asyncTimeoutPolicy = CreateAsyncTimeoutPolicyForDIGAPICalls(configuredTimeoutSeconds);
            return Policy.WrapAsync(asyncRetryPolicyForTimedOut, asyncRetryPolicyForNonTimedOut, asyncTimeoutPolicy);
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
        /// Creates an <see cref="AsyncRetryPolicy"/> for handling rate limit (429) responses.
        /// </summary>
        static AsyncRetryPolicy CreateAsyncRetryPolicyForRateLimiting<TException>(
            DIGExceptionHelper exceptionHelper,
            Logger logger,
            IDIGAPIRateLimiter? rateLimiter) where TException : Exception
        {
            return Policy
                .Handle<TException>(exception =>
                    exception.Message.Contains(DIGRateLimitExceptionMessage_TooManyRequests, StringComparison.OrdinalIgnoreCase) ||
                    exception.Message.Contains(DIGRateLimitExceptionMessage_RateLimitExceeded, StringComparison.OrdinalIgnoreCase))
                .WaitAndRetryAsync(
                    retryCount: MaxRateLimitRetries,
                    sleepDurationProvider: (retryAttempt, exception, context) =>
                    {
                        int retryAfterSeconds = ParseRetryAfterFromException(exception) ?? GetExponentialBackoffSeconds(retryAttempt);

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
                        logger.Warn($"[{methodName}] {nameof(DIGAPIResilienceHelper)} caught rate limit (429) exception: '{exception.Message}'. Waiting {sleepDuration.TotalSeconds:F0} seconds before retry (attempt {attemptNumber} of {MaxRateLimitRetries}).");
                        await Task.CompletedTask;
                    }
                );
        }

        /// <summary>
        /// Attempts to parse a Retry-After value from an exception message.
        /// </summary>
        static int? ParseRetryAfterFromException(Exception exception)
        {
            var message = exception.Message;
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
        static int GetExponentialBackoffSeconds(int retryAttempt)
        {
            const int BaseDelaySeconds = 10;
            const int MaxDelaySeconds = 120;

            var delay = BaseDelaySeconds * (int)Math.Pow(2, retryAttempt - 1);
            return Math.Min(delay, MaxDelaySeconds);
        }

        /// <summary>
        /// Creates an <see cref="AsyncRetryPolicy"/> for exceptions that are NOT timeout-related.
        /// </summary>
        static AsyncRetryPolicy CreateAsyncRetryPolicyForDIGAPICallsNotTimedOut<TException>(DIGExceptionHelper exceptionHelper, Logger logger) where TException : Exception
        {
            return Policy
                .Handle<TException>(exception =>
                    !exception.Message.Contains(DIGTimeoutExceptionMessage_OperationTimedOut)
                    && !exception.Message.Contains(DIGConnectionExceptionMessage_ServiceUnavailable)
                    && !exception.Message.Contains(DIGTokenExpiredExceptionMessage)
                    && !exception.Message.Contains(DIGTokenInvalidExceptionMessage)
                    && !exception.Message.Contains(DIGRateLimitExceptionMessage_TooManyRequests, StringComparison.OrdinalIgnoreCase)
                    && !exception.Message.Contains(DIGRateLimitExceptionMessage_RateLimitExceeded, StringComparison.OrdinalIgnoreCase)
                    && !exception.Message.Contains(DIGBadRequestExceptionMessage)
                    && !exception.Message.Contains(DIGPayloadTooLargeExceptionMessage)
                    && !JsonExceptionType.IsAssignableFrom(exception.GetType())
                    && exception is not TaskCanceledException
                    && exception is not OperationCanceledException)
                .WaitAndRetryAsync(
                    retryCount: MaxRetries,
                    sleepDurationProvider: (retryAttempt) =>
                    {
                        int sleepDurationSeconds;
                        if (retryAttempt < 2)
                            sleepDurationSeconds = 1;
                        else if (retryAttempt < 11)
                            sleepDurationSeconds = 10;
                        else if (retryAttempt < 21)
                            sleepDurationSeconds = 30;
                        else if (retryAttempt < 31)
                            sleepDurationSeconds = 60;
                        else if (retryAttempt < 41)
                            sleepDurationSeconds = 300;
                        else
                            sleepDurationSeconds = 600;
                        return TimeSpan.FromSeconds(sleepDurationSeconds);
                    },
                    onRetry: (exception, sleepDuration, attemptNumber, context) =>
                    {
                        context[PollyContextKeyIsExceptionTimeoutRelated] = false;
                        exceptionHelper.LogException(exception, NLogLogLevelName.Warn, DefaultErrorMessagePrefix);
                        context[PollyContextKeyRetryAttemptNumber] = attemptNumber;
                        var methodName = GetMethodNameFromContext(context);
                        logger.Warn($"[{methodName}] {nameof(DIGAPIResilienceHelper)} caught exception: '{exception.Message}'. Retrying in {sleepDuration} (attempt {attemptNumber} of {MaxRetries}).");
                    }
                );
        }

        /// <summary>
        /// Creates an <see cref="AsyncRetryPolicy"/> for exceptions that ARE timeout-related.
        /// </summary>
        static AsyncRetryPolicy CreateAsyncRetryPolicyForDIGAPICallsTimedOut<TException>(DIGExceptionHelper exceptionHelper, Logger logger) where TException : Exception
        {
            return Policy
                .Handle<TException>(exception =>
                    exception.Message.Contains(DIGTimeoutExceptionMessage_OperationTimedOut))
                .Or<TException>(exception =>
                    exception.Message.Contains(DIGConnectionExceptionMessage_ServiceUnavailable))
                .Or<TaskCanceledException>()
                .Or<OperationCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: MaxRetries,
                    sleepDurationProvider: (retryAttempt) => TimeSpan.FromSeconds(1),
                    onRetry: (exception, sleepDuration, attemptNumber, context) =>
                    {
                        context[PollyContextKeyIsExceptionTimeoutRelated] = true;
                        exceptionHelper.LogException(exception, NLogLogLevelName.Warn, DefaultErrorMessagePrefix);
                        context[PollyContextKeyRetryAttemptNumber] = attemptNumber;
                        var methodName = GetMethodNameFromContext(context);
                        logger.Warn($"[{methodName}] {nameof(DIGAPIResilienceHelper)} caught timeout exception: '{exception.Message}'. Retrying in {sleepDuration} (attempt {attemptNumber} of {MaxRetries}).");
                    }
                );
        }

        /// <summary>
        /// Creates an <see cref="AsyncRetryPolicy"/> that handles DIG token expiry by re-authenticating and retrying.
        /// </summary>
        static AsyncRetryPolicy CreateAsyncRetryPolicyForTokenExpiry<TException>(DIGExceptionHelper exceptionHelper, Logger logger, Func<Task> reauthenticateAsync) where TException : Exception
        {
            return Policy
                .Handle<TException>(exception =>
                    exception.Message.Contains(DIGTokenExpiredExceptionMessage) ||
                    exception.Message.Contains(DIGTokenInvalidExceptionMessage))
                .WaitAndRetryAsync(
                    retryCount: MaxReauthenticationRetries,
                    sleepDurationProvider: (retryAttempt) => TimeSpan.FromSeconds(1),
                    onRetryAsync: async (exception, sleepDuration, attemptNumber, context) =>
                    {
                        exceptionHelper.LogException(exception, NLogLogLevelName.Warn, DefaultErrorMessagePrefix);
                        var methodName = GetMethodNameFromContext(context);
                        logger.Warn($"[{methodName}] {nameof(DIGAPIResilienceHelper)} detected DIG token expiry/invalid: '{exception.Message}'. Attempting re-authentication (attempt {attemptNumber} of {MaxReauthenticationRetries}).");

                        try
                        {
                            await reauthenticateAsync();
                            logger.Info("DIG re-authentication successful. Retrying original operation.");
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
        /// Creates an <see cref="AsyncTimeoutPolicy"/> with dynamic timeout calculation.
        /// </summary>
        static AsyncTimeoutPolicy CreateAsyncTimeoutPolicyForDIGAPICalls(int initialTimeoutSeconds)
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

                int timeoutSeconds = isExceptionTimeoutRelated
                    ? GetRetrySecondsForCurrentAttemptTimedOut(initialTimeoutSeconds, attemptNumber)
                    : GetRetrySecondsForCurrentAttemptNotTimedOut(attemptNumber);

                var retryAttemptTimeoutTimeSpan = TimeSpan.FromSeconds(timeoutSeconds);
                context[PollyContextKeyRetryAttemptTimeoutTimeSpan] = retryAttemptTimeoutTimeSpan;
                return retryAttemptTimeoutTimeSpan;
            });
        }

        static int GetRetrySecondsForCurrentAttemptNotTimedOut(int attemptNumber)
        {
            const int TimeoutSecondsForAttemptRange1 = 30;
            const int TimeoutSecondsForAttemptRange2 = 300;
            int TimeoutSecondsForAttemptRange3 = MaxTimeoutSeconds;

            if (attemptNumber < 1) attemptNumber = 1;
            if (attemptNumber < 4) return TimeoutSecondsForAttemptRange1;
            if (attemptNumber < 7) return TimeoutSecondsForAttemptRange2;
            return TimeoutSecondsForAttemptRange3;
        }

        static int GetRetrySecondsForCurrentAttemptTimedOut(int initialTimeoutSeconds, int attemptNumber)
        {
            if (attemptNumber < 1) attemptNumber = 1;
            if (initialTimeoutSeconds < MinTimeoutSeconds) initialTimeoutSeconds = MinTimeoutSeconds;
            if (initialTimeoutSeconds >= MaxTimeoutSeconds) return MaxTimeoutSeconds;
            int retrySecondsForCurrentAttempt = initialTimeoutSeconds * attemptNumber;
            if (retrySecondsForCurrentAttempt > MaxTimeoutSeconds) return MaxTimeoutSeconds;
            return retrySecondsForCurrentAttempt;
        }

        static int GetValidatedInitialTimeoutSeconds(int initialTimeoutSeconds)
        {
            if (initialTimeoutSeconds < MinTimeoutSeconds) return MinTimeoutSeconds;
            if (initialTimeoutSeconds > MaxTimeoutSeconds) return MaxTimeoutSeconds;
            return initialTimeoutSeconds;
        }
    }
}