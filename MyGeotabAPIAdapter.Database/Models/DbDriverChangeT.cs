using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("DriverChangesT")]
    public class DbDriverChangeT : IDbEntity, IIdCacheableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "DriverChangesT";

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
        public DateTime? DateTime { get; set; }
        public long DeviceId { get; set; }
        public long DriverId { get; set; }
        public long DriverChangeTypeId { get; set; }
        public long Version { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
