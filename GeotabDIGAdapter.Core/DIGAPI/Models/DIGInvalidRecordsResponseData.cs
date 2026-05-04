using System.Text.Json.Serialization;

namespace MyGeotabAPIAdapter.DIGAPI.Models
{
    /// <summary>
    /// Represents the data payload of a DIG API /invalidrecords response.
    /// </summary>
    public class DIGInvalidRecordsResponseData
    {
        /// <summary>
        /// The cursor value to use for the next request to retrieve additional records. Null if no more records are available.
        /// </summary>
        [JsonPropertyName("NextResultKey")]
        public int? NextResultKey { get; set; }

        /// <summary>
        /// The cursor value used for the current request.
        /// </summary>
        [JsonPropertyName("CurrentResultKey")]
        public int? CurrentResultKey { get; set; }

        /// <summary>
        /// The total number of invalid records available.
        /// </summary>
        [JsonPropertyName("TotalNumberOfInvalidRecords")]
        public int TotalNumberOfInvalidRecords { get; set; }

        /// <summary>
        /// The number of invalid records returned in this response.
        /// </summary>
        [JsonPropertyName("CurrentNumberOfInvalidRecords")]
        public int CurrentNumberOfInvalidRecords { get; set; }

        /// <summary>
        /// The list of invalid records returned in this response.
        /// </summary>
        [JsonPropertyName("InvalidRecords")]
        public List<DIGInvalidRecord> InvalidRecords { get; set; } = new();
    }
}
