using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DbGdaQDriverChangeRecordFail"/> entities from <see cref="DbGdaQDriverChangeRecord"/> entities.
    /// </summary>
    public class DbGdaQDriverChangeRecordDbGdaQDriverChangeRecordFailEntityMapper : IDbGdaQDriverChangeRecordDbGdaQDriverChangeRecordFailEntityMapper
    {
        /// <inheritdoc/>
        public DbGdaQDriverChangeRecordFail CreateEntity(DbGdaQDriverChangeRecord dbGdaQDriverChangeRecord, string failureReason)
        {
            var dbGdaQDriverChangeRecordFail = new DbGdaQDriverChangeRecordFail
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                OriginalQueueId = dbGdaQDriverChangeRecord.id,
                ThirdPartyId = dbGdaQDriverChangeRecord.ThirdPartyId,
                DateTime = dbGdaQDriverChangeRecord.DateTime,
                KeyType = dbGdaQDriverChangeRecord.KeyType,
                DriverId = dbGdaQDriverChangeRecord.DriverId,
                OriginalRecordLastChangedUtc = dbGdaQDriverChangeRecord.RecordLastChangedUtc,
                FailureReason = failureReason,
                RecordCreationTimeUtc = DateTime.UtcNow
            };

            return dbGdaQDriverChangeRecordFail;
        }
    }
}
