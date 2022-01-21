using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbStatusDataT"/> entities.
    /// </summary>
    public class DbStatusDataTRepository : BaseRepository2<DbStatusDataT>
    {
        public DbStatusDataTRepository(IConnectionContext context) : base(context)
        {
        }
    }
}
