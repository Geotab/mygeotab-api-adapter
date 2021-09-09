--------------------------------------------------------
--  File created - Wednesday-September-08-2021   
--------------------------------------------------------
--------------------------------------------------------
--  DDL for Table Conditions
--------------------------------------------------------

  CREATE TABLE "Conditions" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"GeotabId" NVARCHAR2(50), 
	"ParentId" NVARCHAR2(50), 
	"RuleId" NVARCHAR2(50), 
	"ConditionType" NVARCHAR2(50), 
	"DeviceId" NVARCHAR2(50), 
	"DiagnosticId" NVARCHAR2(100), 
	"DriverId" NVARCHAR2(50), 
	"Value" FLOAT(53), 
	"WorkTimeId" NVARCHAR2(50), 
	"ZoneId" NVARCHAR2(50), 
	"EntityStatus" NUMBER(10,0), 
	"RecordLastChangedUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table ConfigFeedVersions
--------------------------------------------------------

  CREATE TABLE "ConfigFeedVersions" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"FeedTypeId" NVARCHAR2(50), 
	"LastProcessedFeedVersion" NUMBER(20,0), 
	"RecordLastChangedUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table DVIRDefectRemarks
--------------------------------------------------------

  CREATE TABLE "DVIRDefectRemarks" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"GeotabId" NVARCHAR2(50), 
	"DVIRDefectId" NVARCHAR2(50), 
	"DateTime" TIMESTAMP (7), 
	"Remark" NCLOB, 
	"RemarkUserId" NVARCHAR2(50), 
	"EntityStatus" NUMBER(10,0), 
	"RecordLastChangedUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table DVIRDefectUpdates
--------------------------------------------------------

  CREATE TABLE "DVIRDefectUpdates" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"DVIRLogId" NVARCHAR2(50), 
	"DVIRDefectId" NVARCHAR2(50), 
	"RepairDateTime" TIMESTAMP (7), 
	"RepairStatus" NVARCHAR2(50), 
	"RepairUserId" NVARCHAR2(50), 
	"Remark" NCLOB, 
	"RemarkDateTime" TIMESTAMP (7), 
	"RemarkUserId" NVARCHAR2(50), 
	"RecordCreationTimeUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table DVIRDefects
--------------------------------------------------------

  CREATE TABLE "DVIRDefects" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"GeotabId" NVARCHAR2(50), 
	"DVIRLogId" NVARCHAR2(50), 
	"DefectListAssetType" NVARCHAR2(50), 
	"DefectListId" NVARCHAR2(50), 
	"DefectListName" NVARCHAR2(255), 
	"PartId" NVARCHAR2(50), 
	"PartName" NVARCHAR2(255), 
	"DefectId" NVARCHAR2(50), 
	"DefectName" NVARCHAR2(255), 
	"DefectSeverity" NVARCHAR2(50), 
	"RepairDateTime" TIMESTAMP (7), 
	"RepairStatus" NVARCHAR2(50), 
	"RepairUserId" NVARCHAR2(50), 
	"EntityStatus" NUMBER(10,0), 
	"RecordLastChangedUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table DVIRLogs
--------------------------------------------------------

  CREATE TABLE "DVIRLogs" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"GeotabId" NVARCHAR2(50), 
	"CertifiedByUserId" NVARCHAR2(50), 
	"CertifiedDate" TIMESTAMP (7), 
	"CertifyRemark" NCLOB, 
	"DateTime" TIMESTAMP (7), 
	"DeviceId" NVARCHAR2(50), 
	"DriverId" NVARCHAR2(50), 
	"DriverRemark" NCLOB, 
	"IsSafeToOperate" NUMBER(3,0), 
	"LocationLatitude" FLOAT(53), 
	"LocationLongitude" FLOAT(53), 
	"LogType" NVARCHAR2(50), 
	"RepairDate" TIMESTAMP (7), 
	"RepairedByUserId" NVARCHAR2(50), 
	"TrailerId" NVARCHAR2(50), 
	"TrailerName" NVARCHAR2(255), 
	"Version" NUMBER(20,0), 
	"RecordCreationTimeUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table Devices
--------------------------------------------------------

  CREATE TABLE "Devices" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"GeotabId" NVARCHAR2(50), 
	"ActiveFrom" TIMESTAMP (7), 
	"ActiveTo" TIMESTAMP (7), 
	"DeviceType" NVARCHAR2(50), 
	"LicensePlate" NVARCHAR2(50), 
	"LicenseState" NVARCHAR2(50), 
	"Name" NVARCHAR2(100), 
	"ProductId" NUMBER(10,0), 
	"SerialNumber" NVARCHAR2(12), 
	"VIN" NVARCHAR2(50), 
	"EntityStatus" NUMBER(10,0), 
	"RecordLastChangedUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table Diagnostics
--------------------------------------------------------

  CREATE TABLE "Diagnostics" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"GeotabId" NVARCHAR2(100), 
	"ControllerId" NVARCHAR2(100), 
	"DiagnosticCode" NUMBER(10,0), 
	"DiagnosticName" NVARCHAR2(255), 
	"DiagnosticSourceId" NVARCHAR2(50), 
	"DiagnosticSourceName" NVARCHAR2(255), 
	"DiagnosticUnitOfMeasureId" NVARCHAR2(50), 
	"DiagnosticUnitOfMeasureName" NVARCHAR2(255), 
	"OBD2DTC" NVARCHAR2(50), 
	"EntityStatus" NUMBER(10,0), 
	"RecordLastChangedUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table DriverChanges
--------------------------------------------------------

  CREATE TABLE "DriverChanges" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"GeotabId" NVARCHAR2(50), 
	"DateTime" TIMESTAMP (7), 
	"DeviceId" NVARCHAR2(50), 
	"DriverId" NVARCHAR2(50), 
	"Type" NVARCHAR2(50), 
	"Version" NUMBER(20,0), 
	"RecordCreationTimeUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table DutyStatusAvailabilities
--------------------------------------------------------

  CREATE TABLE "DutyStatusAvailabilities" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"DriverId" NVARCHAR2(50), 
	"CycleAvailabilities" NCLOB, 
	"CycleTicks" NUMBER(20,0), 
	"CycleRestTicks" NUMBER(20,0), 
	"DrivingTicks" NUMBER(20,0), 
	"DutyTicks" NUMBER(20,0), 
	"DutySinceCycleRestTicks" NUMBER(20,0), 
	"Is16HourExemptionAvailable" NUMBER(3,0), 
	"IsAdverseDrivingExemptionAvailable" NUMBER(3,0), 
	"IsOffDutyDeferralExemptionAvailable" NUMBER(3,0), 
	"Recap" NCLOB, 
	"RestTicks" NUMBER(20,0), 
	"WorkdayTicks" NUMBER(20,0), 
	"RecordLastChangedUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table ExceptionEvents
