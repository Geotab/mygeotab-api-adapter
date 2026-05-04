using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DIGJ1708FaultRecord"/> entities from <see cref="DbGdaQJ1708FaultRecord"/> entities.
    /// </summary>
    public class DbGdaQJ1708FaultRecordDIGJ1708FaultRecordEntityMapper : IDbGdaQJ1708FaultRecordDIGJ1708FaultRecordEntityMapper
    {
        /// <inheritdoc/>
        public DIGJ1708FaultRecord CreateEntity(DbGdaQJ1708FaultRecord dbGdaQJ1708FaultRecord, string serialNo)
        {
            var digJ1708FaultRecord = new DIGJ1708FaultRecord
            {
                DateTime = DateTime.SpecifyKind(dbGdaQJ1708FaultRecord.DateTime, DateTimeKind.Utc),
                SerialNo = serialNo,
                MessageId = dbGdaQJ1708FaultRecord.MessageId,
                ParameterId = dbGdaQJ1708FaultRecord.ParameterId,
                SubsystemId = dbGdaQJ1708FaultRecord.SubsystemId,
                FailureModeIdentifier = dbGdaQJ1708FaultRecord.FailureModeIdentifier,
                OccurrenceCount = dbGdaQJ1708FaultRecord.OccurrenceCount,
                FaultStateActive = dbGdaQJ1708FaultRecord.FaultStateActive
            };

            return digJ1708FaultRecord;
        }
    }
}
