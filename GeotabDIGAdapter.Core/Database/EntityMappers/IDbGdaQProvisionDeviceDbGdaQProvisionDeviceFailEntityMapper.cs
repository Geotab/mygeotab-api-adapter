using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DbGdaQProvisionDeviceFail"/> entities from <see cref="DbGdaQProvisionDevice"/> entities.
    /// </summary>
    public interface IDbGdaQProvisionDeviceDbGdaQProvisionDeviceFailEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DbGdaQProvisionDeviceFail"/> entity from a <see cref="DbGdaQProvisionDevice"/> entity.
        /// </summary>
        /// <param name="dbGdaQProvisionDevice">The source <see cref="DbGdaQProvisionDevice"/> entity.</param>
        /// <param name="failureReason">The reason for the failure.</param>
        /// <returns>A new <see cref="DbGdaQProvisionDeviceFail"/> entity.</returns>
        DbGdaQProvisionDeviceFail CreateEntity(DbGdaQProvisionDevice dbGdaQProvisionDevice, string failureReason);
    }
}