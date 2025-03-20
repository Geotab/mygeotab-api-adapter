using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="Diagnostic"/> and <see cref="DbStgDiagnostic2"/> entities.
    /// </summary>
    public class GeotabDiagnosticDbStgDiagnostic2ObjectMapper : IGeotabDiagnosticDbStgDiagnostic2ObjectMapper
    {
        // OBD-II DTC prefixes associated with Controller Ids.
        const string OBD2DTCPrefixBody = "B";
        const string OBD2DTCPrefixChassis = "C";
        const string OBD2DTCPrefixNetworking = "U";
        const string OBD2DTCPrefixPowertrain = "P";

        public GeotabDiagnosticDbStgDiagnostic2ObjectMapper()
        {
        }

        /// <inheritdoc/>
        public List<DbStgDiagnostic2> CreateEntities(List<Diagnostic> entitiesToMapTo)
        {
            var dbStgDiagnostic2s = new List<DbStgDiagnostic2>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbStgDiagnostic2 = CreateEntity(entity);
                dbStgDiagnostic2s.Add(dbStgDiagnostic2);
            }
            return dbStgDiagnostic2s;
        }

        /// <inheritdoc/>
        public DbStgDiagnostic2 CreateEntity(Diagnostic entityToMapTo)
        {
            Source diagnosticSource = entityToMapTo.Source;
            UnitOfMeasure diagnosticUnitOfMeasure = entityToMapTo.UnitOfMeasure;
            Controller diagnosticController = entityToMapTo.Controller;
            var diagnosticId = entityToMapTo.Id;
            var geotabGUID = diagnosticId.GetValue().ToString();
            var isShimId = (diagnosticId.GetType().Name == "ShimId");

            DbStgDiagnostic2 dbStgDiagnostic2 = new()
            {
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                DiagnosticCode = entityToMapTo.Code,
                DiagnosticName = entityToMapTo.Name,
                DiagnosticSourceId = diagnosticSource.Id.ToString(),
                DiagnosticSourceName = diagnosticSource.Name,
                DiagnosticUnitOfMeasureId = diagnosticUnitOfMeasure.Id.ToString(),
                DiagnosticUnitOfMeasureName = diagnosticUnitOfMeasure.Name,
                EntityStatus = (int)Common.DatabaseRecordStatus.Active, // Set to Active by default. Database logic will handle setting to inactive if necessary (i.e. for detected deletions on the MYG side).
                GeotabId = entityToMapTo.Id.ToString(),
                GeotabGUIDString = geotabGUID,
                HasShimId = isShimId,
                RecordLastChangedUtc = DateTime.UtcNow
            };

            if (diagnosticController != null)
            {
                dbStgDiagnostic2.ControllerId = diagnosticController.Id.ToString();

                // Derive the OBD-II Diagnostic Trouble Code (DTC), if applicable.
                if (dbStgDiagnostic2.DiagnosticSourceId == KnownId.SourceObdId.ToString() || dbStgDiagnostic2.DiagnosticSourceId == KnownId.SourceObdSaId.ToString())
                {
                    int diagnosticCode;
                    if (entityToMapTo.Code != null)
                    {
                        diagnosticCode = (int)entityToMapTo.Code;
                        string dtcPrefix = "";
                        switch (diagnosticController.Id.ToString())
                        {
                            case nameof(KnownId.ControllerObdPowertrainId):
                                dtcPrefix = OBD2DTCPrefixPowertrain;
                                break;
                            case nameof(KnownId.ControllerObdWwhPowertrainId):
                                dtcPrefix = OBD2DTCPrefixPowertrain;
                                break;
                            case nameof(KnownId.ControllerObdBodyId):
                                dtcPrefix = OBD2DTCPrefixBody;
                                break;
                            case nameof(KnownId.ControllerObdWwhBodyId):
                                dtcPrefix = OBD2DTCPrefixBody;
                                break;
                            case nameof(KnownId.ControllerObdChassisId):
                                dtcPrefix = OBD2DTCPrefixChassis;
                                break;
                            case nameof(KnownId.ControllerObdWwhChassisId):
                                dtcPrefix = OBD2DTCPrefixChassis;
                                break;
                            case nameof(KnownId.ControllerObdNetworkingId):
                                dtcPrefix = OBD2DTCPrefixNetworking;
                                break;
                            case nameof(KnownId.ControllerObdWwhNetworkingId):
                                dtcPrefix = OBD2DTCPrefixNetworking;
                                break;
                            default:
                                break;
                        }
                        if (dtcPrefix.Length > 0)
                        {
                            string dtc = Convert.ToString(diagnosticCode, 16).PadLeft(4, '0');
                            dbStgDiagnostic2.OBD2DTC = $"{dtcPrefix}{dtc.ToUpper()}";
                        }
                    }
                }
            }
            return dbStgDiagnostic2;
        }
    }
}
