using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbLogRecord"/> entities.
    /// </summary>
    public class DbLogRecordRepository : BaseRepository<DbLogRecord>
    {
        /// <summary>
        /// Inserts a number of <see cref="DbLogRecord"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbLogRecords">A list of <see cref="DbLogRecord"/> entities to be inserted.</param>
        /// <param name="dbConfigFeedVersion">The <see cref="DbConfigFeedVersion"/> containing feed version information for the subject batch of records.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<long> InsertAsync(ConnectionInfo connectionInfo, List<DbLogRecord> dbLogRecords, DbConfigFeedVersion dbConfigFeedVersion, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            long insertedRowsCount = 0;
            try
            {
                using (var connection = await new ConnectionProvider(connectionInfo).GetOpenConnectionAsync())
                {
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        foreach (var dbLogRecord in dbLogRecords)
                        {
                            await InsertAsync(connection, transaction, dbLogRecord, commandTimeout);
                            insertedRowsCount += 1;
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        // Update DbConfigFeedVersion.
                        await new DbConfigFeedVersionRepository().UpdateAsync(connection, transaction, dbConfigFeedVersion, commandTimeout);

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
    }
}
