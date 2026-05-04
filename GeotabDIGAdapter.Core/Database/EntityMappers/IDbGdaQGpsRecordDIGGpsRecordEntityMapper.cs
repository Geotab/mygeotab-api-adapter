using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DIGGpsRecord"/> entities from <see cref="DbGdaQGpsRecord"/> entities.
    /// </summary>
    public interface IDbGdaQGpsRecordDIGGpsRecordEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DIGGpsRecord"/> entity from a <see cref="DbGdaQGpsRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQGpsRecord">The source <see cref="DbGdaQGpsRecord"/> entity.</param>
        /// <param name="serialNo">The Geotab serial number for the device.</param>
        /// <returns>A new <see cref="DIGGpsRecord"/> entity.</returns>
        DIGGpsRecord CreateEntity(DbGdaQGpsRecord dbGdaQGpsRecord, string serialNo);
    }
}
