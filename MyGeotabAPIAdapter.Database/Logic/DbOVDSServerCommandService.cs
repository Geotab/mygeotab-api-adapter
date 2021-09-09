using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    /// <summary>
    /// A service class that handles database CRUD operations for <see cref="DbOVDSServerCommand"/> entities.
    /// </summary>
    public static class DbOVDSServerCommandService
    {
        /// <summary>
        /// Deletes a <see cref="DbOVDSServerCommand"/> entity from the database.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbOVDSServerCommand">The <see cref="DbOVDSServerCommand"/> entity to be deleted from the database.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<bool> DeleteAsync(ConnectionInfo connectionInfo, DbOVDSServerCommand dbOVDSServerCommand, int commandTimeout)
        {
            return await new DbOVDSServerCommandRepository().DeleteAsync(connectionInfo, dbOVDSServerCommand, commandTimeout);
        }

        /// <summary>
        /// Retrieves a <see cref="DbOVDSServerCommand"/> with the specified <see cref="DbOVDSServerCommand.Id"/>. Throws an <see cref="Exception"/> if an entity with the specified ID cannot be found.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="id">The ID of the database record to be returned.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<DbOVDSServerCommand> GetAsync(ConnectionInfo connectionInfo, string id, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbOVDSServerCommandRepository().GetAsync(connectionInfo, id, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Retrieves all <see cref="DbOVDSServerCommand"/> entities.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <param name="resultsLimit">The maximum number of entities to return. If null, no limit is applied.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<DbOVDSServerCommand>> GetAllAsync(ConnectionInfo connectionInfo, int commandTimeout, int? resultsLimit = null)
        {
            return await new DbOVDSServerCommandRepository().GetAllAsync(connectionInfo, commandTimeout, resultsLimit);
        }

        /// <summary>
        /// Inserts a number of <see cref="DbOVDSServerCommand"/> entities into the database within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbOVDSServerCommands">A list of <see cref="DbOVDSServerCommand"/> entities to be inserted.</param>
        /// <param name="dbConfigFeedVersion">The <see cref="DbConfigFeedVersion"/> containing feed version information for the subject batch of records.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertAsync(ConnectionInfo connectionInfo, List<DbOVDSServerCommand> dbOVDSServerCommands, DbConfigFeedVersion dbConfigFeedVersion, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            return await new DbOVDSServerCommandRepository().InsertAsync(connectionInfo, dbOVDSServerCommands, dbConfigFeedVersion, cancellationTokenSource, commandTimeout);
        }

        /// <summary>
        /// Inserts the <see cref="DbOVDSServerCommand"/> and <see cref="DbLogRecord"/> entities into the database and updates the <see cref="DbConfigFeedVersion"/> entity - all within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbOVDSServerCommands">A list of <see cref="DbOVDSServerCommand"/> entities to be inserted. These should have been generated using the <paramref name="dbLogRecords"/>.</param>
        /// <param name="dbLogRecords">A list of <see cref="DbLogRecord"/> entities to be inserted. These should have been used to generate the <paramref name="dbOVDSServerCommands"/>.</param>
        /// <param name="dbConfigFeedVersion">The <see cref="DbConfigFeedVersion"/> containing feed version information for the subject batch of <paramref name="dbLogRecords"/>.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns>A comma-delimited string containing in the following order: 
        /// <list type="number">
        /// <item>The number of <see cref="DbOVDSServerCommand"/> records inserted.</item>
        /// <item>The number of <see cref="DbLogRecord"/> records inserted.</item>
        /// <item>A boolean indicating whether the <see cref="DbConfigFeedVersion"/> record was updated successfully (<c>true</c> or <c>false</c>).</item>
        /// </list>
        public static async Task<string> InsertOVDSServerCommandsAndLogRecordsAsync(ConnectionInfo connectionInfo, List<DbOVDSServerCommand> dbOVDSServerCommands, List<DbLogRecord> dbLogRecords, DbConfigFeedVersion dbConfigFeedVersion, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            _ = cancellationTokenSource.Token;
            using (var connection = await new ConnectionProvider(connectionInfo).GetOpenConnectionAsync())
            {
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    var insertDbOVDSServerCommandsAsyncTask = new DbOVDSServerCommandRepository().InsertAsync(connection, transaction, dbOVDSServerCommands, cancellationTokenSource, commandTimeout);
                    var insertDbLogRecordsAsyncTask = DbLogRecordService.InsertAsync(connection, transaction, dbLogRecords, cancellationTokenSource, commandTimeout);
                    var updateDbConfigFeedVersionAsyncTask = new DbConfigFeedVersionRepository().UpdateAsync(connection, transaction, dbConfigFeedVersion, commandTimeout);

                    Task[] tasks = { insertDbOVDSServerCommandsAsyncTask, insertDbLogRecordsAsyncTask, updateDbConfigFeedVersionAsyncTask };

                    await Task.WhenAll(tasks);
                    await transaction.CommitAsync();
                    string results = $"{insertDbOVDSServerCommandsAsyncTask.Result},{insertDbLogRecordsAsyncTask.Result},{updateDbConfigFeedVersionAsyncTask.Result}";
                    return results;
                }
            }
        }

        /// <summary>
        /// Inserts the <see cref="DbOVDSServerCommand"/> and <see cref="DbStatusData"/> entities into the database and updates the <see cref="DbConfigFeedVersion"/> entity - all within a single transaction.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbOVDSServerCommands">A list of <see cref="DbOVDSServerCommand"/> entities to be inserted. These should have been generated using the <paramref name="dbStatusDatas"/>.</param>
        /// <param name="dbStatusDatas">A list of <see cref="DbStatusData"/> entities to be inserted. These should have been used to generate the <paramref name="dbOVDSServerCommands"/>.</param>
        /// <param name="dbConfigFeedVersion">The <see cref="DbConfigFeedVersion"/> containing feed version information for the subject batch of <paramref name="dbStatusDatas"/>.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns>A comma-delimited string containing in the following order: 
        /// <list type="number">
        /// <item>The number of <see cref="DbOVDSServerCommand"/> records inserted.</item>
        /// <item>The number of <see cref="DbStatusData"/> records inserted.</item>
        /// <item>A boolean indicating whether the <see cref="DbConfigFeedVersion"/> record was updated successfully (<c>true</c> or <c>false</c>).</item>
        /// </list>
        public static async Task<string> InsertOVDSServerCommandsAndStatusDatasAsync(ConnectionInfo connectionInfo, List<DbOVDSServerCommand> dbOVDSServerCommands, List<DbStatusData> dbStatusDatas, DbConfigFeedVersion dbConfigFeedVersion, CancellationTokenSource cancellationTokenSource, int commandTimeout)
        {
            _ = cancellationTokenSource.Token;
            using (var connection = await new ConnectionProvider(connectionInfo).GetOpenConnectionAsync())
            {
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    var insertDbOVDSServerCommandsAsyncTask = new DbOVDSServerCommandRepository().InsertAsync(connection, transaction, dbOVDSServerCommands, cancellationTokenSource, commandTimeout);
                    var insertDbStatusDatasAsyncTask = DbStatusDataService.InsertAsync(connection, transaction, dbStatusDatas, cancellationTokenSource, commandTimeout);
                    var updateDbConfigFeedVersionAsyncTask = new DbConfigFeedVersionRepository().UpdateAsync(connection, transaction, dbConfigFeedVersion, commandTimeout);

                    Task[] tasks = { insertDbOVDSServerCommandsAsyncTask, insertDbStatusDatasAsyncTask, updateDbConfigFeedVersionAsyncTask };

                    await Task.WhenAll(tasks);
                    await transaction.CommitAsync();
                    string results = $"{insertDbOVDSServerCommandsAsyncTask.Result},{insertDbStatusDatasAsyncTask.Result},{updateDbConfigFeedVersionAsyncTask.Result}";
                    return results;
                }
            }
        }
    }
}
