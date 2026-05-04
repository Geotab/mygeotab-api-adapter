using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DbGdaQVinRecordFail"/> entities from <see cref="DbGdaQVinRecord"/> entities.
    /// </summary>
    public class DbGdaQVinRecordDbGdaQVinRecordFailEntityMapper : IDbGdaQVinRecordDbGdaQVinRecordFailEntityMapper
    {
        /// <inheritdoc/>
        public DbGdaQVinRecordFail CreateEntity(DbGdaQVinRecord dbGdaQVinRecord, string failureReason)
        {
            var dbGdaQVinRecordFail = new DbGdaQVinRecordFail
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                OriginalQueueId = dbGdaQVinRecord.id,
                ThirdPartyId = dbGdaQVinRecord.ThirdPartyId,
                DateTime = dbGdaQVinRecord.DateTime,
                VehicleIdentificationNumber = dbGdaQVinRecord.VehicleIdentificationNumber,
                OriginalRecordLastChangedUtc = dbGdaQVinRecord.RecordLastChangedUtc,
                FailureReason = failureReason,
                RecordCreationTimeUtc = DateTime.UtcNow
            };

            return dbGdaQVinRecordFail;
        }
    }
}
