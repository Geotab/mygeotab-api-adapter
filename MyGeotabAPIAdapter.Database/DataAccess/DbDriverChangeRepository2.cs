using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbDriverChange"/> entities.
    /// </summary>
    public class DbDriverChangeRepository2 : BaseRepository2<DbDriverChange>
    {
        public DbDriverChangeRepository2(IConnectionContext context) : base(context) { }
    }
}
