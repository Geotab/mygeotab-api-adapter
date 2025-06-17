using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DVIRLog"/> and <see cref="DbStgDVIRLog2"/> entities.
    /// </summary>
    public class GeotabDVIRLogDbStgDVIRLog2ObjectMapper : IGeotabDVIRLogDbStgDVIRLog2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;

        public GeotabDVIRLogDbStgDVIRLog2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }

        /// <inheritdoc/>
        public DbStgDVIRLog2 CreateEntity(DVIRLog entityToMapTo, long? certifiedByUserId, long deviceId, long? driverId, long? repairedByUserId)
        {
            DbStgDVIRLog2 dbStgDVIRLog2 = new()
            {
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                AuthorityAddress = entityToMapTo.AuthorityAddress,
                AuthorityName = entityToMapTo.AuthorityName,
                CertifiedByUserId = certifiedByUserId,
                CertifiedDate = entityToMapTo.CertifyDate,
                CertifyRemark = entityToMapTo.CertifyRemark,
                DateTime = (DateTime)entityToMapTo.DateTime,
                DeviceId = deviceId,
                DriverId = driverId,
                DriverRemark = entityToMapTo.DriverRemark,
                Duration = entityToMapTo.Duration,
                EngineHours = entityToMapTo.EngineHours,
                GeotabId = entityToMapTo.Id.ToString(),
                id = geotabIdConverter.ToGuid(entityToMapTo.Id),
                IsSafeToOperate = entityToMapTo.IsSafeToOperate,
                LoadHeight = entityToMapTo.LoadHeight,
                LoadWidth = entityToMapTo.LoadWidth,
                Odometer = entityToMapTo.Odometer,
                RepairDate = entityToMapTo.RepairDate,
                RepairedByUserId = repairedByUserId,
                RepairRemark = entityToMapTo.RepairRemark,
                Version = entityToMapTo.Version,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            if (entityToMapTo.Location != null)
            {
                dbStgDVIRLog2.LocationLatitude = entityToMapTo.Location.Location.Y;
                dbStgDVIRLog2.LocationLongitude = entityToMapTo.Location.Location.X;
            }
            if (entityToMapTo.LogType != null)
            {
                dbStgDVIRLog2.LogType = entityToMapTo.LogType.ToString();
            }
            return dbStgDVIRLog2;
        }
    }
}
