using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    /// <summary>
    /// A service class that handles database CRUD operations for <see cref="DbFaultData"/> entities.
    /// </summary>
    public static class DbFaultDataService
    {
        /// <summary>
        /// Inserts a number of <see cref="DbFaultData"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbFaultDatas">A list of <see cref="DbFaultData"/> entities to be inserted.</param>
        /// <param name="dbConfigFeedVersion">The <see cref="DbConfigFeedVersion"/> containing feed version information for the subject batch of records.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertAsync(ConnectionInfo connectionInfo, IEnumerable<DbFaultData> dbFaultDatas, DbConfigFeedVersion dbConfigFeedVersion, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbFaultDataRepository().InsertAsync(connectionInfo, dbFaultDatas, dbConfigFeedVersion, cancellationTokenSource, commandTimeout);
        }
    }
}
