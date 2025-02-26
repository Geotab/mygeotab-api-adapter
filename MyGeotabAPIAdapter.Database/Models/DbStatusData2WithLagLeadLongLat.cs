using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("spStatusData2WithLagLeadLongLatBatch")]
    public class DbStatusData2WithLagLeadLongLat : IDbEntity, IIdCacheableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "spStatusData2WithLagLeadLongLatBatch";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => StatusDataDateTime; }

        [Write(false)]
        public long id { get; set; }
        [ExplicitKey]
        public string GeotabId { get; set; }
        public DateTime StatusDataDateTime { get; set; }
        public long DeviceId { get; set; }
        public DateTime? LagDateTime { get; set; }
        public double? LagLatitude { get; set; }
        public double? LagLongitude { get; set; }
        public float? LagSpeed { get; set; }
        public DateTime? LeadDateTime { get; set; }
        public double? LeadLatitude { get; set; }
        public double? LeadLongitude { get; set; }
        public DateTime? LogRecords2MinDateTime { get; set; }
        public DateTime? LogRecords2MaxDateTime { get; set; }
        public DateTime? DeviceLogRecords2MinDateTime { get; set; }
        public DateTime? DeviceLogRecords2MaxDateTime { get; set; }
    }
}
