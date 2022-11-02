using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models.Add_Ons.VSS
{
    [Table("FailedOVDSServerCommands")]
    public class DbFailedOVDSServerCommand : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "FailedOVDSServerCommands";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }
        public long OVDSServerCommandId { get; set; }
        public string Command { get; set; }
        public string FailureMessage { get; set; }
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
