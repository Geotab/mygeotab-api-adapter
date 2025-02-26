using Dapper.Contrib.Extensions;
using System;


namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("DBMaintenanceLogs2")]
    public class DbDBMaintenanceLog2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "DBMaintenanceLogs2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }
        public short MaintenanceTypeId { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; }
        public bool? Success { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
