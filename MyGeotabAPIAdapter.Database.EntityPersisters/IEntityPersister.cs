using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.EntityPersisters
{
    /// <summary>
    /// Interface for a class with methods involving the persistence of <typeparamref name="T"/> entities to a corresponding database table.
    /// </summary>
    /// <typeparam name="T">The type of entity being persisted.</typeparam>
    public interface IEntityPersister<T> where T : IDbEntity
    {
        /// <summary>
        /// Persists the <paramref name="entitiesToPersist"/> to database.
        /// </summary>
        /// <param name="context">The <see cref="IConnectionContext"/> to use.</param>
        /// <param name="entitiesToPersist">The list of entities to be persisted to database.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="logLevel">The <see cref="LogLevel"/> to apply to the log message.</param>
        /// <param name="logPersistenceOperationDetails">Indicates whether to log the details of the persistence operation (i.e. number of records inserted/updated, etc.)</param>
        /// <returns></returns>
        Task PersistEntitiesToDatabaseAsync(IConnectionContext context, IEnumerable<T> entitiesToPersist, CancellationTokenSource cancellationTokenSource, LogLevel logLevel, bool logPersistenceOperationDetails = true);
    }
}
