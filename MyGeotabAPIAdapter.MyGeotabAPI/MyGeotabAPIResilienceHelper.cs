using MyGeotabAPIAdapter.Logging;
using NLog;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;
using System;

namespace MyGeotabAPIAdapter.MyGeotabAPI
{
    /// <summary>
    /// A helper class for enhancing application resiliency through the provision of policies that can provide for things such as setting timeouts and retries when executing MyGeotab API calls and encountering unhandled exceptions (rather than simply failing on the first attempt when the exception may be transient in nature).
    /// </summary>
    public static class MyGeotabAPIResilienceHelper
    {
        static string DefaultErrorMessagePrefix { get => $"{nameof(MyGeotabAPIResilienceHelper)} process caught an exception"; }

        // Known MyGeotab API connextivity-related exception detail constants.
        public const string MyGeotabConnectionExceptionMessage_DbUnavailableException = "DbUnavailableException";
        public const string MyGeotabConnectionExceptionMessage_HttpRequestException_Connection_refused = "HttpRequestException Connection refused";
        public const string MyGeotabConnectionExceptionMessage_ServiceUnavailableException_Service_temporarily_unavailable = "ServiceUnavailableException Service temporarily unavailable";
        
        // Known MyGeotab API exception detail constants,
        public const string MyGeotabException_WebServerInvoker = "WebServerInvoker";
        public const string MyGeotabTimeoutExceptionMessage_OperationTimedOut = "The operation has timed out.";

        /// <summary>
        /// The maximum allowed number of retries for each MyGeotab API call.
        /// </summary>
        public static int MaxRetries { get => 1000000; } // Essentially unlimited.

        /// <summary>
        /// The maximum allowed timeout in seconds for each MyGeotab API call.
        /// </summary>
        public static int MaxTimeoutSeconds { get => 3600; } // 1 hour

        /// <summary>
        /// The minimum allowed timeout in seconds for each MyGeotab API call.
        /// </summary>
        public static int MinTimeoutSeconds { get => 10; }

        // Polly context variable names:
        public static string PollyContextKeyIsExceptionTimeoutRelated { get => "IsExceptionTimeoutRelated"; }
        public static string PollyContextKeyRetryAttemptNumber { get => "RetryAttemptNumber"; }
        public static string PollyContextKeyRetryAttemptTimeoutTimeSpan { get => "RetryAttemptTimeoutTimeSpan"; }

        /// <summary>
        /// Creates an <see cref="AsyncRetryPolicy"/> for the <typeparamref name="TException"/> - handling only those <typeparamref name="TException"/>s that are deemed NOT to be timeout-related and are also not known exceptions that are already handled by existing MyGeotab connectivity restoration logic. Because unhandled exceptions can range from those that are transient in nature (which might be resolved with a quick retry) to those which are outage-related (and could extend over long periods of time), a staggered delay is implemented between succesive retry attempts. A delay of 1 second will be applied before the first retry attempt. Attempts 2-10 will each be preceeded by a 10-second delay. Attempts 11-20 will each be preceeded by a 30-second delay. Attempts 21-30 will each be preceeded by a 1-minute delay. Attempts 31-40 will each be preceeded by a 5-minute delay. All further retry attempts will each be preceeded by a 10-minute delay.
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy.</typeparam>
        /// <param name="exceptionHelper">The <see cref="ExceptionHelper"/> to be used for logging details of the <typeparam name="TException">.</param>
        /// <param name="logger">The logger to be used for logging any exception and retry-related information.</param>
        /// <returns></returns>
        static AsyncRetryPolicy CreateAsyncRetryPolicyForMyGeotabAPICallsNotTimedOut<TException>(ExceptionHelper exceptionHelper, Logger logger) where TException : Exception
        {
            return Policy
                .Handle<TException>(exception =>
                    exception.Message.Contains(MyGeotabTimeoutExceptionMessage_OperationTimedOut) == false
                    && exception.Message.Contains(MyGeotabConnectionExceptionMessage_DbUnavailableException) == false
                    && exception.Message.Contains(MyGeotabConnectionExceptionMessage_HttpRequestException_Connection_refused) == false
                    && exception.Message.Contains(MyGeotabConnectionExceptionMessage_ServiceUnavailableException_Service_temporarily_unavailable) == false)
                .WaitAndRetryAsync(
                    retryCount: MaxRetries,
                    sleepDurationProvider: (retryAttempt) => 
                    {
                        // Set the delay time before retrying based on the retry attempt number.
                        int sleepDurationSeconds = 0;
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
                        // Set the IsExceptionTimeoutRelated variable value in the Polly context.
                        context[PollyContextKeyIsExceptionTimeoutRelated] = false;

                        // Log the exception first so that full details are available for debugging if needed.
                        exceptionHelper.LogException(exception, NLogLogLevelName.Warn, DefaultErrorMessagePrefix);

                        // Log details of the exception handling policy.
                        var callTimeout = (TimeSpan)context[PollyContextKeyRetryAttemptTimeoutTimeSpan];
                        context[PollyContextKeyRetryAttemptNumber] = attemptNumber;
                        logger.Warn($"{nameof(MyGeotabAPIResilienceHelper)} caught exception of type \'{exception.GetType().Name}\' with message \'{exception.Message}\'. Full details were logged immediately before this message. Geotab API call timeout was {callTimeout}. Retrying in {sleepDuration} (attempt {attemptNumber} of {MaxRetries}).");
                    }
                 );
        }

