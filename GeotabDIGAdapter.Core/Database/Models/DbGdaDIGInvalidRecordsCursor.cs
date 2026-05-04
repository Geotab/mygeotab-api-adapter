using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.DIGInvalidRecordsCursor database table.
    /// This is a single-row table that persists the pagination cursor for the InvalidRecordRetrievalService.
    /// </summary>
    [Table("gda.DIGInvalidRecordsCursor")]
    public class DbGdaDIGInvalidRecordsCursor : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.DIGInvalidRecordsCursor";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <summary>
        /// The primary key. Always 1 for this single-row table.
        /// </summary>
        [ExplicitKey]
        public int id { get; set; } = 1;

        /// <summary>
        /// The cursor value to use for the next API request. 0 means start from the beginning.
        /// </summary>
        public int NextResultKey { get; set; }

        /// <summary>
        /// The date and time when this cursor was last updated.
        /// </summary>
        [ChangeTracker]
        public DateTime LastUpdatedUtc { get; set; }
    }
}
