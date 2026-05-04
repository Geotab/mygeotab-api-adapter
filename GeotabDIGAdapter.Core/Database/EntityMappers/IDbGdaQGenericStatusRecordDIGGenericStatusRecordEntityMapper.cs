using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DIGGenericStatusRecord"/> entities from <see cref="DbGdaQGenericStatusRecord"/> entities.
    /// </summary>
    public interface IDbGdaQGenericStatusRecordDIGGenericStatusRecordEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DIGGenericStatusRecord"/> entity from a <see cref="DbGdaQGenericStatusRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQGenericStatusRecord">The source <see cref="DbGdaQGenericStatusRecord"/> entity.</param>
        /// <param name="serialNo">The Geotab serial number for the device.</param>
        /// <returns>A new <see cref="DIGGenericStatusRecord"/> entity.</returns>
        DIGGenericStatusRecord CreateEntity(DbGdaQGenericStatusRecord dbGdaQGenericStatusRecord, string serialNo);
    }
}
