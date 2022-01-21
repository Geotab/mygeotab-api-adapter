#nullable enable
using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("BinaryDataT")]
    public class DbBinaryDataT : IDbEntity, IIdCacheableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "DriverChangeTypesT";

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
        public long BinaryTypeId { get; set; }
        public long ControllerId { get; set; }
        public string Data { get; set; }
        public DateTime? DateTime { get; set; }
        public long DeviceId { get; set; }
        public string? Version { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
