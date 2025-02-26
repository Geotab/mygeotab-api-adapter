using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("vwStatsForLevel2DBMaintenance")]
    public class DbvwStatForLevel2DBMaintenance_MSSQL : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "vwStatsForLevel2DBMaintenance";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
        public long RowId { get; set; }
        public string SchemaName { get; set; }
        public string TableName { get; set; }
        public string IndexName { get; set; }
        public long PartitionNumber { get; set; }
        public double FragmentationPct { get; set; }
        public long IndexSizeKB { get; set; }
        public long TotalPartitions { get; set; }
        public long HighFragPartitions { get; set; }
        public double PctHighFragPartitions { get; set; }
    }
}
