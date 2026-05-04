using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DbGdaQGenericFaultRecordFail"/> entities from <see cref="DbGdaQGenericFaultRecord"/> entities.
    /// </summary>
    public class DbGdaQGenericFaultRecordDbGdaQGenericFaultRecordFailEntityMapper : IDbGdaQGenericFaultRecordDbGdaQGenericFaultRecordFailEntityMapper
    {
        /// <inheritdoc/>
        public DbGdaQGenericFaultRecordFail CreateEntity(DbGdaQGenericFaultRecord dbGdaQGenericFaultRecord, string failureReason)
        {
            var dbGdaQGenericFaultRecordFail = new DbGdaQGenericFaultRecordFail
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                OriginalQueueId = dbGdaQGenericFaultRecord.id,
                ThirdPartyId = dbGdaQGenericFaultRecord.ThirdPartyId,
                DateTime = dbGdaQGenericFaultRecord.DateTime,
                Code = dbGdaQGenericFaultRecord.Code,
                FaultStateActive = dbGdaQGenericFaultRecord.FaultStateActive,
                OriginalRecordLastChangedUtc = dbGdaQGenericFaultRecord.RecordLastChangedUtc,
                FailureReason = failureReason,
                RecordCreationTimeUtc = DateTime.UtcNow
            };

            return dbGdaQGenericFaultRecordFail;
        }
    }
}
