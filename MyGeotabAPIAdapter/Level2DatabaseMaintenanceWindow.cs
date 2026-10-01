using System;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Applies the optional daily window that Level 2 database maintenance may be confined to.
    /// </summary>
    public static class Level2DatabaseMaintenanceWindow
    {
        /// <summary>
        /// Whether a Level 2 run may start at <paramref name="nowUtc"/> and, if it may, the time after which it must not start maintaining another index.
        /// <para>
        /// With the window DISABLED a run may start at any time and has NO stop time. A disabled window imposes nothing, including an end.
        /// </para>
        /// <para>
        /// With the window ENABLED, the window that contains now may have started the day before. A window starting at 23:30 UTC and lasting an hour covers 00:10 UTC, but at 00:10 today's window has not begun - it is yesterday's that is open. So both are checked, and the stop time is the end of whichever one contains now. Comparing with today's start alone meant that any part of a window falling after midnight UTC was never used.
        /// </para>
        /// </summary>
        /// <param name="windowEnabled">Whether Level 2 maintenance is confined to the daily window.</param>
        /// <param name="windowStartTimeUtc">When the window starts. Only its time of day is used.</param>
        /// <param name="windowMaxMinutes">How long the window lasts, in minutes.</param>
        /// <param name="nowUtc">The current time, in UTC.</param>
        /// <returns>Whether a run may start, and the time after which it must not start maintaining another index - or <c>null</c> when there is no such time.</returns>
        public static (bool MayStart, DateTime? StopTimeUtc) Evaluate(bool windowEnabled, DateTime windowStartTimeUtc, int windowMaxMinutes, DateTime nowUtc)
        {
            if (windowEnabled == false)
            {
                return (true, null);
            }

            // Today's window, then yesterday's. Both ends are inside the window.
            foreach (var dayOffset in new[] { 0, -1 })
            {
                var windowStartUtc = nowUtc.Date.AddDays(dayOffset) + windowStartTimeUtc.TimeOfDay;
                var windowEndUtc = windowStartUtc.AddMinutes(windowMaxMinutes);
                if (nowUtc >= windowStartUtc && nowUtc <= windowEndUtc)
                {
                    return (true, windowEndUtc);
                }
            }
            return (false, null);
        }
    }
}
