using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DbGdaStgDeviceSubscription"/> entities from <see cref="DbGdaQDeviceSubscription"/> entities.
    /// </summary>
    public class DbGdaQDeviceSubscriptionDbGdaStgDeviceSubscriptionEntityMapper : IDbGdaQDeviceSubscriptionDbGdaStgDeviceSubscriptionEntityMapper
    {
        /// <inheritdoc/>
        public DbGdaStgDeviceSubscription CreateEntity(DbGdaQDeviceSubscription dbGdaQDeviceSubscription, Guid batchId, byte operationType)
        {
            var dbGdaStgDeviceSubscription = new DbGdaStgDeviceSubscription
            {
                BatchId = batchId,
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                EffectiveDateTimeUtc = dbGdaQDeviceSubscription.EffectiveDateTimeUtc,
                GeotabSerialNumber = dbGdaQDeviceSubscription.GeotabSerialNumber,
                IdentifierType = dbGdaQDeviceSubscription.IdentifierType,
                OperationType = operationType,
                Ordinal = dbGdaQDeviceSubscription.OrderingOrdinal,
                OriginalQueueId = dbGdaQDeviceSubscription.id,
                SourceReference = dbGdaQDeviceSubscription.SourceReference,
                SourceStatus = dbGdaQDeviceSubscription.SourceStatus,
                SubPlan = dbGdaQDeviceSubscription.SubPlan,
                TerminationReason = dbGdaQDeviceSubscription.TerminationReason,
                ThirdPartyId = dbGdaQDeviceSubscription.ThirdPartyId
            };

            return dbGdaStgDeviceSubscription;
        }
    }
}
