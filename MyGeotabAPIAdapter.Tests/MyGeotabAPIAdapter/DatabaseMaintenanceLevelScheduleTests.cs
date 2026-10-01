using MyGeotabAPIAdapter.Database.Models;
using System;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// When a Level 1 or Level 2 database maintenance run is due: the interval measured from the START of the latest successful
    /// run, or from the END of the latest failed one.
    /// </summary>
    public class DatabaseMaintenanceLevelScheduleTests
    {
        static readonly DateTime Now = new(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);

        static DbDBMaintenanceLog2 Log(DateTime startTimeUtc, DateTime? endTimeUtc, bool? success) =>
            new() { StartTimeUtc = startTimeUtc, EndTimeUtc = endTimeUtc, Success = success };

        [Fact]
        public void ARunThatNeverHappened_IsDue()
        {
            Assert.True(DatabaseMaintenanceLevelSchedule.IsDue(null, 60, Now));
        }

        [Fact]
        public void ARunStillInProgress_IsNeverDue()
        {
            Assert.False(DatabaseMaintenanceLevelSchedule.IsDue(Log(Now.AddDays(-2), null, null), 60, Now));
        }

        [Fact]
        public void ASuccessfulRun_IsSpacedFromItsStart()
        {
            Assert.True(DatabaseMaintenanceLevelSchedule.IsDue(Log(Now.AddMinutes(-70), Now.AddMinutes(-5), true), 60, Now));
            Assert.False(DatabaseMaintenanceLevelSchedule.IsDue(Log(Now.AddMinutes(-50), Now.AddMinutes(-45), true), 60, Now));
        }

        [Fact]
        public void AFailedRun_IsSpacedFromItsEnd_SoARunThatOutlastedTheIntervalIsNotDueAgainAtOnce()
        {
            // A Level 2 item that cannot finish inside its one-hour attempt fails after about an hour. Spaced from its start, the
            // run was due again the moment it failed, and every data feed was paused again at the next five-minute tick - about
            // 60 minutes in every 65, indefinitely.
            Assert.False(DatabaseMaintenanceLevelSchedule.IsDue(Log(Now.AddMinutes(-65), Now.AddMinutes(-5), false), 60, Now));
            Assert.True(DatabaseMaintenanceLevelSchedule.IsDue(Log(Now.AddMinutes(-125), Now.AddMinutes(-61), false), 60, Now));
        }
    }
}
