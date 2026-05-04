-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- NOTES:
--   1: This script applies to the Geotab DIG Adapter database starting with
--      application version 5.0.0.0. It does not apply to earlier versions of the
--      application.
--   2: This script will not be updated. Any future schema changes will be included
--      in new incremental scripts tagged with the relevant application versions.
--   3: Be sure to connect to the "geotabadapterdb" database before executing.
--
-- DESCRIPTION:
--   This script is intended for use in creating the database schema for version
--   5.0.0.0 of the Geotab DIG Adapter in an empty database.
-- ================================================================================


/*** [START] Part 1: Create Schema ***/
CREATE SCHEMA IF NOT EXISTS gda;
/*** [END] Part 1: Create Schema ***/


/*** [START] Part 2: Create Tables ***/

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: MiddlewareVersionInfo
CREATE TABLE gda."MiddlewareVersionInfo" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "DatabaseVersion" varchar(50) NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: OServiceTracking
CREATE TABLE gda."OServiceTracking" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ServiceId" varchar(50) NOT NULL,
    "AdapterVersion" varchar(50) NULL,
    "AdapterMachineName" varchar(100) NULL,
    "EntitiesLastProcessedUtc" timestamp without time zone NULL,
    "LastProcessedFeedVersion" bigint NULL,
    "LastBatchSize" integer NULL,
    "SuccessCount" bigint NULL,
    "FailureCount" bigint NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: ProvisionedDevices
CREATE TABLE gda."ProvisionedDevices" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ThirdPartyId" varchar(50) NOT NULL,
    "ErpNo" varchar(50) NULL,
    "GeotabSerialNumber" varchar(50) NOT NULL,
    "IsOkayToSendDataToGeotab" boolean NOT NULL,
    "DeviceProvisionedDateTimeUtc" timestamp without time zone NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

CREATE UNIQUE INDEX "IX_ProvisionedDevices_ThirdPartyId"
ON gda."ProvisionedDevices" ("ThirdPartyId");


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_ProvisionDevices
CREATE TABLE gda."Q_ProvisionDevices" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ThirdPartyId" varchar(50) NOT NULL,
    "ErpNo" varchar(50) NULL,
    "HardwareId" integer NULL,
    "ProductId" integer NOT NULL,
    "PromoCode" varchar(50) NULL,
    "SubPlan" varchar(50) NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "RecordLastChangedUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "ProcessingStatus" smallint NOT NULL DEFAULT 0,
    "ProcessingStartTimeUtc" timestamp without time zone NULL,
    "RetryCount" smallint NOT NULL DEFAULT 0
);

CREATE INDEX "IX_Q_ProvisionDevices_PendingWork"
ON gda."Q_ProvisionDevices" ("id")
WHERE "ProcessingStatus" = 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_ProvisionDevicesFail
CREATE TABLE gda."Q_ProvisionDevicesFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "ErpNo" varchar(50) NULL,
    "HardwareId" integer NULL,
    "ProductId" integer NOT NULL,
    "PromoCode" varchar(50) NULL,
    "SubPlan" varchar(50) NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

CREATE INDEX "IX_Q_ProvisionDevicesFail_ThirdPartyId"
ON gda."Q_ProvisionDevicesFail" ("ThirdPartyId");


-- ================================================================================
-- DIG TELEMETRY RECORD QUEUE TABLES
-- ================================================================================
-- The following tables support the 12 DIG record types. External systems write
-- records to the queue tables, and the TelemetryDataService reads, transforms,
-- and posts them to the DIG API.
-- ================================================================================

