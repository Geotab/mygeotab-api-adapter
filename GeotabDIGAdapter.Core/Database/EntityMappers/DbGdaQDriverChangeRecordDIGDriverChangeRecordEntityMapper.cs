using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DIGDriverChangeRecord"/> entities from <see cref="DbGdaQDriverChangeRecord"/> entities.
    /// </summary>
    public class DbGdaQDriverChangeRecordDIGDriverChangeRecordEntityMapper : IDbGdaQDriverChangeRecordDIGDriverChangeRecordEntityMapper
    {
        /// <inheritdoc/>
        public DIGDriverChangeRecord CreateEntity(DbGdaQDriverChangeRecord dbGdaQDriverChangeRecord, string serialNo)
        {
            var digDriverChangeRecord = new DIGDriverChangeRecord
            {
                DateTime = DateTime.SpecifyKind(dbGdaQDriverChangeRecord.DateTime, DateTimeKind.Utc),
                SerialNo = serialNo,
                KeyType = dbGdaQDriverChangeRecord.KeyType,
                DriverId = dbGdaQDriverChangeRecord.DriverId
            };

            return digDriverChangeRecord;
        }
    }
}
