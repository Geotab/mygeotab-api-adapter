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
    /// The device lookup source backed by the ProvisionedDevices table. Contributes the devices that the device provisioning workflow has provisioned and flagged as okay to send data for.
    /// </summary>
    public class ProvisionedDeviceCache : DeviceLookupContributorBase, IProvisionedDeviceCache
    {
        static readonly Logger StaticLogger = LogManager.GetCurrentClassLogger();

        readonly IDIGAdapterConfiguration adapterConfiguration;
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;
        readonly Logger logger = StaticLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProvisionedDeviceCache"/> class.
        /// </summary>
        public ProvisionedDeviceCache(
            IDIGAdapterConfiguration adapterConfiguration,
            IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
            : base(StaticLogger)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterContext = adapterContext;

            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            logger.Debug($"{nameof(ProvisionedDeviceCache)} instantiated.");
        }

        /// <inheritdoc/>
        public override string SourceName => "ProvisionedDevices";

        /// <inheritdoc/>
        public override bool IsParticipating => adapterConfiguration.EnableDeviceProvisioningService;

        /// <inheritdoc/>
        protected override int CacheRefreshIntervalSeconds => adapterConfiguration.ProvisionedDeviceCacheRefreshIntervalSeconds;

        /// <summary>
        /// Reads every provisioned device rather than only those okay to send data for, and splits them here. A device
        /// whose row says not to send is a device this source has withdrawn authorisation from, which is a different
        /// thing from a device it has never heard of, and filtering it out in SQL erases the difference.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The source's contribution to the composed device lookup.</returns>
        protected override async Task<DeviceLookupContribution> QueryContributionAsync(CancellationToken cancellationToken)
        {
            return await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var repo = new BaseRepository<DbGdaProvisionedDevice>(adapterContext);

                var sql = adapterContext.ProviderType switch
                {
                    ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                        "SELECT \"ThirdPartyId\", \"GeotabSerialNumber\", \"RecordLastChangedUtc\", \"IsOkayToSendDataToGeotab\" FROM gda.\"ProvisionedDevices\"",
                    ConnectionInfo.DataAccessProviderType.SQLServer =>
                        "SELECT ThirdPartyId, GeotabSerialNumber, RecordLastChangedUtc, IsOkayToSendDataToGeotab FROM gda.ProvisionedDevices",
                    _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
                };

                var devices = await repo.QueryAsync(sql, null, cancellationTokenSource, true, adapterContext);

                var entries = new List<DeviceLookupEntry>();
                var unauthorizedThirdPartyIds = new List<string>();
                foreach (var device in devices)
                {
                    if (device.IsOkayToSendDataToGeotab)
                    {
                        entries.Add(new DeviceLookupEntry
                        {
                            ThirdPartyId = device.ThirdPartyId,
                            GeotabSerialNumber = device.GeotabSerialNumber,
                            RecordLastChangedUtc = device.RecordLastChangedUtc
                        });
                    }
                    else
                    {
                        unauthorizedThirdPartyIds.Add(device.ThirdPartyId);
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
