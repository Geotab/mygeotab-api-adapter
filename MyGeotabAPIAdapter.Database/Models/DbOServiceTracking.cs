#nullable enable
using Dapper.Contrib.Extensions;
using System;
using System.Globalization;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("OServiceTracking")]
    public class DbOServiceTracking : IDbEntity, IIdCacheableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "OServiceTracking";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => RecordLastChangedUtc; }

        /// <summary>
        /// A surrogate for <see cref="ServiceId"/> to allow this class to be used as an <see cref="IIdCacheableDbEntity"/>.
        /// </summary>
        [Write(false)]
        public string GeotabId { get => ServiceId; set => ServiceId = value; }

        /// <summary>
        /// The default <see cref="DateTime"/> value to use in place of a null value.
        /// </summary>
        [Write(false)]
        public static DateTime DefaultDateTime { get => DateTime.ParseExact("1912/06/23", "yyyy/MM/dd", CultureInfo.InvariantCulture); }

        /// <summary>
        /// Indicates whether the subject processor has processed any entities. If <c>false</c>, the <see cref="EntitiesLastProcessedUtc"/> and <see cref="LastProcessedFeedVersion"/> properties of the processor should all be null.
        /// </summary>
        [Write(false)]
        public bool EntitiesHaveBeenProcessed { get => EntitiesLastProcessedUtc != null && EntitiesLastProcessedUtc != DefaultDateTime; }

        DateTime? entitiesLastProcessedUtc;

        [Key]
        public long id { get; set; }
        public string ServiceId { get; set; }
        public string? AdapterVersion { get; set; }
        public string? AdapterMachineName { get; set; }
        public DateTime? EntitiesLastProcessedUtc
        {
            // Allow null value to be written to the database, but substitute null value with the DefaultDateTime when returning in order to facilitate DateTime operations. If the AdapterDbLastId is null, also return the DefaultDateTime because it is possible that no records may have been returned on previous checks (especially on start-up) and we do not want to miss any data.
            get
            {
                if (entitiesLastProcessedUtc == null)
                {
                    return DefaultDateTime;
                }
                return entitiesLastProcessedUtc;
            }
            set
            {
                entitiesLastProcessedUtc = value;
            }
        }
        public long? LastProcessedFeedVersion { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
