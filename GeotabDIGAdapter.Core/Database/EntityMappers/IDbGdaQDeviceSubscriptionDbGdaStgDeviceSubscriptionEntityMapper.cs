using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DbGdaStgDeviceSubscription"/> entities from <see cref="DbGdaQDeviceSubscription"/> entities.
    /// </summary>
    public interface IDbGdaQDeviceSubscriptionDbGdaStgDeviceSubscriptionEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DbGdaStgDeviceSubscription"/> entity from a <see cref="DbGdaQDeviceSubscription"/> entity.
        /// </summary>
        /// <param name="dbGdaQDeviceSubscription">The source <see cref="DbGdaQDeviceSubscription"/> entity.</param>
        /// <param name="batchId">The batch the staged row belongs to.</param>
        /// <param name="operationType">The operation type to stage the row under - <see cref="DeviceSubscriptionOperationType.Subscribe"/> or <see cref="DeviceSubscriptionOperationType.Terminate"/>. Callers pass the queue record's own operation type; the merge procedure recognises no other value.</param>
        /// <returns>A new <see cref="DbGdaStgDeviceSubscription"/> entity.</returns>
        DbGdaStgDeviceSubscription CreateEntity(DbGdaQDeviceSubscription dbGdaQDeviceSubscription, Guid batchId, byte operationType);
    }
}
