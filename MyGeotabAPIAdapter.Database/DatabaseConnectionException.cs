using System;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// A custom <see cref="Exception"/> to be raised when there is a problem connecting to the MyGeotab API Adapter database.
    /// </summary>
    public class DatabaseConnectionException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="DatabaseConnectionException"/>.
        /// </summary>
        /// <param name="message">The error messaege.</param>
        public DatabaseConnectionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new <see cref="DatabaseConnectionException"/>.
        /// </summary>
        /// <param name="message">The error messaege.</param>
        /// <param name="inner">The inner <see cref="Exception"/>.</param>
        public DatabaseConnectionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
