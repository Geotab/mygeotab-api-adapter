using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbBinaryData"/> entities.
    /// </summary>
    public class DbBinaryDataRepository2 : BaseRepository2<DbBinaryData>
    {
        public DbBinaryDataRepository2(IConnectionContext context) : base(context) { }
    }
}
