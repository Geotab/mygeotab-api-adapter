#nullable enable
using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("Devices2")]
    public class DbDevice2 : IDbEntity, IIdCacheableDbEntity, IStatusableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "Devices2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => RecordLastChangedUtc; }

        /// <summary>
        /// The <see cref="GeotabId"/> decoded to its <see cref="long"/> format.
        /// </summary>
        [ExplicitKey]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public DateTime? ActiveFrom { get; set; }
        public DateTime? ActiveTo { get; set; }
        public string? Comment { get; set; }
        [Write(false)]
        public string? CommentOracle { get => Comment; }
        public string DeviceType { get; set; }
        public string LicensePlate { get; set; }
        public string LicenseState { get; set; }
        public string Name { get; set; }
        public int? ProductId { get; set; }
        public string SerialNumber { get; set; }
        public string VIN { get; set; }
        public int EntityStatus { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
