using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MyGeotabAPIAdapter.DIGAPI;
using MyGeotabAPIAdapter.Logging;
using Xunit;

namespace MyGeotabAPIAdapter.Tests.GeotabDIGAdapter.Core.DIGAPI
{
    /// <summary>
    /// Coverage for the proactive token refresh-ahead decision (defense-in-depth). Complements the reactive
    /// 401/403 re-authentication retry policy verified in <see cref="DIGAPIResilienceHelperTests"/>.
    /// </summary>
    public class DIGAPIHelperTests
    {
        static readonly TimeSpan Buffer = TimeSpan.FromMinutes(DIGAPIHelper.ProactiveTokenRefreshBufferMinutes);

        [Fact]
        public void ShouldProactivelyRefreshBearerToken_NullToken_ReturnsFalse()
        {
            Assert.False(DIGAPIHelper.ShouldProactivelyRefreshBearerToken(null, Buffer));
        }

        [Fact]
        public void ShouldProactivelyRefreshBearerToken_NoKnownExpiry_ReturnsFalse()
        {
            // Expires == default(DateTime): expiry unknown, so defer to the reactive policy rather than churn tokens.
            var token = new DIGToken { TokenString = "t" };
            Assert.False(DIGAPIHelper.ShouldProactivelyRefreshBearerToken(token, Buffer));
        }

        [Fact]
        public void ShouldProactivelyRefreshBearerToken_FarFromExpiry_ReturnsFalse()
        {
            var token = new DIGToken { TokenString = "t", Expires = DateTime.UtcNow.AddHours(10) };
            Assert.False(DIGAPIHelper.ShouldProactivelyRefreshBearerToken(token, Buffer));
        }

        [Fact]
        public void ShouldProactivelyRefreshBearerToken_WithinBuffer_ReturnsTrue()
        {
            var token = new DIGToken { TokenString = "t", Expires = DateTime.UtcNow.AddMinutes(30) };
            Assert.True(DIGAPIHelper.ShouldProactivelyRefreshBearerToken(token, Buffer));
        }

        [Fact]
        public void ShouldProactivelyRefreshBearerToken_AlreadyExpired_ReturnsTrue()
        {
            var token = new DIGToken { TokenString = "t", Expires = DateTime.UtcNow.AddMinutes(-5) };
            Assert.True(DIGAPIHelper.ShouldProactivelyRefreshBearerToken(token, Buffer));
        }

        [Fact]
        public void ShouldProactivelyRefreshBearerToken_ShortLivedToken_BufferCappedAtHalfLifetime_ReturnsFalse()
        {
            // 30-minute token, 20 minutes remaining. The 60-minute absolute buffer would naively say "refresh", but
            // it is capped at half the lifetime (15 minutes); 20 > 15, so no refresh yet — avoids refreshing on
            // every call for short-lived tokens.
            var now = DateTime.UtcNow;
            var token = new DIGToken { TokenString = "t", Created = now.AddMinutes(-10), Expires = now.AddMinutes(20) };
            Assert.False(DIGAPIHelper.ShouldProactivelyRefreshBearerToken(token, Buffer));
        }

        [Fact]
        public void ShouldProactivelyRefreshBearerToken_ShortLivedToken_WithinHalfLifetime_ReturnsTrue()
        {
            // Same 30-minute token, now only 5 minutes remaining (past the 15-minute half-lifetime cap) → refresh.
            var now = DateTime.UtcNow;
            var token = new DIGToken { TokenString = "t", Created = now.AddMinutes(-25), Expires = now.AddMinutes(5) };
            Assert.True(DIGAPIHelper.ShouldProactivelyRefreshBearerToken(token, Buffer));
        }

        // ---- End-to-end escalation on refresh-token rejection ----

