using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DIGAccelerationRecord"/> entities from <see cref="DbGdaQAccelerationRecord"/> entities.
    /// </summary>
    public interface IDbGdaQAccelerationRecordDIGAccelerationRecordEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DIGAccelerationRecord"/> entity from a <see cref="DbGdaQAccelerationRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQAccelerationRecord">The source <see cref="DbGdaQAccelerationRecord"/> entity.</param>
        /// <param name="serialNo">The Geotab serial number for the device.</param>
        /// <returns>A new <see cref="DIGAccelerationRecord"/> entity.</returns>
        DIGAccelerationRecord CreateEntity(DbGdaQAccelerationRecord dbGdaQAccelerationRecord, string serialNo);
    }
}
