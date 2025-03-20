#nullable enable
using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("stg_Diagnostics2")]
    public class DbStgDiagnostic2 : IDbEntity, IIdCacheableDbEntity, IGeotabGUIDCacheableDbEntity, IStatusableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "stg_Diagnostics2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => RecordLastChangedUtc; }

        [Key]
        public long id { get; set; }
        public string GeotabId { get; set; }
        [Write(false)]
        public Guid? GeotabGUID { get; set; }
        public string GeotabGUIDString { get; set; }
        public bool HasShimId { get; set; }
        public string FormerShimGeotabGUIDString { get; set; }
        public string ControllerId { get; set; }
        public int? DiagnosticCode { get; set; }
        public string DiagnosticName { get; set; }
        public string DiagnosticSourceId { get; set; }
        public string DiagnosticSourceName { get; set; }
        public string DiagnosticUnitOfMeasureId { get; set; }
        public string DiagnosticUnitOfMeasureName { get; set; }
        public string OBD2DTC { get; set; }
        public int EntityStatus { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
