using MyGeotabAPIAdapter.Database.Models;
using BinaryData = Geotab.Checkmate.ObjectModel.BinaryData;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="BinaryData"/> and <see cref="DbBinaryData2"/> entities.
    /// </summary>
    public interface IGeotabBinaryDataDbBinaryData2ObjectMapper
    {
        /// <summary>
        /// Creates and returns a new entity.
        /// </summary>
        /// <param name="entityToMapTo">The entity from which to obtain property values.</param>
        /// <param name="deviceId">The value to be used for the entity property of the same name.</param>
        /// <returns></returns>
        DbBinaryData2 CreateEntity(BinaryData entityToMapTo, long deviceId);
    }
}