        /// <summary>
        /// Creates an <see cref="AsyncRetryPolicy"/> for the <typeparamref name="TException"/> - handling only those <typeparamref name="TException"/>s that are deemed to be timeout-related and are also not known exceptions that are already handled by existing MyGeotab connectivity restoration logic. There is a one second delay before each retry attempt.
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy.</typeparam>
        /// <param name="exceptionHelper">The <see cref="ExceptionHelper"/> to be used for logging details of the <typeparam name="TException">.</param>
        /// <param name="logger">The logger to be used for logging any exception and retry-related information.</param>
        /// <returns></returns>
        static AsyncRetryPolicy CreateAsyncRetryPolicyForMyGeotabAPICallsTimedOut<TException>(ExceptionHelper exceptionHelper, Logger logger) where TException : Exception
        {
            return Policy
                .Handle<TException>(exception =>
                    exception.Message.Contains(MyGeotabTimeoutExceptionMessage_OperationTimedOut) == true
                    && exception.StackTrace != null && exception.StackTrace.Contains(MyGeotabException_WebServerInvoker) == true
                    && exception.Message.Contains(MyGeotabConnectionExceptionMessage_DbUnavailableException) == false 
                    && exception.Message.Contains(MyGeotabConnectionExceptionMessage_HttpRequestException_Connection_refused) == false 
                    && exception.Message.Contains(MyGeotabConnectionExceptionMessage_ServiceUnavailableException_Service_temporarily_unavailable) == false)
                .Or<TException>(exception =>
                    exception.Message.Contains(MyGeotabConnectionExceptionMessage_ServiceUnavailableException_Service_temporarily_unavailable))
                .OrInner<TException>(exception =>
                    exception.Message.Contains(MyGeotabConnectionExceptionMessage_ServiceUnavailableException_Service_temporarily_unavailable))
                .WaitAndRetryAsync(
                    retryCount: MaxRetries,
                    sleepDurationProvider: (retryAttempt) => TimeSpan.FromSeconds(1),
                    onRetry: (exception, sleepDuration, attemptNumber, context) =>
                    {
                        // Set the IsExceptionTimeoutRelated variable value in the Polly context.
                        context[PollyContextKeyIsExceptionTimeoutRelated] = true;

                        // Log the exception first so that full details are available for debugging if needed.
                        exceptionHelper.LogException(exception, NLogLogLevelName.Warn, DefaultErrorMessagePrefix);

                        // Log details of the exception handling policy.
                        var callTimeout = (TimeSpan)context[PollyContextKeyRetryAttemptTimeoutTimeSpan];
                        context[PollyContextKeyRetryAttemptNumber] = attemptNumber;
                        logger.Warn($"{nameof(MyGeotabAPIResilienceHelper)} caught exception of type \'{exception.GetType().Name}\' with message \'{exception.Message}\'. Full details were logged immediately before this message. Geotab API call timeout was {callTimeout}. Retrying in {sleepDuration} (attempt {attemptNumber} of {MaxRetries}).");
                    }
                 );
        }

        /// <summary>
        /// Creates an <see cref="AsyncTimeoutPolicy"/>. For exceptions that are timeout-related, the timeout will generally be calcualated as <paramref name="initialTimeoutSeconds"/> multiplied by the retry attempt number (up to <see cref="MaxTimeoutSeconds"/>). For exceptions that are NOT timeout-related, the timeout value will be 30 seconds for each of the first three retry attempts, 5 minutes for the next three retries and <see cref="MaxTimeoutSeconds"/> (1 hour) for each subsequent retry. 
        /// </summary>
        /// <param name="initialTimeoutSeconds">The initial timeout in seconds to be applied to the first attempt of the async delegate.</param>
        static AsyncTimeoutPolicy CreateAsyncTimeoutPolicyForMyGeotabAPICalls(int initialTimeoutSeconds)
        {
            initialTimeoutSeconds = GetValidatedInitialTimeoutSeconds(initialTimeoutSeconds);

            return Policy.TimeoutAsync(context => {
                // Get the current retry attempt number.
                int attemptNumber = 0;
                if (context.ContainsKey(PollyContextKeyRetryAttemptNumber))
                {
                    attemptNumber = (int)context[PollyContextKeyRetryAttemptNumber];
                }

                // Determine whether the exception is timeout-related.
                bool isExceptionTimeoutRelated = false;
                if (context.ContainsKey(PollyContextKeyIsExceptionTimeoutRelated))
                {
                    isExceptionTimeoutRelated = (bool)context[PollyContextKeyIsExceptionTimeoutRelated];
                }

                // Calculate the timeout value to use for the next retry attempt.
                int timeoutSeconds = 1;
                if (isExceptionTimeoutRelated == true)
                {
                    timeoutSeconds = GetRetrySecondsForCurrentAttemptTimedOut(initialTimeoutSeconds, attemptNumber);
                }
                else
                {
                    timeoutSeconds = GetRetrySecondsForCurrentAttemptNotTimedOut(attemptNumber);
                }

                // Return the calcualted timeout value after storing it in the Polly context for use by the retry policies.
                var retryAttemptTimeoutTimeSpan = TimeSpan.FromSeconds(timeoutSeconds);
                context[PollyContextKeyRetryAttemptTimeoutTimeSpan] = retryAttemptTimeoutTimeSpan;
                return retryAttemptTimeoutTimeSpan;
            });
        }

