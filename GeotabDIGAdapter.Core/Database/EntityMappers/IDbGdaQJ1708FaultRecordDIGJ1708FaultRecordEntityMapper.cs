using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DIGJ1708FaultRecord"/> entities from <see cref="DbGdaQJ1708FaultRecord"/> entities.
    /// </summary>
    public interface IDbGdaQJ1708FaultRecordDIGJ1708FaultRecordEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DIGJ1708FaultRecord"/> entity from a <see cref="DbGdaQJ1708FaultRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQJ1708FaultRecord">The source <see cref="DbGdaQJ1708FaultRecord"/> entity.</param>
        /// <param name="serialNo">The Geotab serial number for the device.</param>
        /// <returns>A new <see cref="DIGJ1708FaultRecord"/> entity.</returns>
        DIGJ1708FaultRecord CreateEntity(DbGdaQJ1708FaultRecord dbGdaQJ1708FaultRecord, string serialNo);
    }
}
