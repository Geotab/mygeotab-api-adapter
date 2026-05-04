using System.Text.Json.Serialization;

namespace MyGeotabAPIAdapter.DIGAPI.Models
{
    /// <summary>
    /// Represents a generic status record for the DIG API. Contains generalized device telemetry.
    /// </summary>
    public class DIGGenericStatusRecord : DIGBaseRecord
    {
        /// <inheritdoc/>
        [JsonPropertyName("Type")]
        public override string Type => "GenericStatusRecord";

        /// <summary>
        /// The diagnostic code (must be &lt;=127 or &gt;=2000).
        /// </summary>
        [JsonPropertyName("Code")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Code { get; set; }

        /// <summary>
        /// The value associated with the diagnostic code.
        /// </summary>
        [JsonPropertyName("Value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Value { get; set; }
    }
}
