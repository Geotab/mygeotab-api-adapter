using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DbLogRecord"/> and <see cref="DbLogRecordT"/> entities.
    /// </summary>
    public interface IDbLogRecordDbLogRecordTEntityMapper
    {
        /// <summary>
        /// Creates and returns a new entity.
        /// </summary>
        /// <param name="entityToMapTo">The entity from which to obtain property values.</param>
        /// <param name="deviceId">The value to be used for the entity property of the same name.</param>
        /// <returns></returns>
        DbLogRecordT CreateEntity(DbLogRecord entityToMapTo, long deviceId);

        /// <summary>
        /// Updates properties of the <paramref name="entityToUpdate"/> using property values of the <paramref name="entityToMapTo"/> and any other properties. 
        /// </summary>
        /// <param name="entityToUpdate">The entity to be updated.</param>
        /// <param name="entityToMapTo">The entity from which to obtain property values.</param>
        /// <param name="deviceId">The value to be used for the entity property of the same name.</param>
        void UpdateEntity(DbLogRecordT entityToUpdate, DbLogRecord entityToMapTo, long deviceId);
    }
}

