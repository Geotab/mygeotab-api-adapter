using System;

namespace MyGeotabAPIAdapter.Exceptions
{
    /// <summary>
    /// A custom <see cref="Exception"/> to be raised when there is a problem connecting to the optimizer database.
    /// </summary>
    public class OptimizerDatabaseConnectionException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="OptimizerDatabaseConnectionException"/>.
        /// </summary>
        /// <param name="message">The error messaege.</param>
        public OptimizerDatabaseConnectionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new <see cref="OptimizerDatabaseConnectionException"/>.
        /// </summary>
        /// <param name="message">The error messaege.</param>
        /// <param name="inner">The inner <see cref="Exception"/>.</param>
        public OptimizerDatabaseConnectionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
