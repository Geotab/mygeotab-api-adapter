using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    /// <summary>
    /// A service class that handles database CRUD operations for <see cref="DbFailedDVIRDefectUpdate"/> entities.
    /// </summary>
    public static class DbFailedDVIRDefectUpdateService
    {
        /// <summary>
        /// Inserts a <see cref="DbFailedDVIRDefectUpdate"/> entity into the database.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbFailedDVIRDefectUpdate">A <see cref="DbFailedDVIRDefectUpdate"/> entity to be inserted.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertAsync(ConnectionInfo connectionInfo, DbFailedDVIRDefectUpdate dbFailedDVIRDefectUpdate, int commandTimeout)
        {
            return await new DbFailedDVIRDefectUpdateRepository().InsertAsync(connectionInfo, dbFailedDVIRDefectUpdate, commandTimeout);
        }
    }
}
