using System;
using MyGeotabAPIAdapter.DIGAPI;
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
    }
}
