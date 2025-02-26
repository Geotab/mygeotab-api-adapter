using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="Diagnostic"/> and <see cref="DbDiagnosticId2"/> entities.
    /// </summary>
    public class GeotabDiagnosticDbDiagnosticId2ObjectMapper : IGeotabDiagnosticDbDiagnosticId2ObjectMapper
    {
        readonly IStringHelper stringHelper;

        public GeotabDiagnosticDbDiagnosticId2ObjectMapper(IStringHelper stringHelper)
        {
            this.stringHelper = stringHelper;
        }

        /// <inheritdoc/>
        public DbDiagnosticId2 CreateEntity(Diagnostic entityToMapTo)
        {
            var diagnosticId = entityToMapTo.Id;
            var geotabGUID = diagnosticId.GetValue().ToString();
            var isShimId = (diagnosticId.GetType().Name == "ShimId");

            DbDiagnosticId2 dbDiagnosticId2 = new()
            {
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                GeotabGUIDString = geotabGUID,
                GeotabId = entityToMapTo.Id.ToString(),
                HasShimId = isShimId,
                FormerShimGeotabGUIDString = null,
                RecordLastChangedUtc = DateTime.UtcNow
            };

            return dbDiagnosticId2;
        }

        /// <inheritdoc/>
        public DbDiagnosticId2 CreateEntity(Diagnostic entityToMapTo, string formerShimGeotabGUID)
        {
            DbDiagnosticId2 dbDiagnosticId2 = CreateEntity(entityToMapTo);
            dbDiagnosticId2.FormerShimGeotabGUIDString = formerShimGeotabGUID;
            return dbDiagnosticId2;
        }

        /// <inheritdoc/>
        public bool EntityRequiresUpdate(DbDiagnosticId2 entityToEvaluate, Diagnostic entityToMapTo)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public DbDiagnosticId2 UpdateEntity(DbDiagnosticId2 entityToUpdate, Diagnostic entityToMapTo)
        {
            throw new NotImplementedException();
        }
    }
}
