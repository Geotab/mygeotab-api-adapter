#nullable enable
using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("BinaryData")]
    public class DbBinaryData : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "BinaryData";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
#pragma warning disable IDE1006 // Naming Styles
        public long id { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        public string GeotabId { get; set; }
        public string? BinaryType { get; set; }
        public string ControllerId { get; set; }
        public string Data { get; set; }
        public DateTime? DateTime { get; set; }
        public string? DeviceId { get; set; }
        public string? Version { get; set; }
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
