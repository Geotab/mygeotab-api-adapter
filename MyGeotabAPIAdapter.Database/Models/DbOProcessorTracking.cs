#nullable enable
using Dapper.Contrib.Extensions;
using System;
using System.Globalization;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("OProcessorTracking")]
    public class DbOProcessorTracking : IDbEntity, IIdCacheableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "OProcessorTracking";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => RecordLastChangedUtc; }

        /// <summary>
        /// A surrogate for <see cref="ProcessorId"/> to allow this class to be used as an <see cref="IIdCacheableDbEntity"/>.
        /// </summary>
        [Write(false)]
        public string GeotabId { get => ProcessorId; set => ProcessorId = value; }

        /// <summary>
        /// The default <see cref="DateTime"/> value to use in place of a null value.
        /// </summary>
        [Write(false)]
        public static DateTime DefaultDateTime { get => DateTime.ParseExact("1912/06/23", "yyyy/MM/dd", CultureInfo.InvariantCulture); }

        /// <summary>
        /// Indicates whether the subject processor has processed any entities. If <c>false</c>, the <see cref="EntitiesLastProcessedUtc"/>, <see cref="AdapterDbLastId"/>, <see cref="AdapterDbLastGeotabId"/> and <see cref="AdapterDbLastRecordCreationTimeUtc"/> properties of the processor should all be null.
        /// </summary>
        [Write(false)]
        public bool EntitiesHaveBeenProcessed { get => EntitiesLastProcessedUtc != null && EntitiesLastProcessedUtc != DefaultDateTime; }

        DateTime? entitiesLastProcessedUtc;

        [Key]
        public long id { get; set; }
        public string ProcessorId { get; set; }
        public string? OptimizerVersion { get; set; }
        public string? OptimizerMachineName { get; set; }
        public DateTime? EntitiesLastProcessedUtc
        {
            // Allow null value to be written to the database, but substitute null value with the DefaultDateTime when returning in order to facilitate DateTime operations.
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
        public long? AdapterDbLastId { get; set; }
        public string? AdapterDbLastGeotabId { get; set; }
        public DateTime? AdapterDbLastRecordCreationTimeUtc { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
