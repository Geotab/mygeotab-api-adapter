using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbDriverChangeTypeT"/> entities.
    /// </summary>
    public class DbDriverChangeTypeTRepository : BaseRepository2<DbDriverChangeTypeT>
    {
        public DbDriverChangeTypeTRepository(IConnectionContext context) : base(context) { }
    }
}
