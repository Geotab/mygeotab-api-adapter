using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbStatusDataTWithLagLeadDriverChange"/> entities.
    /// </summary>
    public class DbStatusDataTWithLagLeadDriverChangeRepository : BaseRepository2<DbStatusDataTWithLagLeadDriverChange>
    {
        public DbStatusDataTWithLagLeadDriverChangeRepository(IConnectionContext context) : base(context)
        {
        }
    }
}
