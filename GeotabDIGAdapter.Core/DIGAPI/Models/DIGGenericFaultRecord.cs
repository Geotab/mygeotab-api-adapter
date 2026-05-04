using System.Text.Json.Serialization;

namespace MyGeotabAPIAdapter.DIGAPI.Models
{
    /// <summary>
    /// Represents a generic fault record for the DIG API. Contains generic diagnostic fault data.
    /// </summary>
    public class DIGGenericFaultRecord : DIGBaseRecord
    {
        /// <inheritdoc/>
        [JsonPropertyName("Type")]
        public override string Type => "GenericFaultRecord";

        /// <summary>
        /// The fault code (128-1999).
        /// </summary>
        [JsonPropertyName("Code")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Code { get; set; }

        /// <summary>
        /// Indicates whether the fault state is active.
        /// </summary>
        [JsonPropertyName("FaultStateActive")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? FaultStateActive { get; set; }
    }
}
