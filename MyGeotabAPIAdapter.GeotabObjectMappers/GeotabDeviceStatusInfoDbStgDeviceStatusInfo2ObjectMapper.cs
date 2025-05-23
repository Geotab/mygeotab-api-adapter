using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Aclass with methods involving mapping between <see cref="DeviceStatusInfo"/> and <see cref="DbStgDeviceStatusInfo2"/> entities.
    /// </summary>
    public class GeotabDeviceStatusInfoDbStgDeviceStatusInfo2ObjectMapper : IGeotabDeviceStatusInfoDbStgDeviceStatusInfo2ObjectMapper
    {
        public GeotabDeviceStatusInfoDbStgDeviceStatusInfo2ObjectMapper()
        {
        }

        /// <inheritdoc/>
        public DbStgDeviceStatusInfo2 CreateEntity(DeviceStatusInfo entityToMapTo, long deviceId, long? driverId)
        {
            DbStgDeviceStatusInfo2 dbStgDeviceStatusInfo2 = new()
            {
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                Bearing = entityToMapTo.Bearing.Value,
                CurrentStateDuration = entityToMapTo.CurrentStateDuration.ToString(),
                DateTime = entityToMapTo.DateTime,
                DeviceId = deviceId,
                DriverId = driverId,
                GeotabId = entityToMapTo.Id.ToString(),
                id = deviceId,
                IsDeviceCommunicating = entityToMapTo.IsDeviceCommunicating,
                IsDriving = entityToMapTo.IsDriving,
                IsHistoricLastDriver = entityToMapTo.IsHistoricLastDriver,
                Latitude = entityToMapTo.Latitude.Value,
                Longitude = entityToMapTo.Longitude.Value,
                Speed = entityToMapTo.Speed.Value,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            return dbStgDeviceStatusInfo2;
        }
    }
}
