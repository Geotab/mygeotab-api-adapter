using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("FaultData2")]
    public class DbFaultData2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "FaultData2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public bool? AmberWarningLamp { get; set; }
        public string ClassCode { get; set; }
        public string ControllerId { get; set; }
        public string ControllerName { get; set; }
        public int? Count { get; set; }
        public DateTime DateTime { get; set; }
        public long DeviceId { get; set; }
        public long DiagnosticId { get; set; }
        public DateTime? DismissDateTime { get; set; }
        public long? DismissUserId { get; set; }
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
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
