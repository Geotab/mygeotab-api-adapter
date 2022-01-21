using MyGeotabAPIAdapter.Database.Models;
using System;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DbDevice"/> and <see cref="DbDeviceT"/> entities.
    /// </summary>
    public class DbDeviceDbDeviceTEntityMapper : IDbDeviceDbDeviceTEntityMapper
    {
        /// <inheritdoc/>
        public DbDeviceT CreateEntity(DbDevice entityToMapTo)
        {
            DbDeviceT dbDeviceT = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                ActiveFrom = entityToMapTo.ActiveFrom,
                ActiveTo = entityToMapTo.ActiveTo,
                Comment = entityToMapTo.Comment,
                DeviceType = entityToMapTo.DeviceType,
                EntityStatus = entityToMapTo.EntityStatus,
                GeotabId = entityToMapTo.GeotabId,
                LicensePlate = entityToMapTo.LicensePlate,
                LicenseState = entityToMapTo.LicenseState,
                Name = entityToMapTo.Name,
                RecordLastChangedUtc = DateTime.UtcNow,
                SerialNumber = entityToMapTo.SerialNumber,
                ProductId = entityToMapTo.ProductId,
                VIN = entityToMapTo.VIN
            };
            return dbDeviceT;
        }

        /// <inheritdoc/>
        public void UpdateEntity(DbDeviceT entityToUpdate, DbDevice entityToMapTo)
        {
            if (entityToUpdate.GeotabId != entityToMapTo.GeotabId)
            {
                throw new ArgumentException($"Cannot update {nameof(DbDeviceT)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(DbDevice)} '{entityToMapTo.id} (GeotabId {entityToMapTo.GeotabId})' because the GeotabIds do not match.");
            }

            entityToUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
            entityToUpdate.ActiveFrom = entityToMapTo.ActiveFrom;
            entityToUpdate.ActiveTo = entityToMapTo.ActiveTo;
            entityToUpdate.Comment = entityToMapTo.Comment;
            entityToUpdate.DeviceType = entityToMapTo.DeviceType;
            entityToUpdate.EntityStatus = entityToMapTo.EntityStatus;
            entityToUpdate.LicensePlate = entityToMapTo.LicensePlate;
            entityToUpdate.LicenseState = entityToMapTo.LicenseState;
            entityToUpdate.Name = entityToMapTo.Name;
            entityToUpdate.RecordLastChangedUtc = DateTime.UtcNow;
            entityToUpdate.SerialNumber = entityToMapTo.SerialNumber;
            entityToUpdate.ProductId = entityToMapTo.ProductId;
            entityToUpdate.VIN = entityToMapTo.VIN;
        }
    }
}
