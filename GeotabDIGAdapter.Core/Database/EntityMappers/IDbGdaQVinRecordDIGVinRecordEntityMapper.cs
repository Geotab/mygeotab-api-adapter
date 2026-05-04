using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DIGVinRecord"/> entities from <see cref="DbGdaQVinRecord"/> entities.
    /// </summary>
    public interface IDbGdaQVinRecordDIGVinRecordEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DIGVinRecord"/> entity from a <see cref="DbGdaQVinRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQVinRecord">The source <see cref="DbGdaQVinRecord"/> entity.</param>
        /// <param name="serialNo">The Geotab serial number for the device.</param>
        /// <returns>A new <see cref="DIGVinRecord"/> entity.</returns>
        DIGVinRecord CreateEntity(DbGdaQVinRecord dbGdaQVinRecord, string serialNo);
    }
}
