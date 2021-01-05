using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("vwRuleObject")]
    public class DbVwRuleObject
    {
        public long RuleAdapterId { get; set; }
        public string GeotabId { get; set; }
        public string Name { get; set; }
        public string BaseType { get; set; }
        public DateTime? ActiveFrom { get; set; }
        public DateTime? ActiveTo { get; set; }
        public string Comment { get; set; }
        public long? Version { get; set; }
        public int EntityStatus { get; set; }
        public DateTime RecordLastChangedUtc { get; set; }
        public long ConditionAdapterId { get; set; }
        public string Cond_Id { get; set; }
        public string Cond_ParentId { get; set; }
        public string Cond_RuleId { get; set; }
        public string Cond_ConditionType { get; set; }
        public string Cond_DeviceId { get; set; }
        public string Cond_DiagnosticId { get; set; }
        public string Cond_DriverId { get; set; }
        public double? Cond_Value { get; set; }
        public string Cond_WorkTimeId { get; set; }
        public string Cond_ZoneId { get; set; }
        public int Cond_EntityStatus { get; set; }
        public DateTime Cond_RecordLastChangedUtc { get; set; }
    }
}
