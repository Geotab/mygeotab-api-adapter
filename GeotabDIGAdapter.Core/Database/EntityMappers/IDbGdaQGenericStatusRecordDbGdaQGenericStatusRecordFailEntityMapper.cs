using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DbGdaQGenericStatusRecordFail"/> entities from <see cref="DbGdaQGenericStatusRecord"/> entities.
    /// </summary>
    public interface IDbGdaQGenericStatusRecordDbGdaQGenericStatusRecordFailEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DbGdaQGenericStatusRecordFail"/> entity from a <see cref="DbGdaQGenericStatusRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQGenericStatusRecord">The source <see cref="DbGdaQGenericStatusRecord"/> entity.</param>
        /// <param name="failureReason">The reason for the failure.</param>
        /// <returns>A new <see cref="DbGdaQGenericStatusRecordFail"/> entity.</returns>
        DbGdaQGenericStatusRecordFail CreateEntity(DbGdaQGenericStatusRecord dbGdaQGenericStatusRecord, string failureReason);
    }
}
