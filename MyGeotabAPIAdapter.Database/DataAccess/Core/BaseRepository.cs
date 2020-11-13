using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A generic base repository class that handles database CRUD operations.
    /// </summary>
    /// <typeparam name="T">The type of entity to be used for representation of database records and for which CRUD operations are to be performed.</typeparam>
    public abstract class BaseRepository<T> : IDisposable where T : class
    {
        ConnectionProvider connectionProvider;

        /// <summary>
        /// Creates a new <see cref="BaseRepository{T}"/> instance.
        /// </summary>
        public BaseRepository()
        {
        }

        /// <summary>
        /// Disposes the current <see cref="BaseRepository{T}"/> instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose();
        }

        /// <summary>
        /// Deletes a record from a database table.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="entity">An entity representing the database record to be deleted.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(ConnectionInfo connectionInfo, T entity, int commandTimeout)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(commandTimeout));

                SetConnection(connectionInfo);
                using (var connection = await connectionProvider.GetOpenConnectionAsync())
                {
                    return await connection.DeleteAsync<T>(entity);
                }
            }
            catch (OperationCanceledException exception)
            {
                throw new DatabaseConnectionException($"Database operation did not complete within the allowed time of {commandTimeout} seconds.", exception);
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }

        /// <summary>
        /// Deletes a record from a database table as part of a <see cref="DbTransaction"/>.
        /// </summary>
        /// <param name="connection">The database connection to be used to perform the subject operation.</param>
        /// <param name="transaction">The database transaction within which to perform the subject operation.</param>
        /// <param name="entity">An entity representing the database record to be deleted.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(DbConnection connection, DbTransaction transaction, T entity, int commandTimeout)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(commandTimeout));

                return await connection.DeleteAsync<T>(entity, transaction);
            }
            catch (OperationCanceledException exception)
            {
                throw new DatabaseConnectionException($"Database operation did not complete within the allowed time of {commandTimeout} seconds.", exception);
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }


        /// <summary>
        /// Returns a collection of objects representing records in a database table that match the specified search criteria.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dynamicParams">The dynamic parameters to be used comprise the WHERE clause of the subject operation.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "<Pending>")]
        public async Task<IEnumerable<T>> GetAsync(ConnectionInfo connectionInfo, dynamic dynamicParams, int commandTimeout)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(commandTimeout));

                SetConnection(connectionInfo);
                using (var connection = await connectionProvider.GetOpenConnectionAsync())
                {
                    return await connection.GetByParamAsync<T>(dynamicParams as object);
                }
            }
            catch (OperationCanceledException exception)
            {
                throw new DatabaseConnectionException($"Database operation did not complete within the allowed time of {commandTimeout} seconds.", exception);
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }

        /// <summary>
        /// Returns a collection of objects representing records in a database table that match the specified search criteria. This overload is intended for use within a <see cref="DbTransaction"/>.
        /// </summary>
        /// <param name="connection">The database connection to be used to perform the subject operation.</param>
        /// <param name="transaction">The database transaction within which to perform the subject operation.</param>
        /// <param name="dynamicParams">The dynamic parameters to be used comprise the WHERE clause of the subject operation.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> GetAsync(DbConnection connection, DbTransaction transaction, dynamic dynamicParams, int commandTimeout)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(commandTimeout));

                return await connection.GetByParamAsync<T>(dynamicParams as object, transaction);
            }
            catch (OperationCanceledException exception)
            {
                throw new DatabaseConnectionException($"Database operation did not complete within the allowed time of {commandTimeout} seconds.", exception);
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }

        /// <summary>
        /// Returns a collection of objects representing all records in a database table.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> GetAllAsync(ConnectionInfo connectionInfo, int commandTimeout)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(commandTimeout));

                SetConnection(connectionInfo);
                using (var connection = await connectionProvider.GetOpenConnectionAsync())
                {
                    return await connection.GetAllAsync<T>();
                }
            }
            catch (OperationCanceledException exception)
            {
                throw new DatabaseConnectionException($"Database operation did not complete within the allowed time of {commandTimeout} seconds.", exception);
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }

        /// <summary>
        /// Returns a collection of objects representing all records in a database table. This overload is intended for use within a <see cref="DbTransaction"/>.
        /// </summary>
        /// <param name="connection">The database connection to be used to perform the subject operation.</param>
        /// <param name="transaction">The database transaction within which to perform the subject operation.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> GetAllAsync(DbConnection connection, DbTransaction transaction, int commandTimeout)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(commandTimeout));

                return await connection.GetAllAsync<T>(transaction);
            }
            catch (OperationCanceledException exception)
            {
                throw new DatabaseConnectionException($"Database operation did not complete within the allowed time of {commandTimeout} seconds.", exception);
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }

        /// <summary>
        /// Returns an object representing a single record in a database table.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="Id">The ID of the database record to be returned.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<T> GetAsync(ConnectionInfo connectionInfo, int Id, int commandTimeout)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(commandTimeout));

                SetConnection(connectionInfo);
                using (var connection = await connectionProvider.GetOpenConnectionAsync())
                {
                    var record = await connection.GetAsync<T>(Id);

                    if (record != null)
                    {
                        return record;
                    }
                }
                throw new Exception($"{typeof(T).Name} with ID of '{Id}' not found.");
            }
            catch (OperationCanceledException exception)
            {
                throw new DatabaseConnectionException($"Database operation did not complete within the allowed time of {commandTimeout} seconds.", exception);
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }

        /// <summary>
        /// Returns an object representing a single record in a database table. This overload is intended for use within a <see cref="DbTransaction"/>.
        /// </summary>
        /// <param name="connection">The database connection to be used to perform the subject operation.</param>
        /// <param name="transaction">The database transaction within which to perform the subject operation.</param>
        /// <param name="Id">The ID of the database record to be returned.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<T> GetAsync(DbConnection connection, DbTransaction transaction, int Id, int commandTimeout)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(commandTimeout));

                var record = await connection.GetAsync<T>(Id, transaction);

                if (record != null)
                {
                    return record;
                }
                throw new Exception($"{typeof(T).Name} with ID of '{Id}' not found.");
            }
            catch (OperationCanceledException exception)
            {
                throw new DatabaseConnectionException($"Database operation did not complete within the allowed time of {commandTimeout} seconds.", exception);
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }

        /// <summary>
        /// Inserts a record into a database table.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="entity">An entity representing the database record to be inserted.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<long> InsertAsync(ConnectionInfo connectionInfo, T entity, int commandTimeout)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(commandTimeout));

                SetConnection(connectionInfo);
                using (var connection = await connectionProvider.GetOpenConnectionAsync())
                {
                    return await connection.InsertAsync(entity);
                }
            }
            catch (OperationCanceledException exception)
            {
                throw new DatabaseConnectionException($"Database operation did not complete within the allowed time of {commandTimeout} seconds.", exception);
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }

        /// <summary>
        /// Inserts a record into a database table as part of a <see cref="DbTransaction"/>.
        /// </summary>
        /// <param name="connection">The database connection to be used to perform the subject operation.</param>
        /// <param name="transaction">The database transaction within which to perform the subject operation.</param>
        /// <param name="entity">An entity representing the database record to be inserted.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<long> InsertAsync(DbConnection connection, DbTransaction transaction, T entity, int commandTimeout)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(commandTimeout));

                return await connection.InsertAsync(entity, transaction);
            }
            catch (OperationCanceledException exception)
            {
                throw new DatabaseConnectionException($"Database operation did not complete within the allowed time of {commandTimeout} seconds.", exception);
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }

        /// <summary>
        /// Sets the <see cref="ConnectionProvider"/> to be used in working with <see cref="DbConnection"/> instances.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        public void SetConnection(ConnectionInfo connectionInfo)
        {
            connectionProvider = new ConnectionProvider(connectionInfo);
        }

        /// <summary>
        /// Updates a record in a database table.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="entity">An entity representing the database record to be updated.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(ConnectionInfo connectionInfo, T entity, int commandTimeout)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(commandTimeout));

                SetConnection(connectionInfo);
                using (var connection = await connectionProvider.GetOpenConnectionAsync())
                {
                    return await connection.UpdateAsync<T>(entity);
                }
            }
            catch (OperationCanceledException exception)
            {
                throw new DatabaseConnectionException($"Database operation did not complete within the allowed time of {commandTimeout} seconds.", exception);
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }

        /// <summary>
        /// Updates a record in a database table as part of a <see cref="DbTransaction"/>.
        /// </summary>
        /// <param name="connection">The database connection to be used to perform the subject operation.</param>
        /// <param name="transaction">The database transaction within which to perform the subject operation.</param>
        /// <param name="entity">An entity representing the database record to be updated.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(DbConnection connection, DbTransaction transaction, T entity, int commandTimeout)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(commandTimeout));

                return await connection.UpdateAsync<T>(entity, transaction);
            }
            catch (OperationCanceledException exception)
            {
                throw new DatabaseConnectionException($"Database operation did not complete within the allowed time of {commandTimeout} seconds.", exception);
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }
    }
}