-- ================================================================================
-- 1. GPS RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_GpsRecords
CREATE TABLE gda."Q_GpsRecords" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Latitude" real NOT NULL,
    "Longitude" real NOT NULL,
    "Speed" real NULL,
    "IsGpsValid" boolean NULL,
    "IsIgnitionOn" boolean NULL,
    "IsAuxiliary1On" boolean NULL,
    "IsAuxiliary2On" boolean NULL,
    "IsAuxiliary3On" boolean NULL,
    "IsAuxiliary4On" boolean NULL,
    "IsAuxiliary5On" boolean NULL,
    "IsAuxiliary6On" boolean NULL,
    "IsAuxiliary7On" boolean NULL,
    "IsAuxiliary8On" boolean NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "RecordLastChangedUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "ProcessingStatus" smallint NOT NULL DEFAULT 0,
    "ProcessingStartTimeUtc" timestamp without time zone NULL,
    "RetryCount" smallint NOT NULL DEFAULT 0
);

CREATE INDEX "IX_Q_GpsRecords_PendingWork"
ON gda."Q_GpsRecords" ("id")
WHERE "ProcessingStatus" = 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_GpsRecordsFail
CREATE TABLE gda."Q_GpsRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Latitude" real NOT NULL,
    "Longitude" real NOT NULL,
    "Speed" real NULL,
    "IsGpsValid" boolean NULL,
    "IsIgnitionOn" boolean NULL,
    "IsAuxiliary1On" boolean NULL,
    "IsAuxiliary2On" boolean NULL,
    "IsAuxiliary3On" boolean NULL,
    "IsAuxiliary4On" boolean NULL,
    "IsAuxiliary5On" boolean NULL,
    "IsAuxiliary6On" boolean NULL,
    "IsAuxiliary7On" boolean NULL,
    "IsAuxiliary8On" boolean NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

CREATE INDEX "IX_Q_GpsRecordsFail_ThirdPartyId"
ON gda."Q_GpsRecordsFail" ("ThirdPartyId");


-- ================================================================================
-- 2. ACCELERATION RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_AccelerationRecords
CREATE TABLE gda."Q_AccelerationRecords" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "X" smallint NOT NULL,
    "Y" smallint NOT NULL,
    "Z" smallint NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "RecordLastChangedUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "ProcessingStatus" smallint NOT NULL DEFAULT 0,
    "ProcessingStartTimeUtc" timestamp without time zone NULL,
    "RetryCount" smallint NOT NULL DEFAULT 0
);

CREATE INDEX "IX_Q_AccelerationRecords_PendingWork"
ON gda."Q_AccelerationRecords" ("id")
WHERE "ProcessingStatus" = 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_AccelerationRecordsFail
CREATE TABLE gda."Q_AccelerationRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "X" smallint NOT NULL,
    "Y" smallint NOT NULL,
    "Z" smallint NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

CREATE INDEX "IX_Q_AccelerationRecordsFail_ThirdPartyId"
ON gda."Q_AccelerationRecordsFail" ("ThirdPartyId");


-- ================================================================================
-- 3. BINARY RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_BinaryRecords
CREATE TABLE gda."Q_BinaryRecords" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Data" bytea NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "RecordLastChangedUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "ProcessingStatus" smallint NOT NULL DEFAULT 0,
    "ProcessingStartTimeUtc" timestamp without time zone NULL,
    "RetryCount" smallint NOT NULL DEFAULT 0
);

CREATE INDEX "IX_Q_BinaryRecords_PendingWork"
ON gda."Q_BinaryRecords" ("id")
WHERE "ProcessingStatus" = 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_BinaryRecordsFail
CREATE TABLE gda."Q_BinaryRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Data" bytea NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

CREATE INDEX "IX_Q_BinaryRecordsFail_ThirdPartyId"
ON gda."Q_BinaryRecordsFail" ("ThirdPartyId");


-- ================================================================================
-- 4. BLUETOOTH RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_BluetoothRecords
CREATE TABLE gda."Q_BluetoothRecords" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Address" varchar(17) NOT NULL,
    "Data" real NOT NULL,
    "DataType" smallint NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "RecordLastChangedUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "ProcessingStatus" smallint NOT NULL DEFAULT 0,
    "ProcessingStartTimeUtc" timestamp without time zone NULL,
    "RetryCount" smallint NOT NULL DEFAULT 0
);

