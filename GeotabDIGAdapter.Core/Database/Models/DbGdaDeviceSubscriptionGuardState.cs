using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.DeviceSubscriptionGuardState database table. This single-row table holds the tripped state of the termination guard. The trip is sticky until an operator clears it: without persistence, a restart would silently re-run the very reconciliation the guard just refused.
    /// </summary>
    [Table("gda.DeviceSubscriptionGuardState")]
    public class DbGdaDeviceSubscriptionGuardState : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.DeviceSubscriptionGuardState";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <summary>
        /// The primary key. Always 1 for this single-row table.
        /// </summary>
        [ExplicitKey]
        public short id { get; set; } = 1;

        /// <summary>
        /// Whether the termination guard has tripped. While this is set, the merge applies nothing.
        /// </summary>
        public bool IsTripped { get; set; }

        public DateTime? TrippedDateTimeUtc { get; set; }

        public string TrippedReason { get; set; }

        /// <summary>
        /// The number of terminations the refused batch would have applied.
        /// </summary>
        public int? TrippedTerminationCount { get; set; }

        /// <summary>
        /// The number of terminations already applied within the rolling window at the moment the guard tripped.
        /// </summary>
        public int? TrippedWindowCount { get; set; }

        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
