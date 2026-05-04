using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DbGdaQJ1939FaultRecordFail"/> entities from <see cref="DbGdaQJ1939FaultRecord"/> entities.
    /// </summary>
    public class DbGdaQJ1939FaultRecordDbGdaQJ1939FaultRecordFailEntityMapper : IDbGdaQJ1939FaultRecordDbGdaQJ1939FaultRecordFailEntityMapper
    {
        /// <inheritdoc/>
        public DbGdaQJ1939FaultRecordFail CreateEntity(DbGdaQJ1939FaultRecord dbGdaQJ1939FaultRecord, string failureReason)
        {
            var dbGdaQJ1939FaultRecordFail = new DbGdaQJ1939FaultRecordFail
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                OriginalQueueId = dbGdaQJ1939FaultRecord.id,
                ThirdPartyId = dbGdaQJ1939FaultRecord.ThirdPartyId,
                DateTime = dbGdaQJ1939FaultRecord.DateTime,
                SuspectParameterNumber = dbGdaQJ1939FaultRecord.SuspectParameterNumber,
                FailureModeIdentifier = dbGdaQJ1939FaultRecord.FailureModeIdentifier,
                OccurrenceCount = dbGdaQJ1939FaultRecord.OccurrenceCount,
                SourceAddress = dbGdaQJ1939FaultRecord.SourceAddress,
                MalfunctionLamp = dbGdaQJ1939FaultRecord.MalfunctionLamp,
                RedStopLamp = dbGdaQJ1939FaultRecord.RedStopLamp,
                AmberWarningLamp = dbGdaQJ1939FaultRecord.AmberWarningLamp,
                ProtectWarningLamp = dbGdaQJ1939FaultRecord.ProtectWarningLamp,
                FaultStateActive = dbGdaQJ1939FaultRecord.FaultStateActive,
                OriginalRecordLastChangedUtc = dbGdaQJ1939FaultRecord.RecordLastChangedUtc,
                FailureReason = failureReason,
                RecordCreationTimeUtc = DateTime.UtcNow
            };

            return dbGdaQJ1939FaultRecordFail;
        }
    }
}
