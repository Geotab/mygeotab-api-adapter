namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// The thresholds that decide which tables and indexes Level 1 and Level 2 database maintenance act on.
    /// <para>
    /// EVERY ONE OF THESE ALSO APPEARS IN A VIEW, OR IS COMPARED AGAINST A COLUMN ONE RETURNS, AND THE TWO MUST AGREE.
    /// <c>gda.vwStatsForLevel1DigMaintenance</c> and <c>gda.vwStatsForLevel2DigMaintenance</c> decide which rows are offered; the
    /// DatabaseMaintenanceService decides which of those it acts on. A disagreement between them does not fail - the view offers
    /// a candidate the service declines, or the service waits for one the view never offers - and the maintenance simply never
    /// happens. A unit test reads the view bodies out of the four schema scripts and compares them with these values.
    /// </para>
    /// <para>
    /// Each value is expressed in the SAME UNIT as the column it is compared against, and the two providers differ:
    /// <list type="bullet">
    /// <item>the SQL Server views return PERCENTAGES (their expressions end in "* 100"), so every <c>MSSQL_</c> constant compared
    /// against one is a percentage in the range 0..100, and is named "...Pct";</item>
    /// <item>the PostgreSQL views return RATIOS (a plain division, no "* 100"), so every <c>PG_</c> constant compared against one
    /// is a ratio in the range 0..1, and is named "...Ratio" - even where the column it is compared against is named "Pct...",
    /// which it is only because the MyGeotab API Adapter's column is.</item>
    /// </list>
    /// Getting this wrong is silent, and it has happened: the MyGeotab API Adapter's equivalents of two SQL Server constants
    /// below held 0.1 and 0.5, read as 10% and 50%, and acted at 0.1% and 0.5%. The names carry the unit so that the next
    /// threshold added here is written in the right one.
    /// </para>
    /// <para>
    /// The values match the MyGeotab API Adapter's (after the correction above), so that one understanding of what the two
    /// levels do covers both adapters.
    /// </para>
    /// </summary>
    public static class DigMaintenanceThresholds
    {
        /// <summary>
        /// SQL Server Level 1. A table is offered for UPDATE STATISTICS only once it has had more than this many modifications
        /// since its statistics were last updated. A COUNT. Also the view's own filter.
        /// </summary>
        public const long MSSQL_UpdateStatisticsThreshold_ModsSinceLastStatsUpdateCount = 1000;

        /// <summary>
        /// SQL Server Level 1. Statistics are updated where the modifications since they were last updated exceed this
        /// PERCENTAGE (0..100) of the table's rows.
        /// </summary>
        public const double MSSQL_UpdateStatisticsThreshold_PctModsSinceLastStatsUpdate = 10;

        /// <summary>
        /// SQL Server Level 2. An index partition more fragmented than this PERCENTAGE (0..100) is reorganized. Also the view's
        /// own filter, so it is the lowest fragmentation the view offers at all.
        /// </summary>
        public const double MSSQL_ReorganizeThreshold_FragmentationPct = 10;

        /// <summary>
        /// SQL Server Level 2. An index partition more fragmented than this PERCENTAGE (0..100) is rebuilt rather than
        /// reorganized. Also the view's definition of a highly fragmented partition.
        /// </summary>
        public const double MSSQL_ReindexThreshold_FragmentationPct = 30;

        /// <summary>
        /// SQL Server Level 2. The whole index is rebuilt, rather than its partitions one at a time, where more than this
        /// PERCENTAGE (0..100) of its partitions are highly fragmented.
        /// </summary>
        public const double MSSQL_ReindexByTableThreshold_HighFragPartitionPct = 50;

        /// <summary>
        /// SQL Server Level 2. An index partition is offered only when it is larger than this many KILOBYTES. Also the view's own
        /// filter.
        /// </summary>
        public const long MSSQL_MinimumIndexSizeKB = 1;

        /// <summary>
        /// PostgreSQL Level 1. A table is vacuumed and analyzed where its dead tuples exceed this RATIO (0..1) of its live tuples.
        /// </summary>
        public const double PG_VacuumThreshold_DeadTupleRatio = 0.2;

        /// <summary>
        /// PostgreSQL Level 1. A table is vacuumed and analyzed where it has more than this many dead tuples, whatever their
        /// ratio. A COUNT.
        /// </summary>
        public const long PG_VacuumThreshold_DeadTuplesCount = 1000;

        /// <summary>
        /// PostgreSQL Level 1. A table is analyzed where its modifications since it was last analyzed exceed this RATIO (0..1) of
        /// its live tuples.
        /// </summary>
        public const double PG_AnalyzeThreshold_ModsSinceLastAnalyzeRatio = 0.1;

        /// <summary>
        /// PostgreSQL Level 2. An index is reindexed where its free space exceeds this RATIO (0..1) of its size - and it is
        /// larger than <see cref="PG_ReindexThreshold_IndexSizeBytes"/>. The view applies no threshold of its own.
        /// </summary>
        public const double PG_ReindexThreshold_BloatRatio = 0.3;

        /// <summary>
        /// PostgreSQL Level 2. An index is reindexed only when it is larger than this many BYTES.
        /// </summary>
        public const long PG_ReindexThreshold_IndexSizeBytes = 1000;
    }
}
