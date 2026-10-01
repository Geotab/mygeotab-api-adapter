using MyGeotabAPIAdapter.DeviceLookup;

namespace GeotabDIGAdapter.Services
{
    /// <summary>
    /// Interface for one source of the devices the adapter may transmit for. Each source is registered unconditionally and reports whether it is participating, because this application has no mechanism for conditional registration.
    /// </summary>
    public interface IDeviceLookupContributor
    {
        /// <summary>
        /// A short human-readable name for the source. Appears in collision log messages so that an operator knows which table to look in.
        /// </summary>
        string SourceName { get; }

        /// <summary>
        /// Whether this source contributes to the composed device lookup in the current configuration. A source that is not participating is never queried, so a deployment that uses only one control plane never reads the other one's table.
        /// </summary>
        bool IsParticipating { get; }

        /// <summary>
        /// Gets what this source knows: the devices it says may be transmitted for, and the devices it manages but has withdrawn authorisation from. Returns a cached result unless the cache has been invalidated or has expired.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The source's contribution to the composed device lookup.</returns>
        Task<DeviceLookupContribution> GetContributionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Discards the cached result so that the next call to <see cref="GetContributionAsync(CancellationToken)"/> re-queries the database. Safe to call while a refresh is in flight: an invalidation raised during a query is not lost, and the in-flight result is not published as current.
        /// </summary>
        void Invalidate();
    }
}
