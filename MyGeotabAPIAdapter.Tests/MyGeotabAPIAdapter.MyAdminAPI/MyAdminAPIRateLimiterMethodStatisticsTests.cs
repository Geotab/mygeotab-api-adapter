using MyGeotabAPIAdapter.MyAdminAPI;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Test data for <see cref="MyAdminAPIRateLimiterMethodStatisticsTests.UtilizationPercent_CalculatesCorrectly"/>.
    /// </summary>
    public class UtilizationPercentTestData : TheoryData<int, int, int, int, double, double>
    {
        public UtilizationPercentTestData()
        {
            // requestsInLastMinute, requestsInLastDay, maxPerMinute, maxPerDay, expectedMinuteUtil, expectedDayUtil
            Add(0, 0, 100, 1000, 0.0, 0.0);
            Add(50, 500, 100, 1000, 50.0, 50.0);
            Add(100, 1000, 100, 1000, 100.0, 100.0);
            Add(25, 250, 100, 1000, 25.0, 25.0);
            Add(75, 750, 100, 1000, 75.0, 75.0);
            Add(1, 1, 100, 1000, 1.0, 0.1);
        }
    }

    /// <summary>
    /// Unit tests for the <see cref="MyAdminAPIRateLimiterMethodStatistics"/> class.
    /// </summary>
    public class MyAdminAPIRateLimiterMethodStatisticsTests
    {
        private readonly ITestOutputHelper output;

        public MyAdminAPIRateLimiterMethodStatisticsTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [ClassData(typeof(UtilizationPercentTestData))]
        public void UtilizationPercent_CalculatesCorrectly(
            int requestsInLastMinute,
            int requestsInLastDay,
            int maxPerMinute,
            int maxPerDay,
            double expectedMinuteUtil,
            double expectedDayUtil)
        {
            // Arrange
            var stats = new MyAdminAPIRateLimiterMethodStatistics
            {
                MethodName = "TestMethod",
                RequestsInLastMinute = requestsInLastMinute,
                RequestsInLastDay = requestsInLastDay,
                MaxRequestsPerMinute = maxPerMinute,
                MaxRequestsPerDay = maxPerDay,
                IsInBackoff = false,
                BackoffRemainingSeconds = 0
            };

            // Act & Assert
            Assert.Equal(expectedMinuteUtil, stats.MinuteUtilizationPercent, precision: 2);
            Assert.Equal(expectedDayUtil, stats.DayUtilizationPercent, precision: 2);
            output.WriteLine($"Requests: {requestsInLastMinute}/min, {requestsInLastDay}/day -> Utilization: {stats.MinuteUtilizationPercent:F1}%/min, {stats.DayUtilizationPercent:F1}%/day");
        }

        [Fact]
        public void UtilizationPercent_WithZeroMax_ReturnsZero()
        {
            // Arrange
            var stats = new MyAdminAPIRateLimiterMethodStatistics
            {
                MethodName = "TestMethod",
                RequestsInLastMinute = 100,
                RequestsInLastDay = 1000,
                MaxRequestsPerMinute = 0, // Edge case: division by zero protection
                MaxRequestsPerDay = 0,
                IsInBackoff = false,
                BackoffRemainingSeconds = 0
            };

            // Act & Assert
            Assert.Equal(0, stats.MinuteUtilizationPercent);
            Assert.Equal(0, stats.DayUtilizationPercent);
            output.WriteLine("Zero max values correctly return 0% utilization");
        }

        [Fact]
        public void Properties_InitializeCorrectly()
        {
            // Arrange & Act
            var stats = new MyAdminAPIRateLimiterMethodStatistics
            {
                MethodName = "ProvisionDevice",
                RequestsInLastMinute = 100,
                RequestsInLastDay = 5000,
                MaxRequestsPerMinute = 4750,
                MaxRequestsPerDay = 95000,
                IsInBackoff = true,
                BackoffRemainingSeconds = 30.5
            };

            // Assert
            Assert.Equal("ProvisionDevice", stats.MethodName);
            Assert.Equal(100, stats.RequestsInLastMinute);
            Assert.Equal(5000, stats.RequestsInLastDay);
            Assert.Equal(4750, stats.MaxRequestsPerMinute);
            Assert.Equal(95000, stats.MaxRequestsPerDay);
            Assert.True(stats.IsInBackoff);
            Assert.Equal(30.5, stats.BackoffRemainingSeconds);
            output.WriteLine($"All properties initialized correctly for method '{stats.MethodName}'");
        }
    }
}