namespace MyGeotabAPIAdapter.Exceptions
{
    /// <summary>
    /// A custom <see cref="Exception"/> to be raised when there is a problem connecting to the DIG API.
    /// </summary>
    public class DIGConnectionException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="DIGConnectionException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public DIGConnectionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new <see cref="DIGConnectionException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="inner">The inner <see cref="Exception"/>.</param>
        public DIGConnectionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}