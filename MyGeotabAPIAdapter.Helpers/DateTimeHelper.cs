using System;

namespace MyGeotabAPIAdapter.Helpers
{
    /// <summary>
    /// A helper class to assist in working with dates and times.
    /// </summary>
    public class DateTimeHelper : IDateTimeHelper
    {
        /// <inheritdoc/>
        public DateTimeIntervalType DateTimeIntervalType { get; }

        /// <inheritdoc/>
        public bool DateTimeRange1FallsWithinDateTimeRange2(DateTime Range1MinDateTime, DateTime Range1MaxDateTime, DateTime Range2MinDateTime, DateTime Range2MaxDateTime)
        {
            if (Range1MinDateTime > Range1MaxDateTime)
            {
                throw new ArgumentException($"The value supplied for {nameof(Range1MinDateTime)} cannot be greater than the value supplied for {nameof(Range1MaxDateTime)}");
            }
            if (Range2MinDateTime > Range2MaxDateTime)
            {
                throw new ArgumentException($"The value supplied for {nameof(Range2MinDateTime)} cannot be greater than the value supplied for {nameof(Range2MaxDateTime)}");
            }
            return Range2MinDateTime < Range1MinDateTime && Range2MaxDateTime > Range1MaxDateTime;
        }

        /// <inheritdoc/>
        public bool DateTimeRangeIsValid(DateTime? minDateTime, DateTime? maxDateTime)
        {
            if (minDateTime == null || maxDateTime == null || minDateTime > maxDateTime)
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public TimeSpan GetTimeSpanToNextDailyStartTimeUTC(DateTime dailyStartTimeUTC, int runTimeAfterStartSeconds)
        {
            const int MinRunTimeAfterStartSeconds = 0;
            const int MaxRunTimeAfterStartSeconds = 86400; // 86400 sec = 1 day

            if (runTimeAfterStartSeconds < MinRunTimeAfterStartSeconds)
            {
                string errorMessage = $"The value of '{runTimeAfterStartSeconds}' provided for the '{nameof(runTimeAfterStartSeconds)}' parameter is less than the minimum allowed value of '{MinRunTimeAfterStartSeconds}'.";
                throw new ArgumentException(errorMessage);
            }
            if (runTimeAfterStartSeconds > MaxRunTimeAfterStartSeconds)
            {
                string errorMessage = $"The value of '{runTimeAfterStartSeconds}' provided for the '{nameof(runTimeAfterStartSeconds)}' parameter is greater than the maximum allowed value of '{MaxRunTimeAfterStartSeconds}'.";
                throw new ArgumentException(errorMessage);
            }

            var timeSpanToNextDailyStartTimeUTC = new TimeSpan(0);
            var runTimeAfterStartTimeSpan = TimeSpan.FromSeconds(runTimeAfterStartSeconds);
            var currentDateTimeUTC = DateTime.UtcNow;

            // Determine scheduled start and stop times for the previous, current and next days.
            var previousDayStartTimeUTC = currentDateTimeUTC.Date.AddDays(-1) + dailyStartTimeUTC.TimeOfDay;
            var previousDayEndTimeUTC = previousDayStartTimeUTC.Add(runTimeAfterStartTimeSpan);
            var currentDayStartTimeUTC = currentDateTimeUTC.Date + dailyStartTimeUTC.TimeOfDay;
            var currentDayEndTimeUTC = currentDayStartTimeUTC.Add(runTimeAfterStartTimeSpan);
            var nextDayStartTimeUTC = currentDateTimeUTC.Date.AddDays(1) + dailyStartTimeUTC.TimeOfDay;

            // Determine timeSpanToNextDailyStartTimeUTC:
            if (currentDateTimeUTC > previousDayEndTimeUTC && currentDateTimeUTC < currentDayStartTimeUTC)
            {
                // Previous day's scheduled run time has completed, but the current day's has not started. Return the timespan between now and the current day's scheduled start time.
                timeSpanToNextDailyStartTimeUTC = currentDayStartTimeUTC - currentDateTimeUTC;
            }
            else if (currentDateTimeUTC > currentDayEndTimeUTC)
            {
                // Current day's scheduled run time has completed. Return the timespan between now and the next day's scheduled start time.
                timeSpanToNextDailyStartTimeUTC = nextDayStartTimeUTC - currentDateTimeUTC;
            }

            return timeSpanToNextDailyStartTimeUTC;
        }

        /// <inheritdoc/>
        public bool TimeIntervalHasElapsed(DateTime startTimeUtc, DateTimeIntervalType dateTimeIntervalType, int interval)
        {
            DateTime endTime = DateTime.MinValue;
            switch (dateTimeIntervalType)
            {
                case DateTimeIntervalType.Milliseconds:
                    endTime = startTimeUtc.AddMilliseconds(interval);
                    break;
                case DateTimeIntervalType.Seconds:
                    endTime = startTimeUtc.AddSeconds(interval);
                    break;
                case DateTimeIntervalType.Minutes:
                    endTime = startTimeUtc.AddMinutes(interval);
                    break;
                case DateTimeIntervalType.Hours:
                    endTime = startTimeUtc.AddHours(interval);
                    break;
                case DateTimeIntervalType.Days:
                    endTime = startTimeUtc.AddDays(interval);
                    break;
                default:
                    break;
            }
            if (DateTime.UtcNow > endTime)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
