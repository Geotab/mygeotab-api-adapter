using System;
using Xunit;
using MyGeotabAPIAdapter.Helpers;

namespace MyGeotabAPIAdapter.Tests
{
    public class DateTimeRange1FallsWithinDateTimeRange2TestData : TheoryData<DateTime, DateTime, DateTime, DateTime, bool>
    {
        public DateTimeRange1FallsWithinDateTimeRange2TestData()
        {
            // Range 1 falls within Range 2.
            Add(DateTime.Now.AddMinutes(2), DateTime.Now.AddMinutes(3), DateTime.Now.AddMinutes(1), DateTime.Now.AddMinutes(4), true);
            // Range 1 falls before Range 2.
            Add(DateTime.Now.AddMinutes(1), DateTime.Now.AddMinutes(2), DateTime.Now.AddMinutes(3), DateTime.Now.AddMinutes(4), false);
            // Range 1 falls after Range 2.
            Add(DateTime.Now.AddMinutes(3), DateTime.Now.AddMinutes(4), DateTime.Now.AddMinutes(1), DateTime.Now.AddMinutes(2), false);
            // Range 1 overlaps Range 2.
            Add(DateTime.Now.AddMinutes(1), DateTime.Now.AddMinutes(3), DateTime.Now.AddMinutes(2), DateTime.Now.AddMinutes(4), false);
        }
    }

    public class DateTimeRange1FallsWithinDateTimeRange2TestData_BadData : TheoryData<DateTime, DateTime, DateTime, DateTime>
    {
        public DateTimeRange1FallsWithinDateTimeRange2TestData_BadData()
        {
            // Range 1 min DateTime > Range 1 max DateTime.
            Add(DateTime.Now.AddMinutes(2), DateTime.Now.AddMinutes(1), DateTime.Now.AddMinutes(3), DateTime.Now.AddMinutes(4));
            // Range 2 min DateTime > Range 2 max DateTime.
            Add(DateTime.Now.AddMinutes(2), DateTime.Now.AddMinutes(3), DateTime.Now.AddMinutes(4), DateTime.Now.AddMinutes(1));
        }
    }

    public class DateTimeRangeIsValidTestData : TheoryData<DateTime?, DateTime?, bool>
    {
        public DateTimeRangeIsValidTestData()
        {
            // Valid range.
            Add(DateTime.Now, DateTime.Now.AddMinutes(2), true);
            // Min DateTime null.
            Add(null, DateTime.Now.AddMinutes(2), false);
            // Max DateTime null.
            Add(DateTime.Now,null, false);
            // Min DateTime > max DateTime.
            Add(DateTime.Now.AddMinutes(2), DateTime.Now, false);
        }
    }

    public class GetTimeSpanToNextDailyStartTimeUTCTestData : TheoryData<DateTime, int, TimeSpan>
    {
        public GetTimeSpanToNextDailyStartTimeUTCTestData()
        {
            // Previous day's scheduled run time has completed, but the current day's has not started. Return the timespan between now and the current day's scheduled start time. Return 1 hour.
            var currentDateTimeUTC = DateTime.UtcNow;
            var runTimeAfterStartSeconds = 3600; // 1-hour run time window.
            var dailyStartTimeUTC = currentDateTimeUTC.AddHours(1);
            var expectedTimeSpan = new TimeSpan(1, 0, 0);
            Add(dailyStartTimeUTC, runTimeAfterStartSeconds, expectedTimeSpan);

            // Current day's scheduled run time has completed. Return the timespan between now and the next day's scheduled start time. Return 23 hours.
            dailyStartTimeUTC = currentDateTimeUTC.AddHours(-1);
            expectedTimeSpan = new TimeSpan(23, 0, 0);
            Add(dailyStartTimeUTC, runTimeAfterStartSeconds, expectedTimeSpan);

            // Previous or current day's scheduled run time has NOT completed. Return 0.
            runTimeAfterStartSeconds = 21600; // 6-hour run time window.
            dailyStartTimeUTC = currentDateTimeUTC.AddHours(-2);
            expectedTimeSpan = new TimeSpan(0);
            Add(dailyStartTimeUTC, runTimeAfterStartSeconds, expectedTimeSpan);
        }
    }
     
    public class GetTimeSpanToNextDailyStartTimeUTCTestData_BadData : TheoryData<DateTime, int>
    {
        public GetTimeSpanToNextDailyStartTimeUTCTestData_BadData()
        {
            // Previous or current day's scheduled run time has NOT completed, but runTimeAfterStartSeconds < MinRunTimeAfterStartSeconds (0)
            var currentDateTimeUTC = DateTime.UtcNow;
            var runTimeAfterStartSeconds = -3600;
            var dailyStartTimeUTC = currentDateTimeUTC.AddHours(-1);
            Add(dailyStartTimeUTC, runTimeAfterStartSeconds);

            // Previous or current day's scheduled run time has NOT completed, but runTimeAfterStartSeconds > MaxRunTimeAfterStartSeconds (86400)
            currentDateTimeUTC = DateTime.UtcNow;
            runTimeAfterStartSeconds = -86401;
            dailyStartTimeUTC = currentDateTimeUTC.AddHours(-1);
            Add(dailyStartTimeUTC, runTimeAfterStartSeconds);
        }
    }

