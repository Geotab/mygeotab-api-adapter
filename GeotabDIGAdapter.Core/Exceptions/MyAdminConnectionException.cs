namespace MyGeotabAPIAdapter.Exceptions
{
    /// <summary>
    /// A custom <see cref="Exception"/> to be raised when there is a problem connecting to the MyAdmin API.
    /// </summary>
    public class MyAdminConnectionException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="MyAdminConnectionException"/>.
        /// </summary>
        /// <param name="message">The error messaege.</param>
        public MyAdminConnectionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new <see cref="MyAdminConnectionException"/>.
        /// </summary>
        /// <param name="message">The error messaege.</param>
        /// <param name="inner">The inner <see cref="Exception"/>.</param>
        public MyAdminConnectionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
