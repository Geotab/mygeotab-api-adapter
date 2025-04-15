using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DeviceStatusInfo"/> and <see cref="DbDeviceStatusInfo"/> entities.
    /// </summary>
    public class GeotabDeviceStatusInfoDbDeviceStatusInfoObjectMapper : IGeotabDeviceStatusInfoDbDeviceStatusInfoObjectMapper
    {
        /// <inheritdoc/>
        public DbDeviceStatusInfo CreateEntity(DeviceStatusInfo entityToMapTo)
        {
            Device entityToMapToDevice = entityToMapTo.Device;
            Driver entityToMapToDriver = entityToMapTo.Driver;

            DbDeviceStatusInfo dbDeviceStatusInfo = new()
            {
                Bearing = entityToMapTo.Bearing.Value,
                GeotabId = entityToMapTo.Id.ToString(),
                CurrentStateDuration = entityToMapTo.CurrentStateDuration.ToString(),
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DateTime = entityToMapTo.DateTime.GetValueOrDefault(),
                DeviceId = entityToMapToDevice.Id.ToString(),
                DriverId = entityToMapToDriver.Id.ToString(),
                IsDeviceCommunicating = entityToMapTo.IsDeviceCommunicating,
                IsDriving = entityToMapTo.IsDriving,
                IsHistoricLastDriver = entityToMapTo.IsHistoricLastDriver,
                Latitude = entityToMapTo.Latitude.Value,
                Longitude = entityToMapTo.Longitude.Value,
                RecordLastChangedUtc = DateTime.UtcNow,
                Speed = entityToMapTo.Speed.Value
            };

            return dbDeviceStatusInfo;
        }

        /// <summary>
        /// Returns <c>true</c> if the <paramref name="DeviceStatusInfo"/> <see cref="DeviceStatusInfo.Id"/> is different from the <paramref name="dbDeviceStatusInfo"/> <see cref="DbDeviceStatusInfo.GeotabId"/>, but the underlying GUIDs are the same. Otherwise, returns <c>false</c>. A return value of <c>false</c> does not necessarily mean that the DeviceStatusInfo Id has changed, since it is possible for mis-matched <paramref name="dbDeviceStatusInfo"/> and <paramref name="DeviceStatusInfo"/> to be supplied as inputs. Intended to assist in identifying <see cref="DeviceStatusInfo"/>s whose <see cref="DeviceStatusInfo.Id"/>s have been changed as a result of the assignment of a <see cref="KnownId"/>.
        /// </summary>
        /// <param name="dbDeviceStatusInfo">The <see cref="DbDeviceStatusInfo"/> to be evaluated.</param>
        /// <param name="DeviceStatusInfo">The <see cref="DeviceStatusInfo"/> to be evaluated.</param>
        /// <returns></returns>
        static bool DeviceStatusInfoIdHasChanged(DbDeviceStatusInfo dbDeviceStatusInfo, DeviceStatusInfo DeviceStatusInfo)
        {
            if (dbDeviceStatusInfo.GeotabId != DeviceStatusInfo.Id.ToString())
            {
                // The Id may have changed due to a new KnownId being assigned to the DeviceStatusInfo. Check whether this is the case and only throw an exception if not.
                var dbDeviceStatusInfoGeotabId = Id.Create(dbDeviceStatusInfo.GeotabId);
                var dbDeviceStatusInfoGeotabGUID = dbDeviceStatusInfoGeotabId.GetValue().ToString();
                var DeviceStatusInfoGeotabGUID = DeviceStatusInfo.Id.GetValue().ToString();
                if (DeviceStatusInfoGeotabGUID == dbDeviceStatusInfoGeotabGUID)
                {
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public bool EntityRequiresUpdate(DbDeviceStatusInfo entityToEvaluate, DeviceStatusInfo entityToMapTo)
        {
            if (entityToEvaluate.GeotabId != entityToMapTo.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare {nameof(DbDeviceStatusInfo)} '{entityToEvaluate.id}' with {nameof(DeviceStatusInfo)} '{entityToMapTo.Id}' because the IDs do not match.");
            }

            string entityToMapToCurrentStateDuration = entityToMapTo.CurrentStateDuration.ToString();
            if (entityToEvaluate.CurrentStateDuration != entityToMapToCurrentStateDuration || entityToEvaluate.DateTime != entityToMapTo.DateTime)
            {
                return true;
            }
            if (entityToEvaluate.Bearing != entityToMapTo.Bearing || entityToEvaluate.DeviceId != entityToMapTo.Device.Id.ToString() || entityToEvaluate.DriverId != entityToMapTo.Driver.Id.ToString()
                || entityToEvaluate.IsDeviceCommunicating != entityToMapTo.IsDeviceCommunicating || entityToEvaluate.IsDriving != entityToMapTo.IsDriving
                || entityToEvaluate.IsHistoricLastDriver != entityToMapTo.IsHistoricLastDriver || entityToEvaluate.Latitude != entityToMapTo.Latitude
                || entityToEvaluate.Longitude != entityToMapTo.Longitude || entityToEvaluate.Speed != entityToMapTo.Speed)
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public DbDeviceStatusInfo UpdateEntity(DbDeviceStatusInfo entityToUpdate, DeviceStatusInfo entityToMapTo)
        {
            if (entityToUpdate.GeotabId != entityToMapTo.Id.ToString())
            {
                throw new ArgumentException($"Cannot update {nameof(DbDeviceStatusInfo)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(DeviceStatusInfo)} '{entityToMapTo.Id}' because the GeotabIds do not match.");
            }

            var updatedDbDeviceStatusInfo = CreateEntity(entityToMapTo);

            // Update id since id is auto-generated by the database on insert and is therefor not set by the CreateEntity method.
            updatedDbDeviceStatusInfo.id = entityToUpdate.id;
            updatedDbDeviceStatusInfo.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

            return updatedDbDeviceStatusInfo;
        }
    }
}
