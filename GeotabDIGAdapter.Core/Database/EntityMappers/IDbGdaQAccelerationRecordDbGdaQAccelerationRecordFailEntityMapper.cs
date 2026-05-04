using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DbGdaQAccelerationRecordFail"/> entities from <see cref="DbGdaQAccelerationRecord"/> entities.
    /// </summary>
    public interface IDbGdaQAccelerationRecordDbGdaQAccelerationRecordFailEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DbGdaQAccelerationRecordFail"/> entity from a <see cref="DbGdaQAccelerationRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQAccelerationRecord">The source <see cref="DbGdaQAccelerationRecord"/> entity.</param>
        /// <param name="failureReason">The reason for the failure.</param>
        /// <returns>A new <see cref="DbGdaQAccelerationRecordFail"/> entity.</returns>
        DbGdaQAccelerationRecordFail CreateEntity(DbGdaQAccelerationRecord dbGdaQAccelerationRecord, string failureReason);
    }
}
