using MyGeotabAPIAdapter.Database.Models;
using System;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="DbDVIRDefectUpdate"/> and <see cref="DbFailedDVIRDefectUpdate"/> entities.
    /// </summary>
    public class DbUpdDVIRDefectUpdate2DbFailDVIRDefectUpdateFailure2EntityMapper : IDbUpdDVIRDefectUpdate2DbFailDVIRDefectUpdateFailure2EntityMapper
    {
        /// <inheritdoc/>
        public DbFailDVIRDefectUpdateFailure2 CreateEntity(DbUpdDVIRDefectUpdate2 dbUpdDVIRDefectUpdate2, string failureMessage)
        {
            DbFailDVIRDefectUpdateFailure2 dbFailDVIRDefectUpdateFailure2 = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DVIRDefectId = dbUpdDVIRDefectUpdate2.DVIRDefectId,
                DVIRDefectUpdateId = dbUpdDVIRDefectUpdate2.id,
                DVIRLogId = dbUpdDVIRDefectUpdate2.DVIRLogId,
                FailureMessage = failureMessage,
                RecordCreationTimeUtc = DateTime.UtcNow,
                RemarkDateTimeUtc = dbUpdDVIRDefectUpdate2.RemarkDateTimeUtc,
                Remark = dbUpdDVIRDefectUpdate2.Remark,
                RemarkUserId = dbUpdDVIRDefectUpdate2.RemarkUserId,
                RepairDateTimeUtc = dbUpdDVIRDefectUpdate2.RepairDateTimeUtc,
                RepairStatusId = dbUpdDVIRDefectUpdate2.RepairStatusId,
                RepairUserId = dbUpdDVIRDefectUpdate2.RepairUserId
            };
            return dbFailDVIRDefectUpdateFailure2;
        }
    }
}
