using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("FaultData")]
    public class DbFaultData
    {
        [Key]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public bool? AmberWarningLamp { get; set; }
        public string ClassCode { get; set; }
        public string ControllerId { get; set; }
        public string ControllerName { get; set; }
        public int? Count { get; set; }
        public DateTime? DateTime { get; set; }
        public string DeviceId { get; set; }
        public string DiagnosticId { get; set; }
        public DateTime? DismissDateTime { get; set; }
        public string DismissUserId { get; set; }
        public int? FailureModeCode { get; set; }
        public string FailureModeId { get; set; }
        public string FailureModeName { get; set; }
        public string FaultLampState { get; set; }
        public string FaultState { get; set; }
        public bool? MalfunctionLamp { get; set; }
        public bool? ProtectWarningLamp { get; set; }
        public bool? RedStopLamp { get; set; }
        public string Severity { get; set; }
        public int? SourceAddress { get; set; }
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
