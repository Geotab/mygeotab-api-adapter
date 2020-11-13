using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("LogRecords")]
    public class DbLogRecord
    {
        [Key]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public DateTime DateTime { get; set; }
        public string DeviceId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Speed { get; set; }
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
