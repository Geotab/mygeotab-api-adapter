using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// USE WITH EXTREME CAUTION! Intended for use with SQL Server bulk operations. Supplements <see cref="IConnectionContext"/>. This interface should only be used when there is a specific need to operate outside of a <see cref="UnitOfWork"/> or <see cref="SqlTransaction"/> and where doing so would not lead to any data integrity issues. For all other cases, use <see cref="ISqlConnectionContext"/>.
    /// </summary>
    public interface IStandaloneSqlConnectionContext
    {
        /// <summary>
        /// USE WITH EXTREME CAUTION! Intended for use with SQL Server bulk operations. Gets a standalone <see cref="SqlConnection"/> (an open connection). This method should only be used when there is a specific need to operate outside of a <see cref="UnitOfWork"/> or <see cref="SqlTransaction"/> and where doing so would not lead to any data integrity issues. For all other cases, use <see cref="ISqlConnectionContext.GetSqlConnection"/>.
        /// </summary>
        /// <returns></returns>
        Task<SqlConnection> GetStandaloneSqlConnectionAsync();
    }
}
