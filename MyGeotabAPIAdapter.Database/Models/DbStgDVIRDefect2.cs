using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("stg_DVIRDefects2")]
    public class DbStgDVIRDefect2 : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "stg_DVIRDefects2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => RecordLastChangedUtc; }

        [ExplicitKey]
        public Guid id { get; set; }
        public string GeotabId { get; set; }
        public Guid DVIRLogId { get; set; }
        public DateTime DVIRLogDateTime { get; set; }
        public string DefectListAssetType { get; set; }
        public string DefectListId { get; set; }
        public string DefectListName { get; set; }
        public string PartId { get; set; }
        public string PartName { get; set; }
        public string DefectId { get; set; }
        public string DefectName { get; set; }
        public Int16? DefectSeverityId { get; set; }
        public DateTime? RepairDateTime { get; set; }
        public Int16? RepairStatusId { get; set; }
        public long? RepairUserId { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
