using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("Devices")]
    public class DbDevice
    {
        [ExplicitKey]
        public string Id { get; set; }
        public DateTime? ActiveFrom { get; set; }
        public DateTime? ActiveTo { get; set; }
        public string DeviceType { get; set; }
        public string LicensePlate { get; set; }
        public string LicenseState { get; set; }
        public string Name { get; set; }
        public int? ProductId { get; set; }
        public string SerialNumber { get; set; }
        public string VIN { get; set; }
        public int EntityStatus { get; set; }
        public DateTime RecordLastChangedUtc { get; set; }
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }
    }
}
