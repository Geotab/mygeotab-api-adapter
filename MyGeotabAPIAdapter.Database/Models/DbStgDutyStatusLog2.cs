using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("stg_DutyStatusLogs2")]
    public class DbStgDutyStatusLog2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "stg_DutyStatusLogs2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
        public Guid id { get; set; }
        //public long id { get; set; }
        public string GeotabId { get; set; }
        public string Annotations { get; set; }
        public string CoDrivers { get; set; }
        [ExplicitKey]
        public DateTime DateTime { get; set; }
        public int? DeferralMinutes { get; set; }
        //public Int16? DeferralStatusId { get; set; }
        public string DeferralStatus { get; set; }
        public long? DeviceId { get; set; }
        public float? DistanceSinceValidCoordinates { get; set; }
        public long? DriverId { get; set; }
        public DateTime? EditDateTime { get; set; }
        public long? EditRequestedByUserId { get; set; }
        public double? EngineHours { get; set; }
        public long? EventCheckSum { get; set; }
        public byte? EventCode { get; set; }
        public byte? EventRecordStatus { get; set; }
        public byte? EventType { get; set; }
        public bool? IsHidden { get; set; }
        public bool? IsIgnored { get; set; }
        public bool? IsTransitioning { get; set; }
        public string Location { get; set; }
        public double? LocationX { get; set; }
        public double? LocationY { get; set; }
        public string Malfunction { get; set; }
        public double? Odometer { get; set; }
        public string Origin { get; set; }
        //public long? ParentId { get; set; }
        public string ParentId { get; set; }
        public long? Sequence { get; set; }
        public string State { get; set; }
        public string Status { get; set; }
        public string UserHosRuleSet { get; set; }
        public DateTime? VerifyDateTime { get; set; }
        public long Version { get; set; }

        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
