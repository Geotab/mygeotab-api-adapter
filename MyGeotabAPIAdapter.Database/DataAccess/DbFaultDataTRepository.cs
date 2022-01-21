using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbFaultDataT"/> entities.
    /// </summary>
    public class DbFaultDataTRepository : BaseRepository2<DbFaultDataT>
    {
        public DbFaultDataTRepository(IConnectionContext context) : base(context)
        {
        }
    }
}
