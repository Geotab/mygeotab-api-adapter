using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A row of the SQL Server gda.vwStatsForLevel2DigMaintenance view: a partition of a gda index that is more than 10 percent fragmented. Its percentage columns are PERCENTAGES (0..100) - see <see cref="DigMaintenanceThresholds"/>.
    /// </summary>
    [Table("gda.vwStatsForLevel2DigMaintenance")]
    public class DbGdavwStatForLevel2DigMaintenance_MSSQL : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.vwStatsForLevel2DigMaintenance";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
        public long RowId { get; set; }
        public string SchemaName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string IndexName { get; set; } = string.Empty;
        public int PartitionNumber { get; set; }

        /// <summary>
        /// Fragmentation of this partition of the index, as a PERCENTAGE (0..100).
        /// </summary>
        public double FragmentationPct { get; set; }
        public long IndexSizeKB { get; set; }

        /// <summary>
        /// The number of partitions of the whole index.
        /// </summary>
        public int TotalPartitions { get; set; }

        /// <summary>
        /// The number of partitions of the whole index that are more than 30 percent fragmented.
        /// </summary>
        public int HighFragPartitions { get; set; }

        /// <summary>
        /// <see cref="HighFragPartitions"/> as a PERCENTAGE (0..100) of <see cref="TotalPartitions"/>.
        /// </summary>
        public double PctHighFragPartitions { get; set; }
    }
}
