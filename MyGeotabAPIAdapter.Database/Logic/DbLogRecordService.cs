using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    /// <summary>
    /// A service class that handles database CRUD operations for <see cref="DbLogRecord"/> entities.
    /// </summary>
    public static class DbLogRecordService
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
        public static async Task<long> InsertAsync(ConnectionInfo connectionInfo, List<DbLogRecord> dbLogRecords, DbConfigFeedVersion dbConfigFeedVersion, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbLogRecordRepository().InsertAsync(connectionInfo, dbLogRecords, dbConfigFeedVersion, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Inserts a number of <see cref="DbLogRecord"/> entities into the database within an existing transaction.
        /// </summary>
        /// <param name="connection">The database connection to be used to perform the inserts.</param>
        /// <param name="transaction">The database transaction within which to perform the inserts.</param>
        /// <param name="dbLogRecords">A list of <see cref="DbLogRecord"/> entities to be inserted.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertAsync(DbConnection connection, DbTransaction transaction, List<DbLogRecord> dbLogRecords, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbLogRecordRepository().InsertAsync(connection, transaction, dbLogRecords, cancellationTokenSource, commandTimeout);
        }
    }
}
