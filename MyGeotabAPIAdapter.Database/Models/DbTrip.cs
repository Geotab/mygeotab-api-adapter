using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("Trips")]
    public class DbTrip
    {
        [Key]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public float? AfterHoursDistance { get; set; }
        [Write(false)]
        public TimeSpan? AfterHoursDrivingDuration
        {
            get { return TimeSpan.FromTicks(AfterHoursDrivingDurationTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    AfterHoursDrivingDurationTicks = value.Value.Ticks;
                }
            }
        }
        public long? AfterHoursDrivingDurationTicks { get; set; }
        public bool? AfterHoursEnd { get; set; }
        public bool? AfterHoursStart { get; set; }
        [Write(false)]
        public TimeSpan? AfterHoursStopDuration
        {
            get { return TimeSpan.FromTicks(AfterHoursStopDurationTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    AfterHoursStopDurationTicks = value.Value.Ticks;
                }
            }
        }
        public long? AfterHoursStopDurationTicks { get; set; }
        public float? AverageSpeed { get; set; }
        public string DeviceId { get; set; }
        public float? Distance { get; set; }
        public string DriverId { get; set; }
        [Write(false)]
        public TimeSpan? DrivingDuration
        {
            get { return TimeSpan.FromTicks(DrivingDurationTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    DrivingDurationTicks = value.Value.Ticks;
                }
            }
        }
        public long? DrivingDurationTicks { get; set; }
        [Write(false)]
        public TimeSpan? IdlingDuration
        {
            get { return TimeSpan.FromTicks(IdlingDurationTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    IdlingDurationTicks = value.Value.Ticks;
                }
            }
        }
        public long? IdlingDurationTicks { get; set; }
        public float? MaximumSpeed { get; set; }
        public DateTime? NextTripStart { get; set; }
        public int? SpeedRange1 { get; set; }
        [Write(false)]
        public TimeSpan? SpeedRange1Duration
        {
            get { return TimeSpan.FromTicks(SpeedRange1DurationTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    SpeedRange1DurationTicks = value.Value.Ticks;
                }
            }
        }
        public long? SpeedRange1DurationTicks { get; set; }
        public int? SpeedRange2 { get; set; }
        [Write(false)]
        public TimeSpan? SpeedRange2Duration
        {
            get { return TimeSpan.FromTicks(SpeedRange2DurationTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    SpeedRange2DurationTicks = value.Value.Ticks;
                }
            }
        }
        public long? SpeedRange2DurationTicks { get; set; }
        public int? SpeedRange3 { get; set; }
        [Write(false)]
        public TimeSpan? SpeedRange3Duration
        {
            get { return TimeSpan.FromTicks(SpeedRange3DurationTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    SpeedRange3DurationTicks = value.Value.Ticks;
                }
            }
        }
        public long? SpeedRange3DurationTicks { get; set; }
        public DateTime? Start { get; set; }
        [Write(false)]
        public DateTime? StartOracle { get => Start; }
        public DateTime? Stop { get; set; }
        [Write(false)]
        public TimeSpan? StopDuration
        {
            get { return TimeSpan.FromTicks(StopDurationTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    StopDurationTicks = value.Value.Ticks;
                }
            }
        }
        public long? StopDurationTicks { get; set; }
        public double? StopPointX { get; set; }
        public double? StopPointY { get; set; }
        public float? WorkDistance { get; set; }
        [Write(false)]
        public TimeSpan? WorkDrivingDuration
        {
            get { return TimeSpan.FromTicks(WorkDrivingDurationTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    WorkDrivingDurationTicks = value.Value.Ticks;
                }
            }
        }
        public long? WorkDrivingDurationTicks { get; set; }
        [Write(false)]
        public TimeSpan? WorkStopDuration
        {
            get { return TimeSpan.FromTicks(WorkStopDurationTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    WorkStopDurationTicks = value.Value.Ticks;
                }
            }
        }
        public long? WorkStopDurationTicks { get; set; }
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}

