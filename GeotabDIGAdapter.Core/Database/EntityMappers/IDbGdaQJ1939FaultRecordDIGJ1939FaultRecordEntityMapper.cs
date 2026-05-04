using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DIGJ1939FaultRecord"/> entities from <see cref="DbGdaQJ1939FaultRecord"/> entities.
    /// </summary>
    public interface IDbGdaQJ1939FaultRecordDIGJ1939FaultRecordEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DIGJ1939FaultRecord"/> entity from a <see cref="DbGdaQJ1939FaultRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQJ1939FaultRecord">The source <see cref="DbGdaQJ1939FaultRecord"/> entity.</param>
        /// <param name="serialNo">The Geotab serial number for the device.</param>
        /// <returns>A new <see cref="DIGJ1939FaultRecord"/> entity.</returns>
        DIGJ1939FaultRecord CreateEntity(DbGdaQJ1939FaultRecord dbGdaQJ1939FaultRecord, string serialNo);
    }
}
