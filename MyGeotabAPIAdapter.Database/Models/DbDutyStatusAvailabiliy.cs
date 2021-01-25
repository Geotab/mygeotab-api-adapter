using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("DutyStatusAvailabilities")]
    public class DbDutyStatusAvailability
    {
        [Key]
        public long id { get; set; }
        public string DriverId { get; set; }
        public string CycleAvailabilities { get; set; }
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
        public bool? IsAdverseDrivingExemptionAvailable { get; set; }
        public bool? IsOffDutyDeferralExemptionAvailable { get; set; }
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
        public DateTime RecordLastChangedUtc { get; set; }
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }
    }
}
