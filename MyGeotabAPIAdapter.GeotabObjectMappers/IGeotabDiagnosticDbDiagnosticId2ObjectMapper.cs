using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Diagnostic"/> and <see cref="DbDiagnosticId2"/> entities.
    /// </summary>
    public interface IGeotabDiagnosticDbDiagnosticId2ObjectMapper : IGeotabObjectMapper<Diagnostic, DbDiagnosticId2>
    {
        /// <summary>
        /// Creates and returns a new <see cref="DbDiagnosticId2"/> entity using property values of the <paramref name="entityToMapTo"/>. Then, sets the <see cref="DbDiagnosticId2.FormerShimGeotabGUIDString"/> property to the <paramref name="formerShimGeotabGUID"/> value.
        /// </summary>
        /// <param name="entityToMapTo">The entity from which property values should be used to populate the new <see cref="DbDiagnosticId2"/> entity.</param>
        /// <param name="formerShimGeotabGUID"></param>The value to set the <see cref="DbDiagnosticId2.FormerShimGeotabGUIDString"/> property to.</param>
        DbDiagnosticId2 CreateEntity(Diagnostic entityToMapTo, string formerShimGeotabGUID);
    }
}
