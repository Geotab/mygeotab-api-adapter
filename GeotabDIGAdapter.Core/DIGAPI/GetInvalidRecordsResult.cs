using MyGeotabAPIAdapter.DIGAPI.Models;
using MyGeotabAPIAdapter.Exceptions;

namespace MyGeotabAPIAdapter.DIGAPI
{
    /// <summary>
    /// Represents the result of a call to the DIG API /invalidrecords endpoint.
    /// </summary>
    public class GetInvalidRecordsResult
    {
        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// The cursor value to use for the next request to retrieve additional records. Null if no more records are available.
        /// </summary>
        public int? NextResultKey { get; set; }

        /// <summary>
        /// The cursor value used for the current request.
        /// </summary>
        public int? CurrentResultKey { get; set; }

        /// <summary>
        /// The total number of invalid records available.
        /// </summary>
        public int TotalNumberOfInvalidRecords { get; set; }

        /// <summary>
        /// The number of invalid records returned in this response.
        /// </summary>
        public int CurrentNumberOfInvalidRecords { get; set; }

        /// <summary>
        /// The list of invalid records retrieved.
        /// </summary>
        public List<DIGInvalidRecord> InvalidRecords { get; set; } = new();

        /// <summary>
        /// If the operation failed, the error message indicating the reason for failure.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// The source of the error, if any.
        /// </summary>
        public ErrorSource ErrorSource { get; set; } = ErrorSource.None;

        /// <summary>
        /// The elapsed time in milliseconds for the DIG API call.
        /// </summary>
        public long ElapsedMilliseconds { get; set; }
    }
}
