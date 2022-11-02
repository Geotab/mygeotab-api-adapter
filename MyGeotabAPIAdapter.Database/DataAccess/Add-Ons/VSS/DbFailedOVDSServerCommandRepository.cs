using MyGeotabAPIAdapter.Database.Models.Add_Ons.VSS;

namespace MyGeotabAPIAdapter.Database.DataAccess.Add_Ons.VSS
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbFailedOVDSServerCommand"/> entities.
    /// </summary>
    public class DbFailedOVDSServerCommandRepository : BaseRepository<DbFailedOVDSServerCommand>
    {
        public DbFailedOVDSServerCommandRepository(IConnectionContext context) : base(context) { }
    }
}
