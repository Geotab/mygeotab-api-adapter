using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DbGdaQVinRecordFail"/> entities from <see cref="DbGdaQVinRecord"/> entities.
    /// </summary>
    public interface IDbGdaQVinRecordDbGdaQVinRecordFailEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DbGdaQVinRecordFail"/> entity from a <see cref="DbGdaQVinRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQVinRecord">The source <see cref="DbGdaQVinRecord"/> entity.</param>
        /// <param name="failureReason">The reason for the failure.</param>
        /// <returns>A new <see cref="DbGdaQVinRecordFail"/> entity.</returns>
        DbGdaQVinRecordFail CreateEntity(DbGdaQVinRecord dbGdaQVinRecord, string failureReason);
    }
}