CREATE INDEX "IX_Q_BluetoothRecords_PendingWork"
ON gda."Q_BluetoothRecords" ("id")
WHERE "ProcessingStatus" = 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_BluetoothRecordsFail
CREATE TABLE gda."Q_BluetoothRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Address" varchar(17) NOT NULL,
    "Data" real NOT NULL,
    "DataType" smallint NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

CREATE INDEX "IX_Q_BluetoothRecordsFail_ThirdPartyId"
ON gda."Q_BluetoothRecordsFail" ("ThirdPartyId");


-- ================================================================================
-- 5. DRIVER CHANGE RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_DriverChangeRecords
CREATE TABLE gda."Q_DriverChangeRecords" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "KeyType" smallint NOT NULL,
    "DriverId" bytea NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "RecordLastChangedUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "ProcessingStatus" smallint NOT NULL DEFAULT 0,
    "ProcessingStartTimeUtc" timestamp without time zone NULL,
    "RetryCount" smallint NOT NULL DEFAULT 0
);

CREATE INDEX "IX_Q_DriverChangeRecords_PendingWork"
ON gda."Q_DriverChangeRecords" ("id")
WHERE "ProcessingStatus" = 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_DriverChangeRecordsFail
CREATE TABLE gda."Q_DriverChangeRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "KeyType" smallint NOT NULL,
    "DriverId" bytea NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

CREATE INDEX "IX_Q_DriverChangeRecordsFail_ThirdPartyId"
ON gda."Q_DriverChangeRecordsFail" ("ThirdPartyId");


-- ================================================================================
-- 6. GENERIC FAULT RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_GenericFaultRecords
CREATE TABLE gda."Q_GenericFaultRecords" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Code" integer NOT NULL,
    "FaultStateActive" boolean NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "RecordLastChangedUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "ProcessingStatus" smallint NOT NULL DEFAULT 0,
    "ProcessingStartTimeUtc" timestamp without time zone NULL,
    "RetryCount" smallint NOT NULL DEFAULT 0
);

CREATE INDEX "IX_Q_GenericFaultRecords_PendingWork"
ON gda."Q_GenericFaultRecords" ("id")
WHERE "ProcessingStatus" = 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_GenericFaultRecordsFail
CREATE TABLE gda."Q_GenericFaultRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Code" integer NOT NULL,
    "FaultStateActive" boolean NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

CREATE INDEX "IX_Q_GenericFaultRecordsFail_ThirdPartyId"
ON gda."Q_GenericFaultRecordsFail" ("ThirdPartyId");


-- ================================================================================
-- 7. GENERIC STATUS RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_GenericStatusRecords
CREATE TABLE gda."Q_GenericStatusRecords" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Code" integer NOT NULL,
    "Value" integer NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "RecordLastChangedUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "ProcessingStatus" smallint NOT NULL DEFAULT 0,
    "ProcessingStartTimeUtc" timestamp without time zone NULL,
    "RetryCount" smallint NOT NULL DEFAULT 0
);

CREATE INDEX "IX_Q_GenericStatusRecords_PendingWork"
ON gda."Q_GenericStatusRecords" ("id")
WHERE "ProcessingStatus" = 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_GenericStatusRecordsFail
CREATE TABLE gda."Q_GenericStatusRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Code" integer NOT NULL,
    "Value" integer NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

CREATE INDEX "IX_Q_GenericStatusRecordsFail_ThirdPartyId"
ON gda."Q_GenericStatusRecordsFail" ("ThirdPartyId");


-- ================================================================================
-- 8. J1708 FAULT RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_J1708FaultRecords
CREATE TABLE gda."Q_J1708FaultRecords" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "MessageId" smallint NOT NULL,
    "ParameterId" smallint NULL,
    "SubsystemId" smallint NULL,
    "FailureModeIdentifier" smallint NOT NULL,
    "OccurrenceCount" smallint NOT NULL,
    "FaultStateActive" boolean NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "RecordLastChangedUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "ProcessingStatus" smallint NOT NULL DEFAULT 0,
    "ProcessingStartTimeUtc" timestamp without time zone NULL,
    "RetryCount" smallint NOT NULL DEFAULT 0
);

