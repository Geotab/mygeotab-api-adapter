using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// Rules are a powerful tool of MyGeotab used to generate exception events based on predefined condition. 
    /// A rule’s conditions are evaluated against either the assets who are members of the rules groups or the specific assets defined in the condition. 
    /// When the conditions are violated an exception event is generated. 
    /// Rules can combine many types of data including speed, distance, location, auxiliaries, engine status data, engine fault data and more.
    /// </summary>
    [Table("Rules")]
    public class DbRule
    {
        [Key]
        public long id { get; set; }
        // The unique identifier for this entity.
        public string GeotabId { get; set; }
        // The name of the rule entity that uniquely identifies it and is used when displaying this entity.
        public string Name { get; set; }
        // The ExceptionRuleBaseType of the rule; either Custom, Stock or ZoneStop.
        public string BaseType { get; set; }
        // Start date of the Rule's notification activity period.
        // The events with earlier date than this date will not be reported through the notification engine.
        // Required
        public DateTime? ActiveFrom { get; set; }
        // End date of the Rule's notification activity period.
        // Required
        public DateTime? ActiveTo { get; set; }
        // Free text field where any user information can be stored and referenced for this entity.
        public string Comment { get; set; }
        // The version of the entity.
        public long? Version { get; set; }
        public int EntityStatus { get; set; }
        public DateTime RecordLastChangedUtc { get; set; }
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }
    }
}
