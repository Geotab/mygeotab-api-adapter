/* Clean Database */ 
-- delete from public."Conditions";
-- delete from public."ConfigFeedVersions";
-- delete from public."DVIRDefectRemarks";
-- delete from public."DVIRDefects";
-- delete from public."DVIRDefectUpdates";
-- delete from public."DVIRLogs";
-- delete from public."Devices";
-- delete from public."Diagnostics";
-- delete from public."DutyStatusAvailabilities";
-- delete from public."ExceptionEvents";
-- delete from public."FailedDVIRDefectUpdates";
-- delete from public."FaultData";
-- delete from public."LogRecords";
-- delete from public."MyGeotabVersionInfo";
-- delete from public."Rules";
-- delete from public."StatusData";
-- delete from public."Trips";
-- delete from public."Users";
-- delete from public."Zones";
-- delete from public."ZoneTypes";
-- ALTER SEQUENCE public."Conditions_id_seq" RESTART;
-- ALTER SEQUENCE public."ConfigFeedVersions_id_seq" RESTART;
-- ALTER SEQUENCE public."DVIRDefectRemarks_id_seq" RESTART;
-- ALTER SEQUENCE public."DVIRDefects_id_seq" RESTART;
-- ALTER SEQUENCE public."DVIRDefectUpdates_id_seq" RESTART;
-- ALTER SEQUENCE public."DVIRLogs_id_seq" RESTART;
-- ALTER SEQUENCE public."Devices_id_seq" RESTART;
-- ALTER SEQUENCE public."Diagnostics_id_seq" RESTART;
-- ALTER SEQUENCE public."DutyStatusAvailabilities_id_seq" RESTART;
-- ALTER SEQUENCE public."ExceptionEvents_id_seq" RESTART;
-- ALTER SEQUENCE public."FailedDVIRDefectUpdates_id_seq" RESTART;
-- ALTER SEQUENCE public."FaultData_id_seq" RESTART;
-- ALTER SEQUENCE public."LogRecords_id_seq" RESTART;
-- ALTER SEQUENCE public."Rules_id_seq" RESTART;
-- ALTER SEQUENCE public."StatusData_id_seq" RESTART;
-- ALTER SEQUENCE public."Trips_id_seq" RESTART;
-- ALTER SEQUENCE public."Users_id_seq" RESTART;
-- ALTER SEQUENCE public."Zones_id_seq" RESTART;
-- ALTER SEQUENCE public."ZoneTypes_id_seq" RESTART;

/* Check counts */
select 'Conditions' as "TableName", count(0) as "RecordCount" from public."Conditions"
union all
select 'ConfigFeedVersions', count(0) from public."ConfigFeedVersions"
union all
select 'DVIRDefectRemarks', count(0) from public."DVIRDefectRemarks"
union all
select 'DVIRDefects', count(0) from public."DVIRDefects"
union all
select 'DVIRDefectUpdates', count(0) from public."DVIRDefectUpdates"
union all
select 'DVIRLogs', count(0) from public."DVIRLogs"
union all
select 'Devices', count(0) from public."Devices"
union all
select 'Diagnostics', count(0) from public."Diagnostics"
union all
select 'DutyStatusAvailabilities', count(0) from public."DutyStatusAvailabilities"
union all
select 'ExceptionEvents', count(0) from public."ExceptionEvents"
union all
select 'FailedDVIRDefectUpdates', count(0) from public."FailedDVIRDefectUpdates"
union all
select 'FaultData', count(0) from public."FaultData"
union all
select 'LogRecords', count(0) from public."LogRecords"
union all
select 'MyGeotabVersionInfo', count(0) from public."MyGeotabVersionInfo"
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