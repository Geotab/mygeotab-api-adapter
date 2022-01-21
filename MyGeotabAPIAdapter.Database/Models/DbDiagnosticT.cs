using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("DiagnosticsT")]
    public class DbDiagnosticT : IDbEntity, IIdCacheableDbEntity
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
        [ExplicitKey]
        public string GeotabId { get; set; }
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
