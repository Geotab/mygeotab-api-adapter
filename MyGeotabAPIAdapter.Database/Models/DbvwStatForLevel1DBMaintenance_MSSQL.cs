using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("vwStatsForLevel1DBMaintenance")]
    public class DbvwStatForLevel1DBMaintenance_MSSQL : IDbEntity
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
        public long RecordCount { get; set; }
        public long UsedSpaceKB { get; set; }
        public long ReservedSpaceKB { get; set; }
        public long FreeSpaceKB { get; set; }
        public long ModsSinceLastStatsUpdate { get; set; }
        public double PctModsSinceLastStatsUpdate { get; set; }
        public DateTime LastStatsUpdate { get; set; }
    }
}
