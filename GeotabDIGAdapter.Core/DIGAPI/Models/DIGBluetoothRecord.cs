using System.Text.Json.Serialization;

namespace MyGeotabAPIAdapter.DIGAPI.Models
{
    /// <summary>
    /// Represents a Bluetooth record for the DIG API. Contains Bluetooth sensor telemetry data.
    /// </summary>
    public class DIGBluetoothRecord : DIGBaseRecord
    {
        /// <inheritdoc/>
        [JsonPropertyName("Type")]
        public override string Type => "BluetoothRecord";

        /// <summary>
        /// The Bluetooth MAC address (e.g., FF:FF:FF:00:AA:9B).
        /// </summary>
        [JsonPropertyName("Address")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Address { get; set; }

        /// <summary>
        /// The sensor data value.
        /// </summary>
        [JsonPropertyName("Data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? Data { get; set; }

        /// <summary>
        /// The data type identifier (0-255).
        /// </summary>
        [JsonPropertyName("DataType")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? DataType { get; set; }
    }
}
