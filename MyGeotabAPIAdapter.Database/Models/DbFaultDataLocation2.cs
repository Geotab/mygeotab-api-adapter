using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("FaultDataLocations2")]
    public class DbFaultDataLocation2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "FaultDataLocations2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
        public long id { get; set; }
        public long DeviceId { get; set; }
        public DateTime DateTime { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public float? Speed { get; set; }
        public float? Bearing { get; set; }
        public string? Direction { get; set; }
        public bool LongLatProcessed { get; set; }
        public byte? LongLatReason { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
