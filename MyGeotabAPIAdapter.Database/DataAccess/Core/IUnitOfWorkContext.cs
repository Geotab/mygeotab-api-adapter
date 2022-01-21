using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// Interface for a class that facilitates interaction between repository classes (that perform database CRUD operations) and <see cref="UnitOfWork"/> instances (that wrap transactions associated with the CRUD operations). This interface contains methods used to manage <see cref="UnitOfWork"/> instances.
    /// </summary>
    public interface IUnitOfWorkContext
    {
        /// <summary>
        /// A unique identifier assigned to the <see cref="IUnitOfWorkContext"/> at the time of its creation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Creates a new <see cref="UnitOfWork"/> instance.
        /// </summary>
        /// <param name="database">The <see cref="Databases"/> identifier (i.e. database) with which the <see cref="UnitOfWork"/> instance should be associated.</param>
        /// <param name="TimeoutSecondsForDatabaseTasks">The maximum number of seconds that a database <see cref="System.Threading.Tasks.Task"/> or batch thereof can take to be completed before it is deemed that there is a database connectivity issue and a <see cref="Database.DatabaseConnectionException"/> should be thrown.</param>
        /// <returns></returns>
        UnitOfWork CreateUnitOfWork(Databases database, int TimeoutSecondsForDatabaseTasks);
    }
}
