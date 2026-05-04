using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.Q_BluetoothRecords database table.
    /// </summary>
    [Table("gda.Q_BluetoothRecords")]
    public class DbGdaQBluetoothRecord : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.Q_BluetoothRecords";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }

        public string ThirdPartyId { get; set; }

        public DateTime DateTime { get; set; }

        public string Address { get; set; }

        public float Data { get; set; }

        public byte DataType { get; set; }

        public DateTime RecordCreationTimeUtc { get; set; }

        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }

        public byte ProcessingStatus { get; set; }

        public DateTime? ProcessingStartTimeUtc { get; set; }

        public byte RetryCount { get; set; }
    }
}
