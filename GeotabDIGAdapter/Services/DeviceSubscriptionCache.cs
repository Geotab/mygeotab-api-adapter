using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DeviceLookup;
using NLog;
using Polly;
using Polly.Retry;

namespace GeotabDIGAdapter.Services
{
    /// <summary>
    /// The device lookup source backed by the DeviceSubscriptions table. Contributes the devices the adapter has been told to transmit for and has not been told to stop.
    /// </summary>
    public class DeviceSubscriptionCache : DeviceLookupContributorBase, IDeviceSubscriptionCache
    {
        static readonly Logger StaticLogger = LogManager.GetCurrentClassLogger();

        readonly IDIGAdapterConfiguration adapterConfiguration;
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;
        readonly Logger logger = StaticLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceSubscriptionCache"/> class.
        /// </summary>
        public DeviceSubscriptionCache(
            IDIGAdapterConfiguration adapterConfiguration,
            IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
            : base(StaticLogger)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterContext = adapterContext;

            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            logger.Debug($"{nameof(DeviceSubscriptionCache)} instantiated.");
        }

        /// <inheritdoc/>
        public override string SourceName => "DeviceSubscriptions";

        /// <summary>
        /// Participation keys on the table rather than on the service that writes it. A deployment whose subscriptions
        /// arrive by some means other than the DeviceSubscriptionService still transmits for whoever DeviceSubscriptions
        /// says it may, and keying on the writing service would force such a deployment to run an idle writer purely to
        /// satisfy this check.
        /// </summary>
        public override bool IsParticipating => adapterConfiguration.EnableDeviceSubscriptionService;

        /// <inheritdoc/>
        protected override int CacheRefreshIntervalSeconds => adapterConfiguration.ProvisionedDeviceCacheRefreshIntervalSeconds;

        /// <summary>
        /// Reads every subscription rather than only the active ones, and splits them here.
        ///
        /// Terminated rows are retained by design (a termination is a status change, not a delete), which makes them
        /// the record of what the adapter has been told to stop transmitting for. Filtering them out in SQL discards
        /// exactly that, and leaves telemetry for a terminated device indistinguishable from telemetry for a device
        /// nobody has heard of. The table holds one row per managed device, so reading all of it costs nothing.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The source's contribution to the composed device lookup.</returns>
        protected override async Task<DeviceLookupContribution> QueryContributionAsync(CancellationToken cancellationToken)
        {
            return await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var repo = new BaseRepository<DbGdaDeviceSubscription>(adapterContext);

                var sql = adapterContext.ProviderType switch
                {
                    ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                        "SELECT \"ThirdPartyId\", \"GeotabSerialNumber\", \"RecordLastChangedUtc\", \"SubscriptionStatus\" FROM gda.\"DeviceSubscriptions\"",
                    ConnectionInfo.DataAccessProviderType.SQLServer =>
                        "SELECT ThirdPartyId, GeotabSerialNumber, RecordLastChangedUtc, SubscriptionStatus FROM gda.DeviceSubscriptions",
                    _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
                };

                var subscriptions = await repo.QueryAsync(sql, null, cancellationTokenSource, true, adapterContext);

                var entries = new List<DeviceLookupEntry>();
                var unauthorizedThirdPartyIds = new List<string>();
                foreach (var subscription in subscriptions)
                {
                    if (subscription.SubscriptionStatus == DbGdaDeviceSubscription.SubscriptionStatusActive)
                    {
                        entries.Add(new DeviceLookupEntry
                        {
                            ThirdPartyId = subscription.ThirdPartyId,
                            GeotabSerialNumber = subscription.GeotabSerialNumber,
                            RecordLastChangedUtc = subscription.RecordLastChangedUtc
                        });
                    }
                    else
                    {
                        unauthorizedThirdPartyIds.Add(subscription.ThirdPartyId);
                    }
                }

                return new DeviceLookupContribution
                {
                    SourceName = SourceName,
                    Entries = entries,
                    UnauthorizedThirdPartyIds = unauthorizedThirdPartyIds
                };
            }, new Context());
        }
    }
}
