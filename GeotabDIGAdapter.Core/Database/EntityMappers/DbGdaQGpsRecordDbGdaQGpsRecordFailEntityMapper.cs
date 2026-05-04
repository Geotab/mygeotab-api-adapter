using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DbGdaQGpsRecordFail"/> entities from <see cref="DbGdaQGpsRecord"/> entities.
    /// </summary>
    public class DbGdaQGpsRecordDbGdaQGpsRecordFailEntityMapper : IDbGdaQGpsRecordDbGdaQGpsRecordFailEntityMapper
    {
        /// <inheritdoc/>
        public DbGdaQGpsRecordFail CreateEntity(DbGdaQGpsRecord dbGdaQGpsRecord, string failureReason)
        {
            var dbGdaQGpsRecordFail = new DbGdaQGpsRecordFail
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                OriginalQueueId = dbGdaQGpsRecord.id,
                ThirdPartyId = dbGdaQGpsRecord.ThirdPartyId,
                DateTime = dbGdaQGpsRecord.DateTime,
                Latitude = dbGdaQGpsRecord.Latitude,
                Longitude = dbGdaQGpsRecord.Longitude,
                Speed = dbGdaQGpsRecord.Speed,
                IsGpsValid = dbGdaQGpsRecord.IsGpsValid,
                IsIgnitionOn = dbGdaQGpsRecord.IsIgnitionOn,
                IsAuxiliary1On = dbGdaQGpsRecord.IsAuxiliary1On,
                IsAuxiliary2On = dbGdaQGpsRecord.IsAuxiliary2On,
                IsAuxiliary3On = dbGdaQGpsRecord.IsAuxiliary3On,
                IsAuxiliary4On = dbGdaQGpsRecord.IsAuxiliary4On,
                IsAuxiliary5On = dbGdaQGpsRecord.IsAuxiliary5On,
                IsAuxiliary6On = dbGdaQGpsRecord.IsAuxiliary6On,
                IsAuxiliary7On = dbGdaQGpsRecord.IsAuxiliary7On,
                IsAuxiliary8On = dbGdaQGpsRecord.IsAuxiliary8On,
                OriginalRecordLastChangedUtc = dbGdaQGpsRecord.RecordLastChangedUtc,
                FailureReason = failureReason,
                RecordCreationTimeUtc = DateTime.UtcNow
            };

            return dbGdaQGpsRecordFail;
        }
    }
}
