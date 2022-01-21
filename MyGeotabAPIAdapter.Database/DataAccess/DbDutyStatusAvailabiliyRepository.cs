using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbDutyStatusAvailability"/> entities.
    /// </summary>
    public class DbDutyStatusAvailabilityRepository : BaseRepository<DbDutyStatusAvailability>
    {
        /// <summary>
        /// Deletes a number of <see cref="DbDutyStatusAvailability"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbDutyStatusAvailabilitys">A list of <see cref="DbDutyStatusAvailability"/> entities to be deleted.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<long> DeleteAsync(ConnectionInfo connectionInfo, IEnumerable<DbDutyStatusAvailability> dbDutyStatusAvailabilitys, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            long deletedRowsCount = 0;
            try
            {
                using (var connection = await new ConnectionProvider(connectionInfo).GetOpenConnectionAsync())
                {
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        foreach (var dbDutyStatusAvailability in dbDutyStatusAvailabilitys)
                        {
                            await DeleteAsync(connection, transaction, dbDutyStatusAvailability, commandTimeout);
                            deletedRowsCount += 1;
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        await transaction.CommitAsync();
                    }
                    return deletedRowsCount;
                }
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }

        /// <summary>
        /// Retrieves a <see cref="DbDutyStatusAvailability"/> with the specified <see cref="DbDutyStatusAvailability.GeotabId"/>. Throws an <see cref="Exception"/> if an entity with the specified ID cannot be found.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="id">The ID of the database record to be returned.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<DbDutyStatusAvailability> GetAsync(ConnectionInfo connectionInfo, string id, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            var record = await GetAsync(connectionInfo, new { Id = id }, commandTimeout);
            cancellationToken.ThrowIfCancellationRequested();
            if (record.Any())
            {
                return record.FirstOrDefault();
            }
            throw new Exception($"{typeof(DbDutyStatusAvailability).Name} with Id '{id}' not found.");
        }

        /// <summary>
        /// Inserts a number of <see cref="DbDutyStatusAvailability"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbDutyStatusAvailabilityEntities">A list of <see cref="DbDutyStatusAvailability"/> entities to be inserted.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<long> InsertAsync(ConnectionInfo connectionInfo, IEnumerable<DbDutyStatusAvailability> dbDutyStatusAvailabilityEntities, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            long insertedRowsCount = 0;
            try
            {
                using (var connection = await new ConnectionProvider(connectionInfo).GetOpenConnectionAsync())
                {
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        foreach (var dbDutyStatusAvailability in dbDutyStatusAvailabilityEntities)
                        {
                            await InsertAsync(connection, transaction, dbDutyStatusAvailability, commandTimeout);
                            insertedRowsCount += 1;
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        cancellationToken.ThrowIfCancellationRequested();
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
        /// Updates a number of <see cref="DbDutyStatusAvailability"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbDutyStatusAvailabilityEntities">A list of <see cref="DbDutyStatusAvailability"/> entities to be updated.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<long> UpdateAsync(ConnectionInfo connectionInfo, IEnumerable<DbDutyStatusAvailability> dbDutyStatusAvailabilityEntities, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            long updatedRowsCount = 0;
            try
            {
                using (var connection = await new ConnectionProvider(connectionInfo).GetOpenConnectionAsync())
                {
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        foreach (var dbDutyStatusAvailability in dbDutyStatusAvailabilityEntities)
                        {
                            await UpdateAsync(connection, transaction, dbDutyStatusAvailability, commandTimeout);
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
