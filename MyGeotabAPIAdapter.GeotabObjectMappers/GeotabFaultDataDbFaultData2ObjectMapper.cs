using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="FaultData"/> and <see cref="DbFaultData2"/> entities.
    /// </summary>
    public class GeotabFaultDataDbFaultData2ObjectMapper : IGeotabFaultDataDbFaultData2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;

        public GeotabFaultDataDbFaultData2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }

        /// <inheritdoc/>
        public DbFaultData2 CreateEntity(FaultData entityToMapTo, long deviceId, long diagnosticId, long? dismissUserId)
        {
            long id = geotabIdConverter.ToLong(entityToMapTo.Id);

            Controller faultDataController = entityToMapTo.Controller;
            FailureMode faultDataFailureMode = entityToMapTo.FailureMode;
            var faultDataFaultState = entityToMapTo.FaultState;

            DbFaultData2 dbFaultData2 = new()
            {
                AmberWarningLamp = entityToMapTo.AmberWarningLamp,
                ControllerId = faultDataController.Id.ToString(),
                ControllerName = faultDataController.Name,
                Count = entityToMapTo.Count,
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DateTime = (DateTime)entityToMapTo.DateTime,
                DeviceId = deviceId,
                DiagnosticId = diagnosticId,
                DismissDateTime = entityToMapTo.DismissDateTime,
                DismissUserId = dismissUserId,
                FailureModeId = faultDataFailureMode.Id.ToString(),
                FailureModeName = faultDataFailureMode.Name,
                FaultLampState = entityToMapTo.FaultLampState.HasValue ? entityToMapTo.FaultLampState.ToString() : null,
                GeotabId = entityToMapTo.Id.ToString(),
                id = id,
                MalfunctionLamp = entityToMapTo.MalfunctionLamp,
                ProtectWarningLamp = entityToMapTo.ProtectWarningLamp,
                RecordCreationTimeUtc = DateTime.UtcNow,
                RedStopLamp = entityToMapTo.RedStopLamp,
                Severity = entityToMapTo.Severity.HasValue ? entityToMapTo.Severity.ToString() : null,
                SourceAddress = entityToMapTo.SourceAddress
            };

            if (entityToMapTo.ClassCode != null)
            {
                dbFaultData2.ClassCode = entityToMapTo.ClassCode.ToString();
            }
            if (faultDataFailureMode.Code != null)
            {
                dbFaultData2.FailureModeCode = faultDataFailureMode.Code;
            }
            if (faultDataFaultState != null)
            {
                dbFaultData2.FaultState = faultDataFaultState.ToString();
            }

            return dbFaultData2;
        }
    }
}
