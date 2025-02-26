using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("vwStatsForLevel1DBMaintenance")]
    public class DbvwStatForLevel1DBMaintenance_PG : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "vwStatsForLevel1DBMaintenance";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
        public long RowId { get; set; }
        public string SchemaName { get; set; }
        public string TableName { get; set; }
        public long LiveTuples { get; set; }
        public long DeadTuples { get; set; }
        public double? PctDeadTuples { get; set; }
        public long ModsSinceLastAnalyze { get; set; }
        public double? PctModsSinceLastAnalyze { get; set; }
    }
}
