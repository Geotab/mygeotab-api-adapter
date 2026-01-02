#nullable enable
using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("stg_Devices2")]
    public class DbStgDevice2 : IDbEntity, IIdCacheableDbEntity, IStatusableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "stg_Devices2";

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
        public string DeviceType { get; set; }
        public string? Groups { get; set; }
        public string LicensePlate { get; set; }
        public string LicenseState { get; set; }
        public string Name { get; set; }
        public int? ProductId { get; set; }
        public string SerialNumber { get; set; }
        public string? TmpTrailerGeotabId { get; set; }
        public Guid? TmpTrailerId { get; set; }
        public string VIN { get; set; }
        public int EntityStatus { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
