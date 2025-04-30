using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("stg_ChargeEvents2")]
    public class DbStgChargeEvent2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "stg_ChargeEvents2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
        public Guid id { get; set; }
        public string GeotabId { get; set; }
        public bool ChargeIsEstimated { get; set; }
        public string ChargeType { get; set; }
        public long DeviceId { get; set; }
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
        public DateTime StartTime { get; set; }
        public DateTime? TripStop { get; set; }
        public long Version { get; set; }

        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
