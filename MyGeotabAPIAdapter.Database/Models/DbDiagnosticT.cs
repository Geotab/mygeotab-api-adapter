#nullable enable
using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("DiagnosticsT")]
    public class DbDiagnosticT : IDbEntity, IIdCacheableDbEntity, IGeotabGUIDCacheableDbEntity, IStatusableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "DiagnosticsT";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => RecordLastChangedUtc; }

        [Write(false)]
        public long id { get; set; }
        /// <summary>
        /// NOTE: The <see cref="GeotabId"/> property is included in the <see cref="DbDiagnosticT"/> class only to satisfy the <see cref="IIdCacheableDbEntity"/> interface. The corresponding column does not exist in the database table. The <see cref="GeotabGUID"/> property is the actual ExplicitKey for the <see cref="DbDiagnosticT"/>. If used, this property will simply relay to/from the <see cref="GeotabGUID"/> property.
        /// </summary>
        [Computed]
        public string GeotabId
        {
            get => GeotabGUID; 
            set
            {
                GeotabGUID = value;
            }
        }
        [ExplicitKey]
        public string GeotabGUID { get; set; }
        public bool HasShimId { get; set; }
        public string FormerShimGeotabGUID { get; set; }
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
