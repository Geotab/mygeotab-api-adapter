using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DIGGenericFaultRecord"/> entities from <see cref="DbGdaQGenericFaultRecord"/> entities.
    /// </summary>
    public class DbGdaQGenericFaultRecordDIGGenericFaultRecordEntityMapper : IDbGdaQGenericFaultRecordDIGGenericFaultRecordEntityMapper
    {
        /// <inheritdoc/>
        public DIGGenericFaultRecord CreateEntity(DbGdaQGenericFaultRecord dbGdaQGenericFaultRecord, string serialNo)
        {
            var digGenericFaultRecord = new DIGGenericFaultRecord
            {
                DateTime = DateTime.SpecifyKind(dbGdaQGenericFaultRecord.DateTime, DateTimeKind.Utc),
                SerialNo = serialNo,
                Code = dbGdaQGenericFaultRecord.Code,
                FaultStateActive = dbGdaQGenericFaultRecord.FaultStateActive
            };

            return digGenericFaultRecord;
        }
    }
}
