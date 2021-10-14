using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    /// <summary>
    /// A service class that handles database CRUD operations for <see cref="DbDeviceStatusInfo"/> entities.
    /// </summary>
    public static class DbDeviceStatusInfoService
    {
        /// <summary>
        /// Retrieves a <see cref="DbDeviceStatusInfo"/> with the specified <see cref="DbDeviceStatusInfo.GeotabId"/>. Throws an <see cref="Exception"/> if an entity with the specified ID cannot be found.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="id">The ID of the database record to be returned.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<DbDeviceStatusInfo> GetAsync(ConnectionInfo connectionInfo, string id, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbDeviceStatusInfoRepository().GetAsync(connectionInfo, id, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Retrieves all <see cref="DbDeviceStatusInfo"/> entities.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<DbDeviceStatusInfo>> GetAllAsync(ConnectionInfo connectionInfo, int commandTimeout)
        {
            return await new DbDeviceStatusInfoRepository().GetAllAsync(connectionInfo, commandTimeout);
        }

        /// <summary>
        /// Inserts a number of <see cref="DbDeviceStatusInfo"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbDeviceStatusInfos">A list of <see cref="DbDeviceStatusInfo"/> entities to be inserted.</param>
        /// <param name="dbConfigFeedVersion">The <see cref="DbConfigFeedVersion"/> containing feed version information for the subject batch of records.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertAsync(ConnectionInfo connectionInfo, List<DbDeviceStatusInfo> dbDeviceStatusInfos, DbConfigFeedVersion dbConfigFeedVersion, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbDeviceStatusInfoRepository().InsertAsync(connectionInfo, dbDeviceStatusInfos, dbConfigFeedVersion, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Updates a number of <see cref="DbDeviceStatusInfo"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbDeviceStatusInfos">A list of <see cref="DbDeviceStatusInfo"/> entities to be updated.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> UpdateAsync(ConnectionInfo connectionInfo, List<DbDeviceStatusInfo> dbDeviceStatusInfos, DbConfigFeedVersion dbConfigFeedVersion, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbDeviceStatusInfoRepository().UpdateAsync(connectionInfo, dbDeviceStatusInfos, dbConfigFeedVersion, cancellationTokenSource, commandTimeout);
        }
    }
}
