using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DbGdaQBinaryRecordFail"/> entities from <see cref="DbGdaQBinaryRecord"/> entities.
    /// </summary>
    public interface IDbGdaQBinaryRecordDbGdaQBinaryRecordFailEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DbGdaQBinaryRecordFail"/> entity from a <see cref="DbGdaQBinaryRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQBinaryRecord">The source <see cref="DbGdaQBinaryRecord"/> entity.</param>
        /// <param name="failureReason">The reason for the failure.</param>
        /// <returns>A new <see cref="DbGdaQBinaryRecordFail"/> entity.</returns>
        DbGdaQBinaryRecordFail CreateEntity(DbGdaQBinaryRecord dbGdaQBinaryRecord, string failureReason);
    }
}
