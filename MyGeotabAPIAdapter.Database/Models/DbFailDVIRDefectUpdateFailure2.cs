using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("fail_DVIRDefectUpdateFailures2")]
    public class DbFailDVIRDefectUpdateFailure2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "fail_DVIRDefectUpdateFailures2";
        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }
#pragma warning disable IDE1006 // Naming Styles
        [Key]
        public long id { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        public long DVIRDefectUpdateId { get; set; }
        public Guid DVIRLogId { get; set; }
        public Guid DVIRDefectId { get; set; }
        public DateTime? RepairDateTimeUtc { get; set; }
        public short? RepairStatusId { get; set; }
        public long? RepairUserId { get; set; }
        public string Remark { get; set; }
        public DateTime? RemarkDateTimeUtc { get; set; }
        public long? RemarkUserId { get; set; }
        public string FailureMessage { get; set; }
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
