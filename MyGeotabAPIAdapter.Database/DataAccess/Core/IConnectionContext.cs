using System.Data.Common;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// Interface for a class that facilitates interaction between repository classes (that perform database CRUD operations) and <see cref="UnitOfWork"/> instances (that wrap transactions associated with the CRUD operations). This interface contains methods to be used by repositories to operate within <see cref="UnitOfWork"/> instances.
    /// </summary>
    public interface IConnectionContext
    {
        /// <summary>
        /// The <see cref="Databases"/> identifier for the database associated with the subject <see cref="ConnectionInfo"/> instance.
        /// </summary>
        Databases Database { get; }

        /// <summary>
        /// The maximum number of seconds that a database <see cref="System.Threading.Tasks.Task"/> or batch thereof can take to be completed before it is deemed that there is a database connectivity issue and a <see cref="Database.DatabaseConnectionException"/> should be thrown.
        /// </summary>
        int TimeoutSecondsForDatabaseTasks { get; }

        /// <summary>
        /// Gets the <see cref="UnitOfWork.Connection"/> (an open <see cref="DbConnection"/>). Throws a <see cref="System.InvalidOperationException"/> if the <see cref="IDatabaseUnitOfWorkContext.IsUnitOfWorkOpen"/> property of the associated <see cref="IDatabaseUnitOfWorkContext"/> is <c>false</c>.
        /// </summary>
        /// <returns></returns>
        DbConnection GetConnection();

        /// <summary>
        /// Gets the <see cref="UnitOfWork.Transaction"/> (the <see cref="DbTransaction"/> associated with the open <see cref="UnitOfWork.Connection"/>). Throws a <see cref="System.InvalidOperationException"/> if the <see cref="IDatabaseUnitOfWorkContext.IsUnitOfWorkOpen"/> property of the associated <see cref="IDatabaseUnitOfWorkContext"/> is <c>false</c>.
        /// </summary>
        /// <returns></returns>
        DbTransaction GetTransaction();

        /// <summary>
        /// The <see cref="UnitOfWork.Id"/> - a unique identifier assigned to the <see cref="UnitOfWork"/> at the time of its creation. Intended for debugging purposes. Throws a <see cref="System.InvalidOperationException"/> if the <see cref="IDatabaseUnitOfWorkContext.IsUnitOfWorkOpen"/> property of the associated <see cref="IDatabaseUnitOfWorkContext"/> is <c>false</c>.
        /// </summary>
        /// <returns></returns>
        string GetUnitOfWorkId();
    }
}
