﻿using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("DriverChanges")]
    public class DbDriverChange : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "DriverChanges";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public DateTime? DateTime { get; set; }
        public string DeviceId { get; set; }
        public string DriverId { get; set; }
        public string Type { get; set; }
        public long Version { get; set; }
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
