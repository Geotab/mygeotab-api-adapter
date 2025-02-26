namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// Interface for a class that validates the adapter database to make sure that the database version matches the required database version associated with the application version.
    /// </summary>
    public interface IDatabaseValidator
    {
        /// <summary>
        /// Validates the adapter database version and throws an exception if the database version is found to be invalid.
        /// </summary>
        void ValidateDatabaseVersion();
    }
}
