using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("AuditLogs2")]
    public class DbAuditLog2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "AuditLogs2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
#pragma warning disable IDE1006 // Naming Styles
        public Guid id { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        public string GeotabId { get; set; }
        public string Comment { get; set; }
        public DateTime DateTime { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public long? Version { get; set; }
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