--------------------------------------------------------

  CREATE TABLE "ExceptionEvents" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"GeotabId" NVARCHAR2(50), 
	"ActiveFrom" TIMESTAMP (7), 
	"ActiveTo" TIMESTAMP (7), 
	"DeviceId" NVARCHAR2(50), 
	"Distance" FLOAT(24), 
	"DriverId" NVARCHAR2(50), 
	"DurationTicks" NUMBER(20,0), 
	"RuleId" NVARCHAR2(50), 
	"Version" NUMBER(20,0), 
	"RecordCreationTimeUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table FailedDVIRDefectUpdates
--------------------------------------------------------

  CREATE TABLE "FailedDVIRDefectUpdates" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"DVIRDefectUpdateId" NUMBER(20,0), 
	"DVIRLogId" NVARCHAR2(50), 
	"DVIRDefectId" NVARCHAR2(50), 
	"RepairDateTime" TIMESTAMP (7), 
	"RepairStatus" NVARCHAR2(50), 
	"RepairUserId" NVARCHAR2(50), 
	"Remark" NCLOB, 
	"RemarkDateTime" TIMESTAMP (7), 
	"RemarkUserId" NVARCHAR2(50), 
	"FailureMessage" NCLOB, 
	"RecordCreationTimeUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table FailedOVDSServerCommands
--------------------------------------------------------

  CREATE TABLE "FailedOVDSServerCommands" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"OVDSServerCommandId" NUMBER(20,0), 
	"Command" NCLOB, 
	"FailureMessage" NCLOB, 
	"RecordCreationTimeUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table FaultData
--------------------------------------------------------

  CREATE TABLE "FaultData" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"GeotabId" NVARCHAR2(50), 
	"AmberWarningLamp" NUMBER(3,0), 
	"ClassCode" NVARCHAR2(50), 
	"ControllerId" NVARCHAR2(100), 
	"ControllerName" NVARCHAR2(255), 
	"Count" NUMBER(10,0), 
	"DateTime" TIMESTAMP (7), 
	"DeviceId" NVARCHAR2(50), 
	"DiagnosticId" NVARCHAR2(100), 
	"DismissDateTime" TIMESTAMP (7), 
	"DismissUserId" NVARCHAR2(50), 
	"FailureModeCode" NUMBER(10,0), 
	"FailureModeId" NVARCHAR2(50), 
	"FailureModeName" NVARCHAR2(255), 
	"FaultLampState" NVARCHAR2(50), 
	"FaultState" NVARCHAR2(50), 
	"MalfunctionLamp" NUMBER(3,0), 
	"ProtectWarningLamp" NUMBER(3,0), 
	"RedStopLamp" NUMBER(3,0), 
	"Severity" NVARCHAR2(50), 
	"SourceAddress" NUMBER(10,0), 
	"RecordCreationTimeUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table LogRecords
--------------------------------------------------------

  CREATE TABLE "LogRecords" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"GeotabId" NVARCHAR2(50), 
	"DateTime" TIMESTAMP (7), 
	"DeviceId" NVARCHAR2(50), 
	"Latitude" FLOAT(53) DEFAULT 0, 
	"Longitude" FLOAT(53) DEFAULT 0, 
	"Speed" FLOAT(24) DEFAULT 0, 
	"RecordCreationTimeUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table MyGeotabVersionInfo
--------------------------------------------------------

  CREATE TABLE "MyGeotabVersionInfo" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"DatabaseName" NVARCHAR2(58), 
	"Server" NVARCHAR2(50), 
	"DatabaseVersion" NVARCHAR2(50), 
	"ApplicationBuild" NVARCHAR2(50), 
	"ApplicationBranch" NVARCHAR2(50), 
	"ApplicationCommit" NVARCHAR2(50), 
	"GoTalkVersion" NVARCHAR2(50), 
	"RecordCreationTimeUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table OVDSServerCommands
--------------------------------------------------------

  CREATE TABLE "OVDSServerCommands" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"Command" NCLOB, 
	"RecordCreationTimeUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table Rules
--------------------------------------------------------

  CREATE TABLE "Rules" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"GeotabId" NVARCHAR2(50), 
	"ActiveFrom" TIMESTAMP (7), 
	"ActiveTo" TIMESTAMP (7), 
	"BaseType" NVARCHAR2(50), 
	"Comment" NVARCHAR2(255), 
	"Name" NVARCHAR2(255), 
	"Version" NUMBER(20,0), 
	"EntityStatus" NUMBER(10,0), 
	"RecordLastChangedUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table StatusData
--------------------------------------------------------

  CREATE TABLE "StatusData" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"GeotabId" NVARCHAR2(50), 
	"Data" FLOAT(53), 
	"DateTime" TIMESTAMP (7), 
	"DeviceId" NVARCHAR2(50), 
	"DiagnosticId" NVARCHAR2(100), 
	"RecordCreationTimeUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table Trips
--------------------------------------------------------

  CREATE TABLE "Trips" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"GeotabId" NVARCHAR2(50), 
	"AfterHoursDistance" FLOAT(24), 
	"AfterHoursDrivingDurationTicks" NUMBER(20,0), 
	"AfterHoursEnd" NUMBER(3,0), 
	"AfterHoursStart" NUMBER(3,0), 
	"AfterHoursStopDurationTicks" NUMBER(20,0), 
	"AverageSpeed" FLOAT(24), 
	"DeviceId" NVARCHAR2(50), 
	"Distance" FLOAT(24), 
	"DriverId" NVARCHAR2(50), 
	"DrivingDurationTicks" NUMBER(20,0), 
	"IdlingDurationTicks" NUMBER(20,0), 
	"MaximumSpeed" FLOAT(24), 
	"NextTripStart" TIMESTAMP (7), 
	"SpeedRange1" NUMBER(10,0), 
	"SpeedRange1DurationTicks" NUMBER(20,0), 
	"SpeedRange2" NUMBER(10,0), 
	"SpeedRange2DurationTicks" NUMBER(20,0), 
	"SpeedRange3" NUMBER(10,0), 
	"SpeedRange3DurationTicks" NUMBER(20,0), 
	"Start" TIMESTAMP (7), 
	"Stop" TIMESTAMP (7), 
	"StopDurationTicks" NUMBER(20,0), 
	"StopPointX" FLOAT(53), 
	"StopPointY" FLOAT(53), 
	"WorkDistance" FLOAT(24), 
	"WorkDrivingDurationTicks" NUMBER(20,0), 
	"WorkStopDurationTicks" NUMBER(20,0), 
	"RecordCreationTimeUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table Users
