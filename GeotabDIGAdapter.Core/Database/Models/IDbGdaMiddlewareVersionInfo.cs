using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// Interface to allow DbGdaMiddlewareVersionInfo implementations to be used interchangeably.
    /// </summary>
    public interface IDbGdaMiddlewareVersionInfo : IDbEntity
    {
        /// <summary>
        /// The primary key identifier.
        /// </summary>
        long id { get; set; }

        /// <summary>
        /// The database version.
        /// </summary>
        string DatabaseVersion { get; set; }

        /// <summary>
        /// The UTC timestamp when the record was created.
        /// </summary>
        DateTime RecordCreationTimeUtc { get; set; }
    }
}