using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("LogRecords")]
    public class DbLogRecord
    {
        [ExplicitKey]
        public string Id { get; set; }
        public DateTime DateTime { get; set; }
        public string DeviceId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Speed { get; set; }
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
