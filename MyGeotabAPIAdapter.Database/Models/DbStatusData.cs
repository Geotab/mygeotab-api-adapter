using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("StatusData")]
    public class DbStatusData
    {
        [ExplicitKey]
        public string Id { get; set; }
        public double? Data { get; set; }
        public DateTime? DateTime { get; set; }
        public string DeviceId { get; set; }
        public string DiagnosticId { get; set; }
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