        [Theory(Timeout = 30000)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        public async Task RefreshTokenRejected_EscalatesToFullReauthentication_AndPostSucceeds(HttpStatusCode refreshStatus)
        {
            // A rejected refresh token (401/403) must escalate to a full re-login (a second /authentication/authenticate)
            // instead of looping on the dead refresh token, and posting must then succeed. The bearer is near-expiry
            // (triggers the proactive refresh); the refresh token is future-dated so ReauthenticateAsync attempts the
            // refresh (which the server rejects) before falling back to the full login.
            var now = DateTime.UtcNow;
            static string Iso(DateTime dt) => dt.ToString("o", CultureInfo.InvariantCulture);
            var authJson =
                "{\"data\":{\"authenticated\":true," +
                "\"bearerToken\":{\"tokenString\":\"bearer-1\",\"expires\":\"" + Iso(now.AddMinutes(2)) + "\"}," +
                "\"refreshToken\":{\"tokenString\":\"refresh-1\",\"expires\":\"" + Iso(now.AddHours(1)) + "\"}}}";

            using var handler = new SequencedDigHandler(authJson, refreshStatus);
            using var httpClient = new HttpClient(handler);
            var helper = new DIGAPIHelper(new DIGExceptionHelper(), rateLimiter: null, httpClient: httpClient);

            await helper.AuthenticateDIGAPIAsync("https://dig.test.local", "user", "pass");
            var result = await helper.PostRecordsAsync(new List<object> { new { placeholder = "record" } });

            Assert.Equal(1, handler.RefreshCount);   // exactly one refresh attempt -- no infinite loop
            Assert.Equal(2, handler.AuthCount);      // initial authentication + full re-login escalation
            Assert.Equal(1, handler.RecordsCount);   // the post proceeded after escalation
            Assert.True(result.IsSuccess);           // pipeline un-wedged
        }

        // ---- Error-body extraction (non-JSON error responses) ----

        [Fact]
        public void ExtractErrorDetail_JsonWithErrors_JoinsThem()
        {
            var detail = DIGAPIHelper.ExtractErrorDetail("{\"Error\":[\"first\",\"second\"]}", out var raw);

            Assert.Equal("first, second", detail);
            Assert.Null(raw);   // parseable body -- nothing for the caller to log
        }

        [Theory]
        [InlineData("[400]")]
        [InlineData("[401]")]
        [InlineData("[403]")]
        [InlineData("[413]")]
        [InlineData("[429]")]
        public void ExtractErrorDetail_ErrorTextNamingABracketedIndex_CannotCarryAStatusToken(string token)
        {
            var detail = DIGAPIHelper.ExtractErrorDetail("{\"Error\":[\"Failed processing records" + token + "\"]}", out var raw);

            Assert.DoesNotContain(token, detail);                                       // the thrower's routing token
            Assert.Contains("records(" + token.Trim('[', ']') + ")", detail);           // the text still reads the same
            Assert.Null(raw);
        }

        [Fact]
        public void ExtractErrorDetail_JsonWithoutErrorArray_HandsRawBodyToCaller()
        {
            // DIGAPIResponse.Error defaults to an empty list, so a JSON body carrying no "error" property has
            // nothing to join. That used to yield an empty detail AND log nothing - no JsonException was thrown, so
            // the raw body was never handed back either - which turned a gateway's explanation into
            // "DIG API returned 502 (BadGateway): " with nothing after the colon. It is now routed like a non-JSON
            // body: a short descriptor for the message, the body itself for the log.
            const string body = "{\"message\":\"upstream connect error\"}";

            var detail = DIGAPIHelper.ExtractErrorDetail(body, out var raw);

            Assert.NotEqual(string.Empty, detail);
            Assert.DoesNotContain("upstream", detail);   // detail stays free of unvetted upstream text
            Assert.Equal(body, raw);                     // ...but the body is available for the log
        }

        [Fact]
        public void ExtractErrorDetail_JsonWithEmptyErrorArray_HandsRawBodyToCaller()
        {
            // The same gap reached through the other door: an "error" property that is present but empty.
            const string body = "{\"error\":[]}";

            var detail = DIGAPIHelper.ExtractErrorDetail(body, out var raw);

            Assert.NotEqual(string.Empty, detail);
            Assert.Equal(body, raw);
        }

        [Fact]
        public void ExtractErrorDetail_JsonNullLiteral_KeepsTheBodyOutOfTheDetail()
        {
            // The third door into "parsed, but nothing to say": the body IS valid JSON and deserialises to a
            // null response. That path used to inline the raw body as the detail, which is the one shape of
            // return value the resilience predicates must never see. It could not actually misroute anything -
            // "null" carries no digits - so this test exists to hold the invariant by construction rather than
            // by that arithmetic, which is what stops a future body shape reaching the same branch.
            const string body = "null";

            var detail = DIGAPIHelper.ExtractErrorDetail(body, out var raw);

            Assert.NotEqual(string.Empty, detail);
            Assert.DoesNotContain(body, detail);   // the body must not be the detail...
            Assert.Equal(body, raw);               // ...but it still reaches the caller for logging
        }

        [Fact]
        public void ExtractErrorDetail_NullBody_ReturnsPlaceholder_AndDoesNotThrow()
        {
            // JsonSerializer.Deserialize raises ArgumentNullException for a null input, not JsonException, so
            // before the guard this escaped the method and pre-empted the throw carrying the numeric status
            // code - defeating the routing guarantee the method exists to provide. Unreachable from this
            // solution's callers today; the method is public, so the guarantee should not depend on that.
            var detail = DIGAPIHelper.ExtractErrorDetail(null!, out var raw);

            Assert.Equal(DIGAPIHelper.ExtractErrorDetail(string.Empty, out _), detail);   // treated as an empty body
            Assert.Null(raw);
        }

        [Fact]
        public void ExtractErrorDetail_HtmlBody_DoesNotThrow_AndHandsRawBodyToCaller()
        {
            const string html = "<html><head><title>502 Bad Gateway</title></head><body>nginx</body></html>";

            var detail = DIGAPIHelper.ExtractErrorDetail(html, out var raw);

            Assert.DoesNotContain("<", detail);   // detail must stay free of unvetted upstream text
            Assert.Equal(html, raw);              // ...but the body is still available for the log
        }

        [Theory]
        [InlineData("400")]
        [InlineData("401")]
        [InlineData("403")]
        [InlineData("413")]
        [InlineData("429")]
        public void ExtractErrorDetail_HtmlBodyContainingPredicateDigits_KeepsThemOutOfTheDetail(string digits)
        {
            // Guards the routing hazard: the resilience predicates match these as bare substrings, so an error page
            // that happens to contain one (in a request id, say) must not be able to steer policy selection.
            var html = $"<html><body>Gateway error. Request id: ab{digits}cd</body></html>";

            var detail = DIGAPIHelper.ExtractErrorDetail(html, out var raw);

            Assert.DoesNotContain(digits, detail);
            Assert.Contains(digits, raw);
        }

        [Theory]
        [InlineData(400)]
        [InlineData(401)]
        [InlineData(403)]
        [InlineData(413)]
        [InlineData(429)]
        public void ExtractErrorDetail_BodyWhoseLengthIsAPredicateSequence_KeepsItOutOfTheDetail(int bodyLength)
        {
            // The same routing hazard reached by arithmetic rather than by text. The detail is interpolated into
            // the thrown message, so a descriptor reading "... response body (400 characters) ..." steers policy
            // exactly as a request id containing "400" would - and worse, deterministically: a gateway returns a
            // fixed-shape error page, so an unlucky page length misroutes EVERY upstream error for that
            // deployment. The Theory above cannot see this, because its fixture body is 60 characters long.
            //
            // Both doors into the descriptor are covered: a body that is not JSON, and one that parses but
            // carries no "error" property.
            var expectedLength = bodyLength.ToString(CultureInfo.InvariantCulture);
            var nonJsonBody = new string('x', bodyLength);
            const string jsonEnvelope = "{\"message\":\"\"}";
            var unrecognizedShapeBody = $"{{\"message\":\"{new string('x', bodyLength - jsonEnvelope.Length)}\"}}";

            Assert.Equal(bodyLength, nonJsonBody.Length);            // the fixtures are the length under test...
            Assert.Equal(bodyLength, unrecognizedShapeBody.Length);  // ...or the assertions below prove nothing

            var nonJsonDetail = DIGAPIHelper.ExtractErrorDetail(nonJsonBody, out var nonJsonRaw);
            var unrecognizedShapeDetail = DIGAPIHelper.ExtractErrorDetail(unrecognizedShapeBody, out var unrecognizedShapeRaw);

            Assert.DoesNotContain(expectedLength, nonJsonDetail);
            Assert.DoesNotContain(expectedLength, unrecognizedShapeDetail);
            Assert.NotNull(nonJsonRaw);            // ...and the body still reaches the caller for logging
            Assert.NotNull(unrecognizedShapeRaw);
        }

        [Fact]
        public void ExtractErrorDetail_EmptyBody_ReturnsPlaceholder()
        {
            var detail = DIGAPIHelper.ExtractErrorDetail(string.Empty, out var raw);

            Assert.Equal(DIGAPIHelper.EmptyErrorBodyPlaceholder, detail);
            Assert.Null(raw);
        }

        [Fact]
        public void ExtractErrorDetail_OversizedHtmlBody_TruncatesTheLoggedCopy()
        {
            var html = "<html>" + new string('x', DIGAPIHelper.MaxRawErrorBodyLength * 3) + "</html>";

            DIGAPIHelper.ExtractErrorDetail(html, out var raw);

            Assert.NotNull(raw);
            Assert.Contains("truncated", raw);
            Assert.True(
                raw!.Length < html.Length,
                $"Expected the logged copy to be shorter than the {html.Length}-character body, but it was {raw.Length}.");
        }

        // ---- End-to-end: a non-JSON error body must be retried, not discarded ----

        [Theory(Timeout = 30000)]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.GatewayTimeout)]
        public async Task NonJsonGatewayError_IsRetried_AndPostEventuallySucceeds(HttpStatusCode gatewayStatus)
        {
            // Before the guard, deserializing the HTML body threw a JsonException ahead of the numeric-status-code
            // throw. The general retry policy excludes JsonException, so no policy handled it: the batch was
            // abandoned after a single attempt and dead-lettered. It must now be retried and succeed.
            var now = DateTime.UtcNow;
            static string Iso(DateTime dt) => dt.ToString("o", CultureInfo.InvariantCulture);
            var authJson =
                "{\"data\":{\"authenticated\":true," +
                "\"bearerToken\":{\"tokenString\":\"bearer-1\",\"expires\":\"" + Iso(now.AddHours(10)) + "\"}," +
                "\"refreshToken\":{\"tokenString\":\"refresh-1\",\"expires\":\"" + Iso(now.AddHours(20)) + "\"}}}";

            using var handler = new NonJsonErrorDigHandler(authJson, gatewayStatus, failingRecordCalls: 1);
            using var httpClient = new HttpClient(handler);
            var helper = new DIGAPIHelper(new DIGExceptionHelper(), rateLimiter: null, httpClient: httpClient);

            await helper.AuthenticateDIGAPIAsync("https://dig.test.local", "user", "pass");
            var result = await helper.PostRecordsAsync(new List<object> { new { placeholder = "record" } });

            Assert.Equal(2, handler.RecordsCount);   // the failed attempt was retried rather than abandoned
            Assert.True(result.IsSuccess);           // ...and the records were delivered
        }

