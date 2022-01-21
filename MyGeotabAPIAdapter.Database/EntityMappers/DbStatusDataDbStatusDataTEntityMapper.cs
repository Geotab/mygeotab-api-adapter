using MyGeotabAPIAdapter.Database.Models;
using System;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DbStatusData"/> and <see cref="DbStatusDataT"/> entities.
    /// </summary>
    public class DbStatusDataDbStatusDataTEntityMapper : IDbStatusDataDbStatusDataTEntityMapper
    {
        /// <inheritdoc/>
        public DbStatusDataT CreateEntity(DbStatusData entityToMapTo, long deviceId, long diagnosticId)
        {
            DbStatusDataT dbStatusDataT = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                Data = entityToMapTo.Data,
                DateTime = entityToMapTo.DateTime,
                DeviceId = deviceId,
                DiagnosticId = diagnosticId,
                GeotabId = entityToMapTo.GeotabId,
                LongLatProcessed = false,
                DriverIdProcessed = false,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            return dbStatusDataT;
        }

        /// <inheritdoc/>
        public void UpdateEntity(DbStatusDataT entityToUpdate, DbStatusData entityToMapTo, long deviceId, long diagnosticId)
        {
            if (entityToUpdate.GeotabId != entityToMapTo.GeotabId)
            {
                throw new ArgumentException($"Cannot update {nameof(DbStatusDataT)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(DbStatusData)} '{entityToMapTo.id} (GeotabId {entityToMapTo.GeotabId})' because the GeotabIds do not match.");
            }

            entityToUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
            entityToUpdate.Data = entityToMapTo.Data;
            entityToUpdate.DateTime = entityToMapTo.DateTime;
            entityToUpdate.DeviceId = deviceId;
            entityToUpdate.DiagnosticId = diagnosticId;
            entityToUpdate.RecordLastChangedUtc = DateTime.UtcNow;
        }
    }
}
