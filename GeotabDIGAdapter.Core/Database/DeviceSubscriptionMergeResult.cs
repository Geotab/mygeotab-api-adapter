namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// The mode spMerge_stg_DeviceSubscriptions is invoked in.
    /// </summary>
    public enum DeviceSubscriptionMergeMode : short
    {
        /// <summary>
        /// The staged rows are individual Subscribe and Terminate instructions. Subscriptions absent from the batch are left untouched.
        /// </summary>
        Delta = 0,

        /// <summary>
        /// The staged rows are the complete set of subscribed devices. Every active subscription absent from the batch is terminated.
        /// </summary>
        Snapshot = 1
    }

    /// <summary>
    /// Discriminates the rows spMerge_stg_DeviceSubscriptions returns.
    /// </summary>
    public enum DeviceSubscriptionMergeResultCode : byte
    {
        /// <summary>
        /// The summary row, returned once per successful merge. Its message carries the counts.
        /// </summary>
        Completed = 0,

        /// <summary>
        /// One staged row was rejected. The caller writes it to Q_DeviceSubscriptionsFail. Other rows in the same batch were still applied.
        /// </summary>
        RowRejected = 1,

        /// <summary>
        /// The batch would have terminated more subscriptions than the termination guard permits. Nothing was applied and the caller persists the tripped guard state.
        /// </summary>
        BrakeWouldTrip = 2,

        /// <summary>
        /// Snapshot mode was invoked with no staged rows, which is the signature of a retried merge whose staging rows were already consumed rather than of a genuinely empty fleet. Nothing was applied.
        /// </summary>
        EmptySnapshotRefused = 3,

        /// <summary>
        /// The termination guard was already tripped when the merge was called. Nothing was applied, and nothing will be until an operator clears the guard state.
        /// </summary>
        GuardAlreadyTripped = 4
    }

    /// <summary>
    /// A row returned by spMerge_stg_DeviceSubscriptions. The procedure returns one row per rejected staged row followed by a single summary row, or a single row describing why it refused to apply anything.
    /// </summary>
    public class DeviceSubscriptionMergeResult
    {
        /// <summary>
        /// Discriminates what this row means. See <see cref="DeviceSubscriptionMergeResultCode"/>.
        /// </summary>
        public byte ResultCode { get; set; }

        /// <summary>
        /// The staging row the result refers to, where it refers to one.
        /// </summary>
        public long? StagingId { get; set; }

        /// <summary>
        /// The queue record the staging row came from, where it came from one. On a tripped termination guard this carries the number of terminations already applied within the rolling window, because a refusal refers to no single queue record.
        /// </summary>
        public long? QueueId { get; set; }

        /// <summary>
        /// The ordering ordinal of the instruction the result refers to. On a tripped termination guard this carries the number of terminations the refused batch would have applied.
        /// </summary>
        public long? QueueOrdinal { get; set; }

        /// <summary>
        /// The third-party identifier the result refers to, where it refers to one.
        /// </summary>
        public string? Identifier { get; set; }

        /// <summary>
        /// The Geotab serial number the result refers to, where it refers to one.
        /// </summary>
        public string? SerialNumber { get; set; }

        /// <summary>
        /// A human-readable description of the result. For a rejected row this is the rejection reason, which the caller records against the failed record.
        /// </summary>
        public string? ResultMessage { get; set; }

        /// <summary>
        /// <see cref="ResultCode"/> as its enum.
        /// </summary>
        public DeviceSubscriptionMergeResultCode Code => (DeviceSubscriptionMergeResultCode)ResultCode;
    }
}
