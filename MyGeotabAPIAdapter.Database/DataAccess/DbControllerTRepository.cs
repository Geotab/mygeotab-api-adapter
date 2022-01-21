using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbControllerT"/> entities.
    /// </summary>
    public class DbControllerTRepository : BaseRepository2<DbControllerT>
    {
        public DbControllerTRepository(IConnectionContext context) : base(context) { }
    }
}
