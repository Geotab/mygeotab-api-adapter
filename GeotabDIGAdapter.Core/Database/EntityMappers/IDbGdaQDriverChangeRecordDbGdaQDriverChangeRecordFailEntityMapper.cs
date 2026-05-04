using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DbGdaQDriverChangeRecordFail"/> entities from <see cref="DbGdaQDriverChangeRecord"/> entities.
    /// </summary>
    public interface IDbGdaQDriverChangeRecordDbGdaQDriverChangeRecordFailEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DbGdaQDriverChangeRecordFail"/> entity from a <see cref="DbGdaQDriverChangeRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQDriverChangeRecord">The source <see cref="DbGdaQDriverChangeRecord"/> entity.</param>
        /// <param name="failureReason">The reason for the failure.</param>
        /// <returns>A new <see cref="DbGdaQDriverChangeRecordFail"/> entity.</returns>
        DbGdaQDriverChangeRecordFail CreateEntity(DbGdaQDriverChangeRecord dbGdaQDriverChangeRecord, string failureReason);
    }
}
