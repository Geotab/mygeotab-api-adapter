using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="StatusData"/> and <see cref="DbStatusData2"/> entities.
    /// </summary>
    public interface IGeotabStatusDataDbStatusData2ObjectMapper
    {
        /// <summary>
        /// Creates and returns a new entity.
        /// </summary>
        /// <param name="entityToMapTo">The entity from which to obtain property values.</param>
        /// <param name="deviceId">The value to be used for the entity property of the same name.</param>
        /// <param name="diagnosticId">The value to be used for the entity property of the same name.</param>
        /// <returns></returns>
        DbStatusData2 CreateEntity(StatusData entityToMapTo, long deviceId, long diagnosticId);
    }
}
