using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("vwStatsForLocationInterpolationProgress")]
    public class DbvwStatForLocationInterpolationProgress : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "vwStatsForLocationInterpolationProgress";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
        public long RowId { get; set; }
        public string Table { get; set; }
        public long Total { get; set; }
        public long LongLatProcessedTotal { get; set; }
        public double LongLatProcessedPercentage { get; set; }
    }
}
