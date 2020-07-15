using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A utility class that creates and provides <see cref="DbConnection"/> objects using the information supplied in a <see cref="ConnectionInfo"/> object. 
    /// </summary>
    public class ConnectionProvider
    {
        DbConnection connection;
        readonly string connectionString;
        readonly DbProviderFactory factory;

        /// <summary>
        /// Creates a new <see cref="ConnectionProvider"/> instance.
        /// </summary>
        /// <param name="connectionInfo">The <see cref="ConnectionInfo"/> containing database connection information to be used for instantiating an internal <see cref="DbProviderFactory"/> that will be used for creating <see cref="DbConnection"/> instances.</param>
        public ConnectionProvider(ConnectionInfo connectionInfo)
        {
            try
            {
                connectionString = connectionInfo.ConnectionString;
                factory = DbProviderFactories.GetFactory(connectionInfo.ConnectionProviderType);
            }
            catch (System.Exception)
            {
                throw;
            }

        }

        /// <summary>
        /// Creates, opens and returns a new <see cref="DbConnection"/>.
        /// </summary>
        /// <returns></returns>
        public async Task<DbConnection> GetOpenConnectionAsync()
        {
            connection = factory.CreateConnection();
            connection.ConnectionString = this.connectionString;
            try
            {
                await connection.OpenAsync();
            }
            catch(Exception e)
            {
                throw new DatabaseConnectionException("An exception occurred while attempting to get an open database connection.", e);
            }

            return connection;
        }
    }
}
