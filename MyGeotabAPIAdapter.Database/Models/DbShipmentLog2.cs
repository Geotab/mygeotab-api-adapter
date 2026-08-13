using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("ShipmentLogs2")]
    public class DbShipmentLog2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "ShipmentLogs2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
        public Guid id { get; set; }
        public string GeotabId { get; set; }
        public DateTime? ActiveFrom { get; set; }
        public DateTime? ActiveTo { get; set; }
        public string Commodity { get; set; }
        [ExplicitKey]
        public DateTime DateTime { get; set; }
        public long? DeviceId { get; set; }
        public string DocumentNumber { get; set; }
        public long? DriverId { get; set; }
        public string ShipperName { get; set; }
        public long? Version { get; set; }

        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
