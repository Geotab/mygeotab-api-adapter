using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DIGBluetoothRecord"/> entities from <see cref="DbGdaQBluetoothRecord"/> entities.
    /// </summary>
    public interface IDbGdaQBluetoothRecordDIGBluetoothRecordEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DIGBluetoothRecord"/> entity from a <see cref="DbGdaQBluetoothRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQBluetoothRecord">The source <see cref="DbGdaQBluetoothRecord"/> entity.</param>
        /// <param name="serialNo">The Geotab serial number for the device.</param>
        /// <returns>A new <see cref="DIGBluetoothRecord"/> entity.</returns>
        DIGBluetoothRecord CreateEntity(DbGdaQBluetoothRecord dbGdaQBluetoothRecord, string serialNo);
    }
}
