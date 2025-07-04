using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;
using Newtonsoft.Json;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Aclass with methods involving mapping between <see cref="DutyStatusAvailability"/> and <see cref="DbStgDutyStatusAvailability2"/> entities.
    /// </summary>
    public class GeotabDutyStatusAvailabilityDbStgDutyStatusAvailability2ObjectMapper : IGeotabDutyStatusAvailabilityDbStgDutyStatusAvailability2ObjectMapper
    {
        public GeotabDutyStatusAvailabilityDbStgDutyStatusAvailability2ObjectMapper()
        {
        }

        /// <inheritdoc/>
        public DbStgDutyStatusAvailability2 CreateEntity(DutyStatusAvailability entityToMapTo, long driverId)
        {
            DbStgDutyStatusAvailability2 dbStgDutyStatusAvailability2 = new()
            {
                Cycle = entityToMapTo.Cycle,
                CycleDriving = entityToMapTo.CycleDriving,
                CycleRest = entityToMapTo.CycleRest,
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                DriverId = driverId,
                Driving = entityToMapTo.Driving,
                DrivingBreakDuration = entityToMapTo.DrivingBreakDuration,
                Duty = entityToMapTo.Duty,
                DutySinceCycleRest = entityToMapTo.DutySinceCycleRest,
                // Use the GeotabId of the Driver rather than that of the DutyStatusAvailability entity since there is one record per Driver in the database table and the GeotabId of the actual DutyStatusAvailability entity offers no real value.
                GeotabId = entityToMapTo.Driver.ToString(),
                id = driverId,
                Is16HourExemptionAvailable = entityToMapTo.Is16HourExemptionAvailable,
                IsAdverseDrivingApplied = entityToMapTo.IsAdverseDrivingApplied,
                IsAdverseDrivingExemptionAvailable = entityToMapTo.IsAdverseDrivingExemptionAvailable,
                IsOffDutyDeferralExemptionAvailable = entityToMapTo.IsOffDutyDeferralExemptionAvailable,
                IsRailroadExemptionAvailable = entityToMapTo.IsRailroadExemptionAvailable,
                Rest = entityToMapTo.Rest,
                Workday = entityToMapTo.Workday,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            if (entityToMapTo.CycleAvailabilities != null)
            {
                string cycleAvailabilities = JsonConvert.SerializeObject(entityToMapTo.CycleAvailabilities);
                dbStgDutyStatusAvailability2.CycleAvailabilities = cycleAvailabilities;
            }
            if (entityToMapTo.Recap != null)
            {
                string recap = JsonConvert.SerializeObject(entityToMapTo.Recap);
                dbStgDutyStatusAvailability2.Recap = recap;
            }
            return dbStgDutyStatusAvailability2;
        }
    }
}
