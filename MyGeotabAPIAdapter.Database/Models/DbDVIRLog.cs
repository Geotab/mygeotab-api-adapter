using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("DVIRLogs")]
    public class DbDVIRLog
    {
        [Key]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public string CertifiedByUserId { get; set; }
        public DateTime? CertifiedDate { get; set; }
        public string CertifyRemark { get; set; }
        public DateTime? DateTime { get; set; }
        public string DeviceId { get; set; }
        public string DriverId { get; set; }
        public string DriverRemark { get; set; }
        public bool? IsSafeToOperate { get; set; }
        public float LocationLatitude { get; set; }
        public float LocationLongitude { get; set; }
        public string LogType { get; set; }
        public DateTime? RepairDate { get; set; }
        public string RepairedByUserId { get; set; }
        public string TrailerId { get; set; }
        public string TrailerName { get; set; }
        public long? Version { get; set; }
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
