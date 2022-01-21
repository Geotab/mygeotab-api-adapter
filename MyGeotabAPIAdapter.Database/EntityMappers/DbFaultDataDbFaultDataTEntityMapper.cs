using MyGeotabAPIAdapter.Database.Models;
using System;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DbFaultData"/> and <see cref="DbFaultDataT"/> entities.
    /// </summary>
    public class DbFaultDataDbFaultDataTEntityMapper : IDbFaultDataDbFaultDataTEntityMapper
    {
        /// <inheritdoc/>
        public DbFaultDataT CreateEntity(DbFaultData entityToMapTo, long deviceId, long diagnosticId, long? dismissUserId)
        {
            DbFaultDataT dbFaultDataT = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                AmberWarningLamp = entityToMapTo.AmberWarningLamp,
                ClassCode = entityToMapTo.ClassCode,
                ControllerId = entityToMapTo.ControllerId,
                ControllerName = entityToMapTo.ControllerName,
                Count = entityToMapTo.Count,
                DateTime = entityToMapTo.DateTime,
                DeviceId = deviceId,
                DiagnosticId = diagnosticId,
                DismissDateTime = entityToMapTo.DismissDateTime,
                DismissUserId = dismissUserId,
                FailureModeCode = entityToMapTo.FailureModeCode,
                FailureModeId = entityToMapTo.FailureModeId,
                FailureModeName = entityToMapTo.FailureModeName,
                FaultLampState = entityToMapTo.FaultLampState,
                FaultState = entityToMapTo.FaultState,
                GeotabId = entityToMapTo.GeotabId,
                MalfunctionLamp = entityToMapTo.MalfunctionLamp,
                ProtectWarningLamp = entityToMapTo.ProtectWarningLamp,
                RedStopLamp = entityToMapTo.RedStopLamp,
                Severity = entityToMapTo.Severity,
                SourceAddress = entityToMapTo.SourceAddress,
                LongLatProcessed = false,
                DriverIdProcessed = false,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            return dbFaultDataT;
        }

        /// <inheritdoc/>
        public void UpdateEntity(DbFaultDataT entityToUpdate, DbFaultData entityToMapTo, long deviceId, long diagnosticId, long? dismissUserId)
        {
            if (entityToUpdate.GeotabId != entityToMapTo.GeotabId)
            {
                throw new ArgumentException($"Cannot update {nameof(DbFaultDataT)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(DbFaultData)} '{entityToMapTo.id} (GeotabId {entityToMapTo.GeotabId})' because the GeotabIds do not match.");
            }

            entityToUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
            entityToUpdate.AmberWarningLamp = entityToMapTo.AmberWarningLamp;
            entityToUpdate.ClassCode = entityToMapTo.ClassCode;
            entityToUpdate.ControllerId = entityToMapTo.ControllerId;
            entityToUpdate.ControllerName = entityToMapTo.ControllerName;
            entityToUpdate.Count = entityToMapTo.Count;
            entityToUpdate.DateTime = entityToMapTo.DateTime;
            entityToUpdate.DeviceId = deviceId;
            entityToUpdate.DiagnosticId = diagnosticId;
            entityToUpdate.DismissDateTime = entityToMapTo.DismissDateTime;
            entityToUpdate.DismissUserId = dismissUserId;
            entityToUpdate.FailureModeCode = entityToMapTo.FailureModeCode;
            entityToUpdate.FailureModeId = entityToMapTo.FailureModeId;
            entityToUpdate.FailureModeName = entityToMapTo.FailureModeName;
            entityToUpdate.FaultLampState = entityToMapTo.FaultLampState;
            entityToUpdate.FaultState = entityToMapTo.FaultState;
            entityToUpdate.MalfunctionLamp = entityToMapTo.MalfunctionLamp;
            entityToUpdate.ProtectWarningLamp = entityToMapTo.ProtectWarningLamp;
            entityToUpdate.RedStopLamp = entityToMapTo.RedStopLamp;
            entityToUpdate.Severity = entityToMapTo.Severity;
            entityToUpdate.SourceAddress = entityToMapTo.SourceAddress;
            entityToUpdate.RecordLastChangedUtc = DateTime.UtcNow;
        }
    }
}
