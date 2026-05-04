using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DbGdaQJ1708FaultRecordFail"/> entities from <see cref="DbGdaQJ1708FaultRecord"/> entities.
    /// </summary>
    public interface IDbGdaQJ1708FaultRecordDbGdaQJ1708FaultRecordFailEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DbGdaQJ1708FaultRecordFail"/> entity from a <see cref="DbGdaQJ1708FaultRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQJ1708FaultRecord">The source <see cref="DbGdaQJ1708FaultRecord"/> entity.</param>
        /// <param name="failureReason">The reason for the failure.</param>
        /// <returns>A new <see cref="DbGdaQJ1708FaultRecordFail"/> entity.</returns>
        DbGdaQJ1708FaultRecordFail CreateEntity(DbGdaQJ1708FaultRecord dbGdaQJ1708FaultRecord, string failureReason);
    }
}
