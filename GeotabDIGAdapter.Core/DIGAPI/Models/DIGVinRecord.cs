using System.Text.Json.Serialization;

namespace MyGeotabAPIAdapter.DIGAPI.Models
{
    /// <summary>
    /// Represents a VIN record for the DIG API. Contains vehicle identification number data.
    /// </summary>
    public class DIGVinRecord : DIGBaseRecord
    {
        /// <inheritdoc/>
        [JsonPropertyName("Type")]
        public override string Type => "VinRecord";

        /// <summary>
        /// The vehicle identification number.
        /// </summary>
        [JsonPropertyName("VehicleIdentificationNumber")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? VehicleIdentificationNumber { get; set; }
    }
}
