using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    /// <summary>
    /// A service class that handles database CRUD operations for <see cref="DbRule"/> entities.
    /// </summary>
    public static class DbRuleService
    {
        /// <summary>
        /// Retrieves a <see cref="DbRule"/> with the specified <see cref="DbRule.GeotabId"/>. Throws an <see cref="Exception"/> if an entity with the specified ID cannot be found.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="id">The ID of the database record to be returned.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<DbRule> GetAsync(ConnectionInfo connectionInfo, string id, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbRuleRepository().GetAsync(connectionInfo, id, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Retrieves all <see cref="DbRule"/> entities.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<DbRule>> GetAllAsync(ConnectionInfo connectionInfo, int commandTimeout)
        {
            return await new DbRuleRepository().GetAllAsync(connectionInfo, commandTimeout);
        }

        /// <summary>
        /// Inserts a number of <see cref="DbRule"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbRules">A list of <see cref="DbRule"/> entities to be inserted.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertAsync(ConnectionInfo connectionInfo, List<DbRule> dbRules, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbRuleRepository().InsertAsync(connectionInfo, dbRules, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Inserts a single <see cref="DbRule"/> entity into the database.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbRule">A <see cref="DbRule"/> entity to be inserted.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertRuleAsync(ConnectionInfo connectionInfo, DbRule dbRule, int commandTimeout)
        {
            return await new DbRuleRepository().InsertAsync(connectionInfo, dbRule, commandTimeout);
        }

        /// <summary>
        /// Updates a number of <see cref="DbRule"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbRules">A list of <see cref="DbRule"/> entities to be updated.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> UpdateAsync(ConnectionInfo connectionInfo, List<DbRule> dbRules, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbRuleRepository().UpdateAsync(connectionInfo, dbRules, cancellationTokenSource, commandTimeout);
        }
    }
}
