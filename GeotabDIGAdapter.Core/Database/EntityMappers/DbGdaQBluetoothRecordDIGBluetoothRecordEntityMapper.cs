using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DIGBluetoothRecord"/> entities from <see cref="DbGdaQBluetoothRecord"/> entities.
    /// </summary>
    public class DbGdaQBluetoothRecordDIGBluetoothRecordEntityMapper : IDbGdaQBluetoothRecordDIGBluetoothRecordEntityMapper
    {
        /// <inheritdoc/>
        public DIGBluetoothRecord CreateEntity(DbGdaQBluetoothRecord dbGdaQBluetoothRecord, string serialNo)
        {
            var digBluetoothRecord = new DIGBluetoothRecord
            {
                DateTime = DateTime.SpecifyKind(dbGdaQBluetoothRecord.DateTime, DateTimeKind.Utc),
                SerialNo = serialNo,
                Address = dbGdaQBluetoothRecord.Address,
                Data = dbGdaQBluetoothRecord.Data,
                DataType = dbGdaQBluetoothRecord.DataType
            };

            return digBluetoothRecord;
        }
    }
}
