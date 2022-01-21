using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("FaultDataT")]
    public class DbFaultDataT : IDbEntity, IIdCacheableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "FaultDataT";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => RecordLastChangedUtc; }

        [Write(false)]
        public long id { get; set; }
        [ExplicitKey]
        public string GeotabId { get; set; }
        public bool? AmberWarningLamp { get; set; }
        public string ClassCode { get; set; }
        public string ControllerId { get; set; }
        public string ControllerName { get; set; }
        public int? Count { get; set; }
        public DateTime? DateTime { get; set; }
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
        public long? DriverId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double Speed { get; set; }
        public double Bearing { get; set; }
        public string Direction { get; set; }
        public bool LongLatProcessed { get; set; }
        public byte? LongLatReason { get; set; }
        public bool DriverIdProcessed { get; set; }
        public byte? DriverIdReason { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
