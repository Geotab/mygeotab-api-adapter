using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;
using Newtonsoft.Json;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DutyStatusLog"/> and <see cref="DbStgDutyStatusLog2"/> entities.
    /// </summary>
    public class GeotabDutyStatusLogDbStgDutyStatusLog2ObjectMapper : IGeotabDutyStatusLogDbStgDutyStatusLog2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;

        public GeotabDutyStatusLogDbStgDutyStatusLog2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }

        /// <inheritdoc/>
        //public DbStgDutyStatusLog2 CreateEntity(DutyStatusLog entityToMapTo, long deviceId, long? driverId, long? editRequestedByUserId, long? ParentId)
        public DbStgDutyStatusLog2 CreateEntity(DutyStatusLog entityToMapTo, long? deviceId, long? driverId, long? editRequestedByUserId)
        {
            DbStgDutyStatusLog2 dbStgDutyStatusLog2 = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DateTime = (DateTime)entityToMapTo.DateTime,
                DeviceId = deviceId,
                DriverId = driverId,
                EditRequestedByUserId = editRequestedByUserId,
                GeotabId = entityToMapTo.Id.ToString(),
                id = geotabIdConverter.ToGuid(entityToMapTo.Id),
                Version = (long)entityToMapTo.Version,
                RecordCreationTimeUtc = DateTime.UtcNow
            };
            if (entityToMapTo.Annotations != null)
            {
                string annotations = JsonConvert.SerializeObject(entityToMapTo.Annotations);
                dbStgDutyStatusLog2.Annotations = annotations;
            }
            if (entityToMapTo.CoDrivers != null)
            {
                string coDrivers = JsonConvert.SerializeObject(entityToMapTo.CoDrivers);
                dbStgDutyStatusLog2.CoDrivers = coDrivers;
            }
            if (entityToMapTo.DeferralMinutes != null)
            {
                dbStgDutyStatusLog2.DeferralMinutes = (int)entityToMapTo.DeferralMinutes;
            }
            if (entityToMapTo.DeferralStatus != null)
            {
                dbStgDutyStatusLog2.DeferralStatus = entityToMapTo.DeferralStatus.ToString();
            }

            if (entityToMapTo.DistanceSinceValidCoordinates != null)
            {
                dbStgDutyStatusLog2.DistanceSinceValidCoordinates = (float)entityToMapTo.DistanceSinceValidCoordinates;
            }

            if (entityToMapTo.EditDateTime != null)
            {
                dbStgDutyStatusLog2.EditDateTime = (DateTime)entityToMapTo.EditDateTime;
            }
            if (entityToMapTo.EngineHours != null)
            {
                dbStgDutyStatusLog2.EngineHours = (double)entityToMapTo.EngineHours;
            }
            if (entityToMapTo.EventCheckSum != null)
            {
                dbStgDutyStatusLog2.EventCheckSum = (long)entityToMapTo.EventCheckSum;
            }
            if (entityToMapTo.EventCode != null)
            {
                dbStgDutyStatusLog2.EventCode = (byte)entityToMapTo.EventCode;
            }
            if (entityToMapTo.EventRecordStatus != null)
            {
                dbStgDutyStatusLog2.EventRecordStatus = (byte)entityToMapTo.EventRecordStatus;
            }
            if (entityToMapTo.EventType != null)
            {
                dbStgDutyStatusLog2.EventType = (byte)entityToMapTo.EventType;
            }
            if (entityToMapTo.IsHidden != null)
            {
                dbStgDutyStatusLog2.IsHidden = (bool)entityToMapTo.IsHidden;
            }
            if (entityToMapTo.IsIgnored != null)
            {
                dbStgDutyStatusLog2.IsIgnored = (bool)entityToMapTo.IsIgnored;
            }
            if (entityToMapTo.IsTransitioning != null)
            {
                dbStgDutyStatusLog2.IsTransitioning = (bool)entityToMapTo.IsTransitioning;
            }
            if (entityToMapTo.Location != null)
            {
                string location = JsonConvert.SerializeObject(entityToMapTo.Location);
                dbStgDutyStatusLog2.Location = location;

                Geotab.Drawing.PointF? dutyStatusLogLocationPoint = entityToMapTo.Location.Location;
                if (dutyStatusLogLocationPoint != null)
                {
                    dbStgDutyStatusLog2.LocationX = dutyStatusLogLocationPoint.X;
                    dbStgDutyStatusLog2.LocationY = dutyStatusLogLocationPoint.Y;
                }
            }
            if (entityToMapTo.Malfunction != null)
            {
                dbStgDutyStatusLog2.Malfunction = Enum.GetName(typeof(DutyStatusMalfunctionTypes), entityToMapTo.Malfunction);
            }
            if (entityToMapTo.Odometer != null)
            {
                dbStgDutyStatusLog2.Odometer = (double)entityToMapTo.Odometer;
            }
            if (entityToMapTo.Origin != null)
            {
                dbStgDutyStatusLog2.Origin = Enum.GetName(typeof(DutyStatusOrigin), entityToMapTo.Origin);
            }
            if (entityToMapTo.ParentId != null)
            {
                dbStgDutyStatusLog2.ParentId = entityToMapTo.ParentId.ToString();
            }
            if (entityToMapTo.Sequence != null)
            {
                dbStgDutyStatusLog2.Sequence = (long)entityToMapTo.Sequence;
            }
            if (entityToMapTo.State != null)
            {
                dbStgDutyStatusLog2.State = Enum.GetName(typeof(DutyStatusState), entityToMapTo.State);
            }
            if (entityToMapTo.Status != null)
            {
                dbStgDutyStatusLog2.Status = Enum.GetName(typeof(DutyStatusLogType), entityToMapTo.Status);
            }
            if (entityToMapTo.UserHosRuleSet != null)
            {
                string userHosRuleSet = JsonConvert.SerializeObject(entityToMapTo.UserHosRuleSet);
                dbStgDutyStatusLog2.UserHosRuleSet = userHosRuleSet;
            }
            if (entityToMapTo.VerifyDateTime != null)
            {
                dbStgDutyStatusLog2.VerifyDateTime = (DateTime)entityToMapTo.VerifyDateTime;
            }
            return dbStgDutyStatusLog2;
        }
    }
}
