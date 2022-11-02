using Microsoft.Data.SqlClient;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// Intended for use with SQL Server bulk operations. Supplements <see cref="IConnectionContext"/>. Interface for a class that facilitates interaction between repository classes (that perform database CRUD operations) and <see cref="UnitOfWork"/> instances (that wrap transactions associated with the CRUD operations). This interface contains methods to be used by repositories to operate within <see cref="UnitOfWork"/> instances.
    /// </summary>
    public interface ISqlConnectionContext
    {
        /// <summary>
        /// Intended for use with SQL Server bulk operations. Gets the <see cref="UnitOfWork.SqlConnection"/> (an open <see cref="SqlConnection"/>). Throws a <see cref="System.InvalidOperationException"/> if the <see cref="IDatabaseUnitOfWorkContext.IsUnitOfWorkOpen"/> property of the associated <see cref="IDatabaseUnitOfWorkContext"/> is <c>false</c>.
        /// </summary>
        /// <returns></returns>
        SqlConnection GetSqlConnection();

        /// <summary>
        /// Intended for use with SQL Server bulk operations. Gets the <see cref="UnitOfWork.SqlTransaction"/> (the <see cref="SqlTransaction"/> associated with the open <see cref="UnitOfWork.SqlConnection"/>). Throws a <see cref="System.InvalidOperationException"/> if the <see cref="IDatabaseUnitOfWorkContext.IsUnitOfWorkOpen"/> property of the associated <see cref="IDatabaseUnitOfWorkContext"/> is <c>false</c>.
        /// </summary>
        /// <returns></returns>
        SqlTransaction GetSqlTransaction();
    }
}
