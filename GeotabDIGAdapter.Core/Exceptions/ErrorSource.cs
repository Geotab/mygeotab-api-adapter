namespace MyGeotabAPIAdapter.Exceptions
{
    /// <summary>
    /// Indicates the source of an error.
    /// </summary>
    public enum ErrorSource
    {
        /// <summary>
        /// No error occurred.
        /// </summary>
        None,

        /// <summary>
        /// The error originated in the DIG API.
        /// </summary>
        DIGAPI,

        /// <summary>
        /// The error originated in the MyAdmin API.
        /// </summary>
        MyAdminAPI,

        /// <summary>
        /// The error originated in the MyGeotab API.
        /// </summary>
        MyGeotabAPI,

        /// <summary>
        /// The error originated in the middleware.
        /// </summary>
        Middleware
    }
}
