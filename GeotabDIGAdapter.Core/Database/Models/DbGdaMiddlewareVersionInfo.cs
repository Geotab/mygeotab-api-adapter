using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.MiddlewareVersionInfo database table.
    /// </summary>
    [Table("gda.MiddlewareVersionInfo")]
    public class DbGdaMiddlewareVersionInfo : IDbGdaMiddlewareVersionInfo
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.MiddlewareVersionInfo";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Key]
        public long id { get; set; }

        /// <inheritdoc/>
        public string DatabaseVersion { get; set; }

        /// <inheritdoc/>
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
