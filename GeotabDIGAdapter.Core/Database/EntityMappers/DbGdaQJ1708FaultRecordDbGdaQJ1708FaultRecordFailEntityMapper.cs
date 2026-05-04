using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DbGdaQJ1708FaultRecordFail"/> entities from <see cref="DbGdaQJ1708FaultRecord"/> entities.
    /// </summary>
    public class DbGdaQJ1708FaultRecordDbGdaQJ1708FaultRecordFailEntityMapper : IDbGdaQJ1708FaultRecordDbGdaQJ1708FaultRecordFailEntityMapper
    {
        /// <inheritdoc/>
        public DbGdaQJ1708FaultRecordFail CreateEntity(DbGdaQJ1708FaultRecord dbGdaQJ1708FaultRecord, string failureReason)
        {
            var dbGdaQJ1708FaultRecordFail = new DbGdaQJ1708FaultRecordFail
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                OriginalQueueId = dbGdaQJ1708FaultRecord.id,
                ThirdPartyId = dbGdaQJ1708FaultRecord.ThirdPartyId,
                DateTime = dbGdaQJ1708FaultRecord.DateTime,
                MessageId = dbGdaQJ1708FaultRecord.MessageId,
                ParameterId = dbGdaQJ1708FaultRecord.ParameterId,
                SubsystemId = dbGdaQJ1708FaultRecord.SubsystemId,
                FailureModeIdentifier = dbGdaQJ1708FaultRecord.FailureModeIdentifier,
                OccurrenceCount = dbGdaQJ1708FaultRecord.OccurrenceCount,
                FaultStateActive = dbGdaQJ1708FaultRecord.FaultStateActive,
                OriginalRecordLastChangedUtc = dbGdaQJ1708FaultRecord.RecordLastChangedUtc,
                FailureReason = failureReason,
                RecordCreationTimeUtc = DateTime.UtcNow
            };

            return dbGdaQJ1708FaultRecordFail;
        }
    }
}
