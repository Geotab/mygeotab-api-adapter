using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    /// <summary>
    /// A service class that handles database CRUD operations for <see cref="DbZoneType"/> entities.
    /// </summary>
    public static class DbZoneTypeService
    {
        /// <summary>
        /// Retrieves a <see cref="DbZoneType"/> with the specified <see cref="DbZoneType.GeotabId"/>. Throws an <see cref="Exception"/> if an entity with the specified ID cannot be found.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="id">The ID of the database record to be returned.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<DbZoneType> GetAsync(ConnectionInfo connectionInfo, string id, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbZoneTypeRepository().GetAsync(connectionInfo, id, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Retrieves all <see cref="DbZoneType"/> entities.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<DbZoneType>> GetAllAsync(ConnectionInfo connectionInfo, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            var result = await new DbZoneTypeRepository().GetAllAsync(connectionInfo, commandTimeout);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }

        /// <summary>
        /// Inserts a number of <see cref="DbZoneType"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbZoneTypes">A list of <see cref="DbZoneType"/> entities to be inserted.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertAsync(ConnectionInfo connectionInfo, IEnumerable<DbZoneType> dbZoneTypes, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbZoneTypeRepository().InsertAsync(connectionInfo, dbZoneTypes, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Updates a number of <see cref="DbZoneType"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbZoneTypes">A list of <see cref="DbZoneType"/> entities to be updated.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> UpdateAsync(ConnectionInfo connectionInfo, IEnumerable<DbZoneType> dbZoneTypes, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbZoneTypeRepository().UpdateAsync(connectionInfo, dbZoneTypes, cancellationTokenSource, commandTimeout);
        }
    }
}
