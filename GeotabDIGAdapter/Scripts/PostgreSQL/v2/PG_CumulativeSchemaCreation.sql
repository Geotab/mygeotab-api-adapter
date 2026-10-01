-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- NOTES:
--   1: This script applies to the Geotab DIG Adapter database starting with
--      application version 5.0.0.0. It does not apply to earlier versions of the
--      application.
--   2: This script will be updated with future schema changes such that someone
--      starting with a future version of the application can use this script as a
--      starting point without having to apply any earlier incremental updates.
--   3: Be sure to connect to the "geotabadapterdb" database before executing.
--   4: Part 7 creates the pgstattuple extension, which needs a superuser. If the
--      user running this script cannot create it, or the server does not offer
--      it, the script carries on with a WARNING and everything else is created;
--      the adapter then skips Level 2 database maintenance, and says so in its
--      log, until the extension exists and geotabdigadapter_client may EXECUTE it.
--
-- DESCRIPTION:
--   This script is intended for use in creating the database schema in an empty
--   database starting from version 5.0.0.0 of the Geotab DIG Adapter and including
--   any cumulative schema changes up to the current application version at time of
--   download.
--
--   Use this script for a FRESH INSTALL. To upgrade an EXISTING database, run the
--   incremental PG_<version>_*.sql scripts instead, in version order.
--
-- PARTITIONED TABLES:
--   The thirteen Q_*Fail tables are RANGE-partitioned on "RecordCreationTimeUtc"
--   so that old failure records can be reclaimed by dropping partitions instead of
--   by deleting rows. Three consequences are worth knowing before you read on:
--
--     a. Their primary key is ("id", "RecordCreationTimeUtc"), not ("id") alone.
--        PostgreSQL requires the partition key to appear in every unique index on
--        a partitioned table. "id" remains unique on its own in practice, because
--        it is still served by a single identity sequence per table.
--     b. Each one is created with a DEFAULT partition, so an INSERT can never fail
--        for want of a period partition. Period partitions are created by
--        spManageDigPartitions, which the DatabaseMaintenanceService runs.
--     c. The live Q_* queue tables are deliberately NOT partitioned. They are work
--        queues whose rows are deleted once transmitted, not accumulators, and a
--        time-based partition drop on one would discard unprocessed records.
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

-- One row per service. Without this, two instances cold-starting against the same
-- database both insert the full set and every later startup fails on duplicates.
CREATE UNIQUE INDEX "IX_OServiceTracking_ServiceId"
ON gda."OServiceTracking" ("ServiceId");


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

-- Serves DeviceDIGReadinessService's batch query, which runs every 10 seconds:
--
--     SELECT * FROM gda."ProvisionedDevices"
--     WHERE "IsOkayToSendDataToGeotab" = false
--     ORDER BY "RecordLastChangedUtc"
--     LIMIT @BatchSize;
--
-- Neither the predicate nor the ORDER BY had an index, so that statement read and
-- sorted the whole table 8,640 times a day, at a cost growing with the size of the
-- fleet. The HEALTHY case is the WORST case: once every device is DIG-ready nothing
-- matches, so the LIMIT can never be satisfied and there is no early exit - every
-- row is read to conclude that none qualify.
--
-- Measured on this table with the statement the service issues: 6.5 ms -> 0.2 ms at
-- 50,000 devices and 27.4 ms -> 0.3 ms at 250,000 - the cost stops growing with the
-- fleet. While a backlog is being worked off it is 7.3 ms -> 2.1 ms at 50,000. The
-- partial predicate is what makes it nearly free: the index holds only the devices
-- still awaiting a readiness check, so on a healthy fleet it holds NOTHING and
-- occupies 8 KB whatever the fleet size. It costs ~50% on a 5,000-row provisioning
-- insert - 93 ms to 141 ms - against a provisioning path that is MyAdmin-bound at
-- about six seconds PER DEVICE, and it makes the service's own write-back no slower.
CREATE INDEX "IX_ProvisionedDevices_PendingDIGReadiness"
ON gda."ProvisionedDevices" ("RecordLastChangedUtc")
WHERE "IsOkayToSendDataToGeotab" = false;


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

-- Two indexes for the claim function. The PendingWork index above serves neither
-- statement: it is partial on "ProcessingStatus" = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that
-- predicate - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take p_batch_size rows, so the cost grows with
-- queue depth - worst exactly when the queue has backed up and throughput matters
-- most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the LIMIT can never
-- be satisfied, so unindexed every row is read before concluding that none qualify.
-- "RetryCount" > 0 is a deliberate SUPERSET of the sweep's own
-- "RetryCount" >= p_max_claim_attempts - the configured ceiling is never below 1 - so
-- this predicate needs no revision if that floor moves, and the index holds only rows
-- a device-lookup hold or a stale reclaim has touched - few or none on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,953 ms to 90 ms here and from 5,073 ms to 464 ms on SQL Server. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. They cost 40 MB and 8 KB
-- respectively at that depth, and 4.3% on the insert path, which retains ~25x
-- headroom over the feed either way.
CREATE INDEX "IX_Q_ProvisionDevices_RecordCreationTimeUtc"
ON gda."Q_ProvisionDevices" ("RecordCreationTimeUtc");

CREATE INDEX "IX_Q_ProvisionDevices_Exhausted"
ON gda."Q_ProvisionDevices" ("id")
WHERE "RetryCount" > 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_ProvisionDevicesFail
CREATE TABLE gda."Q_ProvisionDevicesFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "ErpNo" varchar(50) NULL,
    "HardwareId" integer NULL,
    "ProductId" integer NOT NULL,
    "PromoCode" varchar(50) NULL,
    "SubPlan" varchar(50) NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    PRIMARY KEY ("id", "RecordCreationTimeUtc")
)
PARTITION BY RANGE ("RecordCreationTimeUtc");

-- The DEFAULT partition guarantees an INSERT can never fail for want of a
-- period partition. spManageDigPartitions creates the period partitions.
CREATE TABLE gda."Q_ProvisionDevicesFail_default" PARTITION OF gda."Q_ProvisionDevicesFail" DEFAULT;

CREATE INDEX "IX_Q_ProvisionDevicesFail_ThirdPartyId"
ON gda."Q_ProvisionDevicesFail" ("ThirdPartyId");


-- ================================================================================
-- DIG TELEMETRY RECORD QUEUE TABLES
-- ================================================================================
-- The following tables support the 11 DIG record types. External systems write
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

-- Two indexes for the claim function. The PendingWork index above serves neither
-- statement: it is partial on "ProcessingStatus" = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that
-- predicate - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take p_batch_size rows, so the cost grows with
-- queue depth - worst exactly when the queue has backed up and throughput matters
-- most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the LIMIT can never
-- be satisfied, so unindexed every row is read before concluding that none qualify.
-- "RetryCount" > 0 is a deliberate SUPERSET of the sweep's own
-- "RetryCount" >= p_max_claim_attempts - the configured ceiling is never below 1 - so
-- this predicate needs no revision if that floor moves, and the index holds only rows
-- a device-lookup hold or a stale reclaim has touched - few or none on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,953 ms to 90 ms here and from 5,073 ms to 464 ms on SQL Server. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. They cost 40 MB and 8 KB
-- respectively at that depth, and 4.3% on the insert path, which retains ~25x
-- headroom over the feed either way.
CREATE INDEX "IX_Q_GpsRecords_RecordCreationTimeUtc"
ON gda."Q_GpsRecords" ("RecordCreationTimeUtc");

CREATE INDEX "IX_Q_GpsRecords_Exhausted"
ON gda."Q_GpsRecords" ("id")
WHERE "RetryCount" > 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_GpsRecordsFail
CREATE TABLE gda."Q_GpsRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY,
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
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    PRIMARY KEY ("id", "RecordCreationTimeUtc")
)
PARTITION BY RANGE ("RecordCreationTimeUtc");

-- The DEFAULT partition guarantees an INSERT can never fail for want of a
-- period partition. spManageDigPartitions creates the period partitions.
CREATE TABLE gda."Q_GpsRecordsFail_default" PARTITION OF gda."Q_GpsRecordsFail" DEFAULT;

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

-- Two indexes for the claim function. The PendingWork index above serves neither
-- statement: it is partial on "ProcessingStatus" = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that
-- predicate - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take p_batch_size rows, so the cost grows with
-- queue depth - worst exactly when the queue has backed up and throughput matters
-- most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the LIMIT can never
-- be satisfied, so unindexed every row is read before concluding that none qualify.
-- "RetryCount" > 0 is a deliberate SUPERSET of the sweep's own
-- "RetryCount" >= p_max_claim_attempts - the configured ceiling is never below 1 - so
-- this predicate needs no revision if that floor moves, and the index holds only rows
-- a device-lookup hold or a stale reclaim has touched - few or none on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,953 ms to 90 ms here and from 5,073 ms to 464 ms on SQL Server. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. They cost 40 MB and 8 KB
-- respectively at that depth, and 4.3% on the insert path, which retains ~25x
-- headroom over the feed either way.
CREATE INDEX "IX_Q_AccelerationRecords_RecordCreationTimeUtc"
ON gda."Q_AccelerationRecords" ("RecordCreationTimeUtc");

CREATE INDEX "IX_Q_AccelerationRecords_Exhausted"
ON gda."Q_AccelerationRecords" ("id")
WHERE "RetryCount" > 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_AccelerationRecordsFail
CREATE TABLE gda."Q_AccelerationRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "X" smallint NOT NULL,
    "Y" smallint NOT NULL,
    "Z" smallint NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    PRIMARY KEY ("id", "RecordCreationTimeUtc")
)
PARTITION BY RANGE ("RecordCreationTimeUtc");

-- The DEFAULT partition guarantees an INSERT can never fail for want of a
-- period partition. spManageDigPartitions creates the period partitions.
CREATE TABLE gda."Q_AccelerationRecordsFail_default" PARTITION OF gda."Q_AccelerationRecordsFail" DEFAULT;

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

-- Two indexes for the claim function. The PendingWork index above serves neither
-- statement: it is partial on "ProcessingStatus" = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that
-- predicate - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take p_batch_size rows, so the cost grows with
-- queue depth - worst exactly when the queue has backed up and throughput matters
-- most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the LIMIT can never
-- be satisfied, so unindexed every row is read before concluding that none qualify.
-- "RetryCount" > 0 is a deliberate SUPERSET of the sweep's own
-- "RetryCount" >= p_max_claim_attempts - the configured ceiling is never below 1 - so
-- this predicate needs no revision if that floor moves, and the index holds only rows
-- a device-lookup hold or a stale reclaim has touched - few or none on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,953 ms to 90 ms here and from 5,073 ms to 464 ms on SQL Server. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. They cost 40 MB and 8 KB
-- respectively at that depth, and 4.3% on the insert path, which retains ~25x
-- headroom over the feed either way.
CREATE INDEX "IX_Q_BinaryRecords_RecordCreationTimeUtc"
ON gda."Q_BinaryRecords" ("RecordCreationTimeUtc");

CREATE INDEX "IX_Q_BinaryRecords_Exhausted"
ON gda."Q_BinaryRecords" ("id")
WHERE "RetryCount" > 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_BinaryRecordsFail
CREATE TABLE gda."Q_BinaryRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Data" bytea NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    PRIMARY KEY ("id", "RecordCreationTimeUtc")
)
PARTITION BY RANGE ("RecordCreationTimeUtc");

-- The DEFAULT partition guarantees an INSERT can never fail for want of a
-- period partition. spManageDigPartitions creates the period partitions.
CREATE TABLE gda."Q_BinaryRecordsFail_default" PARTITION OF gda."Q_BinaryRecordsFail" DEFAULT;

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

-- Two indexes for the claim function. The PendingWork index above serves neither
-- statement: it is partial on "ProcessingStatus" = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that
-- predicate - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take p_batch_size rows, so the cost grows with
-- queue depth - worst exactly when the queue has backed up and throughput matters
-- most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the LIMIT can never
-- be satisfied, so unindexed every row is read before concluding that none qualify.
-- "RetryCount" > 0 is a deliberate SUPERSET of the sweep's own
-- "RetryCount" >= p_max_claim_attempts - the configured ceiling is never below 1 - so
-- this predicate needs no revision if that floor moves, and the index holds only rows
-- a device-lookup hold or a stale reclaim has touched - few or none on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,953 ms to 90 ms here and from 5,073 ms to 464 ms on SQL Server. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. They cost 40 MB and 8 KB
-- respectively at that depth, and 4.3% on the insert path, which retains ~25x
-- headroom over the feed either way.
CREATE INDEX "IX_Q_BluetoothRecords_RecordCreationTimeUtc"
ON gda."Q_BluetoothRecords" ("RecordCreationTimeUtc");

CREATE INDEX "IX_Q_BluetoothRecords_Exhausted"
ON gda."Q_BluetoothRecords" ("id")
WHERE "RetryCount" > 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_BluetoothRecordsFail
CREATE TABLE gda."Q_BluetoothRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Address" varchar(17) NOT NULL,
    "Data" real NOT NULL,
    "DataType" smallint NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    PRIMARY KEY ("id", "RecordCreationTimeUtc")
)
PARTITION BY RANGE ("RecordCreationTimeUtc");

-- The DEFAULT partition guarantees an INSERT can never fail for want of a
-- period partition. spManageDigPartitions creates the period partitions.
CREATE TABLE gda."Q_BluetoothRecordsFail_default" PARTITION OF gda."Q_BluetoothRecordsFail" DEFAULT;

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

-- Two indexes for the claim function. The PendingWork index above serves neither
-- statement: it is partial on "ProcessingStatus" = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that
-- predicate - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take p_batch_size rows, so the cost grows with
-- queue depth - worst exactly when the queue has backed up and throughput matters
-- most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the LIMIT can never
-- be satisfied, so unindexed every row is read before concluding that none qualify.
-- "RetryCount" > 0 is a deliberate SUPERSET of the sweep's own
-- "RetryCount" >= p_max_claim_attempts - the configured ceiling is never below 1 - so
-- this predicate needs no revision if that floor moves, and the index holds only rows
-- a device-lookup hold or a stale reclaim has touched - few or none on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,953 ms to 90 ms here and from 5,073 ms to 464 ms on SQL Server. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. They cost 40 MB and 8 KB
-- respectively at that depth, and 4.3% on the insert path, which retains ~25x
-- headroom over the feed either way.
CREATE INDEX "IX_Q_DriverChangeRecords_RecordCreationTimeUtc"
ON gda."Q_DriverChangeRecords" ("RecordCreationTimeUtc");

CREATE INDEX "IX_Q_DriverChangeRecords_Exhausted"
ON gda."Q_DriverChangeRecords" ("id")
WHERE "RetryCount" > 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_DriverChangeRecordsFail
CREATE TABLE gda."Q_DriverChangeRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "KeyType" smallint NOT NULL,
    "DriverId" bytea NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    PRIMARY KEY ("id", "RecordCreationTimeUtc")
)
PARTITION BY RANGE ("RecordCreationTimeUtc");

-- The DEFAULT partition guarantees an INSERT can never fail for want of a
-- period partition. spManageDigPartitions creates the period partitions.
CREATE TABLE gda."Q_DriverChangeRecordsFail_default" PARTITION OF gda."Q_DriverChangeRecordsFail" DEFAULT;

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

-- Two indexes for the claim function. The PendingWork index above serves neither
-- statement: it is partial on "ProcessingStatus" = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that
-- predicate - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take p_batch_size rows, so the cost grows with
-- queue depth - worst exactly when the queue has backed up and throughput matters
-- most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the LIMIT can never
-- be satisfied, so unindexed every row is read before concluding that none qualify.
-- "RetryCount" > 0 is a deliberate SUPERSET of the sweep's own
-- "RetryCount" >= p_max_claim_attempts - the configured ceiling is never below 1 - so
-- this predicate needs no revision if that floor moves, and the index holds only rows
-- a device-lookup hold or a stale reclaim has touched - few or none on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,953 ms to 90 ms here and from 5,073 ms to 464 ms on SQL Server. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. They cost 40 MB and 8 KB
-- respectively at that depth, and 4.3% on the insert path, which retains ~25x
-- headroom over the feed either way.
CREATE INDEX "IX_Q_GenericFaultRecords_RecordCreationTimeUtc"
ON gda."Q_GenericFaultRecords" ("RecordCreationTimeUtc");

CREATE INDEX "IX_Q_GenericFaultRecords_Exhausted"
ON gda."Q_GenericFaultRecords" ("id")
WHERE "RetryCount" > 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_GenericFaultRecordsFail
CREATE TABLE gda."Q_GenericFaultRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Code" integer NOT NULL,
    "FaultStateActive" boolean NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    PRIMARY KEY ("id", "RecordCreationTimeUtc")
)
PARTITION BY RANGE ("RecordCreationTimeUtc");

-- The DEFAULT partition guarantees an INSERT can never fail for want of a
-- period partition. spManageDigPartitions creates the period partitions.
CREATE TABLE gda."Q_GenericFaultRecordsFail_default" PARTITION OF gda."Q_GenericFaultRecordsFail" DEFAULT;

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

-- Two indexes for the claim function. The PendingWork index above serves neither
-- statement: it is partial on "ProcessingStatus" = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that
-- predicate - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take p_batch_size rows, so the cost grows with
-- queue depth - worst exactly when the queue has backed up and throughput matters
-- most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the LIMIT can never
-- be satisfied, so unindexed every row is read before concluding that none qualify.
-- "RetryCount" > 0 is a deliberate SUPERSET of the sweep's own
-- "RetryCount" >= p_max_claim_attempts - the configured ceiling is never below 1 - so
-- this predicate needs no revision if that floor moves, and the index holds only rows
-- a device-lookup hold or a stale reclaim has touched - few or none on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,953 ms to 90 ms here and from 5,073 ms to 464 ms on SQL Server. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. They cost 40 MB and 8 KB
-- respectively at that depth, and 4.3% on the insert path, which retains ~25x
-- headroom over the feed either way.
CREATE INDEX "IX_Q_GenericStatusRecords_RecordCreationTimeUtc"
ON gda."Q_GenericStatusRecords" ("RecordCreationTimeUtc");

CREATE INDEX "IX_Q_GenericStatusRecords_Exhausted"
ON gda."Q_GenericStatusRecords" ("id")
WHERE "RetryCount" > 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_GenericStatusRecordsFail
CREATE TABLE gda."Q_GenericStatusRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Code" integer NOT NULL,
    "Value" integer NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    PRIMARY KEY ("id", "RecordCreationTimeUtc")
)
PARTITION BY RANGE ("RecordCreationTimeUtc");

-- The DEFAULT partition guarantees an INSERT can never fail for want of a
-- period partition. spManageDigPartitions creates the period partitions.
CREATE TABLE gda."Q_GenericStatusRecordsFail_default" PARTITION OF gda."Q_GenericStatusRecordsFail" DEFAULT;

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

-- Two indexes for the claim function. The PendingWork index above serves neither
-- statement: it is partial on "ProcessingStatus" = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that
-- predicate - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take p_batch_size rows, so the cost grows with
-- queue depth - worst exactly when the queue has backed up and throughput matters
-- most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the LIMIT can never
-- be satisfied, so unindexed every row is read before concluding that none qualify.
-- "RetryCount" > 0 is a deliberate SUPERSET of the sweep's own
-- "RetryCount" >= p_max_claim_attempts - the configured ceiling is never below 1 - so
-- this predicate needs no revision if that floor moves, and the index holds only rows
-- a device-lookup hold or a stale reclaim has touched - few or none on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,953 ms to 90 ms here and from 5,073 ms to 464 ms on SQL Server. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. They cost 40 MB and 8 KB
-- respectively at that depth, and 4.3% on the insert path, which retains ~25x
-- headroom over the feed either way.
CREATE INDEX "IX_Q_J1708FaultRecords_RecordCreationTimeUtc"
ON gda."Q_J1708FaultRecords" ("RecordCreationTimeUtc");

CREATE INDEX "IX_Q_J1708FaultRecords_Exhausted"
ON gda."Q_J1708FaultRecords" ("id")
WHERE "RetryCount" > 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_J1708FaultRecordsFail
CREATE TABLE gda."Q_J1708FaultRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY,
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
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    PRIMARY KEY ("id", "RecordCreationTimeUtc")
)
PARTITION BY RANGE ("RecordCreationTimeUtc");

-- The DEFAULT partition guarantees an INSERT can never fail for want of a
-- period partition. spManageDigPartitions creates the period partitions.
CREATE TABLE gda."Q_J1708FaultRecordsFail_default" PARTITION OF gda."Q_J1708FaultRecordsFail" DEFAULT;

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

-- Two indexes for the claim function. The PendingWork index above serves neither
-- statement: it is partial on "ProcessingStatus" = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that
-- predicate - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take p_batch_size rows, so the cost grows with
-- queue depth - worst exactly when the queue has backed up and throughput matters
-- most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the LIMIT can never
-- be satisfied, so unindexed every row is read before concluding that none qualify.
-- "RetryCount" > 0 is a deliberate SUPERSET of the sweep's own
-- "RetryCount" >= p_max_claim_attempts - the configured ceiling is never below 1 - so
-- this predicate needs no revision if that floor moves, and the index holds only rows
-- a device-lookup hold or a stale reclaim has touched - few or none on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,953 ms to 90 ms here and from 5,073 ms to 464 ms on SQL Server. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. They cost 40 MB and 8 KB
-- respectively at that depth, and 4.3% on the insert path, which retains ~25x
-- headroom over the feed either way.
CREATE INDEX "IX_Q_J1939FaultRecords_RecordCreationTimeUtc"
ON gda."Q_J1939FaultRecords" ("RecordCreationTimeUtc");

CREATE INDEX "IX_Q_J1939FaultRecords_Exhausted"
ON gda."Q_J1939FaultRecords" ("id")
WHERE "RetryCount" > 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_J1939FaultRecordsFail
CREATE TABLE gda."Q_J1939FaultRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY,
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
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    PRIMARY KEY ("id", "RecordCreationTimeUtc")
)
PARTITION BY RANGE ("RecordCreationTimeUtc");

-- The DEFAULT partition guarantees an INSERT can never fail for want of a
-- period partition. spManageDigPartitions creates the period partitions.
CREATE TABLE gda."Q_J1939FaultRecordsFail_default" PARTITION OF gda."Q_J1939FaultRecordsFail" DEFAULT;

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

-- Two indexes for the claim function. The PendingWork index above serves neither
-- statement: it is partial on "ProcessingStatus" = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that
-- predicate - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take p_batch_size rows, so the cost grows with
-- queue depth - worst exactly when the queue has backed up and throughput matters
-- most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the LIMIT can never
-- be satisfied, so unindexed every row is read before concluding that none qualify.
-- "RetryCount" > 0 is a deliberate SUPERSET of the sweep's own
-- "RetryCount" >= p_max_claim_attempts - the configured ceiling is never below 1 - so
-- this predicate needs no revision if that floor moves, and the index holds only rows
-- a device-lookup hold or a stale reclaim has touched - few or none on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,953 ms to 90 ms here and from 5,073 ms to 464 ms on SQL Server. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. They cost 40 MB and 8 KB
-- respectively at that depth, and 4.3% on the insert path, which retains ~25x
-- headroom over the feed either way.
CREATE INDEX "IX_Q_ObdiiFaultRecords_RecordCreationTimeUtc"
ON gda."Q_ObdiiFaultRecords" ("RecordCreationTimeUtc");

CREATE INDEX "IX_Q_ObdiiFaultRecords_Exhausted"
ON gda."Q_ObdiiFaultRecords" ("id")
WHERE "RetryCount" > 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_ObdiiFaultRecordsFail
CREATE TABLE gda."Q_ObdiiFaultRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Code" varchar(10) NOT NULL,
    "FaultStateActive" boolean NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    PRIMARY KEY ("id", "RecordCreationTimeUtc")
)
PARTITION BY RANGE ("RecordCreationTimeUtc");

-- The DEFAULT partition guarantees an INSERT can never fail for want of a
-- period partition. spManageDigPartitions creates the period partitions.
CREATE TABLE gda."Q_ObdiiFaultRecordsFail_default" PARTITION OF gda."Q_ObdiiFaultRecordsFail" DEFAULT;

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

-- Two indexes for the claim function. The PendingWork index above serves neither
-- statement: it is partial on "ProcessingStatus" = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that
-- predicate - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take p_batch_size rows, so the cost grows with
-- queue depth - worst exactly when the queue has backed up and throughput matters
-- most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the LIMIT can never
-- be satisfied, so unindexed every row is read before concluding that none qualify.
-- "RetryCount" > 0 is a deliberate SUPERSET of the sweep's own
-- "RetryCount" >= p_max_claim_attempts - the configured ceiling is never below 1 - so
-- this predicate needs no revision if that floor moves, and the index holds only rows
-- a device-lookup hold or a stale reclaim has touched - few or none on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,953 ms to 90 ms here and from 5,073 ms to 464 ms on SQL Server. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. They cost 40 MB and 8 KB
-- respectively at that depth, and 4.3% on the insert path, which retains ~25x
-- headroom over the feed either way.
CREATE INDEX "IX_Q_VinRecords_RecordCreationTimeUtc"
ON gda."Q_VinRecords" ("RecordCreationTimeUtc");

CREATE INDEX "IX_Q_VinRecords_Exhausted"
ON gda."Q_VinRecords" ("id")
WHERE "RetryCount" > 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_VinRecordsFail
CREATE TABLE gda."Q_VinRecordsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY,
    "OriginalQueueId" bigint NOT NULL,
    "ThirdPartyId" varchar(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "VehicleIdentificationNumber" varchar(17) NOT NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NOT NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    PRIMARY KEY ("id", "RecordCreationTimeUtc")
)
PARTITION BY RANGE ("RecordCreationTimeUtc");

-- The DEFAULT partition guarantees an INSERT can never fail for want of a
-- period partition. spManageDigPartitions creates the period partitions.
CREATE TABLE gda."Q_VinRecordsFail_default" PARTITION OF gda."Q_VinRecordsFail" DEFAULT;

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

