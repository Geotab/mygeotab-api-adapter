#nullable enable
using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("ZoneTypes2")]
    public class DbZoneType2 : IDbEntity, IIdCacheableDbEntity, IStatusableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "ZoneTypes2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => RecordLastChangedUtc; }

        [Key]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public string Comment { get; set; }
        [Write(false)]
        public string CommentOracle { get => Comment; }
        public string Name { get; set; }
        public int EntityStatus { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