        [Theory(Timeout = 30000)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        public async Task NonJsonTokenRejection_StillEscalatesToReauthentication(HttpStatusCode rejectionStatus)
        {
            // Regression guard for the reactive re-authentication path: if a 401/403 arrives with a non-JSON body,
            // the JsonException used to pre-empt the numeric-status-code throw, so the re-authentication that the
            // token-expiry policy exists to perform never fired. The bearer token is long-lived here, so nothing
            // other than the rejection itself can trigger re-authentication.
            var now = DateTime.UtcNow;
            static string Iso(DateTime dt) => dt.ToString("o", CultureInfo.InvariantCulture);
            var authJson =
                "{\"data\":{\"authenticated\":true," +
                "\"bearerToken\":{\"tokenString\":\"bearer-1\",\"expires\":\"" + Iso(now.AddHours(10)) + "\"}," +
                "\"refreshToken\":{\"tokenString\":\"refresh-1\",\"expires\":\"" + Iso(now.AddHours(20)) + "\"}}}";

            using var handler = new NonJsonErrorDigHandler(authJson, rejectionStatus, failingRecordCalls: 1);
            using var httpClient = new HttpClient(handler);
            var helper = new DIGAPIHelper(new DIGExceptionHelper(), rateLimiter: null, httpClient: httpClient);

            await helper.AuthenticateDIGAPIAsync("https://dig.test.local", "user", "pass");
            var result = await helper.PostRecordsAsync(new List<object> { new { placeholder = "record" } });

            Assert.True(
                handler.RefreshCount + handler.AuthCount > 1,
                "Expected the non-JSON 401/403 to trigger a token refresh or a full re-login, but neither occurred.");
            Assert.Equal(2, handler.RecordsCount);
            Assert.True(result.IsSuccess);
        }

        // ---- Application errors inside a successful HTTP response ----

        const string ApplicationErrorNamingABracketedIndex = "{\"data\":null,\"error\":[\"Temporary failure at records[400]\"]}";

        [Fact(Timeout = 30000)]
        public async Task AuthenticationApplicationErrorNamingABracketedIndex_IsRetried_AndAuthenticationSucceeds()
        {
            // An HTTP 200 whose body carries an error array is thrown as "DIG API authentication failed: <errors>". The
            // errors are upstream text, and "[400]" is a routing token the non-timeout retry policy excludes: before
            // the text was neutralised, an error naming an array element ended authentication after one attempt.
            var authJson = LongLivedAuthJson();

            using var handler = new ApplicationErrorDigHandler(authJson, failingAuthCalls: 1, failingRefreshCalls: 0);
            using var httpClient = new HttpClient(handler);
            var helper = new DIGAPIHelper(new DIGExceptionHelper(), rateLimiter: null, httpClient: httpClient);

            await helper.AuthenticateDIGAPIAsync("https://dig.test.local", "user", "pass");

            Assert.Equal(2, handler.AuthCount);   // the application error was retried rather than ending authentication
            Assert.True(helper.DIGAPIIsAuthenticated);
        }

        [Fact(Timeout = 30000)]
        public async Task RefreshApplicationErrorNamingABracketedIndex_IsRetried_NotEscalatedToFullReauthentication()
        {
            // The same for a token refresh whose HTTP 200 carries no tokens and an error array. Before the text was
            // neutralised, "[400]" in it ended the refresh after one attempt and ReauthenticateAsync fell back to a full
            // re-login. The bearer is near expiry so the post triggers the proactive refresh.
            var now = DateTime.UtcNow;
            static string Iso(DateTime dt) => dt.ToString("o", CultureInfo.InvariantCulture);
            var authJson =
                "{\"data\":{\"authenticated\":true," +
                "\"bearerToken\":{\"tokenString\":\"bearer-1\",\"expires\":\"" + Iso(now.AddMinutes(2)) + "\"}," +
                "\"refreshToken\":{\"tokenString\":\"refresh-1\",\"expires\":\"" + Iso(now.AddHours(1)) + "\"}}}";

            using var handler = new ApplicationErrorDigHandler(authJson, failingAuthCalls: 0, failingRefreshCalls: 1);
            using var httpClient = new HttpClient(handler);
            var helper = new DIGAPIHelper(new DIGExceptionHelper(), rateLimiter: null, httpClient: httpClient);

            await helper.AuthenticateDIGAPIAsync("https://dig.test.local", "user", "pass");
            var result = await helper.PostRecordsAsync(new List<object> { new { placeholder = "record" } });

            Assert.Equal(2, handler.RefreshCount);   // the refresh was retried...
            Assert.Equal(1, handler.AuthCount);      // ...rather than escalated to a full re-login
            Assert.True(result.IsSuccess);
        }

        static string LongLivedAuthJson()
        {
            var now = DateTime.UtcNow;
            static string Iso(DateTime dt) => dt.ToString("o", CultureInfo.InvariantCulture);
            return "{\"data\":{\"authenticated\":true," +
                   "\"bearerToken\":{\"tokenString\":\"bearer-1\",\"expires\":\"" + Iso(now.AddHours(10)) + "\"}," +
                   "\"refreshToken\":{\"tokenString\":\"refresh-1\",\"expires\":\"" + Iso(now.AddHours(20)) + "\"}}}";
        }

        /// <summary>
        /// Test double for the DIG API that answers the first <c>failingAuthCalls</c> authentications and the first
        /// <c>failingRefreshCalls</c> token refreshes with HTTP 200 and a body carrying no data and an error array that
        /// names a bracketed index, then succeeds. Record posts always succeed.
        /// </summary>
        sealed class ApplicationErrorDigHandler : HttpMessageHandler
        {
            readonly string authJson;
            readonly int failingAuthCalls;
            readonly int failingRefreshCalls;
            public int AuthCount;
            public int RefreshCount;
            public int RecordsCount;

            public ApplicationErrorDigHandler(string authJson, int failingAuthCalls, int failingRefreshCalls)
            {
                this.authJson = authJson;
                this.failingAuthCalls = failingAuthCalls;
                this.failingRefreshCalls = failingRefreshCalls;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri!.AbsolutePath;
                HttpResponseMessage response;

                if (path.Contains("refresh-token"))
                {
                    RefreshCount++;
                    response = Json(HttpStatusCode.OK, RefreshCount <= failingRefreshCalls
                        ? ApplicationErrorNamingABracketedIndex
                        : "{\"data\":{\"bearerToken\":{\"tokenString\":\"bearer-2\"},\"refreshToken\":{\"tokenString\":\"refresh-2\"}}}");
                }
                else if (path.Contains("authenticate"))
                {
                    AuthCount++;
                    response = Json(HttpStatusCode.OK, AuthCount <= failingAuthCalls ? ApplicationErrorNamingABracketedIndex : authJson);
                }
                else if (path.Contains("/records"))
                {
                    RecordsCount++;
                    response = Json(HttpStatusCode.OK, "{\"data\":\"11111111-1111-1111-1111-111111111111\"}");
                }
                else
                {
                    response = Json(HttpStatusCode.InternalServerError, "{\"Error\":[\"unexpected path\"]}");
                }

                return Task.FromResult(response);
            }

            static HttpResponseMessage Json(HttpStatusCode status, string body) =>
                new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }

        /// <summary>
        /// Test double for the DIG API that answers the first <c>failingRecordCalls</c> record-post attempts with a
        /// configurable status and an HTML (non-JSON) body, as an intervening gateway would, then succeeds.
        /// Authentication and token refresh always succeed.
        /// </summary>
        sealed class NonJsonErrorDigHandler : HttpMessageHandler
        {
            const string HtmlErrorBody =
                "<html><head><title>Gateway Error</title></head><body><center><h1>Gateway Error</h1></center></body></html>";

            readonly string authJson;
            readonly HttpStatusCode errorStatus;
            readonly int failingRecordCalls;
            public int AuthCount;
            public int RefreshCount;
            public int RecordsCount;

            public NonJsonErrorDigHandler(string authJson, HttpStatusCode errorStatus, int failingRecordCalls)
            {
                this.authJson = authJson;
                this.errorStatus = errorStatus;
                this.failingRecordCalls = failingRecordCalls;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri!.AbsolutePath;
                HttpResponseMessage response;

                if (path.Contains("refresh-token"))
                {
                    RefreshCount++;
                    response = Body(
                        HttpStatusCode.OK,
                        "{\"data\":{\"bearerToken\":{\"tokenString\":\"bearer-2\"},\"refreshToken\":{\"tokenString\":\"refresh-2\"}}}",
                        "application/json");
                }
                else if (path.Contains("authenticate"))
                {
                    AuthCount++;
                    response = Body(HttpStatusCode.OK, authJson, "application/json");
                }
                else if (path.Contains("/records"))
                {
                    RecordsCount++;
                    response = RecordsCount <= failingRecordCalls
                        ? Body(errorStatus, HtmlErrorBody, "text/html")
                        : Body(HttpStatusCode.OK, "{\"data\":\"11111111-1111-1111-1111-111111111111\"}", "application/json");
                }
                else
                {
                    response = Body(HttpStatusCode.InternalServerError, "{\"Error\":[\"unexpected path\"]}", "application/json");
                }

                return Task.FromResult(response);
            }

            static HttpResponseMessage Body(HttpStatusCode status, string body, string mediaType) =>
                new(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) };
        }

