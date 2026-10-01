using System;
using System.Threading.Tasks;
using MyGeotabAPIAdapter.DIGAPI;
using MyGeotabAPIAdapter.Logging;
using NLog;
using Polly;
using Xunit;

namespace MyGeotabAPIAdapter.Tests.GeotabDIGAdapter.Core.DIGAPI
{
    /// <summary>
    /// Regression coverage for the DIG token-expiry re-authentication contract. A thrown DIG API exception message must carry the numeric HTTP
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
        [InlineData("DIG API returned [401] (Unauthorized): Invalid or rejected token")]
        [InlineData("DIG API returned [403] (Forbidden): Invalid or rejected token")]
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

        // ---- DIG *refresh-token* rejection (distinct from the data-call re-auth path above) ----
        // The token-refresh call runs under the NON-reauth wrap (no token-expiry policy). These guard that a numeric
        // 401/403 refresh rejection escapes that wrap immediately -- so ReauthenticateAsync's catch can escalate to a
        // full re-login -- whereas the old bare-enum-name message is retried (the infinite loop the fix removes).

        [Theory]
        [InlineData("DIG API token refresh failed with status [401] (Unauthorized): {\"Error\":[\"Token refresh failed\"]}")]
        [InlineData("DIG API token refresh failed with status [403] (Forbidden): {\"Error\":[\"Token refresh failed\"]}")]
        public async Task NonReauthWrap_NumericTokenRejection_SurfacesImmediately(string message)
        {
            int calls = 0;
            var policyWrap = DIGAPIResilienceHelper.CreateAsyncPolicyWrapForDIGAPICalls<Exception>(30, exceptionHelper, logger);
            var context = DIGAPIResilienceHelper.CreateContextWithMethodName("RefreshToken");

            Func<Context, Task> action = _ =>
            {
                calls++;
                throw new Exception(message);
            };

            await Assert.ThrowsAnyAsync<Exception>(() => policyWrap.ExecuteAsync(action, context));

            Assert.Equal(1, calls); // not retried -> escapes so ReauthenticateAsync's catch runs the full re-login
        }

        [Fact]
        public async Task NonReauthWrap_EnumNameOnlyTokenRejection_IsRetried()
        {
            // Pre-fix message (bare enum name, no "401"/"403"): the non-reauth wrap's generic retry policy handles
            // it -- against a real dead refresh token this is the infinite loop the fix removes. Guards against
            // reverting the refresh throw-site back to the bare enum name.
            int calls = 0;
            bool thrown = false;
            var policyWrap = DIGAPIResilienceHelper.CreateAsyncPolicyWrapForDIGAPICalls<Exception>(30, exceptionHelper, logger);
            var context = DIGAPIResilienceHelper.CreateContextWithMethodName("RefreshToken");

            var result = await policyWrap.ExecuteAsync(_ =>
            {
                calls++;
                if (!thrown)
                {
                    thrown = true;
                    throw new Exception("DIG API token refresh failed with status Unauthorized: {\"Error\":[\"Token refresh failed\"]}");
                }
                return Task.FromResult("success");
            }, context);

            Assert.Equal(2, calls);      // retried once, then succeeded
            Assert.Equal("success", result);
        }

        // ---- Upstream text must not steer policy (round-4 adversarial review) ----
        // ExtractErrorDetail's second return path joins the DIG error array verbatim, and the three
        // DIGConnectionException throw sites interpolate the whole response body. While the predicates matched
        // BARE digits, a 5xx whose text happened to contain 400, 401, 403, 413 or 429 anywhere - a batch id, an
        // offset, a queue depth, an epoch - was excluded from the general retry policy and matched by no other
        // one, and Policy.WrapAsync has no catch-all member. That meant one attempt and then the caller's
        // dead-letter path, repeatably rather than occasionally, because a given deployment's error pages are a
        // fixed shape. The predicates now match a delimited "[401]" that only the thrower emits.

        [Theory]
        [InlineData("DIG API returned [500] (InternalServerError): Internal error persisting batch 41329-b")] // 413
        [InlineData("DIG API returned [502] (BadGateway): upstream rejected at offset 8401")]                 // 401
        [InlineData("DIG API returned [503] (ServiceUnavailable): queue depth 4290 exceeded")]                // 429
        [InlineData("DIG API returned [500] (InternalServerError): trace 14003f on shard 4132")]        // 400 and 413
        public async Task ServerErrorCarryingAStatusCodeDigitRunInItsText_IsStillRetried(string message)
        {
            int calls = 0;
            bool thrown = false;
            var policyWrap = DIGAPIResilienceHelper.CreateAsyncPolicyWrapForDIGAPICalls<Exception>(30, exceptionHelper, logger);
            var context = DIGAPIResilienceHelper.CreateContextWithMethodName("PostRecords");

            var result = await policyWrap.ExecuteAsync(_ =>
            {
                calls++;
                if (!thrown)
                {
                    thrown = true;
                    throw new Exception(message);
                }
                return Task.FromResult("success");
            }, context);

            Assert.Equal(2, calls);   // retried once, then succeeded - handled by a policy, not dead-lettered
            Assert.Equal("success", result);
        }

        // ---- ...nor can it produce the delimited token itself (Part B review, C2-3) ----
        // Delimiting the token moved routing onto text the thrower emits, but the DIG error array is still joined
        // into the message, and an error naming an array element - "records[400]" - contains "[400]". A 5xx
        // carrying it was excluded from the general retry policy, matched nothing else, and was dead-lettered after
        // one attempt. The message is built here exactly as PostRecordsAsync builds it, from a DIG error body.

        [Theory]
        [InlineData("{\"error\":[\"Failed processing records[400]\"]}")]
        [InlineData("{\"error\":[\"Payload element [413] could not be stored\"]}")]
        [InlineData("{\"error\":[\"shard[401] unavailable\", \"retry [403] later\"]}")]
        [InlineData("{\"error\":[\"records[429] rejected\"]}")]
        public async Task ServerErrorWhoseDigErrorTextNamesABracketedIndex_IsStillRetried(string errorBody)
        {
            var message = $"DIG API returned [500] (InternalServerError): {DIGAPIHelper.ExtractErrorDetail(errorBody, out _)}";
            int calls = 0;
            bool thrown = false;
            var policyWrap = DIGAPIResilienceHelper.CreateAsyncPolicyWrapForDIGAPICalls<Exception>(30, exceptionHelper, logger);
            var context = DIGAPIResilienceHelper.CreateContextWithMethodName("PostRecords");

            var result = await policyWrap.ExecuteAsync(_ =>
            {
                calls++;
                if (!thrown)
                {
                    thrown = true;
                    throw new Exception(message);
                }
                return Task.FromResult("success");
            }, context);

            Assert.Equal(2, calls);   // retried once, then succeeded - handled by the general retry policy
            Assert.Equal("success", result);
        }
    }
}
