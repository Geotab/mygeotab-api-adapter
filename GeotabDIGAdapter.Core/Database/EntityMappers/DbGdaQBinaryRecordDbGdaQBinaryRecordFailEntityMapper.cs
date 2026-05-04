using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DbGdaQBinaryRecordFail"/> entities from <see cref="DbGdaQBinaryRecord"/> entities.
    /// </summary>
    public class DbGdaQBinaryRecordDbGdaQBinaryRecordFailEntityMapper : IDbGdaQBinaryRecordDbGdaQBinaryRecordFailEntityMapper
    {
        /// <inheritdoc/>
        public DbGdaQBinaryRecordFail CreateEntity(DbGdaQBinaryRecord dbGdaQBinaryRecord, string failureReason)
        {
            var dbGdaQBinaryRecordFail = new DbGdaQBinaryRecordFail
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                OriginalQueueId = dbGdaQBinaryRecord.id,
                ThirdPartyId = dbGdaQBinaryRecord.ThirdPartyId,
                DateTime = dbGdaQBinaryRecord.DateTime,
                Data = dbGdaQBinaryRecord.Data,
                OriginalRecordLastChangedUtc = dbGdaQBinaryRecord.RecordLastChangedUtc,
                FailureReason = failureReason,
                RecordCreationTimeUtc = DateTime.UtcNow
            };

            return dbGdaQBinaryRecordFail;
        }
    }
}
