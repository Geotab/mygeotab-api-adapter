using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.Q_DeviceSubscriptions database table. An external process inserts Subscribe and Terminate instructions into this queue; the DeviceSubscriptionService claims, stages and merges them.
    /// </summary>
    [Table("gda.Q_DeviceSubscriptions")]
    public class DbGdaQDeviceSubscription : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.Q_DeviceSubscriptions";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }

        /// <summary>
        /// The instruction's position in the global application order, drawn from the gda.SeqDeviceSubscriptionOrdinal sequence. This is what the claim procedure orders by and what the merge compares against each subscription's high-water mark, so that an instruction cannot be applied out of order or replayed.
        /// </summary>
        public long OrderingOrdinal { get; set; }

        /// <summary>
        /// What this instruction asks for. See <see cref="DeviceSubscriptionOperationType"/> for the values and for why they are numbered the way they are.
        /// </summary>
        public byte OperationType { get; set; }

        public string ThirdPartyId { get; set; }

        public string IdentifierType { get; set; }

        public string GeotabSerialNumber { get; set; }

        public string SubPlan { get; set; }

        /// <summary>
        /// The subscription status as reported by the source, where the source supplies one.
        /// </summary>
        public string SourceStatus { get; set; }

        public DateTime? EffectiveDateTimeUtc { get; set; }

        public string TerminationReason { get; set; }

        /// <summary>
        /// Identifies where the instruction came from, so that an applied instruction can be traced back to its source after the queue record is deleted.
        /// </summary>
        public string SourceReference { get; set; }

        public DateTime RecordCreationTimeUtc { get; set; }

        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }

        public byte ProcessingStatus { get; set; }

        public DateTime? ProcessingStartTimeUtc { get; set; }

        public Guid? ClaimId { get; set; }

        public byte RetryCount { get; set; }
    }
}
