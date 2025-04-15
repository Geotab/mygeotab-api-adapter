using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using Newtonsoft.Json;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DutyStatusLog"/> and <see cref="DbDutyStatusLog"/> entities.
    /// </summary>
    public class GeotabDutyStatusLogDbDutyStatusLogObjectMapper : IGeotabDutyStatusLogDbDutyStatusLogObjectMapper
    {
        /// <inheritdoc/>
        public List<DbDutyStatusLog> CreateEntities(List<DutyStatusLog> entitiesToMapTo)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbDutyStatusLogs = new List<DbDutyStatusLog>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbDutyStatusLog = CreateEntity(entity);
                dbDutyStatusLog.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbDutyStatusLogs.Add(dbDutyStatusLog);
            }
            return dbDutyStatusLogs;
        }

        /// <inheritdoc/>
        public DbDutyStatusLog CreateEntity(DutyStatusLog entityToMapTo)
        {
            DbDutyStatusLog dbDutyStatusLog = new()
            {
                GeotabId = entityToMapTo.Id.ToString(),
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DateTime = entityToMapTo.DateTime,
                Version = (long)entityToMapTo.Version,
                RecordCreationTimeUtc = DateTime.UtcNow
            };
            if (entityToMapTo.Annotations != null)
            {
                string annotations = JsonConvert.SerializeObject(entityToMapTo.Annotations);
                dbDutyStatusLog.Annotations = annotations;
            }
            if (entityToMapTo.CoDrivers != null)
            {
                string coDrivers = JsonConvert.SerializeObject(entityToMapTo.CoDrivers);
                dbDutyStatusLog.CoDrivers = coDrivers;
            }
            if (entityToMapTo.DeferralMinutes != null)
            {
                dbDutyStatusLog.DeferralMinutes = (int)entityToMapTo.DeferralMinutes;
            }
            if (entityToMapTo.DeferralStatus != null)
            {
                dbDutyStatusLog.DeferralStatus = Enum.GetName(typeof(DutyStatusDeferralType), entityToMapTo.DeferralStatus);
            }
            if (entityToMapTo.Device != null)
            {
                Device dutyStatusLogDevice = entityToMapTo.Device;
                dbDutyStatusLog.DeviceId = dutyStatusLogDevice.Id.ToString();
            }
            if (entityToMapTo.DistanceSinceValidCoordinates != null)
            {
                dbDutyStatusLog.DistanceSinceValidCoordinates = (float)entityToMapTo.DistanceSinceValidCoordinates;
            }
            if (entityToMapTo.Driver != null)
            {
                User dbDutyStatusLogDriver = entityToMapTo.Driver;
                dbDutyStatusLog.DriverId = dbDutyStatusLogDriver.Id.ToString();
            }
            if (entityToMapTo.EditDateTime != null)
            {
                dbDutyStatusLog.EditDateTime = (DateTime)entityToMapTo.EditDateTime;
            }
            if (entityToMapTo.EditRequestedByUser != null)
            {
                User dbDutyStatusLogEditRequestedByUser = entityToMapTo.EditRequestedByUser;
                dbDutyStatusLog.EditRequestedByUserId = dbDutyStatusLogEditRequestedByUser.Id.ToString();
            }
            if (entityToMapTo.EngineHours != null)
            {
                dbDutyStatusLog.EngineHours = (double)entityToMapTo.EngineHours;
            }
            if (entityToMapTo.EventCheckSum != null)
            {
                dbDutyStatusLog.EventCheckSum = (long)entityToMapTo.EventCheckSum;
            }
            if (entityToMapTo.EventCode != null)
            {
                dbDutyStatusLog.EventCode = (byte)entityToMapTo.EventCode;
            }
            if (entityToMapTo.EventRecordStatus != null)
            {
                dbDutyStatusLog.EventRecordStatus = (byte)entityToMapTo.EventRecordStatus;
            }
            if (entityToMapTo.EventType != null)
            {
                dbDutyStatusLog.EventType = (byte)entityToMapTo.EventType;
            }
            if (entityToMapTo.IsHidden != null)
            {
                dbDutyStatusLog.IsHidden = (bool)entityToMapTo.IsHidden;
            }
            if (entityToMapTo.IsIgnored != null)
            {
                dbDutyStatusLog.IsIgnored = (bool)entityToMapTo.IsIgnored;
            }
            if (entityToMapTo.IsTransitioning != null)
            {
                dbDutyStatusLog.IsTransitioning = (bool)entityToMapTo.IsTransitioning;
            }
            if (entityToMapTo.Location != null)
            {
                string location = JsonConvert.SerializeObject(entityToMapTo.Location);
                dbDutyStatusLog.Location = location;

                Geotab.Drawing.PointF? dutyStatusLogLocationPoint = entityToMapTo.Location.Location;
                if (dutyStatusLogLocationPoint != null)
                {
                    dbDutyStatusLog.LocationX = dutyStatusLogLocationPoint.X;
                    dbDutyStatusLog.LocationY = dutyStatusLogLocationPoint.Y;
                }
            }
            if (entityToMapTo.Malfunction != null)
            {
                dbDutyStatusLog.Malfunction = Enum.GetName(typeof(DutyStatusMalfunctionTypes), entityToMapTo.Malfunction);
            }
            if (entityToMapTo.Odometer != null)
            {
                dbDutyStatusLog.Odometer = (double)entityToMapTo.Odometer;
            }
            if (entityToMapTo.Origin != null)
            {
                dbDutyStatusLog.Origin = Enum.GetName(typeof(DutyStatusOrigin), entityToMapTo.Origin);
            }
            if (entityToMapTo.ParentId != null)
            {
                dbDutyStatusLog.ParentId = entityToMapTo.ParentId.ToString();
            }
            if (entityToMapTo.Sequence != null)
            {
                dbDutyStatusLog.Sequence = (long)entityToMapTo.Sequence;
            }
            if (entityToMapTo.State != null)
            {
                dbDutyStatusLog.State = Enum.GetName(typeof(DutyStatusState), entityToMapTo.State);
            }
            if (entityToMapTo.Status != null)
            {
                dbDutyStatusLog.Status = Enum.GetName(typeof(DutyStatusLogType), entityToMapTo.Status);
            }
            if (entityToMapTo.UserHosRuleSet != null)
            {
                string userHosRuleSet = JsonConvert.SerializeObject(entityToMapTo.UserHosRuleSet);
                dbDutyStatusLog.UserHosRuleSet = userHosRuleSet;
            }
            if (entityToMapTo.VerifyDateTime != null)
            {
                dbDutyStatusLog.VerifyDateTime = (DateTime)entityToMapTo.VerifyDateTime;
            }
            return dbDutyStatusLog;
        }
    }
}
