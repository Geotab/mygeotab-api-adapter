using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("vwStatusDataTWithLagLeadLongLatBatch")]
    public class DbStatusDataTWithLagLeadLongLat : IDbEntity, IIdCacheableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "vwStatusDataTWithLagLeadLongLatBatch";

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
        public double? LagSpeed { get; set; }
        public DateTime? LeadDateTime { get; set; }
        public double? LeadLatitude { get; set; }
        public double? LeadLongitude { get; set; }
        public DateTime? LogRecordsTMinDateTime { get; set; }
        public DateTime? LogRecordsTMaxDateTime { get; set; }
        public DateTime? DeviceLogRecordsTMinDateTime { get; set; }
        public DateTime? DeviceLogRecordsTMaxDateTime { get; set; }
    }
}
