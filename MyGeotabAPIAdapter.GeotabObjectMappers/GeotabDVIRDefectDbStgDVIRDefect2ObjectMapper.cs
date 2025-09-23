using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Enums;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DVIRDefect"/> and <see cref="DbStgDVIRDefect2"/> entities.
    /// </summary>
    public class GeotabDVIRDefectDbStgDVIRDefect2ObjectMapper : IGeotabDVIRDefectDbStgDVIRDefect2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;
        public GeotabDVIRDefectDbStgDVIRDefect2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }

        /// <inheritdoc/>
        public DbStgDVIRDefect2 CreateEntity(DbStgDVIRLog2 dbStgDVIRLog2, DVIRDefect dvirDefect, Defect defect, DefectListPartDefect2 defectListPartDefect2)
        {
            DbStgDVIRDefect2 dbStgDVIRDefect2 = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                id = geotabIdConverter.ToGuid(dvirDefect.Id),
                GeotabId = dvirDefect.Id.ToString(),
                DVIRLogId = dbStgDVIRLog2.id,
                DVIRLogDateTime = dbStgDVIRLog2.DateTime,
                DefectListAssetType = defectListPartDefect2.DefectListAssetType,
                DefectListId = defectListPartDefect2.DefectListID,
                DefectListName = defectListPartDefect2.DefectListName,
                PartId = defectListPartDefect2.PartID,
                PartName = defectListPartDefect2.PartName,
                DefectId = defect.Id.ToString(),
                DefectName = defectListPartDefect2.DefectName,
                DefectSeverityId = defectListPartDefect2.DefectSeverity,
                RepairDateTime = dvirDefect.RepairDateTime,
                RecordLastChangedUtc = DateTime.UtcNow
            };

            if (dvirDefect.RepairStatus != null)
            {
                dbStgDVIRDefect2.RepairStatusId = (short)dvirDefect.RepairStatus;
            }

            long? dvirDefectRepairUserId = null;
            if (dvirDefect.RepairUser != null)
            {
                if (dvirDefect.RepairUser.GetType() == typeof(NoDriver))
                {
                    dvirDefectRepairUserId = AdapterDbSentinelIdsForMYGKnownIds.NoDriverId;
                }
                else if (dvirDefect.RepairUser.GetType() == typeof(UnknownDriver))
                {
                    dvirDefectRepairUserId = AdapterDbSentinelIdsForMYGKnownIds.UnknownDriverId;
                }
                else if (dvirDefect.RepairUser.GetType() == typeof(NoUser))
                {
                    dvirDefectRepairUserId = AdapterDbSentinelIdsForMYGKnownIds.NoUserId;
                }
                else if (dvirDefect.RepairUser.Id != null)
                {
                    dvirDefectRepairUserId = geotabIdConverter.ToLong(dvirDefect.RepairUser.Id);
                }
            }
            dbStgDVIRDefect2.RepairUserId = dvirDefectRepairUserId;

            return dbStgDVIRDefect2;
        }
    }
}
