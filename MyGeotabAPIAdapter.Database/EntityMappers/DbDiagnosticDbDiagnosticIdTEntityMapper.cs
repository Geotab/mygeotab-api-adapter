﻿using MyGeotabAPIAdapter.Database.Models;
using System;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DbDiagnostic"/> and <see cref="DbDiagnosticIdT"/> entities.
    /// </summary>
    public class DbDiagnosticDbDiagnosticIdTEntityMapper : IDbDiagnosticDbDiagnosticIdTEntityMapper
    {
        /// <inheritdoc/>
        public DbDiagnosticIdT CreateEntity(DbDiagnostic entityToMapTo)
        {
            DbDiagnosticIdT DbDiagnosticIdT = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                GeotabGUIDString = entityToMapTo.GeotabGUIDString,
                GeotabId = entityToMapTo.GeotabId,
                HasShimId = entityToMapTo.HasShimId,
                FormerShimGeotabGUID = entityToMapTo.FormerShimGeotabGUID,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            return DbDiagnosticIdT;
        }

        /// <inheritdoc/>
        public void UpdateEntity(DbDiagnosticIdT entityToUpdate, DbDiagnostic entityToMapTo)
        {
            if (entityToUpdate.GeotabGUIDString != entityToMapTo.GeotabGUIDString || entityToUpdate.GeotabId != entityToMapTo.GeotabId)
            {
                throw new ArgumentException($"Cannot update {nameof(DbDiagnosticIdT)} '{entityToUpdate.id} (GeotabGUID '{entityToUpdate.GeotabGUIDString}', GeotabId '{entityToUpdate.GeotabId}')' with {nameof(DbDiagnostic)} '{entityToMapTo.id} (GeotabGUID '{entityToMapTo.GeotabGUIDString}', GeotabId '{entityToMapTo.GeotabId}')' because the GeotabGUIDs and/or GeotabIds do not match.");
            }

            entityToUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
            entityToUpdate.RecordLastChangedUtc = DateTime.UtcNow;
        }
    }
}
