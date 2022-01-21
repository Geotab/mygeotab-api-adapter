using MyGeotabAPIAdapter.Database.Models;


namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbDevice"/> entities.
    /// </summary>
    public class DbDeviceRepository2 : BaseRepository2<DbDevice>
    {
        public DbDeviceRepository2(IConnectionContext context) : base(context) { }
    }
}
