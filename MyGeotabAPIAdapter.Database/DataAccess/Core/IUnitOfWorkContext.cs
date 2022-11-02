using static MyGeotabAPIAdapter.Database.ConnectionInfo;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// Interface for a class that facilitates interaction between repository classes (that perform database CRUD operations) and <see cref="UnitOfWork"/> instances (that wrap transactions associated with the CRUD operations). This interface contains methods used to manage <see cref="UnitOfWork"/> instances.
    /// </summary>
    public interface IUnitOfWorkContext
    {
        /// <summary>
        /// The <see cref="DataAccessProviderType"/> associated with the current <see cref="IUnitOfWorkContext"/>.
        /// </summary>
        public DataAccessProviderType ProviderType { get; }

        /// <summary>
        /// A unique identifier assigned to the <see cref="IUnitOfWorkContext"/> at the time of its creation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Creates a new <see cref="UnitOfWork"/> instance.
        /// </summary>
        /// <param name="database">The <see cref="Databases"/> identifier (i.e. database) with which the <see cref="UnitOfWork"/> instance should be associated.</param>
        /// <returns></returns>
        UnitOfWork CreateUnitOfWork(Databases database);
    }

    /// <summary>
    /// Type of database connection to be used. Intended to support SQL Server bulk operations in addition to or in combination with standard database operations. Choices include:
    /// <list type="bullet">
    /// <item><see cref="DbConnection"/ - Use standard database-agnostic <see cref="System.Data.Common.DbConnection"/> only.></item>
    /// <item><see cref="SqlConnection"/ - Use <see cref="Microsoft.Data.SqlClient.SqlConnection"/> only. This option should only be used when the database type is SQL Server and only bulk operations will be executed in the subject <see cref="UnitOfWork"/>.></item>
    /// <item><see cref="DualConnections"/ - Use both the standard database-agnostic <see cref="System.Data.Common.DbConnection"/> and the <see cref="Microsoft.Data.SqlClient.SqlConnection"/>. One connection of each type along with a transaction for each type. This option should only be used when the database type is SQL Server. Additionally, this option should be used only when multiple tables need to be written to and there are some standard operations as well as one or more bulk operations within the same Unit of Work.></item>
    /// <item><see cref="NullValue"/ - A null value substitute.></item>
    /// </list>
    /// </summary>
    public enum DatabaseConnectionType { DbConnection, SqlConnection, DualConnections, NullValue }
}
