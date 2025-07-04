using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("stg_DutyStatusAvailabilities2")]
    public class DbStgDutyStatusAvailability2 : IDbEntity, IIdCacheableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "stg_DutyStatusAvailabilities2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => RecordLastChangedUtc; }

        /// <summary>
        /// The GeotabId of the <see cref="DriverId"/> rather than that of the DutyStatusAvailability entity since there is one record per Driver in the database table and the GeotabId of the actual DutyStatusAvailability entity offers no real value.
        /// </summary>
        public string GeotabId { get; set; }

        [ExplicitKey]
        public long id { get; set; }
        public long DriverId { get; set; }
        public string CycleAvailabilities { get; set; }

        [Write(false)]
        public TimeSpan? CycleDriving
        {
            get { return TimeSpan.FromTicks(CycleDrivingTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    CycleDrivingTicks = value.Value.Ticks;
                }
            }
        }
        public long? CycleDrivingTicks { get; set; }

        [Write(false)]
        public TimeSpan? Cycle
        {
            get { return TimeSpan.FromTicks(CycleTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    CycleTicks = value.Value.Ticks;
                }
            }
        }
        public long? CycleTicks { get; set; }

        [Write(false)]
        public TimeSpan? CycleRest
        {
            get { return TimeSpan.FromTicks(CycleRestTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    CycleRestTicks = value.Value.Ticks;
                }
            }
        }
        public long? CycleRestTicks { get; set; }

        [Write(false)]
        public TimeSpan? DrivingBreakDuration
        {
            get { return TimeSpan.FromTicks(DrivingBreakDurationTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    DrivingBreakDurationTicks = value.Value.Ticks;
                }
            }
        }
        public long? DrivingBreakDurationTicks { get; set; }

        [Write(false)]
        public TimeSpan? Driving
        {
            get { return TimeSpan.FromTicks(DrivingTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    DrivingTicks = value.Value.Ticks;
                }
            }
        }
        public long? DrivingTicks { get; set; }

        [Write(false)]
        public TimeSpan? Duty
        {
            get { return TimeSpan.FromTicks(DutyTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    DutyTicks = value.Value.Ticks;
                }
            }
        }
        public long? DutyTicks { get; set; }

        [Write(false)]
        public TimeSpan? DutySinceCycleRest
        {
            get { return TimeSpan.FromTicks(DutySinceCycleRestTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    DutySinceCycleRestTicks = value.Value.Ticks;
                }
            }
        }
        public long? DutySinceCycleRestTicks { get; set; }

        public bool? Is16HourExemptionAvailable { get; set; }
        public bool? IsAdverseDrivingApplied { get; set; }
        public bool? IsAdverseDrivingExemptionAvailable { get; set; }
        public bool? IsOffDutyDeferralExemptionAvailable { get; set; }
        public bool? IsRailroadExemptionAvailable { get; set; }
        public string Recap { get; set; }

        [Write(false)]
        public TimeSpan? Rest
        {
            get { return TimeSpan.FromTicks(RestTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    RestTicks = value.Value.Ticks;
                }
            }
        }
        public long? RestTicks { get; set; }

        [Write(false)]
        public TimeSpan? Workday
        {
            get { return TimeSpan.FromTicks(WorkdayTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    WorkdayTicks = value.Value.Ticks;
                }
            }
        }
        public long? WorkdayTicks { get; set; }

        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
