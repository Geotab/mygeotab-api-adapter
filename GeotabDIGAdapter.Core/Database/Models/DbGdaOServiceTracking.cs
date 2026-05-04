using Dapper.Contrib.Extensions;
using System.Globalization;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.OServiceTracking database table.
    /// </summary>
    [Table("gda.OServiceTracking")]
    public class DbGdaOServiceTracking : IDbOServiceTracking
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.OServiceTracking";

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
        public string ServiceId { get; set; } = string.Empty;
        public string? AdapterVersion { get; set; }
        public string? AdapterMachineName { get; set; }
        public DateTime? EntitiesLastProcessedUtc
        {
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

        /// <summary>
        /// The size of the last processed batch.
        /// </summary>
        public int? LastBatchSize { get; set; }

        /// <summary>
        /// Cumulative count of successfully processed records.
        /// </summary>
        public long? SuccessCount { get; set; }

        /// <summary>
        /// Cumulative count of failed records.
        /// </summary>
        public long? FailureCount { get; set; }

        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}