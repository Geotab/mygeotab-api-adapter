using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DbGdaQObdiiFaultRecordFail"/> entities from <see cref="DbGdaQObdiiFaultRecord"/> entities.
    /// </summary>
    public class DbGdaQObdiiFaultRecordDbGdaQObdiiFaultRecordFailEntityMapper : IDbGdaQObdiiFaultRecordDbGdaQObdiiFaultRecordFailEntityMapper
    {
        /// <inheritdoc/>
        public DbGdaQObdiiFaultRecordFail CreateEntity(DbGdaQObdiiFaultRecord dbGdaQObdiiFaultRecord, string failureReason)
        {
            var dbGdaQObdiiFaultRecordFail = new DbGdaQObdiiFaultRecordFail
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                OriginalQueueId = dbGdaQObdiiFaultRecord.id,
                ThirdPartyId = dbGdaQObdiiFaultRecord.ThirdPartyId,
                DateTime = dbGdaQObdiiFaultRecord.DateTime,
                Code = dbGdaQObdiiFaultRecord.Code,
                FaultStateActive = dbGdaQObdiiFaultRecord.FaultStateActive,
                OriginalRecordLastChangedUtc = dbGdaQObdiiFaultRecord.RecordLastChangedUtc,
                FailureReason = failureReason,
                RecordCreationTimeUtc = DateTime.UtcNow
            };

            return dbGdaQObdiiFaultRecordFail;
        }
    }
}
