#nullable enable
using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("Groups")]
    public class DbGroup : IDbEntity, IIdCacheableDbEntity, IStatusableDbEntity
    //public class DbGroup : IDbEntity, IIdCacheableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "Groups";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => RecordLastChangedUtc; }

        [Key]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public string? Children { get; set; }
        public string? Color { get; set; }
        public string? Comments { get; set; }
        public string? Name { get; set; }
        public string? Reference { get; set; }
        public int EntityStatus { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}