using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using ChargeEvent = Geotab.Checkmate.ObjectModel.Charging.ChargeEvent;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="ChargeEvent"/> and <see cref="DbChargeEvent"/> entities.
    /// </summary>
    public class GeotabChargeEventDbChargeEventObjectMapper : IGeotabChargeEventDbChargeEventObjectMapper
    {
        /// <inheritdoc/>
        public List<DbChargeEvent> CreateEntities(List<ChargeEvent> entitiesToMapTo)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbChargeEvents = new List<DbChargeEvent>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbChargeEvent = CreateEntity(entity);
                dbChargeEvent.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbChargeEvents.Add(dbChargeEvent);
            }
            return dbChargeEvents;
        }

        /// <inheritdoc/>
        public DbChargeEvent CreateEntity(ChargeEvent entityToMapTo)
        {
            Device chargeEventDevice = entityToMapTo.Device;
            var chargeEventLocation = entityToMapTo.Location;
            DbChargeEvent dbChargeEvent = new()
            {
                GeotabId = entityToMapTo.Id.ToString(),
                ChargeIsEstimated = (bool)entityToMapTo.ChargeIsEstimated,
                ChargeType = entityToMapTo.ChargeType.ToString(),
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DeviceId = chargeEventDevice.Id.ToString(),
                Duration = (TimeSpan)entityToMapTo.Duration,
                StartTime = (DateTime)entityToMapTo.StartTime,
                Version = (long)entityToMapTo.Version,
                RecordCreationTimeUtc = DateTime.UtcNow
            };

            if (entityToMapTo.EndStateOfCharge != null)
            { 
                dbChargeEvent.EndStateOfCharge = (float)entityToMapTo.EndStateOfCharge;
            }
            if (entityToMapTo.EnergyConsumedKwh != null)
            {
                dbChargeEvent.EnergyConsumedKwh = (float)entityToMapTo.EnergyConsumedKwh;
            }
            if (entityToMapTo.EnergyUsedSinceLastChargeKwh != null)
            {
                dbChargeEvent.EnergyUsedSinceLastChargeKwh = (float)entityToMapTo.EnergyUsedSinceLastChargeKwh;
            }
            if (chargeEventLocation != null)
            { 
                dbChargeEvent.Latitude = (float)chargeEventLocation.Y;
                dbChargeEvent.Longitude = (float)chargeEventLocation.X;
            }
            if (entityToMapTo.MaxACVoltage != null)
            {
                dbChargeEvent.MaxACVoltage = (float)entityToMapTo.MaxACVoltage;
            }
            if (entityToMapTo.MeasuredBatteryEnergyInKwh != null)
            {
                dbChargeEvent.MeasuredBatteryEnergyInKwh = (float)entityToMapTo.MeasuredBatteryEnergyInKwh;
            }
            if (entityToMapTo.MeasuredBatteryEnergyOutKwh != null)
            {
                dbChargeEvent.MeasuredBatteryEnergyOutKwh = (float)entityToMapTo.MeasuredBatteryEnergyOutKwh;
            }
            if (entityToMapTo.MeasuredOnBoardChargerEnergyInKwh != null)
            {
                dbChargeEvent.MeasuredOnBoardChargerEnergyInKwh = (float)entityToMapTo.MeasuredOnBoardChargerEnergyInKwh;
            }
            if (entityToMapTo.MeasuredOnBoardChargerEnergyOutKwh != null)
            {
                dbChargeEvent.MeasuredOnBoardChargerEnergyOutKwh = (float)entityToMapTo.MeasuredOnBoardChargerEnergyOutKwh;
            }
            if (entityToMapTo.PeakPowerKw != null)
            {
                dbChargeEvent.PeakPowerKw = (float)entityToMapTo.PeakPowerKw;
            }
            if (entityToMapTo.StartStateOfCharge != null)
            {
                dbChargeEvent.StartStateOfCharge = (float)entityToMapTo.StartStateOfCharge;
            }
            if (entityToMapTo.TripStop != null)
            {
                dbChargeEvent.TripStop = (DateTime)entityToMapTo.TripStop;
            }

            return dbChargeEvent;
        }
    }
}
