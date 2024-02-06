using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("ChargeEvents")]
    public class DbChargeEvent : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "ChargeEvents";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
#pragma warning disable IDE1006 // Naming Styles
        public long id { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        public string GeotabId { get; set; }
        public bool ChargeIsEstimated { get; set; }
        public string ChargeType { get; set; }
        public DateTime StartTime { get; set; }
        public string DeviceId { get; set; }
        [Write(false)]
        public TimeSpan? Duration
        {
            get { return TimeSpan.FromTicks(DurationTicks); }
            set
            {
                if (value.HasValue)
                {
                    DurationTicks = value.Value.Ticks;
                }
            }
        }
        public long DurationTicks { get; set; }
        public double? EndStateOfCharge { get; set; }
        public double? EnergyConsumedKwh { get; set; }
        public double? EnergyUsedSinceLastChargeKwh { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? MaxACVoltage { get; set; }
        public double? MeasuredBatteryEnergyInKwh { get; set; }
        public double? MeasuredBatteryEnergyOutKwh { get; set; }
        public double? MeasuredOnBoardChargerEnergyInKwh { get; set; }
        public double? MeasuredOnBoardChargerEnergyOutKwh { get; set; }
        public double? PeakPowerKw { get; set; }
        public double? StartStateOfCharge { get; set; }
        public DateTime? TripStop { get; set; }
        public long Version { get; set; }

        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
