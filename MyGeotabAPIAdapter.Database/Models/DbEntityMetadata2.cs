using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("EntityMetadata2")]
    public class DbEntityMetadata2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "EntityMetadata2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }
        public DateTime DateTime { get; set; }
        public long DeviceId { get; set; }
        public long EntityId { get; set; }
        public byte EntityType { get; set; }
        public bool? IsDeleted { get; set; }
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
