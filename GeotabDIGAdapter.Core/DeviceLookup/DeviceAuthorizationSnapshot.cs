namespace MyGeotabAPIAdapter.DeviceLookup
{
    /// <summary>
    /// What the composed device lookup knows, as of one refresh: which devices may be transmitted for, and which have had their authorisation withdrawn.
    ///
    /// The two travel together rather than being fetched separately so that the transmit path cannot read one of them from one refresh and the other from the next. A record is classified against both, and a classification drawn from two vintages could hold telemetry for a device that had just been terminated, or fail telemetry for one that had just been subscribed.
    /// </summary>
    public class DeviceAuthorizationSnapshot
    {
        /// <summary>
        /// An empty snapshot: no device may be transmitted for, and no device is known to have been withdrawn.
        /// </summary>
        public static DeviceAuthorizationSnapshot Empty { get; } = new()
        {
            ThirdPartyIdToSerialNoMap = new Dictionary<string, string>(StringComparer.Ordinal),
            UnauthorizedThirdPartyIdToSourceName = new Dictionary<string, string>(StringComparer.Ordinal)
        };

        /// <summary>
        /// The composed map of third-party identifier to the Geotab serial number the device's telemetry is transmitted as.
        /// </summary>
        public required IReadOnlyDictionary<string, string> ThirdPartyIdToSerialNoMap { get; init; }

        /// <summary>
        /// Every identifier a source has withdrawn authorisation from and no source authorises, mapped to the name of the source that withdrew it. An identifier in neither this nor <see cref="ThirdPartyIdToSerialNoMap"/> is simply unknown, which is a different thing and is treated differently.
        /// </summary>
        public required IReadOnlyDictionary<string, string> UnauthorizedThirdPartyIdToSourceName { get; init; }
    }
}
