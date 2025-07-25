using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("stg_FuelAndEnergyUsed2")]
    public class DbStgFuelAndEnergyUsed2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "stg_FuelAndEnergyUsed2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
        public Guid id { get; set; }
        public string GeotabId { get; set; }
        public DateTime? DateTime { get; set; }
        public long DeviceId { get; set; }
        public double? TotalEnergyUsedKwh { get; set; }
        public double? TotalFuelUsed { get; set; }
        public double? TotalIdlingEnergyUsedKwh { get; set; }
        public double? TotalIdlingFuelUsedL { get; set; }
        public long Version { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
