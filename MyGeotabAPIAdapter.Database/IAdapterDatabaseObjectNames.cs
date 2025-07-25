using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// Interface for a class that lists names of objects (e.g. tables, views, etc.) in the Adapter database.
    /// </summary>
    public interface IAdapterDatabaseObjectNames
    {
        /// <summary>
        /// The nickname of the Adapter database. For use in logging.
        /// </summary>
        string AdapterDatabaseNickname { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbBinaryData"/> entities.
        /// </summary>
        string DbBinaryDataTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbBinaryData2"/> entities.
        /// </summary>
        string DbBinaryData2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbChargeEvent"/> entities.
        /// </summary>
        string DbChargeEventTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbChargeEvent2"/> entities.
        /// </summary>
        string DbChargeEvent2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbConfigFeedVersion"/> entities.
        /// </summary>
        string DbConfigFeedVersionsTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDBMaintenanceLog2"/> entities.
        /// </summary>
        string DbDBMaintenanceLog2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDebugData"/> entities.
        /// </summary>
        string DbDebugDataTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDeviceStatusInfo2"/> entities.
        /// </summary>
        string DbDeviceStatusInfo2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDevice"/> entities.
        /// </summary>
        string DbDeviceTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDevice2"/> entities.
        /// </summary>
        string DbDevice2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDiagnostic"/> entities.
        /// </summary>
        string DbDiagnosticTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDiagnostic2"/> entities.
        /// </summary>
        string DbDiagnostic2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDiagnosticId2"/> entities.
        /// </summary>
        string DbDiagnosticId2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDriverChange"/> entities.
        /// </summary>
        string DbDriverChangeTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDriverChange2"/> entities.
        /// </summary>
        string DbDriverChange2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDutyStatusAvailability"/> entities.
        /// </summary>
        string DbDutyStatusAvailabilityTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDutyStatusAvailability2"/> entities.
        /// </summary>
        string DbDutyStatusAvailability2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDutyStatusLog"/> entities.
        /// </summary>
        string DbDutyStatusLogsTableName { get; }


        /// <summary>
        /// The name of the database table associated with <see cref="DbDVIRDefect"/> entities.
        /// </summary>
        string DbDVIRDefectTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDVIRDefect2"/> entities.
        /// </summary>
        string DbDVIRDefect2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDVIRDefectRemark"/> entities.
        /// </summary>
        string DbDVIRDefectRemarkTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDVIRDefectRemark2"/> entities.
        /// </summary>
        string DbDVIRDefectRemark2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDVIRDefectUpdate"/> entities.
        /// </summary>
        string DbDVIRDefectUpdatesTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDVIRLog"/> entities.
        /// </summary>
        string DbDVIRLogTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDVIRLog2"/> entities.
        /// </summary>
        string DbDVIRLog2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbExceptionEvent"/> entities.
        /// </summary>
        string DbExceptionEventTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbExceptionEvent2"/> entities.
        /// </summary>
        string DbExceptionEvent2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbFailedDVIRDefectUpdate"/> entities.
        /// </summary>
        string DbFailedDVIRDefectUpdatesTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbFailedOVDSServerCommand"/> entities.
        /// </summary>
        string DbFailedOVDSServerCommandTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbFaultData"/> entities.
        /// </summary>
        string DbFaultDataTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbFaultData2"/> entities.
        /// </summary>
        string DbFaultData2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbFaultData2WithLagLeadLongLat"/> entities.
        /// </summary>
        string DbFaultData2WithLagLeadLongLatStoredProcedureName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbFaultDataLocation2"/> entities.
        /// </summary>
        string DbFaultDataLocation2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbFaultDataLocation2Tracking"/> entities.
        /// </summary>
        string DbFaultDataLocation2TrackingTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbFuelAndEnergyUsed"/> entities.
        /// </summary>
        string DbFuelAndEnergyUsed2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGroup"/> entities.
        /// </summary>
        string DbGroupTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbGroup2"/> entities.
        /// </summary>
        string DbGroup2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbLogRecord"/> entities.
        /// </summary>
        string DbLogRecordTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbLogRecord2"/> entities.
        /// </summary>
        string DbLogRecord2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbOVDSServerCommand"/> entities.
        /// </summary>
        string DbOVDSServerCommandTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDBPartitionInfo2"/> entities.
        /// </summary>
        string DBPartitionInfo2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbRule"/> entities.
        /// </summary>
        string DbRuleTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbRule2"/> entities.
        /// </summary>
        string DbRule2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStatusData"/> entities.
        /// </summary>
        string DbStatusDataTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStatusData2"/> entities.
        /// </summary>
        string DbStatusData2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStatusData2WithLagLeadLongLat"/> entities.
        /// </summary>
        string DbStatusData2WithLagLeadLongLatStoredProcedureName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStatusDataLocation2"/> entities.
        /// </summary>
        string DbStatusDataLocation2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStatusDataLocation2Tracking"/> entities.
        /// </summary>
        string DbStatusDataLocation2TrackingTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgChargeEvent2"/> entities.
        /// </summary>
        string DbStgChargeEvent2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgDevice2"/> entities.
        /// </summary>
        string DbStgDevice2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgDeviceStatusInfo2"/> entities.
        /// </summary>
        string DbStgDeviceStatusInfo2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgDiagnostic2"/> entities.
        /// </summary>
        string DbStgDiagnostic2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgDriverChange2"/> entities.
        /// </summary>
        string DbStgDriverChange2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgDutyStatusAvailability2"/> entities.
        /// </summary>
        string DbStgDutyStatusAvailability2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgDVIRDefect2"/> entities.
        /// </summary>
        string DbStgDVIRDefect2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgDVIRDefectRemark2"/> entities.
        /// </summary>
        string DbStgDVIRDefectRemark2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgDVIRLog2"/> entities.
        /// </summary>
        string DbStgDVIRLog2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgExceptionEvent2"/> entities.
        /// </summary>
        string DbStgExceptionEvent2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgFuelAndEnergyUsed2"/> entities.
        /// </summary>
        string DbStgFuelAndEnergyUsed2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgGroup2"/> entities.
        /// </summary>
        string DbStgGroup2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgRule2"/> entities.
        /// </summary>
        string DbStgRule2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgTrip2"/> entities.
        /// </summary>
        string DbStgTrip2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgUser2"/> entities.
        /// </summary>
        string DbStgUser2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgZone2"/> entities.
        /// </summary>
        string DbStgZone2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStgZoneType2"/> entities.
        /// </summary>
        string DbStgZoneType2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbTrip"/> entities.
        /// </summary>
        string DbTripTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbTrip2"/> entities.
        /// </summary>
        string DbTrip2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbUser"/> entities.
        /// </summary>
        string DbUserTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbUser2"/> entities.
        /// </summary>
        string DbUser2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbZone"/> entities.
        /// </summary>
        string DbZoneTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbZone2"/> entities.
        /// </summary>
        string DbZone2TableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbZoneType"/> entities.
        /// </summary>
        string DbZoneTypeTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbZoneType2"/> entities.
        /// </summary>
        string DbZoneType2TableName { get; }

        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging ChargeEvents into the main ChargeEvents2 table.
        /// </summary>
        string MergeStagingChargeEventsProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging Devices into the main Devices2 table.
        /// </summary>
        string MergeStagingDevicesProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging DeviceStatusInfo records into the main DeviceStatusInfo2 table.
        /// </summary>
        string MergeStagingDeviceStatusInfoProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging Diagnostics into the main Diagnostics2 table.
        /// </summary>
        string MergeStagingDiagnosticsProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging DriverChanges into the main DriverChanges2 table.
        /// </summary>
        string MergeStagingDriverChangesProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging DutyStatusAvailabilities into the main DutyStatusAvailabilities2 table.
        /// </summary>
        string MergeStagingDutyStatusAvailabilitiesProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging DVIRDefects into the main DVIRDefects2 table.
        /// </summary>
        string MergeStagingDVIRDefectsProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging DVIRDefectRemarks into the main DVIRDefectRemarks2 table.
        /// </summary>
        string MergeStagingDVIRDefectRemarksProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging DVIRLogs into the main DVIRLogs2 table.
        /// </summary>
        string MergeStagingDVIRLogsProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging ExceptionEvents into the main ExceptionEvents2 table.
        /// </summary>
        string MergeStagingExceptionEventsProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging FuelAndEnergyUsed records into the main FuelAndEnergyUsed2 table.
        /// </summary>
        string MergeStagingFuelAndEnergyUsedProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging Groups into the main Groups2 table.
        /// </summary>
        string MergeStagingGroupsProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging Rules into the main Rules2 table.
        /// </summary>
        string MergeStagingRulesProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging Trips into the main Trips2 table.
        /// </summary>
        string MergeStagingTripsProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging Users into the main Users2 table.
        /// </summary>
        string MergeStagingUsersProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging Zones into the main Zones2 table.
        /// </summary>
        string MergeStagingZonesProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for merging staging ZoneTypes into the main ZoneTypes2 table.
        /// </summary>
        string MergeStagingZoneTypesProcedureName { get; }

        /// <summary>
        /// The name of the stored procedure or function responsible for database partitioning.
        /// </summary>
        string PartitioningProcedureName { get; }
    }
}
