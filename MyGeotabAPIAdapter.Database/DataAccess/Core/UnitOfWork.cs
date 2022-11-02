using Microsoft.Data.SqlClient;
using NLog;
using System;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A class designed to wrap transactions associated with database CRUD operations.
    /// </summary>
    public class UnitOfWork : IDisposable
    {
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        SqlConnection sqlConnection;

        /// <summary>
        /// The <see cref="DbConnection"/> associated with the <see cref="UnitOfWork"/> instance.
        /// </summary>
        public DbConnection Connection { get; }

        /// <summary>
        /// A unique identifier assigned to the <see cref="UnitOfWork"/> at the time of its creation. Intended for debugging purposes.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Indicates whether the <see cref="UnitOfWork"/> instance has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; } = false;

        /// <summary>
        /// The <see cref="Microsoft.Data.SqlClient.SqlConnection"/> associated with the <see cref="UnitOfWork"/> instance. Intended for use with SQL Server bulk operations.
        /// </summary>
        public SqlConnection SqlConnection 
        { 
            get => sqlConnection;
            internal set 
            {
                if (sqlConnection != null)
                {
                    throw new InvalidOperationException("Cannot set the SqlConnection because it has already been set.");
                }
                sqlConnection = value;
                SqlTransaction = sqlConnection.BeginTransaction();
            }
        }

        /// <summary>
        /// The <see cref="Microsoft.Data.SqlClient.SqlTransaction"/> associated with the <see cref="UnitOfWork"/> instance. Intended for use with SQL Server bulk operations.
        /// </summary>
        public SqlTransaction SqlTransaction { get; private set; }

        /// <summary>
        /// The maximum number of seconds that a database <see cref="System.Threading.Tasks.Task"/> or batch thereof can take to be completed before it is deemed that there is a database connectivity issue and a <see cref="Database.DatabaseConnectionException"/> should be thrown.
        /// </summary>
        public int TimeoutSecondsForDatabaseTasks { get; }

        /// <summary>
        /// The <see cref="DbTransaction"/> associated with the <see cref="UnitOfWork"/> instance.
        /// </summary>
        public DbTransaction Transaction { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWork"/> class and starts a database transaction on the <paramref name="connection"/>. Should be called by the <see cref="IUnitOfWorkContext.CreateUnitOfWork(Databases, int, DatabaseConnectionType)"/> method.
        /// </summary>
        /// <param name="connection">An open <see cref="DbConnection"/> to be associated with the <see cref="UnitOfWork"/> instance.</param>
        /// <param name="timeoutSecondsForDatabaseTasks">The maximum number of seconds that a database <see cref="System.Threading.Tasks.Task"/> or batch thereof can take to be completed before it is deemed that there is a database connectivity issue and a <see cref="Database.DatabaseConnectionException"/> should be thrown.</param>
        public UnitOfWork(DbConnection connection, int timeoutSecondsForDatabaseTasks)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            Id = System.Guid.NewGuid().ToString();
            Connection = connection;
            TimeoutSecondsForDatabaseTasks = timeoutSecondsForDatabaseTasks;
            Transaction = Connection.BeginTransaction();
            
            logger.Trace($"{nameof(UnitOfWork)} [Id: {Id}] created and database transaction initiated.");
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Disposes the current <see cref="UnitOfWork"/> instance.
        /// </summary>
        public void Dispose()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the <see cref="Transaction"/> associated with the current <see cref="UnitOfWork"/> instance.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Transaction != null)
                {
                    Transaction.Dispose();
                    logger.Trace($"{nameof(UnitOfWork)} [Id: {Id}] database transaction disposed.");
                }

                if (Connection != null && Connection.State == System.Data.ConnectionState.Open)
                {
                    Connection.Close();
                    Connection.Dispose();
                }

                if (SqlTransaction != null)
                { 
                    SqlTransaction.Dispose();
                    logger.Trace($"{nameof(UnitOfWork)} [Id: {Id}] SQL Server database transaction disposed.");
                }

                if (SqlConnection != null && SqlConnection.State == System.Data.ConnectionState.Open)
                { 
                    SqlConnection.Close();
                    SqlConnection.Dispose();
                }

                var id = Id;
                Id = string.Empty;
                IsDisposed = true;
                logger.Trace($"{nameof(UnitOfWork)} [Id: {id}] disposed.");
            }
        }

        /// <summary>
        /// Asynchronously commits the <see cref="Transaction"/>.
        /// </summary>
        /// <returns></returns>
        public async Task CommitAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (Transaction != null)
            {
                await Transaction.CommitAsync();
                logger.Debug($"{nameof(UnitOfWork)} [Id: {Id}] database transaction committed.");
            }

            if (SqlTransaction != null)
            { 
                await SqlTransaction.CommitAsync();
                logger.Debug($"{nameof(UnitOfWork)} [Id: {Id}] SQL Server database transaction committed.");
            }
            
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Rolls back the <see cref="Transaction"/> from a pending state.
        /// </summary>
        /// <returns></returns>
        public async Task RollBackAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (Transaction != null)
            {
                await Transaction.RollbackAsync();
                logger.Debug($"{nameof(UnitOfWork)} [Id: {Id}] database transaction rolled back.");
            }

            if (SqlTransaction != null)
            {
                await SqlTransaction.RollbackAsync();
                logger.Debug($"{nameof(UnitOfWork)} [Id: {Id}] SQL Server database transaction rolled back.");
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
