using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DIGAccelerationRecord"/> entities from <see cref="DbGdaQAccelerationRecord"/> entities.
    /// </summary>
    public class DbGdaQAccelerationRecordDIGAccelerationRecordEntityMapper : IDbGdaQAccelerationRecordDIGAccelerationRecordEntityMapper
    {
        /// <inheritdoc/>
        public DIGAccelerationRecord CreateEntity(DbGdaQAccelerationRecord dbGdaQAccelerationRecord, string serialNo)
        {
            var digAccelerationRecord = new DIGAccelerationRecord
            {
                DateTime = DateTime.SpecifyKind(dbGdaQAccelerationRecord.DateTime, DateTimeKind.Utc),
                SerialNo = serialNo,
                X = dbGdaQAccelerationRecord.X,
                Y = dbGdaQAccelerationRecord.Y,
                Z = dbGdaQAccelerationRecord.Z
            };

            return digAccelerationRecord;
        }
    }
}
