using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class that creates <see cref="DbGdaQBluetoothRecordFail"/> entities from <see cref="DbGdaQBluetoothRecord"/> entities.
    /// </summary>
    public interface IDbGdaQBluetoothRecordDbGdaQBluetoothRecordFailEntityMapper
    {
        /// <summary>
        /// Creates a <see cref="DbGdaQBluetoothRecordFail"/> entity from a <see cref="DbGdaQBluetoothRecord"/> entity.
        /// </summary>
        /// <param name="dbGdaQBluetoothRecord">The source <see cref="DbGdaQBluetoothRecord"/> entity.</param>
        /// <param name="failureReason">The reason for the failure.</param>
        /// <returns>A new <see cref="DbGdaQBluetoothRecordFail"/> entity.</returns>
        DbGdaQBluetoothRecordFail CreateEntity(DbGdaQBluetoothRecord dbGdaQBluetoothRecord, string failureReason);
    }
}
