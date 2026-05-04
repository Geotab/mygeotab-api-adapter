using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DIGBinaryRecord"/> entities from <see cref="DbGdaQBinaryRecord"/> entities.
    /// </summary>
    public interface IDbGdaQBinaryRecordDIGBinaryRecordEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DIGBinaryRecord"/> entity from a <see cref="DbGdaQBinaryRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQBinaryRecord">The source <see cref="DbGdaQBinaryRecord"/> entity.</param>
        /// <param name="serialNo">The Geotab serial number for the device.</param>
        /// <returns>A new <see cref="DIGBinaryRecord"/> entity.</returns>
        DIGBinaryRecord CreateEntity(DbGdaQBinaryRecord dbGdaQBinaryRecord, string serialNo);
    }
}
