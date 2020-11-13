using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    public static class DbTripService
    {
        /// <summary>
        /// Retrieves a <see cref="DbTrip"/> with the specified <see cref="DbTrip.GeotabId"/>. Throws an <see cref="Exception"/> if an entity with the specified ID cannot be found.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="id">The ID of the database record to be returned.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<DbTrip> GetAsync(ConnectionInfo connectionInfo, string id, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbTripRepository().GetAsync(connectionInfo, id, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Retrieves all <see cref="DbTrip"/> entities.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<DbTrip>> GetAllAsync(ConnectionInfo connectionInfo, int commandTimeout)
        {
            return await new DbTripRepository().GetAllAsync(connectionInfo, commandTimeout);
        }

        /// <summary>
        /// Inserts a number of <see cref="DbTrip"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbTrips">A list of <see cref="DbTrip"/> entities to be inserted.</param>
        /// <param name="dbConfigFeedVersion">The <see cref="DbConfigFeedVersion"/> containing feed version information for the subject batch of records.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertAsync(ConnectionInfo connectionInfo, List<DbTrip> dbTrips, DbConfigFeedVersion dbConfigFeedVersion, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbTripRepository().InsertAsync(connectionInfo, dbTrips, dbConfigFeedVersion, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Updates a number of <see cref="DbTrip"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbTrips">A list of <see cref="DbTrip"/> entities to be updated.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> UpdateAsync(ConnectionInfo connectionInfo, List<DbTrip> dbTrips, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbTripRepository().UpdateAsync(connectionInfo, dbTrips, cancellationTokenSource, commandTimeout);
        }
    }
}