CREATE INDEX "IX_Q_J1708FaultRecords_PendingWork"
ON gda."Q_J1708FaultRecords" ("id")
WHERE "ProcessingStatus" = 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_J1708FaultRecordsFail
CREATE TABLE gda."Q_J1708FaultRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "MessageId" smallint NOT NULL,
    "ParameterId" smallint NULL,
    "SubsystemId" smallint NULL,
    "FailureModeIdentifier" smallint NOT NULL,
    "OccurrenceCount" smallint NOT NULL,
    "FaultStateActive" boolean NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

CREATE INDEX "IX_Q_J1708FaultRecordsFail_ThirdPartyId"
ON gda."Q_J1708FaultRecordsFail" ("ThirdPartyId");


-- ================================================================================
-- 9. J1939 FAULT RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_J1939FaultRecords
CREATE TABLE gda."Q_J1939FaultRecords" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "SuspectParameterNumber" integer NOT NULL,
    "FailureModeIdentifier" smallint NOT NULL,
    "OccurrenceCount" smallint NOT NULL,
    "SourceAddress" smallint NOT NULL,
    "MalfunctionLamp" boolean NULL,
    "RedStopLamp" boolean NULL,
    "AmberWarningLamp" boolean NULL,
    "ProtectWarningLamp" boolean NULL,
    "FaultStateActive" boolean NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "RecordLastChangedUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "ProcessingStatus" smallint NOT NULL DEFAULT 0,
    "ProcessingStartTimeUtc" timestamp without time zone NULL,
    "RetryCount" smallint NOT NULL DEFAULT 0
);

CREATE INDEX "IX_Q_J1939FaultRecords_PendingWork"
ON gda."Q_J1939FaultRecords" ("id")
WHERE "ProcessingStatus" = 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_J1939FaultRecordsFail
CREATE TABLE gda."Q_J1939FaultRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "SuspectParameterNumber" integer NOT NULL,
    "FailureModeIdentifier" smallint NOT NULL,
    "OccurrenceCount" smallint NOT NULL,
    "SourceAddress" smallint NOT NULL,
    "MalfunctionLamp" boolean NULL,
    "RedStopLamp" boolean NULL,
    "AmberWarningLamp" boolean NULL,
    "ProtectWarningLamp" boolean NULL,
    "FaultStateActive" boolean NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

CREATE INDEX "IX_Q_J1939FaultRecordsFail_ThirdPartyId"
ON gda."Q_J1939FaultRecordsFail" ("ThirdPartyId");


-- ================================================================================
-- 10. OBDII FAULT RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_ObdiiFaultRecords
CREATE TABLE gda."Q_ObdiiFaultRecords" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Code" varchar(10) NOT NULL,
    "FaultStateActive" boolean NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "RecordLastChangedUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "ProcessingStatus" smallint NOT NULL DEFAULT 0,
    "ProcessingStartTimeUtc" timestamp without time zone NULL,
    "RetryCount" smallint NOT NULL DEFAULT 0
);

CREATE INDEX "IX_Q_ObdiiFaultRecords_PendingWork"
ON gda."Q_ObdiiFaultRecords" ("id")
WHERE "ProcessingStatus" = 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_ObdiiFaultRecordsFail
CREATE TABLE gda."Q_ObdiiFaultRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Code" varchar(10) NOT NULL,
    "FaultStateActive" boolean NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

CREATE INDEX "IX_Q_ObdiiFaultRecordsFail_ThirdPartyId"
ON gda."Q_ObdiiFaultRecordsFail" ("ThirdPartyId");


-- ================================================================================
-- 11. VIN RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_VinRecords
CREATE TABLE gda."Q_VinRecords" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "VehicleIdentificationNumber" varchar(17) NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "RecordLastChangedUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "ProcessingStatus" smallint NOT NULL DEFAULT 0,
    "ProcessingStartTimeUtc" timestamp without time zone NULL,
    "RetryCount" smallint NOT NULL DEFAULT 0
);

