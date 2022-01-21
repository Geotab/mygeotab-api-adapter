using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbBinaryTypeT"/> entities.
    /// </summary>
    public class DbBinaryTypeTRepository : BaseRepository2<DbBinaryTypeT>
    {
        public DbBinaryTypeTRepository(IConnectionContext context) : base(context) { }
    }
}
