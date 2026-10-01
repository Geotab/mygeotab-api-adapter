using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DbGdaQDeviceSubscriptionFail"/> entities from <see cref="DbGdaQDeviceSubscription"/> entities.
    /// </summary>
    public interface IDbGdaQDeviceSubscriptionDbGdaQDeviceSubscriptionFailEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DbGdaQDeviceSubscriptionFail"/> entity from a <see cref="DbGdaQDeviceSubscription"/> entity.
        /// </summary>
        /// <param name="dbGdaQDeviceSubscription">The source <see cref="DbGdaQDeviceSubscription"/> entity.</param>
        /// <param name="failureReason">The reason for the failure.</param>
        /// <returns>A new <see cref="DbGdaQDeviceSubscriptionFail"/> entity.</returns>
        DbGdaQDeviceSubscriptionFail CreateEntity(DbGdaQDeviceSubscription dbGdaQDeviceSubscription, string failureReason);
    }
}
