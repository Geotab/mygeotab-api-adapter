using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DbDevice"/> and <see cref="DbDeviceT"/> entities.
    /// </summary>
    public interface IDbDeviceDbDeviceTEntityMapper : IEntityMapper<DbDevice, DbDeviceT>
    {
    }
}
