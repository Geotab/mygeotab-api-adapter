using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.DeviceSubscriptions database table. This table records which devices the adapter has been told to transmit for. It is written exclusively by the spMerge_stg_DeviceSubscriptions stored procedure/function, never by the entity persister.
    /// </summary>
    [Table("gda.DeviceSubscriptions")]
    public class DbGdaDeviceSubscription : IDbEntity
    {
        /// <summary>
        /// The value of <see cref="SubscriptionStatus"/> for a subscription that has been terminated. Terminated rows are retained rather than deleted so that the audit trail remains unambiguous and a re-subscription is an update rather than an insert.
        /// </summary>
        public const byte SubscriptionStatusTerminated = 0;

        /// <summary>
        /// The value of <see cref="SubscriptionStatus"/> for an active subscription. Only devices with this status may be transmitted for.
        /// </summary>
        public const byte SubscriptionStatusActive = 1;

        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.DeviceSubscriptions";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }

        public string ThirdPartyId { get; set; }

        public string IdentifierType { get; set; }

        public string GeotabSerialNumber { get; set; }

        public byte SubscriptionStatus { get; set; }

        public string SubPlan { get; set; }

        /// <summary>
        /// The ordinal of the most recent instruction applied to this subscription. Instructions at or below this value are replays and are recorded as superseded rather than applied.
        /// </summary>
        public long LastAppliedOrdinal { get; set; }

        public DateTime? SubscribedDateTimeUtc { get; set; }

        public DateTime? TerminatedDateTimeUtc { get; set; }

        public string TerminationReason { get; set; }

        public DateTime? LastSeenInSnapshotUtc { get; set; }

        public DateTime RecordCreationTimeUtc { get; set; }

        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
