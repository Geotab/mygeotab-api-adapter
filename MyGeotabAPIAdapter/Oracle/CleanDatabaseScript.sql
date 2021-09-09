/* DROP TABLE */
/* 
DROP TABLE GeotabAdapter_Client."Conditions";
DROP TABLE GeotabAdapter_Client."ConfigFeedVersions";
DROP TABLE GeotabAdapter_Client."Devices";
DROP TABLE GeotabAdapter_Client."Diagnostics";
DROP TABLE GeotabAdapter_Client."DriverChanges";
DROP TABLE GeotabAdapter_Client."DutyStatusAvailabilities";
DROP TABLE GeotabAdapter_Client."DVIRDefectRemarks";
DROP TABLE GeotabAdapter_Client."DVIRDefects";
DROP TABLE GeotabAdapter_Client."DVIRDefectUpdates";
DROP TABLE GeotabAdapter_Client."DVIRLogs";
DROP TABLE GeotabAdapter_Client."ExceptionEvents";
DROP TABLE GeotabAdapter_Client."FailedDVIRDefectUpdates";
DROP TABLE GeotabAdapter_Client."FailedOVDSServerCommands";
DROP TABLE GeotabAdapter_Client."FaultData";
DROP TABLE GeotabAdapter_Client."LogRecords";
DROP TABLE GeotabAdapter_Client."MyGeotabVersionInfo";
DROP TABLE GeotabAdapter_Client."OVDSServerCommands";
DROP TABLE GeotabAdapter_Client."Rules";
DROP TABLE GeotabAdapter_Client."StatusData";
DROP TABLE GeotabAdapter_Client."Trips";
DROP TABLE GeotabAdapter_Client."Users";
DROP TABLE GeotabAdapter_Client."Zones";
DROP TABLE GeotabAdapter_Client."ZoneTypes";
DROP VIEW GeotabAdapter_Client."vwRuleObject";
*/


/* Clean Database */ 
/* 
DELETE FROM GeotabAdapter_Client."Conditions";
DELETE FROM GeotabAdapter_Client."ConfigFeedVersions";
DELETE FROM GeotabAdapter_Client."Devices";
DELETE FROM GeotabAdapter_Client."Diagnostics";
DELETE FROM GeotabAdapter_Client."DriverChanges";
DELETE FROM GeotabAdapter_Client."DutyStatusAvailabilities";
DELETE FROM GeotabAdapter_Client."DVIRDefectRemarks";
DELETE FROM GeotabAdapter_Client."DVIRDefects";
DELETE FROM GeotabAdapter_Client."DVIRDefectUpdates";
DELETE FROM GeotabAdapter_Client."DVIRLogs";
DELETE FROM GeotabAdapter_Client."ExceptionEvents";
DELETE FROM GeotabAdapter_Client."FailedDVIRDefectUpdates";
DELETE FROM GeotabAdapter_Client."FailedOVDSServerCommands";
DELETE FROM GeotabAdapter_Client."FaultData";
DELETE FROM GeotabAdapter_Client."LogRecords";
DELETE FROM GeotabAdapter_Client."MyGeotabVersionInfo";
DELETE FROM GeotabAdapter_Client."OVDSServerCommands";
DELETE FROM GeotabAdapter_Client."Rules";
DELETE FROM GeotabAdapter_Client."StatusData";
DELETE FROM GeotabAdapter_Client."Trips";
DELETE FROM GeotabAdapter_Client."Users";
DELETE FROM GeotabAdapter_Client."Zones";
DELETE FROM GeotabAdapter_Client."ZoneTypes";
ALTER TABLE GeotabAdapter_Client."Conditions" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."ConfigFeedVersions" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."Devices" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."Diagnostics" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."DriverChanges" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."DutyStatusAvailabilities" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."DVIRDefectRemarks" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."DVIRDefects" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."DVIRDefectUpdates" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."DVIRLogs" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."ExceptionEvents" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."FailedDVIRDefectUpdates" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."FailedOVDSServerCommands" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."FaultData" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."LogRecords" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."MyGeotabVersionInfo" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."OVDSServerCommands" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."Rules" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."StatusData" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."Trips" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."Users" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."Zones" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
ALTER TABLE GeotabAdapter_Client."ZoneTypes" MODIFY("id" GENERATED AS IDENTITY (START WITH 1));
*/

/* Check counts */
EXEC DBMS_STATS.GATHER_SCHEMA_STATS(OWNNAME => 'GeotabAdapter_Client');
SELECT TABLE_NAME, NUM_ROWS, LAST_ANALYZED FROM ALL_TABLES WHERE OWNER = 'GEOTABADAPTER_CLIENT' ORDER BY TABLE_NAME;


