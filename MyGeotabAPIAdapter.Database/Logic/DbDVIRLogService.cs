using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    /// <summary>
    /// A service class that handles database CRUD operations for <see cref="DbDVIRLog"/> entities.
    /// </summary>
    public static class DbDVIRLogService
    {
        /// <summary>
        /// Retrieves a <see cref="DbDVIRLog"/> with the specified <see cref="DbDVIRLog.Id"/>. Throws an <see cref="Exception"/> if an entity with the specified ID cannot be found.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="id">The ID of the database record to be returned.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<DbDVIRLog> GetAsync(ConnectionInfo connectionInfo, string id, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbDVIRLogRepository().GetAsync(connectionInfo, id, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Retrieves a <see cref="DbDVIRLog"/> with the specified <see cref="DbDVIRLog.Id"/>. Returns <c>null</c> if an entity with the specified ID cannot be found.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="id">The ID of the database record to be returned.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<DbDVIRLog> GetIfExistsAsync(ConnectionInfo connectionInfo, string id, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbDVIRLogRepository().GetIfExistsAsync(connectionInfo, id, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Retrieves all <see cref="DbDVIRLog"/> entities.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<DbDVIRLog>> GetAllAsync(ConnectionInfo connectionInfo, int commandTimeout)
        {
            return await new DbDVIRLogRepository().GetAllAsync(connectionInfo, commandTimeout);
        }

        /// <summary>
        /// Inserts/Updates a number of <see cref="DbDVIRLog"/>, <see cref="DbDVIRDefect"/> and <see cref="DbDVIRDefectRemark"/> entities into the database within a single transaction. Returns a string containing comma-separated counts: "[DbDVIRLog entities inserted],[DbDVIRDefect entities inserted],[DbDVIRDefect entities updated],[DbDVIRDefectRemark entities inserted]"
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbDVIRLogsToInsert">A list of <see cref="DbDVIRLog"/> entities to be inserted.</param>
        /// <param name="dbDVIRDefectsToInsert">A list of <see cref="DbDVIRDefect"/> entities to be inserted.</param>
        /// <param name="dbDVIRDefectsToUpdate">A list of <see cref="DbDVIRDefect"/> entities to be updated.</param>
        /// <param name="dbDVIRDefectRemarksToInsert">A list of <see cref="DbDVIRDefectRemark"/> entities to be inserted.</param>
        /// <param name="dbConfigFeedVersion">The <see cref="DbConfigFeedVersion"/> containing feed version information for the subject batch of records.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<string> PersistAllDVIRLogChangesToDatabase(ConnectionInfo connectionInfo, List<DbDVIRLog> dbDVIRLogsToInsert, List<DbDVIRDefect> dbDVIRDefectsToInsert, List<DbDVIRDefect> dbDVIRDefectsToUpdate, List<DbDVIRDefectRemark> dbDVIRDefectRemarksToInsert, DbConfigFeedVersion dbConfigFeedVersion, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbDVIRLogRepository().PersistAllDVIRLogChangesToDatabase(connectionInfo, dbDVIRLogsToInsert, dbDVIRDefectsToInsert, dbDVIRDefectsToUpdate, dbDVIRDefectRemarksToInsert, dbConfigFeedVersion, cancellationTokenSource, commandTimeout);
        }
    }
}
