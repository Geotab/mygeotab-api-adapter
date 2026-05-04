using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DbGdaProvisionedDevice"/> entities from <see cref="DbGdaQProvisionDevice"/> entities.
    /// </summary>
    public interface IDbGdaQProvisionDeviceDbGdaProvisionedDeviceEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DbGdaProvisionedDevice"/> entity from a <see cref="DbGdaQProvisionDevice"/> entity.
        /// </summary>
        /// <param name="dbGdaQProvisionDevice">The source <see cref="DbGdaQProvisionDevice"/> entity.</param>
        /// <param name="geotabSerialNumber">The Geotab serial number assigned during provisioning.</param>
        /// <returns>A new <see cref="DbGdaProvisionedDevice"/> entity.</returns>
        DbGdaProvisionedDevice CreateEntity(DbGdaQProvisionDevice dbGdaQProvisionDevice, string geotabSerialNumber);
    }
}