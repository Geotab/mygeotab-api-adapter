using System.Text.Json.Serialization;

namespace MyGeotabAPIAdapter.DIGAPI.Models
{
    /// <summary>
    /// Represents a J1939 fault record for the DIG API. Contains J1939 fault code with lamp status indicators.
    /// </summary>
    public class DIGJ1939FaultRecord : DIGBaseRecord
    {
        /// <inheritdoc/>
        [JsonPropertyName("Type")]
        public override string Type => "J1939FaultRecord";

        /// <summary>
        /// The suspect parameter number.
        /// </summary>
        [JsonPropertyName("SuspectParameterNumber")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SuspectParameterNumber { get; set; }

        /// <summary>
        /// The failure mode identifier (0-255).
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
        /// The source address (0-255).
        /// </summary>
        [JsonPropertyName("SourceAddress")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SourceAddress { get; set; }

        /// <summary>
        /// Indicates whether the malfunction lamp is on.
        /// </summary>
        [JsonPropertyName("MalfunctionLamp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? MalfunctionLamp { get; set; }

        /// <summary>
        /// Indicates whether the red stop lamp is on.
        /// </summary>
        [JsonPropertyName("RedStopLamp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? RedStopLamp { get; set; }

        /// <summary>
        /// Indicates whether the amber warning lamp is on.
        /// </summary>
        [JsonPropertyName("AmberWarningLamp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? AmberWarningLamp { get; set; }

        /// <summary>
        /// Indicates whether the protect warning lamp is on.
        /// </summary>
        [JsonPropertyName("ProtectWarningLamp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? ProtectWarningLamp { get; set; }

        /// <summary>
        /// Indicates whether the fault state is active.
        /// </summary>
        [JsonPropertyName("FaultStateActive")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? FaultStateActive { get; set; }
    }
}
