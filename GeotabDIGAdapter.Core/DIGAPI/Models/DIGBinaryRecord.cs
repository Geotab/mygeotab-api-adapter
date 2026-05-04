using System.Text.Json.Serialization;

namespace MyGeotabAPIAdapter.DIGAPI.Models
{
    /// <summary>
    /// Represents a binary record for the DIG API. Carries arbitrary binary payload (max 32745 bytes).
    /// </summary>
    public class DIGBinaryRecord : DIGBaseRecord
    {
        /// <inheritdoc/>
        [JsonPropertyName("Type")]
        public override string Type => "BinaryRecord";

        /// <summary>
        /// The binary data payload (base64 encoded when serialized). Maximum 32745 bytes.
        /// </summary>
        [JsonPropertyName("Data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public byte[]? Data { get; set; }
    }
}
