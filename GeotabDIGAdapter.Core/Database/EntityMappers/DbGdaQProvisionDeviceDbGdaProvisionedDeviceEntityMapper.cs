using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DbGdaProvisionedDevice"/> entities from <see cref="DbGdaQProvisionDevice"/> entities.
    /// </summary>
    public class DbGdaQProvisionDeviceDbGdaProvisionedDeviceEntityMapper : IDbGdaQProvisionDeviceDbGdaProvisionedDeviceEntityMapper
    {
        /// <inheritdoc/>
        public DbGdaProvisionedDevice CreateEntity(DbGdaQProvisionDevice dbGdaQProvisionDevice, string geotabSerialNumber)
        {
            var currentUtcDateTime = DateTime.UtcNow;

            var dbGdaProvisionedDevice = new DbGdaProvisionedDevice
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DeviceProvisionedDateTimeUtc = currentUtcDateTime,
                ErpNo = dbGdaQProvisionDevice.ErpNo,
                GeotabSerialNumber = geotabSerialNumber,
                IsOkayToSendDataToGeotab = false,
                RecordLastChangedUtc = currentUtcDateTime,
                ThirdPartyId = dbGdaQProvisionDevice.ThirdPartyId
            };

            return dbGdaProvisionedDevice;
        }
    }
}