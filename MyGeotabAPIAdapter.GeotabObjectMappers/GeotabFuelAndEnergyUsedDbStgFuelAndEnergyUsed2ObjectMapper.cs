using Geotab.Checkmate.ObjectModel.Fuel;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="FuelAndEnergyUsed"/> and <see cref="DbStgFuelAndEnergyUsed2"/> entities.
    /// </summary>
    public class GeotabFuelAndEnergyUsedDbStgFuelAndEnergyUsed2ObjectMapper : IGeotabFuelAndEnergyUsedDbStgFuelAndEnergyUsed2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;

        public GeotabFuelAndEnergyUsedDbStgFuelAndEnergyUsed2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }

        /// <inheritdoc/>
        public DbStgFuelAndEnergyUsed2 CreateEntity(FuelAndEnergyUsed entityToMapTo, long deviceId)
        {
            DbStgFuelAndEnergyUsed2 dbStgFuelAndEnergyUsed2 = new()
            {
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                DateTime = entityToMapTo.DateTime,
                DeviceId = deviceId,
                GeotabId = entityToMapTo.Id.ToString(),
                id = geotabIdConverter.ToGuid(entityToMapTo.Id),
                TotalEnergyUsedKwh = entityToMapTo.TotalEnergyUsedKwh,
                TotalFuelUsed = entityToMapTo.TotalFuelUsed,
                TotalIdlingEnergyUsedKwh = entityToMapTo.TotalIdlingEnergyUsedKwh,
                TotalIdlingFuelUsedL = entityToMapTo.TotalIdlingFuelUsedL,
                Version = (long)entityToMapTo.Version,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            return dbStgFuelAndEnergyUsed2;
        }
    }
}
