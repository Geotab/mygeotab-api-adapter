using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("DiagnosticIdsT")]
    public class DbDiagnosticIdT : IDbEntity, IIdCacheableDbEntity, IGeotabGUIDCacheableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "DiagnosticIdsT";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => RecordLastChangedUtc; }

        [Key]
        public long id { get; set; }
        [Write(false)]
        public Guid? GeotabGUID { get; set; }
        public string GeotabGUIDString { get; set; }
        public string GeotabId { get; set; }
        public bool HasShimId { get; set; }
        public string FormerShimGeotabGUID { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
