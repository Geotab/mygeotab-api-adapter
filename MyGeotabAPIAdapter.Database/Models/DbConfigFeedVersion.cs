using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("ConfigFeedVersions")]
    public class DbConfigFeedVersion
    {
        [Key]
        public long id { get; set; }
        public string FeedTypeId { get; set; }
        public long LastProcessedFeedVersion { get; set; }
        public DateTime RecordLastChangedUtc { get; set; }
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }
    }
}
