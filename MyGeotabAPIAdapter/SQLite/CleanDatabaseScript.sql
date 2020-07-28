/* Clean Database */ 
--delete from "Conditions";
--delete from "ConfigFeedVersions";
--delete from "DVIRDefectRemarks";
--delete from "DVIRDefects";
--delete from "DVIRLogs";
--delete from "Devices";
--delete from "Diagnostics";
--delete from "ExceptionEvents";
--delete from "FaultData";
--delete from "LogRecords";
--delete from "MyGeotabVersionInfo";
--delete from "Rules";
--delete from "StatusData";
--delete from "Trips";
--delete from "Users";
--delete from "Zones";

/* Check counts */
select 'Conditions' as "TableName", count(0) as "RecordCount" from "Conditions"
union all
select 'ConfigFeedVersions', count(0) from "ConfigFeedVersions"
union all
select 'DVIRDefectRemarks', count(0) from "DVIRDefectRemarks"
union all
select 'DVIRDefects', count(0) from "DVIRDefects"
union all
select 'DVIRLogs', count(0) from "DVIRLogs"
union all
select 'Devices', count(0) from "Devices"
union all
select 'Diagnostics', count(0) from "Diagnostics"
union all
select 'ExceptionEvents', count(0) from "ExceptionEvents"
union all
select 'FaultData', count(0) from "FaultData"
union all
select 'LogRecords', count(0) from "LogRecords"
union all
select 'MyGeotabVersionInfo', count(0) from "MyGeotabVersionInfo"
union all
select 'Rules', count(0) from "Rules"
union all
select 'StatusData', count(0) from "StatusData"
union all
select 'Trips', count(0) from "Trips"
union all
select 'Users', count(0) from "Users"
union all
select 'Zones', count(0) from "Zones"
order by "TableName";