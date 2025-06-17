using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
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

            if (defectRemark.User != null)
            {
                dbStgDVIRDefectRemark2.RemarkUserId = geotabIdConverter.ToLong(defectRemark.User.Id);
            }

            return dbStgDVIRDefectRemark2;
        }
    }
}
