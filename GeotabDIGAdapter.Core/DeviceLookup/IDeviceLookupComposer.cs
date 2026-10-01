namespace MyGeotabAPIAdapter.DeviceLookup
{
    /// <summary>
    /// Interface for a class that composes several device lookup sources into the single map the transmit path reads, applying the rules that govern what happens when two sources disagree.
    /// </summary>
    public interface IDeviceLookupComposer
    {
        /// <summary>
        /// Composes <paramref name="contributions"/> into a single map of third-party identifier to Geotab serial number.
        /// </summary>
        /// <param name="contributions">The participating sources, in precedence order. Order only decides an exact tie; every other outcome is decided by the data.</param>
        /// <returns>The composed map together with a description of every collision encountered.</returns>
        DeviceLookupCompositionResult Compose(IReadOnlyList<DeviceLookupContribution> contributions);
    }
}
