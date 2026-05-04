using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DbGdaQGenericStatusRecordFail"/> entities from <see cref="DbGdaQGenericStatusRecord"/> entities.
    /// </summary>
    public class DbGdaQGenericStatusRecordDbGdaQGenericStatusRecordFailEntityMapper : IDbGdaQGenericStatusRecordDbGdaQGenericStatusRecordFailEntityMapper
    {
        /// <inheritdoc/>
        public DbGdaQGenericStatusRecordFail CreateEntity(DbGdaQGenericStatusRecord dbGdaQGenericStatusRecord, string failureReason)
        {
            var dbGdaQGenericStatusRecordFail = new DbGdaQGenericStatusRecordFail
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                OriginalQueueId = dbGdaQGenericStatusRecord.id,
                ThirdPartyId = dbGdaQGenericStatusRecord.ThirdPartyId,
                DateTime = dbGdaQGenericStatusRecord.DateTime,
                Code = dbGdaQGenericStatusRecord.Code,
                Value = dbGdaQGenericStatusRecord.Value,
                OriginalRecordLastChangedUtc = dbGdaQGenericStatusRecord.RecordLastChangedUtc,
                FailureReason = failureReason,
                RecordCreationTimeUtc = DateTime.UtcNow
            };

            return dbGdaQGenericStatusRecordFail;
        }
    }
}
