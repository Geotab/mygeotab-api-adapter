using MyGeotabAPIAdapter.Database.Models;
using System;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DbDiagnostic"/> and <see cref="DbDiagnosticT"/> entities.
    /// </summary>
    public class DbDiagnosticDbDiagnosticTEntityMapper : IDbDiagnosticDbDiagnosticTEntityMapper
    {
        /// <inheritdoc/>
        public DbDiagnosticT CreateEntity(DbDiagnostic entityToMapTo)
        {
            DbDiagnosticT dbDiagnosticT = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                ControllerId = entityToMapTo.ControllerId,
                DiagnosticCode = entityToMapTo.DiagnosticCode,
                DiagnosticName = entityToMapTo.DiagnosticName,
                DiagnosticSourceId = entityToMapTo.DiagnosticSourceId,
                DiagnosticSourceName = entityToMapTo.DiagnosticSourceName,
                DiagnosticUnitOfMeasureId = entityToMapTo.DiagnosticUnitOfMeasureId,
                DiagnosticUnitOfMeasureName = entityToMapTo.DiagnosticUnitOfMeasureName,
                EntityStatus = entityToMapTo.EntityStatus,
                GeotabGUIDString = entityToMapTo.GeotabGUIDString,
                HasShimId = entityToMapTo.HasShimId,
                FormerShimGeotabGUID = entityToMapTo.FormerShimGeotabGUID,
                OBD2DTC = entityToMapTo.OBD2DTC,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            return dbDiagnosticT;
        }

        /// <inheritdoc/>
        public void UpdateEntity(DbDiagnosticT entityToUpdate, DbDiagnostic entityToMapTo)
        {
            if (entityToUpdate.GeotabGUIDString != entityToMapTo.GeotabGUIDString)
            {
                throw new ArgumentException($"Cannot update {nameof(DbDiagnosticT)} '{entityToUpdate.id} (GeotabGUID {entityToUpdate.GeotabGUIDString})' with {nameof(DbDiagnostic)} '{entityToMapTo.id} (GeotabGUID {entityToMapTo.GeotabGUIDString})' because the GeotabGUIDs do not match.");
            }

            entityToUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
            entityToUpdate.ControllerId = entityToMapTo.ControllerId;
            entityToUpdate.DiagnosticCode = entityToMapTo.DiagnosticCode;
            entityToUpdate.DiagnosticName = entityToMapTo.DiagnosticName;
            entityToUpdate.DiagnosticSourceId = entityToMapTo.DiagnosticSourceId;
            entityToUpdate.DiagnosticSourceName = entityToMapTo.DiagnosticSourceName;
            entityToUpdate.DiagnosticUnitOfMeasureId = entityToMapTo.DiagnosticUnitOfMeasureId;
            entityToUpdate.DiagnosticUnitOfMeasureName = entityToMapTo.DiagnosticUnitOfMeasureName;
            entityToUpdate.EntityStatus = entityToMapTo.EntityStatus;
            entityToUpdate.RecordLastChangedUtc = DateTime.UtcNow;
        }
    }
}
