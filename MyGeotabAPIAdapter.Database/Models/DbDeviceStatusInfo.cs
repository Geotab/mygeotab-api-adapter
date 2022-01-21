using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("DeviceStatusInfo")]
    public class DbDeviceStatusInfo
    {
        [Key]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public double Bearing { get; set; }
        public string CurrentStateDuration { get; set; }
        public DateTime? DateTime { get; set; }
        public string DeviceId { get; set; }
        public string DriverId { get; set; }
        public bool? IsDeviceCommunicating { get; set; }
        public bool? IsDriving { get; set; }
        public bool? IsHistoricLastDriver { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Speed { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }
    }
}
