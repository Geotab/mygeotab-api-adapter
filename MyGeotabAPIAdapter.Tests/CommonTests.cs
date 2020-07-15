using System;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    public class CommonTests
    {
        [Fact]
        public void TimeIntervalHasElapsed_SimpleValuesShouldCalculateFact()
        {
            // Arrange
            bool expected = true;

            // Act
            DateTime testDate = DateTime.UtcNow.AddMinutes(-2);
            bool actual = MyGeotabAPIAdapter.Globals.TimeIntervalHasElapsed(testDate,
                MyGeotabAPIAdapter.Globals.DateTimeIntervalType.Minutes, 1);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(-2, 1, true)]
        [InlineData(1, 1, false)]
        public void TimeIntervalHasElapsed_SimpleValuesShouldCalculateTheory(double addMinutes, int interval, bool expected)
        {
            // Act
            DateTime testDate = DateTime.UtcNow.AddMinutes(addMinutes);
            bool actual = MyGeotabAPIAdapter.Globals.TimeIntervalHasElapsed(testDate,
                MyGeotabAPIAdapter.Globals.DateTimeIntervalType.Minutes, interval);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
