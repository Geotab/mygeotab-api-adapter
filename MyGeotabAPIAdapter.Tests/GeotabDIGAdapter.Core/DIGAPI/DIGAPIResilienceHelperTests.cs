using System;
using System.Threading.Tasks;
using MyGeotabAPIAdapter.DIGAPI;
using MyGeotabAPIAdapter.Logging;
using NLog;
using Xunit;

namespace MyGeotabAPIAdapter.Tests.GeotabDIGAdapter.Core.DIGAPI
{
    /// <summary>
    /// Regression coverage for IICON-34325. A thrown DIG API exception message must carry the numeric HTTP
    /// status code (e.g. "401"/"403") so the token-expiry policy re-authenticates. If the message carries only
    /// the <see cref="System.Net.HttpStatusCode"/> enum name ("Unauthorized"/"Forbidden"), it falls through to
    /// the generic retry policy, which retries the dead token forever instead of re-authenticating.
    /// </summary>
    public class DIGAPIResilienceHelperTests
    {
        readonly DIGExceptionHelper exceptionHelper = new();
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Runs a throw-once-then-succeed action through the re-authentication policy wrap and returns how many
        /// times re-authentication was invoked plus the final result.
        /// </summary>
        async Task<(int reauthCount, string result)> RunThrowOnceThenSucceedAsync(string firstThrowMessage)
        {
            int reauthCount = 0;
            Func<Task> reauthenticateAsync = () => { reauthCount++; return Task.CompletedTask; };

            var policyWrap = DIGAPIResilienceHelper.CreateAsyncPolicyWrapForDIGAPICallsWithReauthentication<Exception>(
                configuredTimeoutSeconds: 30,
                exceptionHelper,
                logger,
                reauthenticateAsync);

            var context = DIGAPIResilienceHelper.CreateContextWithMethodName("TestMethod");

            bool thrown = false;
            var result = await policyWrap.ExecuteAsync(_ =>
            {
                if (!thrown)
                {
                    thrown = true;
                    throw new Exception(firstThrowMessage);
                }
                return Task.FromResult("success");
            }, context);

            return (reauthCount, result);
        }

        [Theory]
        [InlineData("DIG API returned 401 (Unauthorized): Invalid or rejected token")]
        [InlineData("DIG API returned 403 (Forbidden): Invalid or rejected token")]
        public async Task NumericStatusCodeMessage_TriggersReauthentication(string message)
        {
            var (reauthCount, result) = await RunThrowOnceThenSucceedAsync(message);

            Assert.Equal(1, reauthCount);
            Assert.Equal("success", result);
        }

        [Fact]
        public async Task EnumNameOnlyMessage_DoesNotTriggerReauthentication()
        {
            // Pre-fix message format. Routes to the generic retry policy (which against a real expired token
            // would retry the dead token forever) rather than the token-expiry/reauth policy. Guards against
            // reverting the throw-site message back to the bare enum name.
            var (reauthCount, result) = await RunThrowOnceThenSucceedAsync("DIG API returned Unauthorized: Invalid or rejected token");

            Assert.Equal(0, reauthCount);
            Assert.Equal("success", result);
        }
    }
}
