using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DIGGenericFaultRecord"/> entities from <see cref="DbGdaQGenericFaultRecord"/> entities.
    /// </summary>
    public interface IDbGdaQGenericFaultRecordDIGGenericFaultRecordEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DIGGenericFaultRecord"/> entity from a <see cref="DbGdaQGenericFaultRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQGenericFaultRecord">The source <see cref="DbGdaQGenericFaultRecord"/> entity.</param>
        /// <param name="serialNo">The Geotab serial number for the device.</param>
        /// <returns>A new <see cref="DIGGenericFaultRecord"/> entity.</returns>
        DIGGenericFaultRecord CreateEntity(DbGdaQGenericFaultRecord dbGdaQGenericFaultRecord, string serialNo);
    }
}
