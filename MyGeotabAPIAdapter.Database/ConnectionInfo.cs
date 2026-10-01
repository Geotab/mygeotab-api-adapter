using MyGeotabAPIAdapter.Configuration;
using System;
using System.Data.Common;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// Stores database connection information.
    /// </summary>
    public class ConnectionInfo
    {
        String connectionProviderType;
        readonly String connectionString;
        DataAccessProviderType providerType;

        /// <summary>
        /// Supported <see cref="DbProviderFactory"/> types.
        /// </summary>
        public enum DataAccessProviderType { PostgreSQL, SQLServer, NullValue }

        /// <summary>
        /// The <see cref="Databases"/> identifier for the database associated with the subject <see cref="ConnectionInfo"/> instance.
        /// </summary>
        public Databases Database { get; private set; }

        /// <summary>
        /// The <see cref="System.Data.IDbConnection"/> provider type.
        /// </summary>
        public string ConnectionProviderType
        {
            get => connectionProviderType;
        }

        /// <summary>
        /// The database connection string.
        /// </summary>
        public String ConnectionString
        {
            get => connectionString;
        }

        /// <summary>
        /// The <see cref="DataAccessProviderType"/> of the current <see cref="ConnectionInfo"/> instance.
        /// </summary>
        public DataAccessProviderType ProviderType 
        { 
            get => providerType;
            private set { providerType = value; } 
        }

        /// <summary>
        /// The maximum number of seconds that a database <see cref="System.Threading.Tasks.Task"/> or batch thereof can take to be completed before it is deemed that there is a database connectivity issue and a <see cref="Database.DatabaseConnectionException"/> should be thrown.
        /// </summary>
        public int TimeoutSecondsForDatabaseTasks { get; private set; }

        /// <summary>
        /// Method is private to prevent usage.
        /// </summary>
        ConnectionInfo() { }

        /// <summary>
        /// Creates a new <see cref="ConnectionInfo"/> instance.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="dataAccessProviderType">A <see cref="String"/> representation of the <see cref="DataAccessProviderType"/> to be used when creating <see cref="System.Data.IDbConnection"/> instances.</param>
        /// <param name="database">The <see cref="Databases"/> identifier for the database associated with the subject <see cref="ConnectionInfo"/> instance.</param>
        /// <param name="timeoutSecondsForDatabaseTasks">The maximum number of seconds that a database <see cref="System.Threading.Tasks.Task"/> or batch thereof can take to be completed before it is deemed that there is a database connectivity issue and a <see cref="Database.DatabaseConnectionException"/> should be thrown.</param>
        public ConnectionInfo(String connectionString, string dataAccessProviderType, Databases database, int timeoutSecondsForDatabaseTasks)
        {
            this.connectionString = connectionString;
            RegisterDbProviderFactory(dataAccessProviderType);
            this.Database = database;
            this.TimeoutSecondsForDatabaseTasks = timeoutSecondsForDatabaseTasks;
        }

        /// <summary>
        /// Registers the <see cref="DbProviderFactory"/> for each database that is supported by this solution. If support is to be added for additional database types, the approprite <see cref="DbProviderFactory"/> must be registered in this method.
        /// <paramref name="dataAccessProviderType"/>A <see cref="String"/> representation of the <see cref="DataAccessProviderType"/> to be used when creating <see cref="System.Data.IDbConnection"/> instances.<paramref name="dataAccessProviderType"/>
        /// </summary>
        void RegisterDbProviderFactory(string dataAccessProviderType)
        {
            if (ConfigurationEnumParser.TryParse(dataAccessProviderType, out DataAccessProviderType connectionProviderTypeValue) == false)
            {
                throw new ArgumentException(BuildUnsupportedProviderTypeMessage(dataAccessProviderType));
            }

            switch (connectionProviderTypeValue)
            {
                case DataAccessProviderType.PostgreSQL:
                    connectionProviderType = "Npgsql";
                    DbProviderFactories.RegisterFactory(connectionProviderType, Npgsql.NpgsqlFactory.Instance);
                    break;
                case DataAccessProviderType.SQLServer:
                    connectionProviderType = "System.Data.SqlClient";
                    DbProviderFactories.RegisterFactory(connectionProviderType, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
                    break;
                default:
                    // Reached by NullValue, and by any member added to the enum but not registered above. Both
                    // leave no provider factory registered and connectionProviderType null, so returning here
                    // would produce a ConnectionInfo that cannot open a connection - and the failure would
                    // surface later, somewhere else, as a different symptom. Refuse it where it is detectable.
                    throw new ArgumentException(BuildUnsupportedProviderTypeMessage(dataAccessProviderType));
            }

            providerType = connectionProviderTypeValue;
        }

        /// <summary>
        /// Builds the message reporting a <see cref="DataAccessProviderType"/> that no provider factory can be
        /// registered for. Shared by both refusal paths so that a value refused at parse time and a value refused
        /// for having no factory read identically to an operator, and name the value they were given.
        /// </summary>
        /// <param name="dataAccessProviderType">The value supplied by the caller.</param>
        /// <returns>The exception message.</returns>
        static string BuildUnsupportedProviderTypeMessage(string dataAccessProviderType)
        {
            return $"Unsupported DataAccessProviderType ('{dataAccessProviderType}'). Valid values are: {ConfigurationEnumParser.DescribeValidValues(DataAccessProviderType.NullValue)}.";
        }
    }
}
