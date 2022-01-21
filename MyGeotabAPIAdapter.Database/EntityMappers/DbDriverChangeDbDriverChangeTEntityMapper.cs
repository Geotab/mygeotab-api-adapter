using MyGeotabAPIAdapter.Database.Models;
using System;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DbDriverChange"/> and <see cref="DbDriverChangeT"/> entities.
    /// </summary>
    public class DbDriverChangeDbDriverChangeTEntityMapper : IDbDriverChangeDbDriverChangeTEntityMapper
    {
        /// <inheritdoc/>
        public DbDriverChangeT CreateEntity(DbDriverChange entityToMapTo, long driverChangeTypeId, long deviceId, long driverId)
        {
            DbDriverChangeT dbDriverChangeT = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DateTime = entityToMapTo.DateTime,
                DeviceId = deviceId,
                DriverChangeTypeId = driverChangeTypeId,
                DriverId = driverId,
                GeotabId = entityToMapTo.GeotabId,
                Version = entityToMapTo.Version,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            return dbDriverChangeT;
        }

        /// <inheritdoc/>
        public void UpdateEntity(DbDriverChangeT entityToUpdate, DbDriverChange entityToMapTo, long driverChangeTypeId, long deviceId, long driverId)
        {
            if (entityToUpdate.GeotabId != entityToMapTo.GeotabId)
            {
                throw new ArgumentException($"Cannot update {nameof(DbDriverChangeT)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(DbDriverChange)} '{entityToMapTo.id} (GeotabId {entityToMapTo.GeotabId})' because the GeotabIds do not match.");
            }

            entityToUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
            entityToUpdate.DateTime = entityToMapTo.DateTime;
            entityToUpdate.DeviceId = deviceId;
            entityToUpdate.DriverChangeTypeId = driverChangeTypeId;
            entityToUpdate.DriverId = driverId;
            entityToUpdate.Version = entityToMapTo.Version;
            entityToUpdate.RecordLastChangedUtc = DateTime.UtcNow;
        }
    }
}
