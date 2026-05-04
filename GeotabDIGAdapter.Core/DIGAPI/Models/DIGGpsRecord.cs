using System.Text.Json.Serialization;

namespace MyGeotabAPIAdapter.DIGAPI.Models
{
    /// <summary>
    /// Represents a GPS record for the DIG API. Contains location and speed data with auxiliary flags.
    /// </summary>
    public class DIGGpsRecord : DIGBaseRecord
    {
        /// <inheritdoc/>
        [JsonPropertyName("Type")]
        public override string Type => "GpsRecord";

        /// <summary>
        /// The latitude coordinate.
        /// </summary>
        [JsonPropertyName("Latitude")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? Latitude { get; set; }

        /// <summary>
        /// The longitude coordinate.
        /// </summary>
        [JsonPropertyName("Longitude")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? Longitude { get; set; }

        /// <summary>
        /// The speed in km/h.
        /// </summary>
        [JsonPropertyName("Speed")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? Speed { get; set; }

        /// <summary>
        /// Indicates whether the GPS data is valid.
        /// </summary>
        [JsonPropertyName("IsGpsValid")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsGpsValid { get; set; }

        /// <summary>
        /// Indicates whether the ignition is on.
        /// </summary>
        [JsonPropertyName("IsIgnitionOn")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsIgnitionOn { get; set; }

        /// <summary>
        /// Indicates whether auxiliary input 1 is on.
        /// </summary>
        [JsonPropertyName("IsAuxiliary1On")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsAuxiliary1On { get; set; }

        /// <summary>
        /// Indicates whether auxiliary input 2 is on.
        /// </summary>
        [JsonPropertyName("IsAuxiliary2On")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsAuxiliary2On { get; set; }

        /// <summary>
        /// Indicates whether auxiliary input 3 is on.
        /// </summary>
        [JsonPropertyName("IsAuxiliary3On")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsAuxiliary3On { get; set; }

        /// <summary>
        /// Indicates whether auxiliary input 4 is on.
        /// </summary>
        [JsonPropertyName("IsAuxiliary4On")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsAuxiliary4On { get; set; }

        /// <summary>
        /// Indicates whether auxiliary input 5 is on.
        /// </summary>
        [JsonPropertyName("IsAuxiliary5On")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsAuxiliary5On { get; set; }

        /// <summary>
        /// Indicates whether auxiliary input 6 is on.
        /// </summary>
        [JsonPropertyName("IsAuxiliary6On")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsAuxiliary6On { get; set; }

        /// <summary>
        /// Indicates whether auxiliary input 7 is on.
        /// </summary>
        [JsonPropertyName("IsAuxiliary7On")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsAuxiliary7On { get; set; }

        /// <summary>
        /// Indicates whether auxiliary input 8 is on.
        /// </summary>
        [JsonPropertyName("IsAuxiliary8On")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsAuxiliary8On { get; set; }
    }
}
