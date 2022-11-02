using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Device"/> and <see cref="DbDevice"/> entities.
    /// </summary>
    public interface IGeotabDeviceDbDeviceObjectMapper : IStatusableGeotabObjectMapper<Device, DbDevice>
    {
    }
}
