﻿using System;
using System.Data.Common;
using Oracle.ManagedDataAccess.Client;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// Stores database connection information.
    /// </summary>
    public class ConnectionInfo
    {
        String connectionProviderType;
        readonly String connectionString;

        /// <summary>
        /// Supported <see cref="DbProviderFactory"/> types.
        /// </summary>
        /// public enum DataAccessProviderType { PostgreSQL, SQLite, SQLServer }
        /// add Oracle support --- public enum DataAccessProviderType { PostgreSQL, SQLite, SQLServer, Oracle }
        public enum DataAccessProviderType { PostgreSQL, SQLite, SQLServer, Oracle }

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
        /// Method is private to prevent usage.
        /// </summary>
        ConnectionInfo() { }

        /// <summary>
        /// Creates a new <see cref="ConnectionInfo"/> instance.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="dataAccessProviderType">A <see cref="String"/> representation of the <see cref="DataAccessProviderType"/> to be used when creating <see cref="System.Data.IDbConnection"/> instances.</param>
        /// <param name="database">The <see cref="Databases"/> identifier for the database associated with the subject <see cref="ConnectionInfo"/> instance.</param>
        public ConnectionInfo(String connectionString, string dataAccessProviderType, Databases database)
        {
            this.connectionString = connectionString;
            RegisterDbProviderFactory(dataAccessProviderType);
            this.Database = database;
        }

        /// <summary>
        /// Registers the <see cref="DbProviderFactory"/> for each database that is supported by this solution. If support is to be added for additional database types, the approprite <see cref="DbProviderFactory"/> must be registered in this method.
        /// <paramref name="dataAccessProviderType"/>A <see cref="String"/> representation of the <see cref="DataAccessProviderType"/> to be used when creating <see cref="System.Data.IDbConnection"/> instances.<paramref name="dataAccessProviderType"/>
        /// </summary>
        void RegisterDbProviderFactory(string dataAccessProviderType)
        {
            if (!Enum.TryParse(dataAccessProviderType, out DataAccessProviderType connectionProviderTypeValue))
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
                case DataAccessProviderType.Oracle:
                    connectionProviderType = "Oracle.ManagedDataAccess.Client";
                    DbProviderFactories.RegisterFactory(connectionProviderType, OracleClientFactory.Instance);
                    //Console.WriteLine("test 1");
                    break;
                default:
                    break;
            }
        }
    }
}
