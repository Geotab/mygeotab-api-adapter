using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DbStatusData"/> and <see cref="DbStatusDataT"/> entities.
    /// </summary>
    public interface IDbStatusDataDbStatusDataTEntityMapper 
    {
        /// <summary>
        /// Creates and returns a new entity.
        /// </summary>
        /// <param name="entityToMapTo">The entity from which to obtain property values.</param>
        /// <param name="deviceId">The value to be used for the entity property of the same name.</param>
        /// <param name="diagnosticId">The value to be used for the entity property of the same name.</param>
        /// <returns></returns>
        DbStatusDataT CreateEntity(DbStatusData entityToMapTo, long deviceId, long diagnosticId);

        /// <summary>
        /// Updates properties of the <paramref name="entityToUpdate"/> using property values of the <paramref name="entityToMapTo"/> and any other properties. 
        /// </summary>
        /// <param name="entityToUpdate">The entity to be updated.</param>
        /// <param name="entityToMapTo">The entity from which to obtain property values.</param>
        /// <param name="deviceId">The value to be used for the entity property of the same name.</param>
        /// <param name="diagnosticId">The value to be used for the entity property of the same name.</param>
        void UpdateEntity(DbStatusDataT entityToUpdate, DbStatusData entityToMapTo, long deviceId, long diagnosticId);
    }
}

