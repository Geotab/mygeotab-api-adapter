using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbFaultDataTWithLagLeadDriverChange"/> entities.
    /// </summary>
    public class DbFaultDataTWithLagLeadDriverChangeRepository : BaseRepository2<DbFaultDataTWithLagLeadDriverChange>
    {
        public DbFaultDataTWithLagLeadDriverChangeRepository(IConnectionContext context) : base(context)
        {
        }
    }
}
