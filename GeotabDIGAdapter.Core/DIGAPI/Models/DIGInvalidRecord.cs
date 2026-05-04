using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyGeotabAPIAdapter.DIGAPI.Models
{
    /// <summary>
    /// Represents an invalid record returned by the DIG API /invalidrecords endpoint.
    /// </summary>
    public class DIGInvalidRecord
    {
        /// <summary>
        /// The original base record that was rejected. Stored as JsonElement because it can be any of the 12+ record types.
        /// </summary>
        [JsonPropertyName("BaseRecord")]
        public JsonElement BaseRecord { get; set; }

        /// <summary>
        /// The cause/reason why the record was marked as invalid.
        /// </summary>
        [JsonPropertyName("Cause")]
        public string Cause { get; set; } = string.Empty;

        /// <summary>
        /// The timestamp when the record was marked as invalid.
        /// </summary>
        [JsonPropertyName("TimeStamp")]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// The user ID associated with the invalid record.
        /// </summary>
        [JsonPropertyName("UserId")]
        public string UserId { get; set; } = string.Empty;
    }
}
