using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DbGdaQGenericFaultRecordFail"/> entities from <see cref="DbGdaQGenericFaultRecord"/> entities.
    /// </summary>
    public interface IDbGdaQGenericFaultRecordDbGdaQGenericFaultRecordFailEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DbGdaQGenericFaultRecordFail"/> entity from a <see cref="DbGdaQGenericFaultRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQGenericFaultRecord">The source <see cref="DbGdaQGenericFaultRecord"/> entity.</param>
        /// <param name="failureReason">The reason for the failure.</param>
        /// <returns>A new <see cref="DbGdaQGenericFaultRecordFail"/> entity.</returns>
        DbGdaQGenericFaultRecordFail CreateEntity(DbGdaQGenericFaultRecord dbGdaQGenericFaultRecord, string failureReason);
    }
}