    public class TimeIntervalHasElapsedTestData : TheoryData<DateTime, DateTimeIntervalType, int, bool>
    {
        public TimeIntervalHasElapsedTestData()
        {
            // Elapsed - milliseconds
            var startDateTime = DateTime.UtcNow.AddMilliseconds(-10000);
            Add(startDateTime, DateTimeIntervalType.Milliseconds, 1, true);
            // Not elapsed - milliseconds
            startDateTime = DateTime.UtcNow.AddMilliseconds(-1000);
            Add(startDateTime, DateTimeIntervalType.Milliseconds, 50000, false);

            // Elapsed - seconds
            startDateTime = DateTime.UtcNow.AddSeconds(-10);
            Add(startDateTime, DateTimeIntervalType.Seconds, 5, true);
            // Not elapsed - seconds
            startDateTime = DateTime.UtcNow.AddSeconds(-1);
            Add(startDateTime, DateTimeIntervalType.Seconds, 30, false);

            // Elapsed - minutes
            startDateTime = DateTime.UtcNow.AddMinutes(-10);
            Add(startDateTime, DateTimeIntervalType.Minutes, 5, true);
            // Not elapsed - minutes
            startDateTime = DateTime.UtcNow.AddMinutes(-2);
            Add(startDateTime, DateTimeIntervalType.Minutes, 10, false);

            // Elapsed - hours
            startDateTime = DateTime.UtcNow.AddHours(-10);
            Add(startDateTime, DateTimeIntervalType.Hours, 5, true);
            // Not elapsed - hours
            startDateTime = DateTime.UtcNow.AddHours(-2);
            Add(startDateTime, DateTimeIntervalType.Hours, 5, false);

            // Elapsed - days
            startDateTime = DateTime.UtcNow.AddDays(-10);
            Add(startDateTime, DateTimeIntervalType.Days, 5, true);
            // Not elapsed - days
            startDateTime = DateTime.UtcNow.AddDays(-2);
            Add(startDateTime, DateTimeIntervalType.Days, 5, false);
        }
    }

    public class DateTimeHelperTests
    {
        [Theory]
        [ClassData(typeof(DateTimeRange1FallsWithinDateTimeRange2TestData))]
        public void DateTimeRange1FallsWithinDateTimeRange2(DateTime range1MinDateTime, DateTime range1MaxDateTime, DateTime range2MinDateTime, DateTime range2MaxDateTime, bool expected)
        {
            var dateTimeHelper = new DateTimeHelper();
            var result = dateTimeHelper.DateTimeRange1FallsWithinDateTimeRange2(range1MinDateTime, range1MaxDateTime, range2MinDateTime, range2MaxDateTime);
            Assert.Equal(expected, result);
        }

        [Theory]
        [ClassData(typeof(DateTimeRange1FallsWithinDateTimeRange2TestData_BadData))]
        public void DateTimeRange1FallsWithinDateTimeRange2_Exceptions(DateTime range1MinDateTime, DateTime range1MaxDateTime, DateTime range2MinDateTime, DateTime range2MaxDateTime)
        {
            var dateTimeHelper = new DateTimeHelper();
            Assert.Throws<ArgumentException>(() => dateTimeHelper.DateTimeRange1FallsWithinDateTimeRange2(range1MinDateTime, range1MaxDateTime, range2MinDateTime, range2MaxDateTime));
        }

        [Theory]
        [ClassData(typeof(DateTimeRangeIsValidTestData))]
        public void DateTimeRangeIsValid(DateTime? minDateTime, DateTime? maxDateTime, bool expected)
        {
            var dateTimeHelper = new DateTimeHelper();
            var result = dateTimeHelper.DateTimeRangeIsValid(minDateTime, maxDateTime);
            Assert.Equal(expected, result);
        }

        [Theory]
        [ClassData(typeof(GetTimeSpanToNextDailyStartTimeUTCTestData))]
        public void GetTimeSpanToNextDailyStartTimeUTC(DateTime dailyStartTimeUTC, int runTimeAfterStartSeconds, TimeSpan expected)
        {
            var dateTimeHelper = new DateTimeHelper();
            var result = dateTimeHelper.GetTimeSpanToNextDailyStartTimeUTC(dailyStartTimeUTC, runTimeAfterStartSeconds);
            var low = expected.Subtract(TimeSpan.FromSeconds(1));
            var high = expected.Add(TimeSpan.FromSeconds(1));
            Assert.InRange(result, low, high);
        }

        [Theory]
        [ClassData(typeof(GetTimeSpanToNextDailyStartTimeUTCTestData_BadData))]
        public void GetTimeSpanToNextDailyStartTimeUTC_Exceptions(DateTime dailyStartTimeUTC, int runTimeAfterStartSeconds)
        {
            var dateTimeHelper = new DateTimeHelper();
            Assert.Throws<ArgumentException>(() => dateTimeHelper.GetTimeSpanToNextDailyStartTimeUTC(dailyStartTimeUTC, runTimeAfterStartSeconds));
        }

        [Theory]
        [ClassData(typeof(TimeIntervalHasElapsedTestData))]
        public void TimeIntervalHasElapsed(DateTime startTimeUtc, DateTimeIntervalType dateTimeIntervalType, int interval, bool expected)
        {
            var dateTimeHelper = new DateTimeHelper();
            var result = dateTimeHelper.TimeIntervalHasElapsed(startTimeUtc, dateTimeIntervalType, interval);
            Assert.Equal(expected, result);
        }
    }
}
