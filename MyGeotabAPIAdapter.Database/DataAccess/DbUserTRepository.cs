using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbUserT"/> entities.
    /// </summary>
    public class DbUserTRepository : BaseRepository2<DbUserT>
    {
        public DbUserTRepository(IConnectionContext context) : base(context) {}
    }
}
