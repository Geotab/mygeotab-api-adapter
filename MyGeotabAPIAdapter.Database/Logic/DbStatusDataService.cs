using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    /// <summary>
    /// A service class that handles database CRUD operations for <see cref="DbStatusData"/> entities.
    /// </summary>
    public static class DbStatusDataService
    {
        /// <summary>
        /// Inserts a number of <see cref="DbStatusData"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbStatusDatas">A list of <see cref="DbStatusData"/> entities to be inserted.</param>
        /// <param name="dbConfigFeedVersion">The <see cref="DbConfigFeedVersion"/> containing feed version information for the subject batch of records.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertAsync(ConnectionInfo connectionInfo, List<DbStatusData> dbStatusDatas, DbConfigFeedVersion dbConfigFeedVersion, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbStatusDataRepository().InsertAsync(connectionInfo, dbStatusDatas, dbConfigFeedVersion, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Inserts a number of <see cref="DbStatusData"/> entities into the database within an existing transaction.
        /// </summary>
        /// <param name="connection">The database connection to be used to perform the inserts.</param>
        /// <param name="transaction">The database transaction within which to perform the inserts.</param>
        /// <param name="dbStatusDatas">A list of <see cref="DbStatusData"/> entities to be inserted.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertAsync(DbConnection connection, DbTransaction transaction, List<DbStatusData> dbStatusDatas, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbStatusDataRepository().InsertAsync(connection, transaction, dbStatusDatas, cancellationTokenSource, commandTimeout);
        }
    }
}
