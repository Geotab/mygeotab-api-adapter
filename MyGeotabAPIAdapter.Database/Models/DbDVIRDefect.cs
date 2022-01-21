using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("DVIRDefects")]
    public class DbDVIRDefect
    {
        [Key]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public string DVIRLogId { get; set; }
        public string DefectListAssetType { get; set; }
        public string DefectListId { get; set; }
        public string DefectListName { get; set; }
        public string PartId { get; set; }
        public string PartName { get; set; }
        public string DefectId { get; set; }
        public string DefectName { get; set; }
        public string DefectSeverity { get; set; }
        public DateTime? RepairDateTime { get; set; }
        public string RepairStatus { get; set; }
        public string RepairUserId { get; set; }
        public int EntityStatus { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }
    }
}
