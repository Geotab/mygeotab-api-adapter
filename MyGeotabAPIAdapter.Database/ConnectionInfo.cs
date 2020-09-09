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
        String connectionString;
        
        /// <summary>
        /// Supported <see cref="DbProviderFactory"/> types.
        /// </summary>
        public enum DataAccessProviderType { PostgreSQL, SQLite, SQLServer }

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
        /// Method is private to prevent usage.
        /// </summary>
        ConnectionInfo() { }

        /// <summary>
        /// Creates a new <see cref="ConnectionInfo"/> instance.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="dataAccessProviderType">A <see cref="String"/> representation of the <see cref="DataAccessProviderType"/> to be used when creating <see cref="System.Data.IDbConnection"/> instances.</param>
        public ConnectionInfo(String connectionString, string dataAccessProviderType)
        {
            this.connectionString = connectionString;
            RegisterDbProviderFactory(dataAccessProviderType);
        }

        /// <summary>
        /// Registers the <see cref="DbProviderFactory"/> for each database that is supported by this solution. If support is to be added for additional database types, the approprite <see cref="DbProviderFactory"/> must be registered in this method.
        /// <paramref name="dataAccessProviderType"/>A <see cref="String"/> representation of the <see cref="DataAccessProviderType"/> to be used when creating <see cref="System.Data.IDbConnection"/> instances.<paramref name="dataAccessProviderType"/>
        /// </summary>
        void RegisterDbProviderFactory(string dataAccessProviderType)
        {
            DataAccessProviderType connectionProviderTypeValue;
            if (!Enum.TryParse(dataAccessProviderType, out connectionProviderTypeValue))
            {
                throw new ArgumentException($"Unsupported DataAccessProviderType ('{connectionProviderType}').");
            }

            switch (connectionProviderTypeValue)
            {
                case DataAccessProviderType.PostgreSQL:
                    connectionProviderType = "Npgsql";
                    DbProviderFactories.RegisterFactory(connectionProviderType, Npgsql.NpgsqlFactory.Instance);
                    break;
                case DataAccessProviderType.SQLite:
                    connectionProviderType = "System.Data.SQLite";
                    DbProviderFactories.RegisterFactory(connectionProviderType, System.Data.SQLite.SQLiteFactory.Instance);
                    break;
                case DataAccessProviderType.SQLServer:
                    connectionProviderType = "System.Data.SqlClient";
                    DbProviderFactories.RegisterFactory(connectionProviderType, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
                    break;
                default:
                    break;
            }
        }
    }
}
