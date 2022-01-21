using MyGeotabAPIAdapter.Database.Models;
using System;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DbLogRecord"/> and <see cref="DbLogRecordT"/> entities.
    /// </summary>
    public class DbLogRecordDbLogRecordTEntityMapper : IDbLogRecordDbLogRecordTEntityMapper
    {
        /// <inheritdoc/>
        public DbLogRecordT CreateEntity(DbLogRecord entityToMapTo, long deviceId)
        {
            DbLogRecordT dbLogRecordT = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DateTime = entityToMapTo.DateTime,
                DeviceId = deviceId,
                GeotabId = entityToMapTo.GeotabId,
                Latitude = entityToMapTo.Latitude,
                Longitude = entityToMapTo.Longitude,
                Speed = entityToMapTo.Speed,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            return dbLogRecordT;
        }

        /// <inheritdoc/>
        public void UpdateEntity(DbLogRecordT entityToUpdate, DbLogRecord entityToMapTo, long deviceId)
        {
            if (entityToUpdate.GeotabId != entityToMapTo.GeotabId)
            {
                throw new ArgumentException($"Cannot update {nameof(DbLogRecordT)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(DbLogRecord)} '{entityToMapTo.id} (GeotabId {entityToMapTo.GeotabId})' because the GeotabIds do not match.");
            }

            entityToUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
            entityToUpdate.DateTime = entityToMapTo.DateTime;
            entityToUpdate.DeviceId = deviceId;
            entityToUpdate.Latitude = entityToMapTo.Latitude;
            entityToUpdate.Longitude = entityToMapTo.Longitude;
            entityToUpdate.Speed = entityToMapTo.Speed;
            entityToUpdate.RecordLastChangedUtc = DateTime.UtcNow;
        }
    }
}
