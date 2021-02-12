using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    /// <summary>
    /// A service class that handles database CRUD operations for <see cref="DbDVIRDefectUpdate"/> entities.
    /// </summary>
    public static class DbDVIRDefectUpdateService
    {
        /// <summary>
        /// Deletes a <see cref="DbDVIRDefectUpdate"/> entity from the database.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbDVIRDefectUpdate">The <see cref="DbDVIRDefectUpdate"/> entity to be deleted.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<bool> DeleteAsync(ConnectionInfo connectionInfo, DbDVIRDefectUpdate dbDVIRDefectUpdate, int commandTimeout)
        {
            return await new DbDVIRDefectUpdateRepository().DeleteAsync(connectionInfo, dbDVIRDefectUpdate, commandTimeout);
        }

        /// <summary>
        /// Retrieves all <see cref="DbDVIRDefectUpdate"/> entities.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <param name="resultsLimit">The maximum number of entities to return. If null, no limit is applied.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<DbDVIRDefectUpdate>> GetAllAsync(ConnectionInfo connectionInfo, int commandTimeout, int? resultsLimit = null)
        {
            return await new DbDVIRDefectUpdateRepository().GetAllAsync(connectionInfo, commandTimeout, resultsLimit);
        }
    }
}
