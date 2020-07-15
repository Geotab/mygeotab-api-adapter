using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    /// <summary>
    /// A service class that handles database CRUD operations for <see cref="DbConfigFeedVersion"/> entities.
    /// </summary>
    public static class DbConfigFeedVersionService
    {
        /// <summary>
        /// Retrieves a <see cref="DbConfigFeedVersion"/> with the specified <see cref="DbConfigFeedVersion.FeedTypeId"/>. Throws an <see cref="Exception"/> if an entity with the specified ID cannot be found.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="feedTypeId">The ID of the database record to be returned.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<DbConfigFeedVersion> GetAsync(ConnectionInfo connectionInfo, string feedTypeId, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbConfigFeedVersionRepository().GetAsync(connectionInfo, feedTypeId, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Retrieves all <see cref="DbConfigFeedVersion"/> entities.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<DbConfigFeedVersion>> GetAllAsync(ConnectionInfo connectionInfo, int commandTimeout)
        {
            return await new DbConfigFeedVersionRepository().GetAllAsync(connectionInfo, commandTimeout);
        }

        /// <summary>
        /// Inserts a number of <see cref="DbConfigFeedVersion"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbConfigFeedVersions">A list of <see cref="DbConfigFeedVersion"/> entities to be inserted.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertAsync(ConnectionInfo connectionInfo, List<DbConfigFeedVersion> dbConfigFeedVersions, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbConfigFeedVersionRepository().InsertAsync(connectionInfo, dbConfigFeedVersions, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Updates a <see cref="DbConfigFeedVersion"/> entity in the database.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbConfigFeedVersion">The <see cref="DbConfigFeedVersion"/> entity to be updated.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<bool> UpdateAsync(ConnectionInfo connectionInfo, DbConfigFeedVersion dbConfigFeedVersion, int commandTimeout)
        {
            return await new DbConfigFeedVersionRepository().UpdateAsync(connectionInfo, dbConfigFeedVersion, commandTimeout);
        }

        /// <summary>
        /// Updates a number of <see cref="DbConfigFeedVersion"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbConfigFeedVersions">A list of <see cref="DbConfigFeedVersion"/> entities to be updated.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> UpdateAsync(ConnectionInfo connectionInfo, List<DbConfigFeedVersion> dbConfigFeedVersions, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbConfigFeedVersionRepository().UpdateAsync(connectionInfo, dbConfigFeedVersions, cancellationTokenSource, commandTimeout);
        }
    }
}
