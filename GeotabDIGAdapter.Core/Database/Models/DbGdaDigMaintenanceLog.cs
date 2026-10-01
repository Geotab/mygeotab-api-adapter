using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.DigMaintenanceLogs database table: one row per database maintenance run. The DatabaseMaintenanceService reads the latest Level 1 and Level 2 rows back to decide when each is next due.
    /// </summary>
    [Table("gda.DigMaintenanceLogs")]
    public class DbGdaDigMaintenanceLog : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.DigMaintenanceLogs";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }

        /// <summary>
        /// The <see cref="DigMaintenanceType"/> id of the kind of maintenance that ran.
        /// </summary>
        public short MaintenanceTypeId { get; set; }

        /// <summary>
        /// When the run started. The interval until the next run of the same kind is measured from here.
        /// </summary>
        public DateTime StartTimeUtc { get; set; }

        /// <summary>
        /// When the run ended.
        /// </summary>
        public DateTime? EndTimeUtc { get; set; }

        /// <summary>
        /// Whether the run completed. False records a run that failed, or a Level 2 run skipped because it could not be performed.
        /// </summary>
        public bool? Success { get; set; }

        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
