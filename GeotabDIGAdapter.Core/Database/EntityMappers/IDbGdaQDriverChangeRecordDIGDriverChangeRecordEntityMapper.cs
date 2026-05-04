using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DIGDriverChangeRecord"/> entities from <see cref="DbGdaQDriverChangeRecord"/> entities.
    /// </summary>
    public interface IDbGdaQDriverChangeRecordDIGDriverChangeRecordEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DIGDriverChangeRecord"/> entity from a <see cref="DbGdaQDriverChangeRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQDriverChangeRecord">The source <see cref="DbGdaQDriverChangeRecord"/> entity.</param>
        /// <param name="serialNo">The Geotab serial number for the device.</param>
        /// <returns>A new <see cref="DIGDriverChangeRecord"/> entity.</returns>
        DIGDriverChangeRecord CreateEntity(DbGdaQDriverChangeRecord dbGdaQDriverChangeRecord, string serialNo);
    }
}
