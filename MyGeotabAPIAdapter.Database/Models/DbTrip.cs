using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// Trip:
    /// A vehicle is always in a trip, even when stopped. 
    /// All trips for a vehicle are connected to each other. see start & nextTripStart.
    /// DeviceId & start are unique - the identifier.
    /// </summary>
    [Table("Trips")]
    public class DbTrip
    {
        [ExplicitKey]
        public string Id { get; set; }
        // device reference
        public string DeviceId { get; set; }
        // driver reference if exists
        public string DriverId { get; set; }
        // distance travelled
        public float? Distance { get; set; }
        // total trip driving duration
        public TimeSpan? DrivingDuration { get; set; }
        // Link to the next trip record. Composite key with deviceId
        public DateTime? NextTripStart { get; set; }
        // Trip start time
        public DateTime? Start { get; set; }
        // Trip stop time
        public DateTime? Stop { get; set; }
        // Duration of this stop
        public TimeSpan? StopDuration { get; set; }
        // stopPoint X coordinate
        public double? StopPointX { get; set; }
        // stopPoint Y coordinate
        public double? StopPointY { get; set; }
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}

