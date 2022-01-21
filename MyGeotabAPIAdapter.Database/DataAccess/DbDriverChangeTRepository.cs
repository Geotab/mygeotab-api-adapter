using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbDriverChangeT"/> entities.
    /// </summary>
    public class DbDriverChangeTRepository : BaseRepository2<DbDriverChangeT>
    {
        public DbDriverChangeTRepository(IConnectionContext context) : base(context) { }
    }
}
