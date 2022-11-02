using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("FailedDVIRDefectUpdates")]
    public class DbFailedDVIRDefectUpdate : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "FailedDVIRDefectUpdates";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }
        public long DVIRDefectUpdateId { get; set; }
        public string DVIRLogId { get; set; }
        public string DVIRDefectId { get; set; }
        public DateTime? RepairDateTime { get; set; }
        public string RepairStatus { get; set; }
        public string RepairUserId { get; set; }
        public string Remark { get; set; }
        public DateTime? RemarkDateTime { get; set; }
        public string RemarkUserId { get; set; }
        public string FailureMessage { get; set; }
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
