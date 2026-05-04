using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DbGdaQGpsRecordFail"/> entities from <see cref="DbGdaQGpsRecord"/> entities.
    /// </summary>
    public interface IDbGdaQGpsRecordDbGdaQGpsRecordFailEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DbGdaQGpsRecordFail"/> entity from a <see cref="DbGdaQGpsRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQGpsRecord">The source <see cref="DbGdaQGpsRecord"/> entity.</param>
        /// <param name="failureReason">The reason for the failure.</param>
        /// <returns>A new <see cref="DbGdaQGpsRecordFail"/> entity.</returns>
        DbGdaQGpsRecordFail CreateEntity(DbGdaQGpsRecord dbGdaQGpsRecord, string failureReason);
    }
}
