using Npgsql;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// Intended for use with PostgreSQL bulk operations. Supplements <see cref="IConnectionContext"/>. Interface for a class that facilitates interaction between repository classes (that perform database CRUD operations) and <see cref="UnitOfWork"/> instances (that wrap transactions associated with the CRUD operations). This interface contains methods to be used by repositories to operate within <see cref="UnitOfWork"/> instances.
    /// </summary>
    public interface INpgsqlConnectionContext
    {
        /// <summary>
        /// Intended for use with PostgreSQL bulk operations. Gets the <see cref="UnitOfWork.NpgsqlConnection"/> (an open <see cref="NpgsqlConnection"/>). Throws a <see cref="System.InvalidOperationException"/> if the <see cref="IDatabaseUnitOfWorkContext.IsUnitOfWorkOpen"/> property of the associated <see cref="IDatabaseUnitOfWorkContext"/> is <c>false</c>.
        /// </summary>
        /// <returns></returns>
        NpgsqlConnection GetNpgsqlConnection();

        /// <summary>
        /// Intended for use with PostgreSQL bulk operations. Gets the <see cref="UnitOfWork.NpgsqlTransaction"/> (the <see cref="NpgsqlTransaction"/> associated with the open <see cref="UnitOfWork.NpgsqlConnection"/>). Throws a <see cref="System.InvalidOperationException"/> if the <see cref="IDatabaseUnitOfWorkContext.IsUnitOfWorkOpen"/> property of the associated <see cref="IDatabaseUnitOfWorkContext"/> is <c>false</c>.
        /// </summary>
        /// <returns></returns>
        NpgsqlTransaction GetNpgsqlTransaction();
    }
}
