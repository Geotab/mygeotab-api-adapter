using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.DIGInvalidRecords database table.
    /// </summary>
    [Table("gda.DIGInvalidRecords")]
    public class DbGdaDIGInvalidRecord : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.DIGInvalidRecords";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }

        /// <summary>
        /// The unique GUID assigned by Geotab to identify this record.
        /// </summary>
        public string GeotabGUID { get; set; } = string.Empty;

        /// <summary>
        /// The type of the original record (e.g., "GpsRecord", "AccelerationRecord").
        /// </summary>
        public string RecordType { get; set; } = string.Empty;

        /// <summary>
        /// The serial number of the device that generated the record.
        /// </summary>
        public string SerialNo { get; set; } = string.Empty;

        /// <summary>
        /// The date and time of the original record.
        /// </summary>
        public DateTime RecordDateTime { get; set; }

        /// <summary>
        /// The original base record serialized as JSON.
        /// </summary>
        public string BaseRecordJson { get; set; } = string.Empty;

        /// <summary>
        /// The reason why the record was marked as invalid.
        /// </summary>
        public string Cause { get; set; } = string.Empty;

        /// <summary>
        /// The timestamp when the record was marked as invalid by the DIG API.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// The user ID associated with the invalid record.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// The date and time when this record was retrieved from the DIG API.
        /// </summary>
        public DateTime RetrievedAtUtc { get; set; }

        /// <summary>
        /// The date and time when this record was created in the database.
        /// </summary>
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
