using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("OVDSServerCommands")]
    public class DbOVDSServerCommand
    {
        [Key]
        public long id { get; set; }
        public string Command { get; set; }
        public DateTime RecordCreationTimeUtc { get; set; }
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }
    }
}
