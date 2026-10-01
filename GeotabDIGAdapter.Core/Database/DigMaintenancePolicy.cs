using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// What Level 1 database maintenance does to one table.
    /// </summary>
    public enum DigLevel1Action
    {
        /// <summary>Nothing: the table does not meet any threshold.</summary>
        None,
        /// <summary>PostgreSQL: ANALYZE only.</summary>
        Analyze,
        /// <summary>PostgreSQL: VACUUM, then ANALYZE.</summary>
        VacuumAndAnalyze,
        /// <summary>SQL Server: UPDATE STATISTICS.</summary>
        UpdateStatistics
    }

    /// <summary>
    /// What Level 2 database maintenance does to one index, or one partition of it.
    /// </summary>
    public enum DigLevel2Action
    {
        /// <summary>Nothing: the index does not meet any threshold.</summary>
        None,
        /// <summary>SQL Server: REORGANIZE the partition (or the index, if it has only one).</summary>
        Reorganize,
        /// <summary>SQL Server: REBUILD the partition (or the index, if it has only one).</summary>
        RebuildPartition,
        /// <summary>SQL Server: REBUILD the whole index, every partition of it.</summary>
        RebuildIndex,
        /// <summary>PostgreSQL: REINDEX the index.</summary>
        Reindex,
        /// <summary>PostgreSQL: REINDEX the index CONCURRENTLY, so that writers of its table are not blocked - for a live queue.</summary>
        ReindexConcurrently
    }

    /// <summary>
    /// The decisions Level 1 and Level 2 database maintenance make, kept apart from the database work so that they can be tested without one: when the DatabaseMaintenanceService wakes, when each kind of maintenance is due, whether a Level 2 run may start and when it must stop, and what each candidate row calls for.
    /// <para>
    /// Every threshold comes from <see cref="DigMaintenanceThresholds"/>, which is also what the views are checked against.
    /// </para>
    /// </summary>
    public static class DigMaintenancePolicy
    {
        /// <summary>
        /// How often the DatabaseMaintenanceService wakes: the shortest of the intervals it has to honour.
        /// <para>
        /// Each task still runs only once its own interval has elapsed, so this changes when the service looks, not how often anything happens. Partition management, retention and the table-depth report keep <see cref="IDIGAdapterConfiguration.DatabaseMaintenanceServiceIntervalMinutes"/>, which therefore means what it meant before Level 1 and Level 2 existed. Only enabled levels count: a disabled Level 1 must not wake the service every half hour for nothing.
        /// </para>
        /// <para>
        /// The Level 2 window counts too, when it is enabled: the service wakes at least twice as often as the window is long. Waking only as often as the window lasts is not enough, because each iteration's own work adds to the gap between wakes, so two wakes could fall either side of the window and Level 2 would not run that day.
        /// </para>
        /// </summary>
        public static int GetServiceTickMinutes(IDIGAdapterConfiguration adapterConfiguration)
        {
            return GetServiceTickMinutes(
                adapterConfiguration.DatabaseMaintenanceServiceIntervalMinutes,
                adapterConfiguration.EnableLevel1DigMaintenance, adapterConfiguration.Level1DigMaintenanceIntervalMinutes,
                adapterConfiguration.EnableLevel2DigMaintenance, adapterConfiguration.Level2DigMaintenanceIntervalMinutes,
                adapterConfiguration.EnableLevel2DigMaintenanceWindow, adapterConfiguration.Level2DigMaintenanceWindowMaxMinutes);
        }

        /// <inheritdoc cref="GetServiceTickMinutes(IDIGAdapterConfiguration)"/>
        public static int GetServiceTickMinutes(int serviceIntervalMinutes, bool level1Enabled, int level1IntervalMinutes, bool level2Enabled, int level2IntervalMinutes, bool level2WindowEnabled, int level2WindowMaxMinutes)
        {
            var tickMinutes = serviceIntervalMinutes;
            if (level1Enabled == true)
            {
                tickMinutes = Math.Min(tickMinutes, level1IntervalMinutes);
            }
            if (level2Enabled == true)
            {
                tickMinutes = Math.Min(tickMinutes, level2IntervalMinutes);
                if (level2WindowEnabled == true)
                {
                    tickMinutes = Math.Min(tickMinutes, Math.Max(level2WindowMaxMinutes / 2, 1));
                }
            }
            return Math.Max(tickMinutes, 1);
        }

        /// <summary>
        /// Whether a kind of maintenance is due, given the latest <see cref="DbGdaDigMaintenanceLog"/> row of that kind: it has never run, or the interval has elapsed since the START of its latest successful run - or since the END of its latest failed one. A row with no end time is a run still in progress, and is never due.
        /// <para>
        /// A failed run counts as a run. Otherwise a failure that recurs - an index that cannot be rebuilt - would be retried on every wake, and Level 2 would pause telemetry each time to fail again. It is timed from its END because a failure can take longer than the interval - a statement stopped at its one-hour limit - and timed from its start it would be due again the moment it ended.
        /// </para>
        /// </summary>
        public static bool IsDue(DbGdaDigMaintenanceLog? latestLog, int intervalMinutes, DateTime nowUtc)
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

        /// <summary>
        /// Whether a Level 2 run may start now and, if it may, the time after which it must not start maintaining another index.
        /// <para>
        /// With the window DISABLED a run may start at any time and has NO stop time. This is the corrected form of a defect the MyGeotab API Adapter shipped with: there, the end of the configured window was applied as a stop time even when the window was disabled, so with the shipped defaults any run beginning after 07:00 UTC stopped before maintaining anything while recording a success.
        /// </para>
        /// <para>
        /// With the window ENABLED, the window that contains now may have started the day before: a window starting at 23:30 UTC and lasting an hour covers 00:10 UTC, which a comparison with today's start alone would miss.
        /// </para>
        /// </summary>
        public static (bool MayStart, DateTime? StopTimeUtc) EvaluateLevel2Window(bool windowEnabled, DateTime windowStartTimeUtc, int windowMaxMinutes, DateTime nowUtc)
        {
            if (windowEnabled == false)
            {
                return (true, null);
            }

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

        /// <summary>
        /// What Level 1 maintenance does to a PostgreSQL table. The columns and thresholds are RATIOS.
        /// </summary>
        public static DigLevel1Action DecideLevel1(DbGdavwStatForLevel1DigMaintenance_PG row)
        {
            if ((row.PctDeadTuples != null && (double)row.PctDeadTuples.Value > DigMaintenanceThresholds.PG_VacuumThreshold_DeadTupleRatio)
                || row.DeadTuples > DigMaintenanceThresholds.PG_VacuumThreshold_DeadTuplesCount)
            {
                return DigLevel1Action.VacuumAndAnalyze;
            }
            if (row.PctModsSinceLastAnalyze != null && (double)row.PctModsSinceLastAnalyze.Value > DigMaintenanceThresholds.PG_AnalyzeThreshold_ModsSinceLastAnalyzeRatio)
            {
                return DigLevel1Action.Analyze;
            }
            return DigLevel1Action.None;
        }

        /// <summary>
        /// What Level 1 maintenance does to a SQL Server table. The percentage column and threshold are PERCENTAGES.
        /// </summary>
        public static DigLevel1Action DecideLevel1(DbGdavwStatForLevel1DigMaintenance_MSSQL row)
        {
            if (row.ModsSinceLastStatsUpdate > DigMaintenanceThresholds.MSSQL_UpdateStatisticsThreshold_ModsSinceLastStatsUpdateCount
                && row.PctModsSinceLastStatsUpdate > DigMaintenanceThresholds.MSSQL_UpdateStatisticsThreshold_PctModsSinceLastStatsUpdate)
            {
                return DigLevel1Action.UpdateStatistics;
            }
            return DigLevel1Action.None;
        }

        /// <summary>
        /// Why PostgreSQL's pgstattuple cannot be used for Level 2 maintenance, and the statements that fix it, given what the probe found: whether
        /// <c>public.pgstattuple(regclass)</c> exists, and which schema - if any - the extension is installed in. The four cases need different
        /// remedies: an absent extension is created; one installed in another schema is MOVED (creating it again is a no-op there); one in public
        /// that is too old to have <c>pgstattuple(regclass)</c> - carried forward by an upgrade without <c>ALTER EXTENSION ... UPDATE</c> - is
        /// UPDATED (moving it to the schema it is already in would change nothing); and one that exists but may not be executed is granted.
        /// </summary>
        public static string DescribePgstattupleProblem(bool existsInPublic, string? extensionSchema, string roleName)
        {
            const string grant = "GRANT EXECUTE ON FUNCTION public.pgstattuple(regclass) TO {0};";
            if (existsInPublic)
            {
                return $"this adapter's role may not execute public.pgstattuple. To enable it, have a superuser run: {string.Format(grant, roleName)}";
            }
            if (extensionSchema == "public")
            {
                return $"the pgstattuple extension installed in schema public is an old version, from before pgstattuple(regclass), which the Level 2 view uses, existed. To enable it, have a superuser run: ALTER EXTENSION pgstattuple UPDATE; {string.Format(grant, roleName)}";
            }
            if (string.IsNullOrEmpty(extensionSchema) == false)
            {
                return $"the pgstattuple extension is installed in schema {extensionSchema}, while the Level 2 view uses public.pgstattuple. To enable it, have a superuser run: ALTER EXTENSION pgstattuple SET SCHEMA public; {string.Format(grant, roleName)}";
            }
            return $"the pgstattuple extension that measures index bloat is not installed. To enable it, have a superuser run: CREATE EXTENSION IF NOT EXISTS pgstattuple WITH SCHEMA public; {string.Format(grant, roleName)}";
        }

        /// <summary>
        /// Whether a Level 2 operation needs the other services paused. Only the OFFLINE ones do - a plain REINDEX
        /// (PostgreSQL) or a REBUILD (SQL Server), which block the table's writers for their whole duration. REINDEX
        /// CONCURRENTLY and REORGANIZE block no writer, so they run with the other services running - which also keeps a
        /// concurrent rebuild that waits for a long transaction (a nightly pg_dump, say) from holding telemetry still.
        /// </summary>
        public static bool RequiresPause(DigLevel2Action action) =>
            action == DigLevel2Action.Reindex || action == DigLevel2Action.RebuildPartition || action == DigLevel2Action.RebuildIndex;

        /// <summary>
        /// Whether a gda table is one of the live <c>Q_*</c> queues - written by the customer's own application, which no maintenance pause reaches - rather than a fail table, a partition of one, <c>DIGInvalidRecords</c> or a control-plane table, which only this adapter writes.
        /// </summary>
        public static bool IsLiveQueueTable(string tableName) =>
            tableName != null && tableName.StartsWith("Q_", StringComparison.Ordinal) && tableName.Contains("Fail", StringComparison.Ordinal) == false;

        /// <summary>
        /// What Level 2 maintenance does to a PostgreSQL index. The bloat ratio and its threshold are RATIOS.
        /// </summary>
        public static DigLevel2Action DecideLevel2(DbGdavwStatForLevel2DigMaintenance_PG row)
        {
            if (row.IndexBloatRatio > DigMaintenanceThresholds.PG_ReindexThreshold_BloatRatio
                && row.IndexSize > DigMaintenanceThresholds.PG_ReindexThreshold_IndexSizeBytes)
            {
                // A plain REINDEX blocks every writer of the table for the whole rebuild. The maintenance pause holds this
                // adapter's services, not the customer's application writing a live queue, so a live queue's index is
                // rebuilt CONCURRENTLY; every other gda table is written only by this adapter, which is paused.
                return IsLiveQueueTable(row.TableName) ? DigLevel2Action.ReindexConcurrently : DigLevel2Action.Reindex;
            }
            return DigLevel2Action.None;
        }

        /// <summary>
        /// What Level 2 maintenance does to one partition of a SQL Server index. Every column and threshold here is a PERCENTAGE.
        /// </summary>
        public static DigLevel2Action DecideLevel2(DbGdavwStatForLevel2DigMaintenance_MSSQL row)
        {
            if (row.IndexSizeKB <= DigMaintenanceThresholds.MSSQL_MinimumIndexSizeKB)
            {
                return DigLevel2Action.None;
            }
            // An offline REBUILD holds a schema-modification lock that blocks every reader and writer of the table for its
            // whole duration. The maintenance pause holds this adapter's services, not the customer's application writing a
            // live queue, so a live queue's index is REORGANIZEd instead, whatever its fragmentation: REORGANIZE is always
            // online, needs no particular edition, and keeps the work it has done if it is stopped.
            var liveQueue = IsLiveQueueTable(row.TableName);
            if (row.PctHighFragPartitions > DigMaintenanceThresholds.MSSQL_ReindexByTableThreshold_HighFragPartitionPct)
            {
                return liveQueue ? DigLevel2Action.Reorganize : DigLevel2Action.RebuildIndex;
            }
            if (row.FragmentationPct > DigMaintenanceThresholds.MSSQL_ReindexThreshold_FragmentationPct)
            {
                return liveQueue ? DigLevel2Action.Reorganize : DigLevel2Action.RebuildPartition;
            }
            if (row.FragmentationPct > DigMaintenanceThresholds.MSSQL_ReorganizeThreshold_FragmentationPct)
            {
                return DigLevel2Action.Reorganize;
            }
            return DigLevel2Action.None;
        }
    }
}
