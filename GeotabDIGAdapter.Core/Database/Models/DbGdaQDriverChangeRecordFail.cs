using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.Q_DriverChangeRecordsFail database table.
    /// </summary>
    [Table("gda.Q_DriverChangeRecordsFail")]
    public class DbGdaQDriverChangeRecordFail : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.Q_DriverChangeRecordsFail";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }

        public long OriginalQueueId { get; set; }

        public string ThirdPartyId { get; set; }

        public DateTime DateTime { get; set; }

        public byte KeyType { get; set; }

        public byte[] DriverId { get; set; }

        public DateTime OriginalRecordLastChangedUtc { get; set; }

        public string FailureReason { get; set; }

        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
