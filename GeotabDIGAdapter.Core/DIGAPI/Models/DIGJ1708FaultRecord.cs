using System.Text.Json.Serialization;

namespace MyGeotabAPIAdapter.DIGAPI.Models
{
    /// <summary>
    /// Represents a J1708 fault record for the DIG API. Contains J1708 diagnostic trouble code data.
    /// </summary>
    public class DIGJ1708FaultRecord : DIGBaseRecord
    {
        /// <inheritdoc/>
        [JsonPropertyName("Type")]
        public override string Type => "J1708FaultRecord";

        /// <summary>
        /// The message identifier (0-255).
        /// </summary>
        [JsonPropertyName("MessageId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MessageId { get; set; }

        /// <summary>
        /// The parameter identifier (0-511, nullable).
        /// </summary>
        [JsonPropertyName("ParameterId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ParameterId { get; set; }

        /// <summary>
        /// The subsystem identifier (0-511, nullable).
        /// </summary>
        [JsonPropertyName("SubsystemId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SubsystemId { get; set; }

        /// <summary>
        /// The failure mode identifier (0-15).
        /// </summary>
        [JsonPropertyName("FailureModeIdentifier")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? FailureModeIdentifier { get; set; }

        /// <summary>
        /// The occurrence count (0-255).
        /// </summary>
        [JsonPropertyName("OccurrenceCount")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? OccurrenceCount { get; set; }

        /// <summary>
        /// Indicates whether the fault state is active.
        /// </summary>
        [JsonPropertyName("FaultStateActive")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? FaultStateActive { get; set; }
    }
}
