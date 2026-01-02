#nullable enable
using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("stg_Zones2")]
    public class DbStgZone2 : IDbEntity, IIdCacheableDbEntity, IStatusableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "stg_Zones2";

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
        public double? CentroidLatitude { get; set; }
        public double? CentroidLongitude { get; set; }
        public string Comment { get; set; }
        public bool? Displayed { get; set; }
        public string ExternalReference { get; set; }
        public string? Groups { get; set; }
        public bool? MustIdentifyStops { get; set; }
        public string Name { get; set; }
        public string Points { get; set; }
        public string ZoneTypeIds { get; set; }
        public long? Version { get; set; }
        public int EntityStatus { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
