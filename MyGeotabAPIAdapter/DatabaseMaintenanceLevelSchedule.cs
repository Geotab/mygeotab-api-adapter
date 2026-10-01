using MyGeotabAPIAdapter.Database.Models;
using System;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Decides when a Level 1 or Level 2 database maintenance run is due.
    /// </summary>
    public static class DatabaseMaintenanceLevelSchedule
    {
        /// <summary>
        /// Whether a maintenance level is due at <paramref name="nowUtc"/>, given the latest log record for that level: it has never run, or the interval has
        /// elapsed since the START of its latest successful run - or since the END of its latest failed one. A record with no end time is a run still in progress,
        /// and is never due.
        /// <para>
        /// A failed run is timed from its END because a failure can take longer than the interval. A Level 2 statement or view read stopped at its one-hour limit
        /// fails after about an hour, and timed from its start - with the shipped 60-minute interval - it was due again the moment it ended, so the next
        /// five-minute tick paused every data feed again.
        /// </para>
        /// </summary>
        public static bool IsDue(DbDBMaintenanceLog2 latestLog, int intervalMinutes, DateTime nowUtc)
        {
            if (latestLog == null)
            {
                return true;
            }
            if (latestLog.EndTimeUtc == null)
            {
                return false;
            }
            var spacedFromUtc = latestLog.Success == false ? latestLog.EndTimeUtc.Value : latestLog.StartTimeUtc;
            return (nowUtc - spacedFromUtc).TotalMinutes >= intervalMinutes;
        }
    }
}
