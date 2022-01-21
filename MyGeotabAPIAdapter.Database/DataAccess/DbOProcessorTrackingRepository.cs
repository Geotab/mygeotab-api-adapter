using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbOProcessorTracking"/> entities.
    /// </summary>
    public class DbOProcessorTrackingRepository : BaseRepository2<DbOProcessorTracking>
    {
        public DbOProcessorTrackingRepository(IConnectionContext context) : base(context) { }
    }
}
