namespace MyGeotabAPIAdapter.DeviceLookup
{
    /// <summary>
    /// What one lookup source knows about the devices it manages: those it says may be transmitted for, and those it manages but has withdrawn authorisation from.
    ///
    /// The second set is what separates a device the source has revoked from one it has never heard of. Both fail to resolve, but only the first is settled: a revoked device is not going to become transmittable by waiting, so its telemetry can be failed at once instead of being held for a window that exists to cover a feeder running ahead of authorisation.
    /// </summary>
    public class DeviceLookupContribution
    {
        /// <summary>
        /// A short human-readable name for the source, used in collision log messages so that an operator knows which table to look in.
        /// </summary>
        public required string SourceName { get; init; }

        /// <summary>
        /// The devices the source says may be transmitted for.
        /// </summary>
        public required IReadOnlyList<DeviceLookupEntry> Entries { get; init; }

        /// <summary>
        /// The identifiers of devices the source manages but has withdrawn authorisation from - a terminated subscription, or a provisioned device no longer okay to send data for.
        ///
        /// Empty by default, which is the safe value: a source that does not report its revoked devices simply leaves them indistinguishable from devices nobody has heard of, and their telemetry is held and then failed exactly as it was before this set existed.
        /// </summary>
        public IReadOnlyList<string> UnauthorizedThirdPartyIds { get; init; } = [];
    }
}