-- Supports retention. This table cannot be partitioned - its UNIQUE index on
-- GeotabGUID alone is the dedupe guarantee the InvalidRecordRetrievalService
-- relies on, and PostgreSQL requires the partition key in every unique index on
-- a partitioned table - so old records are removed by a batched DELETE on
-- RecordCreationTimeUtc instead. Without this index each batch would scan the
-- whole of the table it is trying to shrink.
CREATE INDEX "IX_DIGInvalidRecords_RecordCreationTimeUtc"
ON gda."DIGInvalidRecords" ("RecordCreationTimeUtc");


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
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is returned by the claiming UPDATE's RETURNING clause, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the now() value just written, and two
--     claims landing on the same value each received BOTH batches - so the atomic pop
--     was stated here but not delivered. See the note in the body.
-- Parameters:
--   p_batch_size: Maximum number of records to claim in a single batch.
--   p_stale_threshold_minutes: Minutes after which an "in progress" record is
--                              considered stale and eligible for reclaim.
--   p_max_claim_attempts: Claims a record may accumulate before it is treated as
--                         poison and moved to Q_ProvisionDevicesFail. NULL reclaims
--                         indefinitely, which is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_ProvisionDevices records, in RecordCreationTimeUtc, id order.
CREATE OR REPLACE FUNCTION gda."spClaimQProvisionDevicesBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer,
    p_max_claim_attempts integer
)
RETURNS SETOF gda."Q_ProvisionDevices"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- "RecordCreationTimeUtc" is NOT carried across. It is the fail table's partition
    -- key and defaults to now, so the row lands in the current partition and is retired
    -- on the retention schedule like every other failure.
    -- Bounded by p_batch_size, like every other statement in this function. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to p_batch_size - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. DELETE accepts no LIMIT, so the bound is a subquery on the primary key, and that
    -- subquery takes its rows ORDER BY "id", then FOR UPDATE SKIP LOCKED.
    --
    -- Both of those are for LOCK ORDER, not for choosing which rows qualify - every qualifying
    -- row has the same destination, so the ordering key is simply the primary key, which is also
    -- the key of the queue's partial _Exhausted index. (The claim below takes its rows in its own
    -- order; it is SKIP LOCKED in both statements, not a shared order, that keeps the sweep and
    -- the claim from waiting on each other.) Without them two concurrent sweeps take an
    -- overlapping set in
    -- different orders and block on each other's rows instead of skipping them, which is a
    -- deadlock cycle: measured here in five of eight runs at four and eight claimers, and only
    -- ever with the sweep actually deleting. Deadlocks are retried by DatabaseResilienceHelper,
    -- so they present as latency rather than as errors, and the larger cost is in fact
    -- throughput - eight colliding sweeps banked about a sixth of a dense backlog per pass
    -- against all of it once each takes a distinct set, and a backed-up queue is precisely when
    -- this statement matters. Nothing is lost to SKIP LOCKED: a row another sweep is holding is
    -- left for the next pass, which is the same bargain the bound above already makes. The SQL
    -- Server sibling needed no such hint while its sweep scanned the queue - raced at both
    -- concurrencies it showed no deadlock - but it takes READPAST now that the sweep seeks its
    -- _Exhausted index instead; see the comment there.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply "ProcessingStatus" = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with "RetryCount" + 1;
    -- skipping every claimed row would mean it is never swept, which is the infinite retry this
    -- ceiling exists to end. Mirroring the claim's own predicate removes abandoned rows and
    -- protects live ones.
    --
    -- The literal "RetryCount" > 0 changes no result - the ceiling is never below 1. It is there
    -- so the planner can use the queue's partial _Exhausted index (WHERE "RetryCount" > 0)
    -- whatever plan it caches for this function: a generic plan cannot prove that
    -- "RetryCount" >= p_max_claim_attempts implies it for every parameter value, and would then
    -- read the whole queue before every claim - in the healthy case, where nothing qualifies,
    -- most of all.
    IF p_max_claim_attempts IS NOT NULL THEN
        WITH exhausted AS (
            DELETE FROM gda."Q_ProvisionDevices" q
            WHERE q."id" IN (
                SELECT q2."id"
                FROM gda."Q_ProvisionDevices" q2
                WHERE q2."RetryCount" >= p_max_claim_attempts
                  AND q2."RetryCount" > 0
                  AND (q2."ProcessingStatus" = 0
                       OR q2."ProcessingStartTimeUtc" < v_stale_threshold)
                ORDER BY q2."id"
                LIMIT p_batch_size
                FOR UPDATE SKIP LOCKED
            )
            RETURNING q.*
        )
        INSERT INTO gda."Q_ProvisionDevicesFail" (
            "OriginalQueueId",
            "ThirdPartyId", "ErpNo", "HardwareId", "ProductId", "PromoCode", "SubPlan",
            "OriginalRecordLastChangedUtc", "FailureReason")
        SELECT e."id",
               e."ThirdPartyId", e."ErpNo", e."HardwareId", e."ProductId",
               e."PromoCode", e."SubPlan",
               e."RecordLastChangedUtc",
               'Exceeded ' || p_max_claim_attempts || ' claim attempts.'
        FROM exhausted e;
    END IF;

    -- The batch this call claimed is returned by RETURNING, not by re-reading the queue
    -- for ProcessingStartTimeUtc = v_processing_start_time_utc. That re-read is NOT
    -- safe: two claims that land on the same clock value each match BOTH batches, and
    -- now() is transaction-start time, so two claims sharing a transaction are
    -- guaranteed to. The column is still stamped, because the stale-reclaim predicate
    -- above reads it; it is simply no longer how the batch is identified.
    --
    -- The ORDER BY on the outer SELECT is what holds the "in RecordCreationTimeUtc, id
    -- order" contract; the ORDER BY inside the claimed CTE chooses only WHICH rows are
    -- claimed. Both carry "id" as a tiebreaker, because "RecordCreationTimeUtc" is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a LIMIT takes, and the order the
    -- batch comes back in, could differ between two runs of this function.
    RETURN QUERY
    WITH claimed AS (
        SELECT q2."id"
        FROM gda."Q_ProvisionDevices" q2
        WHERE q2."ProcessingStatus" = 0
           OR (q2."ProcessingStatus" = 1 AND q2."ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY q2."RecordCreationTimeUtc", q2."id"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    ),
    updated AS (
        UPDATE gda."Q_ProvisionDevices" q
        SET
            "ProcessingStatus" = 1,
            "ProcessingStartTimeUtc" = v_processing_start_time_utc,
            "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
        FROM claimed c
        WHERE q."id" = c."id"
        RETURNING q.*
    )
    SELECT * FROM updated ORDER BY "RecordCreationTimeUtc", "id";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQGpsRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_GpsRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is returned by the claiming UPDATE's RETURNING clause, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the now() value just written, and two
--     claims landing on the same value each received BOTH batches - so the atomic pop
--     was stated here but not delivered. See the note in the body.
-- Parameters:
--   p_batch_size: Maximum number of records to claim in a single batch.
--   p_stale_threshold_minutes: Minutes after which an "in progress" record is
--                              considered stale and eligible for reclaim.
--   p_max_claim_attempts: Claims a record may accumulate before it is treated as
--                         poison and moved to Q_GpsRecordsFail. NULL reclaims
--                         indefinitely, which is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_GpsRecords records, in RecordCreationTimeUtc, id order.
CREATE OR REPLACE FUNCTION gda."spClaimQGpsRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer,
    p_max_claim_attempts integer
)
RETURNS SETOF gda."Q_GpsRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- "RecordCreationTimeUtc" is NOT carried across. It is the fail table's partition
    -- key and defaults to now, so the row lands in the current partition and is retired
    -- on the retention schedule like every other failure.
    -- Bounded by p_batch_size, like every other statement in this function. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to p_batch_size - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. DELETE accepts no LIMIT, so the bound is a subquery on the primary key, and that
    -- subquery takes its rows ORDER BY "id", then FOR UPDATE SKIP LOCKED.
    --
    -- Both of those are for LOCK ORDER, not for choosing which rows qualify - every qualifying
    -- row has the same destination, so the ordering key is simply the primary key, which is also
    -- the key of the queue's partial _Exhausted index. (The claim below takes its rows in its own
    -- order; it is SKIP LOCKED in both statements, not a shared order, that keeps the sweep and
    -- the claim from waiting on each other.) Without them two concurrent sweeps take an
    -- overlapping set in
    -- different orders and block on each other's rows instead of skipping them, which is a
    -- deadlock cycle: measured here in five of eight runs at four and eight claimers, and only
    -- ever with the sweep actually deleting. Deadlocks are retried by DatabaseResilienceHelper,
    -- so they present as latency rather than as errors, and the larger cost is in fact
    -- throughput - eight colliding sweeps banked about a sixth of a dense backlog per pass
    -- against all of it once each takes a distinct set, and a backed-up queue is precisely when
    -- this statement matters. Nothing is lost to SKIP LOCKED: a row another sweep is holding is
    -- left for the next pass, which is the same bargain the bound above already makes. The SQL
    -- Server sibling needed no such hint while its sweep scanned the queue - raced at both
    -- concurrencies it showed no deadlock - but it takes READPAST now that the sweep seeks its
    -- _Exhausted index instead; see the comment there.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply "ProcessingStatus" = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with "RetryCount" + 1;
    -- skipping every claimed row would mean it is never swept, which is the infinite retry this
    -- ceiling exists to end. Mirroring the claim's own predicate removes abandoned rows and
    -- protects live ones.
    --
    -- The literal "RetryCount" > 0 changes no result - the ceiling is never below 1. It is there
    -- so the planner can use the queue's partial _Exhausted index (WHERE "RetryCount" > 0)
    -- whatever plan it caches for this function: a generic plan cannot prove that
    -- "RetryCount" >= p_max_claim_attempts implies it for every parameter value, and would then
    -- read the whole queue before every claim - in the healthy case, where nothing qualifies,
    -- most of all.
    IF p_max_claim_attempts IS NOT NULL THEN
        WITH exhausted AS (
            DELETE FROM gda."Q_GpsRecords" q
            WHERE q."id" IN (
                SELECT q2."id"
                FROM gda."Q_GpsRecords" q2
                WHERE q2."RetryCount" >= p_max_claim_attempts
                  AND q2."RetryCount" > 0
                  AND (q2."ProcessingStatus" = 0
                       OR q2."ProcessingStartTimeUtc" < v_stale_threshold)
                ORDER BY q2."id"
                LIMIT p_batch_size
                FOR UPDATE SKIP LOCKED
            )
            RETURNING q.*
        )
        INSERT INTO gda."Q_GpsRecordsFail" (
            "OriginalQueueId",
            "ThirdPartyId", "DateTime", "Latitude", "Longitude", "Speed", "IsGpsValid",
            "IsIgnitionOn", "IsAuxiliary1On", "IsAuxiliary2On", "IsAuxiliary3On",
            "IsAuxiliary4On", "IsAuxiliary5On", "IsAuxiliary6On", "IsAuxiliary7On",
            "IsAuxiliary8On",
            "OriginalRecordLastChangedUtc", "FailureReason")
        SELECT e."id",
               e."ThirdPartyId", e."DateTime", e."Latitude", e."Longitude", e."Speed",
               e."IsGpsValid", e."IsIgnitionOn", e."IsAuxiliary1On",
               e."IsAuxiliary2On", e."IsAuxiliary3On", e."IsAuxiliary4On",
               e."IsAuxiliary5On", e."IsAuxiliary6On", e."IsAuxiliary7On",
               e."IsAuxiliary8On",
               e."RecordLastChangedUtc",
               'Exceeded ' || p_max_claim_attempts || ' claim attempts.'
        FROM exhausted e;
    END IF;

    -- The batch this call claimed is returned by RETURNING, not by re-reading the queue
    -- for ProcessingStartTimeUtc = v_processing_start_time_utc. That re-read is NOT
    -- safe: two claims that land on the same clock value each match BOTH batches, and
    -- now() is transaction-start time, so two claims sharing a transaction are
    -- guaranteed to. The column is still stamped, because the stale-reclaim predicate
    -- above reads it; it is simply no longer how the batch is identified.
    --
    -- The ORDER BY on the outer SELECT is what holds the "in RecordCreationTimeUtc, id
    -- order" contract; the ORDER BY inside the claimed CTE chooses only WHICH rows are
    -- claimed. Both carry "id" as a tiebreaker, because "RecordCreationTimeUtc" is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a LIMIT takes, and the order the
    -- batch comes back in, could differ between two runs of this function.
    RETURN QUERY
    WITH claimed AS (
        SELECT q2."id"
        FROM gda."Q_GpsRecords" q2
        WHERE q2."ProcessingStatus" = 0
           OR (q2."ProcessingStatus" = 1 AND q2."ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY q2."RecordCreationTimeUtc", q2."id"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    ),
    updated AS (
        UPDATE gda."Q_GpsRecords" q
        SET
            "ProcessingStatus" = 1,
            "ProcessingStartTimeUtc" = v_processing_start_time_utc,
            "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
        FROM claimed c
        WHERE q."id" = c."id"
        RETURNING q.*
    )
    SELECT * FROM updated ORDER BY "RecordCreationTimeUtc", "id";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQAccelerationRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_AccelerationRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is returned by the claiming UPDATE's RETURNING clause, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the now() value just written, and two
--     claims landing on the same value each received BOTH batches - so the atomic pop
--     was stated here but not delivered. See the note in the body.
-- Parameters:
--   p_batch_size: Maximum number of records to claim in a single batch.
--   p_stale_threshold_minutes: Minutes after which an "in progress" record is
--                              considered stale and eligible for reclaim.
--   p_max_claim_attempts: Claims a record may accumulate before it is treated as
--                         poison and moved to Q_AccelerationRecordsFail. NULL reclaims
--                         indefinitely, which is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_AccelerationRecords records, in RecordCreationTimeUtc, id order.
CREATE OR REPLACE FUNCTION gda."spClaimQAccelerationRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer,
    p_max_claim_attempts integer
)
RETURNS SETOF gda."Q_AccelerationRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- "RecordCreationTimeUtc" is NOT carried across. It is the fail table's partition
    -- key and defaults to now, so the row lands in the current partition and is retired
    -- on the retention schedule like every other failure.
    -- Bounded by p_batch_size, like every other statement in this function. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to p_batch_size - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. DELETE accepts no LIMIT, so the bound is a subquery on the primary key, and that
    -- subquery takes its rows ORDER BY "id", then FOR UPDATE SKIP LOCKED.
    --
    -- Both of those are for LOCK ORDER, not for choosing which rows qualify - every qualifying
    -- row has the same destination, so the ordering key is simply the primary key, which is also
    -- the key of the queue's partial _Exhausted index. (The claim below takes its rows in its own
    -- order; it is SKIP LOCKED in both statements, not a shared order, that keeps the sweep and
    -- the claim from waiting on each other.) Without them two concurrent sweeps take an
    -- overlapping set in
    -- different orders and block on each other's rows instead of skipping them, which is a
    -- deadlock cycle: measured here in five of eight runs at four and eight claimers, and only
    -- ever with the sweep actually deleting. Deadlocks are retried by DatabaseResilienceHelper,
    -- so they present as latency rather than as errors, and the larger cost is in fact
    -- throughput - eight colliding sweeps banked about a sixth of a dense backlog per pass
    -- against all of it once each takes a distinct set, and a backed-up queue is precisely when
    -- this statement matters. Nothing is lost to SKIP LOCKED: a row another sweep is holding is
    -- left for the next pass, which is the same bargain the bound above already makes. The SQL
    -- Server sibling needed no such hint while its sweep scanned the queue - raced at both
    -- concurrencies it showed no deadlock - but it takes READPAST now that the sweep seeks its
    -- _Exhausted index instead; see the comment there.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply "ProcessingStatus" = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with "RetryCount" + 1;
    -- skipping every claimed row would mean it is never swept, which is the infinite retry this
    -- ceiling exists to end. Mirroring the claim's own predicate removes abandoned rows and
    -- protects live ones.
    --
    -- The literal "RetryCount" > 0 changes no result - the ceiling is never below 1. It is there
    -- so the planner can use the queue's partial _Exhausted index (WHERE "RetryCount" > 0)
    -- whatever plan it caches for this function: a generic plan cannot prove that
    -- "RetryCount" >= p_max_claim_attempts implies it for every parameter value, and would then
    -- read the whole queue before every claim - in the healthy case, where nothing qualifies,
    -- most of all.
    IF p_max_claim_attempts IS NOT NULL THEN
        WITH exhausted AS (
            DELETE FROM gda."Q_AccelerationRecords" q
            WHERE q."id" IN (
                SELECT q2."id"
                FROM gda."Q_AccelerationRecords" q2
                WHERE q2."RetryCount" >= p_max_claim_attempts
                  AND q2."RetryCount" > 0
                  AND (q2."ProcessingStatus" = 0
                       OR q2."ProcessingStartTimeUtc" < v_stale_threshold)
                ORDER BY q2."id"
                LIMIT p_batch_size
                FOR UPDATE SKIP LOCKED
            )
            RETURNING q.*
        )
        INSERT INTO gda."Q_AccelerationRecordsFail" (
            "OriginalQueueId",
            "ThirdPartyId", "DateTime", "X", "Y", "Z",
            "OriginalRecordLastChangedUtc", "FailureReason")
        SELECT e."id",
               e."ThirdPartyId", e."DateTime", e."X", e."Y", e."Z",
               e."RecordLastChangedUtc",
               'Exceeded ' || p_max_claim_attempts || ' claim attempts.'
        FROM exhausted e;
    END IF;

    -- The batch this call claimed is returned by RETURNING, not by re-reading the queue
    -- for ProcessingStartTimeUtc = v_processing_start_time_utc. That re-read is NOT
    -- safe: two claims that land on the same clock value each match BOTH batches, and
    -- now() is transaction-start time, so two claims sharing a transaction are
    -- guaranteed to. The column is still stamped, because the stale-reclaim predicate
    -- above reads it; it is simply no longer how the batch is identified.
    --
    -- The ORDER BY on the outer SELECT is what holds the "in RecordCreationTimeUtc, id
    -- order" contract; the ORDER BY inside the claimed CTE chooses only WHICH rows are
    -- claimed. Both carry "id" as a tiebreaker, because "RecordCreationTimeUtc" is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a LIMIT takes, and the order the
    -- batch comes back in, could differ between two runs of this function.
    RETURN QUERY
    WITH claimed AS (
        SELECT q2."id"
        FROM gda."Q_AccelerationRecords" q2
        WHERE q2."ProcessingStatus" = 0
           OR (q2."ProcessingStatus" = 1 AND q2."ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY q2."RecordCreationTimeUtc", q2."id"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    ),
    updated AS (
        UPDATE gda."Q_AccelerationRecords" q
        SET
            "ProcessingStatus" = 1,
            "ProcessingStartTimeUtc" = v_processing_start_time_utc,
            "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
        FROM claimed c
        WHERE q."id" = c."id"
        RETURNING q.*
    )
    SELECT * FROM updated ORDER BY "RecordCreationTimeUtc", "id";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQBinaryRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_BinaryRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is returned by the claiming UPDATE's RETURNING clause, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the now() value just written, and two
--     claims landing on the same value each received BOTH batches - so the atomic pop
--     was stated here but not delivered. See the note in the body.
-- Parameters:
--   p_batch_size: Maximum number of records to claim in a single batch.
--   p_stale_threshold_minutes: Minutes after which an "in progress" record is
--                              considered stale and eligible for reclaim.
--   p_max_claim_attempts: Claims a record may accumulate before it is treated as
--                         poison and moved to Q_BinaryRecordsFail. NULL reclaims
--                         indefinitely, which is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_BinaryRecords records, in RecordCreationTimeUtc, id order.
CREATE OR REPLACE FUNCTION gda."spClaimQBinaryRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer,
    p_max_claim_attempts integer
)
RETURNS SETOF gda."Q_BinaryRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- "RecordCreationTimeUtc" is NOT carried across. It is the fail table's partition
    -- key and defaults to now, so the row lands in the current partition and is retired
    -- on the retention schedule like every other failure.
    -- Bounded by p_batch_size, like every other statement in this function. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to p_batch_size - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. DELETE accepts no LIMIT, so the bound is a subquery on the primary key, and that
    -- subquery takes its rows ORDER BY "id", then FOR UPDATE SKIP LOCKED.
    --
    -- Both of those are for LOCK ORDER, not for choosing which rows qualify - every qualifying
    -- row has the same destination, so the ordering key is simply the primary key, which is also
    -- the key of the queue's partial _Exhausted index. (The claim below takes its rows in its own
    -- order; it is SKIP LOCKED in both statements, not a shared order, that keeps the sweep and
    -- the claim from waiting on each other.) Without them two concurrent sweeps take an
    -- overlapping set in
    -- different orders and block on each other's rows instead of skipping them, which is a
    -- deadlock cycle: measured here in five of eight runs at four and eight claimers, and only
    -- ever with the sweep actually deleting. Deadlocks are retried by DatabaseResilienceHelper,
    -- so they present as latency rather than as errors, and the larger cost is in fact
    -- throughput - eight colliding sweeps banked about a sixth of a dense backlog per pass
    -- against all of it once each takes a distinct set, and a backed-up queue is precisely when
    -- this statement matters. Nothing is lost to SKIP LOCKED: a row another sweep is holding is
    -- left for the next pass, which is the same bargain the bound above already makes. The SQL
    -- Server sibling needed no such hint while its sweep scanned the queue - raced at both
    -- concurrencies it showed no deadlock - but it takes READPAST now that the sweep seeks its
    -- _Exhausted index instead; see the comment there.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply "ProcessingStatus" = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with "RetryCount" + 1;
    -- skipping every claimed row would mean it is never swept, which is the infinite retry this
    -- ceiling exists to end. Mirroring the claim's own predicate removes abandoned rows and
    -- protects live ones.
    --
    -- The literal "RetryCount" > 0 changes no result - the ceiling is never below 1. It is there
    -- so the planner can use the queue's partial _Exhausted index (WHERE "RetryCount" > 0)
    -- whatever plan it caches for this function: a generic plan cannot prove that
    -- "RetryCount" >= p_max_claim_attempts implies it for every parameter value, and would then
    -- read the whole queue before every claim - in the healthy case, where nothing qualifies,
    -- most of all.
    IF p_max_claim_attempts IS NOT NULL THEN
        WITH exhausted AS (
            DELETE FROM gda."Q_BinaryRecords" q
            WHERE q."id" IN (
                SELECT q2."id"
                FROM gda."Q_BinaryRecords" q2
                WHERE q2."RetryCount" >= p_max_claim_attempts
                  AND q2."RetryCount" > 0
                  AND (q2."ProcessingStatus" = 0
                       OR q2."ProcessingStartTimeUtc" < v_stale_threshold)
                ORDER BY q2."id"
                LIMIT p_batch_size
                FOR UPDATE SKIP LOCKED
            )
            RETURNING q.*
        )
        INSERT INTO gda."Q_BinaryRecordsFail" (
            "OriginalQueueId",
            "ThirdPartyId", "DateTime", "Data",
            "OriginalRecordLastChangedUtc", "FailureReason")
        SELECT e."id",
               e."ThirdPartyId", e."DateTime", e."Data",
               e."RecordLastChangedUtc",
               'Exceeded ' || p_max_claim_attempts || ' claim attempts.'
        FROM exhausted e;
    END IF;

    -- The batch this call claimed is returned by RETURNING, not by re-reading the queue
    -- for ProcessingStartTimeUtc = v_processing_start_time_utc. That re-read is NOT
    -- safe: two claims that land on the same clock value each match BOTH batches, and
    -- now() is transaction-start time, so two claims sharing a transaction are
    -- guaranteed to. The column is still stamped, because the stale-reclaim predicate
    -- above reads it; it is simply no longer how the batch is identified.
    --
    -- The ORDER BY on the outer SELECT is what holds the "in RecordCreationTimeUtc, id
    -- order" contract; the ORDER BY inside the claimed CTE chooses only WHICH rows are
    -- claimed. Both carry "id" as a tiebreaker, because "RecordCreationTimeUtc" is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a LIMIT takes, and the order the
    -- batch comes back in, could differ between two runs of this function.
    RETURN QUERY
    WITH claimed AS (
        SELECT q2."id"
        FROM gda."Q_BinaryRecords" q2
        WHERE q2."ProcessingStatus" = 0
           OR (q2."ProcessingStatus" = 1 AND q2."ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY q2."RecordCreationTimeUtc", q2."id"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    ),
    updated AS (
        UPDATE gda."Q_BinaryRecords" q
        SET
            "ProcessingStatus" = 1,
            "ProcessingStartTimeUtc" = v_processing_start_time_utc,
            "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
        FROM claimed c
        WHERE q."id" = c."id"
        RETURNING q.*
    )
    SELECT * FROM updated ORDER BY "RecordCreationTimeUtc", "id";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQBluetoothRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_BluetoothRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is returned by the claiming UPDATE's RETURNING clause, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the now() value just written, and two
--     claims landing on the same value each received BOTH batches - so the atomic pop
--     was stated here but not delivered. See the note in the body.
-- Parameters:
--   p_batch_size: Maximum number of records to claim in a single batch.
--   p_stale_threshold_minutes: Minutes after which an "in progress" record is
--                              considered stale and eligible for reclaim.
--   p_max_claim_attempts: Claims a record may accumulate before it is treated as
--                         poison and moved to Q_BluetoothRecordsFail. NULL reclaims
--                         indefinitely, which is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_BluetoothRecords records, in RecordCreationTimeUtc, id order.
CREATE OR REPLACE FUNCTION gda."spClaimQBluetoothRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer,
    p_max_claim_attempts integer
)
RETURNS SETOF gda."Q_BluetoothRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- "RecordCreationTimeUtc" is NOT carried across. It is the fail table's partition
    -- key and defaults to now, so the row lands in the current partition and is retired
    -- on the retention schedule like every other failure.
    -- Bounded by p_batch_size, like every other statement in this function. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to p_batch_size - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. DELETE accepts no LIMIT, so the bound is a subquery on the primary key, and that
    -- subquery takes its rows ORDER BY "id", then FOR UPDATE SKIP LOCKED.
    --
    -- Both of those are for LOCK ORDER, not for choosing which rows qualify - every qualifying
    -- row has the same destination, so the ordering key is simply the primary key, which is also
    -- the key of the queue's partial _Exhausted index. (The claim below takes its rows in its own
    -- order; it is SKIP LOCKED in both statements, not a shared order, that keeps the sweep and
    -- the claim from waiting on each other.) Without them two concurrent sweeps take an
    -- overlapping set in
    -- different orders and block on each other's rows instead of skipping them, which is a
    -- deadlock cycle: measured here in five of eight runs at four and eight claimers, and only
    -- ever with the sweep actually deleting. Deadlocks are retried by DatabaseResilienceHelper,
    -- so they present as latency rather than as errors, and the larger cost is in fact
    -- throughput - eight colliding sweeps banked about a sixth of a dense backlog per pass
    -- against all of it once each takes a distinct set, and a backed-up queue is precisely when
    -- this statement matters. Nothing is lost to SKIP LOCKED: a row another sweep is holding is
    -- left for the next pass, which is the same bargain the bound above already makes. The SQL
    -- Server sibling needed no such hint while its sweep scanned the queue - raced at both
    -- concurrencies it showed no deadlock - but it takes READPAST now that the sweep seeks its
    -- _Exhausted index instead; see the comment there.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply "ProcessingStatus" = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with "RetryCount" + 1;
    -- skipping every claimed row would mean it is never swept, which is the infinite retry this
    -- ceiling exists to end. Mirroring the claim's own predicate removes abandoned rows and
    -- protects live ones.
    --
    -- The literal "RetryCount" > 0 changes no result - the ceiling is never below 1. It is there
    -- so the planner can use the queue's partial _Exhausted index (WHERE "RetryCount" > 0)
    -- whatever plan it caches for this function: a generic plan cannot prove that
    -- "RetryCount" >= p_max_claim_attempts implies it for every parameter value, and would then
    -- read the whole queue before every claim - in the healthy case, where nothing qualifies,
    -- most of all.
    IF p_max_claim_attempts IS NOT NULL THEN
        WITH exhausted AS (
            DELETE FROM gda."Q_BluetoothRecords" q
            WHERE q."id" IN (
                SELECT q2."id"
                FROM gda."Q_BluetoothRecords" q2
                WHERE q2."RetryCount" >= p_max_claim_attempts
                  AND q2."RetryCount" > 0
                  AND (q2."ProcessingStatus" = 0
                       OR q2."ProcessingStartTimeUtc" < v_stale_threshold)
                ORDER BY q2."id"
                LIMIT p_batch_size
                FOR UPDATE SKIP LOCKED
            )
            RETURNING q.*
        )
        INSERT INTO gda."Q_BluetoothRecordsFail" (
            "OriginalQueueId",
            "ThirdPartyId", "DateTime", "Address", "Data", "DataType",
            "OriginalRecordLastChangedUtc", "FailureReason")
        SELECT e."id",
               e."ThirdPartyId", e."DateTime", e."Address", e."Data", e."DataType",
               e."RecordLastChangedUtc",
               'Exceeded ' || p_max_claim_attempts || ' claim attempts.'
        FROM exhausted e;
    END IF;

    -- The batch this call claimed is returned by RETURNING, not by re-reading the queue
    -- for ProcessingStartTimeUtc = v_processing_start_time_utc. That re-read is NOT
    -- safe: two claims that land on the same clock value each match BOTH batches, and
    -- now() is transaction-start time, so two claims sharing a transaction are
    -- guaranteed to. The column is still stamped, because the stale-reclaim predicate
    -- above reads it; it is simply no longer how the batch is identified.
    --
    -- The ORDER BY on the outer SELECT is what holds the "in RecordCreationTimeUtc, id
    -- order" contract; the ORDER BY inside the claimed CTE chooses only WHICH rows are
    -- claimed. Both carry "id" as a tiebreaker, because "RecordCreationTimeUtc" is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a LIMIT takes, and the order the
    -- batch comes back in, could differ between two runs of this function.
    RETURN QUERY
    WITH claimed AS (
        SELECT q2."id"
        FROM gda."Q_BluetoothRecords" q2
        WHERE q2."ProcessingStatus" = 0
           OR (q2."ProcessingStatus" = 1 AND q2."ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY q2."RecordCreationTimeUtc", q2."id"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    ),
    updated AS (
        UPDATE gda."Q_BluetoothRecords" q
        SET
            "ProcessingStatus" = 1,
            "ProcessingStartTimeUtc" = v_processing_start_time_utc,
            "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
        FROM claimed c
        WHERE q."id" = c."id"
        RETURNING q.*
    )
    SELECT * FROM updated ORDER BY "RecordCreationTimeUtc", "id";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQDriverChangeRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_DriverChangeRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is returned by the claiming UPDATE's RETURNING clause, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the now() value just written, and two
--     claims landing on the same value each received BOTH batches - so the atomic pop
--     was stated here but not delivered. See the note in the body.
-- Parameters:
--   p_batch_size: Maximum number of records to claim in a single batch.
--   p_stale_threshold_minutes: Minutes after which an "in progress" record is
--                              considered stale and eligible for reclaim.
--   p_max_claim_attempts: Claims a record may accumulate before it is treated as
--                         poison and moved to Q_DriverChangeRecordsFail. NULL reclaims
--                         indefinitely, which is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_DriverChangeRecords records, in RecordCreationTimeUtc, id order.
CREATE OR REPLACE FUNCTION gda."spClaimQDriverChangeRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer,
    p_max_claim_attempts integer
)
RETURNS SETOF gda."Q_DriverChangeRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- "RecordCreationTimeUtc" is NOT carried across. It is the fail table's partition
    -- key and defaults to now, so the row lands in the current partition and is retired
    -- on the retention schedule like every other failure.
    -- Bounded by p_batch_size, like every other statement in this function. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to p_batch_size - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. DELETE accepts no LIMIT, so the bound is a subquery on the primary key, and that
    -- subquery takes its rows ORDER BY "id", then FOR UPDATE SKIP LOCKED.
    --
    -- Both of those are for LOCK ORDER, not for choosing which rows qualify - every qualifying
    -- row has the same destination, so the ordering key is simply the primary key, which is also
    -- the key of the queue's partial _Exhausted index. (The claim below takes its rows in its own
    -- order; it is SKIP LOCKED in both statements, not a shared order, that keeps the sweep and
    -- the claim from waiting on each other.) Without them two concurrent sweeps take an
    -- overlapping set in
    -- different orders and block on each other's rows instead of skipping them, which is a
    -- deadlock cycle: measured here in five of eight runs at four and eight claimers, and only
    -- ever with the sweep actually deleting. Deadlocks are retried by DatabaseResilienceHelper,
    -- so they present as latency rather than as errors, and the larger cost is in fact
    -- throughput - eight colliding sweeps banked about a sixth of a dense backlog per pass
    -- against all of it once each takes a distinct set, and a backed-up queue is precisely when
    -- this statement matters. Nothing is lost to SKIP LOCKED: a row another sweep is holding is
    -- left for the next pass, which is the same bargain the bound above already makes. The SQL
    -- Server sibling needed no such hint while its sweep scanned the queue - raced at both
    -- concurrencies it showed no deadlock - but it takes READPAST now that the sweep seeks its
    -- _Exhausted index instead; see the comment there.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply "ProcessingStatus" = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with "RetryCount" + 1;
    -- skipping every claimed row would mean it is never swept, which is the infinite retry this
    -- ceiling exists to end. Mirroring the claim's own predicate removes abandoned rows and
    -- protects live ones.
    --
    -- The literal "RetryCount" > 0 changes no result - the ceiling is never below 1. It is there
    -- so the planner can use the queue's partial _Exhausted index (WHERE "RetryCount" > 0)
    -- whatever plan it caches for this function: a generic plan cannot prove that
    -- "RetryCount" >= p_max_claim_attempts implies it for every parameter value, and would then
    -- read the whole queue before every claim - in the healthy case, where nothing qualifies,
    -- most of all.
    IF p_max_claim_attempts IS NOT NULL THEN
        WITH exhausted AS (
            DELETE FROM gda."Q_DriverChangeRecords" q
            WHERE q."id" IN (
                SELECT q2."id"
                FROM gda."Q_DriverChangeRecords" q2
                WHERE q2."RetryCount" >= p_max_claim_attempts
                  AND q2."RetryCount" > 0
                  AND (q2."ProcessingStatus" = 0
                       OR q2."ProcessingStartTimeUtc" < v_stale_threshold)
                ORDER BY q2."id"
                LIMIT p_batch_size
                FOR UPDATE SKIP LOCKED
            )
            RETURNING q.*
        )
        INSERT INTO gda."Q_DriverChangeRecordsFail" (
            "OriginalQueueId",
            "ThirdPartyId", "DateTime", "KeyType", "DriverId",
            "OriginalRecordLastChangedUtc", "FailureReason")
        SELECT e."id",
               e."ThirdPartyId", e."DateTime", e."KeyType", e."DriverId",
               e."RecordLastChangedUtc",
               'Exceeded ' || p_max_claim_attempts || ' claim attempts.'
        FROM exhausted e;
    END IF;

    -- The batch this call claimed is returned by RETURNING, not by re-reading the queue
    -- for ProcessingStartTimeUtc = v_processing_start_time_utc. That re-read is NOT
    -- safe: two claims that land on the same clock value each match BOTH batches, and
    -- now() is transaction-start time, so two claims sharing a transaction are
    -- guaranteed to. The column is still stamped, because the stale-reclaim predicate
    -- above reads it; it is simply no longer how the batch is identified.
    --
    -- The ORDER BY on the outer SELECT is what holds the "in RecordCreationTimeUtc, id
    -- order" contract; the ORDER BY inside the claimed CTE chooses only WHICH rows are
    -- claimed. Both carry "id" as a tiebreaker, because "RecordCreationTimeUtc" is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a LIMIT takes, and the order the
    -- batch comes back in, could differ between two runs of this function.
    RETURN QUERY
    WITH claimed AS (
        SELECT q2."id"
        FROM gda."Q_DriverChangeRecords" q2
        WHERE q2."ProcessingStatus" = 0
           OR (q2."ProcessingStatus" = 1 AND q2."ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY q2."RecordCreationTimeUtc", q2."id"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    ),
    updated AS (
        UPDATE gda."Q_DriverChangeRecords" q
        SET
            "ProcessingStatus" = 1,
            "ProcessingStartTimeUtc" = v_processing_start_time_utc,
            "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
        FROM claimed c
        WHERE q."id" = c."id"
        RETURNING q.*
    )
    SELECT * FROM updated ORDER BY "RecordCreationTimeUtc", "id";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQGenericFaultRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_GenericFaultRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is returned by the claiming UPDATE's RETURNING clause, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the now() value just written, and two
--     claims landing on the same value each received BOTH batches - so the atomic pop
--     was stated here but not delivered. See the note in the body.
-- Parameters:
--   p_batch_size: Maximum number of records to claim in a single batch.
--   p_stale_threshold_minutes: Minutes after which an "in progress" record is
--                              considered stale and eligible for reclaim.
--   p_max_claim_attempts: Claims a record may accumulate before it is treated as
--                         poison and moved to Q_GenericFaultRecordsFail. NULL reclaims
--                         indefinitely, which is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_GenericFaultRecords records, in RecordCreationTimeUtc, id order.
CREATE OR REPLACE FUNCTION gda."spClaimQGenericFaultRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer,
    p_max_claim_attempts integer
)
RETURNS SETOF gda."Q_GenericFaultRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- "RecordCreationTimeUtc" is NOT carried across. It is the fail table's partition
    -- key and defaults to now, so the row lands in the current partition and is retired
    -- on the retention schedule like every other failure.
    -- Bounded by p_batch_size, like every other statement in this function. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to p_batch_size - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. DELETE accepts no LIMIT, so the bound is a subquery on the primary key, and that
    -- subquery takes its rows ORDER BY "id", then FOR UPDATE SKIP LOCKED.
    --
    -- Both of those are for LOCK ORDER, not for choosing which rows qualify - every qualifying
    -- row has the same destination, so the ordering key is simply the primary key, which is also
    -- the key of the queue's partial _Exhausted index. (The claim below takes its rows in its own
    -- order; it is SKIP LOCKED in both statements, not a shared order, that keeps the sweep and
    -- the claim from waiting on each other.) Without them two concurrent sweeps take an
    -- overlapping set in
    -- different orders and block on each other's rows instead of skipping them, which is a
    -- deadlock cycle: measured here in five of eight runs at four and eight claimers, and only
    -- ever with the sweep actually deleting. Deadlocks are retried by DatabaseResilienceHelper,
    -- so they present as latency rather than as errors, and the larger cost is in fact
    -- throughput - eight colliding sweeps banked about a sixth of a dense backlog per pass
    -- against all of it once each takes a distinct set, and a backed-up queue is precisely when
    -- this statement matters. Nothing is lost to SKIP LOCKED: a row another sweep is holding is
    -- left for the next pass, which is the same bargain the bound above already makes. The SQL
    -- Server sibling needed no such hint while its sweep scanned the queue - raced at both
    -- concurrencies it showed no deadlock - but it takes READPAST now that the sweep seeks its
    -- _Exhausted index instead; see the comment there.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply "ProcessingStatus" = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with "RetryCount" + 1;
    -- skipping every claimed row would mean it is never swept, which is the infinite retry this
    -- ceiling exists to end. Mirroring the claim's own predicate removes abandoned rows and
    -- protects live ones.
    --
    -- The literal "RetryCount" > 0 changes no result - the ceiling is never below 1. It is there
    -- so the planner can use the queue's partial _Exhausted index (WHERE "RetryCount" > 0)
    -- whatever plan it caches for this function: a generic plan cannot prove that
    -- "RetryCount" >= p_max_claim_attempts implies it for every parameter value, and would then
    -- read the whole queue before every claim - in the healthy case, where nothing qualifies,
    -- most of all.
    IF p_max_claim_attempts IS NOT NULL THEN
        WITH exhausted AS (
            DELETE FROM gda."Q_GenericFaultRecords" q
            WHERE q."id" IN (
                SELECT q2."id"
                FROM gda."Q_GenericFaultRecords" q2
                WHERE q2."RetryCount" >= p_max_claim_attempts
                  AND q2."RetryCount" > 0
                  AND (q2."ProcessingStatus" = 0
                       OR q2."ProcessingStartTimeUtc" < v_stale_threshold)
                ORDER BY q2."id"
                LIMIT p_batch_size
                FOR UPDATE SKIP LOCKED
            )
            RETURNING q.*
        )
        INSERT INTO gda."Q_GenericFaultRecordsFail" (
            "OriginalQueueId",
            "ThirdPartyId", "DateTime", "Code", "FaultStateActive",
            "OriginalRecordLastChangedUtc", "FailureReason")
        SELECT e."id",
               e."ThirdPartyId", e."DateTime", e."Code", e."FaultStateActive",
               e."RecordLastChangedUtc",
               'Exceeded ' || p_max_claim_attempts || ' claim attempts.'
        FROM exhausted e;
    END IF;

    -- The batch this call claimed is returned by RETURNING, not by re-reading the queue
    -- for ProcessingStartTimeUtc = v_processing_start_time_utc. That re-read is NOT
    -- safe: two claims that land on the same clock value each match BOTH batches, and
    -- now() is transaction-start time, so two claims sharing a transaction are
    -- guaranteed to. The column is still stamped, because the stale-reclaim predicate
    -- above reads it; it is simply no longer how the batch is identified.
    --
    -- The ORDER BY on the outer SELECT is what holds the "in RecordCreationTimeUtc, id
    -- order" contract; the ORDER BY inside the claimed CTE chooses only WHICH rows are
    -- claimed. Both carry "id" as a tiebreaker, because "RecordCreationTimeUtc" is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a LIMIT takes, and the order the
    -- batch comes back in, could differ between two runs of this function.
    RETURN QUERY
    WITH claimed AS (
        SELECT q2."id"
        FROM gda."Q_GenericFaultRecords" q2
        WHERE q2."ProcessingStatus" = 0
           OR (q2."ProcessingStatus" = 1 AND q2."ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY q2."RecordCreationTimeUtc", q2."id"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    ),
    updated AS (
        UPDATE gda."Q_GenericFaultRecords" q
        SET
            "ProcessingStatus" = 1,
            "ProcessingStartTimeUtc" = v_processing_start_time_utc,
            "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
        FROM claimed c
        WHERE q."id" = c."id"
        RETURNING q.*
    )
    SELECT * FROM updated ORDER BY "RecordCreationTimeUtc", "id";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQGenericStatusRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_GenericStatusRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is returned by the claiming UPDATE's RETURNING clause, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the now() value just written, and two
--     claims landing on the same value each received BOTH batches - so the atomic pop
--     was stated here but not delivered. See the note in the body.
-- Parameters:
--   p_batch_size: Maximum number of records to claim in a single batch.
--   p_stale_threshold_minutes: Minutes after which an "in progress" record is
--                              considered stale and eligible for reclaim.
--   p_max_claim_attempts: Claims a record may accumulate before it is treated as
--                         poison and moved to Q_GenericStatusRecordsFail. NULL reclaims
--                         indefinitely, which is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_GenericStatusRecords records, in RecordCreationTimeUtc, id order.
CREATE OR REPLACE FUNCTION gda."spClaimQGenericStatusRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer,
    p_max_claim_attempts integer
)
RETURNS SETOF gda."Q_GenericStatusRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- "RecordCreationTimeUtc" is NOT carried across. It is the fail table's partition
    -- key and defaults to now, so the row lands in the current partition and is retired
    -- on the retention schedule like every other failure.
    -- Bounded by p_batch_size, like every other statement in this function. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to p_batch_size - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. DELETE accepts no LIMIT, so the bound is a subquery on the primary key, and that
    -- subquery takes its rows ORDER BY "id", then FOR UPDATE SKIP LOCKED.
    --
    -- Both of those are for LOCK ORDER, not for choosing which rows qualify - every qualifying
    -- row has the same destination, so the ordering key is simply the primary key, which is also
    -- the key of the queue's partial _Exhausted index. (The claim below takes its rows in its own
    -- order; it is SKIP LOCKED in both statements, not a shared order, that keeps the sweep and
    -- the claim from waiting on each other.) Without them two concurrent sweeps take an
    -- overlapping set in
    -- different orders and block on each other's rows instead of skipping them, which is a
    -- deadlock cycle: measured here in five of eight runs at four and eight claimers, and only
    -- ever with the sweep actually deleting. Deadlocks are retried by DatabaseResilienceHelper,
    -- so they present as latency rather than as errors, and the larger cost is in fact
    -- throughput - eight colliding sweeps banked about a sixth of a dense backlog per pass
    -- against all of it once each takes a distinct set, and a backed-up queue is precisely when
    -- this statement matters. Nothing is lost to SKIP LOCKED: a row another sweep is holding is
    -- left for the next pass, which is the same bargain the bound above already makes. The SQL
    -- Server sibling needed no such hint while its sweep scanned the queue - raced at both
    -- concurrencies it showed no deadlock - but it takes READPAST now that the sweep seeks its
    -- _Exhausted index instead; see the comment there.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply "ProcessingStatus" = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with "RetryCount" + 1;
    -- skipping every claimed row would mean it is never swept, which is the infinite retry this
    -- ceiling exists to end. Mirroring the claim's own predicate removes abandoned rows and
    -- protects live ones.
    --
    -- The literal "RetryCount" > 0 changes no result - the ceiling is never below 1. It is there
    -- so the planner can use the queue's partial _Exhausted index (WHERE "RetryCount" > 0)
    -- whatever plan it caches for this function: a generic plan cannot prove that
    -- "RetryCount" >= p_max_claim_attempts implies it for every parameter value, and would then
    -- read the whole queue before every claim - in the healthy case, where nothing qualifies,
    -- most of all.
    IF p_max_claim_attempts IS NOT NULL THEN
        WITH exhausted AS (
            DELETE FROM gda."Q_GenericStatusRecords" q
            WHERE q."id" IN (
                SELECT q2."id"
                FROM gda."Q_GenericStatusRecords" q2
                WHERE q2."RetryCount" >= p_max_claim_attempts
                  AND q2."RetryCount" > 0
                  AND (q2."ProcessingStatus" = 0
                       OR q2."ProcessingStartTimeUtc" < v_stale_threshold)
                ORDER BY q2."id"
                LIMIT p_batch_size
                FOR UPDATE SKIP LOCKED
            )
            RETURNING q.*
        )
        INSERT INTO gda."Q_GenericStatusRecordsFail" (
            "OriginalQueueId",
            "ThirdPartyId", "DateTime", "Code", "Value",
            "OriginalRecordLastChangedUtc", "FailureReason")
        SELECT e."id",
               e."ThirdPartyId", e."DateTime", e."Code", e."Value",
               e."RecordLastChangedUtc",
               'Exceeded ' || p_max_claim_attempts || ' claim attempts.'
        FROM exhausted e;
    END IF;

    -- The batch this call claimed is returned by RETURNING, not by re-reading the queue
    -- for ProcessingStartTimeUtc = v_processing_start_time_utc. That re-read is NOT
    -- safe: two claims that land on the same clock value each match BOTH batches, and
    -- now() is transaction-start time, so two claims sharing a transaction are
    -- guaranteed to. The column is still stamped, because the stale-reclaim predicate
    -- above reads it; it is simply no longer how the batch is identified.
    --
    -- The ORDER BY on the outer SELECT is what holds the "in RecordCreationTimeUtc, id
    -- order" contract; the ORDER BY inside the claimed CTE chooses only WHICH rows are
    -- claimed. Both carry "id" as a tiebreaker, because "RecordCreationTimeUtc" is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a LIMIT takes, and the order the
    -- batch comes back in, could differ between two runs of this function.
    RETURN QUERY
    WITH claimed AS (
        SELECT q2."id"
        FROM gda."Q_GenericStatusRecords" q2
        WHERE q2."ProcessingStatus" = 0
           OR (q2."ProcessingStatus" = 1 AND q2."ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY q2."RecordCreationTimeUtc", q2."id"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    ),
    updated AS (
        UPDATE gda."Q_GenericStatusRecords" q
        SET
            "ProcessingStatus" = 1,
            "ProcessingStartTimeUtc" = v_processing_start_time_utc,
            "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
        FROM claimed c
        WHERE q."id" = c."id"
        RETURNING q.*
    )
    SELECT * FROM updated ORDER BY "RecordCreationTimeUtc", "id";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQJ1708FaultRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_J1708FaultRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is returned by the claiming UPDATE's RETURNING clause, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the now() value just written, and two
--     claims landing on the same value each received BOTH batches - so the atomic pop
--     was stated here but not delivered. See the note in the body.
-- Parameters:
--   p_batch_size: Maximum number of records to claim in a single batch.
--   p_stale_threshold_minutes: Minutes after which an "in progress" record is
--                              considered stale and eligible for reclaim.
--   p_max_claim_attempts: Claims a record may accumulate before it is treated as
--                         poison and moved to Q_J1708FaultRecordsFail. NULL reclaims
--                         indefinitely, which is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_J1708FaultRecords records, in RecordCreationTimeUtc, id order.
CREATE OR REPLACE FUNCTION gda."spClaimQJ1708FaultRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer,
    p_max_claim_attempts integer
)
RETURNS SETOF gda."Q_J1708FaultRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- "RecordCreationTimeUtc" is NOT carried across. It is the fail table's partition
    -- key and defaults to now, so the row lands in the current partition and is retired
    -- on the retention schedule like every other failure.
    -- Bounded by p_batch_size, like every other statement in this function. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to p_batch_size - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. DELETE accepts no LIMIT, so the bound is a subquery on the primary key, and that
    -- subquery takes its rows ORDER BY "id", then FOR UPDATE SKIP LOCKED.
    --
    -- Both of those are for LOCK ORDER, not for choosing which rows qualify - every qualifying
    -- row has the same destination, so the ordering key is simply the primary key, which is also
    -- the key of the queue's partial _Exhausted index. (The claim below takes its rows in its own
    -- order; it is SKIP LOCKED in both statements, not a shared order, that keeps the sweep and
    -- the claim from waiting on each other.) Without them two concurrent sweeps take an
    -- overlapping set in
    -- different orders and block on each other's rows instead of skipping them, which is a
    -- deadlock cycle: measured here in five of eight runs at four and eight claimers, and only
    -- ever with the sweep actually deleting. Deadlocks are retried by DatabaseResilienceHelper,
    -- so they present as latency rather than as errors, and the larger cost is in fact
    -- throughput - eight colliding sweeps banked about a sixth of a dense backlog per pass
    -- against all of it once each takes a distinct set, and a backed-up queue is precisely when
    -- this statement matters. Nothing is lost to SKIP LOCKED: a row another sweep is holding is
    -- left for the next pass, which is the same bargain the bound above already makes. The SQL
    -- Server sibling needed no such hint while its sweep scanned the queue - raced at both
    -- concurrencies it showed no deadlock - but it takes READPAST now that the sweep seeks its
    -- _Exhausted index instead; see the comment there.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply "ProcessingStatus" = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with "RetryCount" + 1;
    -- skipping every claimed row would mean it is never swept, which is the infinite retry this
    -- ceiling exists to end. Mirroring the claim's own predicate removes abandoned rows and
    -- protects live ones.
    --
    -- The literal "RetryCount" > 0 changes no result - the ceiling is never below 1. It is there
    -- so the planner can use the queue's partial _Exhausted index (WHERE "RetryCount" > 0)
    -- whatever plan it caches for this function: a generic plan cannot prove that
    -- "RetryCount" >= p_max_claim_attempts implies it for every parameter value, and would then
    -- read the whole queue before every claim - in the healthy case, where nothing qualifies,
    -- most of all.
    IF p_max_claim_attempts IS NOT NULL THEN
        WITH exhausted AS (
            DELETE FROM gda."Q_J1708FaultRecords" q
            WHERE q."id" IN (
                SELECT q2."id"
                FROM gda."Q_J1708FaultRecords" q2
                WHERE q2."RetryCount" >= p_max_claim_attempts
                  AND q2."RetryCount" > 0
                  AND (q2."ProcessingStatus" = 0
                       OR q2."ProcessingStartTimeUtc" < v_stale_threshold)
                ORDER BY q2."id"
                LIMIT p_batch_size
                FOR UPDATE SKIP LOCKED
            )
            RETURNING q.*
        )
        INSERT INTO gda."Q_J1708FaultRecordsFail" (
            "OriginalQueueId",
            "ThirdPartyId", "DateTime", "MessageId", "ParameterId", "SubsystemId",
            "FailureModeIdentifier", "OccurrenceCount", "FaultStateActive",
            "OriginalRecordLastChangedUtc", "FailureReason")
        SELECT e."id",
               e."ThirdPartyId", e."DateTime", e."MessageId", e."ParameterId",
               e."SubsystemId", e."FailureModeIdentifier", e."OccurrenceCount",
               e."FaultStateActive",
               e."RecordLastChangedUtc",
               'Exceeded ' || p_max_claim_attempts || ' claim attempts.'
        FROM exhausted e;
    END IF;

    -- The batch this call claimed is returned by RETURNING, not by re-reading the queue
    -- for ProcessingStartTimeUtc = v_processing_start_time_utc. That re-read is NOT
    -- safe: two claims that land on the same clock value each match BOTH batches, and
    -- now() is transaction-start time, so two claims sharing a transaction are
    -- guaranteed to. The column is still stamped, because the stale-reclaim predicate
    -- above reads it; it is simply no longer how the batch is identified.
    --
    -- The ORDER BY on the outer SELECT is what holds the "in RecordCreationTimeUtc, id
    -- order" contract; the ORDER BY inside the claimed CTE chooses only WHICH rows are
    -- claimed. Both carry "id" as a tiebreaker, because "RecordCreationTimeUtc" is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a LIMIT takes, and the order the
    -- batch comes back in, could differ between two runs of this function.
    RETURN QUERY
    WITH claimed AS (
        SELECT q2."id"
        FROM gda."Q_J1708FaultRecords" q2
        WHERE q2."ProcessingStatus" = 0
           OR (q2."ProcessingStatus" = 1 AND q2."ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY q2."RecordCreationTimeUtc", q2."id"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    ),
    updated AS (
        UPDATE gda."Q_J1708FaultRecords" q
        SET
            "ProcessingStatus" = 1,
            "ProcessingStartTimeUtc" = v_processing_start_time_utc,
            "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
        FROM claimed c
        WHERE q."id" = c."id"
        RETURNING q.*
    )
    SELECT * FROM updated ORDER BY "RecordCreationTimeUtc", "id";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQJ1939FaultRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_J1939FaultRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is returned by the claiming UPDATE's RETURNING clause, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the now() value just written, and two
--     claims landing on the same value each received BOTH batches - so the atomic pop
--     was stated here but not delivered. See the note in the body.
-- Parameters:
--   p_batch_size: Maximum number of records to claim in a single batch.
--   p_stale_threshold_minutes: Minutes after which an "in progress" record is
--                              considered stale and eligible for reclaim.
--   p_max_claim_attempts: Claims a record may accumulate before it is treated as
--                         poison and moved to Q_J1939FaultRecordsFail. NULL reclaims
--                         indefinitely, which is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_J1939FaultRecords records, in RecordCreationTimeUtc, id order.
CREATE OR REPLACE FUNCTION gda."spClaimQJ1939FaultRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer,
    p_max_claim_attempts integer
)
RETURNS SETOF gda."Q_J1939FaultRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- "RecordCreationTimeUtc" is NOT carried across. It is the fail table's partition
    -- key and defaults to now, so the row lands in the current partition and is retired
    -- on the retention schedule like every other failure.
    -- Bounded by p_batch_size, like every other statement in this function. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to p_batch_size - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. DELETE accepts no LIMIT, so the bound is a subquery on the primary key, and that
    -- subquery takes its rows ORDER BY "id", then FOR UPDATE SKIP LOCKED.
    --
    -- Both of those are for LOCK ORDER, not for choosing which rows qualify - every qualifying
    -- row has the same destination, so the ordering key is simply the primary key, which is also
    -- the key of the queue's partial _Exhausted index. (The claim below takes its rows in its own
    -- order; it is SKIP LOCKED in both statements, not a shared order, that keeps the sweep and
    -- the claim from waiting on each other.) Without them two concurrent sweeps take an
    -- overlapping set in
    -- different orders and block on each other's rows instead of skipping them, which is a
    -- deadlock cycle: measured here in five of eight runs at four and eight claimers, and only
    -- ever with the sweep actually deleting. Deadlocks are retried by DatabaseResilienceHelper,
    -- so they present as latency rather than as errors, and the larger cost is in fact
    -- throughput - eight colliding sweeps banked about a sixth of a dense backlog per pass
    -- against all of it once each takes a distinct set, and a backed-up queue is precisely when
    -- this statement matters. Nothing is lost to SKIP LOCKED: a row another sweep is holding is
    -- left for the next pass, which is the same bargain the bound above already makes. The SQL
    -- Server sibling needed no such hint while its sweep scanned the queue - raced at both
    -- concurrencies it showed no deadlock - but it takes READPAST now that the sweep seeks its
    -- _Exhausted index instead; see the comment there.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply "ProcessingStatus" = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with "RetryCount" + 1;
    -- skipping every claimed row would mean it is never swept, which is the infinite retry this
    -- ceiling exists to end. Mirroring the claim's own predicate removes abandoned rows and
    -- protects live ones.
    --
    -- The literal "RetryCount" > 0 changes no result - the ceiling is never below 1. It is there
    -- so the planner can use the queue's partial _Exhausted index (WHERE "RetryCount" > 0)
    -- whatever plan it caches for this function: a generic plan cannot prove that
    -- "RetryCount" >= p_max_claim_attempts implies it for every parameter value, and would then
    -- read the whole queue before every claim - in the healthy case, where nothing qualifies,
    -- most of all.
    IF p_max_claim_attempts IS NOT NULL THEN
        WITH exhausted AS (
            DELETE FROM gda."Q_J1939FaultRecords" q
            WHERE q."id" IN (
                SELECT q2."id"
                FROM gda."Q_J1939FaultRecords" q2
                WHERE q2."RetryCount" >= p_max_claim_attempts
                  AND q2."RetryCount" > 0
                  AND (q2."ProcessingStatus" = 0
                       OR q2."ProcessingStartTimeUtc" < v_stale_threshold)
                ORDER BY q2."id"
                LIMIT p_batch_size
                FOR UPDATE SKIP LOCKED
            )
            RETURNING q.*
        )
        INSERT INTO gda."Q_J1939FaultRecordsFail" (
            "OriginalQueueId",
            "ThirdPartyId", "DateTime", "SuspectParameterNumber",
            "FailureModeIdentifier", "OccurrenceCount", "SourceAddress",
            "MalfunctionLamp", "RedStopLamp", "AmberWarningLamp", "ProtectWarningLamp",
            "FaultStateActive",
            "OriginalRecordLastChangedUtc", "FailureReason")
        SELECT e."id",
               e."ThirdPartyId", e."DateTime", e."SuspectParameterNumber",
               e."FailureModeIdentifier", e."OccurrenceCount", e."SourceAddress",
               e."MalfunctionLamp", e."RedStopLamp", e."AmberWarningLamp",
               e."ProtectWarningLamp", e."FaultStateActive",
               e."RecordLastChangedUtc",
               'Exceeded ' || p_max_claim_attempts || ' claim attempts.'
        FROM exhausted e;
    END IF;

    -- The batch this call claimed is returned by RETURNING, not by re-reading the queue
    -- for ProcessingStartTimeUtc = v_processing_start_time_utc. That re-read is NOT
    -- safe: two claims that land on the same clock value each match BOTH batches, and
    -- now() is transaction-start time, so two claims sharing a transaction are
    -- guaranteed to. The column is still stamped, because the stale-reclaim predicate
    -- above reads it; it is simply no longer how the batch is identified.
    --
    -- The ORDER BY on the outer SELECT is what holds the "in RecordCreationTimeUtc, id
    -- order" contract; the ORDER BY inside the claimed CTE chooses only WHICH rows are
    -- claimed. Both carry "id" as a tiebreaker, because "RecordCreationTimeUtc" is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a LIMIT takes, and the order the
    -- batch comes back in, could differ between two runs of this function.
    RETURN QUERY
    WITH claimed AS (
        SELECT q2."id"
        FROM gda."Q_J1939FaultRecords" q2
        WHERE q2."ProcessingStatus" = 0
           OR (q2."ProcessingStatus" = 1 AND q2."ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY q2."RecordCreationTimeUtc", q2."id"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    ),
    updated AS (
        UPDATE gda."Q_J1939FaultRecords" q
        SET
            "ProcessingStatus" = 1,
            "ProcessingStartTimeUtc" = v_processing_start_time_utc,
            "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
        FROM claimed c
        WHERE q."id" = c."id"
        RETURNING q.*
    )
    SELECT * FROM updated ORDER BY "RecordCreationTimeUtc", "id";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQObdiiFaultRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_ObdiiFaultRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is returned by the claiming UPDATE's RETURNING clause, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the now() value just written, and two
--     claims landing on the same value each received BOTH batches - so the atomic pop
--     was stated here but not delivered. See the note in the body.
-- Parameters:
--   p_batch_size: Maximum number of records to claim in a single batch.
--   p_stale_threshold_minutes: Minutes after which an "in progress" record is
--                              considered stale and eligible for reclaim.
--   p_max_claim_attempts: Claims a record may accumulate before it is treated as
--                         poison and moved to Q_ObdiiFaultRecordsFail. NULL reclaims
--                         indefinitely, which is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_ObdiiFaultRecords records, in RecordCreationTimeUtc, id order.
CREATE OR REPLACE FUNCTION gda."spClaimQObdiiFaultRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer,
    p_max_claim_attempts integer
)
RETURNS SETOF gda."Q_ObdiiFaultRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- "RecordCreationTimeUtc" is NOT carried across. It is the fail table's partition
    -- key and defaults to now, so the row lands in the current partition and is retired
    -- on the retention schedule like every other failure.
    -- Bounded by p_batch_size, like every other statement in this function. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to p_batch_size - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. DELETE accepts no LIMIT, so the bound is a subquery on the primary key, and that
    -- subquery takes its rows ORDER BY "id", then FOR UPDATE SKIP LOCKED.
    --
    -- Both of those are for LOCK ORDER, not for choosing which rows qualify - every qualifying
    -- row has the same destination, so the ordering key is simply the primary key, which is also
    -- the key of the queue's partial _Exhausted index. (The claim below takes its rows in its own
    -- order; it is SKIP LOCKED in both statements, not a shared order, that keeps the sweep and
    -- the claim from waiting on each other.) Without them two concurrent sweeps take an
    -- overlapping set in
    -- different orders and block on each other's rows instead of skipping them, which is a
    -- deadlock cycle: measured here in five of eight runs at four and eight claimers, and only
    -- ever with the sweep actually deleting. Deadlocks are retried by DatabaseResilienceHelper,
    -- so they present as latency rather than as errors, and the larger cost is in fact
    -- throughput - eight colliding sweeps banked about a sixth of a dense backlog per pass
    -- against all of it once each takes a distinct set, and a backed-up queue is precisely when
    -- this statement matters. Nothing is lost to SKIP LOCKED: a row another sweep is holding is
    -- left for the next pass, which is the same bargain the bound above already makes. The SQL
    -- Server sibling needed no such hint while its sweep scanned the queue - raced at both
    -- concurrencies it showed no deadlock - but it takes READPAST now that the sweep seeks its
    -- _Exhausted index instead; see the comment there.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply "ProcessingStatus" = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with "RetryCount" + 1;
    -- skipping every claimed row would mean it is never swept, which is the infinite retry this
    -- ceiling exists to end. Mirroring the claim's own predicate removes abandoned rows and
    -- protects live ones.
    --
    -- The literal "RetryCount" > 0 changes no result - the ceiling is never below 1. It is there
    -- so the planner can use the queue's partial _Exhausted index (WHERE "RetryCount" > 0)
    -- whatever plan it caches for this function: a generic plan cannot prove that
    -- "RetryCount" >= p_max_claim_attempts implies it for every parameter value, and would then
    -- read the whole queue before every claim - in the healthy case, where nothing qualifies,
    -- most of all.
    IF p_max_claim_attempts IS NOT NULL THEN
        WITH exhausted AS (
            DELETE FROM gda."Q_ObdiiFaultRecords" q
            WHERE q."id" IN (
                SELECT q2."id"
                FROM gda."Q_ObdiiFaultRecords" q2
                WHERE q2."RetryCount" >= p_max_claim_attempts
                  AND q2."RetryCount" > 0
                  AND (q2."ProcessingStatus" = 0
                       OR q2."ProcessingStartTimeUtc" < v_stale_threshold)
                ORDER BY q2."id"
                LIMIT p_batch_size
                FOR UPDATE SKIP LOCKED
            )
            RETURNING q.*
        )
        INSERT INTO gda."Q_ObdiiFaultRecordsFail" (
            "OriginalQueueId",
            "ThirdPartyId", "DateTime", "Code", "FaultStateActive",
            "OriginalRecordLastChangedUtc", "FailureReason")
        SELECT e."id",
               e."ThirdPartyId", e."DateTime", e."Code", e."FaultStateActive",
               e."RecordLastChangedUtc",
               'Exceeded ' || p_max_claim_attempts || ' claim attempts.'
        FROM exhausted e;
    END IF;

    -- The batch this call claimed is returned by RETURNING, not by re-reading the queue
    -- for ProcessingStartTimeUtc = v_processing_start_time_utc. That re-read is NOT
    -- safe: two claims that land on the same clock value each match BOTH batches, and
    -- now() is transaction-start time, so two claims sharing a transaction are
    -- guaranteed to. The column is still stamped, because the stale-reclaim predicate
    -- above reads it; it is simply no longer how the batch is identified.
    --
    -- The ORDER BY on the outer SELECT is what holds the "in RecordCreationTimeUtc, id
    -- order" contract; the ORDER BY inside the claimed CTE chooses only WHICH rows are
    -- claimed. Both carry "id" as a tiebreaker, because "RecordCreationTimeUtc" is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a LIMIT takes, and the order the
    -- batch comes back in, could differ between two runs of this function.
    RETURN QUERY
    WITH claimed AS (
        SELECT q2."id"
        FROM gda."Q_ObdiiFaultRecords" q2
        WHERE q2."ProcessingStatus" = 0
           OR (q2."ProcessingStatus" = 1 AND q2."ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY q2."RecordCreationTimeUtc", q2."id"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    ),
    updated AS (
        UPDATE gda."Q_ObdiiFaultRecords" q
        SET
            "ProcessingStatus" = 1,
            "ProcessingStartTimeUtc" = v_processing_start_time_utc,
            "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
        FROM claimed c
        WHERE q."id" = c."id"
        RETURNING q.*
    )
    SELECT * FROM updated ORDER BY "RecordCreationTimeUtc", "id";
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQVinRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_VinRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is returned by the claiming UPDATE's RETURNING clause, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the now() value just written, and two
--     claims landing on the same value each received BOTH batches - so the atomic pop
--     was stated here but not delivered. See the note in the body.
-- Parameters:
--   p_batch_size: Maximum number of records to claim in a single batch.
--   p_stale_threshold_minutes: Minutes after which an "in progress" record is
--                              considered stale and eligible for reclaim.
--   p_max_claim_attempts: Claims a record may accumulate before it is treated as
--                         poison and moved to Q_VinRecordsFail. NULL reclaims
--                         indefinitely, which is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_VinRecords records, in RecordCreationTimeUtc, id order.
CREATE OR REPLACE FUNCTION gda."spClaimQVinRecordsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer,
    p_max_claim_attempts integer
)
RETURNS SETOF gda."Q_VinRecords"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
BEGIN
    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- "RecordCreationTimeUtc" is NOT carried across. It is the fail table's partition
    -- key and defaults to now, so the row lands in the current partition and is retired
    -- on the retention schedule like every other failure.
    -- Bounded by p_batch_size, like every other statement in this function. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to p_batch_size - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. DELETE accepts no LIMIT, so the bound is a subquery on the primary key, and that
    -- subquery takes its rows ORDER BY "id", then FOR UPDATE SKIP LOCKED.
    --
    -- Both of those are for LOCK ORDER, not for choosing which rows qualify - every qualifying
    -- row has the same destination, so the ordering key is simply the primary key, which is also
    -- the key of the queue's partial _Exhausted index. (The claim below takes its rows in its own
    -- order; it is SKIP LOCKED in both statements, not a shared order, that keeps the sweep and
    -- the claim from waiting on each other.) Without them two concurrent sweeps take an
    -- overlapping set in
    -- different orders and block on each other's rows instead of skipping them, which is a
    -- deadlock cycle: measured here in five of eight runs at four and eight claimers, and only
    -- ever with the sweep actually deleting. Deadlocks are retried by DatabaseResilienceHelper,
    -- so they present as latency rather than as errors, and the larger cost is in fact
    -- throughput - eight colliding sweeps banked about a sixth of a dense backlog per pass
    -- against all of it once each takes a distinct set, and a backed-up queue is precisely when
    -- this statement matters. Nothing is lost to SKIP LOCKED: a row another sweep is holding is
    -- left for the next pass, which is the same bargain the bound above already makes. The SQL
    -- Server sibling needed no such hint while its sweep scanned the queue - raced at both
    -- concurrencies it showed no deadlock - but it takes READPAST now that the sweep seeks its
    -- _Exhausted index instead; see the comment there.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply "ProcessingStatus" = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with "RetryCount" + 1;
    -- skipping every claimed row would mean it is never swept, which is the infinite retry this
    -- ceiling exists to end. Mirroring the claim's own predicate removes abandoned rows and
    -- protects live ones.
    --
    -- The literal "RetryCount" > 0 changes no result - the ceiling is never below 1. It is there
    -- so the planner can use the queue's partial _Exhausted index (WHERE "RetryCount" > 0)
    -- whatever plan it caches for this function: a generic plan cannot prove that
    -- "RetryCount" >= p_max_claim_attempts implies it for every parameter value, and would then
    -- read the whole queue before every claim - in the healthy case, where nothing qualifies,
    -- most of all.
    IF p_max_claim_attempts IS NOT NULL THEN
        WITH exhausted AS (
            DELETE FROM gda."Q_VinRecords" q
            WHERE q."id" IN (
                SELECT q2."id"
                FROM gda."Q_VinRecords" q2
                WHERE q2."RetryCount" >= p_max_claim_attempts
                  AND q2."RetryCount" > 0
                  AND (q2."ProcessingStatus" = 0
                       OR q2."ProcessingStartTimeUtc" < v_stale_threshold)
                ORDER BY q2."id"
                LIMIT p_batch_size
                FOR UPDATE SKIP LOCKED
            )
            RETURNING q.*
        )
        INSERT INTO gda."Q_VinRecordsFail" (
            "OriginalQueueId",
            "ThirdPartyId", "DateTime", "VehicleIdentificationNumber",
            "OriginalRecordLastChangedUtc", "FailureReason")
        SELECT e."id",
               e."ThirdPartyId", e."DateTime", e."VehicleIdentificationNumber",
               e."RecordLastChangedUtc",
               'Exceeded ' || p_max_claim_attempts || ' claim attempts.'
        FROM exhausted e;
    END IF;

    -- The batch this call claimed is returned by RETURNING, not by re-reading the queue
    -- for ProcessingStartTimeUtc = v_processing_start_time_utc. That re-read is NOT
    -- safe: two claims that land on the same clock value each match BOTH batches, and
    -- now() is transaction-start time, so two claims sharing a transaction are
    -- guaranteed to. The column is still stamped, because the stale-reclaim predicate
    -- above reads it; it is simply no longer how the batch is identified.
    --
    -- The ORDER BY on the outer SELECT is what holds the "in RecordCreationTimeUtc, id
    -- order" contract; the ORDER BY inside the claimed CTE chooses only WHICH rows are
    -- claimed. Both carry "id" as a tiebreaker, because "RecordCreationTimeUtc" is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a LIMIT takes, and the order the
    -- batch comes back in, could differ between two runs of this function.
    RETURN QUERY
    WITH claimed AS (
        SELECT q2."id"
        FROM gda."Q_VinRecords" q2
        WHERE q2."ProcessingStatus" = 0
           OR (q2."ProcessingStatus" = 1 AND q2."ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY q2."RecordCreationTimeUtc", q2."id"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    ),
    updated AS (
        UPDATE gda."Q_VinRecords" q
        SET
            "ProcessingStatus" = 1,
            "ProcessingStartTimeUtc" = v_processing_start_time_utc,
            "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
        FROM claimed c
        WHERE q."id" = c."id"
        RETURNING q.*
    )
    SELECT * FROM updated ORDER BY "RecordCreationTimeUtc", "id";
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


/*** [START] Part 5: OEM Subscription Mode (added in 5.2.0.0) ***/
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Sequence: SeqDeviceSubscriptionOrdinal
--
-- The single ordering key shared by both subscription intakes. The delta intake
-- draws from it via the Q_DeviceSubscriptions."OrderingOrdinal" column default;
-- the polling service draws ONE value per poll and stamps it on every staged row.
--
-- Values are monotonic but not gapless (both providers cache ranges, and a restart
-- may skip forward). Monotonicity is what the high-water mark requires; gaplessness
-- is not. Note also that ordinal order is DRAW order, not COMMIT order - two writers
-- can draw 5 and 6 and commit 6 first. The same is true of IDENTITY columns.
CREATE SEQUENCE gda."SeqDeviceSubscriptionOrdinal"
    AS bigint
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: DeviceSubscriptions
--
-- The OEM control plane's state table. Separate from ProvisionedDevices by design;
-- the two control planes never share a table. Terminated rows are RETAINED (status
-- 0) rather than deleted, so re-enrollment is an update and the audit answer is
-- unambiguous.
CREATE TABLE gda."DeviceSubscriptions" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ThirdPartyId" varchar(50) NOT NULL,
    "IdentifierType" varchar(20) NOT NULL DEFAULT 'Vin',
    "GeotabSerialNumber" varchar(50) NOT NULL,
    "SubscriptionStatus" smallint NOT NULL DEFAULT 1,
    "SubPlan" varchar(50) NULL,
    "LastAppliedOrdinal" bigint NOT NULL DEFAULT 0,
    "SubscribedDateTimeUtc" timestamp without time zone NULL,
    "TerminatedDateTimeUtc" timestamp without time zone NULL,
    "TerminationReason" varchar(200) NULL,
    "LastSeenInSnapshotUtc" timestamp without time zone NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "RecordLastChangedUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

-- UNFILTERED ON PURPOSE. This is the merge's ON CONFLICT target. Filtering it would
-- let a re-subscribe insert a second row for an identifier that already has a
-- terminated one, and the upsert would stop being an upsert.
CREATE UNIQUE INDEX "IX_DeviceSubscriptions_ThirdPartyId"
ON gda."DeviceSubscriptions" ("ThirdPartyId");

-- FILTERED ON PURPOSE. MyGeotab enforces one identifier per serial number for the
-- life of a subscription, but terminating a subscription removes that registration
-- entirely - so after terminate-then-reprovision a serial number can legitimately
-- acquire a DIFFERENT identifier. An unfiltered unique index would encode "one
-- identifier per serial number forever" and would wedge re-provisioning with no
-- recovery short of manual SQL.
CREATE UNIQUE INDEX "IX_DeviceSubscriptions_GeotabSerialNumber_Active"
ON gda."DeviceSubscriptions" ("GeotabSerialNumber")
WHERE "SubscriptionStatus" = 1;

-- Serves the snapshot-mode passes of spMerge_stg_DeviceSubscriptions, which find the rows
-- a snapshot has left out by selecting on "SubscriptionStatus".
--
-- It does NOT cover the device-lookup cache query. That query also selects
-- "RecordLastChangedUtc", which is in neither the key nor the INCLUDE list. Adding the
-- column was measured at 50,000 rows and landed either side of zero - +2.8% here against
-- -16.9% for the equivalent index over ProvisionedDevices - on a cache that reloads on a
-- multi-minute interval. The INCLUDE list is therefore left as it is, deliberately.
CREATE INDEX "IX_DeviceSubscriptions_Active"
ON gda."DeviceSubscriptions" ("SubscriptionStatus")
INCLUDE ("ThirdPartyId", "GeotabSerialNumber");


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_DeviceSubscriptions
--
-- The delta intake, used by the email-driven process. An external process inserts
-- Subscribe and Terminate instructions; DeviceSubscriptionService claims, stages
-- and merges them.
--
-- Deliberately NOT unique on ThirdPartyId - a device can be subscribed, terminated
-- and resubscribed.
CREATE TABLE gda."Q_DeviceSubscriptions" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "OrderingOrdinal" bigint NOT NULL DEFAULT nextval('gda."SeqDeviceSubscriptionOrdinal"'),
    -- Both values are legal, so a wrong one is silent rather than an error; the constraint at least closes the set.
    "OperationType" smallint NOT NULL CONSTRAINT "Q_DeviceSubscriptions_OperationType_check" CHECK ("OperationType" IN (0, 1)),
    "ThirdPartyId" varchar(50) NULL,
    "IdentifierType" varchar(20) NULL,
    "GeotabSerialNumber" varchar(50) NULL,
    "SubPlan" varchar(50) NULL,
    "SourceStatus" varchar(30) NULL,
    "EffectiveDateTimeUtc" timestamp without time zone NULL,
    "TerminationReason" varchar(200) NULL,
    "SourceReference" varchar(500) NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "RecordLastChangedUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "ProcessingStatus" smallint NOT NULL DEFAULT 0,
    "ProcessingStartTimeUtc" timestamp without time zone NULL,
    "ClaimId" uuid NULL,
    "RetryCount" smallint NOT NULL DEFAULT 0
);

-- Keyed on OrderingOrdinal rather than id, because OrderingOrdinal is what the
-- claim function orders by and what the merge compares against the high-water mark.
CREATE INDEX "IX_Q_DeviceSubscriptions_PendingWork"
ON gda."Q_DeviceSubscriptions" ("OrderingOrdinal")
WHERE "ProcessingStatus" = 0;

-- Two indexes for the claim function, the same pair the other twelve queues
-- carry - but keyed on "OrderingOrdinal", because that is what THIS claim orders
-- by. An index on "RecordCreationTimeUtc" would serve nothing here. The
-- PendingWork index above serves neither statement: it is partial on
-- "ProcessingStatus" = 0, and both the claim and the sweep also take rows whose
-- claim has gone stale, which lie outside that predicate - so the OR cannot use
-- it.
--
-- The _OrderingOrdinal index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take p_batch_size rows, so the cost grows with
-- queue depth - worst exactly when the queue has backed up and throughput matters
-- most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the LIMIT can never
-- be satisfied, so unindexed every row is read before concluding that none qualify.
-- "RetryCount" > 0 is a deliberate SUPERSET of the sweep's own
-- "RetryCount" >= p_max_claim_attempts - the configured ceiling is never below 1 - so
-- this predicate needs no revision if that floor moves, and the index holds only rows
-- a device-lookup hold or a stale reclaim has touched - few or none on a healthy queue.
--
-- The pair was measured on a telemetry queue holding 6,000,000 rows: a 5,000-row
-- claim fell from 5,953 ms to 90 ms here and from 5,073 ms to 464 ms on SQL
-- Server, and NEITHER INDEX ALONE WAS SUFFICIENT ON EITHER PROVIDER. This queue
-- carries operator instructions and does not reach that depth; it takes the pair
-- so that one claim shape is indexed one way on all thirteen.
CREATE INDEX "IX_Q_DeviceSubscriptions_OrderingOrdinal"
ON gda."Q_DeviceSubscriptions" ("OrderingOrdinal");

CREATE INDEX "IX_Q_DeviceSubscriptions_Exhausted"
ON gda."Q_DeviceSubscriptions" ("id")
WHERE "RetryCount" > 0;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_DeviceSubscriptionsFail
--
-- Follows the Q_ProvisionDevicesFail shape: no ProcessingStatus /
-- ProcessingStartTimeUtc / ClaimId / RetryCount, RecordLastChangedUtc renamed to
-- OriginalRecordLastChangedUtc, and RecordCreationTimeUtc redefined to mean "when
-- the failure was recorded".
--
-- OriginalQueueId, OriginalOrderingOrdinal and OriginalRecordCreationTimeUtc are
-- ALL NULLABLE ON PURPOSE. The polling service stages directly and never creates a
-- queue row, so a row the merge rejects in snapshot mode has no queue identity to
-- record. Making these NOT NULL would leave poll-sourced rejections with nowhere
-- to land.
CREATE TABLE gda."Q_DeviceSubscriptionsFail" (
    "id" bigint GENERATED ALWAYS AS IDENTITY,
    "OriginalQueueId" bigint NULL,
    "OriginalOrderingOrdinal" bigint NULL,
    "OperationType" smallint NOT NULL,
    "ThirdPartyId" varchar(50) NULL,
    "IdentifierType" varchar(20) NULL,
    "GeotabSerialNumber" varchar(50) NULL,
    "SubPlan" varchar(50) NULL,
    "SourceStatus" varchar(30) NULL,
    "EffectiveDateTimeUtc" timestamp without time zone NULL,
    "TerminationReason" varchar(200) NULL,
    "SourceReference" varchar(500) NULL,
    "OriginalRecordCreationTimeUtc" timestamp without time zone NULL,
    "OriginalRecordLastChangedUtc" timestamp without time zone NULL,
    "FailureReason" text NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    PRIMARY KEY ("id", "RecordCreationTimeUtc")
)
PARTITION BY RANGE ("RecordCreationTimeUtc");

-- The DEFAULT partition guarantees an INSERT can never fail for want of a
-- period partition. spManageDigPartitions creates the period partitions.
CREATE TABLE gda."Q_DeviceSubscriptionsFail_default" PARTITION OF gda."Q_DeviceSubscriptionsFail" DEFAULT;

CREATE INDEX "IX_Q_DeviceSubscriptionsFail_ThirdPartyId"
ON gda."Q_DeviceSubscriptionsFail" ("ThirdPartyId");


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: stg_DeviceSubscriptions
--
-- Both intakes converge here. Unlike the MyGeotab API Adapter's staging tables,
-- this one has two potential writers (DeviceSubscriptionService and, later, the
-- polling service), so the merge scopes every operation to a BatchId and DELETEs
-- only its own rows rather than TRUNCATEing the table.
--
-- Removing TRUNCATE also removed the pattern's only self-healing step, so
-- StagedDateTimeUtc exists to let the service sweep orphaned batches left behind by
-- a crash between staging and merge.
CREATE TABLE gda."stg_DeviceSubscriptions" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "BatchId" uuid NOT NULL,
    -- Both values are legal, so a wrong one is silent rather than an error; the constraint at least closes the set.
    "OperationType" smallint NOT NULL CONSTRAINT "stg_DeviceSubscriptions_OperationType_check" CHECK ("OperationType" IN (0, 1)),
    "Ordinal" bigint NOT NULL,
    "OriginalQueueId" bigint NULL,
    "ThirdPartyId" varchar(50) NULL,
    "IdentifierType" varchar(20) NULL,
    "GeotabSerialNumber" varchar(50) NULL,
    "SubPlan" varchar(50) NULL,
    "SourceStatus" varchar(30) NULL,
    "EffectiveDateTimeUtc" timestamp without time zone NULL,
    "TerminationReason" varchar(200) NULL,
    "SourceReference" varchar(500) NULL,
    "StagedDateTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

CREATE INDEX "IX_stg_DeviceSubscriptions_BatchId"
ON gda."stg_DeviceSubscriptions" ("BatchId");

CREATE INDEX "IX_stg_DeviceSubscriptions_StagedDateTimeUtc"
ON gda."stg_DeviceSubscriptions" ("StagedDateTimeUtc");


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: DeviceSubscriptionHistory
--
-- Append-only audit. Written per APPLIED INSTRUCTION, not per state transition -
-- a transition-only table loses superseded instructions and plan changes, which are
-- two of the things it exists to record.
--
-- OperationType: 0 = Terminate, 1 = Subscribe
-- Outcome:       0 = Applied, 1 = NoOp, 2 = Superseded, 3 = Rejected
CREATE TABLE gda."DeviceSubscriptionHistory" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ThirdPartyId" varchar(50) NULL,
    "IdentifierType" varchar(20) NULL,
    "GeotabSerialNumber" varchar(50) NULL,
    "SubPlan" varchar(50) NULL,
    "SourceStatus" varchar(30) NULL,
    "OperationType" smallint NOT NULL,
    "Outcome" smallint NOT NULL,
    "PreviousStatus" smallint NULL,
    "NewStatus" smallint NULL,
    "Ordinal" bigint NOT NULL,
    "EffectiveDateTimeUtc" timestamp without time zone NULL,
    "TerminationReason" varchar(200) NULL,
    "AppliedDateTimeUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
    "SourceReference" varchar(500) NULL
);

