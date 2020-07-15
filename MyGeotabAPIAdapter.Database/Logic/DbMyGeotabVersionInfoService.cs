using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    /// <summary>
    /// A service class that handles database CRUD operations for <see cref="DbMyGeotabVersionInfo"/> entities.
    /// </summary>
    public static class DbMyGeotabVersionInfoService
    {
        /// <summary>
        /// Retrieves all <see cref="DbMyGeotabVersionInfo"/> entities.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<DbMyGeotabVersionInfo>> GetAllAsync(ConnectionInfo connectionInfo, int commandTimeout)
        {
            return await new DbMyGeotabVersionInfoRepository().GetAllAsync(connectionInfo, commandTimeout);
        }

        /// <summary>
        /// Inserts a number of <see cref="DbMyGeotabVersionInfo"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbMyGeotabVersionInfo">A <see cref="DbMyGeotabVersionInfo"/> entity to be inserted.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertAsync(ConnectionInfo connectionInfo, DbMyGeotabVersionInfo dbMyGeotabVersionInfo, int commandTimeout)
        {
            return await new DbMyGeotabVersionInfoRepository().InsertAsync(connectionInfo, dbMyGeotabVersionInfo, commandTimeout);
        }
    }
}
