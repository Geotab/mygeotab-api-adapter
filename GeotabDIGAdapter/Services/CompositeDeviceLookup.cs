using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.DeviceLookup;
using NLog;

namespace GeotabDIGAdapter.Services
{
    /// <summary>
    /// Composes every participating device lookup source into the single map the transmit path reads.
    ///
    /// The composed map is published as one immutable dictionary rather than as several dictionaries probed in turn.
    /// That keeps the transmit path at exactly one lookup, and it makes conflict detection something that happens once
    /// per refresh rather than once per record.
    ///
    /// Every source is refreshed under this class's invalidation epoch, so the published map is always of one vintage.
    /// Sources refreshing independently would guarantee the opposite: two sources of different ages disagreeing about
    /// a device that had simply moved from one to the other, and the conflict rules firing on data that was correct.
    /// </summary>
    public class CompositeDeviceLookup : IDeviceLookup
    {
        /// <summary>
        /// The percentage by which the device count must fall between two refreshes before a warning is logged. A relative check like this cannot be relied on alone, because each new lower count becomes the baseline for the next comparison; the absolute floor is what catches a fleet that shrinks repeatedly.
        /// </summary>
        const int RelativeDeviceCountDropWarningThresholdPercent = 20;

        readonly IDIGAdapterConfiguration adapterConfiguration;
        readonly IDeviceLookupComposer deviceLookupComposer;
        readonly IReadOnlyList<IDeviceLookupContributor> contributors;
        readonly SemaphoreSlim refreshSemaphore = new(1, 1);
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        long invalidationEpoch;
        long cachedEpoch = -1;
        long lastCacheRefreshTicksUtc;
        int previousDeviceCount = -1;
        volatile DeviceAuthorizationSnapshot? publishedSnapshot;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeDeviceLookup"/> class.
        /// </summary>
        public CompositeDeviceLookup(
            IDIGAdapterConfiguration adapterConfiguration,
            IDeviceLookupComposer deviceLookupComposer,
            IProvisionedDeviceCache provisionedDeviceCache,
            IDeviceSubscriptionCache deviceSubscriptionCache)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.deviceLookupComposer = deviceLookupComposer;

            // Order decides an exact tie in the conflict rules and nothing else. ProvisionedDevices comes first
            // because it is the pre-existing control plane, so a deployment that adds subscriptions to an existing
            // installation sees no change in behaviour from a tie.
            contributors = new List<IDeviceLookupContributor> { provisionedDeviceCache, deviceSubscriptionCache };

            logger.Debug($"{nameof(CompositeDeviceLookup)} instantiated.");
        }

        /// <inheritdoc/>
        public void Invalidate()
        {
            Interlocked.Increment(ref invalidationEpoch);
            logger.Debug("Composed device lookup invalidated; it will be rebuilt on the next request.");
        }

        /// <inheritdoc/>
        public async Task<DeviceAuthorizationSnapshot> GetDeviceAuthorizationsAsync(CancellationToken cancellationToken)
        {
            if (TryGetCurrentPublishedSnapshot(out var currentSnapshot))
            {
                return currentSnapshot;
            }

            await refreshSemaphore.WaitAsync(cancellationToken);
            try
            {
                // Another caller may have refreshed while this one waited.
                if (TryGetCurrentPublishedSnapshot(out currentSnapshot))
                {
                    return currentSnapshot;
                }

                // Captured before any source is queried, so an invalidation raised during the refresh leaves the
                // resulting map stale on arrival rather than being lost.
                var epochAtRefreshStart = Interlocked.Read(ref invalidationEpoch);
                var refreshStartTimeUtc = DateTime.UtcNow;

                var participatingContributors = contributors.Where(contributor => contributor.IsParticipating).ToList();
                if (participatingContributors.Count == 0)
                {
                    logger.Error("No device lookup source is participating, so the adapter cannot transmit for any device. Enable the device provisioning service, the device subscription service, or both.");
                }

                var contributions = new List<DeviceLookupContribution>();
                foreach (var contributor in participatingContributors)
                {
                    // Force the source to re-query, so that every source in the composed map is of this refresh's vintage.
                    contributor.Invalidate();
                    contributions.Add(await contributor.GetContributionAsync(cancellationToken));
                }

                var compositionResult = deviceLookupComposer.Compose(contributions);
                LogCompositionResult(compositionResult, contributions);

                var snapshot = new DeviceAuthorizationSnapshot
                {
                    ThirdPartyIdToSerialNoMap = compositionResult.ThirdPartyIdToSerialNoMap,
                    UnauthorizedThirdPartyIdToSourceName = compositionResult.UnauthorizedThirdPartyIdToSourceName
                };

                publishedSnapshot = snapshot;
                Interlocked.Exchange(ref lastCacheRefreshTicksUtc, refreshStartTimeUtc.Ticks);
                Interlocked.Exchange(ref cachedEpoch, epochAtRefreshStart);

                return snapshot;
            }
            finally
            {
                refreshSemaphore.Release();
            }
        }

