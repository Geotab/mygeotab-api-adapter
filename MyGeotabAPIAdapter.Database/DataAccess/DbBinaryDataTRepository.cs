using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbBinaryDataT"/> entities.
    /// </summary>
    public class DbBinaryDataTRepository : BaseRepository2<DbBinaryDataT>
    {
        public DbBinaryDataTRepository(IConnectionContext context) : base(context) { }
    }
}
