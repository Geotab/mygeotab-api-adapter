namespace MyGeotabAPIAdapter.DIGAPI
{
    /// <summary>
    /// Represents a generic response from the DIG API.
    /// </summary>
    /// <typeparam name="T">The type of data in the response.</typeparam>
    public class DIGAPIResponse<T>
    {
        /// <summary>
        /// List of error messages, if any.
        /// </summary>
        public List<string> Error { get; set; } = new();

        /// <summary>
        /// The response data.
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Indicates whether the response contains errors.
        /// </summary>
        public bool HasErrors => Error != null && Error.Count > 0;
    }
}