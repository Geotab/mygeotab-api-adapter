using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using System;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Unit tests covering the decisions the Geotab DIG Adapter's Level 1 and Level 2 database maintenance make: when the
    /// DatabaseMaintenanceService wakes, when each kind of maintenance is due, whether a Level 2 run may start and when it
    /// stops, and what each candidate row calls for.
    /// <para>
    /// The row values are given in the units the views actually return - percentages on SQL Server, ratios on PostgreSQL -
    /// so that a threshold written in the wrong unit shows up here as a wrong decision, not only as a mismatched number.
    /// </para>
    /// </summary>
    public class DigMaintenancePolicyTests
    {
        // ---------------------------------------------------------------------------------------------- the service's tick

        [Fact]
        public void Tick_WithTheShippedDefaults_IsTheLevel1Interval()
        {
            // IntervalMinutes 1440, Level 1 every 30, Level 2 every 1440, window disabled.
            Assert.Equal(30, DigMaintenancePolicy.GetServiceTickMinutes(1440, true, 30, true, 1440, false, 60));
        }

        [Fact]
        public void Tick_WithBothLevelsDisabled_IsTheServiceIntervalUnchanged()
        {
            // An operator who switches both levels off gets exactly the cadence the service had before they existed.
            Assert.Equal(1440, DigMaintenancePolicy.GetServiceTickMinutes(1440, false, 30, false, 60, true, 45));
        }

        [Fact]
        public void Tick_IgnoresTheIntervalOfADisabledLevel()
        {
            Assert.Equal(1440, DigMaintenancePolicy.GetServiceTickMinutes(1440, false, 30, true, 1440, false, 60));
        }

        [Fact]
        public void Tick_WithTheLevel2WindowEnabled_IsAtMostHalfTheWindow()
        {
            // Waking once a day could sleep straight across a one-hour window every day, and Level 2 would never run. Waking
            // once an hour is not enough either: each iteration's own work adds to the gap between wakes, so two wakes could
            // fall either side of the window.
            Assert.Equal(30, DigMaintenancePolicy.GetServiceTickMinutes(1440, false, 30, true, 1440, true, 60));
            Assert.Equal(1, DigMaintenancePolicy.GetServiceTickMinutes(1440, false, 30, true, 1440, true, 1));
        }

        [Fact]
        public void Tick_IsNeverLessThanOneMinute()
        {
            Assert.Equal(1, DigMaintenancePolicy.GetServiceTickMinutes(0, false, 30, false, 60, false, 60));
        }

        // ---------------------------------------------------------------------------------------------------- due-ness

        static readonly DateTime Now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

        static DbGdaDigMaintenanceLog Log(DateTime startTimeUtc, DateTime? endTimeUtc, bool success = true) =>
            new() { StartTimeUtc = startTimeUtc, EndTimeUtc = endTimeUtc, Success = success };

        [Fact]
        public void Due_WhenItHasNeverRun()
        {
            Assert.True(DigMaintenancePolicy.IsDue(null, 30, Now));
        }

        [Fact]
        public void NotDue_BeforeTheIntervalHasElapsedSinceTheLastStart()
        {
            Assert.False(DigMaintenancePolicy.IsDue(Log(Now.AddMinutes(-29), Now.AddMinutes(-20)), 30, Now));
        }

        [Fact]
        public void Due_OnceTheIntervalHasElapsedSinceTheLastStart()
        {
            // Measured from the START of the last run, so a long run does not push the next one back by its own length.
            Assert.True(DigMaintenancePolicy.IsDue(Log(Now.AddMinutes(-30), Now.AddMinutes(-1)), 30, Now));
        }

        [Fact]
        public void NotDue_WhileARunHasNotEnded()
        {
            Assert.False(DigMaintenancePolicy.IsDue(Log(Now.AddDays(-2), null), 30, Now));
        }

        [Fact]
        public void AFailedRun_StillSpacesOutTheNextAttempt()
        {
            // Otherwise a failure that recurs would be retried on every wake - and a failing Level 2 would pause telemetry
            // each time, only to fail again.
            Assert.False(DigMaintenancePolicy.IsDue(Log(Now.AddMinutes(-10), Now.AddMinutes(-9), success: false), 1440, Now));
        }

        [Fact]
        public void AFailedRun_IsSpacedFromItsEnd_SoARunThatOutlastedTheIntervalIsNotDueAgainAtOnce()
        {
            // A run that failed after running longer than the interval - a statement stopped at its one-hour limit - would be
            // due again the moment it ended if it were spaced from its start, and would pause the other services again at once.
            Assert.False(DigMaintenancePolicy.IsDue(Log(Now.AddMinutes(-70), Now.AddMinutes(-5), success: false), 60, Now));
            Assert.True(DigMaintenancePolicy.IsDue(Log(Now.AddMinutes(-130), Now.AddMinutes(-65), success: false), 60, Now));
        }

        // ------------------------------------------------------------------ why pgstattuple cannot be used, and the remedy

        [Fact]
        public void PgstattupleProblem_Absent_PrescribesCreatingIt()
        {
            var text = DigMaintenancePolicy.DescribePgstattupleProblem(existsInPublic: false, extensionSchema: null, "geotabdigadapter_client");
            Assert.Contains("is not installed", text);
            Assert.Contains("CREATE EXTENSION IF NOT EXISTS pgstattuple WITH SCHEMA public;", text);
        }

        [Fact]
        public void PgstattupleProblem_InstalledElsewhere_PrescribesMovingIt_NotCreatingIt()
        {
            // CREATE EXTENSION IF NOT EXISTS is a no-op when the extension lives in another schema, so prescribing it would
            // leave Level 2 skipped for ever.
            var text = DigMaintenancePolicy.DescribePgstattupleProblem(existsInPublic: false, extensionSchema: "extensions", "geotabdigadapter_client");
            Assert.Contains("schema extensions", text);
            Assert.Contains("ALTER EXTENSION pgstattuple SET SCHEMA public;", text);
            Assert.DoesNotContain("CREATE EXTENSION", text);
        }

        [Fact]
        public void PgstattupleProblem_OldVersionInPublic_PrescribesUpdatingIt_NotMovingIt()
        {
            // An extension carried forward by pg_upgrade without ALTER EXTENSION ... UPDATE can be in public and still lack
            // pgstattuple(regclass); moving it to the schema it is already in would change nothing.
            var text = DigMaintenancePolicy.DescribePgstattupleProblem(existsInPublic: false, extensionSchema: "public", "geotabdigadapter_client");
            Assert.Contains("ALTER EXTENSION pgstattuple UPDATE;", text);
            Assert.DoesNotContain("SET SCHEMA", text);
            Assert.DoesNotContain("CREATE EXTENSION", text);
        }

        [Fact]
        public void PgstattupleProblem_NotGranted_PrescribesTheGrantOnly()
        {
            var text = DigMaintenancePolicy.DescribePgstattupleProblem(existsInPublic: true, extensionSchema: "public", "geotabdigadapter_client");
            Assert.Contains("GRANT EXECUTE ON FUNCTION public.pgstattuple(regclass) TO geotabdigadapter_client;", text);
            Assert.DoesNotContain("CREATE EXTENSION", text);
            Assert.DoesNotContain("ALTER EXTENSION", text);
        }

        [Fact]
        public void ASuccessfulRun_IsStillSpacedFromItsStart()
        {
            Assert.True(DigMaintenancePolicy.IsDue(Log(Now.AddMinutes(-70), Now.AddMinutes(-5)), 60, Now));
        }

        // --------------------------------------------------------------------------------------------- the Level 2 window

        static readonly DateTime SixAm = new(2020, 6, 23, 6, 0, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData(0, 30)]
        [InlineData(6, 30)]
        [InlineData(12, 0)]
        [InlineData(23, 59)]
        public void WithTheWindowDisabled_ARunMayStartAtAnyTime_AndHasNoStopTime(int hour, int minute)
        {
            // The MyGeotab API Adapter applied the end of the configured window - 07:00 UTC with the shipped defaults - as a
            // stop time even with the window disabled, so any run starting after it stopped before maintaining anything and
            // recorded a success. A disabled window must mean no stop time at all, whatever the window settings say.
            var nowUtc = new DateTime(2026, 9, 23, hour, minute, 0, DateTimeKind.Utc);

            var (mayStart, stopTimeUtc) = DigMaintenancePolicy.EvaluateLevel2Window(false, SixAm, 60, nowUtc);

            Assert.True(mayStart);
            Assert.Null(stopTimeUtc);
        }

        [Fact]
        public void WithTheWindowEnabled_ARunInsideItStopsAtItsEnd()
        {
            var (mayStart, stopTimeUtc) = DigMaintenancePolicy.EvaluateLevel2Window(true, SixAm, 60, new DateTime(2026, 9, 23, 6, 20, 0, DateTimeKind.Utc));

            Assert.True(mayStart);
            Assert.Equal(new DateTime(2026, 9, 23, 7, 0, 0), stopTimeUtc);
        }

        [Theory]
        [InlineData(5, 59)]
        [InlineData(7, 1)]
        [InlineData(18, 0)]
        public void WithTheWindowEnabled_ARunOutsideItMayNotStart(int hour, int minute)
        {
            var (mayStart, _) = DigMaintenancePolicy.EvaluateLevel2Window(true, SixAm, 60, new DateTime(2026, 9, 23, hour, minute, 0, DateTimeKind.Utc));

            Assert.False(mayStart);
        }

        [Fact]
        public void AWindowCrossingMidnight_CoversTheTimeAfterMidnight()
        {
            // 23:30 plus an hour ends at 00:30. At 00:10 today's window has not started, but yesterday's is still open.
            var elevenThirty = new DateTime(2020, 6, 23, 23, 30, 0, DateTimeKind.Utc);

            var (mayStart, stopTimeUtc) = DigMaintenancePolicy.EvaluateLevel2Window(true, elevenThirty, 60, new DateTime(2026, 9, 24, 0, 10, 0, DateTimeKind.Utc));

            Assert.True(mayStart);
            Assert.Equal(new DateTime(2026, 9, 24, 0, 30, 0), stopTimeUtc);
        }

        [Fact]
        public void AWindowCrossingMidnight_CoversTheTimeBeforeMidnight()
        {
            var elevenThirty = new DateTime(2020, 6, 23, 23, 30, 0, DateTimeKind.Utc);

            var (mayStart, stopTimeUtc) = DigMaintenancePolicy.EvaluateLevel2Window(true, elevenThirty, 60, new DateTime(2026, 9, 23, 23, 45, 0, DateTimeKind.Utc));

            Assert.True(mayStart);
            Assert.Equal(new DateTime(2026, 9, 24, 0, 30, 0), stopTimeUtc);
        }

        [Fact]
        public void AWindowCrossingMidnight_IsClosedOnceItEnds()
        {
            var elevenThirty = new DateTime(2020, 6, 23, 23, 30, 0, DateTimeKind.Utc);

            var (mayStart, _) = DigMaintenancePolicy.EvaluateLevel2Window(true, elevenThirty, 60, new DateTime(2026, 9, 24, 0, 31, 0, DateTimeKind.Utc));

            Assert.False(mayStart);
        }

        // ----------------------------------------------------------------------------------------- Level 1, PostgreSQL

        static DbGdavwStatForLevel1DigMaintenance_PG PgTable(decimal? deadTupleRatio, long deadTuples, decimal? modsRatio) =>
            new() { SchemaName = "gda", TableName = "Q_GpsRecords", PctDeadTuples = deadTupleRatio, DeadTuples = deadTuples, PctModsSinceLastAnalyze = modsRatio };

        [Fact]
        public void PostgreSQLLevel1_VacuumsAboveTheDeadTupleRatio()
        {
            // A ratio of 0.25: a quarter of the table is dead tuples.
            Assert.Equal(DigLevel1Action.VacuumAndAnalyze, DigMaintenancePolicy.DecideLevel1(PgTable(0.25m, 50, 0m)));
        }

        [Fact]
        public void PostgreSQLLevel1_VacuumsAboveTheDeadTupleCount_EvenWithNoLiveTuples()
        {
            // An emptied queue: no live tuples, so no ratio, and the count is what decides.
            Assert.Equal(DigLevel1Action.VacuumAndAnalyze, DigMaintenancePolicy.DecideLevel1(PgTable(null, 1001, null)));
        }

        [Fact]
        public void PostgreSQLLevel1_AnalyzesAboveTheModificationRatio()
        {
            Assert.Equal(DigLevel1Action.Analyze, DigMaintenancePolicy.DecideLevel1(PgTable(0.05m, 10, 0.15m)));
        }

        [Fact]
        public void PostgreSQLLevel1_LeavesATableBelowEveryThreshold()
        {
            Assert.Equal(DigLevel1Action.None, DigMaintenancePolicy.DecideLevel1(PgTable(0.19m, 1000, 0.09m)));
        }

        // ----------------------------------------------------------------------------------------- Level 1, SQL Server

        static DbGdavwStatForLevel1DigMaintenance_MSSQL MssqlTable(long mods, double pctMods) =>
            new() { SchemaName = "gda", TableName = "Q_GpsRecords", ModsSinceLastStatsUpdate = mods, PctModsSinceLastStatsUpdate = pctMods };

        [Fact]
        public void SqlServerLevel1_UpdatesStatisticsAboveTenPercent()
        {
            // The view returns a PERCENTAGE: 30 means 30% of the table's rows.
            Assert.Equal(DigLevel1Action.UpdateStatistics, DigMaintenancePolicy.DecideLevel1(MssqlTable(30000, 30)));
        }

        [Theory]
        [InlineData(2.5)]
        [InlineData(0.5)]
        public void SqlServerLevel1_LeavesATableModifiedByLessThanTenPercent(double pctMods)
        {
            // 0.5 means half of one percent. The MyGeotab API Adapter's pre-fix threshold of 0.1 acted on it.
            Assert.Equal(DigLevel1Action.None, DigMaintenancePolicy.DecideLevel1(MssqlTable(5000, pctMods)));
        }

        [Fact]
        public void SqlServerLevel1_LeavesATableWithTooFewModifications()
        {
            Assert.Equal(DigLevel1Action.None, DigMaintenancePolicy.DecideLevel1(MssqlTable(1000, 50)));
        }

        // ----------------------------------------------------------------------------------------- Level 2, PostgreSQL

        static DbGdavwStatForLevel2DigMaintenance_PG PgIndex(double bloatRatio, long sizeBytes, string tableName = "DIGInvalidRecords", string indexName = "IX_DIGInvalidRecords_RecordCreationTimeUtc") =>
            new() { SchemaName = "gda", TableName = tableName, IndexName = indexName, IndexBloatRatio = bloatRatio, IndexSize = sizeBytes };

        [Fact]
        public void PostgreSQLLevel2_ReindexesABloatedIndex()
        {
            Assert.Equal(DigLevel2Action.Reindex, DigMaintenancePolicy.DecideLevel2(PgIndex(0.35, 16384)));
        }

        [Fact]
        public void PostgreSQLLevel2_ReindexesALiveQueueIndexConcurrently()
        {
            // A live queue is written by the customer's application, which the maintenance pause does not reach, and a plain
            // REINDEX blocks every writer of the table for the whole rebuild.
            Assert.Equal(DigLevel2Action.ReindexConcurrently, DigMaintenancePolicy.DecideLevel2(PgIndex(0.35, 16384, "Q_GpsRecords", "IX_Q_GpsRecords_PendingWork")));
        }

        [Theory]
        [InlineData("Q_GpsRecords", true)]
        [InlineData("Q_J1708FaultRecords", true)]
        [InlineData("Q_ProvisionDevices", true)]
        [InlineData("Q_DeviceSubscriptions", true)]
        [InlineData("Q_GpsRecordsFail", false)]
        [InlineData("Q_GpsRecordsFail_default", false)]
        [InlineData("DIGInvalidRecords", false)]
        [InlineData("DeviceSubscriptions", false)]
        [InlineData("ProvisionedDevices", false)]
        public void IsLiveQueueTable_TellsTheQueuesTheCustomerWritesFromTheTablesOnlyTheAdapterWrites(string tableName, bool expected)
        {
            Assert.Equal(expected, DigMaintenancePolicy.IsLiveQueueTable(tableName));
        }

        [Theory]
        [InlineData(DigLevel2Action.Reindex, true)]
        [InlineData(DigLevel2Action.RebuildPartition, true)]
        [InlineData(DigLevel2Action.RebuildIndex, true)]
        [InlineData(DigLevel2Action.ReindexConcurrently, false)]
        [InlineData(DigLevel2Action.Reorganize, false)]
        [InlineData(DigLevel2Action.None, false)]
        public void OnlyOfflineOperations_NeedTheOtherServicesPaused(DigLevel2Action action, bool expected)
        {
            // Offline rebuilds block the table's writers, so the adapter's own services are held still for them. Online ones
            // block no writer, and a concurrent rebuild can wait a long time for other transactions - it must not hold
            // telemetry still while it does.
            Assert.Equal(expected, DigMaintenancePolicy.RequiresPause(action));
        }

        [Theory]
        [InlineData(0.35, 1000)]
        [InlineData(0.3, 16384)]
        [InlineData(0.1, 16384)]
        public void PostgreSQLLevel2_LeavesAnIndexBelowEitherThreshold(double bloatRatio, long sizeBytes)
        {
            Assert.Equal(DigLevel2Action.None, DigMaintenancePolicy.DecideLevel2(PgIndex(bloatRatio, sizeBytes)));
        }

        // ----------------------------------------------------------------------------------------- Level 2, SQL Server

        static DbGdavwStatForLevel2DigMaintenance_MSSQL MssqlPartition(double fragmentationPct, int totalPartitions, int highFragPartitions, long sizeKB = 64) =>
            new()
            {
                SchemaName = "gda", TableName = "Q_GpsRecordsFail", IndexName = "IX_Q_GpsRecordsFail_ThirdPartyId", PartitionNumber = 1,
                FragmentationPct = fragmentationPct, IndexSizeKB = sizeKB, TotalPartitions = totalPartitions, HighFragPartitions = highFragPartitions,
                PctHighFragPartitions = totalPartitions > 0 ? 100.0 * highFragPartitions / totalPartitions : 0
            };

        [Fact]
        public void SqlServerLevel2_RebuildsTheWholeIndex_WhenMostOfItsPartitionsAreHighlyFragmented()
        {
            Assert.Equal(DigLevel2Action.RebuildIndex, DigMaintenancePolicy.DecideLevel2(MssqlPartition(45, 12, 7)));
        }

        [Fact]
        public void SqlServerLevel2_RebuildsOnlyThePartition_WhenOneOfTwelveIsHighlyFragmented()
        {
            // One fragmented partition in twelve is 8.3% of them. The MyGeotab API Adapter's pre-fix threshold of 0.5 read that
            // as more than half, and rebuilt all twelve.
            Assert.Equal(DigLevel2Action.RebuildPartition, DigMaintenancePolicy.DecideLevel2(MssqlPartition(35, 12, 1)));
        }

        [Fact]
        public void SqlServerLevel2_ReorganizesAModeratelyFragmentedPartition()
        {
            Assert.Equal(DigLevel2Action.Reorganize, DigMaintenancePolicy.DecideLevel2(MssqlPartition(20, 12, 0)));
        }

        [Theory]
        [InlineData(45, 1, 1)]
        [InlineData(35, 1, 1)]
        public void SqlServerLevel2_NeverRebuildsALiveQueueIndexOffline_ItReorganizesIt(double fragmentationPct, int totalPartitions, int highFragPartitions)
        {
            // An offline REBUILD holds a schema-modification lock that blocks every reader and writer of the table - here the
            // customer's own application, which the pause does not reach. REORGANIZE is always online.
            var row = MssqlPartition(fragmentationPct, totalPartitions, highFragPartitions);
            row.TableName = "Q_GpsRecords";
            row.IndexName = "IX_Q_GpsRecords_RecordCreationTimeUtc";
            Assert.Equal(DigLevel2Action.Reorganize, DigMaintenancePolicy.DecideLevel2(row));
        }

        [Theory]
        [InlineData(10, 64)]
        [InlineData(50, 1)]
        public void SqlServerLevel2_LeavesAPartitionBelowTheReorganizeThresholdOrTooSmall(double fragmentationPct, long sizeKB)
        {
            Assert.Equal(DigLevel2Action.None, DigMaintenancePolicy.DecideLevel2(MssqlPartition(fragmentationPct, 1, fragmentationPct > 30 ? 1 : 0, sizeKB)));
        }
    }
}