CREATE INDEX "IX_DeviceSubscriptionHistory_ThirdPartyId"
ON gda."DeviceSubscriptionHistory" ("ThirdPartyId", "AppliedDateTimeUtc" DESC);

-- Read by the termination brake's rolling window on every merge. Without it the
-- window scan is the merge's hot spot.
CREATE INDEX "IX_DeviceSubscriptionHistory_Terminations"
ON gda."DeviceSubscriptionHistory" ("AppliedDateTimeUtc")
WHERE "NewStatus" = 0;

-- Read by the merge's pass 3b for every staged Subscribe: the highest ordinal of a
-- Terminate already merged for an identifier, and for a serial number. Filtered to
-- the Applied and NoOp Terminates pass 3b reads, so Subscribes and Superseded and
-- Rejected instructions stay out of both. They still grow by one row for every such
-- Terminate merged, re-sent ones included.
CREATE INDEX "IX_DeviceSubscriptionHistory_TerminateByIdentifier"
ON gda."DeviceSubscriptionHistory" ("ThirdPartyId", "Ordinal")
WHERE "OperationType" = 0 AND "Outcome" IN (0, 1);

CREATE INDEX "IX_DeviceSubscriptionHistory_TerminateBySerial"
ON gda."DeviceSubscriptionHistory" ("GeotabSerialNumber", "Ordinal")
WHERE "OperationType" = 0 AND "Outcome" IN (0, 1);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: DeviceSubscriptionGuardState
--
-- Single-row table holding the termination brake's tripped state, following the
-- DIGInvalidRecordsCursor precedent. The trip is STICKY until an operator clears
-- it - without persistence, a restart would silently re-run the reconciliation the
-- brake just refused.
--
-- NOTE: this table is written by the CALLER, not by spMerge_stg_DeviceSubscriptions.
-- A PostgreSQL function runs inside the caller's transaction and cannot commit
-- independently, so the merge cannot persist a trip that survives its own rollback.
-- Instead the merge evaluates the brake BEFORE applying anything, applies nothing,
-- and returns a sentinel; the caller then writes this row in its own transaction.
-- There is nothing to roll back because nothing was applied.
CREATE TABLE gda."DeviceSubscriptionGuardState" (
    "id" smallint NOT NULL DEFAULT 1 CHECK ("id" = 1) PRIMARY KEY,
    "IsTripped" boolean NOT NULL DEFAULT false,
    "TrippedDateTimeUtc" timestamp without time zone NULL,
    "TrippedReason" varchar(500) NULL,
    "TrippedTerminationCount" integer NULL,
    "TrippedWindowCount" integer NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC')
);

