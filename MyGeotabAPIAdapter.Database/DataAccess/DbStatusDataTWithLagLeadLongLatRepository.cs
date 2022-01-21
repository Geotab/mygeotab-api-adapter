using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbStatusDataTWithLagLeadLongLat"/> entities.
    /// </summary>
    public class DbStatusDataTWithLagLeadLongLatRepository : BaseRepository2<DbStatusDataTWithLagLeadLongLat>
    {
        readonly IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames;

        public DbStatusDataTWithLagLeadLongLatRepository(IConnectionContext context, IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames) : base(context)
        {
            this.optimizerDatabaseObjectNames = optimizerDatabaseObjectNames;
        }
    }
}
