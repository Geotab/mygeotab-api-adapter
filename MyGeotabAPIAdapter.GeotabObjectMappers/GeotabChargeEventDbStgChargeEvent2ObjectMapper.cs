using Geotab.Checkmate.ObjectModel.Charging;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="ChargeEvent"/> and <see cref="DbStgChargeEvent2"/> entities.
    /// </summary>
    public class GeotabChargeEventDbStgChargeEvent2ObjectMapper : IGeotabChargeEventDbStgChargeEvent2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;

        public GeotabChargeEventDbStgChargeEvent2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }

        public List<DbStgChargeEvent2> CreateEntities(List<ChargeEvent> entitiesToMapTo)
        {
            var dbStgChargeEvent2s = new List<DbStgChargeEvent2>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbStgChargeEvent2 = CreateEntity(entity);
                dbStgChargeEvent2s.Add(dbStgChargeEvent2);
            }
            return dbStgChargeEvent2s;
        }

        public DbStgChargeEvent2 CreateEntity(ChargeEvent entityToMapTo)
        {
            var device = entityToMapTo.Device;
            var chargeEventLocation = entityToMapTo.Location;

            DbStgChargeEvent2 dbStgChargeEvent2 = new()
            {
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                id = geotabIdConverter.ToGuid(entityToMapTo.Id),
                GeotabId = entityToMapTo.Id.ToString(),
                ChargeIsEstimated = (bool)entityToMapTo.ChargeIsEstimated,
                ChargeType = entityToMapTo.ChargeType.ToString(),
                DeviceId = geotabIdConverter.ToLong(device.Id),
                Duration = entityToMapTo.Duration,
                EndStateOfCharge = entityToMapTo.EndStateOfCharge,
                EnergyConsumedKwh = entityToMapTo.EnergyConsumedKwh,
                EnergyUsedSinceLastChargeKwh = entityToMapTo.EnergyUsedSinceLastChargeKwh,
                MaxACVoltage = entityToMapTo.MaxACVoltage,
                MeasuredBatteryEnergyInKwh = entityToMapTo.MeasuredBatteryEnergyInKwh,
                MeasuredBatteryEnergyOutKwh = entityToMapTo.MeasuredBatteryEnergyOutKwh,
                MeasuredOnBoardChargerEnergyInKwh = entityToMapTo.MeasuredOnBoardChargerEnergyInKwh,
                MeasuredOnBoardChargerEnergyOutKwh = entityToMapTo.MeasuredOnBoardChargerEnergyOutKwh,
                PeakPowerKw = entityToMapTo.PeakPowerKw,
                StartStateOfCharge = entityToMapTo.StartStateOfCharge,
                StartTime = (DateTime)entityToMapTo.StartTime,
                TripStop = entityToMapTo.TripStop,
                Version = (long)entityToMapTo.Version,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            if (chargeEventLocation != null)
            {
                dbStgChargeEvent2.Latitude = chargeEventLocation.Y;
                dbStgChargeEvent2.Longitude = chargeEventLocation.X;
            }

            return dbStgChargeEvent2;
        }
    }
}
