using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("stg_DVIRLogs2")]
    public class DbStgDVIRLog2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "stg_DVIRLogs2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
        public Guid id { get; set; }
        public string GeotabId { get; set; }
        public string AuthorityAddress { get; set; }
        public string AuthorityName { get; set; }
        public long? CertifiedByUserId { get; set; }
        public DateTime? CertifiedDate { get; set; }
        public string CertifyRemark { get; set; }
        public DateTime DateTime { get; set; }
        public long DeviceId { get; set; }
        public long? DriverId { get; set; }
        public string DriverRemark { get; set; }
        [Write(false)]
        public TimeSpan? Duration
        {
            get { return TimeSpan.FromTicks(DurationTicks.GetValueOrDefault()); }
            set
            {
                if (value.HasValue)
                {
                    DurationTicks = value.Value.Ticks;
                }
            }
        }
        public long? DurationTicks { get; set; }
        public Single? EngineHours { get; set; }
        public bool? IsSafeToOperate { get; set; }
        public Single? LoadHeight { get; set; }
        public Single? LoadWidth { get; set; }
        public double? LocationLatitude { get; set; }
        public double? LocationLongitude { get; set; }
        public string LogType { get; set; }
        public double? Odometer { get; set; }
        public DateTime? RepairDate { get; set; }
        public long? RepairedByUserId { get; set; }
        public string RepairRemark { get; set; }
        public long? Version { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