CREATE INDEX "IX_Q_VinRecords_PendingWork"
ON gda."Q_VinRecords" ("id")
WHERE "ProcessingStatus" = 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_VinRecordsFail
CREATE TABLE gda."Q_VinRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "VehicleIdentificationNumber" varchar(17) NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

CREATE INDEX "IX_Q_VinRecordsFail_ThirdPartyId"
ON gda."Q_VinRecordsFail" ("ThirdPartyId");


-- ================================================================================
-- INVALID RECORDS RETRIEVAL TABLES
-- ================================================================================
-- The following tables support the InvalidRecordRetrievalService which polls
-- the DIG API /invalidrecords endpoint to retrieve and store records that
-- were marked as invalid during processing.
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: DIGInvalidRecords
-- Stores invalid records retrieved from the DIG API for later analysis.
CREATE TABLE gda."DIGInvalidRecords" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "GeotabGUID" varchar(50) NOT NULL,
    "RecordType" varchar(50) NOT NULL,
    "SerialNo" varchar(50) NOT NULL,
    "RecordDateTime" timestamp without time zone NOT NULL,
    "BaseRecordJson" text NOT NULL,
    "Cause" varchar(1000) NOT NULL,
    "TimeStamp" timestamp without time zone NOT NULL,
    "UserId" varchar(100) NOT NULL,
    "RetrievedAtUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

CREATE UNIQUE INDEX "IX_DIGInvalidRecords_GeotabGUID"
ON gda."DIGInvalidRecords" ("GeotabGUID");