-- Insert the initial (untripped) guard row.
INSERT INTO gda."DeviceSubscriptionGuardState"
    ("id", "IsTripped", "RecordLastChangedUtc")
VALUES (1, false, (now() AT TIME ZONE 'UTC'));

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spClaimQDeviceSubscriptionsBatch
--
-- Atomically claims and retrieves a batch of Q_DeviceSubscriptions records for
-- processing. Retarget of spClaimQProvisionDevicesBatch with three deliberate
-- divergences:
--
--   1. ORDER BY "OrderingOrdinal", "id" rather than ORDER BY "RecordCreationTimeUtc".
--      now() is transaction-start time in PostgreSQL and does not advance within a
--      transaction, so every row of a multi-row INSERT carries an identical value.
--      That is harmless for an insert-only, order-independent queue; it is fatal for
--      a queue carrying Subscribe AND Terminate for the same key.
--   2. A ClaimId column. Returning the batch from the UPDATE itself is NOT a
--      divergence any more - all thirteen claim functions do that, and the twelve were
--      rewritten to in 5.2.0.0. ClaimId is stamped here and nowhere else, for
--      traceability.
--   3. A RetryCount cap. Nothing in the codebase reads RetryCount today, so a
--      permanently-failing row is reclaimed forever. Over-cap rows are moved to
--      Q_DeviceSubscriptionsFail before claiming.
--
-- Parameters:
--   p_batch_size: Maximum number of records to claim in a single batch.
--   p_stale_threshold_minutes: Minutes after which an "in progress" record is
--                              considered stale and eligible for reclaim.
--   p_max_claim_attempts: RetryCount ceiling. Rows at or above it are failed out.
-- Returns: The claimed Q_DeviceSubscriptions records, in application order.
CREATE OR REPLACE FUNCTION gda."spClaimQDeviceSubscriptionsBatch"(
    p_batch_size integer,
    p_stale_threshold_minutes integer,
    p_max_claim_attempts integer
)
RETURNS SETOF gda."Q_DeviceSubscriptions"
LANGUAGE plpgsql
AS $$
DECLARE
    v_processing_start_time_utc timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_stale_threshold timestamp without time zone := (now() AT TIME ZONE 'UTC') - (p_stale_threshold_minutes * interval '1 minute');
    v_claim_id uuid := gen_random_uuid();
