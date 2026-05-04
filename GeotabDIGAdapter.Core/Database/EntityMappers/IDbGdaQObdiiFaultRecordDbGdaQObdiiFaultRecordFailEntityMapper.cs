using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DbGdaQObdiiFaultRecordFail"/> entities from <see cref="DbGdaQObdiiFaultRecord"/> entities.
    /// </summary>
    public interface IDbGdaQObdiiFaultRecordDbGdaQObdiiFaultRecordFailEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DbGdaQObdiiFaultRecordFail"/> entity from a <see cref="DbGdaQObdiiFaultRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQObdiiFaultRecord">The source <see cref="DbGdaQObdiiFaultRecord"/> entity.</param>
        /// <param name="failureReason">The reason for the failure.</param>
        /// <returns>A new <see cref="DbGdaQObdiiFaultRecordFail"/> entity.</returns>
        DbGdaQObdiiFaultRecordFail CreateEntity(DbGdaQObdiiFaultRecord dbGdaQObdiiFaultRecord, string failureReason);
    }
}
