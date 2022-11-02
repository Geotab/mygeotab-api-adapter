using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DeviceStatusInfo"/> and <see cref="DbDeviceStatusInfo"/> entities.
    /// </summary>
    public interface IGeotabDeviceStatusInfoDbDeviceStatusInfoObjectMapper : IGeotabObjectMapper<DeviceStatusInfo, DbDeviceStatusInfo>
    {
    }
}