BEGIN
    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever.
    -- Bounded by p_batch_size, like every other statement in this function. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to p_batch_size - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. DELETE accepts no LIMIT, so the bound is a subquery on the primary key, and that
    -- subquery takes its rows ORDER BY "id", then FOR UPDATE SKIP LOCKED.
    --
    -- Both of those are for LOCK ORDER, not for choosing which rows qualify - every qualifying
    -- row has the same destination, so the ordering key is simply the primary key, which is also
    -- the key of the queue's partial _Exhausted index. (The claim below takes its rows in its own
    -- order; it is SKIP LOCKED in both statements, not a shared order, that keeps the sweep and
    -- the claim from waiting on each other.) Without them two concurrent sweeps take an
    -- overlapping set in
    -- different orders and block on each other's rows instead of skipping them, which is a
    -- deadlock cycle: measured here in five of eight runs at four and eight claimers, and only
    -- ever with the sweep actually deleting. Deadlocks are retried by DatabaseResilienceHelper,
    -- so they present as latency rather than as errors, and the larger cost is in fact
    -- throughput - eight colliding sweeps banked about a sixth of a dense backlog per pass
    -- against all of it once each takes a distinct set, and a backed-up queue is precisely when
    -- this statement matters. Nothing is lost to SKIP LOCKED: a row another sweep is holding is
    -- left for the next pass, which is the same bargain the bound above already makes. The SQL
    -- Server sibling needed no such hint while its sweep scanned the queue - raced at both
    -- concurrencies it showed no deadlock - but it takes READPAST now that the sweep seeks its
    -- _Exhausted index instead; see the comment there.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply "ProcessingStatus" = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with "RetryCount" + 1;
    -- skipping every claimed row would mean it is never swept, which is the infinite retry this
    -- ceiling exists to end. Mirroring the claim's own predicate removes abandoned rows and
    -- protects live ones.
    --
    -- The literal "RetryCount" > 0 changes no result - the ceiling is never below 1. It is there
    -- so the planner can use the queue's partial _Exhausted index (WHERE "RetryCount" > 0)
    -- whatever plan it caches for this function: a generic plan cannot prove that
    -- "RetryCount" >= p_max_claim_attempts implies it for every parameter value, and would then
    -- read the whole queue before every claim - in the healthy case, where nothing qualifies,
    -- most of all.
    IF p_max_claim_attempts IS NOT NULL THEN
        WITH exhausted AS (
            DELETE FROM gda."Q_DeviceSubscriptions" q
            WHERE q."id" IN (
                SELECT q2."id"
                FROM gda."Q_DeviceSubscriptions" q2
                WHERE q2."RetryCount" >= p_max_claim_attempts
                  AND q2."RetryCount" > 0
                  AND (q2."ProcessingStatus" = 0
                       OR q2."ProcessingStartTimeUtc" < v_stale_threshold)
                ORDER BY q2."id"
                LIMIT p_batch_size
                FOR UPDATE SKIP LOCKED
            )
            RETURNING q.*
        )
        INSERT INTO gda."Q_DeviceSubscriptionsFail" (
            "OriginalQueueId", "OriginalOrderingOrdinal", "OperationType", "ThirdPartyId",
            "IdentifierType", "GeotabSerialNumber", "SubPlan", "SourceStatus",
            "EffectiveDateTimeUtc", "TerminationReason", "SourceReference",
            "OriginalRecordCreationTimeUtc", "OriginalRecordLastChangedUtc", "FailureReason")
        SELECT e."id", e."OrderingOrdinal", e."OperationType", e."ThirdPartyId",
               e."IdentifierType", e."GeotabSerialNumber", e."SubPlan", e."SourceStatus",
               e."EffectiveDateTimeUtc", e."TerminationReason", e."SourceReference",
               e."RecordCreationTimeUtc", e."RecordLastChangedUtc",
               'Exceeded ' || p_max_claim_attempts || ' claim attempts.'
        FROM exhausted e;
    END IF;

    RETURN QUERY
    WITH claimed AS (
        SELECT q2."id"
        FROM gda."Q_DeviceSubscriptions" q2
        WHERE q2."ProcessingStatus" = 0
           OR (q2."ProcessingStatus" = 1 AND q2."ProcessingStartTimeUtc" < v_stale_threshold)
        ORDER BY q2."OrderingOrdinal", q2."id"
        LIMIT p_batch_size
        FOR UPDATE SKIP LOCKED
    ),
    updated AS (
        UPDATE gda."Q_DeviceSubscriptions" q
        SET "ProcessingStatus" = 1,
            "ProcessingStartTimeUtc" = v_processing_start_time_utc,
            "ClaimId" = v_claim_id,
            "RetryCount" = CASE WHEN q."ProcessingStatus" = 1 THEN q."RetryCount" + 1 ELSE q."RetryCount" END
        FROM claimed c
        WHERE q."id" = c."id"
        RETURNING q.*
    )
    SELECT * FROM updated ORDER BY "OrderingOrdinal", "id";
END;
$$;

ALTER FUNCTION gda."spClaimQDeviceSubscriptionsBatch"(integer, integer, integer)
    OWNER TO geotabdigadapter_client;

GRANT EXECUTE ON FUNCTION gda."spClaimQDeviceSubscriptionsBatch"(integer, integer, integer)
    TO geotabdigadapter_client;

REVOKE ALL ON FUNCTION gda."spClaimQDeviceSubscriptionsBatch"(integer, integer, integer)
    FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spMerge_stg_DeviceSubscriptions
CREATE OR REPLACE FUNCTION gda."spMerge_stg_DeviceSubscriptions"(
    p_batch_id uuid,
    p_mode smallint,
    p_snapshot_ordinal bigint DEFAULT NULL,
    p_expected_identifier_types text DEFAULT 'Vin',
    p_max_terminations_per_window integer DEFAULT NULL,
    p_termination_window_minutes integer DEFAULT 60
)
RETURNS TABLE (
    "ResultCode" smallint,
    "StagingId" bigint,
    "QueueId" bigint,
    "QueueOrdinal" bigint,
    "Identifier" varchar(50),
    "SerialNumber" varchar(50),
    "ResultMessage" text
)
LANGUAGE plpgsql
AS $$
-- ==========================================================================================
-- Description:
--		Upserts records from the stg_DeviceSubscriptions staging table to the
--		DeviceSubscriptions table and then clears the batch's staging rows. This is the only
--		writer to DeviceSubscriptions; both the queue-driven intake and any snapshot-driven
--		intake converge here so that the two cannot disagree on identical data.
--
--		In Delta mode (p_mode = 0), rows are applied as individual Subscribe and Terminate
--		instructions and rows absent from the batch are left untouched. In Snapshot mode
--		(p_mode = 1), the batch is the complete subscribed set: rows present are upserted to
--		Active and every Active row absent from the batch is set to 0 (Terminated), following
--		the SetEntityStatusDeletedForMissing… semantics used by the other merge procedures.
--
--		p_expected_identifier_types is a comma-separated allow-list. A scalar would forbid the
--		mixed-identifier fleet that IdentifierType exists to support, and would also miss a
--		fleet-wide identifier-type change - the day the source starts sending Imei for devices
--		previously sent as Vin, every device becomes a new ThirdPartyId, which is a full fleet
--		of subscribes AND a full fleet of terminations.
--
-- Returns one row per rejected staged row plus a final summary row, discriminated by
-- ResultCode:
--		0 = Completed          (summary row; ResultMessage carries counts)
--		1 = RowRejected        (caller writes the row to Q_DeviceSubscriptionsFail)
--		2 = BrakeWouldTrip     (nothing applied; caller persists the guard state)
--		3 = EmptySnapshotRefused (nothing applied)
--		4 = GuardAlreadyTripped  (nothing applied)
--
-- Notes:
--		- No transaction used as application should manage the transaction. The application
--		  owns the unit of work spanning stage -> merge -> queue-delete.
--		- The output column names deliberately avoid matching any table column name. A
--		  RETURNS TABLE column is a PL/pgSQL variable, and an ON CONFLICT target cannot be
--		  alias-qualified, so an output column called "ThirdPartyId" would make the upsert
--		  below fail as ambiguous.
-- ==========================================================================================
DECLARE
    v_now timestamp without time zone := (now() AT TIME ZONE 'UTC');
    v_is_tripped boolean;
    v_staged_count integer;
    v_allowed_types text[];
    v_proposed_terminations integer := 0;
    v_window_terminations integer := 0;
    v_subscribed_count integer := 0;
    v_terminated_count integer := 0;
    v_rejected_count integer := 0;
