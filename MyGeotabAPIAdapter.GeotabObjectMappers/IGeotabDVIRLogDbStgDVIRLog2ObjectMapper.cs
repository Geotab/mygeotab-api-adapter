using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DVIRLog"/> and <see cref="DbStgDVIRLog2"/> entities.
    /// </summary>
    public interface IGeotabDVIRLogDbStgDVIRLog2ObjectMapper
    {
        /// <summary>
        /// Creates and returns a new entity.
        /// </summary>
        /// <param name="entityToMapTo">The entity from which to obtain property values.</param>
        /// <param name="certifiedByUserId">The value to be used for the entity property of the same name.</param>
        /// <param name="deviceId">The value to be used for the entity property of the same name.</param>
        /// <param name="driverId">The value to be used for the entity property of the same name.</param>
        /// <param name="repairedByUserId">The value to be used for the entity property of the same name.</param>
        /// <returns></returns>
        DbStgDVIRLog2 CreateEntity(DVIRLog entityToMapTo, long? certifiedByUserId, long deviceId, long? driverId, long? repairedByUserId);
    }
}
