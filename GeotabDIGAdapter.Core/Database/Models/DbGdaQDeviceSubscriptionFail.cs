using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.Q_DeviceSubscriptionsFail database table. Note that <see cref="RecordCreationTimeUtc"/> records when the failure was captured, not when the original instruction was queued.
    /// </summary>
    [Table("gda.Q_DeviceSubscriptionsFail")]
    public class DbGdaQDeviceSubscriptionFail : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.Q_DeviceSubscriptionsFail";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }

        /// <summary>
        /// The id of the queue record the failure originated from. Null for an instruction that was staged directly rather than queued.
        /// </summary>
        public long? OriginalQueueId { get; set; }

        /// <summary>
        /// The ordering ordinal of the instruction that failed. Null for an instruction that was staged directly rather than queued.
        /// </summary>
        public long? OriginalOrderingOrdinal { get; set; }

        public byte OperationType { get; set; }

        public string ThirdPartyId { get; set; }

        public string IdentifierType { get; set; }

        public string GeotabSerialNumber { get; set; }

        public string SubPlan { get; set; }

        public string SourceStatus { get; set; }

        public DateTime? EffectiveDateTimeUtc { get; set; }

        public string TerminationReason { get; set; }

        public string SourceReference { get; set; }

        public DateTime? OriginalRecordCreationTimeUtc { get; set; }

        public DateTime? OriginalRecordLastChangedUtc { get; set; }

        public string FailureReason { get; set; }

        /// <summary>
        /// When the failure was recorded.
        /// </summary>
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
