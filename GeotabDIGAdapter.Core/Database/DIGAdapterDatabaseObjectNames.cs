namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// A class that lists names of objects (e.g. tables, views, etc.) in the DIG Adapter database.
    /// </summary>
    public class DIGAdapterDatabaseObjectNames : IDIGAdapterDatabaseObjectNames
    {
        /// <inheritdoc/>
        public string DIGAdapterDatabaseNickname => "DIG Adapter";

        /// <inheritdoc/>
        public string DIGAdapterSchemaName => "gda";

        /// <inheritdoc/>
        public string DbGdaMiddlewareVersionInfoTableName => $"{DIGAdapterSchemaName}.MiddlewareVersionInfo";

        /// <inheritdoc/>
        public string DbGdaOServiceTrackingTableName => $"{DIGAdapterSchemaName}.OServiceTracking";

        /// <inheritdoc/>
        public string DbGdaProvisionedDevicesTableName => $"{DIGAdapterSchemaName}.ProvisionedDevices";

        /// <inheritdoc/>
        public string DbGdaQProvisionDeviceTableName => $"{DIGAdapterSchemaName}.Q_ProvisionDevices";

        /// <inheritdoc/>
        public string DbGdaQProvisionDeviceFailTableName => $"{DIGAdapterSchemaName}.Q_ProvisionDevicesFail";

        /// <inheritdoc/>
        public string SpClaimQProvisionDevicesBatchName => $"{DIGAdapterSchemaName}.spClaimQProvisionDevicesBatch";

        /// <inheritdoc/>
        public string Id { get; private set; }

        #region DIG Telemetry Record Queue Tables

        // GPS Records
        /// <inheritdoc/>
        public string DbGdaQGpsRecordTableName => $"{DIGAdapterSchemaName}.Q_GpsRecords";

        /// <inheritdoc/>
        public string DbGdaQGpsRecordFailTableName => $"{DIGAdapterSchemaName}.Q_GpsRecordsFail";

        /// <inheritdoc/>
        public string SpClaimQGpsRecordsBatchName => $"{DIGAdapterSchemaName}.spClaimQGpsRecordsBatch";

        // Acceleration Records
        /// <inheritdoc/>
        public string DbGdaQAccelerationRecordTableName => $"{DIGAdapterSchemaName}.Q_AccelerationRecords";

        /// <inheritdoc/>
        public string DbGdaQAccelerationRecordFailTableName => $"{DIGAdapterSchemaName}.Q_AccelerationRecordsFail";

        /// <inheritdoc/>
        public string SpClaimQAccelerationRecordsBatchName => $"{DIGAdapterSchemaName}.spClaimQAccelerationRecordsBatch";

        // Binary Records
        /// <inheritdoc/>
        public string DbGdaQBinaryRecordTableName => $"{DIGAdapterSchemaName}.Q_BinaryRecords";

        /// <inheritdoc/>
        public string DbGdaQBinaryRecordFailTableName => $"{DIGAdapterSchemaName}.Q_BinaryRecordsFail";

        /// <inheritdoc/>
        public string SpClaimQBinaryRecordsBatchName => $"{DIGAdapterSchemaName}.spClaimQBinaryRecordsBatch";

        // Bluetooth Records
        /// <inheritdoc/>
        public string DbGdaQBluetoothRecordTableName => $"{DIGAdapterSchemaName}.Q_BluetoothRecords";

        /// <inheritdoc/>
        public string DbGdaQBluetoothRecordFailTableName => $"{DIGAdapterSchemaName}.Q_BluetoothRecordsFail";

        /// <inheritdoc/>
        public string SpClaimQBluetoothRecordsBatchName => $"{DIGAdapterSchemaName}.spClaimQBluetoothRecordsBatch";

        // Driver Change Records
        /// <inheritdoc/>
        public string DbGdaQDriverChangeRecordTableName => $"{DIGAdapterSchemaName}.Q_DriverChangeRecords";

        /// <inheritdoc/>
        public string DbGdaQDriverChangeRecordFailTableName => $"{DIGAdapterSchemaName}.Q_DriverChangeRecordsFail";

        /// <inheritdoc/>
        public string SpClaimQDriverChangeRecordsBatchName => $"{DIGAdapterSchemaName}.spClaimQDriverChangeRecordsBatch";

        // Generic Fault Records
        /// <inheritdoc/>
        public string DbGdaQGenericFaultRecordTableName => $"{DIGAdapterSchemaName}.Q_GenericFaultRecords";

        /// <inheritdoc/>
        public string DbGdaQGenericFaultRecordFailTableName => $"{DIGAdapterSchemaName}.Q_GenericFaultRecordsFail";

        /// <inheritdoc/>
        public string SpClaimQGenericFaultRecordsBatchName => $"{DIGAdapterSchemaName}.spClaimQGenericFaultRecordsBatch";

        // Generic Status Records
        /// <inheritdoc/>
        public string DbGdaQGenericStatusRecordTableName => $"{DIGAdapterSchemaName}.Q_GenericStatusRecords";

        /// <inheritdoc/>
        public string DbGdaQGenericStatusRecordFailTableName => $"{DIGAdapterSchemaName}.Q_GenericStatusRecordsFail";

        /// <inheritdoc/>
        public string SpClaimQGenericStatusRecordsBatchName => $"{DIGAdapterSchemaName}.spClaimQGenericStatusRecordsBatch";

        // J1708 Fault Records
        /// <inheritdoc/>
        public string DbGdaQJ1708FaultRecordTableName => $"{DIGAdapterSchemaName}.Q_J1708FaultRecords";

        /// <inheritdoc/>
        public string DbGdaQJ1708FaultRecordFailTableName => $"{DIGAdapterSchemaName}.Q_J1708FaultRecordsFail";

        /// <inheritdoc/>
        public string SpClaimQJ1708FaultRecordsBatchName => $"{DIGAdapterSchemaName}.spClaimQJ1708FaultRecordsBatch";

        // J1939 Fault Records
        /// <inheritdoc/>
        public string DbGdaQJ1939FaultRecordTableName => $"{DIGAdapterSchemaName}.Q_J1939FaultRecords";

        /// <inheritdoc/>
        public string DbGdaQJ1939FaultRecordFailTableName => $"{DIGAdapterSchemaName}.Q_J1939FaultRecordsFail";

        /// <inheritdoc/>
        public string SpClaimQJ1939FaultRecordsBatchName => $"{DIGAdapterSchemaName}.spClaimQJ1939FaultRecordsBatch";

        // OBDII Fault Records
        /// <inheritdoc/>
        public string DbGdaQObdiiFaultRecordTableName => $"{DIGAdapterSchemaName}.Q_ObdiiFaultRecords";

        /// <inheritdoc/>
        public string DbGdaQObdiiFaultRecordFailTableName => $"{DIGAdapterSchemaName}.Q_ObdiiFaultRecordsFail";

        /// <inheritdoc/>
        public string SpClaimQObdiiFaultRecordsBatchName => $"{DIGAdapterSchemaName}.spClaimQObdiiFaultRecordsBatch";

        // VIN Records
        /// <inheritdoc/>
        public string DbGdaQVinRecordTableName => $"{DIGAdapterSchemaName}.Q_VinRecords";

        /// <inheritdoc/>
        public string DbGdaQVinRecordFailTableName => $"{DIGAdapterSchemaName}.Q_VinRecordsFail";

        /// <inheritdoc/>
        public string SpClaimQVinRecordsBatchName => $"{DIGAdapterSchemaName}.spClaimQVinRecordsBatch";

        #endregion

        #region DIG Invalid Records Tables

        /// <inheritdoc/>
        public string DbGdaDIGInvalidRecordsTableName => $"{DIGAdapterSchemaName}.DIGInvalidRecords";

        /// <inheritdoc/>
        public string DbGdaDIGInvalidRecordsCursorTableName => $"{DIGAdapterSchemaName}.DIGInvalidRecordsCursor";

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="DIGAdapterDatabaseObjectNames"/> class.
        /// </summary>
        public DIGAdapterDatabaseObjectNames()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