--------------------------------------------------------

  CREATE TABLE "Users" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"GeotabId" NVARCHAR2(50), 
	"ActiveFrom" TIMESTAMP (7), 
	"ActiveTo" TIMESTAMP (7), 
	"EmployeeNo" NVARCHAR2(50), 
	"FirstName" NVARCHAR2(255), 
	"HosRuleSet" NCLOB, 
	"IsDriver" NUMBER(3,0), 
	"LastAccessDate" TIMESTAMP (7), 
	"LastName" NVARCHAR2(255), 
	"Name" NVARCHAR2(255), 
	"EntityStatus" NUMBER(10,0), 
	"RecordLastChangedUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table ZoneTypes
--------------------------------------------------------

  CREATE TABLE "ZoneTypes" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"GeotabId" NVARCHAR2(100), 
	"Comment" NVARCHAR2(255), 
	"Name" NVARCHAR2(255), 
	"EntityStatus" NUMBER(10,0), 
	"RecordLastChangedUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for Table Zones
--------------------------------------------------------

  CREATE TABLE "Zones" 
   (	"id" NUMBER(20,0) GENERATED ALWAYS AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE , 
	"GeotabId" NVARCHAR2(100), 
	"ActiveFrom" TIMESTAMP (7), 
	"ActiveTo" TIMESTAMP (7), 
	"CentroidLatitude" FLOAT(53), 
	"CentroidLongitude" FLOAT(53), 
	"Comment" NVARCHAR2(500), 
	"Displayed" NUMBER(3,0), 
	"ExternalReference" NVARCHAR2(255), 
	"MustIdentifyStops" NUMBER(3,0), 
	"Name" NVARCHAR2(255), 
	"Points" NCLOB, 
	"ZoneTypeIds" NCLOB, 
	"Version" NUMBER(20,0), 
	"EntityStatus" NUMBER(10,0), 
	"RecordLastChangedUtc" TIMESTAMP (7)
   )
--------------------------------------------------------
--  DDL for View vwRuleObject
--------------------------------------------------------

  CREATE OR REPLACE NONEDITIONABLE VIEW "vwRuleObject" ("RuleAdapterId", "GeotabId", "ActiveFrom", "ActiveTo", "BaseType", "Comment", "Name", "Version", "EntityStatus", "RecordLastChangedUtc", "ConditionAdapterId", "Cond_Id", "Cond_ParentId", "Cond_RuleId", "Cond_ConditionType", "Cond_DeviceId", "Cond_DiagnosticId", "Cond_Value", "Cond_WorkTimeId", "Cond_ZoneId", "Cond_EntityStatus", "Cond_RecordLastChangedUtc") AS 
  SELECT r."id", r."GeotabId", r."ActiveFrom", r."ActiveTo", r."BaseType", r."Comment", r."Name", r."Version", r."EntityStatus", r."RecordLastChangedUtc", 
