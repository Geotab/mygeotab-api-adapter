-- ================================================================================
-- DATABASE TYPE: PostgreSQL
-- 
-- NOTES: 
--   1: This script applies to OLDER versions of the MyGeotab API Adapter database 
--      (prior to application version 3.0.0). It does not apply to later versions 
--      of the application except in any cases where v1 and v2 versions of a new
--      feed may be implemented. 
--   2: This script is updated as new tables are added to the database (i.e. there
--      is only one version of this script).
--
-- DESCRIPTION: 
--   This script is intended for manual use in the following situations:
-- 
--     1: Obtaining record counts for each of the database tables:
--          - Simply execute the script as-is to obtain record counts.
--     2: Clearing all database tables and reseeding identity values:
--          - Uncomment the commented-out lines below and then execute the script
--            to clear all tables of data. 
-- ================================================================================

-- /* Clean Database */ 
-- delete from public."BinaryData";
-- delete from public."ChargeEvents";
-- delete from public."Conditions";
-- delete from public."DebugData";
-- delete from public."Devices";
-- delete from public."DeviceStatusInfo";
-- delete from public."Diagnostics";
-- delete from public."DriverChanges";
-- delete from public."DutyStatusAvailabilities";
-- delete from public."DutyStatusLogs";
-- delete from public."DVIRDefectRemarks";
-- delete from public."DVIRDefects";
-- delete from public."DVIRDefectUpdates";
-- delete from public."DVIRLogs";
-- delete from public."ExceptionEvents";
-- delete from public."FailedDVIRDefectUpdates";
-- delete from public."FailedOVDSServerCommands";
-- delete from public."FaultData";
-- delete from public."Groups";
-- delete from public."LogRecords";
-- delete from public."MyGeotabVersionInfo";
-- delete from public."OServiceTracking";
-- delete from public."OVDSServerCommands";
-- delete from public."Rules";
-- delete from public."StatusData";
-- delete from public."Trips";
-- delete from public."Users";
-- delete from public."Zones";
-- delete from public."ZoneTypes";
-- ALTER SEQUENCE public."BinaryData_id_seq" RESTART;
-- ALTER SEQUENCE public."ChargeEvents_id_seq" RESTART;
-- ALTER SEQUENCE public."Conditions_id_seq" RESTART;
-- ALTER SEQUENCE public."DebugData_id_seq" RESTART;
-- ALTER SEQUENCE public."Devices_id_seq" RESTART;
-- ALTER SEQUENCE public."DeviceStatusInfo_id_seq" RESTART;
-- ALTER SEQUENCE public."Diagnostics_id_seq" RESTART;
-- ALTER SEQUENCE public."DriverChanges_id_seq" RESTART;
-- ALTER SEQUENCE public."DutyStatusAvailabilities_id_seq" RESTART;
-- ALTER SEQUENCE public."DutyStatusLogs_id_seq" RESTART;
-- ALTER SEQUENCE public."DVIRDefectRemarks_id_seq" RESTART;
-- ALTER SEQUENCE public."DVIRDefects_id_seq" RESTART;
-- ALTER SEQUENCE public."DVIRDefectUpdates_id_seq" RESTART;
-- ALTER SEQUENCE public."DVIRLogs_id_seq" RESTART;
-- ALTER SEQUENCE public."ExceptionEvents_id_seq" RESTART;
-- ALTER SEQUENCE public."FailedDVIRDefectUpdates_id_seq" RESTART;
-- ALTER SEQUENCE public."FailedOVDSServerCommands_id_seq" RESTART;
-- ALTER SEQUENCE public."FaultData_id_seq" RESTART;
-- ALTER SEQUENCE public."Groups_id_seq" RESTART;
-- ALTER SEQUENCE public."LogRecords_id_seq" RESTART;
-- ALTER SEQUENCE public."OServiceTracking_id_seq" RESTART;
-- ALTER SEQUENCE public."OVDSServerCommands_id_seq" RESTART;
-- ALTER SEQUENCE public."Rules_id_seq" RESTART;
-- ALTER SEQUENCE public."StatusData_id_seq" RESTART;
-- ALTER SEQUENCE public."Trips_id_seq" RESTART;
-- ALTER SEQUENCE public."Users_id_seq" RESTART;
-- ALTER SEQUENCE public."Zones_id_seq" RESTART;
-- ALTER SEQUENCE public."ZoneTypes_id_seq" RESTART;

/* Check counts */
select 'BinaryData' as "TableName", count(0) as "RecordCount" from public."BinaryData"
union all
select 'ChargeEvents', count(0) from public."ChargeEvents"
union all
select 'Conditions', count(0) from public."Conditions"
union all
select 'DebugData', count(0) from public."DebugData"
union all
select 'Devices', count(0) from public."Devices"
union all
select 'DeviceStatusInfo', count(0) from public."DeviceStatusInfo"
union all
select 'Diagnostics', count(0) from public."Diagnostics"
union all
select 'DriverChanges', count(0) from public."DriverChanges"
union all
select 'DVIRDefectRemarks', count(0) from public."DVIRDefectRemarks"
union all
select 'DVIRDefects', count(0) from public."DVIRDefects"
union all
select 'DVIRDefectUpdates', count(0) from public."DVIRDefectUpdates"
union all
select 'DVIRLogs', count(0) from public."DVIRLogs"
union all
select 'DutyStatusAvailabilities', count(0) from public."DutyStatusAvailabilities"
union all
select 'DutyStatusLogs', count(0) from public."DutyStatusLogs"
union all
select 'ExceptionEvents', count(0) from public."ExceptionEvents"
union all
select 'FailedDVIRDefectUpdates', count(0) from public."FailedDVIRDefectUpdates"
union all
select 'FailedOVDSServerCommands', count(0) from public."FailedOVDSServerCommands"
union all
select 'FaultData', count(0) from public."FaultData"
union all
select 'Groups', count(0) from public."Groups"
union all
select 'LogRecords', count(0) from public."LogRecords"
union all
select 'MyGeotabVersionInfo', count(0) from public."MyGeotabVersionInfo"
union all
select 'OServiceTracking', count(0) from public."OServiceTracking"
union all
select 'OVDSServerCommands', count(0) from public."OVDSServerCommands"
union all
select 'Rules', count(0) from public."Rules"
union all
select 'StatusData', count(0) from public."StatusData"
union all
select 'Trips', count(0) from public."Trips"
union all
select 'Users', count(0) from public."Users"
union all
select 'Zones', count(0) from public."Zones"
union all
select 'ZoneTypes', count(0) from public."ZoneTypes"
order by "TableName";
