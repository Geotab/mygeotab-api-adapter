using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A row of the PostgreSQL gda.vwStatsForLevel1DigMaintenance view: a gda table that may need VACUUM and ANALYZE, or ANALYZE alone. Its two "Pct" columns are RATIOS (0..1), not percentages - the names follow the MyGeotab API Adapter's view. See <see cref="DigMaintenanceThresholds"/>.
    /// </summary>
    [Table("gda.vwStatsForLevel1DigMaintenance")]
    public class DbGdavwStatForLevel1DigMaintenance_PG : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.vwStatsForLevel1DigMaintenance";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
        public long RowId { get; set; }
        public string SchemaName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public long LiveTuples { get; set; }
        public long DeadTuples { get; set; }

        /// <summary>
        /// Dead tuples as a RATIO (0..1) of live tuples, or null for a table with no live tuples.
        /// </summary>
        public decimal? PctDeadTuples { get; set; }
        public long ModsSinceLastAnalyze { get; set; }

        /// <summary>
        /// <see cref="ModsSinceLastAnalyze"/> as a RATIO (0..1) of live tuples, or null for a table with no live tuples.
        /// </summary>
        public decimal? PctModsSinceLastAnalyze { get; set; }
    }
}