        /// <summary>
        /// Creates an <see cref="AsyncPolicyWrap"/> that includes <see cref="AsyncRetryPolicy"/>s for timeout-related and other exceptions encountered when making Geotab API calls. Exceptions that are known to be related to Geotab connectivity or outages (e.g. for maintenance windows) are ignored by these policies and continue to be handled by the pre-existing MyGeotab connectivivity loss handling logic. These retry policies are specifically for unhandled/unknown exceptions. Also includes a <see cref="AsyncTimeoutPolicy"/> that varies the timeout length for each retry attempt depending on whether the exception is determined to be timeout-related or not. 
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy.</typeparam>
        /// <param name="configuredTimeoutSeconds">The initial timeout in seconds to be applied to the first attempt of the async delegate (i.e. the first MyGeotab API call retry attempt).</param>
        /// <param name="exceptionHelper">The <see cref="ExceptionHelper"/> to be used for logging details of the <typeparam name="TException">.</param>
        /// <param name="logger">The logger to be used for logging any exception and retry-related information.</param>
        /// <returns></returns>
        public static AsyncPolicyWrap CreateAsyncPolicyWrapForMyGeotabAPICalls<TException>(int configuredTimeoutSeconds, ExceptionHelper exceptionHelper, Logger logger) where TException : Exception
        {
            var asyncRetryPolicyForMyGeotabAPICallsTimedOut = CreateAsyncRetryPolicyForMyGeotabAPICallsTimedOut<TException>(exceptionHelper, logger);
            var asyncRetryPolicyForMyGeotabAPICallsNonTimedOut = CreateAsyncRetryPolicyForMyGeotabAPICallsNotTimedOut<TException>(exceptionHelper, logger);
            var asyncTimeoutPolicy = CreateAsyncTimeoutPolicyForMyGeotabAPICalls(configuredTimeoutSeconds);
            return Policy.WrapAsync(asyncRetryPolicyForMyGeotabAPICallsTimedOut, asyncRetryPolicyForMyGeotabAPICallsNonTimedOut, asyncTimeoutPolicy);
        }

        /// <summary>
        /// Intended for use with exceptions that are NOT timeout-related. Returns a calculated timeout value, in seconds, based on the <paramref name="attemptNumber"/> value. For retry attempts 1-3, the timeout will be 30 seconds. For retry attempts 4-6, the timeout will be 5 minutes. All subsequent retry attempts will have a timeout of <see cref="MaxTimeoutSeconds"/>. 
        /// </summary>
        /// <param name="attemptNumber">The current retry attempt number.</param>
        /// <returns></returns>
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
        /// Intended for use with exceptions that are timeout-related. Returns a calculated timeout value, in seconds, based on the <paramref name="initialTimeoutSeconds"/> and <paramref name="attemptNumber"/> values. The <paramref name="initialTimeoutSeconds"/> may be adjsuted to ensure that it falls within the valid range of <see cref="MinTimeoutSeconds"/> to <see cref="MaxTimeoutSeconds"/>. The returned value will be <paramref name="initialTimeoutSeconds"/> multiplied by <paramref name="attemptNumber"/> or <see cref="MaxTimeoutSeconds"/> - whichever is lower.
        /// </summary>
        /// <param name="initialTimeoutSeconds">The initial timeout, in seconds, that should be applied on the first retry attempt.</param>
        /// <param name="attemptNumber">The current retry attempt number.</param>
        /// <returns></returns>
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
        /// Ensures that <paramref name="initialTimeoutSeconds"/> falls within the allowed range. If <paramref name="initialTimeoutSeconds"/> is less than <see cref="MinTimeoutSeconds"/>, returns <see cref="MinTimeoutSeconds"/>. If <paramref name="initialTimeoutSeconds"/> is greater than <see cref="MaxTimeoutSeconds"/>, returns <see cref="MaxTimeoutSeconds"/>. Otherwise returns <paramref name="initialTimeoutSeconds"/>.
        /// </summary>
        /// <param name="initialTimeoutSeconds">The timeout value to be validated.</param>
        /// <returns></returns>
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