BEGIN
    -- Defence in depth. The ordinal high-water mark is what makes concurrent merges
    -- CORRECT; this lock is what keeps them cheap, by avoiding pointless conflict.
    PERFORM pg_advisory_xact_lock(hashtext('gda.spMerge_stg_DeviceSubscriptions'));

    IF p_mode NOT IN (0, 1) THEN
        RAISE EXCEPTION 'p_mode must be 0 (Delta) or 1 (Snapshot); received %.', p_mode;
    END IF;

    IF p_mode = 1 AND p_snapshot_ordinal IS NULL THEN
        RAISE EXCEPTION 'p_snapshot_ordinal is required when p_mode = 1 (Snapshot).';
    END IF;

    ------------------------------------------------------------------------------
    -- Pass 0a: refuse outright if the brake is already tripped. Sticky until an
    -- operator clears gda.DeviceSubscriptionGuardState.
    ------------------------------------------------------------------------------
    SELECT g."IsTripped" INTO v_is_tripped
    FROM gda."DeviceSubscriptionGuardState" g WHERE g."id" = 1;

    IF COALESCE(v_is_tripped, false) THEN
        RETURN QUERY SELECT 4::smallint, NULL::bigint, NULL::bigint, NULL::bigint,
            NULL::varchar(50), NULL::varchar(50),
            'Termination guard is tripped; no changes applied. Clear gda."DeviceSubscriptionGuardState" to resume.'::text;
        RETURN;
    END IF;

    SELECT COUNT(*) INTO v_staged_count
    FROM gda."stg_DeviceSubscriptions" s WHERE s."BatchId" = p_batch_id;

    ------------------------------------------------------------------------------
    -- Pass 0b: zero staged rows in snapshot mode is a HARD ERROR, not "everything is
    -- absent". The merge deletes its own staging rows as its last act, so if a commit
    -- ACK is lost and the call is retried, the retry sees zero rows for this BatchId.
    -- Treating that as a full absence would wipe the fleet. This check lives here
    -- rather than in the caller precisely because the caller is the thing retrying.
    ------------------------------------------------------------------------------
    IF p_mode = 1 AND v_staged_count = 0 THEN
        RETURN QUERY SELECT 3::smallint, NULL::bigint, NULL::bigint, NULL::bigint,
            NULL::varchar(50), NULL::varchar(50),
            'Snapshot mode invoked with zero staged rows for the supplied BatchId; refusing to reconcile. This is the signature of a retried merge whose staging rows were already consumed.'::text;
        RETURN;
    END IF;

    IF v_staged_count = 0 THEN
        RETURN QUERY SELECT 0::smallint, NULL::bigint, NULL::bigint, NULL::bigint,
            NULL::varchar(50), NULL::varchar(50),
            'No staged rows for the supplied BatchId; nothing to do.'::text;
        RETURN;
    END IF;

    SELECT array_agg(btrim(upper(t)))
    INTO v_allowed_types
    FROM unnest(string_to_array(COALESCE(p_expected_identifier_types, 'Vin'), ',')) AS t
    WHERE btrim(t) <> '';

    ------------------------------------------------------------------------------
    -- Pass 1: materialise, trim, and normalise this batch.
    -- Trim only. NEVER case-fold: casing can be the only thing separating two valid
    -- identifiers, and the pinned collation on SQL Server makes comparison
    -- case-sensitive on both providers by design.
    ------------------------------------------------------------------------------
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT
        s."id",
        s."OperationType",
        s."Ordinal",
        s."OriginalQueueId",
        NULLIF(btrim(s."ThirdPartyId"), '')       AS "ThirdPartyId",
        NULLIF(btrim(s."IdentifierType"), '')     AS "IdentifierType",
        NULLIF(btrim(s."GeotabSerialNumber"), '') AS "GeotabSerialNumber",
        s."SubPlan",
        s."SourceStatus",
        s."EffectiveDateTimeUtc",
        s."TerminationReason",
        s."SourceReference"
    FROM gda."stg_DeviceSubscriptions" s
    WHERE s."BatchId" = p_batch_id;
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("ThirdPartyId");
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("GeotabSerialNumber");

    DROP TABLE IF EXISTS "TMP_Rejected";
    CREATE TEMP TABLE "TMP_Rejected" (
        "id" bigint, "OperationType" smallint, "Ordinal" bigint, "OriginalQueueId" bigint,
        "ThirdPartyId" varchar(50), "IdentifierType" varchar(20), "GeotabSerialNumber" varchar(50),
        "SubPlan" varchar(50), "SourceStatus" varchar(30),
        "EffectiveDateTimeUtc" timestamp without time zone, "TerminationReason" varchar(200),
        "SourceReference" varchar(500), "Reason" text
    );

    DROP TABLE IF EXISTS "TMP_Superseded";
    CREATE TEMP TABLE "TMP_Superseded" (LIKE "TMP_Rejected" INCLUDING DEFAULTS);

    ------------------------------------------------------------------------------
    -- Pass 1b: resolve serial-keyed Terminates.
    -- The termination channel is not guaranteed to carry the same identifier the
    -- activation did (it may carry an IMEI where the activation carried a VIN, or no
    -- identifier at all), so GeotabSerialNumber is the only reliable cross-channel
    -- join key. Resolve those rows to their identifier via the Active target row.
    ------------------------------------------------------------------------------
    UPDATE "TMP_DeduplicatedStaging" t
    SET "ThirdPartyId" = d."ThirdPartyId",
        "IdentifierType" = COALESCE(t."IdentifierType", d."IdentifierType")
    FROM gda."DeviceSubscriptions" d
    WHERE t."ThirdPartyId" IS NULL
      AND t."GeotabSerialNumber" IS NOT NULL
      AND d."GeotabSerialNumber" = t."GeotabSerialNumber"
      AND d."SubscriptionStatus" = 1;

    ------------------------------------------------------------------------------
    -- Pass 1c: dedupe. spMerge_stg_Devices2 de-duplicates FIRST, and for good
    -- reason: two rows for one key raise SQLSTATE 21000 and abort the whole merge.
    -- Highest Ordinal wins, id as tiebreaker. Losers are retained as Superseded so
    -- the audit trail keeps them and their SourceReferences.
    ------------------------------------------------------------------------------
    INSERT INTO "TMP_Superseded"
    SELECT r."id", r."OperationType", r."Ordinal", r."OriginalQueueId", r."ThirdPartyId",
           r."IdentifierType", r."GeotabSerialNumber", r."SubPlan", r."SourceStatus",
           r."EffectiveDateTimeUtc", r."TerminationReason", r."SourceReference",
           'Superseded within the same batch by a higher-ordinal instruction for the same identifier.'
    FROM (
        SELECT t.*, ROW_NUMBER() OVER (PARTITION BY t."ThirdPartyId"
                                       ORDER BY t."Ordinal" DESC, t."id" DESC) AS rn
        FROM "TMP_DeduplicatedStaging" t WHERE t."ThirdPartyId" IS NOT NULL
    ) r WHERE r.rn > 1;

    DELETE FROM "TMP_DeduplicatedStaging" t USING "TMP_Superseded" s WHERE t."id" = s."id";

    -- Only a SUBSCRIBE may be superseded here. A Terminate and a Subscribe naming one
    -- serial in a single batch is the documented mis-keyed-identifier repair (README
    -- section 3.1): pass 6 clears the old pairing and pass 7 re-pairs the serial,
    -- which is precisely why pass 6 runs before the upsert. Superseding the Terminate
    -- here defeated that and applied NEITHER instruction - the Terminate was dropped
    -- as superseded, and pass 2 then rejected the Subscribe against the stored row
    -- the Terminate would have cleared.
    --
    -- Terminates still take part in the RANKING, so a Terminate that is the
    -- highest-ordinal row for a serial still supersedes a lower-ordinal Subscribe.
    -- They are exempt only from BEING superseded.
    INSERT INTO "TMP_Superseded"
    SELECT r."id", r."OperationType", r."Ordinal", r."OriginalQueueId", r."ThirdPartyId",
           r."IdentifierType", r."GeotabSerialNumber", r."SubPlan", r."SourceStatus",
           r."EffectiveDateTimeUtc", r."TerminationReason", r."SourceReference",
           'Superseded within the same batch by a higher-ordinal instruction for the same serial number.'
    FROM (
        SELECT t.*, ROW_NUMBER() OVER (PARTITION BY t."GeotabSerialNumber"
                                       ORDER BY t."Ordinal" DESC, t."id" DESC) AS rn
        FROM "TMP_DeduplicatedStaging" t WHERE t."GeotabSerialNumber" IS NOT NULL
    ) r WHERE r.rn > 1 AND r."OperationType" = 1;

    DELETE FROM "TMP_DeduplicatedStaging" t USING "TMP_Superseded" s WHERE t."id" = s."id";

    ------------------------------------------------------------------------------
    -- Pass 2: validation. This lives in the merge rather than in the service so that
    -- the delta intake and the polling intake cannot disagree on identical data.
    ------------------------------------------------------------------------------

    -- Snapshot mode must NOT ignore status. A device returned as anything other than
    -- Subscribed must not be activated - that is over-transmission, which the brake
    -- by construction cannot catch because it counts terminations.
    IF p_mode = 1 THEN
        INSERT INTO "TMP_Rejected"
        SELECT t.*, 'Snapshot row carries SourceStatus ' || COALESCE(t."SourceStatus", '(null)')
                    || '; only Subscribed rows may be activated.'
        FROM "TMP_DeduplicatedStaging" t
        WHERE COALESCE(t."SourceStatus", 'Subscribed') <> 'Subscribed';
        DELETE FROM "TMP_DeduplicatedStaging" t USING "TMP_Rejected" r WHERE t."id" = r."id";
    END IF;

    -- Subscribe with no serial: nothing to transmit as.
    INSERT INTO "TMP_Rejected"
    SELECT t.*, 'Subscribe instruction has no GeotabSerialNumber.'
    FROM "TMP_DeduplicatedStaging" t
    WHERE t."OperationType" = 1 AND t."GeotabSerialNumber" IS NULL;
    DELETE FROM "TMP_DeduplicatedStaging" t USING "TMP_Rejected" r WHERE t."id" = r."id";

    -- Subscribe with no identifier and no resolvable one.
    INSERT INTO "TMP_Rejected"
    SELECT t.*, 'Subscribe instruction has no ThirdPartyId.'
    FROM "TMP_DeduplicatedStaging" t
    WHERE t."OperationType" = 1 AND t."ThirdPartyId" IS NULL;
    DELETE FROM "TMP_DeduplicatedStaging" t USING "TMP_Rejected" r WHERE t."id" = r."id";

    -- Identifier type outside the allow-list: fail closed, reconcile nothing.
    --
    -- An allow-list that resolves to NO entries means "not enforced", which is what
    -- the v_allowed_types IS NOT NULL test below is for. Rejecting every typed row
    -- instead would turn a blank setting into a fleet-wide outage while still
    -- accepting every untyped row, which is not a coherent posture. Reachable only
    -- by a whitespace- or comma-only setting or by invoking this directly, because
    -- the configuration layer substitutes the default for an empty value.
    --
    -- The SQL Server procedure must agree with this exactly. The two diverged here
    -- once: SQL Server had no emptiness test, and NOT IN over an empty set is true
    -- for every row, so the same setting rejected everything there.
    INSERT INTO "TMP_Rejected"
    SELECT t.*, 'IdentifierType ' || t."IdentifierType" || ' is not in the configured allow-list ('
                || p_expected_identifier_types || ').'
    FROM "TMP_DeduplicatedStaging" t
    WHERE t."IdentifierType" IS NOT NULL
      AND v_allowed_types IS NOT NULL
      AND upper(t."IdentifierType") <> ALL (v_allowed_types);
    DELETE FROM "TMP_DeduplicatedStaging" t USING "TMP_Rejected" r WHERE t."id" = r."id";

    -- Serial already belongs to a DIFFERENT ACTIVE identifier. Scoped to Active rows
    -- to match the filtered unique index; an unscoped test would reject the
    -- legitimate terminate-then-reprovision re-pairing.
    --
    -- The NOT EXISTS exempts a stored Active row that a Terminate SURVIVING THIS
    -- BATCH will clear in pass 6. That is what makes the same-batch mis-keyed
    -- identifier repair work; without it the Subscribe is rejected against a row
    -- that no longer exists by the time pass 7 runs.
    --
    -- Its three predicates mirror, exactly, the three tests that stand between a
    -- staged Terminate and pass 6. THEY ARE LOAD-BEARING, because this check runs
    -- BEFORE all of them. Exempt a Subscribe on the strength of a Terminate that is
    -- then discarded, and pass 7 inserts against a still-Active row, raising a
    -- filtered-unique-index violation that aborts the WHOLE MERGE - worse than the
    -- silent no-op this fixes.
    --    Ordinal > LastAppliedOrdinal   mirrors pass 3, the order filter.
    --    serial IS NULL OR = stored     mirrors the contradiction check below.
    --    p_mode = 0                     mirrors pass 6, which applies staged
    --                                   Terminates in delta mode only.
    -- If any of those three changes, this must change with it.
    --
    -- SNAPSHOT MODE IS KNOWINGLY NOT COVERED, and the p_mode test is what confines
    -- this exemption to the case it was reasoned about. Snapshot mode clears a
    -- stored row by ABSENCE from the batch rather than by a staged Terminate, so
    -- the same repair shape is still rejected there: a fleet re-keyed to a new
    -- identifier type - the case this function's own header names - would have
    -- every Subscribe rejected against the row the snapshot was about to
    -- terminate. Nothing invokes snapshot mode today; DeviceSubscriptionService
    -- merges in delta mode only. Phase E owes a SECOND exemption mirroring pass 6's
    -- snapshot predicate the way this one mirrors pass 3, and must not simply widen
    -- this one, whose other two predicates are meaningless for an absent row.
    INSERT INTO "TMP_Rejected"
    SELECT t.*, 'GeotabSerialNumber ' || t."GeotabSerialNumber"
                || ' is already assigned to active subscription ' || d."ThirdPartyId" || '.'
    FROM "TMP_DeduplicatedStaging" t
    JOIN gda."DeviceSubscriptions" d
      ON d."GeotabSerialNumber" = t."GeotabSerialNumber"
     AND d."SubscriptionStatus" = 1
     AND d."ThirdPartyId" <> t."ThirdPartyId"
    WHERE t."OperationType" = 1
      AND NOT EXISTS (
            SELECT 1
            FROM "TMP_DeduplicatedStaging" t2
            WHERE p_mode = 0
              AND t2."OperationType" = 0
              AND t2."ThirdPartyId" = d."ThirdPartyId"
              AND t2."Ordinal" > d."LastAppliedOrdinal"
              AND (t2."GeotabSerialNumber" IS NULL
                   OR t2."GeotabSerialNumber" = d."GeotabSerialNumber"));
    DELETE FROM "TMP_DeduplicatedStaging" t USING "TMP_Rejected" r WHERE t."id" = r."id";

    -- Terminate whose supplied serial contradicts the stored pairing.
    INSERT INTO "TMP_Rejected"
    SELECT t.*, 'Terminate instruction supplies GeotabSerialNumber ' || t."GeotabSerialNumber"
                || ' but the stored subscription holds ' || d."GeotabSerialNumber" || '.'
    FROM "TMP_DeduplicatedStaging" t
    JOIN gda."DeviceSubscriptions" d ON d."ThirdPartyId" = t."ThirdPartyId"
    WHERE t."OperationType" = 0
      AND t."GeotabSerialNumber" IS NOT NULL
      AND d."GeotabSerialNumber" <> t."GeotabSerialNumber";
    DELETE FROM "TMP_DeduplicatedStaging" t USING "TMP_Rejected" r WHERE t."id" = r."id";

    ------------------------------------------------------------------------------
    -- Pass 3: order filter. Discard anything at or below the target row's
    -- high-water mark. This is what makes a replayed or out-of-order instruction a
    -- no-op instead of a state change, and it is what stops a stale snapshot from
    -- terminating a device a fresher delta just subscribed.
    ------------------------------------------------------------------------------
    INSERT INTO "TMP_Superseded"
    SELECT t.*, 'Ordinal ' || t."Ordinal" || ' is at or below the already-applied ordinal '
                || d."LastAppliedOrdinal" || ' for this subscription.'
    FROM "TMP_DeduplicatedStaging" t
    JOIN gda."DeviceSubscriptions" d ON d."ThirdPartyId" = t."ThirdPartyId"
    WHERE t."Ordinal" <= d."LastAppliedOrdinal";
    DELETE FROM "TMP_DeduplicatedStaging" t
    USING "TMP_Superseded" s WHERE t."id" = s."id";

    ------------------------------------------------------------------------------
    -- Pass 3b: the order filter for a Subscribe, against every Terminate already
    -- merged - including one that changed nothing.
    --
    -- A Terminate raises LastAppliedOrdinal only on the Active row it terminates
    -- (pass 6). One that finds nothing Active - the identifier already terminated,
    -- an identifier with no row, or a serial number no Active row holds - is
    -- recorded in history as NoOp and raises no mark at all. Without this pass an
    -- OLDER Subscribe merged after it would pass pass 3 and reactivate the device
    -- its newer Terminate had said to stop. That order does not need two instances:
    -- a batch the guard refused, or one claimed by a process that died before
    -- committing, stays claimed for 30 minutes while newer instructions are claimed
    -- and merged at once.
    --
    -- History is the one place every merged Terminate leaves its ordinal, so it is
    -- read here as the mark such a Terminate would have set. Matching the serial
    -- number as well as the identifier covers a serial-keyed Terminate, which may
    -- name no identifier of its own, and a Subscribe pairing a different identifier
    -- with a serial number that a newer Terminate cleared.
    --
    -- Which Terminates count for the serial number follows from what each would
    -- have done had it been merged in ordinal order. One that terminated a
    -- subscription cleared that subscription's serial number, which pass 8 records
    -- even when the instruction named none. A serial-keyed one that found nothing
    -- Active would have terminated whichever identifier held the serial number. One
    -- keyed on an identifier that found nothing Active could only ever have
    -- terminated that identifier, so it does not count for a serial number it names:
    -- termination lists are re-sent, and such a Terminate for a device's former
    -- owner would otherwise supersede a Subscribe pairing the serial number with its
    -- new owner.
    --
    -- Subscribes only. A Terminate removed here would have changed nothing anyway,
    -- and removing only Subscribes keeps pass 2's exemption, which mirrors what
    -- reaches pass 6, exact.
    ------------------------------------------------------------------------------
    INSERT INTO "TMP_Superseded"
    SELECT t.*, 'Ordinal ' || t."Ordinal" || ' is at or below the ordinal ' || h."TerminateOrdinal"
                || ' of a Terminate already merged for this identifier or serial number.'
    FROM "TMP_DeduplicatedStaging" t
    CROSS JOIN LATERAL (
        SELECT GREATEST(
            (SELECT MAX(x."Ordinal") FROM gda."DeviceSubscriptionHistory" x
             WHERE x."ThirdPartyId" = t."ThirdPartyId"
               AND x."OperationType" = 0 AND x."Outcome" IN (0, 1)),
            (SELECT MAX(x."Ordinal") FROM gda."DeviceSubscriptionHistory" x
             WHERE x."GeotabSerialNumber" = t."GeotabSerialNumber"
               AND x."OperationType" = 0 AND x."Outcome" IN (0, 1)
               AND (x."Outcome" = 0 OR x."ThirdPartyId" IS NULL))) AS "TerminateOrdinal"
    ) h
    WHERE t."OperationType" = 1
      AND t."Ordinal" <= h."TerminateOrdinal";
    DELETE FROM "TMP_DeduplicatedStaging" t
    USING "TMP_Superseded" s WHERE t."id" = s."id";

    ------------------------------------------------------------------------------
    -- Pass 4: the brake, evaluated BEFORE anything is applied.
    --
    -- A rolling window rather than a per-call cap: a per-call cap is defeated by
    -- BatchSize chunking (cap 50, batch size 100, four batches terminate 200).
    -- Active in BOTH modes - delta is the mode that ships first, and it is the one
    -- fed by hand-written INSERT statements.
    --
    -- On trip nothing is applied and the caller persists the guard state. The merge
    -- cannot persist it itself: a PostgreSQL function shares the caller's
    -- transaction, so a trip row it wrote would be rolled back along with everything
    -- else. Evaluating first and applying nothing removes the need for a rollback.
    ------------------------------------------------------------------------------
    IF p_mode = 0 THEN
        SELECT COUNT(*) INTO v_proposed_terminations
        FROM "TMP_DeduplicatedStaging" t
        JOIN gda."DeviceSubscriptions" d ON d."ThirdPartyId" = t."ThirdPartyId"
        WHERE t."OperationType" = 0 AND d."SubscriptionStatus" = 1;
    ELSE
        SELECT COUNT(*) INTO v_proposed_terminations
        FROM gda."DeviceSubscriptions" d
        WHERE d."SubscriptionStatus" = 1
          AND d."LastAppliedOrdinal" < p_snapshot_ordinal
          AND NOT EXISTS (SELECT 1 FROM "TMP_DeduplicatedStaging" t WHERE t."ThirdPartyId" = d."ThirdPartyId");
    END IF;

    IF p_max_terminations_per_window IS NOT NULL AND v_proposed_terminations > 0 THEN
        SELECT COUNT(*) INTO v_window_terminations
        FROM gda."DeviceSubscriptionHistory" h
        WHERE h."NewStatus" = 0
          AND h."AppliedDateTimeUtc" >= v_now - (p_termination_window_minutes * interval '1 minute');

        IF (v_window_terminations + v_proposed_terminations) > p_max_terminations_per_window THEN
            -- QueueId carries the rolling-window count and QueueOrdinal the proposed
            -- count. Neither has a queue row to refer to on a refusal, and the caller
            -- needs both numbers to record the trip. Keep in step with SQL Server.
            RETURN QUERY SELECT 2::smallint, NULL::bigint, v_window_terminations::bigint,
                v_proposed_terminations::bigint, NULL::varchar(50), NULL::varchar(50),
                ('Termination brake: this batch would terminate ' || v_proposed_terminations
                 || ' subscription(s); ' || v_window_terminations || ' already terminated in the last '
                 || p_termination_window_minutes || ' minute(s); configured maximum is '
                 || p_max_terminations_per_window || '. Nothing applied.')::text;

            -- Drop temporary tables.
            DROP TABLE "TMP_DeduplicatedStaging";
            DROP TABLE "TMP_Rejected";
            DROP TABLE "TMP_Superseded";
            RETURN;
        END IF;
    END IF;

    ------------------------------------------------------------------------------
    -- Pass 5: capture the pre-image BEFORE any mutation.
    -- PostgreSQL's ON CONFLICT ... RETURNING yields the NEW tuple, so PreviousStatus
    -- is not recoverable from the upsert. SQL Server's MERGE ... OUTPUT deleted.*
    -- would supply it, but both providers capture into a temp table so the two
    -- procedures keep the same control flow.
    ------------------------------------------------------------------------------
    DROP TABLE IF EXISTS "TMP_PreImage";
    CREATE TEMP TABLE "TMP_PreImage" AS
    SELECT d."ThirdPartyId", d."SubscriptionStatus", d."SubPlan", d."GeotabSerialNumber"
    FROM gda."DeviceSubscriptions" d
    WHERE d."ThirdPartyId" IN (SELECT t."ThirdPartyId" FROM "TMP_DeduplicatedStaging" t)
       OR (p_mode = 1 AND d."SubscriptionStatus" = 1);

    ------------------------------------------------------------------------------
    -- Pass 6: TERMINATION PASS - runs BEFORE the upsert pass.
    -- Required by the filtered unique serial index: a serial being re-paired in the
    -- same batch is still rejected by the index if the upsert runs first.
    ------------------------------------------------------------------------------
    IF p_mode = 0 THEN
        UPDATE gda."DeviceSubscriptions" d
        SET "SubscriptionStatus" = 0,
            "TerminatedDateTimeUtc" = v_now,
            "TerminationReason" = COALESCE(t."TerminationReason", d."TerminationReason"),
            "LastAppliedOrdinal" = t."Ordinal",
            "RecordLastChangedUtc" = v_now
        FROM "TMP_DeduplicatedStaging" t
        WHERE t."OperationType" = 0
          AND d."ThirdPartyId" = t."ThirdPartyId"
          AND d."SubscriptionStatus" = 1;
        GET DIAGNOSTICS v_terminated_count = ROW_COUNT;
    ELSE
        -- Snapshot: terminate every Active row absent from the batch whose high-water
        -- mark predates this snapshot. The LastAppliedOrdinal comparison is the
        -- ordering predicate; it is inert until a polling service exists, because
        -- nothing else invokes snapshot mode.
        UPDATE gda."DeviceSubscriptions" d
        SET "SubscriptionStatus" = 0,
            "TerminatedDateTimeUtc" = v_now,
            "TerminationReason" = 'Absent from subscription snapshot.',
            "LastAppliedOrdinal" = p_snapshot_ordinal,
            "RecordLastChangedUtc" = v_now
        WHERE d."SubscriptionStatus" = 1
          AND d."LastAppliedOrdinal" < p_snapshot_ordinal
          AND NOT EXISTS (SELECT 1 FROM "TMP_DeduplicatedStaging" t WHERE t."ThirdPartyId" = d."ThirdPartyId");
        GET DIAGNOSTICS v_terminated_count = ROW_COUNT;
    END IF;

    ------------------------------------------------------------------------------
    -- Pass 7: UPSERT PASS.
    ------------------------------------------------------------------------------
    INSERT INTO gda."DeviceSubscriptions" AS d (
        "ThirdPartyId", "IdentifierType", "GeotabSerialNumber", "SubscriptionStatus",
        "SubPlan", "LastAppliedOrdinal", "SubscribedDateTimeUtc", "LastSeenInSnapshotUtc",
        "RecordCreationTimeUtc", "RecordLastChangedUtc")
    SELECT t."ThirdPartyId", COALESCE(t."IdentifierType", 'Vin'), t."GeotabSerialNumber", 1,
           t."SubPlan", t."Ordinal", v_now,
           CASE WHEN p_mode = 1 THEN v_now ELSE NULL END, v_now, v_now
    FROM "TMP_DeduplicatedStaging" t
    WHERE t."OperationType" = 1
    ON CONFLICT ("ThirdPartyId") DO UPDATE
    SET "IdentifierType"        = EXCLUDED."IdentifierType",
        "GeotabSerialNumber"    = EXCLUDED."GeotabSerialNumber",
        "SubscriptionStatus"    = 1,
        "SubPlan"               = COALESCE(EXCLUDED."SubPlan", d."SubPlan"),
        "LastAppliedOrdinal"    = EXCLUDED."LastAppliedOrdinal",
        "SubscribedDateTimeUtc" = COALESCE(d."SubscribedDateTimeUtc", EXCLUDED."SubscribedDateTimeUtc"),
        "LastSeenInSnapshotUtc" = COALESCE(EXCLUDED."LastSeenInSnapshotUtc", d."LastSeenInSnapshotUtc"),
        "RecordLastChangedUtc"  = v_now;
    GET DIAGNOSTICS v_subscribed_count = ROW_COUNT;

    ------------------------------------------------------------------------------
    -- Pass 8: history, ONE ROW PER APPLIED INSTRUCTION.
    -- Transition-only history loses two things it exists to record: superseded
    -- instructions (there would be no record that a termination was ever sent) and
    -- plan changes (which alter no status and so would write nothing).
    ------------------------------------------------------------------------------

    -- Applied instructions, plus NoOp for a Terminate naming nothing Active.
    --
    -- An applied Terminate records the serial number it cleared, taken from the
    -- pre-image when the instruction named only an identifier, as the
    -- snapshot-absent rows below record theirs. Pass 3b reads it: without it, an
    -- older Subscribe pairing that serial number with a different identifier, merged
    -- after the Terminate, found nothing to stop it. A NoOp Terminate keeps the
    -- serial number it named, if any.
    INSERT INTO gda."DeviceSubscriptionHistory" (
        "ThirdPartyId", "IdentifierType", "GeotabSerialNumber", "SubPlan", "SourceStatus",
        "OperationType", "Outcome", "PreviousStatus", "NewStatus", "Ordinal",
        "EffectiveDateTimeUtc", "TerminationReason", "AppliedDateTimeUtc", "SourceReference")
    SELECT t."ThirdPartyId", t."IdentifierType",
           CASE WHEN t."OperationType" = 0 AND p."SubscriptionStatus" = 1
                THEN COALESCE(t."GeotabSerialNumber", p."GeotabSerialNumber")
                ELSE t."GeotabSerialNumber" END,
           t."SubPlan", t."SourceStatus",
           t."OperationType",
           CASE WHEN t."OperationType" = 0 AND COALESCE(p."SubscriptionStatus", 0) <> 1
                THEN 1::smallint ELSE 0::smallint END,
           p."SubscriptionStatus",
           CASE WHEN t."OperationType" = 0 AND COALESCE(p."SubscriptionStatus", 0) <> 1 THEN NULL
                WHEN t."OperationType" = 0 THEN 0::smallint
                ELSE 1::smallint END,
           t."Ordinal", t."EffectiveDateTimeUtc", t."TerminationReason", v_now, t."SourceReference"
    FROM "TMP_DeduplicatedStaging" t
    LEFT JOIN "TMP_PreImage" p ON p."ThirdPartyId" = t."ThirdPartyId";

    -- Snapshot-absent terminations have no staged instruction of their own.
    IF p_mode = 1 AND v_terminated_count > 0 THEN
        INSERT INTO gda."DeviceSubscriptionHistory" (
            "ThirdPartyId", "IdentifierType", "GeotabSerialNumber", "SubPlan",
            "OperationType", "Outcome", "PreviousStatus", "NewStatus", "Ordinal",
            "TerminationReason", "AppliedDateTimeUtc", "SourceReference")
        SELECT d."ThirdPartyId", d."IdentifierType", d."GeotabSerialNumber", d."SubPlan",
               0::smallint, 0::smallint, 1::smallint, 0::smallint, p_snapshot_ordinal,
               d."TerminationReason", v_now, 'snapshot:absent'
        FROM gda."DeviceSubscriptions" d
        WHERE d."SubscriptionStatus" = 0
          AND d."LastAppliedOrdinal" = p_snapshot_ordinal
          AND d."TerminatedDateTimeUtc" = v_now;
    END IF;

    -- Superseded and rejected instructions are recorded too, so the audit trail keeps
    -- their SourceReferences after the queue rows are deleted.
    INSERT INTO gda."DeviceSubscriptionHistory" (
        "ThirdPartyId", "IdentifierType", "GeotabSerialNumber", "SubPlan", "SourceStatus",
        "OperationType", "Outcome", "PreviousStatus", "NewStatus", "Ordinal",
        "EffectiveDateTimeUtc", "TerminationReason", "AppliedDateTimeUtc", "SourceReference")
    SELECT x."ThirdPartyId", x."IdentifierType", x."GeotabSerialNumber", x."SubPlan", x."SourceStatus",
           x."OperationType", x."Outcome", NULL, NULL, x."Ordinal",
           x."EffectiveDateTimeUtc", x."TerminationReason", v_now, x."SourceReference"
    FROM (
        SELECT s.*, 2::smallint AS "Outcome" FROM "TMP_Superseded" s
        UNION ALL
        SELECT r.*, 3::smallint AS "Outcome" FROM "TMP_Rejected" r
    ) x;

    ------------------------------------------------------------------------------
    -- Pass 9: clear THIS batch's staging rows. Never TRUNCATE - a concurrent writer
    -- may hold rows under a different BatchId.
    ------------------------------------------------------------------------------
    DELETE FROM gda."stg_DeviceSubscriptions" s WHERE s."BatchId" = p_batch_id;

    SELECT COUNT(*) INTO v_rejected_count FROM "TMP_Rejected";

    -- Rejected rows go back to the caller, which writes them to
    -- Q_DeviceSubscriptionsFail.
    RETURN QUERY
    SELECT 1::smallint, r."id", r."OriginalQueueId", r."Ordinal",
           r."ThirdPartyId", r."GeotabSerialNumber", r."Reason"
    FROM "TMP_Rejected" r;

    RETURN QUERY SELECT 0::smallint, NULL::bigint, NULL::bigint, NULL::bigint,
        NULL::varchar(50), NULL::varchar(50),
        ('Merge completed. Subscribed/updated: ' || v_subscribed_count
         || '; terminated: ' || v_terminated_count
         || '; rejected: ' || v_rejected_count || '.')::text;

    -- Drop temporary tables.
    DROP TABLE "TMP_DeduplicatedStaging";
    DROP TABLE "TMP_Rejected";
    DROP TABLE "TMP_Superseded";
    DROP TABLE "TMP_PreImage";
