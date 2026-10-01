namespace MyGeotabAPIAdapter.DeviceLookup
{
    /// <summary>
    /// A single device's entry in a device lookup source: the third-party identifier the feeder supplies telemetry under, the Geotab serial number that telemetry is transmitted as, and when the underlying record last changed.
    /// </summary>
    public class DeviceLookupEntry
    {
        /// <summary>
        /// The identifier the feeder uses to refer to the device.
        /// </summary>
        public required string ThirdPartyId { get; init; }

        /// <summary>
        /// The Geotab serial number the device's telemetry is transmitted as.
        /// </summary>
        public required string GeotabSerialNumber { get; init; }

        /// <summary>
        /// When the source record last changed. Used to break a tie when two sources disagree about which serial number an identifier maps to: the more recently changed record wins.
        /// </summary>
        public DateTime RecordLastChangedUtc { get; init; }
    }
}