c."id", c."GeotabId", c."ParentId", c."RuleId", c."ConditionType", c."DeviceId", c."DiagnosticId", c."Value", c."WorkTimeId", c."ZoneId", c."EntityStatus", c."RecordLastChangedUtc"
FROM GeotabAdapter_Client."Rules" r
INNER JOIN
GeotabAdapter_Client."Conditions" c
ON r."GeotabId" = c."RuleId"
--------------------------------------------------------
--  DDL for Index PK_CONDITIONS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_CONDITIONS" ON "Conditions" ("id")
--------------------------------------------------------
--  DDL for Index PK_CONFIGFEEDVERSIONS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_CONFIGFEEDVERSIONS" ON "ConfigFeedVersions" ("id")
--------------------------------------------------------
--  DDL for Index PK_DEVICES
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_DEVICES" ON "Devices" ("id")
--------------------------------------------------------
--  DDL for Index PK_DIAGNOSTICS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_DIAGNOSTICS" ON "Diagnostics" ("id")
--------------------------------------------------------
--  DDL for Index PK_DRIVERCHANGES
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_DRIVERCHANGES" ON "DriverChanges" ("id")
--------------------------------------------------------
--  DDL for Index PK_DUTYSTATUSAVAILABILITIES
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_DUTYSTATUSAVAILABILITIES" ON "DutyStatusAvailabilities" ("id")
--------------------------------------------------------
--  DDL for Index PK_DVIRDEFECTREMARKS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_DVIRDEFECTREMARKS" ON "DVIRDefectRemarks" ("id")
--------------------------------------------------------
--  DDL for Index PK_DVIRDEFECTS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_DVIRDEFECTS" ON "DVIRDefects" ("id")
--------------------------------------------------------
--  DDL for Index PK_DVIRDEFECTUPDATES
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_DVIRDEFECTUPDATES" ON "DVIRDefectUpdates" ("id")
--------------------------------------------------------
--  DDL for Index PK_DVIRLOGS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_DVIRLOGS" ON "DVIRLogs" ("id")
--------------------------------------------------------
--  DDL for Index PK_EXCEPTIONEVENTS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_EXCEPTIONEVENTS" ON "ExceptionEvents" ("id")
--------------------------------------------------------
--  DDL for Index PK_FAILEDDVIRDEFECTUPDATES
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_FAILEDDVIRDEFECTUPDATES" ON "FailedDVIRDefectUpdates" ("id")
--------------------------------------------------------
--  DDL for Index PK_FAILEDOVDSSERVERCOMMANDS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_FAILEDOVDSSERVERCOMMANDS" ON "FailedOVDSServerCommands" ("id")
--------------------------------------------------------
--  DDL for Index PK_FAULTDATA
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_FAULTDATA" ON "FaultData" ("id")
--------------------------------------------------------
--  DDL for Index PK_LOGRECORDS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_LOGRECORDS" ON "LogRecords" ("id")
--------------------------------------------------------
--  DDL for Index PK_MYGEOTABVERSIONINFO
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_MYGEOTABVERSIONINFO" ON "MyGeotabVersionInfo" ("id")
--------------------------------------------------------
--  DDL for Index PK_OVDSSERVERCOMMANDS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_OVDSSERVERCOMMANDS" ON "OVDSServerCommands" ("id")
--------------------------------------------------------
--  DDL for Index PK_RULES
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_RULES" ON "Rules" ("id")
--------------------------------------------------------
--  DDL for Index PK_STATUSDATA
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_STATUSDATA" ON "StatusData" ("id")
--------------------------------------------------------
--  DDL for Index PK_TRIPS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_TRIPS" ON "Trips" ("id")
--------------------------------------------------------
--  DDL for Index PK_USERS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_USERS" ON "Users" ("id")
--------------------------------------------------------
--  DDL for Index PK_ZONES
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_ZONES" ON "Zones" ("id")
--------------------------------------------------------
--  DDL for Index PK_ZONETYPES
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_ZONETYPES" ON "ZoneTypes" ("id")
--------------------------------------------------------
--  DDL for Index SYS_IL0000076254C00003$$
--------------------------------------------------------

  CREATE UNIQUE INDEX "SYS_IL0000076254C00003$$" ON "DutyStatusAvailabilities" (
--------------------------------------------------------
--  DDL for Index SYS_IL0000076254C00012$$
--------------------------------------------------------

  CREATE UNIQUE INDEX "SYS_IL0000076254C00012$$" ON "DutyStatusAvailabilities" (
--------------------------------------------------------
--  DDL for Index SYS_IL0000076261C00005$$
--------------------------------------------------------

  CREATE UNIQUE INDEX "SYS_IL0000076261C00005$$" ON "DVIRDefectRemarks" (
--------------------------------------------------------
--  DDL for Index SYS_IL0000076269C00007$$
--------------------------------------------------------

  CREATE UNIQUE INDEX "SYS_IL0000076269C00007$$" ON "DVIRDefectUpdates" (
--------------------------------------------------------
--  DDL for Index SYS_IL0000076274C00005$$
--------------------------------------------------------

  CREATE UNIQUE INDEX "SYS_IL0000076274C00005$$" ON "DVIRLogs" (
--------------------------------------------------------
--  DDL for Index SYS_IL0000076274C00009$$
--------------------------------------------------------

  CREATE UNIQUE INDEX "SYS_IL0000076274C00009$$" ON "DVIRLogs" (
--------------------------------------------------------
--  DDL for Index SYS_IL0000076284C00008$$
--------------------------------------------------------

  CREATE UNIQUE INDEX "SYS_IL0000076284C00008$$" ON "FailedDVIRDefectUpdates" (
--------------------------------------------------------
--  DDL for Index SYS_IL0000076284C00011$$
--------------------------------------------------------

  CREATE UNIQUE INDEX "SYS_IL0000076284C00011$$" ON "FailedDVIRDefectUpdates" (
--------------------------------------------------------
--  DDL for Index SYS_IL0000076306C00007$$
--------------------------------------------------------

  CREATE UNIQUE INDEX "SYS_IL0000076306C00007$$" ON "Users" (
--------------------------------------------------------
--  DDL for Index SYS_IL0000076311C00012$$
--------------------------------------------------------

  CREATE UNIQUE INDEX "SYS_IL0000076311C00012$$" ON "Zones" (
--------------------------------------------------------
--  DDL for Index SYS_IL0000076311C00013$$
--------------------------------------------------------

  CREATE UNIQUE INDEX "SYS_IL0000076311C00013$$" ON "Zones" (
--------------------------------------------------------
--  DDL for Index SYS_IL0000076470C00003$$
--------------------------------------------------------

  CREATE UNIQUE INDEX "SYS_IL0000076470C00003$$" ON "FailedOVDSServerCommands" (
--------------------------------------------------------
--  DDL for Index SYS_IL0000076470C00004$$
--------------------------------------------------------

  CREATE UNIQUE INDEX "SYS_IL0000076470C00004$$" ON "FailedOVDSServerCommands" (
--------------------------------------------------------
--  DDL for Index SYS_IL0000076477C00002$$
--------------------------------------------------------

  CREATE UNIQUE INDEX "SYS_IL0000076477C00002$$" ON "OVDSServerCommands" (
--------------------------------------------------------
--  DDL for Index PK_CONDITIONS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_CONDITIONS" ON "Conditions" ("id")
--------------------------------------------------------
--  DDL for Index PK_CONFIGFEEDVERSIONS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_CONFIGFEEDVERSIONS" ON "ConfigFeedVersions" ("id")
--------------------------------------------------------
--  DDL for Index PK_DVIRDEFECTREMARKS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_DVIRDEFECTREMARKS" ON "DVIRDefectRemarks" ("id")
--------------------------------------------------------
--  DDL for Index PK_DVIRDEFECTUPDATES
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_DVIRDEFECTUPDATES" ON "DVIRDefectUpdates" ("id")
--------------------------------------------------------
--  DDL for Index PK_DVIRDEFECTS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_DVIRDEFECTS" ON "DVIRDefects" ("id")
--------------------------------------------------------
--  DDL for Index PK_DVIRLOGS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_DVIRLOGS" ON "DVIRLogs" ("id")
--------------------------------------------------------
--  DDL for Index PK_DEVICES
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_DEVICES" ON "Devices" ("id")
--------------------------------------------------------
--  DDL for Index PK_DIAGNOSTICS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_DIAGNOSTICS" ON "Diagnostics" ("id")
--------------------------------------------------------
--  DDL for Index PK_DRIVERCHANGES
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_DRIVERCHANGES" ON "DriverChanges" ("id")
--------------------------------------------------------
--  DDL for Index PK_DUTYSTATUSAVAILABILITIES
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_DUTYSTATUSAVAILABILITIES" ON "DutyStatusAvailabilities" ("id")
--------------------------------------------------------
--  DDL for Index PK_EXCEPTIONEVENTS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_EXCEPTIONEVENTS" ON "ExceptionEvents" ("id")
--------------------------------------------------------
--  DDL for Index PK_FAILEDDVIRDEFECTUPDATES
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_FAILEDDVIRDEFECTUPDATES" ON "FailedDVIRDefectUpdates" ("id")
--------------------------------------------------------
--  DDL for Index PK_FAILEDOVDSSERVERCOMMANDS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_FAILEDOVDSSERVERCOMMANDS" ON "FailedOVDSServerCommands" ("id")
--------------------------------------------------------
--  DDL for Index PK_FAULTDATA
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_FAULTDATA" ON "FaultData" ("id")
--------------------------------------------------------
--  DDL for Index PK_LOGRECORDS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_LOGRECORDS" ON "LogRecords" ("id")
--------------------------------------------------------
--  DDL for Index PK_MYGEOTABVERSIONINFO
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_MYGEOTABVERSIONINFO" ON "MyGeotabVersionInfo" ("id")
--------------------------------------------------------
--  DDL for Index PK_OVDSSERVERCOMMANDS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_OVDSSERVERCOMMANDS" ON "OVDSServerCommands" ("id")
--------------------------------------------------------
--  DDL for Index PK_RULES
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_RULES" ON "Rules" ("id")
--------------------------------------------------------
--  DDL for Index PK_STATUSDATA
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_STATUSDATA" ON "StatusData" ("id")
--------------------------------------------------------
--  DDL for Index PK_TRIPS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_TRIPS" ON "Trips" ("id")
--------------------------------------------------------
--  DDL for Index PK_USERS
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_USERS" ON "Users" ("id")
--------------------------------------------------------
--  DDL for Index PK_ZONETYPES
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_ZONETYPES" ON "ZoneTypes" ("id")
--------------------------------------------------------
--  DDL for Index PK_ZONES
--------------------------------------------------------

  CREATE UNIQUE INDEX "PK_ZONES" ON "Zones" ("id")
--------------------------------------------------------
--  DDL for Function LOGMNR$COL_GG_TABF_PUBLIC
--------------------------------------------------------

  CREATE OR REPLACE NONEDITIONABLE FUNCTION "LOGMNR$COL_GG_TABF_PUBLIC" wrapped
a000000
1
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
8
237 185
nkvJV1w6wH1y7mRApzf9mGuebNUwgxDILkhGfHQCmP8+Vi4fyqh3SG1Fyq+pCts1OlgnK761
YuzKBA4JE5DNwZzBIF/Y4ZM5eUlquVyTkOg+AodK3vQJt9NLvPITXbP42O37gO+zKr4BQEJk
ypwrP1U/Pf6MLZONN8LUaVqHCN87T14HqHs5taX7LhLXQ2lCVBE1Ll8dyB9CDOlbyvQS/lrb
+0n1pQi9IJAWySL85ChAqnTaqFJm0YeToD4lZ8UUPQqIZNoX0x73WK9OzsmdBrvEC97iduxe
PEXVkxF6xklPod6yOGBvW7DAFMBgf+LajDLVKOAwB2EAiKCXYMuTUTtMYYkCFFf4sj1rCpsj
TLth6TSru530aM2HP6bEbm3m
--------------------------------------------------------
--  DDL for Function LOGMNR$GSBA_GG_TABF_PUBLIC
--------------------------------------------------------

  CREATE OR REPLACE NONEDITIONABLE FUNCTION "LOGMNR$GSBA_GG_TABF_PUBLIC" wrapped
a000000
1
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
8
206 171
6gxi/CQwK1I5Rfwuw/SXrOGpVRYwgwLI1yfbfHRGEjNe54OE4QwRZCoA20oG536tzgcBrj+1
xE3tE8jIhAoTlUdUmkdYMmZycO1SdiJZwwt/6BrM1wHXl/E5+3Ip2NXzC9j8v4+KjkD9d5AT
p05eEsEWjU1CBTMSpjZZrXzbgFl9QNnQ+zJGjSug21f76ajs78m6anxz7vFcTcem6XpAgKjc
EXzd/ijP8qiOqwblTfnXcRslJn3MljD02u+5fh9NBctOmnaw/tOjRCFPUhY8I9gCoMptjG7U
rHEIFzHOFyxBEdulRGq4ngSgcm7l2yOdSHgNM8rO2vUH4gozvJoLE1S8GBBzG/wrvHPhACQ/
2w==
--------------------------------------------------------
--  DDL for Function LOGMNR$KEY_GG_TABF_PUBLIC
--------------------------------------------------------

  CREATE OR REPLACE NONEDITIONABLE FUNCTION "LOGMNR$KEY_GG_TABF_PUBLIC" wrapped
a000000
1
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
8
2a0 1a1
3O4I5hDO715d8A2tqbxMCflFW0owg/D319xqfC9Grfg+K6yE71zMvtNS45AyRXBk77WpI5v4
nUSfE2lbvUYgk3JHySIe28XxC3xIYYpPGQjxwa3GzPw0FN5aN6kerQQTHUBp29Dd+vLSgBaC
2pAFrq059ZvN0ZPN11XG/2RuDY7HaTQu/QffhnY8rVlNxpFmbkVidwtZQahx5qIFu9Uww/tv
o1AvhjaORi898/KiPtOqv7LpsPFbyNuMnZEG48cxtZuesMBJFP/bKtgU2DN69xiT8Pxf+N2n
g0D2ximYzZqwY/4dBQj9dyQDuXRFo40hdqtWw0L96zV6723aQ8Xp0cqBaZj2wWTI4+6Ikry9
zY0Mdm3bV8TYqsOa+zT4fnikGO0eYbTFHEiW9QUbl/UwzuERwk8p
--------------------------------------------------------
--  DDL for Function LOGMNR$SEQ_GG_TABF_PUBLIC
--------------------------------------------------------

  CREATE OR REPLACE NONEDITIONABLE FUNCTION "LOGMNR$SEQ_GG_TABF_PUBLIC" wrapped
a000000
1
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
8
249 181
++dDv/cHZatK7/vHG9lvR8DQCpYwg/D3AEhqfHQC2h6ONoOvOeHvTNX1S5GDyajM4j8vkSVz
IMw+LbYS3goujprvmrB/LUpdBF8TVvjEqZpC7MCKPXWcGnTeL7ja8C2tcOdjOpRXkwL5NmPJ
B0KqMvwepdiQDY7HUDQrBddQC1lBqHGEogWkwRJ3+2+jUC+Gpo5GTazIWS0V551NkSl3+h0W
BhkPglLbvQDPzxWTnmu4ZuJIlTiNwTf1R0WxghyyKFjES9CJsCrGT8Fn7prlF4Mr5kx1YBGf
5xaODtRnmVJgb65RlKbAN9+Xxf2QnQjKQL99RZAgsEwGVKNfx9lFKwHLGUwzjOxmIAXXYd/Z
+L9osPQJjZYkNrD0pQ==
--------------------------------------------------------
--  DDL for Function LOGMNR$TAB_GG_TABF_PUBLIC
--------------------------------------------------------

  CREATE OR REPLACE NONEDITIONABLE FUNCTION "LOGMNR$TAB_GG_TABF_PUBLIC" wrapped
a000000
1
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
8
22e 181
PRVt0FiLRPgjIhZlCZcQhnOHXQ4wgzJp2UhGfHSKrQ843hKm3gKD9swoFAS4jVcW0CsSl7W1
6banVWdjktOVM18XyRjY4ZM5eRdquSp0ZdfHl3KJPBYqPi9LXIwum30Qh7ymgO+zTKj+R1N2
nSs/TPnknfYLwcUdAfBryDQGEIMisMuE9XT5ix3sudhHa5tLJRjsBIDKlqL7zk2CH0C1NRAZ
XrP7WgavVdNS3Yikz88VupZG21hTuAGspJBgCagmNWIwi9pgCIWP3rxF4p+uMps/ABEg+MBP
6Iykm62kO6hWhVHJXkfKF/jrQFjYBTTzatr1VTcOXt/AFuagDR7isNtb//lnXh8TXyAFWyCT
ubv6GXL0aM0PP/t+DfSl
--------------------------------------------------------
--  DDL for Function LOGMNR$USER_GG_TABF_PUBLIC
--------------------------------------------------------

  CREATE OR REPLACE NONEDITIONABLE FUNCTION "LOGMNR$USER_GG_TABF_PUBLIC" wrapped
a000000
1
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
abcd
8
272 191
Vc+i+Mga8m7/BSlvXl0J+GmParYwg/D319wCfC8CTE6Ot30G8QRR0WzR8ohw8Z/y6DuL4pL5
IDjZApnCaZgeKa1OTjLj2B2tOYZg2ZuOAPKrxo7DeBqtn0Ahw0tubS36jP4xc+d2eQebdJMD
c/U0JxdiAl0qIPzsltBjeoGAEsxUk0aubCMJmysgc2d8ojil6ixQ37D7RA0HWMkh27QdOuXF
vSwuufDunMMT8Hue9dvy4vRXj+PhuyylJSukStsxyIb234EahCXyrjDlnzbC91eoU7v5sb4D
OvtDmggQCEViyhFXwspP9P0dOObin8JENsDJeFZYr/oVAgSHa97LRKvNZgd+f//XHlpEAOP4
rNm5mF0wTCABbb7tc5c7uo09M+79i7en8g==
--------------------------------------------------------
--  Constraints for Table Conditions
--------------------------------------------------------

  ALTER TABLE "Conditions" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "Conditions" MODIFY ("GeotabId" NOT NULL ENABLE)
  ALTER TABLE "Conditions" MODIFY ("ConditionType" NOT NULL ENABLE)
  ALTER TABLE "Conditions" MODIFY ("EntityStatus" NOT NULL ENABLE)
  ALTER TABLE "Conditions" MODIFY ("RecordLastChangedUtc" NOT NULL ENABLE)
  ALTER TABLE "Conditions" ADD CONSTRAINT "PK_CONDITIONS" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table ConfigFeedVersions
--------------------------------------------------------

  ALTER TABLE "ConfigFeedVersions" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "ConfigFeedVersions" MODIFY ("FeedTypeId" NOT NULL ENABLE)
  ALTER TABLE "ConfigFeedVersions" MODIFY ("LastProcessedFeedVersion" NOT NULL ENABLE)
  ALTER TABLE "ConfigFeedVersions" MODIFY ("RecordLastChangedUtc" NOT NULL ENABLE)
  ALTER TABLE "ConfigFeedVersions" ADD CONSTRAINT "PK_CONFIGFEEDVERSIONS" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table DVIRDefectRemarks
--------------------------------------------------------

  ALTER TABLE "DVIRDefectRemarks" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "DVIRDefectRemarks" MODIFY ("GeotabId" NOT NULL ENABLE)
  ALTER TABLE "DVIRDefectRemarks" MODIFY ("DVIRDefectId" NOT NULL ENABLE)
  ALTER TABLE "DVIRDefectRemarks" MODIFY ("DateTime" NOT NULL ENABLE)
  ALTER TABLE "DVIRDefectRemarks" MODIFY ("Remark" NOT NULL ENABLE)
  ALTER TABLE "DVIRDefectRemarks" MODIFY ("EntityStatus" NOT NULL ENABLE)
  ALTER TABLE "DVIRDefectRemarks" MODIFY ("RecordLastChangedUtc" NOT NULL ENABLE)
  ALTER TABLE "DVIRDefectRemarks" ADD CONSTRAINT "PK_DVIRDEFECTREMARKS" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table DVIRDefectUpdates
--------------------------------------------------------

  ALTER TABLE "DVIRDefectUpdates" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "DVIRDefectUpdates" MODIFY ("DVIRLogId" NOT NULL ENABLE)
  ALTER TABLE "DVIRDefectUpdates" MODIFY ("DVIRDefectId" NOT NULL ENABLE)
  ALTER TABLE "DVIRDefectUpdates" MODIFY ("RecordCreationTimeUtc" NOT NULL ENABLE)
  ALTER TABLE "DVIRDefectUpdates" ADD CONSTRAINT "PK_DVIRDEFECTUPDATES" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table DVIRDefects
--------------------------------------------------------

  ALTER TABLE "DVIRDefects" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "DVIRDefects" MODIFY ("GeotabId" NOT NULL ENABLE)
  ALTER TABLE "DVIRDefects" MODIFY ("DVIRLogId" NOT NULL ENABLE)
  ALTER TABLE "DVIRDefects" MODIFY ("EntityStatus" NOT NULL ENABLE)
  ALTER TABLE "DVIRDefects" MODIFY ("RecordLastChangedUtc" NOT NULL ENABLE)
  ALTER TABLE "DVIRDefects" ADD CONSTRAINT "PK_DVIRDEFECTS" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table DVIRLogs
--------------------------------------------------------

  ALTER TABLE "DVIRLogs" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "DVIRLogs" MODIFY ("GeotabId" NOT NULL ENABLE)
  ALTER TABLE "DVIRLogs" MODIFY ("DateTime" NOT NULL ENABLE)
  ALTER TABLE "DVIRLogs" MODIFY ("Version" NOT NULL ENABLE)
  ALTER TABLE "DVIRLogs" MODIFY ("RecordCreationTimeUtc" NOT NULL ENABLE)
  ALTER TABLE "DVIRLogs" ADD CONSTRAINT "PK_DVIRLOGS" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table Devices
--------------------------------------------------------

  ALTER TABLE "Devices" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "Devices" MODIFY ("GeotabId" NOT NULL ENABLE)
  ALTER TABLE "Devices" MODIFY ("DeviceType" NOT NULL ENABLE)
  ALTER TABLE "Devices" MODIFY ("Name" NOT NULL ENABLE)
  ALTER TABLE "Devices" MODIFY ("EntityStatus" NOT NULL ENABLE)
  ALTER TABLE "Devices" MODIFY ("RecordLastChangedUtc" NOT NULL ENABLE)
  ALTER TABLE "Devices" ADD CONSTRAINT "PK_DEVICES" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table Diagnostics
--------------------------------------------------------

  ALTER TABLE "Diagnostics" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "Diagnostics" MODIFY ("GeotabId" NOT NULL ENABLE)
  ALTER TABLE "Diagnostics" MODIFY ("DiagnosticName" NOT NULL ENABLE)
  ALTER TABLE "Diagnostics" MODIFY ("DiagnosticSourceId" NOT NULL ENABLE)
  ALTER TABLE "Diagnostics" MODIFY ("DiagnosticSourceName" NOT NULL ENABLE)
  ALTER TABLE "Diagnostics" MODIFY ("DiagnosticUnitOfMeasureId" NOT NULL ENABLE)
  ALTER TABLE "Diagnostics" MODIFY ("DiagnosticUnitOfMeasureName" NOT NULL ENABLE)
  ALTER TABLE "Diagnostics" MODIFY ("EntityStatus" NOT NULL ENABLE)
  ALTER TABLE "Diagnostics" MODIFY ("RecordLastChangedUtc" NOT NULL ENABLE)
  ALTER TABLE "Diagnostics" ADD CONSTRAINT "PK_DIAGNOSTICS" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table DriverChanges
--------------------------------------------------------

  ALTER TABLE "DriverChanges" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "DriverChanges" MODIFY ("GeotabId" NOT NULL ENABLE)
  ALTER TABLE "DriverChanges" MODIFY ("DeviceId" NOT NULL ENABLE)
  ALTER TABLE "DriverChanges" MODIFY ("DriverId" NOT NULL ENABLE)
  ALTER TABLE "DriverChanges" MODIFY ("Type" NOT NULL ENABLE)
  ALTER TABLE "DriverChanges" MODIFY ("Version" NOT NULL ENABLE)
  ALTER TABLE "DriverChanges" MODIFY ("RecordCreationTimeUtc" NOT NULL ENABLE)
  ALTER TABLE "DriverChanges" ADD CONSTRAINT "PK_DRIVERCHANGES" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table DutyStatusAvailabilities
--------------------------------------------------------

  ALTER TABLE "DutyStatusAvailabilities" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "DutyStatusAvailabilities" MODIFY ("DriverId" NOT NULL ENABLE)
  ALTER TABLE "DutyStatusAvailabilities" MODIFY ("RecordLastChangedUtc" NOT NULL ENABLE)
  ALTER TABLE "DutyStatusAvailabilities" ADD CONSTRAINT "PK_DUTYSTATUSAVAILABILITIES" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table ExceptionEvents
--------------------------------------------------------

  ALTER TABLE "ExceptionEvents" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "ExceptionEvents" MODIFY ("GeotabId" NOT NULL ENABLE)
  ALTER TABLE "ExceptionEvents" MODIFY ("RecordCreationTimeUtc" NOT NULL ENABLE)
  ALTER TABLE "ExceptionEvents" ADD CONSTRAINT "PK_EXCEPTIONEVENTS" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table FailedDVIRDefectUpdates
--------------------------------------------------------

  ALTER TABLE "FailedDVIRDefectUpdates" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "FailedDVIRDefectUpdates" MODIFY ("DVIRDefectUpdateId" NOT NULL ENABLE)
  ALTER TABLE "FailedDVIRDefectUpdates" MODIFY ("DVIRLogId" NOT NULL ENABLE)
  ALTER TABLE "FailedDVIRDefectUpdates" MODIFY ("DVIRDefectId" NOT NULL ENABLE)
  ALTER TABLE "FailedDVIRDefectUpdates" MODIFY ("RecordCreationTimeUtc" NOT NULL ENABLE)
  ALTER TABLE "FailedDVIRDefectUpdates" ADD CONSTRAINT "PK_FAILEDDVIRDEFECTUPDATES" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table FailedOVDSServerCommands
--------------------------------------------------------

  ALTER TABLE "FailedOVDSServerCommands" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "FailedOVDSServerCommands" MODIFY ("OVDSServerCommandId" NOT NULL ENABLE)
  ALTER TABLE "FailedOVDSServerCommands" MODIFY ("Command" NOT NULL ENABLE)
  ALTER TABLE "FailedOVDSServerCommands" MODIFY ("FailureMessage" NOT NULL ENABLE)
  ALTER TABLE "FailedOVDSServerCommands" MODIFY ("RecordCreationTimeUtc" NOT NULL ENABLE)
  ALTER TABLE "FailedOVDSServerCommands" ADD CONSTRAINT "PK_FAILEDOVDSSERVERCOMMANDS" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table FaultData
--------------------------------------------------------

  ALTER TABLE "FaultData" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "FaultData" MODIFY ("GeotabId" NOT NULL ENABLE)
  ALTER TABLE "FaultData" MODIFY ("ControllerId" NOT NULL ENABLE)
  ALTER TABLE "FaultData" MODIFY ("Count" NOT NULL ENABLE)
  ALTER TABLE "FaultData" MODIFY ("DeviceId" NOT NULL ENABLE)
  ALTER TABLE "FaultData" MODIFY ("DiagnosticId" NOT NULL ENABLE)
  ALTER TABLE "FaultData" MODIFY ("FailureModeId" NOT NULL ENABLE)
  ALTER TABLE "FaultData" MODIFY ("RecordCreationTimeUtc" NOT NULL ENABLE)
  ALTER TABLE "FaultData" ADD CONSTRAINT "PK_FAULTDATA" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table LogRecords
--------------------------------------------------------

  ALTER TABLE "LogRecords" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "LogRecords" MODIFY ("GeotabId" NOT NULL ENABLE)
  ALTER TABLE "LogRecords" MODIFY ("DateTime" NOT NULL ENABLE)
  ALTER TABLE "LogRecords" MODIFY ("DeviceId" NOT NULL ENABLE)
  ALTER TABLE "LogRecords" MODIFY ("Latitude" NOT NULL ENABLE)
  ALTER TABLE "LogRecords" MODIFY ("Longitude" NOT NULL ENABLE)
  ALTER TABLE "LogRecords" MODIFY ("Speed" NOT NULL ENABLE)
  ALTER TABLE "LogRecords" MODIFY ("RecordCreationTimeUtc" NOT NULL ENABLE)
  ALTER TABLE "LogRecords" ADD CONSTRAINT "PK_LOGRECORDS" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table MyGeotabVersionInfo
--------------------------------------------------------

  ALTER TABLE "MyGeotabVersionInfo" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "MyGeotabVersionInfo" MODIFY ("DatabaseName" NOT NULL ENABLE)
  ALTER TABLE "MyGeotabVersionInfo" MODIFY ("Server" NOT NULL ENABLE)
  ALTER TABLE "MyGeotabVersionInfo" MODIFY ("DatabaseVersion" NOT NULL ENABLE)
  ALTER TABLE "MyGeotabVersionInfo" MODIFY ("ApplicationBuild" NOT NULL ENABLE)
  ALTER TABLE "MyGeotabVersionInfo" MODIFY ("ApplicationBranch" NOT NULL ENABLE)
  ALTER TABLE "MyGeotabVersionInfo" MODIFY ("ApplicationCommit" NOT NULL ENABLE)
  ALTER TABLE "MyGeotabVersionInfo" MODIFY ("GoTalkVersion" NOT NULL ENABLE)
  ALTER TABLE "MyGeotabVersionInfo" MODIFY ("RecordCreationTimeUtc" NOT NULL ENABLE)
  ALTER TABLE "MyGeotabVersionInfo" ADD CONSTRAINT "PK_MYGEOTABVERSIONINFO" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table OVDSServerCommands
--------------------------------------------------------

  ALTER TABLE "OVDSServerCommands" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "OVDSServerCommands" MODIFY ("Command" NOT NULL ENABLE)
  ALTER TABLE "OVDSServerCommands" MODIFY ("RecordCreationTimeUtc" NOT NULL ENABLE)
  ALTER TABLE "OVDSServerCommands" ADD CONSTRAINT "PK_OVDSSERVERCOMMANDS" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table Rules
--------------------------------------------------------

  ALTER TABLE "Rules" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "Rules" MODIFY ("GeotabId" NOT NULL ENABLE)
  ALTER TABLE "Rules" MODIFY ("Version" NOT NULL ENABLE)
  ALTER TABLE "Rules" MODIFY ("EntityStatus" NOT NULL ENABLE)
  ALTER TABLE "Rules" MODIFY ("RecordLastChangedUtc" NOT NULL ENABLE)
  ALTER TABLE "Rules" ADD CONSTRAINT "PK_RULES" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table StatusData
--------------------------------------------------------

  ALTER TABLE "StatusData" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "StatusData" MODIFY ("GeotabId" NOT NULL ENABLE)
  ALTER TABLE "StatusData" MODIFY ("DeviceId" NOT NULL ENABLE)
  ALTER TABLE "StatusData" MODIFY ("DiagnosticId" NOT NULL ENABLE)
  ALTER TABLE "StatusData" MODIFY ("RecordCreationTimeUtc" NOT NULL ENABLE)
  ALTER TABLE "StatusData" ADD CONSTRAINT "PK_STATUSDATA" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table Trips
--------------------------------------------------------

  ALTER TABLE "Trips" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "Trips" MODIFY ("GeotabId" NOT NULL ENABLE)
  ALTER TABLE "Trips" MODIFY ("DeviceId" NOT NULL ENABLE)
  ALTER TABLE "Trips" MODIFY ("Distance" NOT NULL ENABLE)
  ALTER TABLE "Trips" MODIFY ("DriverId" NOT NULL ENABLE)
  ALTER TABLE "Trips" MODIFY ("DrivingDurationTicks" NOT NULL ENABLE)
  ALTER TABLE "Trips" MODIFY ("NextTripStart" NOT NULL ENABLE)
  ALTER TABLE "Trips" MODIFY ("Start" NOT NULL ENABLE)
  ALTER TABLE "Trips" MODIFY ("Stop" NOT NULL ENABLE)
  ALTER TABLE "Trips" MODIFY ("StopDurationTicks" NOT NULL ENABLE)
  ALTER TABLE "Trips" MODIFY ("RecordCreationTimeUtc" NOT NULL ENABLE)
  ALTER TABLE "Trips" ADD CONSTRAINT "PK_TRIPS" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table Users
--------------------------------------------------------

  ALTER TABLE "Users" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "Users" MODIFY ("GeotabId" NOT NULL ENABLE)
  ALTER TABLE "Users" MODIFY ("ActiveFrom" NOT NULL ENABLE)
  ALTER TABLE "Users" MODIFY ("ActiveTo" NOT NULL ENABLE)
  ALTER TABLE "Users" MODIFY ("IsDriver" NOT NULL ENABLE)
  ALTER TABLE "Users" MODIFY ("Name" NOT NULL ENABLE)
  ALTER TABLE "Users" MODIFY ("EntityStatus" NOT NULL ENABLE)
  ALTER TABLE "Users" MODIFY ("RecordLastChangedUtc" NOT NULL ENABLE)
  ALTER TABLE "Users" ADD CONSTRAINT "PK_USERS" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table ZoneTypes
--------------------------------------------------------

  ALTER TABLE "ZoneTypes" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "ZoneTypes" MODIFY ("GeotabId" NOT NULL ENABLE)
  ALTER TABLE "ZoneTypes" MODIFY ("Name" NOT NULL ENABLE)
  ALTER TABLE "ZoneTypes" MODIFY ("EntityStatus" NOT NULL ENABLE)
  ALTER TABLE "ZoneTypes" MODIFY ("RecordLastChangedUtc" NOT NULL ENABLE)
  ALTER TABLE "ZoneTypes" ADD CONSTRAINT "PK_ZONETYPES" PRIMARY KEY ("id")
  USING INDEX  ENABLE
--------------------------------------------------------
--  Constraints for Table Zones
--------------------------------------------------------

  ALTER TABLE "Zones" MODIFY ("id" NOT NULL ENABLE)
  ALTER TABLE "Zones" MODIFY ("GeotabId" NOT NULL ENABLE)
  ALTER TABLE "Zones" MODIFY ("Name" NOT NULL ENABLE)
  ALTER TABLE "Zones" MODIFY ("ZoneTypeIds" NOT NULL ENABLE)
  ALTER TABLE "Zones" MODIFY ("EntityStatus" NOT NULL ENABLE)
  ALTER TABLE "Zones" MODIFY ("RecordLastChangedUtc" NOT NULL ENABLE)
  ALTER TABLE "Zones" ADD CONSTRAINT "PK_ZONES" PRIMARY KEY ("id")
  USING INDEX  ENABLE
