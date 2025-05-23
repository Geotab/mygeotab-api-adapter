using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("DeviceStatusInfo2")]
    public class DbDeviceStatusInfo2 : IDbEntity, IIdCacheableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "DeviceStatusInfo2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => RecordLastChangedUtc; }

        /// <summary>
        /// The <see cref="DeviceId"/> decoded to its <see cref="long"/> format. Normally, it would be the <see cref="GeotabId"/> decoded to its <see cref="long"/> format, but in this case, since DeviceStatusInfo is a snapshot of the Device's status that is current at the time of the Get/GetFeed call, the GeotabId is not of any real value. The database table contains one record per Device that gets updated when new DeviceStatusInfo is received. As such, using the <see cref="DeviceId"/> as the key is more appropriate.
        /// </summary>
        [ExplicitKey]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public double Bearing { get; set; }
        public string CurrentStateDuration { get; set; }
        public DateTime? DateTime { get; set; }
        public long DeviceId { get; set; }
        public long? DriverId { get; set; }
        public bool? IsDeviceCommunicating { get; set; }
        public bool? IsDriving { get; set; }
        public bool? IsHistoricLastDriver { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public float Speed { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
