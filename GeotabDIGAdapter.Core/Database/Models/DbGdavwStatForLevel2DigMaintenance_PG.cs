using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A row of the PostgreSQL gda.vwStatsForLevel2DigMaintenance view: a gda index, with its size and bloat ratio. The bloat ratio is a RATIO (0..1) - see <see cref="DigMaintenanceThresholds"/>. Reading the view requires the pgstattuple extension, and EXECUTE on it.
    /// </summary>
    [Table("gda.vwStatsForLevel2DigMaintenance")]
    public class DbGdavwStatForLevel2DigMaintenance_PG : IDbEntity
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
        public string IndexSizeText { get; set; } = string.Empty;

        /// <summary>
        /// The size of the index, in bytes.
        /// </summary>
        public long IndexSize { get; set; }

        /// <summary>
        /// The index's free space as a RATIO (0..1) of its size.
        /// </summary>
        public double IndexBloatRatio { get; set; }
    }
}
