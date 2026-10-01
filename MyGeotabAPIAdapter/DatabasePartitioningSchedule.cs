using MyGeotabAPIAdapter.Database.Models;
using System;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Decides when the database partitioning procedure is due to run.
    /// </summary>
    public static class DatabasePartitioningSchedule
    {
        /// <summary>
        /// How many days pass between successful runs of the partitioning procedure once the adapter is running. Each run creates partitions through the end of the following calendar month, so a run every 20 days always leaves the next period's partition in place before it is needed.
        /// </summary>
        public const double IntervalDays = 20;

        /// <summary>
        /// How many minutes to wait after a FAILED periodic run before attempting it again.
        /// <para>
        /// The latest run is what the interval is measured from, whether or not it succeeded, so without this a failure would hold the next attempt off for the whole of <see cref="IntervalDays"/>. The retry is deliberately not immediate either: every periodic run pauses the other services first, so a failure that persists - a missing grant, say - would pause them on every five-minute tick of the service. An hour keeps each attempt visible, since each one logs its failure at Error, while leaving days in hand before the partitions created by the last successful run run out.
        /// </para>
        /// </summary>
        public const int RetryDelayMinutesAfterFailure = 60;

        /// <summary>
        /// How many days before the partitions created by the last successful run end the adapter stops, if no run has succeeded since. See <see cref="ArePartitionsNearlyExhausted"/>.
        /// </summary>
        public const double StopMarginDays = 2;

        /// <summary>
        /// Whether the partitioning procedure is due at <paramref name="nowUtc"/>.
        /// <para>
        /// It is always due until the run at startup has completed, whatever the log says, because partitions must exist before the other services write anything. After that it is due <see cref="IntervalDays"/> after the start of the latest successful run, or <see cref="RetryDelayMinutesAfterFailure"/> after the end of the latest run if that run failed. A run with no end time is never due again.
        /// </para>
        /// </summary>
        /// <param name="startupRunCompleted">Whether the run at startup has completed successfully.</param>
        /// <param name="latestRun">The latest partitioning record in the maintenance log, or <c>null</c> if there is none.</param>
        /// <param name="nowUtc">The current time, in UTC.</param>
        /// <returns>Whether the partitioning procedure should run now.</returns>
        public static bool IsDue(bool startupRunCompleted, DbDBMaintenanceLog2 latestRun, DateTime nowUtc)
        {
            if (startupRunCompleted == false || latestRun == null)
            {
                return true;
            }
            if (latestRun.EndTimeUtc == null)
            {
                return false;
            }
            if (latestRun.Success == false)
            {
                return (nowUtc - latestRun.EndTimeUtc.Value).TotalMinutes >= RetryDelayMinutesAfterFailure;
            }
            return (nowUtc - latestRun.StartTimeUtc).TotalDays >= IntervalDays;
        }

        /// <summary>
        /// When the partitions created by a successful run that started at <paramref name="runStartTimeUtc"/> end: the first day of the second calendar month after it. Whatever the partition interval, a run creates partitions through at least the end of the calendar month after the one it runs in - with weekly partitions the last can run a few days beyond it, so this is the earliest the partitions can end. After that, a new record lands in the DEFAULT partition on PostgreSQL, or in the last, open-ended partition on SQL Server - and each such record has to be moved when its partition is eventually created.
        /// </summary>
        /// <param name="runStartTimeUtc">When the successful run started, in UTC.</param>
        /// <returns>The end of the partitions that run created, in UTC.</returns>
        public static DateTime GetPartitionsEndUtc(DateTime runStartTimeUtc)
        {
            return new DateTime(runStartTimeUtc.Year, runStartTimeUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(2);
        }

        /// <summary>
        /// Whether the partitions created by the latest successful run end within <see cref="StopMarginDays"/> days of <paramref name="nowUtc"/>, so that the adapter should stop rather than carry on without a successful run.
        /// <para>
        /// A failed periodic run does not stop the adapter; it is attempted again after <see cref="RetryDelayMinutesAfterFailure"/> minutes. But if the failure persists until the partitions run out, new records land outside them, and on PostgreSQL every one of them must later be moved out of the DEFAULT partition before the partition it belongs in can be created. So once fewer than <see cref="StopMarginDays"/> days of partitions remain the adapter stops, as a failed run at startup does. With runs every <see cref="IntervalDays"/> days, a run that falls due always has at least six days in hand before this happens.
        /// </para>
        /// </summary>
        /// <param name="latestSuccessfulRunStartTimeUtc">When the latest successful run started, in UTC.</param>
        /// <param name="nowUtc">The current time, in UTC.</param>
        /// <returns>Whether the adapter should stop.</returns>
        public static bool ArePartitionsNearlyExhausted(DateTime latestSuccessfulRunStartTimeUtc, DateTime nowUtc)
        {
            return nowUtc >= GetPartitionsEndUtc(latestSuccessfulRunStartTimeUtc).AddDays(-StopMarginDays);
        }
    }
}
