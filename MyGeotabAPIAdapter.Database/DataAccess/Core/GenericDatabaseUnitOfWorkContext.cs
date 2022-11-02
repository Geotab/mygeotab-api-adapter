using Microsoft.Data.SqlClient;
using System.Data.Common;

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
