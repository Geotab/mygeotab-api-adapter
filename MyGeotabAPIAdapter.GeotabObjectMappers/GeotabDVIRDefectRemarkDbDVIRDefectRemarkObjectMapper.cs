using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DefectRemark"/> and <see cref="DbDVIRDefectRemark"/> entities.
    /// </summary>
    public class GeotabDVIRDefectRemarkDbDVIRDefectRemarkObjectMapper : IGeotabDVIRDefectRemarkDbDVIRDefectRemarkObjectMapper
    {
        /// <inheritdoc/>
        public DbDVIRDefectRemark CreateEntity(DefectRemark defectRemark, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            DbDVIRDefectRemark dbDVIRDefectRemark = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DVIRDefectId = defectRemark.DVIRDefect.Id.ToString(),
                EntityStatus = (int)entityStatus,
                GeotabId = defectRemark.Id.ToString(),
                RecordLastChangedUtc = DateTime.UtcNow,
                Remark = defectRemark.Remark
            };
            if (defectRemark.DateTime != null)
            {
                dbDVIRDefectRemark.DateTime = defectRemark.DateTime;
            }
            if (defectRemark.User != null)
            {
                dbDVIRDefectRemark.RemarkUserId = defectRemark.User.Id.ToString();
            }

            return dbDVIRDefectRemark;
        }
    }
}
