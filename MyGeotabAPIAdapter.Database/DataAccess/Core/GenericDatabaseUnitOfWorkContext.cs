using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data.Common;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A generic provider of the <see cref="IDatabaseUnitOfWorkContext"/> interface. Used to wrap the "true provider" so that multiple implementations of the same interface can be distinguised and correctly retrieved when used with dependency injection. 
    /// </summary>
    public class GenericDatabaseUnitOfWorkContext<T> : IGenericDatabaseUnitOfWorkContext<T> where T : IDatabaseUnitOfWorkContext
    {
        private readonly T implementation;

        public Databases Database { get => implementation.Database; }

        public string Id { get => implementation.Id; }

        public int TimeoutSecondsForDatabaseTasks { get => implementation.TimeoutSecondsForDatabaseTasks; }

        public ConnectionInfo.DataAccessProviderType ProviderType { get => implementation.ProviderType; }

        public GenericDatabaseUnitOfWorkContext(T implementation)
        {
            this.implementation = implementation;
        }

        public UnitOfWork CreateUnitOfWork(Databases database)
        {
            return implementation.CreateUnitOfWork(database);
        }

        public DbConnection GetConnection()
        {
            return implementation.GetConnection();
        }

        public SqlConnection GetSqlConnection()
        {
            return implementation.GetSqlConnection();
        }

        public NpgsqlConnection GetNpgsqlConnection()
        {
            return implementation.GetNpgsqlConnection();
        }

        public Task<NpgsqlConnection> GetStandaloneNpgsqlConnectionAsync()
        {
            return implementation.GetStandaloneNpgsqlConnectionAsync();
        }

        public Task<SqlConnection> GetStandaloneSqlConnectionAsync()
        {
            return implementation.GetStandaloneSqlConnectionAsync();
        }

        public NpgsqlTransaction GetNpgsqlTransaction()
        {
            return implementation.GetNpgsqlTransaction();
        }

        public SqlTransaction GetSqlTransaction()
        {
            return implementation.GetSqlTransaction();
        }

        public DbTransaction GetTransaction()
        {
            return implementation.GetTransaction();
        }

        public string GetUnitOfWorkId()
        {
            return implementation.GetUnitOfWorkId();
        }
    }
}
