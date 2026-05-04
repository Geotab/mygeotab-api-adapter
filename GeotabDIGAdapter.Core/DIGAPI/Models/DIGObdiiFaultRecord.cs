using System.Text.Json.Serialization;

namespace MyGeotabAPIAdapter.DIGAPI.Models
{
    /// <summary>
    /// Represents an OBD-II fault record for the DIG API. Contains OBD-II diagnostic trouble codes.
    /// </summary>
    public class DIGObdiiFaultRecord : DIGBaseRecord
    {
        /// <inheritdoc/>
        [JsonPropertyName("Type")]
        public override string Type => "ObdiiFaultRecord";

        /// <summary>
        /// The trouble code identifier (e.g., P0420).
        /// </summary>
        [JsonPropertyName("Code")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Code { get; set; }

        /// <summary>
        /// Indicates whether the fault state is active.
        /// </summary>
        [JsonPropertyName("FaultStateActive")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? FaultStateActive { get; set; }
    }
}
