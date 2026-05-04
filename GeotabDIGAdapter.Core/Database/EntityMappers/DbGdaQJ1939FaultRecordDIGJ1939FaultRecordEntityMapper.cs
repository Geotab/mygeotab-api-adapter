using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DIGJ1939FaultRecord"/> entities from <see cref="DbGdaQJ1939FaultRecord"/> entities.
    /// </summary>
    public class DbGdaQJ1939FaultRecordDIGJ1939FaultRecordEntityMapper : IDbGdaQJ1939FaultRecordDIGJ1939FaultRecordEntityMapper
    {
        /// <inheritdoc/>
        public DIGJ1939FaultRecord CreateEntity(DbGdaQJ1939FaultRecord dbGdaQJ1939FaultRecord, string serialNo)
        {
            var digJ1939FaultRecord = new DIGJ1939FaultRecord
            {
                DateTime = DateTime.SpecifyKind(dbGdaQJ1939FaultRecord.DateTime, DateTimeKind.Utc),
                SerialNo = serialNo,
                SuspectParameterNumber = dbGdaQJ1939FaultRecord.SuspectParameterNumber,
                FailureModeIdentifier = dbGdaQJ1939FaultRecord.FailureModeIdentifier,
                OccurrenceCount = dbGdaQJ1939FaultRecord.OccurrenceCount,
                SourceAddress = dbGdaQJ1939FaultRecord.SourceAddress,
                MalfunctionLamp = dbGdaQJ1939FaultRecord.MalfunctionLamp,
                RedStopLamp = dbGdaQJ1939FaultRecord.RedStopLamp,
                AmberWarningLamp = dbGdaQJ1939FaultRecord.AmberWarningLamp,
                ProtectWarningLamp = dbGdaQJ1939FaultRecord.ProtectWarningLamp,
                FaultStateActive = dbGdaQJ1939FaultRecord.FaultStateActive
            };

            return digJ1939FaultRecord;
        }
    }
}
