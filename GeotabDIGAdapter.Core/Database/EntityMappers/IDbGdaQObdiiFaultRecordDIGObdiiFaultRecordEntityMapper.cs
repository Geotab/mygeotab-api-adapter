using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DIGObdiiFaultRecord"/> entities from <see cref="DbGdaQObdiiFaultRecord"/> entities.
    /// </summary>
    public interface IDbGdaQObdiiFaultRecordDIGObdiiFaultRecordEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DIGObdiiFaultRecord"/> entity from a <see cref="DbGdaQObdiiFaultRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQObdiiFaultRecord">The source <see cref="DbGdaQObdiiFaultRecord"/> entity.</param>
        /// <param name="serialNo">The Geotab serial number for the device.</param>
        /// <returns>A new <see cref="DIGObdiiFaultRecord"/> entity.</returns>
        DIGObdiiFaultRecord CreateEntity(DbGdaQObdiiFaultRecord dbGdaQObdiiFaultRecord, string serialNo);
    }
}
