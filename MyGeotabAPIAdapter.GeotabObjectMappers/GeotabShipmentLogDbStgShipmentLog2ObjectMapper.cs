using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="ShipmentLog"/> and <see cref="DbStgShipmentLog2"/> entities.
    /// </summary>
    public class GeotabShipmentLogDbStgShipmentLog2ObjectMapper : IGeotabShipmentLogDbStgShipmentLog2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;

        public GeotabShipmentLogDbStgShipmentLog2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }

        /// <inheritdoc/>
        public DbStgShipmentLog2 CreateEntity(ShipmentLog entityToMapTo, long? deviceId, long? driverId)
        {
            DbStgShipmentLog2 dbStgShipmentLog2 = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DateTime = (DateTime)entityToMapTo.DateTime,
                DeviceId = deviceId,
                DriverId = driverId,
                GeotabId = entityToMapTo.Id.ToString(),
                id = geotabIdConverter.ToGuid(entityToMapTo.Id),
                RecordLastChangedUtc = DateTime.UtcNow
            };
            if (entityToMapTo.ActiveFrom != null)
            {
                dbStgShipmentLog2.ActiveFrom = (DateTime)entityToMapTo.ActiveFrom;
            }
            if (entityToMapTo.ActiveTo != null)
            {
                dbStgShipmentLog2.ActiveTo = (DateTime)entityToMapTo.ActiveTo;
            }
            if (entityToMapTo.Commodity != null)
            {
                dbStgShipmentLog2.Commodity = entityToMapTo.Commodity;
            }
            if (entityToMapTo.DocumentNumber != null)
            {
                dbStgShipmentLog2.DocumentNumber = entityToMapTo.DocumentNumber;
            }
            if (entityToMapTo.ShipperName != null)
            {
                dbStgShipmentLog2.ShipperName = entityToMapTo.ShipperName;
            }
            if (entityToMapTo.Version != null)
            {
                dbStgShipmentLog2.Version = (long)entityToMapTo.Version;
            }
            return dbStgShipmentLog2;
        }
    }
}
