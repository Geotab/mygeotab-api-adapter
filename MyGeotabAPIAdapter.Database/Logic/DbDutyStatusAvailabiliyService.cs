using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    public static class DbDutyStatusAvailabilityService
    {
        /// <summary>
        /// Deletes a number of <see cref="DbDutyStatusAvailability"/> entities from the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbDutyStatusAvailabilitys">A list of <see cref="DbDutyStatusAvailability"/> entities to be deleted.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> DeleteAsync(ConnectionInfo connectionInfo, IList<DbDutyStatusAvailability> dbDutyStatusAvailabilitys, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbDutyStatusAvailabilityRepository().DeleteAsync(connectionInfo, dbDutyStatusAvailabilitys, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Retrieves a <see cref="DbDutyStatusAvailability"/> with the specified <see cref="DbDutyStatusAvailability.GeotabId"/>. Throws an <see cref="Exception"/> if an entity with the specified ID cannot be found.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="id">The ID of the database record to be returned.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<DbDutyStatusAvailability> GetAsync(ConnectionInfo connectionInfo, string id, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbDutyStatusAvailabilityRepository().GetAsync(connectionInfo, id, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Retrieves all <see cref="DbDutyStatusAvailability"/> entities.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<DbDutyStatusAvailability>> GetAllAsync(ConnectionInfo connectionInfo, int commandTimeout)
        {
            return await new DbDutyStatusAvailabilityRepository().GetAllAsync(connectionInfo, commandTimeout);
        }

        /// <summary>
        /// Inserts a number of <see cref="DbDutyStatusAvailability"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbDutyStatusAvailabilityEntities">A list of <see cref="DbDutyStatusAvailability"/> entities to be inserted.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertAsync(ConnectionInfo connectionInfo, List<DbDutyStatusAvailability> dbDutyStatusAvailabilityEntities, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbDutyStatusAvailabilityRepository().InsertAsync(connectionInfo, dbDutyStatusAvailabilityEntities, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Updates a number of <see cref="DbDutyStatusAvailability"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbDutyStatusAvailabilityEntities">A list of <see cref="DbDutyStatusAvailability"/> entities to be updated.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> UpdateAsync(ConnectionInfo connectionInfo, List<DbDutyStatusAvailability> dbDutyStatusAvailabilityEntities, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbDutyStatusAvailabilityRepository().UpdateAsync(connectionInfo, dbDutyStatusAvailabilityEntities, cancellationTokenSource, commandTimeout);
        }
    }
}
