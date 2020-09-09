using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("Trips")]
    public class DbTrip
    {
        [ExplicitKey]
        public string Id { get; set; }
        public string DeviceId { get; set; }
        public string DriverId { get; set; }
        public float? Distance { get; set; }
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
        public DateTime? NextTripStart { get; set; }
        public DateTime? Start { get; set; }
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
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}

