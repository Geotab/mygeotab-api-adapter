using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("vwStatsForLevel2DBMaintenance")]
    public class DbvwStatForLevel2DBMaintenance_PG : IDbEntity
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
        public string IndexSizeText { get; set; }
        public long IndexSize { get; set; }
        public double IndexBloatRatio { get; set; }
    }
}
