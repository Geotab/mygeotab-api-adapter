using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("ZoneTypes")]
    public class DbZoneType
    {
        [Key]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public string Comment { get; set; }
        public string Name { get; set; }
        public int EntityStatus { get; set; }
        public DateTime RecordLastChangedUtc { get; set; }
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }
    }
}
