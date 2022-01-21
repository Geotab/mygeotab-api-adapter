using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbFaultDataTLongLatUpdate"/> entities.
    /// </summary>
    public class DbFaultDataTLongLatUpdateRepository : BaseRepository2<DbFaultDataTLongLatUpdate>
    {
        readonly IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames;

        public DbFaultDataTLongLatUpdateRepository(IConnectionContext context, IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames) : base(context)
        {
            this.optimizerDatabaseObjectNames = optimizerDatabaseObjectNames;
        }
    }
}

