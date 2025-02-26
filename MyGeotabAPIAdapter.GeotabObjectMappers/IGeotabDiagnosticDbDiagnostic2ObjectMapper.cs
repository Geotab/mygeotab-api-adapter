using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Diagnostic"/> and <see cref="DbDiagnostic2"/> entities.
    /// </summary>
    public interface IGeotabDiagnosticDbDiagnostic2ObjectMapper : IStatusableGeotabObjectMapper<Diagnostic, DbDiagnostic2>
    {
    }
}
