using System.Text.Json.Serialization;

namespace MyGeotabAPIAdapter.DIGAPI.Models
{
    /// <summary>
    /// Represents a driver change record for the DIG API. Contains operator transition information.
    /// </summary>
    public class DIGDriverChangeRecord : DIGBaseRecord
    {
        /// <inheritdoc/>
        [JsonPropertyName("Type")]
        public override string Type => "DriverChangeRecord";

        /// <summary>
        /// The key type identifier (0-255).
        /// </summary>
        [JsonPropertyName("KeyType")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? KeyType { get; set; }

        /// <summary>
        /// The driver ID (base64 encoded when serialized). Maximum 239 bytes.
        /// </summary>
        [JsonPropertyName("DriverId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public byte[]? DriverId { get; set; }
    }
}
