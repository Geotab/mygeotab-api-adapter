using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="Diagnostic"/> and <see cref="DbDiagnostic"/> entities.
    /// </summary>
    public class GeotabDiagnosticDbDiagnosticObjectMapper : IGeotabDiagnosticDbDiagnosticObjectMapper
    {
        readonly IStringHelper stringHelper;

        public GeotabDiagnosticDbDiagnosticObjectMapper(IStringHelper stringHelper)
        { 
            this.stringHelper = stringHelper;
        }

        // OBD-II DTC prefixes associated with Controller Ids.
        const string OBD2DTCPrefixBody = "B";
        const string OBD2DTCPrefixChassis = "C";
        const string OBD2DTCPrefixNetworking = "U";
        const string OBD2DTCPrefixPowertrain = "P";

        /// <inheritdoc/>
        public DbDiagnostic CreateEntity(Diagnostic entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            Source diagnosticSource = entityToMapTo.Source;
            UnitOfMeasure diagnosticUnitOfMeasure = entityToMapTo.UnitOfMeasure;
            Controller diagnosticController = entityToMapTo.Controller;
            var diagnosticId = entityToMapTo.Id;
            var geotabGUID = diagnosticId.GetValue().ToString();
            var isShimId = (diagnosticId.GetType().Name == "ShimId");

            DbDiagnostic dbDiagnostic = new()
            {
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                DiagnosticCode = entityToMapTo.Code,
                DiagnosticName = entityToMapTo.Name,
                DiagnosticSourceId = diagnosticSource.Id.ToString(),
                DiagnosticSourceName = diagnosticSource.Name,
                DiagnosticUnitOfMeasureId = diagnosticUnitOfMeasure.Id.ToString(),
                DiagnosticUnitOfMeasureName = diagnosticUnitOfMeasure.Name,
                EntityStatus = (int)entityStatus,
                GeotabId = entityToMapTo.Id.ToString(),
                GeotabGUIDString = geotabGUID,
                HasShimId = isShimId,
                RecordLastChangedUtc = DateTime.UtcNow
            }; if (diagnosticController != null)
            {
                dbDiagnostic.ControllerId = diagnosticController.Id.ToString();

                // Derive the OBD-II Diagnostic Trouble Code (DTC), if applicable.
                if (dbDiagnostic.DiagnosticSourceId == KnownId.SourceObdId.ToString() || dbDiagnostic.DiagnosticSourceId == KnownId.SourceObdSaId.ToString())
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
                            dbDiagnostic.OBD2DTC = $"{dtcPrefix}{dtc.ToUpper()}";
                        }
                    }
                }
            }
            return dbDiagnostic;
        }

        /// <summary>
        /// Returns <c>true</c> if the <paramref name="diagnostic"/> <see cref="Diagnostic.Id"/> is different from the <paramref name="dbDiagnostic"/> <see cref="DbDiagnostic.GeotabId"/>, but the underlying GUIDs are the same. Otherwise, returns <c>false</c>. A return value of <c>false</c> does not necessarily mean that the Diagnostic Id has changed, since it is possible for mis-matched <paramref name="dbDiagnostic"/> and <paramref name="diagnostic"/> to be supplied as inputs. Intended to assist in identifying <see cref="Diagnostic"/>s whose <see cref="Diagnostic.Id"/>s have been changed as a result of the assignment of a <see cref="KnownId"/>.
        /// </summary>
        /// <param name="dbDiagnostic">The <see cref="DbDiagnostic"/> to be evaluated.</param>
        /// <param name="diagnostic">The <see cref="Diagnostic"/> to be evaluated.</param>
        /// <returns></returns>
        static bool DiagnosticIdHasChanged(DbDiagnostic dbDiagnostic, Diagnostic diagnostic)
        {
            if (dbDiagnostic.GeotabId != diagnostic.Id.ToString())
            {
                // The Id may have changed due to a new KnownId being assigned to the Diagnostic. Check whether this is the case and only throw an exception if not.
                var dbDiagnosticGeotabId = Id.Create(dbDiagnostic.GeotabId);
                var dbDiagnosticGeotabGUID = dbDiagnosticGeotabId.GetValue().ToString();
                var diagnosticGeotabGUID = diagnostic.Id.GetValue().ToString();
                if (diagnosticGeotabGUID == dbDiagnosticGeotabGUID)
                {
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public bool EntityRequiresUpdate(DbDiagnostic entityToEvaluate, Diagnostic entityToMapTo)
        {
            if (entityToEvaluate.GeotabId != entityToMapTo.Id.ToString())
            {
                // The Id may have changed due to a new KnownId being assigned to the Diagnostic. Check whether this is the case and only throw an exception if not.
                var diagnosticIdHasChanged = DiagnosticIdHasChanged(entityToEvaluate, entityToMapTo);
                if (diagnosticIdHasChanged)
                {
                    return true;
                }
                throw new ArgumentException($"Cannot compare {nameof(DbDiagnostic)} '{entityToEvaluate.id}' with {nameof(Diagnostic)} '{entityToMapTo.Id}' because the IDs do not match.");
            }

            Source diagnosticSource = entityToMapTo.Source;
            UnitOfMeasure diagnosticUnitOfMeasure = entityToMapTo.UnitOfMeasure;
            if (entityToEvaluate.DiagnosticCode != entityToMapTo.Code || stringHelper.AreEqual(entityToEvaluate.DiagnosticName, entityToMapTo.Name) == false || entityToEvaluate.DiagnosticSourceId != diagnosticSource.Id.ToString() || stringHelper.AreEqual(entityToEvaluate.DiagnosticSourceName, diagnosticSource.Name) == false || entityToEvaluate.DiagnosticUnitOfMeasureId != diagnosticUnitOfMeasure.Id.ToString() || stringHelper.AreEqual(entityToEvaluate.DiagnosticUnitOfMeasureName, diagnosticUnitOfMeasure.Name) == false)
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public DbDiagnostic UpdateEntity(DbDiagnostic entityToUpdate, Diagnostic entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            if (entityToUpdate.GeotabId != entityToMapTo.Id.ToString())
            {
                throw new ArgumentException($"Cannot update {nameof(DbDiagnostic)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(Diagnostic)} '{entityToMapTo.Id}' because the GeotabIds do not match.");
            }

            var updatedDbDiagnostic = CreateEntity(entityToMapTo);

            // Update id since id is auto-generated by the database on insert and is therefor not set by the CreateEntity method.
            updatedDbDiagnostic.id = entityToUpdate.id;
            updatedDbDiagnostic.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

            return updatedDbDiagnostic;
        }
    }
}