CREATE INDEX "IX_DIGInvalidRecords_RecordType_TimeStamp"
ON gda."DIGInvalidRecords" ("RecordType", "TimeStamp" DESC);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: DIGInvalidRecordsCursor
-- Persists the pagination cursor for the InvalidRecordRetrievalService.
-- Single-row table enforced by CHECK constraint.
CREATE TABLE gda."DIGInvalidRecordsCursor" (
    "id" integer NOT NULL DEFAULT 1 CHECK ("id" = 1) PRIMARY KEY,
    "NextResultKey" integer NOT NULL DEFAULT 0,
    "LastUpdatedUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

-- Insert the initial cursor row
INSERT INTO gda."DIGInvalidRecordsCursor" ("id", "NextResultKey", "LastUpdatedUtc")
VALUES (1, 0, (now() AT TIME ZONE 'UTC'));

/*** [END] Part 2: Create Tables ***/


/*** [START] Part 3: Create Functions (Stored Procedures) ***/

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQProvisionDevicesBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_ProvisionDevices records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in multi-instance
--     scenarios and to ensure crash recovery. Records that are "stale" (stuck in progress
--     beyond the threshold) are automatically reclaimed.
-- Parameters:
--   p_batch_size: Maximum number of records to claim in a single batch.
--   p_stale_threshold_minutes: Minutes after which an "in progress" record is considered
--                              stale and eligible for reclaim.
-- Returns: The claimed Q_ProvisionDevices records.
CREATE OR REPLACE FUNCTION gda."spClaimQProvisionDevicesBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer
)
RETURNS SETOF gda."Q_ProvisionDevices"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    -- Update and claim records atomically
    WITH claimed AS (
        SELECT "id"
        FROM gda."Q_ProvisionDevices"
        WHERE "ProcessingStatus" = 0
           OR ("ProcessingStatus" = 1 AND "ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY "RecordCreationTimeUtc"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    )
    UPDATE gda."Q_ProvisionDevices" q
    SET
        "ProcessingStatus" = 1,
        "ProcessingStartTimeUtc" = v_processing_start_time_utc,
        "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
    FROM claimed c
    WHERE q."id" = c."id";

    -- Return the claimed records
    RETURN QUERY
    SELECT *
    FROM gda."Q_ProvisionDevices"
    WHERE "ProcessingStatus" = 1
      AND "ProcessingStartTimeUtc" = v_processing_start_time_utc
    ORDER BY "RecordCreationTimeUtc";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQGpsRecordsBatch
CREATE OR REPLACE FUNCTION gda."spClaimQGpsRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer
)
RETURNS SETOF gda."Q_GpsRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    WITH claimed AS (
        SELECT "id"
        FROM gda."Q_GpsRecords"
        WHERE "ProcessingStatus" = 0
           OR ("ProcessingStatus" = 1 AND "ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY "RecordCreationTimeUtc"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    )
    UPDATE gda."Q_GpsRecords" q
    SET
        "ProcessingStatus" = 1,
        "ProcessingStartTimeUtc" = v_processing_start_time_utc,
        "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
    FROM claimed c
    WHERE q."id" = c."id";

    RETURN QUERY
    SELECT *
    FROM gda."Q_GpsRecords"
    WHERE "ProcessingStatus" = 1
      AND "ProcessingStartTimeUtc" = v_processing_start_time_utc
    ORDER BY "RecordCreationTimeUtc";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQAccelerationRecordsBatch
CREATE OR REPLACE FUNCTION gda."spClaimQAccelerationRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer
)
RETURNS SETOF gda."Q_AccelerationRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    WITH claimed AS (
        SELECT "id"
        FROM gda."Q_AccelerationRecords"
        WHERE "ProcessingStatus" = 0
           OR ("ProcessingStatus" = 1 AND "ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY "RecordCreationTimeUtc"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    )
    UPDATE gda."Q_AccelerationRecords" q
    SET
        "ProcessingStatus" = 1,
        "ProcessingStartTimeUtc" = v_processing_start_time_utc,
        "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
    FROM claimed c
    WHERE q."id" = c."id";

    RETURN QUERY
    SELECT *
    FROM gda."Q_AccelerationRecords"
    WHERE "ProcessingStatus" = 1
      AND "ProcessingStartTimeUtc" = v_processing_start_time_utc
    ORDER BY "RecordCreationTimeUtc";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQBinaryRecordsBatch
CREATE OR REPLACE FUNCTION gda."spClaimQBinaryRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer
)
RETURNS SETOF gda."Q_BinaryRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    WITH claimed AS (
        SELECT "id"
        FROM gda."Q_BinaryRecords"
        WHERE "ProcessingStatus" = 0
           OR ("ProcessingStatus" = 1 AND "ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY "RecordCreationTimeUtc"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    )
    UPDATE gda."Q_BinaryRecords" q
    SET
        "ProcessingStatus" = 1,
        "ProcessingStartTimeUtc" = v_processing_start_time_utc,
        "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
    FROM claimed c
    WHERE q."id" = c."id";

    RETURN QUERY
    SELECT *
    FROM gda."Q_BinaryRecords"
    WHERE "ProcessingStatus" = 1
      AND "ProcessingStartTimeUtc" = v_processing_start_time_utc
    ORDER BY "RecordCreationTimeUtc";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQBluetoothRecordsBatch
CREATE OR REPLACE FUNCTION gda."spClaimQBluetoothRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer
)
RETURNS SETOF gda."Q_BluetoothRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    WITH claimed AS (
        SELECT "id"
        FROM gda."Q_BluetoothRecords"
        WHERE "ProcessingStatus" = 0
           OR ("ProcessingStatus" = 1 AND "ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY "RecordCreationTimeUtc"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    )
    UPDATE gda."Q_BluetoothRecords" q
    SET
        "ProcessingStatus" = 1,
        "ProcessingStartTimeUtc" = v_processing_start_time_utc,
        "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
    FROM claimed c
    WHERE q."id" = c."id";

    RETURN QUERY
    SELECT *
    FROM gda."Q_BluetoothRecords"
    WHERE "ProcessingStatus" = 1
      AND "ProcessingStartTimeUtc" = v_processing_start_time_utc
    ORDER BY "RecordCreationTimeUtc";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQDriverChangeRecordsBatch
CREATE OR REPLACE FUNCTION gda."spClaimQDriverChangeRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer
)
RETURNS SETOF gda."Q_DriverChangeRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    WITH claimed AS (
        SELECT "id"
        FROM gda."Q_DriverChangeRecords"
        WHERE "ProcessingStatus" = 0
           OR ("ProcessingStatus" = 1 AND "ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY "RecordCreationTimeUtc"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    )
    UPDATE gda."Q_DriverChangeRecords" q
    SET
        "ProcessingStatus" = 1,
        "ProcessingStartTimeUtc" = v_processing_start_time_utc,
        "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
    FROM claimed c
    WHERE q."id" = c."id";

    RETURN QUERY
    SELECT *
    FROM gda."Q_DriverChangeRecords"
    WHERE "ProcessingStatus" = 1
      AND "ProcessingStartTimeUtc" = v_processing_start_time_utc
    ORDER BY "RecordCreationTimeUtc";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQGenericFaultRecordsBatch
CREATE OR REPLACE FUNCTION gda."spClaimQGenericFaultRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer
)
RETURNS SETOF gda."Q_GenericFaultRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    WITH claimed AS (
        SELECT "id"
        FROM gda."Q_GenericFaultRecords"
        WHERE "ProcessingStatus" = 0
           OR ("ProcessingStatus" = 1 AND "ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY "RecordCreationTimeUtc"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    )
    UPDATE gda."Q_GenericFaultRecords" q
    SET
        "ProcessingStatus" = 1,
        "ProcessingStartTimeUtc" = v_processing_start_time_utc,
        "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
    FROM claimed c
    WHERE q."id" = c."id";

    RETURN QUERY
    SELECT *
    FROM gda."Q_GenericFaultRecords"
    WHERE "ProcessingStatus" = 1
      AND "ProcessingStartTimeUtc" = v_processing_start_time_utc
    ORDER BY "RecordCreationTimeUtc";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQGenericStatusRecordsBatch
CREATE OR REPLACE FUNCTION gda."spClaimQGenericStatusRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer
)
RETURNS SETOF gda."Q_GenericStatusRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    WITH claimed AS (
        SELECT "id"
        FROM gda."Q_GenericStatusRecords"
        WHERE "ProcessingStatus" = 0
           OR ("ProcessingStatus" = 1 AND "ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY "RecordCreationTimeUtc"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    )
    UPDATE gda."Q_GenericStatusRecords" q
    SET
        "ProcessingStatus" = 1,
        "ProcessingStartTimeUtc" = v_processing_start_time_utc,
        "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
    FROM claimed c
    WHERE q."id" = c."id";

    RETURN QUERY
    SELECT *
    FROM gda."Q_GenericStatusRecords"
    WHERE "ProcessingStatus" = 1
      AND "ProcessingStartTimeUtc" = v_processing_start_time_utc
    ORDER BY "RecordCreationTimeUtc";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQJ1708FaultRecordsBatch
CREATE OR REPLACE FUNCTION gda."spClaimQJ1708FaultRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer
)
RETURNS SETOF gda."Q_J1708FaultRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    WITH claimed AS (
        SELECT "id"
        FROM gda."Q_J1708FaultRecords"
        WHERE "ProcessingStatus" = 0
           OR ("ProcessingStatus" = 1 AND "ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY "RecordCreationTimeUtc"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    )
    UPDATE gda."Q_J1708FaultRecords" q
    SET
        "ProcessingStatus" = 1,
        "ProcessingStartTimeUtc" = v_processing_start_time_utc,
        "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
    FROM claimed c
    WHERE q."id" = c."id";

    RETURN QUERY
    SELECT *
    FROM gda."Q_J1708FaultRecords"
    WHERE "ProcessingStatus" = 1
      AND "ProcessingStartTimeUtc" = v_processing_start_time_utc
    ORDER BY "RecordCreationTimeUtc";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQJ1939FaultRecordsBatch
CREATE OR REPLACE FUNCTION gda."spClaimQJ1939FaultRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer
)
RETURNS SETOF gda."Q_J1939FaultRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    WITH claimed AS (
        SELECT "id"
        FROM gda."Q_J1939FaultRecords"
        WHERE "ProcessingStatus" = 0
           OR ("ProcessingStatus" = 1 AND "ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY "RecordCreationTimeUtc"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    )
    UPDATE gda."Q_J1939FaultRecords" q
    SET
        "ProcessingStatus" = 1,
        "ProcessingStartTimeUtc" = v_processing_start_time_utc,
        "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
    FROM claimed c
    WHERE q."id" = c."id";

    RETURN QUERY
    SELECT *
    FROM gda."Q_J1939FaultRecords"
    WHERE "ProcessingStatus" = 1
      AND "ProcessingStartTimeUtc" = v_processing_start_time_utc
    ORDER BY "RecordCreationTimeUtc";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQObdiiFaultRecordsBatch
CREATE OR REPLACE FUNCTION gda."spClaimQObdiiFaultRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer
)
RETURNS SETOF gda."Q_ObdiiFaultRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    WITH claimed AS (
        SELECT "id"
        FROM gda."Q_ObdiiFaultRecords"
        WHERE "ProcessingStatus" = 0
           OR ("ProcessingStatus" = 1 AND "ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY "RecordCreationTimeUtc"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    )
    UPDATE gda."Q_ObdiiFaultRecords" q
    SET
        "ProcessingStatus" = 1,
        "ProcessingStartTimeUtc" = v_processing_start_time_utc,
        "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
    FROM claimed c
    WHERE q."id" = c."id";

    RETURN QUERY
    SELECT *
    FROM gda."Q_ObdiiFaultRecords"
    WHERE "ProcessingStatus" = 1
      AND "ProcessingStartTimeUtc" = v_processing_start_time_utc
    ORDER BY "RecordCreationTimeUtc";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQVinRecordsBatch
CREATE OR REPLACE FUNCTION gda."spClaimQVinRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer
)
RETURNS SETOF gda."Q_VinRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    WITH claimed AS (
        SELECT "id"
        FROM gda."Q_VinRecords"
        WHERE "ProcessingStatus" = 0
           OR ("ProcessingStatus" = 1 AND "ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY "RecordCreationTimeUtc"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    )
    UPDATE gda."Q_VinRecords" q
    SET
        "ProcessingStatus" = 1,
        "ProcessingStartTimeUtc" = v_processing_start_time_utc,
        "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
    FROM claimed c
    WHERE q."id" = c."id";

    RETURN QUERY
    SELECT *
    FROM gda."Q_VinRecords"
    WHERE "ProcessingStatus" = 1
      AND "ProcessingStartTimeUtc" = v_processing_start_time_utc
    ORDER BY "RecordCreationTimeUtc";
END;
$$;

/*** [END] Part 3: Create Functions (Stored Procedures) ***/


/*** [START] Part 4: Grant Permissions ***/
-- Grant usage on schema
GRANT USAGE ON SCHEMA gda TO geotabdigadapter_client;

-- Grant select, insert, update, delete on all tables
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA gda TO geotabdigadapter_client;

-- Grant execute on all functions
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA gda TO geotabdigadapter_client;

-- Grant usage on all sequences (for IDENTITY columns)
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA gda TO geotabdigadapter_client;

-- Set default privileges for future objects
ALTER DEFAULT PRIVILEGES IN SCHEMA gda
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO geotabdigadapter_client;

ALTER DEFAULT PRIVILEGES IN SCHEMA gda
GRANT EXECUTE ON FUNCTIONS TO geotabdigadapter_client;

ALTER DEFAULT PRIVILEGES IN SCHEMA gda
GRANT USAGE, SELECT ON SEQUENCES TO geotabdigadapter_client;
/*** [END] Part 4: Grant Permissions ***/


/*** [START] Part 5: Database Version Update ***/
-- Insert a record into the MiddlewareVersionInfo table to reflect the current
-- database version.
INSERT INTO gda."MiddlewareVersionInfo" ("DatabaseVersion", "RecordCreationTimeUtc")
VALUES ('5.0.0.0', (now() AT TIME ZONE 'UTC'));
/*** [END] Part 5: Database Version Update ***/
