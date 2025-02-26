using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("DBPartitionInfo2")]
    public class DbDBPartitionInfo2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "DBPartitionInfo2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }
        public DateTime InitialMinDateTimeUTC { get; set; }
        public string InitialPartitionInterval { get; set; }
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
