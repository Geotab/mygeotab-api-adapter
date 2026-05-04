using MyGeotabAPIAdapter.MyAdminAPI;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="MyAdminAPIRateLimiter"/> class.
    /// </summary>
    public class MyAdminAPIRateLimiterTests
    {
        private readonly ITestOutputHelper output;

        public MyAdminAPIRateLimiterTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Constructor_WithDefaultValues_InitializesCorrectly()
        {
            // Arrange & Act
            var rateLimiter = new MyAdminAPIRateLimiter();
            var stats = rateLimiter.GetStatistics();

            // Assert
            Assert.Equal(5000, stats.MaxRequestsPerMinute);
            Assert.Equal(100000, stats.MaxRequestsPerDay);
            // Effective limits should be 95% of max
            Assert.Equal(4750, stats.EffectiveRequestsPerMinute);
            Assert.Equal(95000, stats.EffectiveRequestsPerDay);
            output.WriteLine($"Default limits: {stats.MaxRequestsPerMinute}/min, {stats.MaxRequestsPerDay}/day");
        }

        [Theory]
        [InlineData(100, 1000)]
        [InlineData(500, 5000)]
        [InlineData(1000, 10000)]
        public void Constructor_WithCustomValues_InitializesCorrectly(int perMinute, int perDay)
        {
            // Arrange & Act
            var rateLimiter = new MyAdminAPIRateLimiter(perMinute, perDay);
            var stats = rateLimiter.GetStatistics();

            // Assert
            Assert.Equal(perMinute, stats.MaxRequestsPerMinute);
            Assert.Equal(perDay, stats.MaxRequestsPerDay);
            Assert.Equal((int)(perMinute * 0.95), stats.EffectiveRequestsPerMinute);
            Assert.Equal((int)(perDay * 0.95), stats.EffectiveRequestsPerDay);
            output.WriteLine($"Custom limits: {stats.MaxRequestsPerMinute}/min, {stats.MaxRequestsPerDay}/day");
        }

        [Theory]
        [InlineData(0, 1000, 5000, 1000)] // Invalid perMinute defaults to 5000
        [InlineData(-1, 1000, 5000, 1000)] // Negative perMinute defaults to 5000
        [InlineData(100, 0, 100, 100000)] // Invalid perDay defaults to 100000
        [InlineData(100, -1, 100, 100000)] // Negative perDay defaults to 100000
        public void Constructor_WithInvalidValues_UsesDefaults(int perMinute, int perDay, int expectedPerMinute, int expectedPerDay)
        {
            // Arrange & Act
            var rateLimiter = new MyAdminAPIRateLimiter(perMinute, perDay);
            var stats = rateLimiter.GetStatistics();

            // Assert
            Assert.Equal(expectedPerMinute, stats.MaxRequestsPerMinute);
            Assert.Equal(expectedPerDay, stats.MaxRequestsPerDay);
            output.WriteLine($"Input: {perMinute}/min, {perDay}/day -> Actual: {stats.MaxRequestsPerMinute}/min, {stats.MaxRequestsPerDay}/day");
        }

        [Fact]
        public async Task WaitForPermitAsync_WithValidMethodName_CompletesSuccessfully()
        {
            // Arrange
            var rateLimiter = new MyAdminAPIRateLimiter();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Act
            await rateLimiter.WaitForPermitAsync("TestMethod", cts.Token);
            var stats = rateLimiter.GetStatistics("TestMethod");

            // Assert
            Assert.Equal(1, stats.RequestsInLastMinute);
            Assert.Equal(1, stats.RequestsInLastDay);
            output.WriteLine($"Requests after one call: {stats.RequestsInLastMinute}/min, {stats.RequestsInLastDay}/day");
        }

        [Fact]
        public async Task WaitForPermitAsync_WithNullMethodName_ThrowsArgumentNullException()
        {
            // Arrange
            var rateLimiter = new MyAdminAPIRateLimiter();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => rateLimiter.WaitForPermitAsync(null));
            output.WriteLine("Correctly threw ArgumentNullException for null method name");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task WaitForPermitAsync_WithEmptyOrWhitespaceMethodName_ThrowsArgumentException(string methodName)
        {
            // Arrange
            var rateLimiter = new MyAdminAPIRateLimiter();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => rateLimiter.WaitForPermitAsync(methodName));
            output.WriteLine($"Correctly threw ArgumentException for method name: '{methodName}'");
        }

        [Fact]
        public async Task WaitForPermitAsync_TracksMethodsSeparately()
        {
            // Arrange
            var rateLimiter = new MyAdminAPIRateLimiter();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Act - Make requests to different methods
            await rateLimiter.WaitForPermitAsync("MethodA", cts.Token);
            await rateLimiter.WaitForPermitAsync("MethodA", cts.Token);
            await rateLimiter.WaitForPermitAsync("MethodB", cts.Token);

            var statsA = rateLimiter.GetStatistics("MethodA");
            var statsB = rateLimiter.GetStatistics("MethodB");
            var statsAll = rateLimiter.GetStatistics();

            // Assert
            Assert.Equal(2, statsA.RequestsInLastMinute);
            Assert.Equal(1, statsB.RequestsInLastMinute);
            Assert.Equal(2, statsAll.MethodStatistics.Count);
            output.WriteLine($"MethodA: {statsA.RequestsInLastMinute}, MethodB: {statsB.RequestsInLastMinute}");
        }

        [Fact]
        public void GetStatistics_ForUnknownMethod_ReturnsEmptyStatistics()
        {
            // Arrange
            var rateLimiter = new MyAdminAPIRateLimiter();

            // Act
            var stats = rateLimiter.GetStatistics("NonExistentMethod");

            // Assert
            Assert.Equal("NonExistentMethod", stats.MethodName);
            Assert.Equal(0, stats.RequestsInLastMinute);
            Assert.Equal(0, stats.RequestsInLastDay);
            Assert.False(stats.IsInBackoff);
            output.WriteLine($"Unknown method stats: {stats.RequestsInLastMinute}/min, {stats.RequestsInLastDay}/day");
        }

        [Fact]
        public void GetStatistics_WithNullMethodName_ThrowsArgumentNullException()
        {
            // Arrange
            var rateLimiter = new MyAdminAPIRateLimiter();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => rateLimiter.GetStatistics(null));
            output.WriteLine("Correctly threw ArgumentNullException for null method name");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GetStatistics_WithEmptyOrWhitespaceMethodName_ThrowsArgumentException(string methodName)
        {
            // Arrange
            var rateLimiter = new MyAdminAPIRateLimiter();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => rateLimiter.GetStatistics(methodName));
            output.WriteLine($"Correctly threw ArgumentException for method name: '{methodName}'");
        }

        [Fact]
        public void RecordRateLimitResponse_WithRetryAfter_SetsBackoff()
        {
            // Arrange
            var rateLimiter = new MyAdminAPIRateLimiter();
            const int retryAfterSeconds = 30;

            // Act
            rateLimiter.RecordRateLimitResponse("TestMethod", retryAfterSeconds);
            var stats = rateLimiter.GetStatistics("TestMethod");

            // Assert
            Assert.True(stats.IsInBackoff);
            Assert.True(stats.BackoffRemainingSeconds > 0);
            Assert.True(stats.BackoffRemainingSeconds <= retryAfterSeconds + 2); // +1 buffer + timing tolerance
            output.WriteLine($"Backoff remaining: {stats.BackoffRemainingSeconds:F1} seconds");
        }

        [Fact]
        public void RecordRateLimitResponse_WithoutRetryAfter_UsesDefaultBackoff()
        {
            // Arrange
            var rateLimiter = new MyAdminAPIRateLimiter();

            // Act
            rateLimiter.RecordRateLimitResponse("TestMethod");
            var stats = rateLimiter.GetStatistics("TestMethod");

            // Assert
            Assert.True(stats.IsInBackoff);
            Assert.True(stats.BackoffRemainingSeconds > 0);
            Assert.True(stats.BackoffRemainingSeconds <= 62); // 60 second default + 1 buffer + timing tolerance
            output.WriteLine($"Default backoff remaining: {stats.BackoffRemainingSeconds:F1} seconds");
        }

        [Fact]
        public void RecordRateLimitResponse_WithNullMethodName_ThrowsArgumentNullException()
        {
            // Arrange
            var rateLimiter = new MyAdminAPIRateLimiter();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => rateLimiter.RecordRateLimitResponse(null, 30));
            output.WriteLine("Correctly threw ArgumentNullException for null method name");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void RecordRateLimitResponse_WithEmptyOrWhitespaceMethodName_ThrowsArgumentException(string methodName)
        {
            // Arrange
            var rateLimiter = new MyAdminAPIRateLimiter();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => rateLimiter.RecordRateLimitResponse(methodName, 30));
            output.WriteLine($"Correctly threw ArgumentException for method name: '{methodName}'");
        }

        [Fact]
        public async Task WaitForPermitAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var rateLimiter = new MyAdminAPIRateLimiter();
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                rateLimiter.WaitForPermitAsync("TestMethod", cts.Token));
            output.WriteLine("Correctly threw OperationCanceledException on cancellation");
        }

        [Fact]
        public void GetStatistics_MethodNameIsCaseInsensitive()
        {
            // Arrange
            var rateLimiter = new MyAdminAPIRateLimiter();

            // Act - Record to lowercase, retrieve with different cases
            rateLimiter.RecordRateLimitResponse("testmethod", 30);
            var stats1 = rateLimiter.GetStatistics("TestMethod");
            var stats2 = rateLimiter.GetStatistics("TESTMETHOD");
            var stats3 = rateLimiter.GetStatistics("testmethod");

            // Assert
            Assert.True(stats1.IsInBackoff);
            Assert.True(stats2.IsInBackoff);
            Assert.True(stats3.IsInBackoff);
            output.WriteLine("Method name matching is case-insensitive");
        }
    }
}