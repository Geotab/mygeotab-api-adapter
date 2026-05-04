using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DIGGpsRecord"/> entities from <see cref="DbGdaQGpsRecord"/> entities.
    /// </summary>
    public class DbGdaQGpsRecordDIGGpsRecordEntityMapper : IDbGdaQGpsRecordDIGGpsRecordEntityMapper
    {
        /// <inheritdoc/>
        public DIGGpsRecord CreateEntity(DbGdaQGpsRecord dbGdaQGpsRecord, string serialNo)
        {
            var digGpsRecord = new DIGGpsRecord
            {
                DateTime = DateTime.SpecifyKind(dbGdaQGpsRecord.DateTime, DateTimeKind.Utc),
                SerialNo = serialNo,
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
                IsAuxiliary8On = dbGdaQGpsRecord.IsAuxiliary8On
            };

            return digGpsRecord;
        }
    }
}
