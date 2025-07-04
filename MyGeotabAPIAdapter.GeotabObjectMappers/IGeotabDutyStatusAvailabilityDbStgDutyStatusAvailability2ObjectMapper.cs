using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DutyStatusAvailability"/> and <see cref="DbStgDutyStatusAvailability2"/> entities.
    /// </summary>
    public interface IGeotabDutyStatusAvailabilityDbStgDutyStatusAvailability2ObjectMapper
    {
        /// <summary>
        /// Creates and returns a new entity.
        /// </summary>
        /// <param name="entityToMapTo">The entity from which to obtain property values.</param>
        /// <param name="driverId">The value to be used for the entity property of the same name.</param>
        /// <returns></returns>
        DbStgDutyStatusAvailability2 CreateEntity(DutyStatusAvailability entityToMapTo, long driverId);
    }
}
