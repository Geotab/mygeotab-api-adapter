using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    /// <summary>
    /// A service class that handles database CRUD operations for <see cref="DbCondition"/> entities.
    /// </summary>
    public static class DbConditionService
    {
        /// <summary>
        /// Retrieves a <see cref="DbCondition"/> with the specified <see cref="DbCondition.GeotabId"/>. Throws an <see cref="Exception"/> if an entity with the specified ID cannot be found.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="id">The ID of the database record to be returned.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<DbCondition> GetAsync(ConnectionInfo connectionInfo, string id, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbConditionRepository().GetAsync(connectionInfo, id, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Retrieves all <see cref="DbCondition"/> entities.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<DbCondition>> GetAllAsync(ConnectionInfo connectionInfo, int commandTimeout)
        {
            return await new DbConditionRepository().GetAllAsync(connectionInfo, commandTimeout);
        }

        /// <summary>
        /// Inserts a number of <see cref="DbCondition"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbConditions">A list of <see cref="DbCondition"/> entities to be inserted.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertAsync(ConnectionInfo connectionInfo, IList<DbCondition> dbConditions, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbConditionRepository().InsertAsync(connectionInfo, dbConditions, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Updates a number of <see cref="DbCondition"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbConditions">A list of <see cref="DbCondition"/> entities to be updated.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> UpdateAsync(ConnectionInfo connectionInfo, List<DbCondition> dbConditions, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbConditionRepository().UpdateAsync(connectionInfo, dbConditions, cancellationTokenSource, commandTimeout);
        }
    }
}
