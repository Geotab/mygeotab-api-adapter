using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DIGObdiiFaultRecord"/> entities from <see cref="DbGdaQObdiiFaultRecord"/> entities.
    /// </summary>
    public class DbGdaQObdiiFaultRecordDIGObdiiFaultRecordEntityMapper : IDbGdaQObdiiFaultRecordDIGObdiiFaultRecordEntityMapper
    {
        /// <inheritdoc/>
        public DIGObdiiFaultRecord CreateEntity(DbGdaQObdiiFaultRecord dbGdaQObdiiFaultRecord, string serialNo)
        {
            var digObdiiFaultRecord = new DIGObdiiFaultRecord
            {
                DateTime = DateTime.SpecifyKind(dbGdaQObdiiFaultRecord.DateTime, DateTimeKind.Utc),
                SerialNo = serialNo,
                Code = dbGdaQObdiiFaultRecord.Code,
                FaultStateActive = dbGdaQObdiiFaultRecord.FaultStateActive
            };

            return digObdiiFaultRecord;
        }
    }
}
