using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbFaultData"/> entities.
    /// </summary>
    public class DbFaultDataRepository2 : BaseRepository2<DbFaultData>
    {
        public DbFaultDataRepository2(IConnectionContext context) : base(context) { }
    }
}
