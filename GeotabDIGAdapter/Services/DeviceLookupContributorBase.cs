using MyGeotabAPIAdapter.DeviceLookup;
using NLog;

namespace GeotabDIGAdapter.Services
{
    /// <summary>
    /// Base class for a device lookup source, providing the caching and invalidation behaviour every source shares.
    ///
    /// Invalidation is tracked as a monotonically increasing epoch rather than by clearing the cache. The epoch is
    /// captured before the query runs and the result is stamped with that captured value, so an invalidation raised
    /// while a query is in flight leaves the arriving result stale and the next caller re-queries. Writing the
    /// current epoch after the query instead would silently discard any invalidation that arrived during it.
    /// </summary>
    public abstract class DeviceLookupContributorBase : IDeviceLookupContributor
    {
        readonly SemaphoreSlim refreshSemaphore = new(1, 1);
        readonly Logger logger;

        long invalidationEpoch;
        long cachedEpoch = -1;
        long lastCacheRefreshTicksUtc;
        volatile DeviceLookupContribution? cachedContribution;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceLookupContributorBase"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="Logger"/> of the deriving class, so that log messages carry its name.</param>
        protected DeviceLookupContributorBase(Logger logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc/>
        public abstract string SourceName { get; }

        /// <inheritdoc/>
        public abstract bool IsParticipating { get; }

        /// <summary>
        /// The number of seconds after which a cached result is considered expired. Under normal operation the
        /// composed lookup invalidates every source before refreshing, so this is a backstop rather than the
        /// mechanism that keeps the cache current.
        /// </summary>
        protected abstract int CacheRefreshIntervalSeconds { get; }

        /// <summary>
        /// Queries the source from the database, returning both the devices it authorises and the devices it has withdrawn authorisation from. Called only when a refresh is actually required.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The source's contribution to the composed device lookup.</returns>
        protected abstract Task<DeviceLookupContribution> QueryContributionAsync(CancellationToken cancellationToken);

        /// <inheritdoc/>
        public void Invalidate()
        {
            Interlocked.Increment(ref invalidationEpoch);
            logger.Debug($"{SourceName} device lookup source invalidated.");
        }

        /// <inheritdoc/>
        public async Task<DeviceLookupContribution> GetContributionAsync(CancellationToken cancellationToken)
        {
            if (TryGetCurrentCachedContribution(out var currentContribution))
            {
                return currentContribution;
            }

            await refreshSemaphore.WaitAsync(cancellationToken);
            try
            {
                // Another caller may have refreshed while this one waited.
                if (TryGetCurrentCachedContribution(out currentContribution))
                {
                    return currentContribution;
                }

                var epochAtQueryStart = Interlocked.Read(ref invalidationEpoch);
                var queryStartTimeUtc = DateTime.UtcNow;

                var queriedContribution = await QueryContributionAsync(cancellationToken);

                cachedContribution = queriedContribution;
                Interlocked.Exchange(ref lastCacheRefreshTicksUtc, queryStartTimeUtc.Ticks);
                Interlocked.Exchange(ref cachedEpoch, epochAtQueryStart);

                logger.Debug($"{SourceName} device lookup source refreshed. {queriedContribution.Entries.Count} device(s) loaded, {queriedContribution.UnauthorizedThirdPartyIds.Count} withdrawn.");

                return queriedContribution;
            }
            finally
            {
                refreshSemaphore.Release();
            }
        }

        /// <summary>
        /// Determines whether the cached result is present, current with respect to the invalidation epoch, and not expired.
        /// </summary>
        /// <param name="contribution">The cached contribution, when the cached result can be used.</param>
        /// <returns><c>true</c> if the cached result can be used without re-querying.</returns>
        bool TryGetCurrentCachedContribution(out DeviceLookupContribution contribution)
        {
            contribution = EmptyContribution;

            var candidateContribution = cachedContribution;
            if (candidateContribution == null)
            {
                return false;
            }

            if (Interlocked.Read(ref cachedEpoch) != Interlocked.Read(ref invalidationEpoch))
            {
                return false;
            }

            var cacheAgeSeconds = (DateTime.UtcNow - new DateTime(Interlocked.Read(ref lastCacheRefreshTicksUtc), DateTimeKind.Utc)).TotalSeconds;
            if (cacheAgeSeconds >= CacheRefreshIntervalSeconds)
            {
                return false;
            }

            contribution = candidateContribution;
            return true;
        }

        /// <summary>
        /// The value handed back when there is no usable cached result. Never returned to a caller - it exists so that the out parameter is assigned on every path.
        /// </summary>
        DeviceLookupContribution EmptyContribution => new() { SourceName = SourceName, Entries = Array.Empty<DeviceLookupEntry>() };
    }
}
