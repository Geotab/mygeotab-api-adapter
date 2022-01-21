using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("FailedOVDSServerCommands")]
    public class DbFailedOVDSServerCommand
    {
        [Key]
        public long id { get; set; }
        public long OVDSServerCommandId { get; set; }
        public string Command { get; set; }
        public string FailureMessage { get; set; }
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }
    }
}
