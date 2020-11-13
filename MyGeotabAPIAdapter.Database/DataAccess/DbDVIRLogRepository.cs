using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbDVIRLog"/> entities.
    /// </summary>
    public class DbDVIRLogRepository : BaseRepository<DbDVIRLog>
    {
        /// <summary>
        /// Retrieves a <see cref="DbDVIRLog"/> with the specified <see cref="DbDVIRLog.GeotabId"/>. Throws an <see cref="Exception"/> if an entity with the specified ID cannot be found.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="id">The ID of the database record to be returned.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<DbDVIRLog> GetAsync(ConnectionInfo connectionInfo, string id, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            var record = await GetAsync(connectionInfo, new { Id = id }, commandTimeout);
            cancellationToken.ThrowIfCancellationRequested();
            if (record.Any())
            {
                return record.FirstOrDefault();
            }
            throw new Exception($"{typeof(DbDVIRLog).Name} with Id '{id}' not found.");
        }

        /// <summary>
        /// Retrieves a <see cref="DbDVIRLog"/> with the specified <see cref="DbDVIRLog.GeotabId"/>. Returns <c>null</c> if an entity with the specified ID cannot be found.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="id">The ID of the database record to be returned.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<DbDVIRLog> GetIfExistsAsync(ConnectionInfo connectionInfo, string id, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            var record = await GetAsync(connectionInfo, new { Id = id }, commandTimeout);
            cancellationToken.ThrowIfCancellationRequested();
            return record.FirstOrDefault();
        }

        /// <summary>
        /// Inserts/Updates a number of <see cref="DbDVIRLog"/>, <see cref="DbDVIRDefect"/> and <see cref="DbDVIRDefectRemark"/> entities into the database within a single transaction. Returns a string containing comma-separated counts: "[DbDVIRLog entities inserted],[DbDVIRDefect entities inserted],[DbDVIRDefect entities updated],[DbDVIRDefectRemark entities inserted]"
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbDVIRLogsToInsert">A list of <see cref="DbDVIRLog"/> entities to be inserted.</param>
        /// <param name="dbDVIRDefectsToInsert">A list of <see cref="DbDVIRDefect"/> entities to be inserted.</param>
        /// <param name="dbDVIRDefectsToUpdate">A list of <see cref="DbDVIRDefect"/> entities to be updated.</param>
        /// <param name="dbDVIRDefectRemarksToInsert">A list of <see cref="DbDVIRDefectRemark"/> entities to be inserted.</param>
        /// <param name="dbConfigFeedVersion">The <see cref="DbConfigFeedVersion"/> containing feed version information for the subject batch of records.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public async Task<string> PersistAllDVIRLogChangesToDatabase(ConnectionInfo connectionInfo, List<DbDVIRLog> dbDVIRLogsToInsert, List<DbDVIRDefect> dbDVIRDefectsToInsert, List<DbDVIRDefect> dbDVIRDefectsToUpdate, List<DbDVIRDefectRemark> dbDVIRDefectRemarksToInsert, DbConfigFeedVersion dbConfigFeedVersion, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            string rowCounts = "";
            long rowCount;
            try
            {
                using (var connection = await new ConnectionProvider(connectionInfo).GetOpenConnectionAsync())
                {
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        // Insert DbDVIRLogs.
                        rowCount = 0;
                        foreach (var dbDVIRLog in dbDVIRLogsToInsert)
                        {
                            await InsertAsync(connection, transaction, dbDVIRLog, commandTimeout);
                            rowCount += 1;
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        rowCounts = rowCount.ToString();

                        // Insert DbDVIRDefects.
                        rowCount = 0;
                        foreach (var dbDVIRDefect in dbDVIRDefectsToInsert)
                        {
                            await new DbDVIRDefectRepository().InsertAsync(connection, transaction, dbDVIRDefect, commandTimeout);
                            rowCount += 1;
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        rowCounts = $"{rowCounts},{rowCount}";

                        // Update DbDVIRDefects.
                        rowCount = 0;
                        foreach (var dbDVIRDefect in dbDVIRDefectsToUpdate)
                        {
                            await new DbDVIRDefectRepository().UpdateAsync(connection, transaction, dbDVIRDefect, commandTimeout);
                            rowCount += 1;
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        rowCounts = $"{rowCounts},{rowCount}";

                        // Insert DbDVIRDefectRemarks.
                        rowCount = 0;
                        foreach (var dbDVIRDefectRemark in dbDVIRDefectRemarksToInsert)
                        {
                            await new DbDVIRDefectRemarkRepository().InsertAsync(connection, transaction, dbDVIRDefectRemark, commandTimeout);
                            rowCount += 1;
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        rowCounts = $"{rowCounts},{rowCount}";

                        // Update DbConfigFeedVersion.
                        await new DbConfigFeedVersionRepository().UpdateAsync(connection, transaction, dbConfigFeedVersion, commandTimeout);

                        cancellationToken.ThrowIfCancellationRequested();
                        await transaction.CommitAsync();
                    }
                    return rowCounts;
                }
            }
            catch (Exception exception)
            {
                throw new DatabaseConnectionException($"Exception encountered while attempting database operation.", exception);
            }
        }
    }
}
