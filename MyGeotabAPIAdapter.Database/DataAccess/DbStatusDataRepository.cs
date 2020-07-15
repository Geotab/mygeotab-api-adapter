using MyGeotabAPIAdapter.Database.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbStatusData"/> entities.
    /// </summary>
    public class DbStatusDataRepository : BaseRepository<DbStatusData>
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
        public async Task<long> InsertAsync(ConnectionInfo connectionInfo, List<DbStatusData> dbStatusDatas, DbConfigFeedVersion dbConfigFeedVersion, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            long insertedRowsCount = 0;
            using (var connection = await new ConnectionProvider(connectionInfo).GetOpenConnectionAsync())
            {
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    foreach (var dbStatusData in dbStatusDatas)
                    {
                        await InsertAsync(connection, transaction, dbStatusData, commandTimeout);
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
    }
}
