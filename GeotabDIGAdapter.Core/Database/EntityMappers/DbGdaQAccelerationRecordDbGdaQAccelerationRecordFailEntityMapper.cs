using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DbGdaQAccelerationRecordFail"/> entities from <see cref="DbGdaQAccelerationRecord"/> entities.
    /// </summary>
    public class DbGdaQAccelerationRecordDbGdaQAccelerationRecordFailEntityMapper : IDbGdaQAccelerationRecordDbGdaQAccelerationRecordFailEntityMapper
    {
        /// <inheritdoc/>
        public DbGdaQAccelerationRecordFail CreateEntity(DbGdaQAccelerationRecord dbGdaQAccelerationRecord, string failureReason)
        {
            var dbGdaQAccelerationRecordFail = new DbGdaQAccelerationRecordFail
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                OriginalQueueId = dbGdaQAccelerationRecord.id,
                ThirdPartyId = dbGdaQAccelerationRecord.ThirdPartyId,
                DateTime = dbGdaQAccelerationRecord.DateTime,
                X = dbGdaQAccelerationRecord.X,
                Y = dbGdaQAccelerationRecord.Y,
                Z = dbGdaQAccelerationRecord.Z,
                OriginalRecordLastChangedUtc = dbGdaQAccelerationRecord.RecordLastChangedUtc,
                FailureReason = failureReason,
                RecordCreationTimeUtc = DateTime.UtcNow
            };

            return dbGdaQAccelerationRecordFail;
        }
    }
}
