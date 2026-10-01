using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.stg_DeviceSubscriptions database table. Every intake stages its instructions here and then calls spMerge_stg_DeviceSubscriptions, so that no two intakes can disagree about how identical data is applied.
    /// </summary>
    [Table("gda.stg_DeviceSubscriptions")]
    public class DbGdaStgDeviceSubscription : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.stg_DeviceSubscriptions";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }

        /// <summary>
        /// Isolates this writer's rows from those of any other writer. The merge scopes every operation to a single batch and deletes only that batch's rows, rather than truncating the table.
        /// </summary>
        public Guid BatchId { get; set; }

        /// <summary>
        /// What this instruction asks for, carried through from the queue record it was staged from. See <see cref="DeviceSubscriptionOperationType"/> for the values and for why they are numbered the way they are.
        /// </summary>
        public byte OperationType { get; set; }

        /// <summary>
        /// The instruction's position in the global application order, carried through from the queue record it was staged from.
        /// </summary>
        public long Ordinal { get; set; }

        /// <summary>
        /// The queue record this row was staged from, so that a rejection can be traced back to it. Null for an instruction that was staged directly rather than queued.
        /// </summary>
        public long? OriginalQueueId { get; set; }

        public string ThirdPartyId { get; set; }

        public string IdentifierType { get; set; }

        public string GeotabSerialNumber { get; set; }

        public string SubPlan { get; set; }

        public string SourceStatus { get; set; }

        public DateTime? EffectiveDateTimeUtc { get; set; }

        public string TerminationReason { get; set; }

        public string SourceReference { get; set; }

        /// <summary>
        /// When the row was staged. Deliberately not written by the adapter: the database default supplies it, so that the stale-batch sweep compares two values from the same clock rather than one from the database and one from the application host.
        /// </summary>
        [Write(false)]
        public DateTime StagedDateTimeUtc { get; set; }
    }
}
