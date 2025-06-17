using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("DVIRDefectRemarks2")]
    public class DbDVIRDefectRemark2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "DVIRDefectRemarks2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => RecordLastChangedUtc; }

        [ExplicitKey]
        public Guid id { get; set; }
       
        public string GeotabId { get; set; }
        public Guid DVIRDefectId { get; set; }
        public DateTime DVIRLogDateTime { get; set; }
        public DateTime DateTime { get; set; }
        public string Remark { get; set; }
        public long? RemarkUserId { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
