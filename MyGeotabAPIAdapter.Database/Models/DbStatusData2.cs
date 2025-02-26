using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("StatusData2")]
    public class DbStatusData2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "StatusData2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public double? Data { get; set; }
        public DateTime DateTime { get; set; }
        public long DeviceId { get; set; }
        public long DiagnosticId { get; set; }
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
