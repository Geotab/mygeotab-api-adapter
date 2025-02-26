using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Device"/> and <see cref="DbDevice2"/> entities.
    /// </summary>
    public interface IGeotabDeviceDbDevice2ObjectMapper : IStatusableGeotabObjectMapper<Device, DbDevice2>
    {
    }
}
