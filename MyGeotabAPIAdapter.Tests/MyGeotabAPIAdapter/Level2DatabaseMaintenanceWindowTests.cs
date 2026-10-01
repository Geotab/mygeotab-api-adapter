using System;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// The Level 2 database maintenance window. The configured start carries a date that is ignored - only its time of day is used - so each start below is on a date years before the "now" it is evaluated against, as the shipped default is.
    /// </summary>
    public class Level2DatabaseMaintenanceWindowTests
    {
        static readonly DateTime SixAm = new(2020, 6, 23, 6, 0, 0, DateTimeKind.Utc);
        static readonly DateTime ElevenThirtyPm = new(2020, 6, 23, 23, 30, 0, DateTimeKind.Utc);
        static readonly DateTime Midnight = new(2020, 6, 23, 0, 0, 0, DateTimeKind.Utc);

        static DateTime At(int day, int hour, int minute) => new(2026, 9, day, hour, minute, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData(0, 0)]
        [InlineData(6, 30)]
        [InlineData(7, 30)]
        [InlineData(23, 59)]
        public void WithTheWindowDisabled_ARunMayStartAtAnyTime_AndHasNoStopTime(int hour, int minute)
        {
            // 07:30 is the case the shipped defaults used to get wrong: after today's computed end of 07:00, a run with
            // the window disabled was still handed that end as a stop time, and maintained nothing.
            var (mayStart, stopTimeUtc) = Level2DatabaseMaintenanceWindow.Evaluate(false, SixAm, 60, At(23, hour, minute));

            Assert.True(mayStart);
            Assert.Null(stopTimeUtc);
        }

        [Fact]
        public void WithTheWindowEnabled_ARunInsideItStopsAtItsEnd()
        {
            var (mayStart, stopTimeUtc) = Level2DatabaseMaintenanceWindow.Evaluate(true, SixAm, 60, At(23, 6, 20));

            Assert.True(mayStart);
            Assert.Equal(At(23, 7, 0), stopTimeUtc);
        }

        [Theory]
        [InlineData(6, 0)]
        [InlineData(7, 0)]
        public void WithTheWindowEnabled_BothEndsAreInsideIt(int hour, int minute)
        {
            var (mayStart, stopTimeUtc) = Level2DatabaseMaintenanceWindow.Evaluate(true, SixAm, 60, At(23, hour, minute));

            Assert.True(mayStart);
            Assert.Equal(At(23, 7, 0), stopTimeUtc);
        }

        [Theory]
        [InlineData(5, 59)]
        [InlineData(7, 1)]
        [InlineData(12, 0)]
        public void WithTheWindowEnabled_ARunOutsideItMayNotStart(int hour, int minute)
        {
            var (mayStart, stopTimeUtc) = Level2DatabaseMaintenanceWindow.Evaluate(true, SixAm, 60, At(23, hour, minute));

            Assert.False(mayStart);
            Assert.Null(stopTimeUtc);
        }

        [Fact]
        public void AWindowCrossingMidnight_CoversTheTimeAfterMidnight()
        {
            // At 00:10 today's 23:30 window has not begun; yesterday's is still open, until 00:30 today.
            var (mayStart, stopTimeUtc) = Level2DatabaseMaintenanceWindow.Evaluate(true, ElevenThirtyPm, 60, At(24, 0, 10));

            Assert.True(mayStart);
            Assert.Equal(At(24, 0, 30), stopTimeUtc);
        }

        [Fact]
        public void AWindowCrossingMidnight_CoversMidnightItself()
        {
            var (mayStart, stopTimeUtc) = Level2DatabaseMaintenanceWindow.Evaluate(true, ElevenThirtyPm, 60, At(24, 0, 0));

            Assert.True(mayStart);
            Assert.Equal(At(24, 0, 30), stopTimeUtc);
        }

        [Fact]
        public void AWindowCrossingMidnight_IncludesItsEndAfterMidnight()
        {
            var (mayStart, stopTimeUtc) = Level2DatabaseMaintenanceWindow.Evaluate(true, ElevenThirtyPm, 60, At(24, 0, 30));

            Assert.True(mayStart);
            Assert.Equal(At(24, 0, 30), stopTimeUtc);
        }

        [Fact]
        public void AWindowCrossingMidnight_CoversTheTimeBeforeMidnight()
        {
            var (mayStart, stopTimeUtc) = Level2DatabaseMaintenanceWindow.Evaluate(true, ElevenThirtyPm, 60, At(23, 23, 45));

            Assert.True(mayStart);
            Assert.Equal(At(24, 0, 30), stopTimeUtc);
        }

        [Theory]
        [InlineData(0, 31)]
        [InlineData(12, 0)]
        [InlineData(23, 29)]
        public void AWindowCrossingMidnight_IsClosedOutsideIt(int hour, int minute)
        {
            var (mayStart, stopTimeUtc) = Level2DatabaseMaintenanceWindow.Evaluate(true, ElevenThirtyPm, 60, At(24, hour, minute));

            Assert.False(mayStart);
            Assert.Null(stopTimeUtc);
        }

        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(0, 45, true)]
        [InlineData(1, 0, true)]
        [InlineData(1, 1, false)]
        [InlineData(23, 59, false)]
        public void AWindowStartingAtMidnight_IsTodaysWindowOnly(int hour, int minute, bool expectedMayStart)
        {
            var (mayStart, stopTimeUtc) = Level2DatabaseMaintenanceWindow.Evaluate(true, Midnight, 60, At(24, hour, minute));

            Assert.Equal(expectedMayStart, mayStart);
            Assert.Equal(expectedMayStart ? (DateTime?)At(24, 1, 0) : null, stopTimeUtc);
        }
    }
}
