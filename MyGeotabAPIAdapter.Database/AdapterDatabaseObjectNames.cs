namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// A class that lists names of objects (e.g. tables, views, etc.) in the Adapter database.
    /// </summary>
    public class AdapterDatabaseObjectNames : IAdapterDatabaseObjectNames
    {
        /// <inheritdoc/>
        public string AdapterDatabaseNickname => "Adapter";

        /// <inheritdoc/>
        public string DbBinaryDataTableName => "BinaryData";

        /// <inheritdoc/>
        public string DbConfigFeedVersionsTableName => "ConfigFeedVersions";

        /// <inheritdoc/>
        public string DbDeviceTableName => "Devices";

        /// <inheritdoc/>
        public string DbDiagnosticTableName => "Diagnostics";

        /// <inheritdoc/>
        public string DbDriverChangeTableName => "DriverChanges";

        /// <inheritdoc/>
        public string DbDutyStatusAvailabilityTableName => "DutyStatusAvailability";

        /// <inheritdoc/>
        public string DbDVIRDefectTableName => "DVIRDefects";

        /// <inheritdoc/>
        public string DbDVIRDefectRemarkTableName => "DVIRDefectRemarks";

        /// <inheritdoc/>
        public string DbDVIRDefectUpdatesTableName => "DVIRDefectUpdates";

        /// <inheritdoc/>
        public string DbDVIRLogTableName => "DVIRLogs";

        /// <inheritdoc/>
        public string DbExceptionEventTableName => "ExceptionEvents";

        /// <inheritdoc/>
        public string DbFailedDVIRDefectUpdatesTableName => "FailedDVIRDefectUpdates";

        /// <inheritdoc/>
        public string DbFailedOVDSServerCommandTableName => "FailedOVDSServerCommands";

        /// <inheritdoc/>
        public string DbFaultDataTableName => "FaultData";

        /// <inheritdoc/>
        public string DbLogRecordTableName => "LogRecords";

        /// <inheritdoc/>
        public string DbOVDSServerCommandTableName => "OVDSServerCommands";

        /// <inheritdoc/>
        public string DbRuleTableName => "Rules";

        /// <inheritdoc/>
        public string DbStatusDataTableName => "StatusData";

        /// <inheritdoc/>
        public string DbTripTableName => "Trips";

        /// <inheritdoc/>
        public string DbUserTableName => "Users";

        /// <inheritdoc/>
        public string DbZoneTableName => "Zones";

        /// <inheritdoc/>
        public string DbZoneTypeTableName => "ZoneTypes";

        /// <inheritdoc/>
        public string Id { get; private set; }
    }
}
