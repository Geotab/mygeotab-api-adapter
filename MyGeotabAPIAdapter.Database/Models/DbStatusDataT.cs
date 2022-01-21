using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("StatusDataT")]
    public class DbStatusDataT : IDbEntity, IIdCacheableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "StatusDataT";

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
        public double? Data { get; set; }
        public DateTime? DateTime { get; set; }
        public long DeviceId { get; set; }
        public long DiagnosticId { get; set; }
        public long? DriverId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double Speed { get; set; }
        public double Bearing { get; set; }
        public string Direction { get; set; }
        public bool LongLatProcessed { get; set; }
        public byte? LongLatReason { get; set; }
        public bool DriverIdProcessed { get; set; }
        public byte? DriverIdReason { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
