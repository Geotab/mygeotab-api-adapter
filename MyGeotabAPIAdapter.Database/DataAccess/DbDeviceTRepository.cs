using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbDeviceT"/> entities.
    /// </summary>
    public class DbDeviceTRepository : BaseRepository2<DbDeviceT>
    {
        public DbDeviceTRepository(IConnectionContext context) : base(context) { }
    }
}
