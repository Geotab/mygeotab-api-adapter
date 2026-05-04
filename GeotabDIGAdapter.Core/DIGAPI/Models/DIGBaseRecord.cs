using System.Text.Json.Serialization;

namespace MyGeotabAPIAdapter.DIGAPI.Models
{
    /// <summary>
    /// Base class for all DIG API record types. Contains the common fields required by all records.
    /// </summary>
    public abstract class DIGBaseRecord
    {
        /// <summary>
        /// The type of record (e.g., "GpsRecord", "AccelerationRecord").
        /// </summary>
        [JsonPropertyName("Type")]
        public abstract string Type { get; }

        /// <summary>
        /// The date and time of the record in ISO 8601 format.
        /// </summary>
        [JsonPropertyName("DateTime")]
        public DateTime DateTime { get; set; }

        /// <summary>
        /// The serial number of the device that generated the record.
        /// </summary>
        [JsonPropertyName("SerialNo")]
        public string SerialNo { get; set; } = string.Empty;
    }
}
