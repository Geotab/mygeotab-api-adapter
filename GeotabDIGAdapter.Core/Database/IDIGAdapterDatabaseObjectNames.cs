using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// Interface for a class that lists names of objects (e.g. tables, views, etc.) in the DIG Adapter database.
    /// </summary>
    public interface IDIGAdapterDatabaseObjectNames
    {
        /// <summary>
        /// The nickname of the DIG Adapter database. For use in logging.
        /// </summary>
        string DIGAdapterDatabaseNickname { get; }

        /// <summary>
        /// The name of the database schema used by the DIG Adapter.
        /// </summary>
        string DIGAdapterSchemaName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaMiddlewareVersionInfo"/> entities.
        /// </summary>
        string DbGdaMiddlewareVersionInfoTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaOServiceTracking"/> entities.
        /// </summary>
        string DbGdaOServiceTrackingTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaProvisionedDevice"/> entities.
        /// </summary>
        string DbGdaProvisionedDevicesTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQProvisionDevice"/> entities.
        /// </summary>
        string DbGdaQProvisionDeviceTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQProvisionDeviceFail"/> entities.
        /// </summary>
        string DbGdaQProvisionDeviceFailTableName { get; }

        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for atomically claiming a batch of Q_ProvisionDevices records for processing.
        /// </summary>
        string SpClaimQProvisionDevicesBatchName { get; }

        #region DIG Telemetry Record Queue Tables

        // GPS Records
        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQGpsRecord"/> entities.
        /// </summary>
        string DbGdaQGpsRecordTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQGpsRecordFail"/> entities.
        /// </summary>
        string DbGdaQGpsRecordFailTableName { get; }

        /// <summary>
        /// The name of the stored procedure responsible for atomically claiming a batch of Q_GpsRecords records for processing.
        /// </summary>
        string SpClaimQGpsRecordsBatchName { get; }

        // Acceleration Records
        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQAccelerationRecord"/> entities.
        /// </summary>
        string DbGdaQAccelerationRecordTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQAccelerationRecordFail"/> entities.
        /// </summary>
        string DbGdaQAccelerationRecordFailTableName { get; }

        /// <summary>
        /// The name of the stored procedure responsible for atomically claiming a batch of Q_AccelerationRecords records for processing.
        /// </summary>
        string SpClaimQAccelerationRecordsBatchName { get; }

        // Binary Records
        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQBinaryRecord"/> entities.
        /// </summary>
        string DbGdaQBinaryRecordTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQBinaryRecordFail"/> entities.
        /// </summary>
        string DbGdaQBinaryRecordFailTableName { get; }

        /// <summary>
        /// The name of the stored procedure responsible for atomically claiming a batch of Q_BinaryRecords records for processing.
        /// </summary>
        string SpClaimQBinaryRecordsBatchName { get; }

        // Bluetooth Records
        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQBluetoothRecord"/> entities.
        /// </summary>
        string DbGdaQBluetoothRecordTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQBluetoothRecordFail"/> entities.
        /// </summary>
        string DbGdaQBluetoothRecordFailTableName { get; }

        /// <summary>
        /// The name of the stored procedure responsible for atomically claiming a batch of Q_BluetoothRecords records for processing.
        /// </summary>
        string SpClaimQBluetoothRecordsBatchName { get; }

        // Driver Change Records
        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQDriverChangeRecord"/> entities.
        /// </summary>
        string DbGdaQDriverChangeRecordTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQDriverChangeRecordFail"/> entities.
        /// </summary>
        string DbGdaQDriverChangeRecordFailTableName { get; }

        /// <summary>
        /// The name of the stored procedure responsible for atomically claiming a batch of Q_DriverChangeRecords records for processing.
        /// </summary>
        string SpClaimQDriverChangeRecordsBatchName { get; }

        // Generic Fault Records
        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQGenericFaultRecord"/> entities.
        /// </summary>
        string DbGdaQGenericFaultRecordTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQGenericFaultRecordFail"/> entities.
        /// </summary>
        string DbGdaQGenericFaultRecordFailTableName { get; }

        /// <summary>
        /// The name of the stored procedure responsible for atomically claiming a batch of Q_GenericFaultRecords records for processing.
        /// </summary>
        string SpClaimQGenericFaultRecordsBatchName { get; }

        // Generic Status Records
        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQGenericStatusRecord"/> entities.
        /// </summary>
        string DbGdaQGenericStatusRecordTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQGenericStatusRecordFail"/> entities.
        /// </summary>
        string DbGdaQGenericStatusRecordFailTableName { get; }

        /// <summary>
        /// The name of the stored procedure responsible for atomically claiming a batch of Q_GenericStatusRecords records for processing.
        /// </summary>
        string SpClaimQGenericStatusRecordsBatchName { get; }

        // J1708 Fault Records
        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQJ1708FaultRecord"/> entities.
        /// </summary>
        string DbGdaQJ1708FaultRecordTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQJ1708FaultRecordFail"/> entities.
        /// </summary>
        string DbGdaQJ1708FaultRecordFailTableName { get; }

        /// <summary>
        /// The name of the stored procedure responsible for atomically claiming a batch of Q_J1708FaultRecords records for processing.
        /// </summary>
        string SpClaimQJ1708FaultRecordsBatchName { get; }

        // J1939 Fault Records
        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQJ1939FaultRecord"/> entities.
        /// </summary>
        string DbGdaQJ1939FaultRecordTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQJ1939FaultRecordFail"/> entities.
        /// </summary>
        string DbGdaQJ1939FaultRecordFailTableName { get; }

        /// <summary>
        /// The name of the stored procedure responsible for atomically claiming a batch of Q_J1939FaultRecords records for processing.
        /// </summary>
        string SpClaimQJ1939FaultRecordsBatchName { get; }

        // OBDII Fault Records
        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQObdiiFaultRecord"/> entities.
        /// </summary>
        string DbGdaQObdiiFaultRecordTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQObdiiFaultRecordFail"/> entities.
        /// </summary>
        string DbGdaQObdiiFaultRecordFailTableName { get; }

        /// <summary>
        /// The name of the stored procedure responsible for atomically claiming a batch of Q_ObdiiFaultRecords records for processing.
        /// </summary>
        string SpClaimQObdiiFaultRecordsBatchName { get; }

        // VIN Records
        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQVinRecord"/> entities.
        /// </summary>
        string DbGdaQVinRecordTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaQVinRecordFail"/> entities.
        /// </summary>
        string DbGdaQVinRecordFailTableName { get; }

        /// <summary>
        /// The name of the stored procedure responsible for atomically claiming a batch of Q_VinRecords records for processing.
        /// </summary>
        string SpClaimQVinRecordsBatchName { get; }

        #endregion

        #region DIG Invalid Records Tables

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaDIGInvalidRecord"/> entities.
        /// </summary>
        string DbGdaDIGInvalidRecordsTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGdaDIGInvalidRecordsCursor"/> entities.
        /// </summary>
        string DbGdaDIGInvalidRecordsCursorTableName { get; }

        #endregion
    }
}
