using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.Q_BluetoothRecordsFail database table.
    /// </summary>
    [Table("gda.Q_BluetoothRecordsFail")]
    public class DbGdaQBluetoothRecordFail : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.Q_BluetoothRecordsFail";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }

        public long OriginalQueueId { get; set; }

        public string ThirdPartyId { get; set; }

        public DateTime DateTime { get; set; }

        public string Address { get; set; }

        public float Data { get; set; }

        public byte DataType { get; set; }

        public DateTime OriginalRecordLastChangedUtc { get; set; }

        public string FailureReason { get; set; }

        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
