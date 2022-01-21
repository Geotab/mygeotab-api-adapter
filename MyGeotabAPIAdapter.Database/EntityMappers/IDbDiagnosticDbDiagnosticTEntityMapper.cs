using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DbDiagnostic"/> and <see cref="DbDiagnosticT"/> entities.
    /// </summary>
    public interface IDbDiagnosticDbDiagnosticTEntityMapper : IEntityMapper<DbDiagnostic, DbDiagnosticT>
    {
    }
}
