using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("Diagnostics")]
    public class DbDiagnostic
    {
        [ExplicitKey]
        public string Id { get; set; }
        public int? DiagnosticCode { get; set; }
        public string DiagnosticName { get; set; }
        public string DiagnosticSourceId { get; set; }
        public string DiagnosticSourceName { get; set; }
        public string DiagnosticUnitOfMeasureId { get; set; }
        public string DiagnosticUnitOfMeasureName { get; set; }
        public int EntityStatus { get; set; }
        public DateTime RecordLastChangedUtc { get; set; }
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }
    }
}
