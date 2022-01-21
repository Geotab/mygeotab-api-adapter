namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// A class that lists names of objects (e.g. tables, views, etc.) in the Optimizer database.
    /// </summary>
    public class OptimizerDatabaseObjectNames : IOptimizerDatabaseObjectNames
    {
        /// <inheritdoc/>
        public string DbBinaryDataTTableName => "BinaryDataT";

        /// <inheritdoc/>
        public string DbBinaryTypeTTableName => "BinaryTypesT";

        /// <inheritdoc/>
        public string DbControllerTTableName => "ControllersT";

        /// <inheritdoc/>
        public string DbDeviceTTableName => "DevicesT";

        /// <inheritdoc/>
        public string DbDiagnosticTTableName => "DiagnosticsT";

        /// <inheritdoc/>
        public string DbDriverChangeTTableName => "DriverChangesT";

        /// <inheritdoc/>
        public string DbDriverChangeTypeTTableName => "DriverChangeTypesT";

        /// <inheritdoc/>
        public string DbFaultDataTDriverIdUpdateTableName => "FaultDataT";

        /// <inheritdoc/>
        public string DbFaultDataTTableName => "FaultDataT";

        /// <inheritdoc/>
        public string DbFaultDataTLongLatUpdateTableName => "FaultDataT";

        /// <inheritdoc/>
        public string DbFaultDataTWithLagLeadLongLatTableName => "vwFaultDataTWithLagLeadLongLatBatch";

        /// <inheritdoc/>
        public string DbLogRecordTTableName => "LogRecordsT";

        /// <inheritdoc/>
        public string DbOProcessorTrackingTableName => "OProcessorTracking";

        /// <inheritdoc/>
        public string DbStatusDataTDriverIdUpdateTableName => "StatusDataT";

        /// <inheritdoc/>
        public string DbStatusDataTTableName => "StatusDataT";

        /// <inheritdoc/>
        public string DbStatusDataTLongLatUpdateTableName => "StatusDataT";

        /// <inheritdoc/>
        public string DbStatusDataTWithLagLeadLongLatTableName => "vwStatusDataTWithLagLeadLongLatBatch";

        /// <inheritdoc/>
        public string DbUserTTableName => "UsersT";

        /// <inheritdoc/>
        public string GetFaultDataTBatchStoredProcedureName => "uspGetFaultDataTBatch";

        /// <inheritdoc/>
        public string GetLogRecordTBatchStoredProcedureName => "uspGetLogRecordTBatch";

        /// <inheritdoc/>
        public string GetStatusDataTBatchStoredProcedureName => "uspGetStatusDataTBatch";

        /// <inheritdoc/>
        public string OptimizerDatabaseNickname => "Optimizer";
    }
}
