using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using System;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// When the database partition function is due. Every case is evaluated at the same "now"; each record is placed relative to it.
    /// </summary>
    public class DatabasePartitioningScheduleTests
    {
        static readonly DateTime Now = new(2026, 9, 24, 14, 0, 0, DateTimeKind.Utc);

        static DbDBMaintenanceLog2 Run(TimeSpan startedAgo, TimeSpan? endedAgo, bool? success) => new()
        {
            MaintenanceTypeId = (short)DBMaintenanceType.Partition.Id,
            StartTimeUtc = Now - startedAgo,
            EndTimeUtc = endedAgo == null ? null : Now - endedAgo.Value,
            Success = success
        };

        static DbDBMaintenanceLog2 SucceededDaysAgo(double days) => Run(TimeSpan.FromDays(days), TimeSpan.FromDays(days) - TimeSpan.FromSeconds(2), true);

        static DbDBMaintenanceLog2 FailedMinutesAgo(double minutes) => Run(TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(minutes), false);

        [Fact]
        public void BeforeTheStartupRunHasCompleted_ItIsDueWhateverTheLogSays()
        {
            Assert.True(DatabasePartitioningSchedule.IsDue(false, null, Now));
            Assert.True(DatabasePartitioningSchedule.IsDue(false, SucceededDaysAgo(0.01), Now));
            Assert.True(DatabasePartitioningSchedule.IsDue(false, FailedMinutesAgo(1), Now));
        }

        [Fact]
        public void WithNoRecordedRun_ItIsDue()
        {
            Assert.True(DatabasePartitioningSchedule.IsDue(true, null, Now));
        }

        [Theory]
        [InlineData(0.01, false)]
        [InlineData(19.99, false)]
        [InlineData(20, true)]
        [InlineData(45, true)]
        public void AfterASuccessfulRun_ItIsDueTwentyDaysAfterThatRunStarted(double daysAgo, bool expectedDue)
        {
            Assert.Equal(expectedDue, DatabasePartitioningSchedule.IsDue(true, SucceededDaysAgo(daysAgo), Now));
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(59, false)]
        [InlineData(60, true)]
        [InlineData(2880, true)]
        public void AfterAFailedRun_ItIsDueAnHourAfterThatRunEnded(double minutesAgo, bool expectedDue)
        {
            // Without this the failure record would hold the retry off for twenty days, because the interval is measured from
            // the latest run whether or not it succeeded - which is the case at 60 minutes and at two days.
            Assert.Equal(expectedDue, DatabasePartitioningSchedule.IsDue(true, FailedMinutesAgo(minutesAgo), Now));
        }

        [Fact]
        public void AfterAFailedRun_TheRetryDelayRunsFromItsEnd_NotItsStart()
        {
            // A run that failed only after an hour-long attempt started more than an hour ago, and must still wait its hour.
            var failedAfterAnHour = Run(TimeSpan.FromMinutes(70), TimeSpan.FromMinutes(10), false);

            Assert.False(DatabasePartitioningSchedule.IsDue(true, failedAfterAnHour, Now));
        }

        [Theory]
        [InlineData(19.99, false)]
        [InlineData(20, true)]
        public void ARunWithNoRecordedOutcome_IsTreatedAsASuccess(double daysAgo, bool expectedDue)
        {
            var run = Run(TimeSpan.FromDays(daysAgo), TimeSpan.FromDays(daysAgo), null);

            Assert.Equal(expectedDue, DatabasePartitioningSchedule.IsDue(true, run, Now));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ARunWithNoEndTime_IsNeverDueAgain(bool succeeded)
        {
            var run = Run(TimeSpan.FromDays(45), null, succeeded);

            Assert.False(DatabasePartitioningSchedule.IsDue(true, run, Now));
        }

        // ------------------------------------------------------------------------ when the partitions of a run end

        [Theory]
        [InlineData("2026-09-24 14:00:00", "2026-11-01")]
        [InlineData("2026-09-01 00:00:00", "2026-11-01")]
        [InlineData("2026-12-15 08:30:00", "2027-02-01")]
        [InlineData("2027-01-31 23:59:59", "2027-03-01")]
        public void TheirPartitionsEnd_OnTheFirstDayOfTheSecondMonthAfterTheRun(string runStart, string expectedEnd)
        {
            // Both procedures create partitions through the end of the calendar month after the one they run in.
            var end = DatabasePartitioningSchedule.GetPartitionsEndUtc(DateTime.SpecifyKind(DateTime.Parse(runStart), DateTimeKind.Utc));

            Assert.Equal(DateTime.SpecifyKind(DateTime.Parse(expectedEnd), DateTimeKind.Utc), end);
            Assert.Equal(DateTimeKind.Utc, end.Kind);
        }

        [Fact]
        public void TheAdapterStops_OnlyOnceFewerThanTwoDaysOfPartitionsRemain()
        {
            // A run on 2026-09-24 created partitions through 2026-10-31, so they end at 2026-11-01 00:00 UTC.
            var run = new DateTime(2026, 9, 24, 14, 0, 0, DateTimeKind.Utc);

            Assert.False(DatabasePartitioningSchedule.ArePartitionsNearlyExhausted(run, new DateTime(2026, 10, 29, 23, 59, 59, DateTimeKind.Utc)));
            Assert.True(DatabasePartitioningSchedule.ArePartitionsNearlyExhausted(run, new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc)));
            Assert.True(DatabasePartitioningSchedule.ArePartitionsNearlyExhausted(run, new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc)));
        }

        [Fact]
        public void ARunThatFallsDue_AlwaysHasAtLeastSixDaysOfRetriesBeforeTheAdapterStops()
        {
            // Whatever day and time the last successful run started - a leap year included - the next run falls due, and
            // begins retrying hourly if it fails, at least six days before the adapter would stop. The tightest case is a run
            // late on 31 January of a year whose February has 28 days.
            for (var start = new DateTime(2027, 1, 1, 23, 59, 59, DateTimeKind.Utc); start.Year < 2029; start = start.AddDays(1))
            {
                var due = start.AddDays(DatabasePartitioningSchedule.IntervalDays);
                var stop = DatabasePartitioningSchedule.GetPartitionsEndUtc(start).AddDays(-DatabasePartitioningSchedule.StopMarginDays);

                Assert.False(DatabasePartitioningSchedule.ArePartitionsNearlyExhausted(start, due), $"run at {start:u}");
                Assert.True(stop - due >= TimeSpan.FromDays(6), $"run at {start:u}: only {(stop - due).TotalDays:F2} days between falling due and stopping");
            }
        }
    }
}
