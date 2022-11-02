using MyGeotabAPIAdapter.Database.Models;
using System;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DbDVIRDefectUpdate"/> and <see cref="DbFailedDVIRDefectUpdate"/> entities.
    /// </summary>
    public class DbDVIRDefectUpdateDbFailedDVIRDefectUpdateEntityMapper : IDbDVIRDefectUpdateDbFailedDVIRDefectUpdateEntityMapper
    {
        /// <inheritdoc/>
        public DbFailedDVIRDefectUpdate CreateEntity(DbDVIRDefectUpdate dbDVIRDefectUpdate, string failureMessage)
        {
            DbFailedDVIRDefectUpdate dbFailedDVIRDefectUpdate = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DVIRDefectId = dbDVIRDefectUpdate.DVIRDefectId,
                DVIRDefectUpdateId = dbDVIRDefectUpdate.id,
                DVIRLogId = dbDVIRDefectUpdate.DVIRLogId,
                FailureMessage = failureMessage,
                RecordCreationTimeUtc = DateTime.UtcNow,
                RemarkDateTime = dbDVIRDefectUpdate.RemarkDateTime,
                Remark = dbDVIRDefectUpdate.Remark,
                RemarkUserId = dbDVIRDefectUpdate.RemarkUserId,
                RepairDateTime = dbDVIRDefectUpdate.RepairDateTime,
                RepairStatus = dbDVIRDefectUpdate.RepairStatus,
                RepairUserId = dbDVIRDefectUpdate.RepairUserId
            };
            return dbFailedDVIRDefectUpdate;
        }
    }
}
