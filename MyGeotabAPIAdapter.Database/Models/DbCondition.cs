#nullable enable
using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("Conditions")]
    public class DbCondition : IDbEntity, IIdCacheableDbEntity, IStatusableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "Conditions";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => RecordLastChangedUtc; }

        [Key]
        public long id { get; set; }
        public string GeotabId { get; set; }
        // Conditions may have embedded/subconditions and they are reflected in this field
        public string ParentId { get; set; }
        // Conditions can be linked to a Rule directly, RuleId displays this link
        public string RuleId { get; set; }
        public string ConditionType { get; set; }
        public string DeviceId { get; set; }
        public string DiagnosticId { get; set; }
        public string DriverId { get; set; }
        public double? Value { get; set; }
        public string WorkTimeId { get; set; }
        public string ZoneId { get; set; }
        public int EntityStatus { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
