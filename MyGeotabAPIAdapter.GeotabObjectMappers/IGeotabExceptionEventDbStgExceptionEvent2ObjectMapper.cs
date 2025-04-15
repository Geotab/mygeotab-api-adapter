using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="ExceptionEvent"/> and <see cref="DbStgExceptionEvent2"/> entities.
    /// </summary>
    public interface IGeotabExceptionEventDbStgExceptionEvent2ObjectMapper
    {
        /// <summary>
        /// Creates and returns a new entity.
        /// </summary>
        /// <param name="entityToMapTo">The entity from which to obtain property values.</param>
        /// <param name="deviceId">The value to be used for the entity property of the same name.</param>
        /// <param name="driverId">The value to be used for the entity property of the same name.</param>
        /// <returns></returns>
        DbStgExceptionEvent2 CreateEntity(ExceptionEvent entityToMapTo, long deviceId, long? driverId);
    }
}
