using System;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A custom <see cref="Exception"/> to be raised when there is a problem connecting to the MyGeotab API.
    /// </summary>
    public class MyGeotabConnectionException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="MyGeotabConnectionException"/>.
        /// </summary>
        /// <param name="message">The error messaege.</param>
        public MyGeotabConnectionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new <see cref="MyGeotabConnectionException"/>.
        /// </summary>
        /// <param name="message">The error messaege.</param>
        /// <param name="inner">The inner <see cref="Exception"/>.</param>
        public MyGeotabConnectionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
