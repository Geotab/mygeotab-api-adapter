using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("vwStatusDataTWithLagLeadDriverChangeBatch")]
    public class DbStatusDataTWithLagLeadDriverChange : IDbEntity, IIdCacheableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "vwStatusDataTWithLagLeadDriverChangeBatch";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => StatusDataDateTime; }

        [Write(false)]
        public long id { get; set; }
        [ExplicitKey]
        public string GeotabId { get; set; }
        public DateTime StatusDataDateTime { get; set; }
        public long DeviceId { get; set; }
        public long DriverId { get; set; }
        public DateTime? LagDateTime { get; set; }
        public DateTime? LeadDateTime { get; set; }
        public DateTime? DriverChangesTMinDateTime { get; set; }
        public DateTime? DriverChangesTMaxDateTime { get; set; }
        public DateTime? DeviceDriverChangesTMinDateTime { get; set; }
        public DateTime? DeviceDriverChangesTMaxDateTime { get; set; }
    }
}
