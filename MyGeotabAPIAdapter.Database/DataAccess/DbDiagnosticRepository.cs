using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbDiagnostic"/> entities.
    /// </summary>
    public class DbDiagnosticRepository : BaseRepository<DbDiagnostic>
    {
        /// <summary>
        /// Retrieves a <see cref="DbDiagnostic"/> with the specified <see cref="DbDiagnostic.Id"/>. Throws an <see cref="Exception"/> if an entity with the specified ID cannot be found.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="id">The ID of the database record to be returned.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<DbDiagnostic> GetAsync(ConnectionInfo connectionInfo, string id, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            var record = await GetAsync(connectionInfo, new { Id = id }, commandTimeout);
            cancellationToken.ThrowIfCancellationRequested();
            if (record.Any())
            {
                return record.FirstOrDefault();
            }
            throw new Exception($"{typeof(DbDiagnostic).Name} with Id '{id}' not found.");
        }

        /// <summary>
        /// Inserts a number of <see cref="DbDiagnostic"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbDiagnostics">A list of <see cref="DbDiagnostic"/> entities to be inserted.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<long> InsertAsync(ConnectionInfo connectionInfo, List<DbDiagnostic> dbDiagnostics, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            long insertedRowsCount = 0;
            try
            {
                using (var connection = await new ConnectionProvider(connectionInfo).GetOpenConnectionAsync())
                {
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        foreach (var dbDiagnostic in dbDiagnostics)
                        {
                            await InsertAsync(connection, transaction, dbDiagnostic, commandTimeout);
                            insertedRowsCount += 1;
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        await transaction.CommitAsync();
                    }
                    return insertedRowsCount;
                }
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }

        /// <summary>
        /// Updates a number of <see cref="DbDiagnostic"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbDiagnostics">A list of <see cref="DbDiagnostic"/> entities to be updated.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<long> UpdateAsync(ConnectionInfo connectionInfo, List<DbDiagnostic> dbDiagnostics, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            long updatedRowsCount = 0;
            try
            {
                using (var connection = await new ConnectionProvider(connectionInfo).GetOpenConnectionAsync())
                {
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        foreach (var dbDiagnostic in dbDiagnostics)
                        {
                            await UpdateAsync(connection, transaction, dbDiagnostic, commandTimeout);
                            updatedRowsCount += 1;
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        await transaction.CommitAsync();
                    }
                    return updatedRowsCount;
                }
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }
    }
}
