using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbDiagnosticIdT"/> entities.
    /// </summary>
    public class DbDiagnosticIdTRepository : BaseRepository2<DbDiagnosticIdT>
    {
        public DbDiagnosticIdTRepository(IConnectionContext context) : base(context) { }
    }
}
