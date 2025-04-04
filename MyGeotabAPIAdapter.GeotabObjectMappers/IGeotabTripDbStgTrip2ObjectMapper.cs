using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Trip"/> and <see cref="DbStgTrip2"/> entities.
    /// </summary>
    public interface IGeotabTripDbStgTrip2ObjectMapper
    {
        /// <summary>
        /// Creates and returns a new entity.
        /// </summary>
        /// <param name="entityToMapTo">The entity from which to obtain property values.</param>
        /// <param name="deviceId">The value to be used for the entity property of the same name.</param>
        /// <param name="driverId">The value to be used for the entity property of the same name.</param>
        /// <returns></returns>
        DbStgTrip2 CreateEntity(Trip entityToMapTo, long deviceId, long? driverId);
    }
}
