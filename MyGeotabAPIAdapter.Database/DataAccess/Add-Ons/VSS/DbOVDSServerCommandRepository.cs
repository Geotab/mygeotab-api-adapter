using MyGeotabAPIAdapter.Database.Models.Add_Ons.VSS;

namespace MyGeotabAPIAdapter.Database.DataAccess.Add_Ons.VSS
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbOVDSServerCommand"/> entities.
    /// </summary>
    public class DbOVDSServerCommandRepository : BaseRepository<DbOVDSServerCommand>
    {
        public DbOVDSServerCommandRepository(IConnectionContext context) : base(context) { }
    }
}
