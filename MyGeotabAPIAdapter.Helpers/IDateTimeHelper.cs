using System;

namespace MyGeotabAPIAdapter.Helpers
{
    /// <summary>
    /// Interface for a helper class to assist in working with dates and times.
    /// </summary>
    public interface IDateTimeHelper
    {
        /// <summary>
        /// Interval types for use when working with DateTime functions.
        /// </summary>
        DateTimeIntervalType DateTimeIntervalType { get; }

        /// <summary>
        /// Indicates whether the timespan defined by <paramref name="range1MinDateTime"/> and <paramref name="range1MaxDateTime"/> falls within the timespan defined by <paramref name="range2MinDateTime"/> and <paramref name="range2MaxDateTime"/>
        /// </summary>
        /// <param name="range1MinDateTime">The lower limit of the "Range 1" timespan.</param>
        /// <param name="range1MaxDateTime">The upper limit of the "Range 1" timespan.</param>
        /// <param name="range2MinDateTime">The lower limit of the "Range 2" timespan.</param>
        /// <param name="range2MaxDateTime">The upper limit of the "Range 2" timespan.</param>
        /// <returns></returns>
        bool DateTimeRange1FallsWithinDateTimeRange2(DateTime range1MinDateTime, DateTime range1MaxDateTime, DateTime range2MinDateTime, DateTime range2MaxDateTime);

        /// <summary>
        /// Indicates whether the timespan defined by <paramref name="minDateTime"/> and <paramref name="maxDateTime"/> is valid.
        /// </summary>
        /// <param name="minDateTime">The lower limit of the timespan.</param>
        /// <param name="maxDateTime">The upper limit of the timespan.</param>
        /// <returns></returns>
        bool DateTimeRangeIsValid(DateTime? minDateTime, DateTime? maxDateTime);

        /// <summary>
        /// Returns the <see cref="TimeSpan"/> to the next daily start time calculated based on the <see cref="DateTime.TimeOfDay"/> of the <paramref name="dailyStartTimeUTC"/> and the <paramref name="runTimeAfterStartSeconds"/>:
        /// <list type="bullet">
        /// <item>
        /// <description>If the previous day's calculated run time has ended, but the current day's calculated run time has not started, the returned value will be the <see cref="TimeSpan"/> between <see cref="DateTime.UtcNow"/> and the current day's calculated start time.</description>
        /// </item>
        /// <item>
        /// <description>If the current day's calculated run time has ended, the returned value will be the <see cref="TimeSpan"/> between <see cref="DateTime.UtcNow"/> and the next day's calculated start time.</description>
        /// </item>
        /// <item>
        /// <description>In all other cases, the returned value will be a <see cref="TimeSpan"/> of zero, since <see cref="DateTime.Now"/> falls within the calculated run time of the previous day or that of the current day.</description>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="dailyStartTimeUTC">The <see cref="DateTime"/> of which the <see cref="DateTime.TimeOfDay"/> will be used to calculate the previous, current and next days' run time windows. It is expected that the value provided will be in Coordinated Universal Time (UTC).</param>
        /// <param name="runTimeAfterStartSeconds">The number of seconds to add to the <see cref="DateTime.TimeOfDay"/> of the <paramref name="dailyStartTimeUTC"/> in order to calculate the end <see cref="DateTime"/>s of the previous, current and next days' run time windows. Must be a value between 0 and 86400 (1 day).</param>
        /// <returns></returns>
        TimeSpan GetTimeSpanToNextDailyStartTimeUTC(DateTime dailyStartTimeUTC, int runTimeAfterStartSeconds);

        /// <summary>
        /// Returns a boolean indicating whether the time interval defined by adding <paramref name="interval"/> to <paramref name="startTimeUtc"/> has elapsed.
        /// </summary>
        /// <param name="startTimeUtc">The interval start time.</param>
        /// <param name="dateTimeIntervalType">The <see cref="DateTimeIntervalType"/> to use in the calcualation.</param>
        /// <param name="interval">The interval duration.</param>
        /// <returns></returns>
        bool TimeIntervalHasElapsed(DateTime startTimeUtc, DateTimeIntervalType dateTimeIntervalType, int interval);
    }

    /// <summary>
    /// Interval types for use when working with DateTime functions.
    /// </summary>
    public enum DateTimeIntervalType { Milliseconds, Seconds, Minutes, Hours, Days }
}
