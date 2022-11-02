using NLog;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;
using System;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A helper class for enhancing application resiliency through the provision of policies that can provide for things such as setting timeouts and retries when executing database commands (rather than simply failing on the first attempt when the exception may be transient in nature).
    /// </summary>
    public static class DatabaseResilienceHelper
    {
        const string DeadlockString = "chosen as the deadlock victim";
        const string TimeoutString = "timeout";

        /// <summary>
        /// The maximum allowed timeout in seconds.
        /// </summary>
        public static int MaxTimeoutSeconds { get => 1000; } // 30 minutes

        public static string PollyContextKeyRetryAttemptNumber { get => "RetryAttemptNumber"; }

        public static string PollyContextKeyRetryAttemptTimeoutTimeSpan { get => "RetryAttemptTimeoutTimeSpan"; }


        /// <summary>
        /// Creates an <see cref="AsyncRetryPolicy"/> for the <typeparamref name="TException"/> - handling only those <typeparamref name="TException"/>s where the message or that of the InnerException (if one is present) contains "timeout".
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy. Note - only those <typeparamref name="TException"/>s where the message or that of the InnerException (if one is present) contains "timeout" will be handled.</typeparam>
        /// <param name="maxRetries">The maximum number of times an action may be retried.</param>
        /// <param name="logger">The logger to be used for logging any exception and retry-related information.</param>
        static AsyncRetryPolicy CreateAsyncRetryPolicyForDatabaseCommandTimeouts<TException>(int maxRetries, Logger logger) where TException : Exception
        {
            return Policy
                .Handle<TException>(exception =>
                    exception.Message.ToLower().Contains(TimeoutString))
                .OrInner<TException>(exception =>
                    exception.Message.ToLower().Contains(TimeoutString))
                .WaitAndRetryAsync(
                    retryCount: maxRetries,
                    sleepDurationProvider: (retryAttempt) => TimeSpan.FromSeconds(1),
                    onRetry: (exception, sleepDuration, attemptNumber, context) =>
                    {
                        var commandTimeout = (TimeSpan)context[PollyContextKeyRetryAttemptTimeoutTimeSpan];
                        context[PollyContextKeyRetryAttemptNumber] = attemptNumber;
                        logger.Warn($"Transient exception of type \'{exception.GetType().Name}\' with message \'{exception.Message}\' detected. Command timeout was {commandTimeout}. Retrying in {sleepDuration} (attempt {attemptNumber} of {maxRetries}).");
                    }
                 );
        }

        /// <summary>
        /// Creates an <see cref="AsyncRetryPolicy"/> for the <typeparamref name="TException"/> - handling only those <typeparamref name="TException"/>s where the message or that of the InnerException (if one is present) contains "timeout".
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy. Note - only those <typeparamref name="TException"/>s where the message or that of the InnerException (if one is present) contains "chosen as the deadlock victim" will be handled.</typeparam>
        /// <param name="maxRetries">The maximum number of times an action may be retried.</param>
        /// <param name="logger">The logger to be used for logging any exception and retry-related information.</param>
        public static AsyncRetryPolicy CreateAsyncRetryPolicyForDatabaseTransactions<TException>(int maxRetries, Logger logger) where TException : Exception
        {
            return Policy
                .Handle<TException>(exception =>
                    exception.Message.ToLower().Contains(DeadlockString))
                .OrInner<TException>(exception =>
                    exception.Message.ToLower().Contains(DeadlockString))
                .WaitAndRetryAsync(
                    retryCount: maxRetries,
                    sleepDurationProvider: (retryAttempt) => TimeSpan.FromSeconds(1),
                    onRetry: (exception, sleepDuration, attemptNumber, context) =>
                    {
                        context[PollyContextKeyRetryAttemptNumber] = attemptNumber;
                        logger.Warn($"Transient exception of type \'{exception.GetType().Name}\' with message \'{exception.Message}\' detected. Retrying in {sleepDuration} (attempt {attemptNumber} of {maxRetries}).");
                    }
                 );
        }

        /// <summary>
        /// Creates an <see cref="AsyncTimeoutPolicy"/> wherein the timeout increases with every attempt up to a maximum of <see cref="MaxTimeoutSeconds"/>. Once this maximum is reached, any subsequent attempts will have the timeout value set to this maximum. With an initial <paramref name="initialTimeoutSeconds"/> value of 30, the timeout values of 10 successive attempts (in seconds) would be: 30, 90, 150, 210, 270, 330, 390, 450, 510 and 570.
        /// </summary>
        /// <param name="initialTimeoutSeconds">The initial timeout in seconds to be applied to the first attempt of the async delegate.</param>
        static AsyncTimeoutPolicy CreateAsyncTimeoutPolicyForDatabaseCommandTimeoutsBasedOnRetryAttemptNumber(int initialTimeoutSeconds)
        {
            return Policy.TimeoutAsync(context => {
                int attemptNumber = 0;
                if (context.ContainsKey(PollyContextKeyRetryAttemptNumber))
                {
                    attemptNumber = (int)context[PollyContextKeyRetryAttemptNumber];
                }

                int timeoutSeconds = initialTimeoutSeconds * (attemptNumber + 1);
                if (attemptNumber > 0)
                {
                    timeoutSeconds += (initialTimeoutSeconds * attemptNumber);
                }
                if (timeoutSeconds > MaxTimeoutSeconds)
                {
                    timeoutSeconds = MaxTimeoutSeconds;
                }

                var retryAttemptTimeoutTimeSpan = TimeSpan.FromSeconds(timeoutSeconds);
                context[PollyContextKeyRetryAttemptTimeoutTimeSpan] = retryAttemptTimeoutTimeSpan;
                return retryAttemptTimeoutTimeSpan;
            });
        }

        /// <summary>
        /// Creates an <see cref="AsyncPolicyWrap"/> that includes an <see cref="AsyncRetryPolicy"/> combined with a <see cref="AsyncTimeoutPolicy"/>. Designed specifically to facilitate timeouts and retries when executing database commands. The timeout increases with every attempt - from <paramref name="timeoutSeconds"/> up to a maximum of <see cref="MaxTimeoutSeconds"/>. Once this maximum is reached, any subsequent attempts will have the timeout value set to this maximum. With an initial <paramref name="timeoutSeconds"/> value of 30, the timeout values of 10 successive attempts (in seconds) would be: 30, 90, 150, 210, 270, 330, 390, 450, 510 and 570.
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy. Note - only those <typeparamref name="TException"/>s where the message or that of the InnerException (if one is present) contains "timeout" will be handled.</typeparam>
        /// <param name="timeoutSeconds">The initial timeout in seconds to be applied to the first attempt of the async delegate (i.e. the first database command execution attempt).</param>
        /// <param name="maxRetries">The maximum number of times an action may be retried (i.e. the maximum number of attempts for the database command before allowing the timeout exception to bubble-up).</param>
        /// <param name="logger">The logger to be used for logging any exception and retry-related information.</param>
        public static AsyncPolicyWrap CreateAsyncPolicyWrapForDatabaseCommandTimeoutAndRetry<TException>(int timeoutSeconds, int maxRetries, Logger logger) where TException : Exception
        {
            var asyncRetryPolicy = CreateAsyncRetryPolicyForDatabaseCommandTimeouts<TException>(maxRetries, logger);
            var asyncTimeoutPolicy = CreateAsyncTimeoutPolicyForDatabaseCommandTimeoutsBasedOnRetryAttemptNumber(timeoutSeconds);
            return Policy.WrapAsync(asyncRetryPolicy, asyncTimeoutPolicy);
        }
    }
}
