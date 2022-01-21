using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DbBinaryData"/> and <see cref="DbBinaryDataT"/> entities.
    /// </summary>
    public interface IDbBinaryDataDbBinaryDataTEntityMapper
    {
        /// <summary>
        /// Creates and returns a new entity.
        /// </summary>
        /// <param name="entityToMapTo">The entity from which to obtain property values.</param>
        /// <param name="binaryTypeId">The value to be used for the entity property of the same name.</param>
        /// <param name="controllerId">The value to be used for the entity property of the same name.</param>
        /// <param name="deviceId">The value to be used for the entity property of the same name.</param>
        /// <returns></returns>
        DbBinaryDataT CreateEntity(DbBinaryData entityToMapTo, long binaryTypeId, long controllerId, long deviceId);

        /// <summary>
        /// Updates properties of the <paramref name="entityToUpdate"/> using property values of the <paramref name="entityToMapTo"/> and any other properties. 
        /// </summary>
        /// <param name="entityToUpdate">The entity to be updated.</param>
        /// <param name="entityToMapTo">The entity from which to obtain property values.</param>
        /// <param name="binaryTypeId">The value to be used for the entity property of the same name.</param>
        /// <param name="controllerId">The value to be used for the entity property of the same name.</param>
        /// <param name="deviceId">The value to be used for the entity property of the same name.</param>
        /// <returns></returns>
        void UpdateEntity(DbBinaryDataT entityToUpdate, DbBinaryData entityToMapTo, long binaryTypeId, long controllerId, long deviceId);
    }
}

