using MyGeotabAPIAdapter.DeviceLookup;

namespace GeotabDIGAdapter.Services
{
    /// <summary>
    /// Interface for the single lookup the transmit path uses to decide what to do with a record: which third-party identifiers may be transmitted for and under which Geotab serial number, and which have had their authorisation withdrawn. Composes every participating device lookup source, so that the transmit path neither knows nor cares which control plane a device came from.
    /// </summary>
    public interface IDeviceLookup
    {
        /// <summary>
        /// Gets the composed authorisation snapshot for every participating source. Returns a cached result unless the cache has been invalidated or has expired.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The devices that may be transmitted for, and the devices whose authorisation has been withdrawn.</returns>
        Task<DeviceAuthorizationSnapshot> GetDeviceAuthorizationsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Discards the composed snapshot so that the next call to <see cref="GetDeviceAuthorizationsAsync(CancellationToken)"/> rebuilds it from freshly queried sources. Call this whenever something has changed which devices may be transmitted for, so that the change takes effect on the next transmit iteration rather than at the end of the refresh interval.
        /// </summary>
        void Invalidate();
    }
}
