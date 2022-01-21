using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DbFaultData"/> and <see cref="DbFaultDataT"/> entities.
    /// </summary>
    public interface IDbFaultDataDbFaultDataTEntityMapper
    {
        /// <summary>
        /// Creates and returns a new entity.
        /// </summary>
        /// <param name="entityToMapTo">The entity from which to obtain property values.</param>
        /// <param name="deviceId">The value to be used for the entity property of the same name.</param>
        /// <param name="diagnosticId">The value to be used for the entity property of the same name.</param>
        /// <param name="dismissUserId">The value to be used for the entity property of the same name.</param>
        /// <returns></returns>
        DbFaultDataT CreateEntity(DbFaultData entityToMapTo, long deviceId, long diagnosticId, long? dismissUserId);

        /// <summary>
        /// Updates properties of the <paramref name="entityToUpdate"/> using property values of the <paramref name="entityToMapTo"/> and any other properties. 
        /// </summary>
        /// <param name="entityToUpdate">The entity to be updated.</param>
        /// <param name="entityToMapTo">The entity from which to obtain property values.</param>
        /// <param name="deviceId">The value to be used for the entity property of the same name.</param>
        /// <param name="diagnosticId">The value to be used for the entity property of the same name.</param>
        /// <param name="dismissUserId">The value to be used for the entity property of the same name.</param>
        void UpdateEntity(DbFaultDataT entityToUpdate, DbFaultData entityToMapTo, long deviceId, long diagnosticId, long? dismissUserId);
    }
}

