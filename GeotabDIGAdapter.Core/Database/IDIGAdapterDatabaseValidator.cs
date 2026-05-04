namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// Interface for a class that validates the DIG Adapter database schema (gda schema).
    /// </summary>
    public interface IDIGAdapterDatabaseValidator
    {
        /// <summary>
        /// Validates that the DIG Adapter database schema version matches the required version for the application.
        /// </summary>
        void ValidateDatabaseVersion();
    }
}