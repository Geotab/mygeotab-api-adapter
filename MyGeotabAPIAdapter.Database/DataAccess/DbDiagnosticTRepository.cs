using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbDiagnosticT"/> entities.
    /// </summary>
    public class DbDiagnosticTRepository : BaseRepository2<DbDiagnosticT>
    {
        public DbDiagnosticTRepository(IConnectionContext context) : base(context) { }
    }
}
