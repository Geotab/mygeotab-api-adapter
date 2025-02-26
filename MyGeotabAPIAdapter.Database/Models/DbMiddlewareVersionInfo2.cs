using Dapper.Contrib.Extensions;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Database;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("MiddlewareVersionInfo2")]
    public class DbMiddlewareVersionInfo2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "MiddlewareVersionInfo2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }
        public string DatabaseVersion { get; set; }
         [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
