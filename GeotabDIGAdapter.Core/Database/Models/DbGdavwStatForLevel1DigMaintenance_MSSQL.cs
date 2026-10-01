using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A row of the SQL Server gda.vwStatsForLevel1DigMaintenance view: a gda table with more than 1000 modifications since its statistics were last updated. Its percentage column is a PERCENTAGE (0..100) - see <see cref="DigMaintenanceThresholds"/>.
    /// </summary>
    [Table("gda.vwStatsForLevel1DigMaintenance")]
    public class DbGdavwStatForLevel1DigMaintenance_MSSQL : IDbEntity
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
        public long RecordCount { get; set; }
        public long UsedSpaceKB { get; set; }
        public long ReservedSpaceKB { get; set; }
        public long FreeSpaceKB { get; set; }
        public long ModsSinceLastStatsUpdate { get; set; }

        /// <summary>
        /// <see cref="ModsSinceLastStatsUpdate"/> as a PERCENTAGE (0..100) of <see cref="RecordCount"/>.
        /// </summary>
        public double PctModsSinceLastStatsUpdate { get; set; }
        public DateTime? LastStatsUpdate { get; set; }
    }
}
