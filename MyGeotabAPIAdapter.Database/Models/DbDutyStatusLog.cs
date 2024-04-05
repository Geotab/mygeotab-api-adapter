using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("DutyStatusLogs")]
    public class DbDutyStatusLog : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "DutyStatusLogs";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
#pragma warning disable IDE1006 // Naming Styles
        public long id { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        public string GeotabId { get; set; }
        public string Annotations { get; set; }
        public string CoDrivers { get; set; }
        public DateTime? DateTime { get; set; }
        public int? DeferralMinutes { get; set; }
        public string DeferralStatus { get; set; }
        public string DeviceId { get; set; }
        public float? DistanceSinceValidCoordinates { get; set; }
        public string DriverId { get; set; }
        public DateTime? EditDateTime { get; set; }
        public string EditRequestedByUserId { get; set; }
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
