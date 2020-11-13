using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("StatusData")]
    public class DbStatusData
    {
        [Key]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public double? Data { get; set; }
        public DateTime? DateTime { get; set; }
        public string DeviceId { get; set; }
        public string DiagnosticId { get; set; }
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
