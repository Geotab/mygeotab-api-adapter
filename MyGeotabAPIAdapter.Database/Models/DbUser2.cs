#nullable enable
using Dapper.Contrib.Extensions;
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("Users2")]
    public class DbUser2 : IDbEntity, IIdCacheableDbEntity, IStatusableDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "Users2";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <inheritdoc/>
        [Write(false)]
        public DateTime LastUpsertedUtc { get => RecordLastChangedUtc; }

        /// <summary>
        /// The <see cref="GeotabId"/> decoded to its <see cref="long"/> format.
        /// </summary>
        [ExplicitKey]
        public long id { get; set; }
        public string GeotabId { get; set; }
        public DateTime? ActiveFrom { get; set; }
        public DateTime? ActiveTo { get; set; }
        public string EmployeeNo { get; set; }
        public string FirstName { get; set; }
        public string HosRuleSet { get; set; }
        public bool IsDriver { get; set; }
        public DateTime? LastAccessDate { get; set; }
        public string LastName { get; set; }
        public string Name { get; set; }
        public int EntityStatus { get; set; }
        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}
