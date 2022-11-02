using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Diagnostic"/> and <see cref="DbDiagnostic"/> entities.
    /// </summary>
    public interface IGeotabDiagnosticDbDiagnosticObjectMapper : IStatusableGeotabObjectMapper<Diagnostic, DbDiagnostic>
    {
    }
}