END;
$$;

ALTER FUNCTION gda."spMerge_stg_DeviceSubscriptions"(uuid, smallint, bigint, text, integer, integer)
    OWNER TO geotabdigadapter_client;

GRANT EXECUTE ON FUNCTION gda."spMerge_stg_DeviceSubscriptions"(uuid, smallint, bigint, text, integer, integer)
    TO geotabdigadapter_client;

REVOKE ALL ON FUNCTION gda."spMerge_stg_DeviceSubscriptions"(uuid, smallint, bigint, text, integer, integer)
    FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Grant Permissions on the new objects.
--
-- EXPLICIT per-object grants are required. The initial-creation script's
-- ALTER DEFAULT PRIVILEGES covers only objects created by the role that set it, so
-- it does not reach objects created by this upgrade script.
GRANT SELECT, INSERT, UPDATE, DELETE ON gda."DeviceSubscriptions"          TO geotabdigadapter_client;
GRANT SELECT, INSERT, UPDATE, DELETE ON gda."Q_DeviceSubscriptions"        TO geotabdigadapter_client;
GRANT SELECT, INSERT, UPDATE, DELETE ON gda."Q_DeviceSubscriptionsFail"    TO geotabdigadapter_client;
GRANT SELECT, INSERT, UPDATE, DELETE ON gda."stg_DeviceSubscriptions"      TO geotabdigadapter_client;
GRANT SELECT, INSERT, UPDATE, DELETE ON gda."DeviceSubscriptionHistory"    TO geotabdigadapter_client;
GRANT SELECT, INSERT, UPDATE, DELETE ON gda."DeviceSubscriptionGuardState" TO geotabdigadapter_client;

-- The IDENTITY-backed sequences for the new tables.
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA gda TO geotabdigadapter_client;

-- The shared ordering sequence. No gda object was a standalone sequence before this
-- script, so this grant is a new KIND of grant and is easy to omit - and a missing
-- one fails at the first INSERT into Q_DeviceSubscriptions, not at deploy time.
GRANT USAGE, SELECT ON SEQUENCE gda."SeqDeviceSubscriptionOrdinal" TO geotabdigadapter_client;
/*** [END] Part 5: OEM Subscription Mode ***/


/*** [START] Part 6: Fail Table Retention (added in 5.2.0.0) ***/
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: DigPartitionInfo
--
-- Records the arguments spManageDigPartitions was first run with. Both are
-- immutable afterwards: changing the interval on a populated database would
-- leave two incompatible partition layouts side by side, and moving the minimum
-- date earlier would ask for partitions in a range the existing ones already
-- cover. The row is written by spManageDigPartitions on its first execution,
-- not by this script, so the interval comes from configuration rather than from
-- whenever the database happened to be installed.
CREATE TABLE gda."DigPartitionInfo" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "InitialMinDateTimeUtc" timestamp without time zone NOT NULL,
    "InitialPartitionInterval" varchar(50) NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spManageDigPartitions
--
-- Creates the period partitions the partitioned gda tables need, through until
-- the end of the next calendar month. The DatabaseMaintenanceService runs it
-- periodically so that a partition always exists before there is data for it.
--
-- Modelled on the MyGeotab API Adapter's public."spManagePartitions", with
-- three deliberate divergences:
--
--   1. It is scoped to the gda schema and to tables carrying the partition
--      signature described below, so it can never touch another product's
--      tables in a shared database. (The API Adapter's equivalent had to be
--      given the same treatment for the reverse reason.)
--   2. There is no filegroup machinery. These tables live on the default
--      tablespace, which is what keeps a DIG-only deployment portable.
--   3. It re-homes rows out of the DEFAULT partition. This is not an
--      optimisation: PostgreSQL REFUSES to create a period partition while the
--      DEFAULT partition holds a row that would belong to it, and immediately
--      after an install or upgrade EVERY row is in DEFAULT. Without this the
--      first run would fail on any database that already has failure records.
--
-- Parameters:
--   MinDateTimeUtc:    Earliest date to create partitions from, on first run.
--                      Pinned in DigPartitionInfo thereafter and may not move
--                      earlier. It should be no later than the oldest existing
--                      RecordCreationTimeUtc, or those rows stay in DEFAULT and
--                      cannot be reclaimed by period.
--   PartitionInterval: 'monthly', 'weekly' or 'daily'. Pinned on first run.
--
-- Partition signature. A gda table is managed by this function, and by
-- spRetireDigPartitions, when ALL of the following hold:
--   - it is RANGE-partitioned on exactly one column of type
--     timestamp without time zone;
--   - it has a DEFAULT child named "<Parent>_default";
--   - every one of its non-DEFAULT children is named "<Parent>_yyyyMM" or
--     "<Parent>_yyyyMMdd".
-- Anything else in gda is ignored. The identical predicate appears in
-- spRetireDigPartitions and the two MUST agree: a table this function creates
-- partitions for but that one will not retire would grow forever.
CREATE OR REPLACE FUNCTION gda."spManageDigPartitions"(
    "MinDateTimeUtc" timestamp without time zone,
    "PartitionInterval" text)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
DECLARE
    -- A guard against a mistyped MinDateTimeUtc, not a tuning knob. Every period
    -- creates one child table per partitioned table, so a MinDateTimeUtc of, say,
    -- 2000-01-01 with a daily interval would try to create over 120,000 of them.
    max_periods_per_table CONSTANT integer := 2000;

    initial_min_datetime_utc TIMESTAMP;
    initial_partition_interval TEXT;
    min_date_utc DATE := "MinDateTimeUtc"::DATE;
    max_date_utc DATE := (date_trunc('month', (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '2 months') - INTERVAL '1 day')::DATE;
    partition_interval INTERVAL;
    truncate_unit TEXT;
    series_start DATE;
    resume_start DATE;
    period_count integer;

    parent RECORD;
    default_child TEXT;
    default_has_rows BOOLEAN;
    default_detached BOOLEAN;
    missing_count integer;
    period_ranges TSRANGE[];
    partition_start DATE;
    partition_end DATE;
    partition_name TEXT;
    rows_moved BIGINT;
    partitions_created integer := 0;
    tables_processed integer := 0;

    function_name TEXT := 'spManageDigPartitions';
    function_start_time TIMESTAMP;
    start_time TIMESTAMP;
    end_time TIMESTAMP;
    duration_string TEXT;

BEGIN
    -- ======================================================================================
    -- Log start of function execution.
    function_start_time := CLOCK_TIMESTAMP();
    start_time := function_start_time;
    RAISE NOTICE 'Executing function ''%'' Start: %', function_name, TO_CHAR(function_start_time, 'YYYY-MM-DD HH24:MI:SS');
    RAISE NOTICE 'min_date_utc: %', min_date_utc;
    RAISE NOTICE 'max_date_utc: %', max_date_utc;

    -- ======================================================================================
    -- STEP 1: Validate input parameter values.
    RAISE NOTICE 'Step 1 [Validating input parameter values]...';

    IF "PartitionInterval" = 'monthly' THEN
        partition_interval := INTERVAL '1 month';
        truncate_unit := 'month';
    ELSIF "PartitionInterval" = 'weekly' THEN
        partition_interval := INTERVAL '1 week';
        truncate_unit := 'week';
    ELSIF "PartitionInterval" = 'daily' THEN
        partition_interval := INTERVAL '1 day';
        truncate_unit := 'day';
    ELSE
        RAISE EXCEPTION 'ERROR: Invalid PartitionInterval value. Accepted values are: monthly, weekly, daily.';
    END IF;

    IF min_date_utc > max_date_utc THEN
        RAISE EXCEPTION 'ERROR: The min_date_utc cannot be greater than max_date_utc. [min_date_utc: %, max_date_utc: %]',
                        min_date_utc, max_date_utc;
    END IF;

    -- Pin the arguments on the first run; enforce them on every run thereafter.
    IF EXISTS (SELECT 1 FROM gda."DigPartitionInfo") THEN
        SELECT "InitialMinDateTimeUtc", "InitialPartitionInterval"
        INTO initial_min_datetime_utc, initial_partition_interval
        FROM gda."DigPartitionInfo"
        ORDER BY "id"
        LIMIT 1;

        IF min_date_utc < initial_min_datetime_utc::DATE THEN
            RAISE EXCEPTION 'ERROR: The MinDateTimeUtc cannot be earlier than the value recorded in gda."DigPartitionInfo". [MinDateTimeUtc: %, InitialMinDateTimeUtc: %]',
                            min_date_utc, initial_min_datetime_utc;
        END IF;

        IF "PartitionInterval" <> initial_partition_interval THEN
            RAISE EXCEPTION 'ERROR: The PartitionInterval cannot differ from the value recorded in gda."DigPartitionInfo". [PartitionInterval: %, InitialPartitionInterval: %]',
                            "PartitionInterval", initial_partition_interval;
        END IF;
    ELSE
        INSERT INTO gda."DigPartitionInfo" ("InitialMinDateTimeUtc", "InitialPartitionInterval", "RecordCreationTimeUtc")
        VALUES (min_date_utc, "PartitionInterval", (CURRENT_TIMESTAMP AT TIME ZONE 'UTC'));
    END IF;

    series_start := date_trunc(truncate_unit, min_date_utc::timestamp)::DATE;

    -- Resume above the newest period partition that is already present, exactly as
    -- the SQL Server procedure resumes above MAX(boundary).
    --
    -- Without this the series always restarts at the pinned InitialMinDateTimeUtc,
    -- so once retirement has run even once, every subsequent run re-creates the
    -- children retirement just dropped and retirement drops them again: unbounded
    -- churn inside the maintenance pause, one Info line per churned partition in
    -- the retirement report, and - on a daily interval - period_count crossing
    -- max_periods_per_table after a few years, which stops partition management
    -- permanently with advice the service cannot take, because it passes back the
    -- value pinned in DigPartitionInfo.
    --
    -- MIN of the per-table maxima, not MAX. A table that is behind - one added by
    -- a later release, say - must still be backfilled, so the slowest table holds
    -- the series back. A candidate table with NO period partitions at all
    -- COALESCEs to one interval below series_start, which resumes nowhere and
    -- leaves the pinned start in force. Every uncertain case therefore degrades to
    -- the previous behaviour, which is wasteful but never skips a partition.
    --
    -- The WHERE clause below is the PARTITION SIGNATURE from this function's header,
    -- in full and character for character as the creation loop and
    -- spRetireDigPartitions carry it. Keeping it a strict copy is not tidiness: a
    -- table counted here but skipped by the creation loop contributes a maximum for
    -- partitions that will never be created, which drags the MIN down to
    -- series_start and silently switches this whole resume off. All three copies
    -- change together.
    SELECT MIN(COALESCE(t.newest_period_start, (series_start - partition_interval)::DATE))
    INTO resume_start
    FROM (
        SELECT p.oid,
               MAX(to_date(
                     right(c.relname, CASE WHEN "PartitionInterval" = 'monthly' THEN 6 ELSE 8 END),
                     CASE WHEN "PartitionInterval" = 'monthly' THEN 'YYYYMM' ELSE 'YYYYMMDD' END
                   )) AS newest_period_start
        FROM pg_catalog.pg_partitioned_table pt
        JOIN pg_catalog.pg_class p     ON p.oid = pt.partrelid
        JOIN pg_catalog.pg_namespace n ON n.oid = p.relnamespace AND n.nspname = 'gda'
        JOIN pg_catalog.pg_attribute a ON a.attrelid = pt.partrelid AND a.attnum = pt.partattrs[0]
        LEFT JOIN pg_catalog.pg_inherits i ON i.inhparent = p.oid
        LEFT JOIN pg_catalog.pg_class c    ON c.oid = i.inhrelid
                                          AND c.relname ~ ('^' || p.relname || '_[0-9]{6}([0-9]{2})?$')
        WHERE pt.partstrat = 'r'
          AND pt.partnatts = 1
          AND a.atttypid = 'timestamp without time zone'::regtype
          AND EXISTS (
              SELECT 1
              FROM pg_catalog.pg_inherits di
              JOIN pg_catalog.pg_class dc ON dc.oid = di.inhrelid
              WHERE di.inhparent = p.oid
                AND dc.relname = p.relname || '_default'
                AND pg_catalog.pg_get_expr(dc.relpartbound, dc.oid) = 'DEFAULT')
          AND NOT EXISTS (
              SELECT 1
              FROM pg_catalog.pg_inherits ni
              JOIN pg_catalog.pg_class nc ON nc.oid = ni.inhrelid
              WHERE ni.inhparent = p.oid
                AND pg_catalog.pg_get_expr(nc.relpartbound, nc.oid) <> 'DEFAULT'
                AND nc.relname !~ ('^' || p.relname || '_[0-9]{6}([0-9]{2})?$'))
        GROUP BY p.oid
    ) t;

    IF resume_start IS NOT NULL
       AND (resume_start + partition_interval)::DATE > series_start THEN
        series_start := (resume_start + partition_interval)::DATE;
    END IF;

    -- Counted from the RESUMED start, so the safety limit bounds the work this run
    -- will actually do. Counting from the pinned minimum meant a long-lived daily
    -- deployment eventually tripped a guard about a MinDateTimeUtc it could not
    -- change.
    SELECT count(*) INTO period_count
    FROM generate_series(series_start::timestamp, max_date_utc::timestamp, partition_interval);

    IF period_count > max_periods_per_table THEN
        RAISE EXCEPTION 'ERROR: MinDateTimeUtc implies % periods per table, above the safety limit of %. Each period creates one child table per partitioned table. Supply a later MinDateTimeUtc or a longer PartitionInterval. [MinDateTimeUtc: %, PartitionInterval: %]',
                        period_count, max_periods_per_table, min_date_utc, "PartitionInterval";
    END IF;

    -- Log.
    end_time := CLOCK_TIMESTAMP();
    duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000;
    RAISE NOTICE 'Step 1 completed. Duration: % milliseconds', duration_string;
    start_time := end_time;

    -- ======================================================================================
    -- Step 2: Create the missing period partitions.
    RAISE NOTICE 'Step 2 [Creating partitions]...';

    FOR parent IN
        SELECT p.oid                                  AS parent_oid,
               p.relname                              AS parent_name,
               a.attname                              AS key_column,
               pg_catalog.pg_get_userbyid(p.relowner) AS parent_owner
        FROM pg_catalog.pg_partitioned_table pt
        JOIN pg_catalog.pg_class p        ON p.oid = pt.partrelid
        JOIN pg_catalog.pg_namespace n    ON n.oid = p.relnamespace AND n.nspname = 'gda'
        JOIN pg_catalog.pg_attribute a    ON a.attrelid = pt.partrelid AND a.attnum = pt.partattrs[0]
        WHERE pt.partstrat = 'r'
          AND pt.partnatts = 1
          AND a.atttypid = 'timestamp without time zone'::regtype
          AND EXISTS (
              SELECT 1
              FROM pg_catalog.pg_inherits i
              JOIN pg_catalog.pg_class dc ON dc.oid = i.inhrelid
              WHERE i.inhparent = p.oid
                AND dc.relname = p.relname || '_default'
                AND pg_catalog.pg_get_expr(dc.relpartbound, dc.oid) = 'DEFAULT')
          AND NOT EXISTS (
              SELECT 1
              FROM pg_catalog.pg_inherits i
              JOIN pg_catalog.pg_class nc ON nc.oid = i.inhrelid
              WHERE i.inhparent = p.oid
                AND pg_catalog.pg_get_expr(nc.relpartbound, nc.oid) <> 'DEFAULT'
                AND nc.relname !~ ('^' || p.relname || '_[0-9]{6}([0-9]{2})?$'))
        ORDER BY p.relname
    LOOP
        tables_processed := tables_processed + 1;
        default_child := parent.parent_name || '_default';
        default_detached := FALSE;
        missing_count := 0;

        -- First pass: is there anything to do for this table at all? On the
        -- overwhelmingly common run there is not, and this keeps that case down
        -- to a handful of catalog lookups with no locks taken.
        FOR partition_start IN
            SELECT generate_series(series_start::timestamp, max_date_utc::timestamp, partition_interval)::DATE
        LOOP
            partition_name := parent.parent_name || '_' ||
                CASE WHEN "PartitionInterval" = 'monthly' THEN to_char(partition_start, 'YYYYMM')
                     ELSE to_char(partition_start, 'YYYYMMDD') END;

            IF NOT EXISTS (
                SELECT 1
                FROM pg_catalog.pg_class c
                JOIN pg_catalog.pg_namespace cn ON cn.oid = c.relnamespace
                WHERE c.relname = partition_name AND cn.nspname = 'gda')
            THEN
                missing_count := missing_count + 1;
            END IF;
        END LOOP;

        IF missing_count = 0 THEN
            CONTINUE;
        END IF;

        -- PostgreSQL validates a new partition against the DEFAULT partition and
        -- refuses if any row there would have belonged to it. Detaching DEFAULT
        -- first sidesteps that, and lets those rows be filed afterwards. The
        -- whole function is one transaction, so a failure anywhere below rolls
        -- the detach back with it - DEFAULT is never left detached.
        EXECUTE format('SELECT EXISTS (SELECT 1 FROM gda.%I)', default_child) INTO default_has_rows;

        IF default_has_rows THEN
            EXECUTE format('ALTER TABLE gda.%I DETACH PARTITION gda.%I', parent.parent_name, default_child);
            default_detached := TRUE;
            RAISE NOTICE '> %: DEFAULT partition holds rows; detached it so the new partitions can be created.', parent.parent_name;
        END IF;

        -- Second pass: create them.
        FOR partition_start IN
            SELECT generate_series(series_start::timestamp, max_date_utc::timestamp, partition_interval)::DATE
        LOOP
            partition_end := (partition_start + partition_interval)::DATE;
            partition_name := parent.parent_name || '_' ||
                CASE WHEN "PartitionInterval" = 'monthly' THEN to_char(partition_start, 'YYYYMM')
                     ELSE to_char(partition_start, 'YYYYMMDD') END;

            IF NOT EXISTS (
                SELECT 1
                FROM pg_catalog.pg_class c
                JOIN pg_catalog.pg_namespace cn ON cn.oid = c.relnamespace
                WHERE c.relname = partition_name AND cn.nspname = 'gda')
            THEN
                EXECUTE format('CREATE TABLE gda.%I PARTITION OF gda.%I FOR VALUES FROM (%L) TO (%L)',
                               partition_name, parent.parent_name, partition_start, partition_end);
                -- Match the parent's owner, so a partition created by a
                -- superuser is still writable by the adapter's own role.
                EXECUTE format('ALTER TABLE gda.%I OWNER TO %I', partition_name, parent.parent_owner);
                partitions_created := partitions_created + 1;
                RAISE NOTICE '> Created partition %.', partition_name;
            END IF;
        END LOOP;

        -- File the rows that were waiting in DEFAULT, then put DEFAULT back.
        IF default_detached THEN
            -- Read the bounds back out of the catalog rather than recomputing
            -- them, and test membership against the resulting set of ranges.
            -- That makes this ONE scan of the default rather than one per
            -- period, and means no assumption that the periods are contiguous -
            -- a row falling in a gap simply stays in DEFAULT instead of being
            -- routed at a partition that does not exist.
            SELECT array_agg(tsrange(b.lo::timestamp, b.hi::timestamp, '[)'))
            INTO period_ranges
            FROM pg_catalog.pg_inherits i
            JOIN pg_catalog.pg_class c ON c.oid = i.inhrelid
            CROSS JOIN LATERAL (
                SELECT substring(pg_catalog.pg_get_expr(c.relpartbound, c.oid) FROM 'FROM \(''([^'']+)''\)') AS lo,
                       substring(pg_catalog.pg_get_expr(c.relpartbound, c.oid) FROM 'TO \(''([^'']+)''\)')   AS hi
            ) b
            WHERE i.inhparent = parent.parent_oid
              AND b.lo IS NOT NULL
              AND b.hi IS NOT NULL;

            IF period_ranges IS NOT NULL THEN
                -- Inserted into the PARENT, not into a child: routing decides
                -- where each row lands. OVERRIDING SYSTEM VALUE preserves the
                -- existing "id" values, which are GENERATED ALWAYS.
                EXECUTE format(
                    'WITH moved AS (
                         DELETE FROM gda.%I d
                         WHERE EXISTS (SELECT 1 FROM unnest($1) r WHERE d.%I <@ r)
                         RETURNING d.*)
                     INSERT INTO gda.%I OVERRIDING SYSTEM VALUE SELECT * FROM moved',
                    default_child, parent.key_column, parent.parent_name)
                USING period_ranges;
                GET DIAGNOSTICS rows_moved = ROW_COUNT;

                IF rows_moved > 0 THEN
                    RAISE NOTICE '>   Moved % row(s) out of % and into their period partitions.', rows_moved, default_child;
                END IF;
            END IF;

            -- This re-validates the default partition, so it fails rather than
            -- misfiling anything if a row that belongs to a period partition is
            -- somehow still here.
            EXECUTE format('ALTER TABLE gda.%I ATTACH PARTITION gda.%I DEFAULT', parent.parent_name, default_child);
        END IF;
    END LOOP;

    -- Log.
    end_time := CLOCK_TIMESTAMP();
    duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000;
    RAISE NOTICE 'Step 2 completed. % partition(s) created across % table(s). Duration: % milliseconds',
                 partitions_created, tables_processed, duration_string;

    -- ======================================================================================
    -- Log end of function execution.
    end_time := CLOCK_TIMESTAMP();
    duration_string := EXTRACT(EPOCH FROM (end_time - function_start_time)) * 1000 || ' milliseconds';
    RAISE NOTICE 'Function ''%'' executed successfully. Total Duration: %', function_name, duration_string;
END;
$BODY$;

ALTER FUNCTION gda."spManageDigPartitions"(timestamp without time zone, text)
    OWNER TO geotabdigadapter_client;

GRANT EXECUTE ON FUNCTION gda."spManageDigPartitions"(timestamp without time zone, text)
    TO geotabdigadapter_client;

REVOKE ALL ON FUNCTION gda."spManageDigPartitions"(timestamp without time zone, text)
    FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: spRetireDigPartitions
--
-- Reclaims the space held by failure records older than a cutoff, by dropping
-- whole partitions rather than by deleting rows. The DatabaseMaintenanceService
-- calls it with Execute => true when automatic retention is enabled; an
-- operator can call it with the default Execute => false to see exactly what
-- would be dropped, and nothing else.
--
--   SELECT * FROM gda."spRetireDigPartitions"('2026-01-01');        -- report only
--   SELECT * FROM gda."spRetireDigPartitions"('2026-01-01', true);  -- perform it
--
-- THIS DELETES DATA and cannot be undone. Only partitions whose ENTIRE range
-- lies before the cutoff are dropped, so the current period is never affected,
-- but the records inside a dropped partition are gone. Take a backup, or set
-- EnableAutomaticRetention to false, if the failure history has to be kept.
--
-- The DEFAULT partition has no range to compare against the cutoff, so it can
-- never be retired BY PERIOD - but it is not exempt from retention. It is
-- emptied when EVERY row in it predates the cutoff. That is the same
-- all-or-nothing rule SQL Server's open-ended leading partition follows there:
-- that partition is truncated only once the cutoff passes the boundary above
-- it. So neither provider removes a record newer than the cutoff, and neither
-- leaves a back-dated one permanently unreclaimable.
--
-- Retiring it by CONTENT is the only route available, because a row that
-- predates the pinned InitialMinDateTimeUtc can never be filed into a period
-- afterwards: spManageDigPartitions REFUSES a MinDateTimeUtc earlier than the
-- recorded one, since the existing partitions were cut on it. Telling an
-- operator to lower it - which this function used to do - is advice the other
-- procedure rejects outright.
--
-- Unlike the MyGeotab API Adapter's retirement utility this both detaches AND
-- drops, and it does not refuse while the adapter is connected. That utility is
-- an operator tool whose job is to hand the operator a detached table to
-- archive; this is a service the adapter runs itself, and the service pauses
-- the other services around it. DETACH still takes a brief ACCESS EXCLUSIVE
-- lock on the parent table, which is why it runs inside that pause.
--
-- The gda schema has no foreign keys, so - again unlike the API Adapter's -
-- this needs no dependency ordering and no retry for partitions pinned by a
-- referencing row.
--
-- Parameters:
--   OlderThan: Partitions whose whole range precedes this are retirable.
--   Execute:   FALSE (the default) reports; TRUE performs.
--
-- The partition signature is the one documented on spManageDigPartitions, and
-- the predicate below is deliberately identical to that function's. The two
-- MUST agree: a table one manages and the other ignores would grow forever.
CREATE OR REPLACE FUNCTION gda."spRetireDigPartitions"(
    "OlderThan" timestamp without time zone,
    "Execute" boolean DEFAULT false)
    RETURNS TABLE("Object" text, "Action" text, "Detail" text)
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
DECLARE
    rec RECORD;
    lo TEXT;
    hi TEXT;
    hi_ts TIMESTAMP;
    approx_rows TEXT;
    default_has_rows boolean;
    -- Counted apart from `retired`, because emptying the catch-all is not retiring a partition:
    -- the partition deliberately stays, and a summary saying "N partition(s) retired" when none
    -- was is a wrong audit trail for an operation that permanently deletes records.
    emptied integer := 0;
    default_all_retirable boolean;
    candidates integer := 0;
    retired integer := 0;
