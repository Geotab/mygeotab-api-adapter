using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("ExceptionEvents")]
    public class DbExceptionEvent
    {
        // The unique identifier for the specific Entity object in the Geotab system. See Id.
        [ExplicitKey]
        public string Id { get; set; }
        // The start date of the ExceptionEvent; at or after this date.
        public DateTime? ActiveFrom { get; set; }
        // The end date of the ExceptionEvent; at or before this date.
        public DateTime? ActiveTo { get; set; }
        // Device reference if it exists
        public string DeviceId { get; set; }
        // The distance travelled since the start of the ExceptionEvent.
        public float? Distance { get; set; }
        // Driver reference if it exists
        public string DriverId { get; set; }
        // The duration of the exception event.
        public TimeSpan? Duration { get; set; }
        // The exception Rule which was broken.
        public string RuleId { get; set; }
        // The version of the entity.
        public long? Version { get; set; }
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
