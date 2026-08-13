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
