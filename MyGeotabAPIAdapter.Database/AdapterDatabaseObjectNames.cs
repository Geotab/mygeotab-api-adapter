﻿namespace MyGeotabAPIAdapter.Database
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
        public string DbChargeEventsTableName => "ChargeEvents";

        /// <inheritdoc/>
        public string DbConfigFeedVersionsTableName => "ConfigFeedVersions";

        /// <inheritdoc/>
        public string DbDBMaintenanceLog2TableName => "DbDBMaintenanceLogs2";

        /// <inheritdoc/>
        public string DbDebugDataTableName => "DebugData";

        /// <inheritdoc/>
        public string DbDeviceTableName => "Devices";

        /// <inheritdoc/>
        public string DbDevice2TableName => "Devices2";

        /// <inheritdoc/>
        public string DbDiagnosticTableName => "Diagnostics";

        /// <inheritdoc/>
        public string DbDiagnostic2TableName => "Diagnostics2";

        /// <inheritdoc/>
        public string DbDiagnosticId2TableName => "DiagnosticIds2";

        /// <inheritdoc/>
        public string DbDriverChangeTableName => "DriverChanges";

        /// <inheritdoc/>
        public string DbDutyStatusAvailabilityTableName => "DutyStatusAvailability";

        /// <inheritdoc/>
        public string DbDutyStatusLogsTableName => "DutyStatusLogs";

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
        public string DbFaultData2TableName => "FaultData2";

        /// <inheritdoc/>
        public string DbFaultData2WithLagLeadLongLatStoredProcedureName => "spFaultData2WithLagLeadLongLatBatch";

        /// <inheritdoc/>
        public string DbFaultDataLocation2TableName => "FaultDataLocations2";

        /// <inheritdoc/>
        public string DbFaultDataLocation2TrackingTableName => "FaultDataLocations2Tracking";

        /// <inheritdoc/>
        public string DbGroupTableName => "Groups";

        /// <inheritdoc/>
        public string DbLogRecordTableName => "LogRecords";

        /// <inheritdoc/>
        public string DbLogRecord2TableName => "LogRecords2";

        /// <inheritdoc/>
        public string DbOVDSServerCommandTableName => "OVDSServerCommands";

        /// <inheritdoc/>
        public string DBPartitionInfo2TableName => "DBPartitionInfo2";

        /// <inheritdoc/>
        public string DbRuleTableName => "Rules";

        /// <inheritdoc/>
        public string DbStatusDataTableName => "StatusData";

        /// <inheritdoc/>
        public string DbStatusData2TableName => "StatusData2";

        /// <inheritdoc/>
        public string DbStatusData2WithLagLeadLongLatStoredProcedureName => "spStatusData2WithLagLeadLongLatBatch";

        /// <inheritdoc/>
        public string DbStatusDataLocation2TableName => "StatusDataLocations2";

        /// <inheritdoc/>
        public string DbStatusDataLocation2TrackingTableName => "StatusDataLocations2Tracking";

        /// <inheritdoc/>
        public string DbTripTableName => "Trips";

        /// <inheritdoc/>
        public string DbUserTableName => "Users";

        /// <inheritdoc/>
        public string DbUser2TableName => "Users2";

        /// <inheritdoc/>
        public string DbZoneTableName => "Zones";

        /// <inheritdoc/>
        public string DbZone2TableName => "Zones2";

        /// <inheritdoc/>
        public string DbZoneTypeTableName => "ZoneTypes";

        /// <inheritdoc/>
        public string DbZoneType2TableName => "ZoneTypes2";

        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <inheritdoc/>
        public string PartitioningProcedureName => "spManagePartitions";
    }
}
