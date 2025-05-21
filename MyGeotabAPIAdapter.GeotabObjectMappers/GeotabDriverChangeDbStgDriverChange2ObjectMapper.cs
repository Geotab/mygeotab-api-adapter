using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DriverChange"/> and <see cref="DbStgDriverChange2"/> entities.
    /// </summary>
    public class GeotabDriverChangeDbStgDriverChange2ObjectMapper : IGeotabDriverChangeDbStgDriverChange2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;

        public GeotabDriverChangeDbStgDriverChange2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }

        /// <inheritdoc/>
        public DbStgDriverChange2 CreateEntity(DriverChange entityToMapTo, long deviceId, long? driverId)
        {
            DbStgDriverChange2 dbStgDriverChange2 = new()
            {
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                DateTime = entityToMapTo.DateTime,
                DeviceId = deviceId,
                DriverId = driverId,
                GeotabId = entityToMapTo.Id.ToString(),
                id = geotabIdConverter.ToGuid(entityToMapTo.Id),
                Type = entityToMapTo.Type.ToString(),
                Version = (long)entityToMapTo.Version,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            return dbStgDriverChange2;
        }
    }
}
