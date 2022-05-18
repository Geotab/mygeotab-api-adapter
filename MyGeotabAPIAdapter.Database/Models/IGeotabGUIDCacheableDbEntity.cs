using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// Interface for database entity models with <see cref="GeotabId"/>s and <see cref="GeotabGUID"/>s that relate to objects from the Geotab platform and corresponding <see cref="id"/>s that are surrogate Ids used in the database. 
    /// </summary>
    public interface IGeotabGUIDCacheableDbEntity
    {
        /// <summary>
        /// The underlying GUID of the object in the Geotab platform.
        /// </summary>
        string GeotabGUID { get; set; }

        /// <summary>
        /// The Id of the object in the Geotab platform.
        /// </summary>
        string GeotabId { get; set; }

        /// <summary>
        /// The surrogate Id of the object in the database.
        /// </summary>
#pragma warning disable IDE1006 // Naming Styles
        long id { get; set; }
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// The last time the object's associated database record was inserted/updated.
        /// </summary>
        DateTime LastUpsertedUtc { get; }
    }
}
