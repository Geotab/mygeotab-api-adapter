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
        /// <summary>
        /// "command is already in progress"
        /// </summary>
        public static string CommandAlreadyInProgressString { get => "command is already in progress"; }

        /// <summary>
        /// "chosen as the deadlock victim"
        /// </summary>
        public static string DeadlockString { get => "chosen as the deadlock victim"; }

        /// <summary>
        /// "exception occurred while attempting to get an open database connection"
        /// </summary>
        public static string ErrorOccurredWhileGettingOpenConnectionString { get => "exception occurred while attempting to get an open database connection"; }

        /// <summary>
        /// "exception occurred while attempting to get an open postgresql database connection"
        /// </summary>
        public static string ErrorOccurredWhileGettingOpenPostgreSQLConnectionString { get => "exception occurred while attempting to get an open postgresql database connection"; }

        /// <summary>
        /// "this sqltransaction has completed; it is no longer usable"
        /// </summary>
        public static string SqlTransactionCompletedNoLongerUsableString { get => "this sqltransaction has completed; it is no longer usable"; }

        /// <summary>
        /// "timeout"
        /// </summary>
        public static string TimeoutString { get => "timeout"; }

        /// <summary>
        /// "time out"
        /// </summary>
        public static string TimeoutString2 { get => "time out"; }

        /// <summary>
        /// The maximum number of seconds to wait between retry attempts for database transactions.
        /// </summary>
        public static int MaxTransactionRetryAttemptDelaySeconds { get => 300; }

        /// <summary>
        /// The number of seconds to add to the timeout on each successive attempt at executing a database command.
        /// </summary>
        public static int TimeoutSecondsToAddOnEachCommandRetryAttempt { get => 300; }

    /// <summary>
    /// The maximum allowed timeout in seconds.
    /// </summary>
    public static int MaxTimeoutSeconds { get => 3600; } // 1 hour

        // Polly context variable names:
        public static string PollyContextKeyRetryAttemptNumber { get => "RetryAttemptNumber"; }
        public static string PollyContextKeyRetryAttemptTimeoutTimeSpan { get => "RetryAttemptTimeoutTimeSpan"; }

        /// <summary>
        /// Creates an <see cref="AsyncRetryPolicy"/> for the <typeparamref name="TException"/> - handling only those <typeparamref name="TException"/>s where the message or that of the InnerException (if one is present) contains <see cref="TimeoutString"/> or <see cref="TimeoutString2"/>.
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy. Note - only those <typeparamref name="TException"/>s where the message or that of the InnerException (if one is present) contains <see cref="TimeoutString"/> or <see cref="TimeoutString2"/> will be handled.</typeparam>
        /// <param name="maxRetries">The maximum number of times an action may be retried.</param>
        /// <param name="logger">The logger to be used for logging any exception and retry-related information.</param>
        public static AsyncRetryPolicy CreateAsyncRetryPolicyForDatabaseCommandTimeouts<TException>(int maxRetries, Logger logger) where TException : Exception
        {
            return Policy
                .Handle<TException>(exception =>
                    exception.Message.ToLower().Contains(TimeoutString))
                .Or<TException>(exception =>
                    exception.Message.ToLower().Contains(TimeoutString2))
                .OrInner<TException>(exception =>
                    exception.Message.ToLower().Contains(TimeoutString))
                .OrInner<TException>(exception =>
                    exception.Message.ToLower().Contains(TimeoutString2))
                .WaitAndRetryAsync(
                    retryCount: maxRetries,
                    sleepDurationProvider: (retryAttempt) =>
                    {
                        // Set the delay time before retrying based on the retry attempt number.
                        int sleepDurationSeconds = 1;
                        var jitterer = new Random();
                        int sleepDurationMilliseconds = sleepDurationSeconds * 1000 + jitterer.Next(0, 3000);
                        var sleepDuration = TimeSpan.FromMilliseconds(sleepDurationMilliseconds);
                        return sleepDuration;

                    },
                    onRetry: (exception, sleepDuration, attemptNumber, context) =>
                    {
                        var commandTimeout = (TimeSpan)context[PollyContextKeyRetryAttemptTimeoutTimeSpan];
                        context[PollyContextKeyRetryAttemptNumber] = attemptNumber;
                        logger.Warn($"Transient exception of type \'{exception.GetType().Name}\' with message \'{exception.Message}\' detected. Command timeout was {commandTimeout}. Retrying command in {sleepDuration} (attempt {attemptNumber} of {maxRetries}).");
                    }
                 );
        }

        /// <summary>
        /// Creates an <see cref="AsyncRetryPolicy"/> for the <typeparamref name="TException"/> - handling only those <typeparamref name="TException"/>s where the message or that of the InnerException (if one is present) contains:
        /// <see cref="DeadlockString"/>, <see cref="ErrorOccurredWhileGettingOpenConnectionString"/>, <see cref="ErrorOccurredWhileGettingOpenPostgreSQLConnectionString"/>, <see cref="SqlTransactionCompletedNoLongerUsableString"/>, <see cref="TimeoutString"/>, <see cref="CommandAlreadyInProgressString"/>, or <see cref="TimeoutString2"/>.
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy. Note - only those <typeparamref name="TException"/>s where the message or that of the InnerException (if one is present) contains one of the strings noted in the summary will be handled.</typeparam>
        /// <param name="logger">The logger to be used for logging any exception and retry-related information.</param>
        /// <param name="maxRetries">OPTIONAL: The maximum number of times an action may be retried. If not specified, the maximum is effectively unlimited.</param>
        public static AsyncRetryPolicy CreateAsyncRetryPolicyForDatabaseTransactions<TException>(Logger logger, int maxRetries = 1000000) where TException : Exception
        {
            return Policy
                .Handle<TException>(exception =>
                    exception.Message.ToLower().Contains(DeadlockString))
                .Or<TException>(exception =>
                    exception.Message.ToLower().Contains(ErrorOccurredWhileGettingOpenConnectionString))
                .Or<TException>(exception =>
                    exception.Message.ToLower().Contains(ErrorOccurredWhileGettingOpenPostgreSQLConnectionString))
                .Or<TException>(exception =>
                    exception.Message.ToLower().Contains(SqlTransactionCompletedNoLongerUsableString))
                .Or<TException>(exception =>
                    exception.Message.ToLower().Contains(TimeoutString))
                .Or<TException>(exception =>
                    exception.Message.ToLower().Contains(TimeoutString2))
                .Or<TException>(exception =>
                    exception.Message.ToLower().Contains(CommandAlreadyInProgressString))
                .OrInner<TException>(exception =>
                    exception.Message.ToLower().Contains(DeadlockString))
                .OrInner<TException>(exception =>
                    exception.Message.ToLower().Contains(ErrorOccurredWhileGettingOpenConnectionString))
                .OrInner<TException>(exception =>
                    exception.Message.ToLower().Contains(ErrorOccurredWhileGettingOpenPostgreSQLConnectionString))
                .OrInner<TException>(exception =>
                    exception.Message.ToLower().Contains(SqlTransactionCompletedNoLongerUsableString))
                .OrInner<TException>(exception =>
                    exception.Message.ToLower().Contains(TimeoutString))
                .OrInner<TException>(exception =>
                    exception.Message.ToLower().Contains(TimeoutString2))
                .OrInner<TException>(exception =>
                    exception.Message.ToLower().Contains(CommandAlreadyInProgressString))
                .WaitAndRetryAsync(
                    retryCount: maxRetries,
                    sleepDurationProvider: (retryAttempt) =>
                    {
                        // Set the delay time before retrying based on the retry attempt number.
                        int sleepDurationSeconds = retryAttempt + 1;
                        if (sleepDurationSeconds > MaxTransactionRetryAttemptDelaySeconds)
                        {
                            sleepDurationSeconds = MaxTransactionRetryAttemptDelaySeconds;
                        }
                        var jitterer = new Random();
                        int sleepDurationMilliseconds = sleepDurationSeconds * 1000 + jitterer.Next(0, 3000);
                        var sleepDuration = TimeSpan.FromMilliseconds(sleepDurationMilliseconds);
                        return sleepDuration;
                    },
                    onRetry: (exception, sleepDuration, attemptNumber, context) =>
                    {
                        context[PollyContextKeyRetryAttemptNumber] = attemptNumber;
                        logger.Warn($"Transient exception of type \'{exception.GetType().Name}\' with message \'{exception.Message}\' detected within a transaction. Retrying transaction in {sleepDuration} (attempt {attemptNumber} of {maxRetries}).");
                    }
                 );
        }

        /// <summary>
        /// Creates an <see cref="AsyncTimeoutPolicy"/> wherein the timeout increases by <see cref="TimeoutSecondsToAddOnEachCommandRetryAttempt"/> with every attempt up to a maximum of <see cref="MaxTimeoutSeconds"/>. Once this maximum is reached, any subsequent attempts will have the timeout value set to this maximum. With an initial <paramref name="initialTimeoutSeconds"/> value of 30, the timeout values of 5 successive attempts (in seconds) would be: 30, 330, 630, 930, 1230.
        /// </summary>
        /// <param name="initialTimeoutSeconds">The initial timeout in seconds to be applied to the first attempt of the async delegate.</param>
        public static AsyncTimeoutPolicy CreateAsyncTimeoutPolicyForDatabaseCommandTimeoutsBasedOnRetryAttemptNumber(int initialTimeoutSeconds)
        {
            return Policy.TimeoutAsync(context => {
                int attemptNumber = 0;
                if (context.ContainsKey(PollyContextKeyRetryAttemptNumber))
                {
                    attemptNumber = (int)context[PollyContextKeyRetryAttemptNumber];
                }

                int timeoutSeconds = initialTimeoutSeconds + (TimeoutSecondsToAddOnEachCommandRetryAttempt * (attemptNumber));
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
        /// Creates an <see cref="AsyncPolicyWrap"/> that includes an <see cref="AsyncRetryPolicy"/> (<see cref="CreateAsyncRetryPolicyForDatabaseCommandTimeouts{TException}(int, Logger)"/>) combined with a <see cref="AsyncTimeoutPolicy"/> (<see cref="CreateAsyncTimeoutPolicyForDatabaseCommandTimeoutsBasedOnRetryAttemptNumber(int)"/>). Designed specifically to facilitate timeouts and retries when executing database commands. 
        /// </summary>
        /// <typeparam name="TException">The type of <see cref="Exception"/> to be handled by the policy.</typeparam>
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
