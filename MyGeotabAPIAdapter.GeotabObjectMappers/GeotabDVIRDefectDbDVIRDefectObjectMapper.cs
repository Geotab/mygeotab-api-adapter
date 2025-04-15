using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DVIRDefect"/> and <see cref="DbDVIRDefect"/> entities.
    /// </summary>
    public class GeotabDVIRDefectDbDVIRDefectObjectMapper : IGeotabDVIRDefectDbDVIRDefectObjectMapper
    {
        readonly IDateTimeHelper dateTimeHelper;

        public GeotabDVIRDefectDbDVIRDefectObjectMapper(IDateTimeHelper dateTimeHelper)
        {
            this.dateTimeHelper = dateTimeHelper;
        }

        /// <inheritdoc/>
        public DbDVIRDefect CreateEntity(DVIRLog dvirLog, DVIRDefect dvirDefect, Defect defect, DefectListPartDefect defectListPartDefect, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            DbDVIRDefect dbDVIRDefect = new()
            {
                GeotabId = dvirDefect.Id.ToString(),
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DVIRLogId = dvirLog.Id.ToString(),
                DefectId = defect.Id.ToString(),
                DefectListAssetType = defectListPartDefect.DefectListAssetType,
                DefectListId = defectListPartDefect.DefectListID,
                DefectListName = defectListPartDefect.DefectListName,
                EntityStatus = (int)entityStatus,
                PartId = defectListPartDefect.PartID,
                PartName = defectListPartDefect.PartName,
                DefectName = defectListPartDefect.DefectName,
                DefectSeverity = defectListPartDefect.DefectSeverity,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            if (dvirDefect.RepairDateTime != null)
            {
                dbDVIRDefect.RepairDateTime = dvirDefect.RepairDateTime;
            }
            if (dvirDefect.RepairStatus != null)
            {
                dbDVIRDefect.RepairStatus = dvirDefect.RepairStatus.ToString();
            }
            User repairUser = dvirDefect.RepairUser;
            if (repairUser != null)
            {
                dbDVIRDefect.RepairUserId = repairUser.Id.ToString();
            }

            return dbDVIRDefect;
        }

        /// <inheritdoc/>
        public bool EntityRequiresUpdate(DbDVIRDefect dbDVIRDefect, DVIRDefect dvirDefect)
        {
            if (dbDVIRDefect.GeotabId != dvirDefect.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare {nameof(DbDVIRDefect)} '{dbDVIRDefect.id}' with {nameof(DVIRDefect)} '{dvirDefect.Id}' because the IDs do not match.");
            }

            DateTime entityToEvaluateRepairDateTime = dbDVIRDefect.RepairDateTime.GetValueOrDefault();
            DateTime entityToEvaluateRepairDateTimeUtc = entityToEvaluateRepairDateTime.ToUniversalTime();
            DateTime entityToMapToRepairDateTime = dvirDefect.RepairDateTime.GetValueOrDefault();

            // Rounding to milliseconds may occur at the database level, so round accordingly such that equality operation will work as expected.
            DateTime entityToEvaluateRepairDateTimeRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToEvaluateRepairDateTime);
            DateTime entityToEvaluateRepairDateTimeUtcRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToEvaluateRepairDateTimeUtc);
            DateTime entityToMapToRepairDateTimeRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToMapToRepairDateTime);

            if (entityToEvaluateRepairDateTimeRoundedToMilliseconds != entityToMapToRepairDateTimeRoundedToMilliseconds && entityToEvaluateRepairDateTimeUtcRoundedToMilliseconds != entityToMapToRepairDateTimeRoundedToMilliseconds)
            {
                return true;
            }
            User entityToMapToRepairUser = dvirDefect.RepairUser;
            if (entityToMapToRepairUser != null && dbDVIRDefect.RepairUserId != entityToMapToRepairUser.Id.ToString())
            {
                return true;
            }
            if (dvirDefect.RepairStatus != null && dbDVIRDefect.RepairStatus != dvirDefect.RepairStatus.ToString())
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public DbDVIRDefect UpdateEntity(DbDVIRDefect entityToUpdate, DVIRLog dvirLog, DVIRDefect dvirDefect, Defect defect, DefectListPartDefect defectListPartDefect, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            if (entityToUpdate.GeotabId != dvirDefect.Id.ToString())
            {
                throw new ArgumentException($"Cannot update {nameof(DbDVIRDefect)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(DVIRDefect)} '{dvirDefect.Id}' because the GeotabIds do not match.");
            }

            var updatedDbDVIRDefect = CreateEntity(dvirLog, dvirDefect, defect, defectListPartDefect);

            // Update id since id is auto-generated by the database on insert and is therefor not set by the CreateEntity method.
            updatedDbDVIRDefect.id = entityToUpdate.id;
            updatedDbDVIRDefect.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

            return updatedDbDVIRDefect;
        }
    }
}
