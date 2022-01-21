using MyGeotabAPIAdapter.Logging;
using System;

namespace MyGeotabAPIAdapter.Database.EntityPersisters
{
    /// <summary>
    /// Interface for a class that assists with logging persistence opertaion details.
    /// </summary>
    public interface IEntityPersistenceLogger
    {
        /// <summary>
        /// Logs the details of the database persistence operation.
        /// </summary>
        /// <param name="databaseWriteOperationType">The <see cref="Common.DatabaseWriteOperationType"/> of the database persistence operation.</param>
        /// <param name="affectedEntityCount">Th number of entities affected.</param>
        /// <param name="database">The <see cref="Databases"/> upon which the persistence opertaion was applied.</param>
        /// <param name="databaseTableName">The name of the database table upon which the persistence opertaion was applied.</param>
        /// <param name="elapsedTime">The amount of time taken for the persistence operation.</param>
        /// <param name="logLevel">The <see cref="LogLevel"/> to apply to the log message.</param>
        void LogPersistenceOperationDetails(Common.DatabaseWriteOperationType databaseWriteOperationType, long affectedEntityCount, Databases database, string databaseTableName, TimeSpan elapsedTime, LogLevel logLevel);
    }
}
