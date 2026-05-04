namespace MyGeotabAPIAdapter.Configuration
{
    /// <summary>
    /// Interface for database connectivity configuration. 
    /// Minimal interface used by database infrastructure components.
    /// </summary>
    public interface IDatabaseConfiguration
    {
        /// <summary>
        /// The connection string used to access the database.
        /// </summary>
        string DatabaseConnectionString { get; }
        /// <summary>
        /// A string representation of the <see cref="MyGeotabAPIAdapter.Database.ConnectionInfo.DataAccessProviderType"/> to be used when creating <see cref="System.Data.IDbConnection"/> instances for the database.
        /// </summary>
        string DatabaseProviderType { get; }
        /// <summary>
        /// The maximum number of seconds that a database <see cref="System.Threading.Tasks.Task"/> or batch thereof can take to be completed before it is deemed that there is a database connectivity issue and a <see cref="Database.DatabaseConnectionException"/> will be thrown.
        /// </summary>
        int TimeoutSecondsForDatabaseTasks { get; }
    }
}
