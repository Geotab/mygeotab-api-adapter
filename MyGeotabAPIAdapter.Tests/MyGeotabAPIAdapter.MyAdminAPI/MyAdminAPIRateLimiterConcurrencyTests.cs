using MyGeotabAPIAdapter.MyAdminAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Concurrency tests for the <see cref="MyAdminAPIRateLimiter"/> class.
    /// </summary>
    public class MyAdminAPIRateLimiterConcurrencyTests
    {
        private readonly ITestOutputHelper output;

        public MyAdminAPIRateLimiterConcurrencyTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task WaitForPermitAsync_ConcurrentCalls_AllComplete()
        {
            // Arrange
            var rateLimiter = new MyAdminAPIRateLimiter(1000, 10000); // High limits for this test
            const int concurrentCalls = 50;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            // Act
            var tasks = Enumerable.Range(0, concurrentCalls)
                .Select(_ => rateLimiter.WaitForPermitAsync("ConcurrentMethod", cts.Token))
                .ToList();

            await Task.WhenAll(tasks);

            var stats = rateLimiter.GetStatistics("ConcurrentMethod");

            // Assert
            Assert.Equal(concurrentCalls, stats.RequestsInLastMinute);
            Assert.Equal(concurrentCalls, stats.RequestsInLastDay);
            output.WriteLine($"All {concurrentCalls} concurrent calls completed. Requests tracked: {stats.RequestsInLastMinute}");
        }

        [Fact]
        public async Task WaitForPermitAsync_ConcurrentCallsToDifferentMethods_AllComplete()
        {
            // Arrange
            var rateLimiter = new MyAdminAPIRateLimiter(1000, 10000);
            const int callsPerMethod = 20;
            var methods = new[] { "MethodA", "MethodB", "MethodC" };
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            // Act
            var tasks = new List<Task>();
            foreach (var method in methods)
            {
                for (int i = 0; i < callsPerMethod; i++)
                {
                    tasks.Add(rateLimiter.WaitForPermitAsync(method, cts.Token));
                }
            }

            await Task.WhenAll(tasks);

            var statsAll = rateLimiter.GetStatistics();

            // Assert
            Assert.Equal(methods.Length, statsAll.MethodStatistics.Count);
            foreach (var method in methods)
            {
                Assert.Equal(callsPerMethod, statsAll.MethodStatistics[method].RequestsInLastMinute);
            }
            output.WriteLine($"Completed {callsPerMethod} calls each for {methods.Length} methods");
        }

        [Fact]
        public async Task WaitForPermitAsync_UnderLimit_CompletesWithoutDelay()
        {
            // Arrange - Set limit high enough that requests complete without delay
            var rateLimiter = new MyAdminAPIRateLimiter(100, 10000);
            const int requestCount = 10; // Well under the effective limit of 95
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var startTime = DateTime.UtcNow;

            // Act - Make requests sequentially
            for (int i = 0; i < requestCount; i++)
            {
                await rateLimiter.WaitForPermitAsync("FastMethod", cts.Token);
            }

            var elapsed = DateTime.UtcNow - startTime;
            var stats = rateLimiter.GetStatistics("FastMethod");

            // Assert - All requests should complete quickly and be tracked
            Assert.Equal(requestCount, stats.RequestsInLastMinute);
            Assert.Equal(requestCount, stats.RequestsInLastDay);
            Assert.True(elapsed.TotalSeconds < 5, $"Expected requests to complete quickly, but took {elapsed.TotalSeconds:F1}s");
            output.WriteLine($"Completed {requestCount} requests in {elapsed.TotalMilliseconds:F0}ms. " +
                            $"Requests in last minute: {stats.RequestsInLastMinute}");
        }

        [Fact]
        public void GetStatistics_ReflectsLimitUtilization()
        {
            // Arrange - Test that statistics correctly show utilization
            var rateLimiter = new MyAdminAPIRateLimiter(100, 1000);

            // Act - Record some rate limit responses to create tracker entries
            rateLimiter.RecordRateLimitResponse("UtilMethod", 1);

            var stats = rateLimiter.GetStatistics("UtilMethod");

            // Assert - Verify statistics structure
            Assert.Equal("UtilMethod", stats.MethodName);
            Assert.Equal(95, stats.MaxRequestsPerMinute); // 100 * 0.95
            Assert.Equal(950, stats.MaxRequestsPerDay);   // 1000 * 0.95
            Assert.True(stats.IsInBackoff);
            output.WriteLine($"Method stats: {stats.MaxRequestsPerMinute}/min, {stats.MaxRequestsPerDay}/day, InBackoff: {stats.IsInBackoff}");
        }

        [Fact]
        public async Task RecordRateLimitResponse_BlocksSubsequentRequests()
        {
            // Arrange
            var rateLimiter = new MyAdminAPIRateLimiter();
            const int backoffSeconds = 2; // Short backoff for testing

            // Record a rate limit response
            rateLimiter.RecordRateLimitResponse("BackoffMethod", backoffSeconds);

            var startTime = DateTime.UtcNow;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // Act - Try to get a permit (should wait for backoff)
            await rateLimiter.WaitForPermitAsync("BackoffMethod", cts.Token);

            var elapsed = DateTime.UtcNow - startTime;

            // Assert - Should have waited at least the backoff duration
            Assert.True(elapsed.TotalSeconds >= backoffSeconds,
                $"Expected to wait at least {backoffSeconds}s, but only waited {elapsed.TotalSeconds:F1}s");
            output.WriteLine($"Request was delayed by backoff for {elapsed.TotalSeconds:F1} seconds");
        }
    }
}