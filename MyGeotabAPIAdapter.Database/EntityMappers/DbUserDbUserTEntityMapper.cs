using MyGeotabAPIAdapter.Database.Models;
using System;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DbUser"/> and <see cref="DbUserT"/> entities.
    /// </summary>
    public class DbUserDbUserTEntityMapper : IDbUserDbUserTEntityMapper
    {
        /// <inheritdoc/>
        public DbUserT CreateEntity(DbUser entityToMapTo)
        {
            DbUserT dbUserT = new()
            {
                ActiveFrom = entityToMapTo.ActiveFrom,
                ActiveTo = entityToMapTo.ActiveTo,
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                EmployeeNo = entityToMapTo.EmployeeNo,
                FirstName = entityToMapTo.FirstName,
                GeotabId = entityToMapTo.GeotabId,
                HosRuleSet = entityToMapTo.HosRuleSet,
                IsDriver = entityToMapTo.IsDriver,
                LastAccessDate = entityToMapTo.LastAccessDate,
                LastName = entityToMapTo.LastName,
                Name = entityToMapTo.Name,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            return dbUserT;
        }

        /// <inheritdoc/>
        public void UpdateEntity(DbUserT entityToUpdate, DbUser entityToMapTo)
        {
            if (entityToUpdate.GeotabId != entityToMapTo.GeotabId)
            {
                throw new ArgumentException($"Cannot update {nameof(DbUserT)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(DbUser)} '{entityToMapTo.id} (GeotabId {entityToMapTo.GeotabId})' because the GeotabIds do not match.");
            }

            entityToUpdate.ActiveFrom = entityToMapTo.ActiveFrom;
            entityToUpdate.ActiveTo = entityToMapTo.ActiveTo;
            entityToUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
            entityToUpdate.EmployeeNo = entityToMapTo.EmployeeNo;
            entityToUpdate.FirstName = entityToMapTo.FirstName;
            entityToUpdate.HosRuleSet = entityToMapTo.HosRuleSet;
            entityToUpdate.IsDriver = entityToMapTo.IsDriver;
            entityToUpdate.LastAccessDate = entityToMapTo.LastAccessDate;
            entityToUpdate.LastName = entityToMapTo.LastName;
            entityToUpdate.Name = entityToMapTo.Name;
            entityToUpdate.RecordLastChangedUtc = DateTime.UtcNow;
        }
    }
}
