using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("DVIRDefectUpdates")]
    public class DbDVIRDefectUpdate
    {
        [Key]
        public long id { get; set; }
        public string DVIRLogId { get; set; }
        public string DVIRDefectId { get; set; }
        public DateTime? RepairDateTime { get; set; }
        public string RepairStatus { get; set; }
        public string RepairUserId { get; set; }
        public string Remark { get; set; }
        public DateTime? RemarkDateTime { get; set; }
        public string RemarkUserId { get; set; }
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }
    }
}
