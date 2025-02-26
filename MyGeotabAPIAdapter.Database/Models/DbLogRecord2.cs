using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("LogRecords2")]
    public class DbLogRecord2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "LogRecords2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public DateTime DateTime { get; set; }
        public long DeviceId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public float Speed { get; set; }
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
