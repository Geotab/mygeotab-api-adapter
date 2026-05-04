namespace GeotabDIGAdapter.Services
{
    /// <summary>
    /// Interface for a cache that provides a mapping of ThirdPartyId to GeotabSerialNumber for provisioned devices.
    /// </summary>
    public interface IProvisionedDeviceCache
    {
        /// <summary>
        /// Gets a map of ThirdPartyId to GeotabSerialNumber for all provisioned devices that are okay to send data.
        /// Returns a cached result if the configured refresh interval has not elapsed; otherwise refreshes the cache.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A dictionary mapping ThirdPartyId to GeotabSerialNumber.</returns>
        Task<Dictionary<string, string>> GetThirdPartyIdToSerialNoMapAsync(CancellationToken cancellationToken);
    }
}
