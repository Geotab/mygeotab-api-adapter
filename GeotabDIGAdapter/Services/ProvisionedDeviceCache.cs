using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using NLog;
using Polly;
using Polly.Retry;

namespace GeotabDIGAdapter.Services
{
    /// <summary>
    /// A cache that provides a mapping of ThirdPartyId to GeotabSerialNumber for provisioned devices.
    /// The cache is refreshed based on the configured interval.
    /// </summary>
    public class ProvisionedDeviceCache : IProvisionedDeviceCache
    {
        readonly IDIGAdapterConfiguration adapterConfiguration;
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        Dictionary<string, string>? cachedThirdPartyIdToSerialNoMap;
        DateTime lastCacheRefreshTimeUtc = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProvisionedDeviceCache"/> class.
        /// </summary>
        public ProvisionedDeviceCache(
            IDIGAdapterConfiguration adapterConfiguration,
            IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterContext = adapterContext;

            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            logger.Debug($"{nameof(ProvisionedDeviceCache)} instantiated.");
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, string>> GetThirdPartyIdToSerialNoMapAsync(CancellationToken cancellationToken)
        {
            var nowUtc = DateTime.UtcNow;
            var cacheAgeSeconds = (nowUtc - lastCacheRefreshTimeUtc).TotalSeconds;

            if (cachedThirdPartyIdToSerialNoMap != null
                && cacheAgeSeconds < adapterConfiguration.ProvisionedDeviceCacheRefreshIntervalSeconds)
            {
                logger.Trace($"Using cached provisioned device map ({cachedThirdPartyIdToSerialNoMap.Count} devices, age: {cacheAgeSeconds:F0}s).");
                return cachedThirdPartyIdToSerialNoMap;
            }

            if (cachedThirdPartyIdToSerialNoMap == null)
            {
                logger.Debug("Building initial provisioned device cache.");
            }
            else
            {
                logger.Debug($"Refreshing provisioned device cache (age: {cacheAgeSeconds:F0}s).");
            }

            cachedThirdPartyIdToSerialNoMap = await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var repo = new BaseRepository<DbGdaProvisionedDevice>(adapterContext);

                var sql = adapterContext.ProviderType switch
                {
                    ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                        "SELECT \"ThirdPartyId\", \"GeotabSerialNumber\" FROM gda.\"ProvisionedDevices\" WHERE \"IsOkayToSendDataToGeotab\" = true",
                    ConnectionInfo.DataAccessProviderType.SQLServer =>
                        "SELECT ThirdPartyId, GeotabSerialNumber FROM gda.ProvisionedDevices WHERE IsOkayToSendDataToGeotab = 1",
                    _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
                };

                var devices = await repo.QueryAsync(sql, null, cancellationTokenSource, true, adapterContext);
                return devices.ToDictionary(d => d.ThirdPartyId, d => d.GeotabSerialNumber);
            }, new Context());

            lastCacheRefreshTimeUtc = nowUtc;
            logger.Info($"Provisioned device cache refreshed. {cachedThirdPartyIdToSerialNoMap.Count} device(s) loaded.");

            return cachedThirdPartyIdToSerialNoMap;
        }
    }
}
