using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// Interface to allow DbMyGeotabVersionInfo and DbMyGeotabVersionInfo2 to be used interchangeably.
    /// </summary>
    public interface IDbMyGeotabVersionInfo : IDbEntity
    {
        string DatabaseName { get; set; }
        string Server { get; set; }
        string DatabaseVersion { get; set; }
        string ApplicationBuild { get; set; }
        string ApplicationBranch { get; set; }
        string ApplicationCommit { get; set; }
        string GoTalkVersion { get; set; }
        DateTime RecordCreationTimeUtc { get; set; }
    }
}
