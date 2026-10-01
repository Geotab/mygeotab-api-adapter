using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.DigPartitionInfo database table. This single-row table records the arguments spManageDigPartitions was first run with. Both are immutable afterwards, so the adapter reads them back rather than recomputing them: the earliest date a run may ask for is fixed on the first run and cannot be moved earlier.
    /// </summary>
    [Table("gda.DigPartitionInfo")]
    public class DbGdaDigPartitionInfo : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.DigPartitionInfo";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <summary>
        /// The primary key. The row is written by spManageDigPartitions, never by this application.
        /// </summary>
        [Key]
        public long id { get; set; }

        /// <summary>
        /// The earliest date partitions were created from, as supplied on the first run of spManageDigPartitions.
        /// </summary>
        public DateTime InitialMinDateTimeUtc { get; set; }

        /// <summary>
        /// The partition interval ('monthly', 'weekly' or 'daily') pinned on the first run of spManageDigPartitions.
        /// </summary>
        public string InitialPartitionInterval { get; set; }

        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
