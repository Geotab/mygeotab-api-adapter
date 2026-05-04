using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DIGVinRecord"/> entities from <see cref="DbGdaQVinRecord"/> entities.
    /// </summary>
    public class DbGdaQVinRecordDIGVinRecordEntityMapper : IDbGdaQVinRecordDIGVinRecordEntityMapper
    {
        /// <inheritdoc/>
        public DIGVinRecord CreateEntity(DbGdaQVinRecord dbGdaQVinRecord, string serialNo)
        {
            var digVinRecord = new DIGVinRecord
            {
                DateTime = DateTime.SpecifyKind(dbGdaQVinRecord.DateTime, DateTimeKind.Utc),
                SerialNo = serialNo,
                VehicleIdentificationNumber = dbGdaQVinRecord.VehicleIdentificationNumber
            };

            return digVinRecord;
        }
    }
}
