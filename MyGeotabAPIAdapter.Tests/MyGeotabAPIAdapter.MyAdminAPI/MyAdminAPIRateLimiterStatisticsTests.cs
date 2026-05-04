using MyGeotabAPIAdapter.MyAdminAPI;
using System.Collections.Generic;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="MyAdminAPIRateLimiterStatistics"/> class.
    /// </summary>
    public class MyAdminAPIRateLimiterStatisticsTests
    {
        private readonly ITestOutputHelper output;

        public MyAdminAPIRateLimiterStatisticsTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Properties_InitializeCorrectly()
        {
            // Arrange
            var methodStats = new Dictionary<string, MyAdminAPIRateLimiterMethodStatistics>
            {
                ["MethodA"] = new MyAdminAPIRateLimiterMethodStatistics
                {
                    MethodName = "MethodA",
                    RequestsInLastMinute = 10,
                    RequestsInLastDay = 100,
                    MaxRequestsPerMinute = 4750,
                    MaxRequestsPerDay = 95000
                },
                ["MethodB"] = new MyAdminAPIRateLimiterMethodStatistics
                {
                    MethodName = "MethodB",
                    RequestsInLastMinute = 20,
                    RequestsInLastDay = 200,
                    MaxRequestsPerMinute = 4750,
                    MaxRequestsPerDay = 95000
                }
            };

            // Act
            var stats = new MyAdminAPIRateLimiterStatistics
            {
                MaxRequestsPerMinute = 5000,
                MaxRequestsPerDay = 100000,
                EffectiveRequestsPerMinute = 4750,
                EffectiveRequestsPerDay = 95000,
                MethodStatistics = methodStats
            };

            // Assert
            Assert.Equal(5000, stats.MaxRequestsPerMinute);
            Assert.Equal(100000, stats.MaxRequestsPerDay);
            Assert.Equal(4750, stats.EffectiveRequestsPerMinute);
            Assert.Equal(95000, stats.EffectiveRequestsPerDay);
            Assert.Equal(2, stats.MethodStatistics.Count);
            Assert.True(stats.MethodStatistics.ContainsKey("MethodA"));
            Assert.True(stats.MethodStatistics.ContainsKey("MethodB"));
            output.WriteLine($"Statistics initialized with {stats.MethodStatistics.Count} methods");
        }

        [Fact]
        public void MethodStatistics_DefaultsToEmptyDictionary()
        {
            // Arrange & Act
            var stats = new MyAdminAPIRateLimiterStatistics
            {
                MaxRequestsPerMinute = 5000,
                MaxRequestsPerDay = 100000,
                EffectiveRequestsPerMinute = 4750,
                EffectiveRequestsPerDay = 95000
            };

            // Assert
            Assert.NotNull(stats.MethodStatistics);
            Assert.Empty(stats.MethodStatistics);
            output.WriteLine("MethodStatistics defaults to empty dictionary");
        }
    }
}