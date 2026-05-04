using System.Text.Json.Serialization;

namespace MyGeotabAPIAdapter.DIGAPI.Models
{
    /// <summary>
    /// Represents an acceleration record for the DIG API. Contains acceleration measurements across three axes.
    /// </summary>
    public class DIGAccelerationRecord : DIGBaseRecord
    {
        /// <inheritdoc/>
        [JsonPropertyName("Type")]
        public override string Type => "AccelerationRecord";

        /// <summary>
        /// The X-axis acceleration in milli-g (1000 milli-g = 1 G).
        /// </summary>
        [JsonPropertyName("X")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public short? X { get; set; }

        /// <summary>
        /// The Y-axis acceleration in milli-g (1000 milli-g = 1 G).
        /// </summary>
        [JsonPropertyName("Y")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public short? Y { get; set; }

        /// <summary>
        /// The Z-axis acceleration in milli-g (1000 milli-g = 1 G).
        /// </summary>
        [JsonPropertyName("Z")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public short? Z { get; set; }
    }
}
