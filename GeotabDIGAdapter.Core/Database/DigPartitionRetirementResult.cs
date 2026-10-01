namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// A row returned by spRetireDigPartitions. The procedure returns one row per partition it retired or declined to retire, followed by a summary row. In report mode it returns exactly the same rows, having changed nothing.
    /// </summary>
    public class DigPartitionRetirementResult
    {
        /// <summary>
        /// The database object the row refers to, or an empty value on the summary row.
        /// </summary>
        public string? Object { get; set; }

        /// <summary>
        /// What was done, or what would have been done in report mode.
        /// </summary>
        public string? Action { get; set; }

        /// <summary>
        /// A human-readable explanation, including the reason where a partition was not retirable.
        /// </summary>
        public string? Detail { get; set; }
    }

    /// <summary>
    /// The minimum RecordCreationTimeUtc found across the partitioned gda tables, used to decide the earliest period spManageDigPartitions must create on its first run.
    /// </summary>
    public class DigOldestRecordResult
    {
        /// <summary>
        /// The oldest value found, or null when every partitioned table is empty.
        /// </summary>
        public DateTime? OldestRecordCreationTimeUtc { get; set; }
    }

    /// <summary>
    /// The outcome of one batch of the DIGInvalidRecords retention sweep. That table cannot be partitioned, so its old records are removed by a batched DELETE rather than by dropping a partition.
    /// </summary>
    public class DigInvalidRecordDeletionResult
    {
        /// <summary>
        /// The number of records the batch deleted. A value below the batch size means nothing eligible was left, which is how the sweep knows to stop.
        /// </summary>
        public int DeletedRecordCount { get; set; }
    }

    /// <summary>
    /// The name of a gda table whose depth is reported, discovered from the database catalog.
    /// </summary>
    public class DigCountableTable
    {
        /// <summary>
        /// The unqualified table name. It always comes from the catalog, never from configuration.
        /// </summary>
        public string TableName { get; set; }
    }

    /// <summary>
    /// The number of records one gda queue, fail or invalid record table currently holds.
    /// </summary>
    public class DigTableDepth
    {
        /// <summary>
        /// The unqualified table name.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The number of records the table holds. Counted exactly rather than estimated from the catalog, because PostgreSQL's estimate is only as current as the last time the table was analyzed and a queue that fills between two analyses is precisely the case this is here to show.
        /// </summary>
        public long Depth { get; set; }
    }

    /// <summary>
    /// What the PostgreSQL pgstattuple probe found: whether public.pgstattuple() may be executed and, when it may not, why.
    /// </summary>
    public class DigPgstattupleProbeResult
    {
        /// <summary>
        /// Whether public.pgstattuple(regclass) exists and the adapter's role may execute it.
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Whether public.pgstattuple(regclass) exists at all.
        /// </summary>
        public bool ExistsInPublic { get; set; }

        /// <summary>
        /// The schema the pgstattuple extension is installed in, or null when it is not installed.
        /// </summary>
        public string? ExtensionSchema { get; set; }
    }

    /// <summary>
    /// An INVALID gda index left behind by an interrupted concurrent rebuild (PostgreSQL), found so that it can be dropped.
    /// </summary>
    public class DigInvalidIndex
    {
        /// <summary>
        /// The unqualified index name. It always comes from the catalog.
        /// </summary>
        public string IndexName { get; set; }
    }

    /// <summary>
    /// The answer to a yes-or-no question put to the database about what it can do - for example whether public.pgstattuple() exists and may be executed.
    /// </summary>
    public class DigCapabilityProbeResult
    {
        /// <summary>
        /// Whether the capability is available.
        /// </summary>
        public bool IsAvailable { get; set; }
    }

    /// <summary>
    /// A partitioned gda table discovered from the database catalog, together with the column it is partitioned on.
    /// </summary>
    public class DigPartitionedTable
    {
        /// <summary>
        /// The unqualified table name. It always comes from the catalog, never from configuration.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The name of the single column the table is RANGE-partitioned on.
        /// </summary>
        public string KeyColumnName { get; set; }
    }
}
