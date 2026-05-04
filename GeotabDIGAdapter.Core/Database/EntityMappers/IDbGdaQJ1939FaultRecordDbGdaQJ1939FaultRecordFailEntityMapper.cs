using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DbGdaQJ1939FaultRecordFail"/> entities from <see cref="DbGdaQJ1939FaultRecord"/> entities.
    /// </summary>
    public interface IDbGdaQJ1939FaultRecordDbGdaQJ1939FaultRecordFailEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DbGdaQJ1939FaultRecordFail"/> entity from a <see cref="DbGdaQJ1939FaultRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQJ1939FaultRecord">The source <see cref="DbGdaQJ1939FaultRecord"/> entity.</param>
        /// <param name="failureReason">The reason for the failure.</param>
        /// <returns>A new <see cref="DbGdaQJ1939FaultRecordFail"/> entity.</returns>
        DbGdaQJ1939FaultRecordFail CreateEntity(DbGdaQJ1939FaultRecord dbGdaQJ1939FaultRecord, string failureReason);
    }
}
