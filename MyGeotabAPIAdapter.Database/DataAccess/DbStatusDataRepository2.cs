using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbStatusData"/> entities.
    /// </summary>
    public class DbStatusDataRepository2 : BaseRepository2<DbStatusData>
    {
        public DbStatusDataRepository2(IConnectionContext context) : base(context) { }
    }
}
