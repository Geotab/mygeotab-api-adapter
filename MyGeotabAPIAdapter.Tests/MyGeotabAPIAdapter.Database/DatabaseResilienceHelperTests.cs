using MyGeotabAPIAdapter.Database.DataAccess;
using NLog;
using Polly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// The retry policies that bound database maintenance statements. Each case counts how many times a delegate is attempted; where a
    /// retry is expected the delegate fails once only, so the policies' own backoff (a couple of seconds for a first retry) is paid once.
    /// </summary>
    public class DatabaseResilienceHelperTests
    {
        static readonly Logger TestLogger = LogManager.GetCurrentClassLogger();

        // The shapes the maintenance statements actually fail with. BaseRepository wraps every error in an exception whose own message
        // matches nothing, so each policy has to find the cause in the inner exception.
        static Exception Wrapped(string innerMessage) => new("Exception encountered while attempting database operation.", new Exception(innerMessage));
        static Exception StatementTimeout() => Wrapped("57014: canceling statement due to statement timeout");
        static Exception LockTimeout() => Wrapped("55P03: canceling statement due to lock timeout");
        static Exception ExecutionTimeoutExpired() => Wrapped("Execution Timeout Expired. The timeout period elapsed prior to completion of the operation or the server is not responding.");
        static Exception Deadlock() => Wrapped("Transaction (Process ID 52) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction.");
        static Exception PostgreSQLDeadlock() => Wrapped("40P01: deadlock detected");

        // ConnectionProvider's three wrappers for a failure to open a connection, each around an open that timed out. Under
        // retryTimeouts: false the inner "timeout" no longer matches, so the wrapper's own text is all that can.
        static Exception OpenFailed(string wrapperMessage) => new(wrapperMessage, new Exception(
            "Connection Timeout Expired. The timeout period elapsed while attempting to consume the pre-login handshake acknowledgement."));

        [Theory]
        [InlineData("An exception occurred while attempting to get an open database connection.")]
        [InlineData("An exception occurred while attempting to get an open PostgreSQL database connection.")]
        [InlineData("An exception occurred while attempting to get an open SQL Server database connection.")]
        public async Task WithTimeoutsNotRetried_AConnectionOpenTimeoutIsStillRetried_ForEveryProvider(string wrapperMessage)
        {
            var policy = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(TestLogger, 3, retryTimeouts: false);

            var (attempts, thrown) = await ExecuteFailingAsync(policy, attempt => attempt == 1 ? OpenFailed(wrapperMessage) : null);

            Assert.Equal(2, attempts);
            Assert.Null(thrown);
        }

        static async Task<(int Attempts, Exception Thrown)> ExecuteFailingAsync(IAsyncPolicy policy, Func<int, Exception> failureOnAttempt)
        {
            var attempts = 0;
            try
            {
                await policy.ExecuteAsync(async _ =>
                {
                    attempts++;
                    await Task.Yield();
                    var failure = failureOnAttempt(attempts);
                    if (failure != null)
                    {
                        throw failure;
                    }
                }, new Context());
                return (attempts, null);
            }
            catch (Exception ex)
            {
                return (attempts, ex);
            }
        }

        [Fact]
        public async Task WithTimeoutsNotRetried_AStatementTimeoutIsAttemptedOnce()
        {
            var policy = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(TestLogger, 3, retryTimeouts: false);

            var (attempts, thrown) = await ExecuteFailingAsync(policy, _ => StatementTimeout());

            Assert.Equal(1, attempts);
            Assert.NotNull(thrown);
        }

        [Fact]
        public async Task WithTimeoutsNotRetried_ALockTimeoutIsAttemptedOnce()
        {
            var policy = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(TestLogger, 3, retryTimeouts: false);

            var (attempts, thrown) = await ExecuteFailingAsync(policy, _ => LockTimeout());

            Assert.Equal(1, attempts);
            Assert.NotNull(thrown);
        }

        [Fact]
        public async Task WithTimeoutsNotRetried_ASqlServerCommandTimeoutIsAttemptedOnce()
        {
            var policy = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(TestLogger, 3, retryTimeouts: false);

            var (attempts, thrown) = await ExecuteFailingAsync(policy, _ => ExecutionTimeoutExpired());

            Assert.Equal(1, attempts);
            Assert.NotNull(thrown);
        }

        [Fact]
        public async Task WithTimeoutsNotRetried_ADeadlockIsStillRetried()
        {
            var policy = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(TestLogger, 3, retryTimeouts: false);

            var (attempts, thrown) = await ExecuteFailingAsync(policy, attempt => attempt == 1 ? Deadlock() : null);

            Assert.Equal(2, attempts);
            Assert.Null(thrown);
        }

        [Fact]
        public async Task WithTimeoutsNotRetried_APostgreSQLDeadlockIsStillRetried()
        {
            var policy = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(TestLogger, 3, retryTimeouts: false);

            var (attempts, thrown) = await ExecuteFailingAsync(policy, attempt => attempt == 1 ? PostgreSQLDeadlock() : null);

            Assert.Equal(2, attempts);
            Assert.Null(thrown);
        }

        [Fact]
        public async Task ByDefault_APostgreSQLDeadlockIsRetried()
        {
            // PostgreSQL reports a deadlock as 40P01 "deadlock detected", which shares no text with SQL Server's
            // "chosen as the deadlock victim". Without a predicate of its own it is not retried at all.
            var policy = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(TestLogger, 3);

            var (attempts, thrown) = await ExecuteFailingAsync(policy, attempt => attempt == 1 ? PostgreSQLDeadlock() : null);

            Assert.Equal(2, attempts);
            Assert.Null(thrown);
        }

        [Fact]
        public async Task ByDefault_AStatementTimeoutIsStillRetried()
        {
            // The control: every other caller of the transaction policy keeps retrying a timeout.
            var policy = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(TestLogger, 3);

            var (attempts, thrown) = await ExecuteFailingAsync(policy, attempt => attempt == 1 ? StatementTimeout() : null);

            Assert.Equal(2, attempts);
            Assert.Null(thrown);
        }

        [Fact]
        public async Task ACommandWrapWithNoRetries_AttemptsATimedOutCommandOnce_AtTheWholeTimeout()
        {
            // What BaseRepository's single-attempt constructor builds for the maintenance statements.
            var wrap = DatabaseResilienceHelper.CreateAsyncPolicyWrapForDatabaseCommandTimeoutAndRetry<Exception>(DatabaseResilienceHelper.MaxTimeoutSeconds, 0, TestLogger);
            var attempts = 0;
            TimeSpan? commandTimeout = null;

            var thrown = await Record.ExceptionAsync(() => wrap.ExecuteAsync(async context =>
            {
                attempts++;
                commandTimeout = (TimeSpan)context[DatabaseResilienceHelper.PollyContextKeyRetryAttemptTimeoutTimeSpan];
                await Task.Yield();
                throw new Exception("Timeout during reading attempt");
            }, new Context()));

            Assert.Equal(1, attempts);
            Assert.Equal(TimeSpan.FromSeconds(DatabaseResilienceHelper.MaxTimeoutSeconds), commandTimeout);
            Assert.NotNull(thrown);
        }

        [Fact]
        public async Task ACommandWrapWithRetries_RetriesATimedOutCommand_WithALongerTimeout()
        {
            // The control: the wrap every other repository builds retries a timeout, allowing each attempt longer than the last.
            var wrap = DatabaseResilienceHelper.CreateAsyncPolicyWrapForDatabaseCommandTimeoutAndRetry<Exception>(600, 10, TestLogger);
            var timeouts = new System.Collections.Generic.List<TimeSpan>();

            var thrown = await Record.ExceptionAsync(() => wrap.ExecuteAsync(async context =>
            {
                timeouts.Add((TimeSpan)context[DatabaseResilienceHelper.PollyContextKeyRetryAttemptTimeoutTimeSpan]);
                await Task.Yield();
                if (timeouts.Count == 1)
                {
                    throw new Exception("Timeout during reading attempt");
                }
            }, new Context()));

            Assert.Null(thrown);
            Assert.Equal(new[] { TimeSpan.FromSeconds(600), TimeSpan.FromSeconds(900) }, timeouts);
        }
    }
}
