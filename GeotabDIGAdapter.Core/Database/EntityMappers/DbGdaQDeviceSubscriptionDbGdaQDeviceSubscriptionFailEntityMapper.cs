using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DbGdaQDeviceSubscriptionFail"/> entities from <see cref="DbGdaQDeviceSubscription"/> entities.
    /// </summary>
    public class DbGdaQDeviceSubscriptionDbGdaQDeviceSubscriptionFailEntityMapper : IDbGdaQDeviceSubscriptionDbGdaQDeviceSubscriptionFailEntityMapper
    {
        /// <inheritdoc/>
        public DbGdaQDeviceSubscriptionFail CreateEntity(DbGdaQDeviceSubscription dbGdaQDeviceSubscription, string failureReason)
        {
            var dbGdaQDeviceSubscriptionFail = new DbGdaQDeviceSubscriptionFail
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                EffectiveDateTimeUtc = dbGdaQDeviceSubscription.EffectiveDateTimeUtc,
                FailureReason = failureReason,
                GeotabSerialNumber = dbGdaQDeviceSubscription.GeotabSerialNumber,
                IdentifierType = dbGdaQDeviceSubscription.IdentifierType,
                OperationType = dbGdaQDeviceSubscription.OperationType,
                OriginalOrderingOrdinal = dbGdaQDeviceSubscription.OrderingOrdinal,
                OriginalQueueId = dbGdaQDeviceSubscription.id,
                OriginalRecordCreationTimeUtc = dbGdaQDeviceSubscription.RecordCreationTimeUtc,
                OriginalRecordLastChangedUtc = dbGdaQDeviceSubscription.RecordLastChangedUtc,
                RecordCreationTimeUtc = DateTime.UtcNow,
                SourceReference = dbGdaQDeviceSubscription.SourceReference,
                SourceStatus = dbGdaQDeviceSubscription.SourceStatus,
                SubPlan = dbGdaQDeviceSubscription.SubPlan,
                TerminationReason = dbGdaQDeviceSubscription.TerminationReason,
                ThirdPartyId = dbGdaQDeviceSubscription.ThirdPartyId
            };

            return dbGdaQDeviceSubscriptionFail;
        }
    }
}
