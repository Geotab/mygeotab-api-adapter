using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="FaultData"/> and <see cref="DbFaultData"/> entities.
    /// </summary>
    public class GeotabFaultDataDbFaultDataObjectMapper : IGeotabFaultDataDbFaultDataObjectMapper
    {
        /// <inheritdoc/>
        public List<DbFaultData> CreateEntities(List<FaultData> entitiesToMapTo)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbFaultDatas = new List<DbFaultData>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbFaultData = CreateEntity(entity);
                dbFaultData.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbFaultDatas.Add(dbFaultData);
            }
            return dbFaultDatas;
        }

        /// <inheritdoc/>
        public DbFaultData CreateEntity(FaultData entityToMapTo)
        {
            Controller faultDataController = entityToMapTo.Controller;
            Device faultDataDevice = entityToMapTo.Device;
            Diagnostic faultDataDiagnostic = null;
            if (entityToMapTo.Diagnostic != null)
            {
                faultDataDiagnostic = entityToMapTo.Diagnostic;
            }
            User faultDataDismissUser = entityToMapTo.DismissUser;
            FailureMode faultDataFailureMode = entityToMapTo.FailureMode;
            var faultDataFaultState = entityToMapTo.FaultState;

            DbFaultData dbFaultData = new()
            {
                AmberWarningLamp = entityToMapTo.AmberWarningLamp,
                ControllerId = faultDataController.Id.ToString(),
                ControllerName = faultDataController.Name,
                Count = entityToMapTo.Count,
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DateTime = entityToMapTo.DateTime,
                DeviceId = faultDataDevice.Id.ToString(),
                DiagnosticId = "",
                DismissDateTime = entityToMapTo.DismissDateTime,
                DismissUserId = faultDataDismissUser.Id.ToString(),
                FailureModeId = faultDataFailureMode.Id.ToString(),
                FailureModeName = faultDataFailureMode.Name,
                FaultLampState = entityToMapTo.FaultLampState.HasValue ? entityToMapTo.FaultLampState.ToString() : null,
                GeotabId = entityToMapTo.Id.ToString(),
                MalfunctionLamp = entityToMapTo.MalfunctionLamp,
                ProtectWarningLamp = entityToMapTo.ProtectWarningLamp,
                RecordCreationTimeUtc = DateTime.UtcNow,
                RedStopLamp = entityToMapTo.RedStopLamp,
                Severity = entityToMapTo.Severity.HasValue ? entityToMapTo.Severity.ToString() : null,
                SourceAddress = entityToMapTo.SourceAddress
            };

            if (faultDataDiagnostic != null)
            {
                dbFaultData.DiagnosticId = faultDataDiagnostic.Id.ToString();
            }
            if (entityToMapTo.ClassCode != null)
            {
                dbFaultData.ClassCode = entityToMapTo.ClassCode.ToString();
            }
            if (faultDataFailureMode.Code != null)
            {
                dbFaultData.FailureModeCode = faultDataFailureMode.Code;
            }
            if (faultDataFaultState != null)
            {
                dbFaultData.FaultState = faultDataFaultState.ToString();
            }

            return dbFaultData;
        }
    }
}
