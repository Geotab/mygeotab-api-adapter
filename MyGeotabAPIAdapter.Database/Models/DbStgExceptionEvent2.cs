using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("stg_ExceptionEvents2")]
    public class DbStgExceptionEvent2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "stg_ExceptionEvents2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
        public Guid id { get; set; }
        public string GeotabId { get; set; }
        public DateTime ActiveFrom { get; set; }
        public DateTime? ActiveTo { get; set; }
        public long DeviceId { get; set; }
        public float? Distance { get; set; }
        public long? DriverId { get; set; }
        [Write(false)]
        public TimeSpan? Duration
        {
            get { return TimeSpan.FromTicks(DurationTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    DurationTicks = value.Value.Ticks;
                }
            }
        }
        public long? DurationTicks { get; set; }
        public DateTime? LastModifiedDateTime { get; set; }
        public string RuleGeotabId { get; set; }
        public int State { get; set; }
        public long? Version { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
