#nullable enable
using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("BinaryData2")]
    public class DbBinaryData2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "BinaryData2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [ExplicitKey]
#pragma warning disable IDE1006 // Naming Styles
        public Guid id { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        public string GeotabId { get; set; }
        public string? BinaryType { get; set; }
        public string ControllerId { get; set; }
        public byte[] Data { get; set; }
        [ExplicitKey]
        public DateTime DateTime { get; set; }
        public long DeviceId { get; set; }
        public long? Version { get; set; }
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