BEGIN
    -- ======================================================================================
    -- Validate input parameter values.
    IF "OlderThan" IS NULL THEN
        RAISE EXCEPTION 'ERROR: OlderThan is required. Example: SELECT * FROM gda."spRetireDigPartitions"(''2026-01-01'');';
    END IF;

    IF "OlderThan" > (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') THEN
        RAISE EXCEPTION 'ERROR: OlderThan is in the future. Refusing: this would drop partitions holding current records.';
    END IF;

    -- Every lock this function takes - a DEFAULT partition's ACCESS EXCLUSIVE below, and whatever
    -- DETACH and DROP take - waits at most 30 seconds. Without a limit a long reader of a fail table
    -- held the run, and the telemetry pause around it, for up to the statement timeout of an hour;
    -- with it the run fails in seconds, says why, and is tried again at the next interval. The
    -- setting is local to this call's transaction.
    PERFORM set_config('lock_timeout', '30s', true);

    IF "Execute" THEN
        RAISE NOTICE 'EXECUTE MODE: partitions entirely older than % are being dropped. This deletes data.', "OlderThan";
    ELSE
        RAISE NOTICE 'REPORT ONLY: no changes are made. The report lists what Execute => true would do.';
    END IF;

    -- ======================================================================================
    -- Walk every partition of every managed table, oldest first.
    FOR rec IN
        SELECT p.relname                                                     AS parent_name,
               c.relname                                                     AS child_name,
               a.attname                                                     AS key_column,
               pg_catalog.pg_get_expr(c.relpartbound, c.oid)                 AS bounds,
               c.reltuples                                                   AS reltuples,
               pg_catalog.pg_size_pretty(pg_catalog.pg_total_relation_size(c.oid)) AS total_size
        FROM pg_catalog.pg_partitioned_table pt
        JOIN pg_catalog.pg_class p        ON p.oid = pt.partrelid
        JOIN pg_catalog.pg_namespace n    ON n.oid = p.relnamespace AND n.nspname = 'gda'
        JOIN pg_catalog.pg_attribute a    ON a.attrelid = pt.partrelid AND a.attnum = pt.partattrs[0]
        JOIN pg_catalog.pg_inherits i     ON i.inhparent = p.oid
        JOIN pg_catalog.pg_class c        ON c.oid = i.inhrelid
        WHERE pt.partstrat = 'r'
          AND pt.partnatts = 1
          AND a.atttypid = 'timestamp without time zone'::regtype
          AND EXISTS (
              SELECT 1
              FROM pg_catalog.pg_inherits i2
              JOIN pg_catalog.pg_class dc ON dc.oid = i2.inhrelid
              WHERE i2.inhparent = p.oid
                AND dc.relname = p.relname || '_default'
                AND pg_catalog.pg_get_expr(dc.relpartbound, dc.oid) = 'DEFAULT')
          AND NOT EXISTS (
              SELECT 1
              FROM pg_catalog.pg_inherits i3
              JOIN pg_catalog.pg_class nc ON nc.oid = i3.inhrelid
              WHERE i3.inhparent = p.oid
                AND pg_catalog.pg_get_expr(nc.relpartbound, nc.oid) <> 'DEFAULT'
                AND nc.relname !~ ('^' || p.relname || '_[0-9]{6}([0-9]{2})?$'))
        ORDER BY p.relname, c.relname
    LOOP
        approx_rows := CASE WHEN rec.reltuples < 0 THEN 'unknown' ELSE rec.reltuples::bigint::text END;

        -- The DEFAULT partition cannot be retired BY PERIOD - it has no range to
        -- compare against the cutoff - so it is retired by CONTENT instead: it is
        -- emptied when every row in it predates the cutoff, and left alone
        -- otherwise. See the note on this function for why that is the same rule
        -- SQL Server's leading partition follows, and why leaving it exempt made a
        -- back-dated record reclaimable on one provider and immortal on the other.
        --
        -- Both probes below are EXISTS rather than reltuples, which is -1 on a
        -- partition that has never been analyzed and only becomes 0 once ANALYZE
        -- or autovacuum has visited it. Treating -1 as a row count would make
        -- every never-analyzed DEFAULT partition look occupied, and the second
        -- probe would then find nothing newer than the cutoff and report a
        -- retirement of an empty table on every fresh install. Testing
        -- reltuples > 0 instead would trade that for the worse error of staying
        -- silent about a partition that really does hold rows but has not been
        -- analyzed yet. Each probe stops at the first row it finds.
        IF rec.bounds = 'DEFAULT' THEN
            -- First look WITHOUT a lock. A DEFAULT partition is almost always empty - a row
            -- reaches one only when its date falls outside every period partition - and an
            -- empty one needs nothing, so the usual run takes no lock here at all. Locking all
            -- thirteen first, on every run, queued this function (and the telemetry pause around
            -- it) behind any long reader of a fail table, such as a nightly pg_dump, and could
            -- make a parallel dump fail outright. A row that arrives after this look is safe: an
            -- empty partition is skipped rather than truncated, and the next run sees the row.
            EXECUTE format('SELECT EXISTS (SELECT 1 FROM gda.%I)', rec.child_name)
                INTO default_has_rows;

            IF NOT default_has_rows THEN
                CONTINUE;
            END IF;

            -- It holds rows. Take the lock the TRUNCATE below takes anyway, BEFORE deciding, and
            -- decide again under it rather than leaving the lock to the TRUNCATE.
            --
            -- DatabaseMaintenanceService calls this function on a STANDALONE, autocommitting
            -- connection, so each statement reads its own snapshot. Without this there is a
            -- window between "nothing in this partition is newer than the cutoff" and the
            -- TRUNCATE, and a row another process commits into the partition inside that window
            -- is invisible to the probe and destroyed by the TRUNCATE without ever having been
            -- considered.
            --
            -- The maintenance pause does NOT close that window. It coordinates through the
            -- in-memory state machine, which is per PROCESS, and README section 7.7 endorses
            -- running different services on different hosts - in which case the service doing
            -- this has paused nothing that writes here. The lock spans processes; the pause
            -- does not.
            --
            -- Report mode takes no lock: it removes nothing, and a read-only report has no
            -- business blocking writers.
            IF "Execute" THEN
                EXECUTE format('LOCK TABLE gda.%I IN ACCESS EXCLUSIVE MODE', rec.child_name);

                EXECUTE format('SELECT EXISTS (SELECT 1 FROM gda.%I)', rec.child_name)
                    INTO default_has_rows;

                IF NOT default_has_rows THEN
                    CONTINUE;
                END IF;
            END IF;

            EXECUTE format('SELECT NOT EXISTS (SELECT 1 FROM gda.%I WHERE %I >= $1)',
                           rec.child_name, rec.key_column)
                INTO default_all_retirable USING "OlderThan";

            IF NOT default_all_retirable THEN
                "Object" := 'gda."' || rec.child_name || '"';
                "Action" := 'NOT RETIRABLE YET - DEFAULT partition';
                "Detail" := 'Approx. ' || approx_rows || ' row(s) fall outside every period partition, and at least one of them is newer than ' || "OlderThan"::text || '. They share one partition, so they are reclaimed together once the cutoff passes all of them. Nothing is wrong; this is telling you when they go.';
                RETURN NEXT;
                CONTINUE;
            END IF;

            candidates := candidates + 1;

            IF "Execute" THEN
                -- TRUNCATE, not DETACH and DROP. The partition signature requires a
                -- DEFAULT child to exist, and spManageDigPartitions detaches and
                -- re-attaches this very table; dropping it would break both.
                -- Emptying reclaims the space and leaves the partition in place.
                EXECUTE format('TRUNCATE TABLE gda.%I', rec.child_name);
                emptied := emptied + 1;
                RAISE NOTICE '> Emptied DEFAULT partition % [approx. % row(s); %].',
                             rec.child_name, approx_rows, rec.total_size;

                "Object" := 'gda."' || rec.child_name || '"';
                "Action" := 'RETIRED - DEFAULT partition emptied';
                "Detail" := 'Every row predated ' || "OlderThan"::text || '; approx. ' || approx_rows || ' row(s); ' || rec.total_size || ' reclaimed.';
                RETURN NEXT;
            ELSE
                "Object" := 'gda."' || rec.child_name || '"';
                "Action" := 'WOULD RETIRE - empty DEFAULT partition';
                "Detail" := 'Every row predates ' || "OlderThan"::text || '; approx. ' || approx_rows || ' row(s); ' || rec.total_size || ' would be reclaimed.';
                RETURN NEXT;
            END IF;

            CONTINUE;
        END IF;

        lo := substring(rec.bounds FROM 'FROM \(''([^'']+)''\)');
        hi := substring(rec.bounds FROM 'TO \(''([^'']+)''\)');

        IF hi IS NULL THEN
            "Object" := 'gda."' || rec.child_name || '"';
            "Action" := 'SKIPPED - unrecognized partition bounds';
            "Detail" := rec.bounds;
            RETURN NEXT;
            CONTINUE;
        END IF;

        hi_ts := hi::timestamp;

        -- Only a partition whose ENTIRE range precedes the cutoff is retirable,
        -- so the period the cutoff falls inside is always left alone.
        IF hi_ts <= "OlderThan" THEN
            candidates := candidates + 1;

            IF "Execute" THEN
                EXECUTE format('ALTER TABLE gda.%I DETACH PARTITION gda.%I', rec.parent_name, rec.child_name);
                EXECUTE format('DROP TABLE gda.%I', rec.child_name);
                retired := retired + 1;
                RAISE NOTICE '> Retired partition % [% to %; approx. % row(s); %].',
                             rec.child_name, COALESCE(lo, 'MINVALUE'), hi, approx_rows, rec.total_size;

                "Object" := 'gda."' || rec.child_name || '"';
                "Action" := 'RETIRED - detached and dropped';
                "Detail" := 'Range ' || COALESCE(lo, 'MINVALUE') || ' to ' || hi || '; approx. ' || approx_rows || ' row(s); ' || rec.total_size || ' reclaimed.';
                RETURN NEXT;
            ELSE
                "Object" := 'gda."' || rec.child_name || '"';
                "Action" := 'WOULD RETIRE - detach and drop';
                "Detail" := 'Range ' || COALESCE(lo, 'MINVALUE') || ' to ' || hi || '; approx. ' || approx_rows || ' row(s); ' || rec.total_size || ' would be reclaimed.';
                RETURN NEXT;
            END IF;
        END IF;
    END LOOP;

    -- ======================================================================================
    -- Summary.
    "Object" := current_database();
    "Action" := CASE
        WHEN candidates = 0 THEN 'Nothing to retire - no partition lies entirely before ' || "OlderThan"::date || ', and no catch-all partition holds only records older than it'
        WHEN "Execute"      THEN CASE
                                     WHEN emptied = 0 THEN retired || ' partition(s) retired'
                                     WHEN retired = 0 THEN emptied || ' catch-all partition(s) emptied'
                                     ELSE retired || ' partition(s) retired and ' || emptied || ' catch-all partition(s) emptied'
                                 END
        ELSE                     candidates || ' partition(s) would be retired'
    END;
    "Detail" := CASE
        WHEN candidates = 0 THEN 'No changes were made.'
        WHEN "Execute"      THEN 'The records in those partitions have been permanently deleted - a partition whose whole period predates the cutoff by dropping it, a catch-all partition whose every record predates the cutoff by emptying it.'
        ELSE                     'Re-run with Execute => true to perform this. It cannot be undone.'
    END;
    RETURN NEXT;

    RETURN;
END;
$BODY$;

ALTER FUNCTION gda."spRetireDigPartitions"(timestamp without time zone, boolean)
    OWNER TO geotabdigadapter_client;

GRANT EXECUTE ON FUNCTION gda."spRetireDigPartitions"(timestamp without time zone, boolean)
    TO geotabdigadapter_client;

REVOKE ALL ON FUNCTION gda."spRetireDigPartitions"(timestamp without time zone, boolean)
    FROM PUBLIC;

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Ownership and schema privileges for geotabdigadapter_client.
--
-- This script is documented as being executed by a database administrator
-- rather than by the adapter's own role, so every object above belongs to
-- whoever ran it. Until the fail tables became partitioned that was harmless:
-- the adapter issued nothing but DML, which the grants above cover.
--
-- spManageDigPartitions changes that. It CREATEs partitions, DETACHes and
-- re-ATTACHes the DEFAULT partition, and sets the owner of each new child, and
-- PostgreSQL requires the caller to OWN the parent table and to hold CREATE on
-- the schema for all of it. Without this block the partition manager fails on
-- its very first statement with "permission denied for schema gda": no
-- partition is ever created, every failure record piles up in the DEFAULT
-- partition, and retention can never reclaim any of it.
--
-- The MyGeotab API Adapter's schema script does the same thing for the same
-- reason, as an ALTER TABLE ... OWNER TO after each CREATE TABLE. A loop is used
-- here instead, so that a table added in a later release cannot be missed and so
-- that this block can be identical in the cumulative and the upgrade script.
--
-- SQL Server needs no equivalent: geotabdigadapter_client is a member of
-- db_owner there, which already carries every right these procedures need.
GRANT CREATE ON SCHEMA gda TO geotabdigadapter_client;

DO $$
DECLARE
    obj RECORD;
BEGIN
    FOR obj IN
        SELECT c.relname
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'gda'
          AND c.relkind IN ('r', 'p')
          AND pg_catalog.pg_get_userbyid(c.relowner) <> 'geotabdigadapter_client'
        ORDER BY c.relname
    LOOP
        EXECUTE format('ALTER TABLE gda.%I OWNER TO geotabdigadapter_client', obj.relname);
    END LOOP;
END $$;

-- An identity-backed sequence is re-owned along with its table, and PostgreSQL
-- refuses to re-own one on its own, which is why the loop above covers tables
-- only. That leaves the one standalone sequence to name explicitly.
ALTER SEQUENCE gda."SeqDeviceSubscriptionOrdinal" OWNER TO geotabdigadapter_client;

/*** [END] Part 6: Fail Table Retention ***/


/*** [START] Part 7: Database Maintenance (added in 5.2.0.0) ***/
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Extension: pgstattuple
--
-- Level 2 database maintenance measures index bloat with public.pgstattuple().
-- Two things make that function usable, and the MyGeotab API Adapter has
-- shipped without the second of them before (GitHub issue #25): the extension
-- itself, and an EXECUTE grant to the role the adapter connects as.
-- geotabdigadapter_client is not the API Adapter's role, so an extension that
-- adapter installed carries no grant for this one.
--
-- CREATE EXTENSION needs a superuser, and a managed PostgreSQL service may not
-- offer this extension at all. Neither is allowed to stop the schema from being
-- installed. Level 2 maintenance is the only thing that needs pgstattuple, and
-- the adapter checks that it is usable before every Level 2 run and skips the
-- run - logging a warning that names the statements that fix it - when it is not.
-- The Level 2 view is written so that it can be created whether or not the
-- extension exists; see fnDigIndexSpaceStats.
DO $$
DECLARE
    extension_schema text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_extension WHERE extname = 'pgstattuple') THEN
        BEGIN
            CREATE EXTENSION pgstattuple WITH SCHEMA public;
        EXCEPTION WHEN OTHERS THEN
            RAISE WARNING 'The pgstattuple extension could not be created (%). The schema is otherwise complete, but Level 2 database maintenance will be skipped on PostgreSQL until a superuser runs: CREATE EXTENSION pgstattuple WITH SCHEMA public; GRANT EXECUTE ON FUNCTION public.pgstattuple(regclass) TO geotabdigadapter_client;', SQLERRM;
        END;
    END IF;

    IF to_regprocedure('public.pgstattuple(regclass)') IS NOT NULL THEN
        BEGIN
            GRANT EXECUTE ON FUNCTION public.pgstattuple(regclass) TO geotabdigadapter_client;
        EXCEPTION WHEN OTHERS THEN
            RAISE WARNING 'EXECUTE on public.pgstattuple(regclass) could not be granted to geotabdigadapter_client (%). Level 2 database maintenance will be skipped on PostgreSQL until a superuser runs: GRANT EXECUTE ON FUNCTION public.pgstattuple(regclass) TO geotabdigadapter_client;', SQLERRM;
        END;
    ELSE
        -- Installed, but public.pgstattuple(regclass) is missing: the extension is in another schema - some managed
        -- services keep extensions in one of their own - or it is an old version, from before that function existed,
        -- carried forward by an upgrade. CREATE EXTENSION IF NOT EXISTS is a no-op either way, so say what fixes it.
        SELECT n.nspname INTO extension_schema
        FROM pg_catalog.pg_extension e
        JOIN pg_catalog.pg_namespace n ON n.oid = e.extnamespace
        WHERE e.extname = 'pgstattuple';
        IF extension_schema = 'public' THEN
            RAISE WARNING 'The pgstattuple extension in schema public is an old version, from before pgstattuple(regclass), which the Level 2 maintenance view uses, existed. Level 2 database maintenance will be skipped on PostgreSQL until a superuser runs: ALTER EXTENSION pgstattuple UPDATE; GRANT EXECUTE ON FUNCTION public.pgstattuple(regclass) TO geotabdigadapter_client;';
        ELSIF extension_schema IS NOT NULL THEN
            RAISE WARNING 'The pgstattuple extension is installed in schema %, but the Level 2 maintenance view uses public.pgstattuple. Level 2 database maintenance will be skipped on PostgreSQL until a superuser runs: ALTER EXTENSION pgstattuple SET SCHEMA public; GRANT EXECUTE ON FUNCTION public.pgstattuple(regclass) TO geotabdigadapter_client;', extension_schema;
        END IF;
    END IF;
END $$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: DigMaintenanceLogs
--
-- One row per database maintenance run, written by the DatabaseMaintenanceService.
-- The service reads the latest Level 1 and Level 2 rows back to decide when each
-- is next due, so these rows are what space those runs out. They are also the
-- only record in the database of when maintenance ran, and whether it
-- succeeded.
--
-- MaintenanceTypeId: 1 = Level 1, 2 = Level 2, 3 = Partition, 4 = Retention.
-- The first three mean what they mean in the MyGeotab API Adapter's
-- DBMaintenanceLogs2; Retention is this adapter's own. Retention writes one row
-- for partition retirement, which runs inside the maintenance pause, and one for
-- the invalid-record sweep, which runs after it.
CREATE TABLE gda."DigMaintenanceLogs" (
    "id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "MaintenanceTypeId" smallint NOT NULL,
    "StartTimeUtc" timestamp without time zone NOT NULL,
    "EndTimeUtc" timestamp without time zone,
    "Success" boolean,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

-- Serves the one query the service issues against this table: the latest row
-- of a given type.
CREATE INDEX "IX_DigMaintenanceLogs_MaintenanceTypeId_StartTimeUtc"
ON gda."DigMaintenanceLogs" ("MaintenanceTypeId", "StartTimeUtc");

CREATE INDEX "IX_DigMaintenanceLogs_RecordLastChangedUtc"
ON gda."DigMaintenanceLogs" ("RecordLastChangedUtc");

ALTER TABLE gda."DigMaintenanceLogs" OWNER TO geotabdigadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Function: fnDigIndexSpaceStats
--
-- Returns the free space and total length of one index, as measured by
-- public.pgstattuple(). vwStatsForLevel2DigMaintenance calls this rather than
-- pgstattuple() directly, for one reason: a view that names a function cannot be
-- created unless that function exists, and then depends on it for as long as the
-- view exists. The call below is dynamic, so it is resolved only when it runs.
-- The view can therefore be created on a server without the extension, the
-- extension can be installed afterwards without re-creating the view, and
-- dropping the extension does not drop the view. The adapter checks that
-- pgstattuple is usable before it reads the view.
--
-- It also reads each index once. (The MyGeotab API Adapter's view named
-- pgstattuple() once per column it took from it, and so read every index
-- twice, until its version 5.2.0.0.)
CREATE OR REPLACE FUNCTION gda."fnDigIndexSpaceStats"(
	"IndexOid" oid)
    RETURNS TABLE("FreeSpace" bigint, "TableLength" bigint)
    LANGUAGE plpgsql
AS $BODY$
BEGIN
    RETURN QUERY EXECUTE
        'SELECT s.free_space, s.table_len FROM public.pgstattuple($1) s'
        USING "IndexOid"::regclass;
END;
$BODY$;

ALTER FUNCTION gda."fnDigIndexSpaceStats"(oid)
    OWNER TO geotabdigadapter_client;

GRANT EXECUTE ON FUNCTION gda."fnDigIndexSpaceStats"(oid)
    TO geotabdigadapter_client;

REVOKE ALL ON FUNCTION gda."fnDigIndexSpaceStats"(oid)
    FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create View: vwStatsForLevel1DigMaintenance
--
-- The gda tables that may need Level 1 maintenance. The adapter VACUUMs and
-- ANALYZEs a table whose dead tuples exceed 0.2 of its live tuples or number more
-- than 1000, and ANALYZEs a table whose modifications since its last analyze
-- exceed 0.1 of its live tuples. Every figure here is a RATIO (0..1), never a
-- percentage.
--
-- THE SAME THRESHOLDS ARE CONSTANTS IN THE ADAPTER (DigMaintenanceThresholds) AND
-- THE TWO MUST AGREE. This view decides which tables are offered and the adapter
-- decides which of those it acts on, so a disagreement between them does not
-- fail: it silently maintains nothing. A unit test reads this script and
-- compares them.
--
-- Mirrors the MyGeotab API Adapter's public."vwStatsForLevel1DBMaintenance",
-- scoped to the gda schema instead of public.
CREATE VIEW gda."vwStatsForLevel1DigMaintenance" AS
WITH orderedrows AS (
    SELECT s.schemaname AS "SchemaName",
        s.relname AS "TableName",
        s.n_live_tup AS "LiveTuples",
        s.n_dead_tup AS "DeadTuples",
        (s.n_dead_tup::numeric / NULLIF(s.n_live_tup, 0)::numeric) AS "PctDeadTuples",
        s.n_mod_since_analyze AS "ModsSinceLastAnalyze",
        (s.n_mod_since_analyze::numeric / NULLIF(s.n_live_tup, 0)::numeric) AS "PctModsSinceLastAnalyze"
    FROM pg_catalog.pg_stat_user_tables s
    WHERE s.schemaname = 'gda'
      AND (s.n_dead_tup::numeric > (0.2 * s.n_live_tup::numeric)
        OR s.n_mod_since_analyze::numeric > (0.1 * s.n_live_tup::numeric)
        OR s.n_dead_tup > 1000)
    ORDER BY (s.n_dead_tup::numeric / NULLIF(s.n_live_tup, 0)::numeric) DESC,
        (s.n_mod_since_analyze::numeric / NULLIF(s.n_live_tup, 0)::numeric) DESC
)
SELECT row_number() OVER () AS "RowId",
    "SchemaName",
    "TableName",
    "LiveTuples",
    "DeadTuples",
    "PctDeadTuples",
    "ModsSinceLastAnalyze",
    "PctModsSinceLastAnalyze"
FROM orderedrows;

ALTER VIEW gda."vwStatsForLevel1DigMaintenance" OWNER TO geotabdigadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create View: vwStatsForLevel2DigMaintenance
--
-- Every index in the gda schema, with its size and its bloat ratio: the fraction
-- of the index that is free space, as a RATIO (0..1). Unlike the Level 1 view
-- this applies no threshold of its own. The adapter reindexes where the bloat
-- ratio exceeds 0.3 and the index is larger than 1000 bytes, and those two
-- thresholds live only in the adapter (DigMaintenanceThresholds).
--
-- Reading this view reads every gda index in full, which is why the adapter
-- reads it only on a Level 2 run, and only once it has confirmed that
-- pgstattuple is usable: without the extension, or without EXECUTE on it, this
-- view raises an error.
--
-- An index on a partitioned table is not listed itself - pg_stat_user_indexes
-- has no row for it - but the index on each of its partitions is, and each of
-- those is reindexed on its own.
--
-- Mirrors the MyGeotab API Adapter's public."vwStatsForLevel2DBMaintenance",
-- scoped to the gda schema instead of public, and, like that view from its
-- version 5.2.0.0, reading each index once (see fnDigIndexSpaceStats).
CREATE VIEW gda."vwStatsForLevel2DigMaintenance" AS
WITH index_info AS (
    SELECT i.schemaname,
        i.relname AS tablename,
        i.indexrelname AS indexname,
        i.indexrelid AS index_oid
    FROM pg_catalog.pg_stat_user_indexes i
    WHERE i.schemaname = 'gda'
), pgstattuple_stats AS (
    SELECT idx.index_oid,
        st."FreeSpace" AS free_space,
        st."TableLength" AS table_len
    FROM index_info idx
    CROSS JOIN LATERAL gda."fnDigIndexSpaceStats"(idx.index_oid) st
), index_sizes AS (
    SELECT index_info.index_oid,
        pg_catalog.pg_relation_size(index_info.index_oid::regclass) AS total_size
    FROM index_info
), results AS (
    SELECT i.indexname,
        i.schemaname,
        i.tablename,
        pg_catalog.pg_size_pretty(s.total_size) AS index_size,
        s.total_size AS index_size_bytes,
        (ps.free_space::double precision / NULLIF(ps.table_len, 0)::double precision) AS bloat_ratio
    FROM index_info i
    JOIN pgstattuple_stats ps ON i.index_oid = ps.index_oid
    JOIN index_sizes s ON i.index_oid = s.index_oid
), orderedrows AS (
    SELECT results.schemaname AS "SchemaName",
        results.tablename AS "TableName",
        results.indexname AS "IndexName",
        results.index_size AS "IndexSizeText",
        results.index_size_bytes AS "IndexSize",
        COALESCE(results.bloat_ratio, 0.0::double precision) AS "IndexBloatRatio"
    FROM results
    ORDER BY results.index_size_bytes DESC, results.bloat_ratio DESC
)
SELECT row_number() OVER () AS "RowId",
    "SchemaName",
    "TableName",
    "IndexName",
    "IndexSizeText",
    "IndexSize",
    "IndexBloatRatio"
FROM orderedrows;

ALTER VIEW gda."vwStatsForLevel2DigMaintenance" OWNER TO geotabdigadapter_client;

/*** [END] Part 7: Database Maintenance ***/


/*** [START] Part 8: Database Version Update ***/
-- Insert a record into the MiddlewareVersionInfo table to reflect the current
-- database version.
INSERT INTO gda."MiddlewareVersionInfo" ("DatabaseVersion", "RecordCreationTimeUtc")
VALUES ('5.2.0.0', (now() AT TIME ZONE 'UTC'));
/*** [END] Part 8: Database Version Update ***/