        /// <summary>
        /// Logs the outcome of a refresh: the device count and how it has moved, every conflict encountered, and any breach of the configured floor.
        /// </summary>
        /// <param name="compositionResult">The result of composing the participating sources.</param>
        /// <param name="contributions">The sources that were composed.</param>
        void LogCompositionResult(DeviceLookupCompositionResult compositionResult, IReadOnlyList<DeviceLookupContribution> contributions)
        {
            var deviceCount = compositionResult.ThirdPartyIdToSerialNoMap.Count;
            var sourceBreakdown = string.Join(", ", contributions.Select(contribution =>
                contribution.UnauthorizedThirdPartyIds.Count == 0
                    ? $"{contribution.SourceName}:{contribution.Entries.Count}"
                    : $"{contribution.SourceName}:{contribution.Entries.Count} (+{contribution.UnauthorizedThirdPartyIds.Count} withdrawn)"));
            var deltaDescription = previousDeviceCount < 0 ? "initial" : $"{deviceCount - previousDeviceCount:+#;-#;0}";

            logger.Info($"Composed device lookup refreshed. {deviceCount} device(s) (delta: {deltaDescription}), {compositionResult.UnauthorizedThirdPartyIdToSourceName.Count} withdrawn identifier(s). Sources: [{sourceBreakdown}].");

            foreach (var message in compositionResult.CorruptingCollisionMessages)
            {
                logger.Warn(message);
            }
            foreach (var message in compositionResult.AmbiguousCollisionMessages)
            {
                logger.Warn(message);
            }
            if (compositionResult.CorruptingCollisionMessages.Count > 0 || compositionResult.AmbiguousCollisionMessages.Count > 0)
            {
                logger.Warn($"Device lookup conflicts this refresh: {compositionResult.CorruptingCollisionMessages.Count} serial number conflict(s) excluding {compositionResult.ExcludedDeviceCount} device(s), {compositionResult.AmbiguousCollisionMessages.Count} identifier conflict(s) resolved by recency.");
            }
            if (compositionResult.AgreeingDuplicateCount > 0)
            {
                logger.Info($"{compositionResult.AgreeingDuplicateCount} identifier(s) are present in more than one device lookup source and agree on the Geotab serial number. This is harmless, but it means the same device is being managed by two control planes.");
            }

            if (deviceCount < adapterConfiguration.MinimumExpectedDeviceCount)
            {
                logger.Error($"The composed device lookup holds {deviceCount} device(s), which is below the configured minimum of {adapterConfiguration.MinimumExpectedDeviceCount}. Telemetry for any device not in the lookup cannot be transmitted.");
            }
            else if (previousDeviceCount > 0 && deviceCount < previousDeviceCount)
            {
                var dropPercent = (previousDeviceCount - deviceCount) * 100 / previousDeviceCount;
                if (dropPercent >= RelativeDeviceCountDropWarningThresholdPercent)
                {
                    logger.Warn($"The composed device lookup fell from {previousDeviceCount} device(s) to {deviceCount} ({dropPercent}%) in a single refresh.");
                }
            }

            previousDeviceCount = deviceCount;
        }

        /// <summary>
        /// Determines whether the published snapshot is present, current with respect to the invalidation epoch, and not expired.
        /// </summary>
        /// <param name="snapshot">The published snapshot, when it can be used.</param>
        /// <returns><c>true</c> if the published snapshot can be used without rebuilding it.</returns>
        bool TryGetCurrentPublishedSnapshot(out DeviceAuthorizationSnapshot snapshot)
        {
            snapshot = DeviceAuthorizationSnapshot.Empty;

            var candidateSnapshot = publishedSnapshot;
            if (candidateSnapshot == null)
            {
                return false;
            }

            if (Interlocked.Read(ref cachedEpoch) != Interlocked.Read(ref invalidationEpoch))
            {
                return false;
            }

            var cacheAgeSeconds = (DateTime.UtcNow - new DateTime(Interlocked.Read(ref lastCacheRefreshTicksUtc), DateTimeKind.Utc)).TotalSeconds;
            if (cacheAgeSeconds >= adapterConfiguration.ProvisionedDeviceCacheRefreshIntervalSeconds)
            {
                return false;
            }

            snapshot = candidateSnapshot;
            return true;
        }
    }
}
