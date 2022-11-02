using Dapper.Contrib.Extensions;
using System;


namespace MyGeotabAPIAdapter.Database.Models
{
    [Table("MyGeotabVersionInfo")]
    public class DbMyGeotabVersionInfo : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "MyGeotabVersionInfo";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        public string DatabaseName { get; set; }
        public string Server { get; set; }
        public string DatabaseVersion { get; set; }
        public string ApplicationBuild { get; set; }
        public string ApplicationBranch { get; set; }
        public string ApplicationCommit { get; set; }
        public string GoTalkVersion { get; set; }
        [ExplicitKey]
        [ChangeTracker]
        public DateTime RecordCreationTimeUtc { get; set; }
    }
}
