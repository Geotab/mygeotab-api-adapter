#nullable enable
using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("BinaryData")]
    public class DbBinaryData
    {
        [Key]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public string? BinaryType { get; set; }
        public string ControllerId { get; set; }
        public string Data { get; set; }
        public DateTime? DateTime { get; set; }
        public string? DeviceId { get; set; }
        public string? Version { get; set; }
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
