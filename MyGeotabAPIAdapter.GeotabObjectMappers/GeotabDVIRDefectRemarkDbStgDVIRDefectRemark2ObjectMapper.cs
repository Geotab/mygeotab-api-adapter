using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Enums;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DefectRemark"/> and <see cref="DbStgDVIRDefectRemark2"/> entities.
    /// </summary>
    public class GeotabDVIRDefectRemarkDbStgDVIRDefectRemark2ObjectMapper : IGeotabDVIRDefectRemarkDbStgDVIRDefectRemark2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;

        public GeotabDVIRDefectRemarkDbStgDVIRDefectRemark2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }

        /// <inheritdoc/>
        public DbStgDVIRDefectRemark2 CreateEntity(DbStgDVIRDefect2 dbStgDVIRDefect2, DefectRemark defectRemark)
        {
            DbStgDVIRDefectRemark2 dbStgDVIRDefectRemark2 = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                id = geotabIdConverter.ToGuid(defectRemark.Id),
                GeotabId = defectRemark.Id.ToString(),
                DVIRDefectId = dbStgDVIRDefect2.id,
                DVIRLogDateTime = dbStgDVIRDefect2.DVIRLogDateTime,
                DateTime = (DateTime)defectRemark.DateTime,
                Remark = defectRemark.Remark,
                RecordLastChangedUtc = DateTime.UtcNow
            };

            long? defectRemarkRepairUserId = null;
            if (defectRemark.User != null)
            {
                if (defectRemark.User.GetType() == typeof(NoDriver))
                {
                    defectRemarkRepairUserId = AdapterDbSentinelIdsForMYGKnownIds.NoDriverId;
                }
                else if (defectRemark.User.GetType() == typeof(UnknownDriver))
                {
                    defectRemarkRepairUserId = AdapterDbSentinelIdsForMYGKnownIds.UnknownDriverId;
                }
                else if (defectRemark.User.GetType() == typeof(NoUser))
                {
                    defectRemarkRepairUserId = AdapterDbSentinelIdsForMYGKnownIds.NoUserId;
                }
                else if (defectRemark.User.Id != null)
                {
                    defectRemarkRepairUserId = geotabIdConverter.ToLong(defectRemark.User.Id);
                }
            }
            dbStgDVIRDefectRemark2.RemarkUserId = defectRemarkRepairUserId;

            return dbStgDVIRDefectRemark2;
        }
    }
}
