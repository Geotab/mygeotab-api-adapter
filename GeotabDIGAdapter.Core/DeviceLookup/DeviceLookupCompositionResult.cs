namespace MyGeotabAPIAdapter.DeviceLookup
{
    /// <summary>
    /// The outcome of composing several device lookup sources into the single map the transmit path reads.
    /// </summary>
    public class DeviceLookupCompositionResult
    {
        /// <summary>
        /// The composed map of third-party identifier to Geotab serial number. Devices excluded by a corrupting collision are absent from it.
        /// </summary>
        public required IReadOnlyDictionary<string, string> ThirdPartyIdToSerialNoMap { get; init; }

        /// <summary>
        /// One message per set of identifiers found sharing a Geotab serial number. Every identifier in such a set is excluded from the composed map, because transmitting either one would post two vehicles' telemetry under a single device. Log these as warnings.
        /// </summary>
        public required IReadOnlyList<string> CorruptingCollisionMessages { get; init; }

        /// <summary>
        /// One message per identifier found in more than one source with a different serial number in each. These are resolved rather than excluded - the more recently changed record wins - but one of the two sources is stale and an operator should know. Log these as warnings.
        /// </summary>
        public required IReadOnlyList<string> AmbiguousCollisionMessages { get; init; }

        /// <summary>
        /// The number of identifiers found in more than one source that agreed on the serial number. There is no ambiguity to resolve in that case, so it is counted rather than described.
        /// </summary>
        public int AgreeingDuplicateCount { get; init; }

        /// <summary>
        /// The number of devices excluded from the composed map by a corrupting collision.
        /// </summary>
        public int ExcludedDeviceCount { get; init; }

        /// <summary>
        /// Every identifier that some source has withdrawn authorisation from and no source authorises, mapped to the name of the source that withdrew it. Telemetry naming one of these can be failed without being held first, and the source name is what lets the failure say which table an operator should look in.
        ///
        /// Two kinds of identifier are deliberately kept out of it. One authorised by any source is absent, because authorisation from one control plane is not undone by a stale record in another. One excluded by a corrupting collision is also absent, because exclusion is a refusal to guess rather than a withdrawal of authorisation, and its telemetry must keep being held until an operator resolves the conflict.
        /// </summary>
        public IReadOnlyDictionary<string, string> UnauthorizedThirdPartyIdToSourceName { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
