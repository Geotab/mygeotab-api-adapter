using MyGeotabAPIAdapter.Database.Models;
using System;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DbBinaryData"/> and <see cref="DbBinaryDataT"/> entities.
    /// </summary>
    public class DbBinaryDataDbBinaryDataTEntityMapper : IDbBinaryDataDbBinaryDataTEntityMapper
    {
        /// <inheritdoc/>
        public DbBinaryDataT CreateEntity(DbBinaryData entityToMapTo, long binaryTypeId, long controllerId, long deviceId)
        {
            DbBinaryDataT dbBinaryDataT = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                BinaryTypeId = binaryTypeId,
                ControllerId = controllerId,
                Data = entityToMapTo.Data,
                DateTime = entityToMapTo.DateTime,
                DeviceId = deviceId,
                GeotabId = entityToMapTo.GeotabId,
                Version = entityToMapTo.Version,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            return dbBinaryDataT;
        }

        /// <inheritdoc/>
        public void UpdateEntity(DbBinaryDataT entityToUpdate, DbBinaryData entityToMapTo, long binaryTypeId, long controllerId, long deviceId)
        {
            if (entityToUpdate.GeotabId != entityToMapTo.GeotabId)
            {
                throw new ArgumentException($"Cannot update {nameof(DbBinaryDataT)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(DbBinaryData)} '{entityToMapTo.id} (GeotabId {entityToMapTo.GeotabId})' because the GeotabIds do not match.");
            }

            entityToUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
            entityToUpdate.BinaryTypeId = binaryTypeId;
            entityToUpdate.ControllerId = controllerId;
            entityToUpdate.Data = entityToMapTo.Data;
            entityToUpdate.DateTime = entityToMapTo.DateTime;
            entityToUpdate.DeviceId = deviceId;
            entityToUpdate.GeotabId = entityToMapTo.GeotabId;
            entityToUpdate.Version = entityToMapTo.Version;
            entityToUpdate.RecordLastChangedUtc = DateTime.UtcNow;
        }
    }
}
