using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.Q_J1939FaultRecordsFail database table.
    /// </summary>
    [Table("gda.Q_J1939FaultRecordsFail")]
    public class DbGdaQJ1939FaultRecordFail : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.Q_J1939FaultRecordsFail";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }

        public long OriginalQueueId { get; set; }

        public string ThirdPartyId { get; set; }

        public DateTime DateTime { get; set; }

        public int SuspectParameterNumber { get; set; }

        public byte FailureModeIdentifier { get; set; }

        public byte OccurrenceCount { get; set; }

        public byte SourceAddress { get; set; }

        public bool? MalfunctionLamp { get; set; }

        public bool? RedStopLamp { get; set; }

        public bool? AmberWarningLamp { get; set; }

        public bool? ProtectWarningLamp { get; set; }

        public bool FaultStateActive { get; set; }

        public DateTime OriginalRecordLastChangedUtc { get; set; }

        public string FailureReason { get; set; }

        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
