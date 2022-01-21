using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("Conditions")]
    public class DbCondition
    {
        /// <summary>
        /// Conditions model the logic that govern a Rule and can apply to many different types of data and entities. 
        /// Conditions are structured in hierarchical tree. 
        /// A condition's type defines the meaning of each condition in the tree and can be an operator, special operator, data or an asset.
        /// </summary>
        [Key]
        public long id { get; set; }
        public string GeotabId { get; set; }
        // Conditions may have embedded/sub conditions and they are reflected in this field
        public string ParentId { get; set; }
        // Conditions can be linked to a Rule directly, RuleId displays this link
        public string RuleId { get; set; }
        public string ConditionType { get; set; }
        // Device reference if it exists
        public string DeviceId { get; set; }
        // Diagnostic reference if it exists
        public string DiagnosticId { get; set; }
        // Driver reference if it exists
        public string DriverId { get; set; }
        public double? Value { get; set; }
        // WorkTime reference if it exists
        public string WorkTimeId { get; set; }
        // Zone reference if it exists
        public string ZoneId { get; set; }
        public int EntityStatus { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }
    }
}
