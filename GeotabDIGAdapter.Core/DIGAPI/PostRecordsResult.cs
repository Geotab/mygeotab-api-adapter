using MyGeotabAPIAdapter.Exceptions;

namespace MyGeotabAPIAdapter.DIGAPI
{
    /// <summary>
    /// Represents the result of a call to the DIG API /records endpoint.
    /// </summary>
    public class PostRecordsResult
    {
        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// The unique identifier returned by the DIG API for tracking the submitted records.
        /// </summary>
        public Guid? TrackingId { get; set; }

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