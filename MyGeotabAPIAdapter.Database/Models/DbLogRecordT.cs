using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("LogRecordsT")]
    public class DbLogRecordT : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "LogRecordsT";

        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Write(false)]
#pragma warning disable IDE1006 // Naming Styles
        public long id { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        [ExplicitKey]
        public string GeotabId { get; set; }
        public DateTime DateTime { get; set; }
        public long DeviceId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public float Speed { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
