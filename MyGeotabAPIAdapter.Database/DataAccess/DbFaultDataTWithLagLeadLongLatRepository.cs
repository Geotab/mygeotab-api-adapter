using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbFaultDataTWithLagLeadLongLat"/> entities.
    /// </summary>
    public class DbFaultDataTWithLagLeadLongLatRepository : BaseRepository2<DbFaultDataTWithLagLeadLongLat>
    {
        readonly IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames;

        public DbFaultDataTWithLagLeadLongLatRepository(IConnectionContext context, IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames) : base(context)
        {
            this.optimizerDatabaseObjectNames = optimizerDatabaseObjectNames;
        }
    }
}
