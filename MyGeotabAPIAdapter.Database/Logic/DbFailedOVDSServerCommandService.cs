using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Logic
{
    /// <summary>
    /// A service class that handles database CRUD operations for <see cref="DbFailedOVDSServerCommand"/> entities.
    /// </summary>
    public static class DbFailedOVDSServerCommandService
    {
        /// <summary>
        /// Inserts a <see cref="DbFailedOVDSServerCommand"/> entity into the database.
        /// </summary>
        /// <param name="connectionInfo">The database connection information.</param>
        /// <param name="dbFailedOVDSServerCommand">A <see cref="DbFailedOVDSServerCommand"/> entity to be inserted.</param>
        /// <param name="commandTimeout">The number of seconds before command execution timeout.</param>
        /// <returns></returns>
        public static async Task<long> InsertAsync(ConnectionInfo connectionInfo, DbFailedOVDSServerCommand dbFailedOVDSServerCommand, int commandTimeout)
        {
            return await new DbFailedOVDSServerCommandRepository().InsertAsync(connectionInfo, dbFailedOVDSServerCommand, commandTimeout);
        }
    }
}
