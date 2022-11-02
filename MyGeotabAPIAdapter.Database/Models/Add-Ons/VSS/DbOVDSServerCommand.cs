using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models.Add_Ons.VSS
{
    [Table("OVDSServerCommands")]
    public class DbOVDSServerCommand : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "OVDSServerCommands";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }
        public string Command { get; set; }
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
