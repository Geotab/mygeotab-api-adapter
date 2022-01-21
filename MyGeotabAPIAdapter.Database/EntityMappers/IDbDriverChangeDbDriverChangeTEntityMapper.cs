using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DbDriverChange"/> and <see cref="DbDriverChangeT"/> entities.
    /// </summary>
    public interface IDbDriverChangeDbDriverChangeTEntityMapper
    {
        /// <summary>
        /// Creates and returns a new entity.
        /// </summary>
        /// <param name="entityToMapTo">The entity from which to obtain property values.</param>
        /// <param name="driverChangeTypeId">The value to be used for the entity property of the same name.</param>
        /// <param name="deviceId">The value to be used for the entity property of the same name.</param>
        /// <param name="driverId">The value to be used for the entity property of the same name.</param>
        /// <returns></returns>
        DbDriverChangeT CreateEntity(DbDriverChange entityToMapTo, long driverChangeTypeId, long deviceId, long driverId);

        /// <summary>
        /// Updates properties of the <paramref name="entityToUpdate"/> using property values of the <paramref name="entityToMapTo"/> and any other properties. 
        /// </summary>
        /// <param name="entityToUpdate">The entity to be updated.</param>
        /// <param name="entityToMapTo">The entity from which to obtain property values.</param>
        /// <param name="driverChangeTypeId">The value to be used for the entity property of the same name.</param>
        /// <param name="deviceId">The value to be used for the entity property of the same name.</param>
        /// <param name="driverId">The value to be used for the entity property of the same name.</param>
        void UpdateEntity(DbDriverChangeT entityToUpdate, DbDriverChange entityToMapTo, long driverChangeTypeId, long deviceId, long driverId);
    }
}

