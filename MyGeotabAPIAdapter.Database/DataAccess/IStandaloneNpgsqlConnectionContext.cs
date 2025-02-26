using Npgsql;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// USE WITH EXTREME CAUTION! Intended for use with PostgreSQL bulk operations. Supplements <see cref="IConnectionContext"/>. This interface should only be used when there is a specific need to operate outside of a <see cref="UnitOfWork"/> or <see cref="NpgsqlTransaction"/> and where doing so would not lead to any data integrity issues. For all other cases, use <see cref="INpgsqlConnectionContext"/>.
    /// </summary>
    public interface IStandaloneNpgsqlConnectionContext
    {
        /// <summary>
        /// USE WITH EXTREME CAUTION! Intended for use with PostgreSQL bulk operations. Gets a standalone <see cref="NpgsqlConnection"/> (an open connection). This method should only be used when there is a specific need to operate outside of a <see cref="UnitOfWork"/> or <see cref="NpgsqlTransaction"/> and where doing so would not lead to any data integrity issues. For all other cases, use <see cref="INpgsqlConnectionContext.GetNpgsqlConnection"/>.
        /// </summary>
        /// <returns></returns>
        Task<NpgsqlConnection> GetStandaloneNpgsqlConnectionAsync();
    }
}
