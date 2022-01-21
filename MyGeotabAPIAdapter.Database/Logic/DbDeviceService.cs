using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    /// <summary>
    /// A service class that handles database CRUD operations for <see cref="DbDevice"/> entities.
    /// </summary>
    public static class DbDeviceService
    {
        /// <summary>
        /// Retrieves a <see cref="DbDevice"/> with the specified <see cref="DbDevice.GeotabId"/>. Throws an <see cref="Exception"/> if an entity with the specified ID cannot be found.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="id">The ID of the database record to be returned.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<DbDevice> GetAsync(ConnectionInfo connectionInfo, string id, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbDeviceRepository().GetAsync(connectionInfo, id, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Retrieves all <see cref="DbDevice"/> entities.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<DbDevice>> GetAllAsync(ConnectionInfo connectionInfo, int commandTimeout)
        {
            return await new DbDeviceRepository().GetAllAsync(connectionInfo, commandTimeout);
        }

        /// <summary>
        /// Inserts a number of <see cref="DbDevice"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbDevices">A list of <see cref="DbDevice"/> entities to be inserted.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertAsync(ConnectionInfo connectionInfo, IEnumerable<DbDevice> dbDevices, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbDeviceRepository().InsertAsync(connectionInfo, dbDevices, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Updates a number of <see cref="DbDevice"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbDevices">A list of <see cref="DbDevice"/> entities to be updated.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> UpdateAsync(ConnectionInfo connectionInfo, IEnumerable<DbDevice> dbDevices, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbDeviceRepository().UpdateAsync(connectionInfo, dbDevices, cancellationTokenSource, commandTimeout);
        }
    }
}
