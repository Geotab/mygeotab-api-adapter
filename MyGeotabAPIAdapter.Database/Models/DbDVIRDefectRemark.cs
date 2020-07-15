using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("DVIRDefectRemarks")]
    public class DbDVIRDefectRemark
    {
        [ExplicitKey]
        public string Id { get; set; }
        public string DVIRDefectId { get; set; }
        public DateTime? DateTime { get; set; }
        public string Remark { get; set; }
        public string RemarkUserId { get; set; }
        public int EntityStatus { get; set; }
        public DateTime RecordLastChangedUtc { get; set; }
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }
    }
}