        /// <summary>
        /// Test double for the DIG API: routes by URL path, records per-endpoint call counts, and returns a
        /// configurable status for the token-refresh endpoint. Authentication and record-post always succeed.
        /// </summary>
        sealed class SequencedDigHandler : HttpMessageHandler
        {
            readonly string authJson;
            readonly HttpStatusCode refreshStatus;
            public int AuthCount;
            public int RefreshCount;
            public int RecordsCount;

            public SequencedDigHandler(string authJson, HttpStatusCode refreshStatus)
            {
                this.authJson = authJson;
                this.refreshStatus = refreshStatus;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri!.AbsolutePath;
                HttpResponseMessage response;

                if (path.Contains("refresh-token"))
                {
                    RefreshCount++;
                    response = Json(refreshStatus, "{\"Error\":[\"Token refresh failed\"]}");
                }
                else if (path.Contains("authenticate"))
                {
                    AuthCount++;
                    response = Json(HttpStatusCode.OK, authJson);
                }
                else if (path.Contains("/records"))
                {
                    RecordsCount++;
                    response = Json(HttpStatusCode.OK, "{\"data\":\"11111111-1111-1111-1111-111111111111\"}");
                }
                else
                {
                    response = Json(HttpStatusCode.InternalServerError, "{\"Error\":[\"unexpected path\"]}");
                }

                return Task.FromResult(response);
            }

            static HttpResponseMessage Json(HttpStatusCode status, string body) =>
                new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
