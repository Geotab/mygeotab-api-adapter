using System;

namespace MyGeotabAPIAdapter.Exceptions
{
    /// <summary>
    /// A custom <see cref="Exception"/> to be raised when there is a problem connecting to the adapter database.
    /// </summary>
    public class AdapterDatabaseConnectionException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="AdapterDatabaseConnectionException"/>.
        /// </summary>
        /// <param name="message">The error messaege.</param>
        public AdapterDatabaseConnectionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new <see cref="AdapterDatabaseConnectionException"/>.
        /// </summary>
        /// <param name="message">The error messaege.</param>
        /// <param name="inner">The inner <see cref="Exception"/>.</param>
        public AdapterDatabaseConnectionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
