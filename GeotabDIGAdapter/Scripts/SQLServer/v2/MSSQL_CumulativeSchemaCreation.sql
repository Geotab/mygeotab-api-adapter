-- ================================================================================
-- DATABASE TYPE: SQL Server
--
-- NOTES:
--   1: This script applies to the Geotab DIG Adapter database starting with
--      application version 5.0.0.0. It does not apply to earlier versions of the
--      application.
--   2: This script will be updated with future schema changes such that someone
--      starting with a future version of the application can use this script as a
--      starting point without having to apply any earlier incremental updates.
--   3: Be sure to alter the "USE [geotabadapterdb]" statement below if you have
--      changed the database name to something else.
--
-- DESCRIPTION:
--   This script is intended for use in creating the database schema in an empty
--   database starting from version 5.0.0.0 of the Geotab DIG Adapter and including
--   any cumulative schema changes up to the current application version at time of
--   download.
--
--   Use this script for a FRESH INSTALL. To upgrade an EXISTING database, run the
--   incremental MSSQL_<version>_*.sql scripts instead, in version order.
--
-- PARTITIONED TABLES:
--   The thirteen Q_*Fail tables are partitioned on [RecordCreationTimeUtc] so that
--   old failure records can be reclaimed by switching out partitions instead of by
--   deleting rows. Four consequences are worth knowing before you read on:
--
--     a. Their clustered primary key is ([RecordCreationTimeUtc], [id]), not ([id])
--        alone, because the clustered index is what places the table on the
--        partition scheme. [id] remains unique on its own in practice, because it
--        is still served by the table's IDENTITY property.
--     b. Their nonclustered index is built on the same partition scheme, which
--        makes it ALIGNED and is what allows a partition to be switched out.
--     c. This script creates its OWN partition function and scheme, mapped
--        ALL TO ([PRIMARY]). It therefore has no filegroup prerequisites and can be
--        run against an empty database on its own - unlike the MyGeotab API
--        Adapter's equivalent script, which requires filegroups to be provisioned
--        first. Using [PRIMARY] rather than a filegroup per period also keeps a
--        DIG-only deployment compatible with Azure SQL Database, and keeps these
--        tables invisible to the API Adapter's filegroup sweep when the two share a
--        database.
--     d. The live Q_* queue tables are deliberately NOT partitioned. They are work
--        queues whose rows are deleted once transmitted, not accumulators, and a
--        time-based partition switch-out on one would discard unprocessed records.
-- ================================================================================
USE [geotabadapterdb]
GO

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Partition Function and Scheme for the Q_*Fail tables.
--
-- These MUST exist before the Q_*Fail tables are created, because a table is
-- placed on a partition scheme at CREATE TABLE time.
--
-- The single boundary value is a fixed sentinel, not a date derived from "today",
-- so that every installation starts from an identical partition layout regardless
-- of when it was installed. spManageDigPartitions splits forward from here to
-- create the real period partitions; SQL Server redistributes rows automatically
-- as part of each SPLIT, so nothing is stranded in the meantime.
--
-- Because the scheme is mapped ALL TO ([PRIMARY]), [PRIMARY] stays permanently
-- marked as the next used filegroup and no ALTER PARTITION SCHEME ... NEXT USED is
-- needed before a SPLIT.
IF NOT EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = 'DateTimePartitionFunction_GeotabDigAdapter')
BEGIN
    CREATE PARTITION FUNCTION [DateTimePartitionFunction_GeotabDigAdapter] (DATETIME2(7))
        AS RANGE RIGHT FOR VALUES ('2000-01-01');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.partition_schemes WHERE name = 'DateTimePartitionScheme_GeotabDigAdapter')
BEGIN
    CREATE PARTITION SCHEME [DateTimePartitionScheme_GeotabDigAdapter]
        AS PARTITION [DateTimePartitionFunction_GeotabDigAdapter] ALL TO ([PRIMARY]);
END
GO

/*** [START] SSMS-Generated Script Below ***/
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: MiddlewareVersionInfo
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [gda].[MiddlewareVersionInfo](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[DatabaseVersion] [nvarchar](50) NOT NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_MiddlewareVersionInfo2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: OServiceTracking
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [gda].[OServiceTracking](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[ServiceId] [nvarchar](50) NOT NULL,
	[AdapterVersion] [nvarchar](50) NULL,
	[AdapterMachineName] [nvarchar](100) NULL,
	[EntitiesLastProcessedUtc] [datetime2](7) NULL,
	[LastProcessedFeedVersion] [bigint] NULL,
	[LastBatchSize] [int] NULL,
	[SuccessCount] [bigint] NULL,
	[FailureCount] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_OServiceTracking] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

-- One row per service. Without this, two instances cold-starting against the same
-- database both insert the full set and every later startup fails on duplicates.
CREATE UNIQUE NONCLUSTERED INDEX [IX_OServiceTracking_ServiceId]
ON [gda].[OServiceTracking] ([ServiceId]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: ProvisionedDevices
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [gda].[ProvisionedDevices](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[ThirdPartyId] [varchar](50) NOT NULL,
	[ErpNo] [varchar](50) NULL,
	[GeotabSerialNumber] [varchar](50) NOT NULL,
	[IsOkayToSendDataToGeotab] [bit] NOT NULL,
	[DeviceProvisionedDateTimeUtc] [datetime2](7) NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ProvisionedDevices] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_ProvisionedDevices_ThirdPartyId_Includes]
ON [gda].[ProvisionedDevices] ([ThirdPartyId])
INCLUDE ([GeotabSerialNumber], [IsOkayToSendDataToGeotab]); 
GO

-- Serves DeviceDIGReadinessService's batch query, which runs every 10 seconds:
--
--     SELECT TOP (@BatchSize) * FROM gda.ProvisionedDevices
--     WHERE IsOkayToSendDataToGeotab = 0
--     ORDER BY RecordLastChangedUtc;
--
-- Neither the predicate nor the ORDER BY had an index, so that statement read and
-- sorted the whole table 8,640 times a day, at a cost growing with the size of the
-- fleet. The HEALTHY case is the WORST case: once every device is DIG-ready nothing
-- matches, so the TOP can never be filled and there is no early exit - every row is
-- read to conclude that none qualify.
--
-- Measured on this table with the statement the service issues: 5.9 ms -> 0.4 ms at
-- 50,000 devices and 23.8 ms -> 0.4 ms at 250,000 - the cost stops growing with the
-- fleet. The filter is what makes it nearly free: the index holds only the devices
-- still awaiting a readiness check, so on a healthy fleet it holds NOTHING and
-- occupies no pages at all. It costs ~53% on a 5,000-row provisioning insert -
-- 90 ms to 138 ms - against a provisioning path that is MyAdmin-bound at about six
-- seconds PER DEVICE, and it makes the service's own write-back no slower.
--
-- KEY ONLY, deliberately. A covering variant carrying the other columns in an
-- INCLUDE list was measured and REJECTED: it is no better in the healthy case
-- (0.6 ms against 0.4 ms) and materially WORSE while a backlog is being worked off
-- (+33.7% at 50,000 devices, +17.4% at 250,000), for three to four times the size.
-- Do not re-propose it.
CREATE NONCLUSTERED INDEX [IX_ProvisionedDevices_PendingDIGReadiness]
ON [gda].[ProvisionedDevices] ([RecordLastChangedUtc])
WHERE ([IsOkayToSendDataToGeotab] = 0);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_ProvisionDevices
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [gda].[Q_ProvisionDevices](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [ErpNo] [varchar](50) NULL,
    [HardwareId] [int] NULL,
    [ProductId] [int] NOT NULL,
    [PromoCode] [varchar](50) NULL,
    [SubPlan] [varchar](50) NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_Prov_Created] DEFAULT (SYSUTCDATETIME()),
    [RecordLastChangedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_Prov_LastChanged] DEFAULT (SYSUTCDATETIME()),
    [ProcessingStatus] [tinyint] NOT NULL CONSTRAINT [DF_Q_Prov_ProcessingStatus] DEFAULT (0),
    [ProcessingStartTimeUtc] [datetime2](7) NULL,
    [RetryCount] [tinyint] NOT NULL CONSTRAINT [DF_Q_Prov_Retry] DEFAULT (0),
 CONSTRAINT [PK_Q_ProvisionDevices] PRIMARY KEY CLUSTERED 
(
    [id] ASC
)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_ProvisionDevices_PendingWork]
ON [gda].[Q_ProvisionDevices] ([id])
INCLUDE ([ThirdPartyId], [ErpNo], [HardwareId], [ProductId], [PromoCode], [SubPlan])
WHERE ([ProcessingStatus] = 0);
GO

-- Two indexes for the claim procedure. The PendingWork index above serves neither
-- statement: it is filtered on [ProcessingStatus] = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that filter
-- - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take @BatchSize rows, so the cost grows with queue
-- depth - worst exactly when the queue has backed up and throughput matters most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the
-- DELETE TOP (@BatchSize) can never be filled, so unindexed every row is read before
-- concluding that none qualify. [RetryCount] > 0 is a deliberate SUPERSET of the
-- sweep's own [RetryCount] >= @MaxClaimAttempts - the configured ceiling is never
-- below 1 - so this predicate needs no revision if that floor moves, and the index
-- holds only rows a device-lookup hold or a stale reclaim has touched - few or none
-- on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,073 ms to 464 ms here and from 5,953 ms to 90 ms on PostgreSQL. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. Those SQL Server figures predate the
-- sweep's literal [RetryCount] > 0, without which it could not use the filtered index
-- at all (by itself that index measured +4.6%); with it, a claim on a steady
-- 1,000,000-row queue fell from 495 ms to 324 ms. They cost 4.3% on the insert path,
-- which retains ~25x headroom over the feed either way.
CREATE NONCLUSTERED INDEX [IX_Q_ProvisionDevices_RecordCreationTimeUtc]
ON [gda].[Q_ProvisionDevices] ([RecordCreationTimeUtc]);
GO

CREATE NONCLUSTERED INDEX [IX_Q_ProvisionDevices_Exhausted]
ON [gda].[Q_ProvisionDevices] ([id])
WHERE ([RetryCount] > 0);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_ProvisionDevicesFail
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [gda].[Q_ProvisionDevicesFail](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [OriginalQueueId] [bigint] NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [ErpNo] [varchar](50) NULL,
    [HardwareId] [int] NULL,
    [ProductId] [int] NOT NULL,
    [PromoCode] [varchar](50) NULL,
    [SubPlan] [varchar](50) NULL,
    [OriginalRecordLastChangedUtc] [datetime2](7) NOT NULL,
    [FailureReason] [nvarchar](max) NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_ProvFail_Date] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_Q_ProvisionDevicesFail] PRIMARY KEY CLUSTERED ([RecordCreationTimeUtc] ASC, [id] ASC)
    ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
) ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
GO

CREATE NONCLUSTERED INDEX [IX_Q_ProvisionDevicesFail_ThirdPartyId]
ON [gda].[Q_ProvisionDevicesFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc])
ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc]);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQProvisionDevicesBatch
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQProvisionDevicesBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_ProvisionDevices records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is identified by the OUTPUT of the claiming UPDATE, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the GETUTCDATE() value just written, and
--     two instances claiming inside one ~3.33 ms tick each received BOTH batches - so
--     the atomic pop was stated here but not delivered. See the note in the body.
-- Parameters:
--   @BatchSize
--     Maximum number of records to claim in a single batch.
--   @StaleThresholdMinutes
--     Minutes after which an "in progress" record is considered stale and eligible for
--     reclaim.
--   @MaxClaimAttempts
--     Claims a record may accumulate before it is treated as poison and moved to
--     Q_ProvisionDevicesFail. Optional; when NULL a record is reclaimed indefinitely, which
--     is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_ProvisionDevices records, in RecordCreationTimeUtc, id order.
CREATE PROCEDURE [gda].[spClaimQProvisionDevicesBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT,
    @MaxClaimAttempts INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- RecordCreationTimeUtc is NOT carried across. It is the fail table's partitioning
    -- column and defaults to now, so the row lands in the current partition and is
    -- retired on the retention schedule like every other failure.
    -- Bounded by @BatchSize, like every other statement in this procedure. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to @BatchSize - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. There is no ORDER BY, because every qualifying row has the same destination
    -- and ordering would buy a sort and no guarantee.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply ProcessingStatus = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with RetryCount + 1; skipping
    -- every claimed row would mean it is never swept, which is the infinite retry this ceiling
    -- exists to end. Mirroring the claim's own predicate removes abandoned rows and protects
    -- live ones.
    --
    -- The literal [RetryCount] > 0 changes no result - the ceiling is never below 1 - and it is
    -- what lets the optimizer use the queue's filtered _Exhausted index (WHERE [RetryCount] > 0).
    -- A cached plan must be valid for every value of @MaxClaimAttempts, so a comparison with the
    -- parameter alone can never match a filtered index: without the literal this statement read
    -- the whole queue before every claim, the healthy case included - measured at 1,000,000 rows
    -- and 9,391 logical reads per claim on a 1,000,000-row queue, and the whole claim at 495 ms
    -- against 324 ms with it. READPAST belongs to the same change. Once the DELETE seeks the
    -- index it looks each row up in the clustered index, and without READPAST that lookup would
    -- wait on - and could deadlock with - a row another session has locked. A row it skips is
    -- taken by a later pass, as with the bound above.
    IF @MaxClaimAttempts IS NOT NULL
    BEGIN
        DELETE TOP (@BatchSize) q
        OUTPUT deleted.[id],
               deleted.[ThirdPartyId], deleted.[ErpNo], deleted.[HardwareId],
               deleted.[ProductId], deleted.[PromoCode], deleted.[SubPlan],
               deleted.[RecordLastChangedUtc],
               CONCAT('Exceeded ', @MaxClaimAttempts, ' claim attempts.')
          INTO [gda].[Q_ProvisionDevicesFail] (
               [OriginalQueueId],
               [ThirdPartyId], [ErpNo], [HardwareId], [ProductId], [PromoCode],
               [SubPlan],
               [OriginalRecordLastChangedUtc],
               [FailureReason])
        FROM [gda].[Q_ProvisionDevices] q WITH (READPAST)
        WHERE q.[RetryCount] >= @MaxClaimAttempts
          AND q.[RetryCount] > 0
          AND (q.[ProcessingStatus] = 0
               OR q.[ProcessingStartTimeUtc] < @StaleThreshold);
    END

    -- The batch this call claimed is identified by OUTPUT, not by the timestamp it
    -- wrote. Re-reading the queue for ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    -- is NOT safe: GETUTCDATE() advances in ~3.33 ms steps, so two adapter instances
    -- claiming within one tick write the same value and each one's re-read matches
    -- BOTH batches. ProcessingStartTimeUtc is still stamped, because the stale-reclaim
    -- predicate above reads it; it is simply no longer how the batch is identified.
    --
    -- Only the ids are captured, and the rows are then projected by id. That diverges
    -- from spClaimQDeviceSubscriptionsBatch, which captures every column into its table
    -- variable, and the divergence is deliberate: an id-only capture carries no column
    -- list to keep in step with the queue table, and no collation to declare. The id
    -- set is exact - ROWLOCK, UPDLOCK, READPAST means no other claimer holds these rows
    -- - so the join is a projection of a set already fixed, not a second selection.
    --
    -- The ORDER BY on the way out is what holds the "in RecordCreationTimeUtc, id
    -- order" contract. An OUTPUT clause has no row order of its own, and the ORDER BY
    -- inside the CTE below chooses only WHICH rows are claimed.
    --
    -- Both ORDER BYs carry id as a tiebreaker. RecordCreationTimeUtc alone is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a TOP (@BatchSize) takes, and the
    -- order the batch comes back in, could differ between two runs of this procedure.
    DECLARE @ClaimedIds TABLE ([id] BIGINT NOT NULL PRIMARY KEY);

    -- Use a CTE to select ordered rows, then update them atomically.
    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_ProvisionDevices] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0  -- Available
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc, id
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,  -- InProgress
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END
    OUTPUT inserted.[id] INTO @ClaimedIds;

    -- Return the claimed records.
    SELECT q.*
    FROM [gda].[Q_ProvisionDevices] q
    INNER JOIN @ClaimedIds c ON c.[id] = q.[id]
    ORDER BY q.[RecordCreationTimeUtc], q.[id];
END;
GO

-- Grant execute permission to the client user/role
GRANT EXECUTE ON [gda].[spClaimQProvisionDevicesBatch] TO [geotabdigadapter_client];
GO


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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [gda].[Q_GpsRecords](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [Latitude] [real] NOT NULL,
    [Longitude] [real] NOT NULL,
    [Speed] [real] NULL,
    [IsGpsValid] [bit] NULL,
    [IsIgnitionOn] [bit] NULL,
    [IsAuxiliary1On] [bit] NULL,
    [IsAuxiliary2On] [bit] NULL,
    [IsAuxiliary3On] [bit] NULL,
    [IsAuxiliary4On] [bit] NULL,
    [IsAuxiliary5On] [bit] NULL,
    [IsAuxiliary6On] [bit] NULL,
    [IsAuxiliary7On] [bit] NULL,
    [IsAuxiliary8On] [bit] NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_Gps_Created] DEFAULT (SYSUTCDATETIME()),
    [RecordLastChangedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_Gps_LastChanged] DEFAULT (SYSUTCDATETIME()),
    [ProcessingStatus] [tinyint] NOT NULL CONSTRAINT [DF_Q_Gps_ProcessingStatus] DEFAULT (0),
    [ProcessingStartTimeUtc] [datetime2](7) NULL,
    [RetryCount] [tinyint] NOT NULL CONSTRAINT [DF_Q_Gps_Retry] DEFAULT (0),
 CONSTRAINT [PK_Q_GpsRecords] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_GpsRecords_PendingWork]
ON [gda].[Q_GpsRecords] ([id])
INCLUDE ([ThirdPartyId], [DateTime], [Latitude], [Longitude], [Speed])
WHERE ([ProcessingStatus] = 0);
GO

-- Two indexes for the claim procedure. The PendingWork index above serves neither
-- statement: it is filtered on [ProcessingStatus] = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that filter
-- - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take @BatchSize rows, so the cost grows with queue
-- depth - worst exactly when the queue has backed up and throughput matters most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the
-- DELETE TOP (@BatchSize) can never be filled, so unindexed every row is read before
-- concluding that none qualify. [RetryCount] > 0 is a deliberate SUPERSET of the
-- sweep's own [RetryCount] >= @MaxClaimAttempts - the configured ceiling is never
-- below 1 - so this predicate needs no revision if that floor moves, and the index
-- holds only rows a device-lookup hold or a stale reclaim has touched - few or none
-- on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,073 ms to 464 ms here and from 5,953 ms to 90 ms on PostgreSQL. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. Those SQL Server figures predate the
-- sweep's literal [RetryCount] > 0, without which it could not use the filtered index
-- at all (by itself that index measured +4.6%); with it, a claim on a steady
-- 1,000,000-row queue fell from 495 ms to 324 ms. They cost 4.3% on the insert path,
-- which retains ~25x headroom over the feed either way.
CREATE NONCLUSTERED INDEX [IX_Q_GpsRecords_RecordCreationTimeUtc]
ON [gda].[Q_GpsRecords] ([RecordCreationTimeUtc]);
GO

CREATE NONCLUSTERED INDEX [IX_Q_GpsRecords_Exhausted]
ON [gda].[Q_GpsRecords] ([id])
WHERE ([RetryCount] > 0);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_GpsRecordsFail
CREATE TABLE [gda].[Q_GpsRecordsFail](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [OriginalQueueId] [bigint] NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [Latitude] [real] NOT NULL,
    [Longitude] [real] NOT NULL,
    [Speed] [real] NULL,
    [IsGpsValid] [bit] NULL,
    [IsIgnitionOn] [bit] NULL,
    [IsAuxiliary1On] [bit] NULL,
    [IsAuxiliary2On] [bit] NULL,
    [IsAuxiliary3On] [bit] NULL,
    [IsAuxiliary4On] [bit] NULL,
    [IsAuxiliary5On] [bit] NULL,
    [IsAuxiliary6On] [bit] NULL,
    [IsAuxiliary7On] [bit] NULL,
    [IsAuxiliary8On] [bit] NULL,
    [OriginalRecordLastChangedUtc] [datetime2](7) NOT NULL,
    [FailureReason] [nvarchar](max) NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_GpsFail_Created] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_Q_GpsRecordsFail] PRIMARY KEY CLUSTERED ([RecordCreationTimeUtc] ASC, [id] ASC)
    ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
) ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
GO

CREATE NONCLUSTERED INDEX [IX_Q_GpsRecordsFail_ThirdPartyId]
ON [gda].[Q_GpsRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc])
ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQGpsRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_GpsRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is identified by the OUTPUT of the claiming UPDATE, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the GETUTCDATE() value just written, and
--     two instances claiming inside one ~3.33 ms tick each received BOTH batches - so
--     the atomic pop was stated here but not delivered. See the note in the body.
-- Parameters:
--   @BatchSize
--     Maximum number of records to claim in a single batch.
--   @StaleThresholdMinutes
--     Minutes after which an "in progress" record is considered stale and eligible for
--     reclaim.
--   @MaxClaimAttempts
--     Claims a record may accumulate before it is treated as poison and moved to
--     Q_GpsRecordsFail. Optional; when NULL a record is reclaimed indefinitely, which
--     is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_GpsRecords records, in RecordCreationTimeUtc, id order.
CREATE PROCEDURE [gda].[spClaimQGpsRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT,
    @MaxClaimAttempts INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- RecordCreationTimeUtc is NOT carried across. It is the fail table's partitioning
    -- column and defaults to now, so the row lands in the current partition and is
    -- retired on the retention schedule like every other failure.
    -- Bounded by @BatchSize, like every other statement in this procedure. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to @BatchSize - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. There is no ORDER BY, because every qualifying row has the same destination
    -- and ordering would buy a sort and no guarantee.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply ProcessingStatus = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with RetryCount + 1; skipping
    -- every claimed row would mean it is never swept, which is the infinite retry this ceiling
    -- exists to end. Mirroring the claim's own predicate removes abandoned rows and protects
    -- live ones.
    --
    -- The literal [RetryCount] > 0 changes no result - the ceiling is never below 1 - and it is
    -- what lets the optimizer use the queue's filtered _Exhausted index (WHERE [RetryCount] > 0).
    -- A cached plan must be valid for every value of @MaxClaimAttempts, so a comparison with the
    -- parameter alone can never match a filtered index: without the literal this statement read
    -- the whole queue before every claim, the healthy case included - measured at 1,000,000 rows
    -- and 9,391 logical reads per claim on a 1,000,000-row queue, and the whole claim at 495 ms
    -- against 324 ms with it. READPAST belongs to the same change. Once the DELETE seeks the
    -- index it looks each row up in the clustered index, and without READPAST that lookup would
    -- wait on - and could deadlock with - a row another session has locked. A row it skips is
    -- taken by a later pass, as with the bound above.
    IF @MaxClaimAttempts IS NOT NULL
    BEGIN
        DELETE TOP (@BatchSize) q
        OUTPUT deleted.[id],
               deleted.[ThirdPartyId], deleted.[DateTime], deleted.[Latitude],
               deleted.[Longitude], deleted.[Speed], deleted.[IsGpsValid],
               deleted.[IsIgnitionOn], deleted.[IsAuxiliary1On],
               deleted.[IsAuxiliary2On], deleted.[IsAuxiliary3On],
               deleted.[IsAuxiliary4On], deleted.[IsAuxiliary5On],
               deleted.[IsAuxiliary6On], deleted.[IsAuxiliary7On],
               deleted.[IsAuxiliary8On],
               deleted.[RecordLastChangedUtc],
               CONCAT('Exceeded ', @MaxClaimAttempts, ' claim attempts.')
          INTO [gda].[Q_GpsRecordsFail] (
               [OriginalQueueId],
               [ThirdPartyId], [DateTime], [Latitude], [Longitude], [Speed],
               [IsGpsValid], [IsIgnitionOn], [IsAuxiliary1On], [IsAuxiliary2On],
               [IsAuxiliary3On], [IsAuxiliary4On], [IsAuxiliary5On], [IsAuxiliary6On],
               [IsAuxiliary7On], [IsAuxiliary8On],
               [OriginalRecordLastChangedUtc],
               [FailureReason])
        FROM [gda].[Q_GpsRecords] q WITH (READPAST)
        WHERE q.[RetryCount] >= @MaxClaimAttempts
          AND q.[RetryCount] > 0
          AND (q.[ProcessingStatus] = 0
               OR q.[ProcessingStartTimeUtc] < @StaleThreshold);
    END

    -- The batch this call claimed is identified by OUTPUT, not by the timestamp it
    -- wrote. Re-reading the queue for ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    -- is NOT safe: GETUTCDATE() advances in ~3.33 ms steps, so two adapter instances
    -- claiming within one tick write the same value and each one's re-read matches
    -- BOTH batches. ProcessingStartTimeUtc is still stamped, because the stale-reclaim
    -- predicate above reads it; it is simply no longer how the batch is identified.
    --
    -- Only the ids are captured, and the rows are then projected by id. That diverges
    -- from spClaimQDeviceSubscriptionsBatch, which captures every column into its table
    -- variable, and the divergence is deliberate: an id-only capture carries no column
    -- list to keep in step with the queue table, and no collation to declare. The id
    -- set is exact - ROWLOCK, UPDLOCK, READPAST means no other claimer holds these rows
    -- - so the join is a projection of a set already fixed, not a second selection.
    --
    -- The ORDER BY on the way out is what holds the "in RecordCreationTimeUtc, id
    -- order" contract. An OUTPUT clause has no row order of its own, and the ORDER BY
    -- inside the CTE below chooses only WHICH rows are claimed.
    --
    -- Both ORDER BYs carry id as a tiebreaker. RecordCreationTimeUtc alone is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a TOP (@BatchSize) takes, and the
    -- order the batch comes back in, could differ between two runs of this procedure.
    DECLARE @ClaimedIds TABLE ([id] BIGINT NOT NULL PRIMARY KEY);

    -- Use a CTE to select ordered rows, then update them atomically.
    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_GpsRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0  -- Available
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc, id
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,  -- InProgress
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END
    OUTPUT inserted.[id] INTO @ClaimedIds;

    -- Return the claimed records.
    SELECT q.*
    FROM [gda].[Q_GpsRecords] q
    INNER JOIN @ClaimedIds c ON c.[id] = q.[id]
    ORDER BY q.[RecordCreationTimeUtc], q.[id];
END;
GO

GRANT EXECUTE ON [gda].[spClaimQGpsRecordsBatch] TO [geotabdigadapter_client];
GO


-- ================================================================================
-- 2. ACCELERATION RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_AccelerationRecords
CREATE TABLE [gda].[Q_AccelerationRecords](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [X] [smallint] NOT NULL,
    [Y] [smallint] NOT NULL,
    [Z] [smallint] NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_Accel_Created] DEFAULT (SYSUTCDATETIME()),
    [RecordLastChangedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_Accel_LastChanged] DEFAULT (SYSUTCDATETIME()),
    [ProcessingStatus] [tinyint] NOT NULL CONSTRAINT [DF_Q_Accel_ProcessingStatus] DEFAULT (0),
    [ProcessingStartTimeUtc] [datetime2](7) NULL,
    [RetryCount] [tinyint] NOT NULL CONSTRAINT [DF_Q_Accel_Retry] DEFAULT (0),
 CONSTRAINT [PK_Q_AccelerationRecords] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_AccelerationRecords_PendingWork]
ON [gda].[Q_AccelerationRecords] ([id])
INCLUDE ([ThirdPartyId], [DateTime], [X], [Y], [Z])
WHERE ([ProcessingStatus] = 0);
GO

-- Two indexes for the claim procedure. The PendingWork index above serves neither
-- statement: it is filtered on [ProcessingStatus] = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that filter
-- - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take @BatchSize rows, so the cost grows with queue
-- depth - worst exactly when the queue has backed up and throughput matters most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the
-- DELETE TOP (@BatchSize) can never be filled, so unindexed every row is read before
-- concluding that none qualify. [RetryCount] > 0 is a deliberate SUPERSET of the
-- sweep's own [RetryCount] >= @MaxClaimAttempts - the configured ceiling is never
-- below 1 - so this predicate needs no revision if that floor moves, and the index
-- holds only rows a device-lookup hold or a stale reclaim has touched - few or none
-- on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,073 ms to 464 ms here and from 5,953 ms to 90 ms on PostgreSQL. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. Those SQL Server figures predate the
-- sweep's literal [RetryCount] > 0, without which it could not use the filtered index
-- at all (by itself that index measured +4.6%); with it, a claim on a steady
-- 1,000,000-row queue fell from 495 ms to 324 ms. They cost 4.3% on the insert path,
-- which retains ~25x headroom over the feed either way.
CREATE NONCLUSTERED INDEX [IX_Q_AccelerationRecords_RecordCreationTimeUtc]
ON [gda].[Q_AccelerationRecords] ([RecordCreationTimeUtc]);
GO

CREATE NONCLUSTERED INDEX [IX_Q_AccelerationRecords_Exhausted]
ON [gda].[Q_AccelerationRecords] ([id])
WHERE ([RetryCount] > 0);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_AccelerationRecordsFail
CREATE TABLE [gda].[Q_AccelerationRecordsFail](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [OriginalQueueId] [bigint] NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [X] [smallint] NOT NULL,
    [Y] [smallint] NOT NULL,
    [Z] [smallint] NOT NULL,
    [OriginalRecordLastChangedUtc] [datetime2](7) NOT NULL,
    [FailureReason] [nvarchar](max) NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_AccelFail_Created] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_Q_AccelerationRecordsFail] PRIMARY KEY CLUSTERED ([RecordCreationTimeUtc] ASC, [id] ASC)
    ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
) ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
GO

CREATE NONCLUSTERED INDEX [IX_Q_AccelerationRecordsFail_ThirdPartyId]
ON [gda].[Q_AccelerationRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc])
ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQAccelerationRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_AccelerationRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is identified by the OUTPUT of the claiming UPDATE, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the GETUTCDATE() value just written, and
--     two instances claiming inside one ~3.33 ms tick each received BOTH batches - so
--     the atomic pop was stated here but not delivered. See the note in the body.
-- Parameters:
--   @BatchSize
--     Maximum number of records to claim in a single batch.
--   @StaleThresholdMinutes
--     Minutes after which an "in progress" record is considered stale and eligible for
--     reclaim.
--   @MaxClaimAttempts
--     Claims a record may accumulate before it is treated as poison and moved to
--     Q_AccelerationRecordsFail. Optional; when NULL a record is reclaimed indefinitely, which
--     is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_AccelerationRecords records, in RecordCreationTimeUtc, id order.
CREATE PROCEDURE [gda].[spClaimQAccelerationRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT,
    @MaxClaimAttempts INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- RecordCreationTimeUtc is NOT carried across. It is the fail table's partitioning
    -- column and defaults to now, so the row lands in the current partition and is
    -- retired on the retention schedule like every other failure.
    -- Bounded by @BatchSize, like every other statement in this procedure. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to @BatchSize - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. There is no ORDER BY, because every qualifying row has the same destination
    -- and ordering would buy a sort and no guarantee.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply ProcessingStatus = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with RetryCount + 1; skipping
    -- every claimed row would mean it is never swept, which is the infinite retry this ceiling
    -- exists to end. Mirroring the claim's own predicate removes abandoned rows and protects
    -- live ones.
    --
    -- The literal [RetryCount] > 0 changes no result - the ceiling is never below 1 - and it is
    -- what lets the optimizer use the queue's filtered _Exhausted index (WHERE [RetryCount] > 0).
    -- A cached plan must be valid for every value of @MaxClaimAttempts, so a comparison with the
    -- parameter alone can never match a filtered index: without the literal this statement read
    -- the whole queue before every claim, the healthy case included - measured at 1,000,000 rows
    -- and 9,391 logical reads per claim on a 1,000,000-row queue, and the whole claim at 495 ms
    -- against 324 ms with it. READPAST belongs to the same change. Once the DELETE seeks the
    -- index it looks each row up in the clustered index, and without READPAST that lookup would
    -- wait on - and could deadlock with - a row another session has locked. A row it skips is
    -- taken by a later pass, as with the bound above.
    IF @MaxClaimAttempts IS NOT NULL
    BEGIN
        DELETE TOP (@BatchSize) q
        OUTPUT deleted.[id],
               deleted.[ThirdPartyId], deleted.[DateTime], deleted.[X], deleted.[Y],
               deleted.[Z],
               deleted.[RecordLastChangedUtc],
               CONCAT('Exceeded ', @MaxClaimAttempts, ' claim attempts.')
          INTO [gda].[Q_AccelerationRecordsFail] (
               [OriginalQueueId],
               [ThirdPartyId], [DateTime], [X], [Y], [Z],
               [OriginalRecordLastChangedUtc],
               [FailureReason])
        FROM [gda].[Q_AccelerationRecords] q WITH (READPAST)
        WHERE q.[RetryCount] >= @MaxClaimAttempts
          AND q.[RetryCount] > 0
          AND (q.[ProcessingStatus] = 0
               OR q.[ProcessingStartTimeUtc] < @StaleThreshold);
    END

    -- The batch this call claimed is identified by OUTPUT, not by the timestamp it
    -- wrote. Re-reading the queue for ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    -- is NOT safe: GETUTCDATE() advances in ~3.33 ms steps, so two adapter instances
    -- claiming within one tick write the same value and each one's re-read matches
    -- BOTH batches. ProcessingStartTimeUtc is still stamped, because the stale-reclaim
    -- predicate above reads it; it is simply no longer how the batch is identified.
    --
    -- Only the ids are captured, and the rows are then projected by id. That diverges
    -- from spClaimQDeviceSubscriptionsBatch, which captures every column into its table
    -- variable, and the divergence is deliberate: an id-only capture carries no column
    -- list to keep in step with the queue table, and no collation to declare. The id
    -- set is exact - ROWLOCK, UPDLOCK, READPAST means no other claimer holds these rows
    -- - so the join is a projection of a set already fixed, not a second selection.
    --
    -- The ORDER BY on the way out is what holds the "in RecordCreationTimeUtc, id
    -- order" contract. An OUTPUT clause has no row order of its own, and the ORDER BY
    -- inside the CTE below chooses only WHICH rows are claimed.
    --
    -- Both ORDER BYs carry id as a tiebreaker. RecordCreationTimeUtc alone is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a TOP (@BatchSize) takes, and the
    -- order the batch comes back in, could differ between two runs of this procedure.
    DECLARE @ClaimedIds TABLE ([id] BIGINT NOT NULL PRIMARY KEY);

    -- Use a CTE to select ordered rows, then update them atomically.
    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_AccelerationRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0  -- Available
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc, id
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,  -- InProgress
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END
    OUTPUT inserted.[id] INTO @ClaimedIds;

    -- Return the claimed records.
    SELECT q.*
    FROM [gda].[Q_AccelerationRecords] q
    INNER JOIN @ClaimedIds c ON c.[id] = q.[id]
    ORDER BY q.[RecordCreationTimeUtc], q.[id];
END;
GO

GRANT EXECUTE ON [gda].[spClaimQAccelerationRecordsBatch] TO [geotabdigadapter_client];
GO


-- ================================================================================
-- 3. BINARY RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_BinaryRecords
CREATE TABLE [gda].[Q_BinaryRecords](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [Data] [varbinary](max) NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_Binary_Created] DEFAULT (SYSUTCDATETIME()),
    [RecordLastChangedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_Binary_LastChanged] DEFAULT (SYSUTCDATETIME()),
    [ProcessingStatus] [tinyint] NOT NULL CONSTRAINT [DF_Q_Binary_ProcessingStatus] DEFAULT (0),
    [ProcessingStartTimeUtc] [datetime2](7) NULL,
    [RetryCount] [tinyint] NOT NULL CONSTRAINT [DF_Q_Binary_Retry] DEFAULT (0),
 CONSTRAINT [PK_Q_BinaryRecords] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

-- No INCLUDE here, deliberately - this is the only PendingWork index without one.
-- The siblings cover their table's payload columns so the claim query is satisfied from the
-- index alone, but Q_BinaryRecords.Data is varbinary(max), the only unbounded payload in the
-- set, and a sibling-shaped INCLUDE would copy every blob into the index. (Q_BluetoothRecords
-- has a Data column too and its index does include it - that one is a [real].)
-- Covering [ThirdPartyId] and [DateTime] alone, without the blob, would be legitimate; it is a
-- performance judgement nobody has needed to make, not an oversight. Please do not "fix" this
-- by copying a sibling.
CREATE NONCLUSTERED INDEX [IX_Q_BinaryRecords_PendingWork]
ON [gda].[Q_BinaryRecords] ([id])
WHERE ([ProcessingStatus] = 0);
GO

-- Two indexes for the claim procedure. The PendingWork index above serves neither
-- statement: it is filtered on [ProcessingStatus] = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that filter
-- - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take @BatchSize rows, so the cost grows with queue
-- depth - worst exactly when the queue has backed up and throughput matters most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the
-- DELETE TOP (@BatchSize) can never be filled, so unindexed every row is read before
-- concluding that none qualify. [RetryCount] > 0 is a deliberate SUPERSET of the
-- sweep's own [RetryCount] >= @MaxClaimAttempts - the configured ceiling is never
-- below 1 - so this predicate needs no revision if that floor moves, and the index
-- holds only rows a device-lookup hold or a stale reclaim has touched - few or none
-- on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,073 ms to 464 ms here and from 5,953 ms to 90 ms on PostgreSQL. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. Those SQL Server figures predate the
-- sweep's literal [RetryCount] > 0, without which it could not use the filtered index
-- at all (by itself that index measured +4.6%); with it, a claim on a steady
-- 1,000,000-row queue fell from 495 ms to 324 ms. They cost 4.3% on the insert path,
-- which retains ~25x headroom over the feed either way.
CREATE NONCLUSTERED INDEX [IX_Q_BinaryRecords_RecordCreationTimeUtc]
ON [gda].[Q_BinaryRecords] ([RecordCreationTimeUtc]);
GO

CREATE NONCLUSTERED INDEX [IX_Q_BinaryRecords_Exhausted]
ON [gda].[Q_BinaryRecords] ([id])
WHERE ([RetryCount] > 0);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_BinaryRecordsFail
CREATE TABLE [gda].[Q_BinaryRecordsFail](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [OriginalQueueId] [bigint] NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [Data] [varbinary](max) NOT NULL,
    [OriginalRecordLastChangedUtc] [datetime2](7) NOT NULL,
    [FailureReason] [nvarchar](max) NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_BinaryFail_Created] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_Q_BinaryRecordsFail] PRIMARY KEY CLUSTERED ([RecordCreationTimeUtc] ASC, [id] ASC)
    ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
) ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
GO

CREATE NONCLUSTERED INDEX [IX_Q_BinaryRecordsFail_ThirdPartyId]
ON [gda].[Q_BinaryRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc])
ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQBinaryRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_BinaryRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is identified by the OUTPUT of the claiming UPDATE, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the GETUTCDATE() value just written, and
--     two instances claiming inside one ~3.33 ms tick each received BOTH batches - so
--     the atomic pop was stated here but not delivered. See the note in the body.
-- Parameters:
--   @BatchSize
--     Maximum number of records to claim in a single batch.
--   @StaleThresholdMinutes
--     Minutes after which an "in progress" record is considered stale and eligible for
--     reclaim.
--   @MaxClaimAttempts
--     Claims a record may accumulate before it is treated as poison and moved to
--     Q_BinaryRecordsFail. Optional; when NULL a record is reclaimed indefinitely, which
--     is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_BinaryRecords records, in RecordCreationTimeUtc, id order.
CREATE PROCEDURE [gda].[spClaimQBinaryRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT,
    @MaxClaimAttempts INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- RecordCreationTimeUtc is NOT carried across. It is the fail table's partitioning
    -- column and defaults to now, so the row lands in the current partition and is
    -- retired on the retention schedule like every other failure.
    -- Bounded by @BatchSize, like every other statement in this procedure. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to @BatchSize - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. There is no ORDER BY, because every qualifying row has the same destination
    -- and ordering would buy a sort and no guarantee.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply ProcessingStatus = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with RetryCount + 1; skipping
    -- every claimed row would mean it is never swept, which is the infinite retry this ceiling
    -- exists to end. Mirroring the claim's own predicate removes abandoned rows and protects
    -- live ones.
    --
    -- The literal [RetryCount] > 0 changes no result - the ceiling is never below 1 - and it is
    -- what lets the optimizer use the queue's filtered _Exhausted index (WHERE [RetryCount] > 0).
    -- A cached plan must be valid for every value of @MaxClaimAttempts, so a comparison with the
    -- parameter alone can never match a filtered index: without the literal this statement read
    -- the whole queue before every claim, the healthy case included - measured at 1,000,000 rows
    -- and 9,391 logical reads per claim on a 1,000,000-row queue, and the whole claim at 495 ms
    -- against 324 ms with it. READPAST belongs to the same change. Once the DELETE seeks the
    -- index it looks each row up in the clustered index, and without READPAST that lookup would
    -- wait on - and could deadlock with - a row another session has locked. A row it skips is
    -- taken by a later pass, as with the bound above.
    IF @MaxClaimAttempts IS NOT NULL
    BEGIN
        DELETE TOP (@BatchSize) q
        OUTPUT deleted.[id],
               deleted.[ThirdPartyId], deleted.[DateTime], deleted.[Data],
               deleted.[RecordLastChangedUtc],
               CONCAT('Exceeded ', @MaxClaimAttempts, ' claim attempts.')
          INTO [gda].[Q_BinaryRecordsFail] (
               [OriginalQueueId],
               [ThirdPartyId], [DateTime], [Data],
               [OriginalRecordLastChangedUtc],
               [FailureReason])
        FROM [gda].[Q_BinaryRecords] q WITH (READPAST)
        WHERE q.[RetryCount] >= @MaxClaimAttempts
          AND q.[RetryCount] > 0
          AND (q.[ProcessingStatus] = 0
               OR q.[ProcessingStartTimeUtc] < @StaleThreshold);
    END

    -- The batch this call claimed is identified by OUTPUT, not by the timestamp it
    -- wrote. Re-reading the queue for ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    -- is NOT safe: GETUTCDATE() advances in ~3.33 ms steps, so two adapter instances
    -- claiming within one tick write the same value and each one's re-read matches
    -- BOTH batches. ProcessingStartTimeUtc is still stamped, because the stale-reclaim
    -- predicate above reads it; it is simply no longer how the batch is identified.
    --
    -- Only the ids are captured, and the rows are then projected by id. That diverges
    -- from spClaimQDeviceSubscriptionsBatch, which captures every column into its table
    -- variable, and the divergence is deliberate: an id-only capture carries no column
    -- list to keep in step with the queue table, and no collation to declare. The id
    -- set is exact - ROWLOCK, UPDLOCK, READPAST means no other claimer holds these rows
    -- - so the join is a projection of a set already fixed, not a second selection.
    --
    -- The ORDER BY on the way out is what holds the "in RecordCreationTimeUtc, id
    -- order" contract. An OUTPUT clause has no row order of its own, and the ORDER BY
    -- inside the CTE below chooses only WHICH rows are claimed.
    --
    -- Both ORDER BYs carry id as a tiebreaker. RecordCreationTimeUtc alone is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a TOP (@BatchSize) takes, and the
    -- order the batch comes back in, could differ between two runs of this procedure.
    DECLARE @ClaimedIds TABLE ([id] BIGINT NOT NULL PRIMARY KEY);

    -- Use a CTE to select ordered rows, then update them atomically.
    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_BinaryRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0  -- Available
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc, id
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,  -- InProgress
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END
    OUTPUT inserted.[id] INTO @ClaimedIds;

    -- Return the claimed records.
    SELECT q.*
    FROM [gda].[Q_BinaryRecords] q
    INNER JOIN @ClaimedIds c ON c.[id] = q.[id]
    ORDER BY q.[RecordCreationTimeUtc], q.[id];
END;
GO

GRANT EXECUTE ON [gda].[spClaimQBinaryRecordsBatch] TO [geotabdigadapter_client];
GO


-- ================================================================================
-- 4. BLUETOOTH RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_BluetoothRecords
CREATE TABLE [gda].[Q_BluetoothRecords](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [Address] [varchar](17) NOT NULL,
    [Data] [real] NOT NULL,
    [DataType] [tinyint] NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_Bluetooth_Created] DEFAULT (SYSUTCDATETIME()),
    [RecordLastChangedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_Bluetooth_LastChanged] DEFAULT (SYSUTCDATETIME()),
    [ProcessingStatus] [tinyint] NOT NULL CONSTRAINT [DF_Q_Bluetooth_ProcessingStatus] DEFAULT (0),
    [ProcessingStartTimeUtc] [datetime2](7) NULL,
    [RetryCount] [tinyint] NOT NULL CONSTRAINT [DF_Q_Bluetooth_Retry] DEFAULT (0),
 CONSTRAINT [PK_Q_BluetoothRecords] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_BluetoothRecords_PendingWork]
ON [gda].[Q_BluetoothRecords] ([id])
INCLUDE ([ThirdPartyId], [DateTime], [Address], [Data], [DataType])
WHERE ([ProcessingStatus] = 0);
GO

-- Two indexes for the claim procedure. The PendingWork index above serves neither
-- statement: it is filtered on [ProcessingStatus] = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that filter
-- - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take @BatchSize rows, so the cost grows with queue
-- depth - worst exactly when the queue has backed up and throughput matters most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the
-- DELETE TOP (@BatchSize) can never be filled, so unindexed every row is read before
-- concluding that none qualify. [RetryCount] > 0 is a deliberate SUPERSET of the
-- sweep's own [RetryCount] >= @MaxClaimAttempts - the configured ceiling is never
-- below 1 - so this predicate needs no revision if that floor moves, and the index
-- holds only rows a device-lookup hold or a stale reclaim has touched - few or none
-- on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,073 ms to 464 ms here and from 5,953 ms to 90 ms on PostgreSQL. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. Those SQL Server figures predate the
-- sweep's literal [RetryCount] > 0, without which it could not use the filtered index
-- at all (by itself that index measured +4.6%); with it, a claim on a steady
-- 1,000,000-row queue fell from 495 ms to 324 ms. They cost 4.3% on the insert path,
-- which retains ~25x headroom over the feed either way.
CREATE NONCLUSTERED INDEX [IX_Q_BluetoothRecords_RecordCreationTimeUtc]
ON [gda].[Q_BluetoothRecords] ([RecordCreationTimeUtc]);
GO

CREATE NONCLUSTERED INDEX [IX_Q_BluetoothRecords_Exhausted]
ON [gda].[Q_BluetoothRecords] ([id])
WHERE ([RetryCount] > 0);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_BluetoothRecordsFail
CREATE TABLE [gda].[Q_BluetoothRecordsFail](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [OriginalQueueId] [bigint] NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [Address] [varchar](17) NOT NULL,
    [Data] [real] NOT NULL,
    [DataType] [tinyint] NOT NULL,
    [OriginalRecordLastChangedUtc] [datetime2](7) NOT NULL,
    [FailureReason] [nvarchar](max) NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_BluetoothFail_Created] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_Q_BluetoothRecordsFail] PRIMARY KEY CLUSTERED ([RecordCreationTimeUtc] ASC, [id] ASC)
    ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
) ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
GO

CREATE NONCLUSTERED INDEX [IX_Q_BluetoothRecordsFail_ThirdPartyId]
ON [gda].[Q_BluetoothRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc])
ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQBluetoothRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_BluetoothRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is identified by the OUTPUT of the claiming UPDATE, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the GETUTCDATE() value just written, and
--     two instances claiming inside one ~3.33 ms tick each received BOTH batches - so
--     the atomic pop was stated here but not delivered. See the note in the body.
-- Parameters:
--   @BatchSize
--     Maximum number of records to claim in a single batch.
--   @StaleThresholdMinutes
--     Minutes after which an "in progress" record is considered stale and eligible for
--     reclaim.
--   @MaxClaimAttempts
--     Claims a record may accumulate before it is treated as poison and moved to
--     Q_BluetoothRecordsFail. Optional; when NULL a record is reclaimed indefinitely, which
--     is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_BluetoothRecords records, in RecordCreationTimeUtc, id order.
CREATE PROCEDURE [gda].[spClaimQBluetoothRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT,
    @MaxClaimAttempts INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- RecordCreationTimeUtc is NOT carried across. It is the fail table's partitioning
    -- column and defaults to now, so the row lands in the current partition and is
    -- retired on the retention schedule like every other failure.
    -- Bounded by @BatchSize, like every other statement in this procedure. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to @BatchSize - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. There is no ORDER BY, because every qualifying row has the same destination
    -- and ordering would buy a sort and no guarantee.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply ProcessingStatus = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with RetryCount + 1; skipping
    -- every claimed row would mean it is never swept, which is the infinite retry this ceiling
    -- exists to end. Mirroring the claim's own predicate removes abandoned rows and protects
    -- live ones.
    --
    -- The literal [RetryCount] > 0 changes no result - the ceiling is never below 1 - and it is
    -- what lets the optimizer use the queue's filtered _Exhausted index (WHERE [RetryCount] > 0).
    -- A cached plan must be valid for every value of @MaxClaimAttempts, so a comparison with the
    -- parameter alone can never match a filtered index: without the literal this statement read
    -- the whole queue before every claim, the healthy case included - measured at 1,000,000 rows
    -- and 9,391 logical reads per claim on a 1,000,000-row queue, and the whole claim at 495 ms
    -- against 324 ms with it. READPAST belongs to the same change. Once the DELETE seeks the
    -- index it looks each row up in the clustered index, and without READPAST that lookup would
    -- wait on - and could deadlock with - a row another session has locked. A row it skips is
    -- taken by a later pass, as with the bound above.
    IF @MaxClaimAttempts IS NOT NULL
    BEGIN
        DELETE TOP (@BatchSize) q
        OUTPUT deleted.[id],
               deleted.[ThirdPartyId], deleted.[DateTime], deleted.[Address],
               deleted.[Data], deleted.[DataType],
               deleted.[RecordLastChangedUtc],
               CONCAT('Exceeded ', @MaxClaimAttempts, ' claim attempts.')
          INTO [gda].[Q_BluetoothRecordsFail] (
               [OriginalQueueId],
               [ThirdPartyId], [DateTime], [Address], [Data], [DataType],
               [OriginalRecordLastChangedUtc],
               [FailureReason])
        FROM [gda].[Q_BluetoothRecords] q WITH (READPAST)
        WHERE q.[RetryCount] >= @MaxClaimAttempts
          AND q.[RetryCount] > 0
          AND (q.[ProcessingStatus] = 0
               OR q.[ProcessingStartTimeUtc] < @StaleThreshold);
    END

    -- The batch this call claimed is identified by OUTPUT, not by the timestamp it
    -- wrote. Re-reading the queue for ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    -- is NOT safe: GETUTCDATE() advances in ~3.33 ms steps, so two adapter instances
    -- claiming within one tick write the same value and each one's re-read matches
    -- BOTH batches. ProcessingStartTimeUtc is still stamped, because the stale-reclaim
    -- predicate above reads it; it is simply no longer how the batch is identified.
    --
    -- Only the ids are captured, and the rows are then projected by id. That diverges
    -- from spClaimQDeviceSubscriptionsBatch, which captures every column into its table
    -- variable, and the divergence is deliberate: an id-only capture carries no column
    -- list to keep in step with the queue table, and no collation to declare. The id
    -- set is exact - ROWLOCK, UPDLOCK, READPAST means no other claimer holds these rows
    -- - so the join is a projection of a set already fixed, not a second selection.
    --
    -- The ORDER BY on the way out is what holds the "in RecordCreationTimeUtc, id
    -- order" contract. An OUTPUT clause has no row order of its own, and the ORDER BY
    -- inside the CTE below chooses only WHICH rows are claimed.
    --
    -- Both ORDER BYs carry id as a tiebreaker. RecordCreationTimeUtc alone is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a TOP (@BatchSize) takes, and the
    -- order the batch comes back in, could differ between two runs of this procedure.
    DECLARE @ClaimedIds TABLE ([id] BIGINT NOT NULL PRIMARY KEY);

    -- Use a CTE to select ordered rows, then update them atomically.
    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_BluetoothRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0  -- Available
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc, id
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,  -- InProgress
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END
    OUTPUT inserted.[id] INTO @ClaimedIds;

    -- Return the claimed records.
    SELECT q.*
    FROM [gda].[Q_BluetoothRecords] q
    INNER JOIN @ClaimedIds c ON c.[id] = q.[id]
    ORDER BY q.[RecordCreationTimeUtc], q.[id];
END;
GO

GRANT EXECUTE ON [gda].[spClaimQBluetoothRecordsBatch] TO [geotabdigadapter_client];
GO


-- ================================================================================
-- 5. DRIVER CHANGE RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_DriverChangeRecords
CREATE TABLE [gda].[Q_DriverChangeRecords](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [KeyType] [tinyint] NOT NULL,
    [DriverId] [varbinary](239) NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_DriverChange_Created] DEFAULT (SYSUTCDATETIME()),
    [RecordLastChangedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_DriverChange_LastChanged] DEFAULT (SYSUTCDATETIME()),
    [ProcessingStatus] [tinyint] NOT NULL CONSTRAINT [DF_Q_DriverChange_ProcessingStatus] DEFAULT (0),
    [ProcessingStartTimeUtc] [datetime2](7) NULL,
    [RetryCount] [tinyint] NOT NULL CONSTRAINT [DF_Q_DriverChange_Retry] DEFAULT (0),
 CONSTRAINT [PK_Q_DriverChangeRecords] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_DriverChangeRecords_PendingWork]
ON [gda].[Q_DriverChangeRecords] ([id])
INCLUDE ([ThirdPartyId], [DateTime], [KeyType])
WHERE ([ProcessingStatus] = 0);
GO

-- Two indexes for the claim procedure. The PendingWork index above serves neither
-- statement: it is filtered on [ProcessingStatus] = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that filter
-- - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take @BatchSize rows, so the cost grows with queue
-- depth - worst exactly when the queue has backed up and throughput matters most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the
-- DELETE TOP (@BatchSize) can never be filled, so unindexed every row is read before
-- concluding that none qualify. [RetryCount] > 0 is a deliberate SUPERSET of the
-- sweep's own [RetryCount] >= @MaxClaimAttempts - the configured ceiling is never
-- below 1 - so this predicate needs no revision if that floor moves, and the index
-- holds only rows a device-lookup hold or a stale reclaim has touched - few or none
-- on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,073 ms to 464 ms here and from 5,953 ms to 90 ms on PostgreSQL. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. Those SQL Server figures predate the
-- sweep's literal [RetryCount] > 0, without which it could not use the filtered index
-- at all (by itself that index measured +4.6%); with it, a claim on a steady
-- 1,000,000-row queue fell from 495 ms to 324 ms. They cost 4.3% on the insert path,
-- which retains ~25x headroom over the feed either way.
CREATE NONCLUSTERED INDEX [IX_Q_DriverChangeRecords_RecordCreationTimeUtc]
ON [gda].[Q_DriverChangeRecords] ([RecordCreationTimeUtc]);
GO

CREATE NONCLUSTERED INDEX [IX_Q_DriverChangeRecords_Exhausted]
ON [gda].[Q_DriverChangeRecords] ([id])
WHERE ([RetryCount] > 0);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_DriverChangeRecordsFail
CREATE TABLE [gda].[Q_DriverChangeRecordsFail](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [OriginalQueueId] [bigint] NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [KeyType] [tinyint] NOT NULL,
    [DriverId] [varbinary](239) NOT NULL,
    [OriginalRecordLastChangedUtc] [datetime2](7) NOT NULL,
    [FailureReason] [nvarchar](max) NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_DriverChangeFail_Created] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_Q_DriverChangeRecordsFail] PRIMARY KEY CLUSTERED ([RecordCreationTimeUtc] ASC, [id] ASC)
    ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
) ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
GO

CREATE NONCLUSTERED INDEX [IX_Q_DriverChangeRecordsFail_ThirdPartyId]
ON [gda].[Q_DriverChangeRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc])
ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQDriverChangeRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_DriverChangeRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is identified by the OUTPUT of the claiming UPDATE, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the GETUTCDATE() value just written, and
--     two instances claiming inside one ~3.33 ms tick each received BOTH batches - so
--     the atomic pop was stated here but not delivered. See the note in the body.
-- Parameters:
--   @BatchSize
--     Maximum number of records to claim in a single batch.
--   @StaleThresholdMinutes
--     Minutes after which an "in progress" record is considered stale and eligible for
--     reclaim.
--   @MaxClaimAttempts
--     Claims a record may accumulate before it is treated as poison and moved to
--     Q_DriverChangeRecordsFail. Optional; when NULL a record is reclaimed indefinitely, which
--     is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_DriverChangeRecords records, in RecordCreationTimeUtc, id order.
CREATE PROCEDURE [gda].[spClaimQDriverChangeRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT,
    @MaxClaimAttempts INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- RecordCreationTimeUtc is NOT carried across. It is the fail table's partitioning
    -- column and defaults to now, so the row lands in the current partition and is
    -- retired on the retention schedule like every other failure.
    -- Bounded by @BatchSize, like every other statement in this procedure. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to @BatchSize - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. There is no ORDER BY, because every qualifying row has the same destination
    -- and ordering would buy a sort and no guarantee.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply ProcessingStatus = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with RetryCount + 1; skipping
    -- every claimed row would mean it is never swept, which is the infinite retry this ceiling
    -- exists to end. Mirroring the claim's own predicate removes abandoned rows and protects
    -- live ones.
    --
    -- The literal [RetryCount] > 0 changes no result - the ceiling is never below 1 - and it is
    -- what lets the optimizer use the queue's filtered _Exhausted index (WHERE [RetryCount] > 0).
    -- A cached plan must be valid for every value of @MaxClaimAttempts, so a comparison with the
    -- parameter alone can never match a filtered index: without the literal this statement read
    -- the whole queue before every claim, the healthy case included - measured at 1,000,000 rows
    -- and 9,391 logical reads per claim on a 1,000,000-row queue, and the whole claim at 495 ms
    -- against 324 ms with it. READPAST belongs to the same change. Once the DELETE seeks the
    -- index it looks each row up in the clustered index, and without READPAST that lookup would
    -- wait on - and could deadlock with - a row another session has locked. A row it skips is
    -- taken by a later pass, as with the bound above.
    IF @MaxClaimAttempts IS NOT NULL
    BEGIN
        DELETE TOP (@BatchSize) q
        OUTPUT deleted.[id],
               deleted.[ThirdPartyId], deleted.[DateTime], deleted.[KeyType],
               deleted.[DriverId],
               deleted.[RecordLastChangedUtc],
               CONCAT('Exceeded ', @MaxClaimAttempts, ' claim attempts.')
          INTO [gda].[Q_DriverChangeRecordsFail] (
               [OriginalQueueId],
               [ThirdPartyId], [DateTime], [KeyType], [DriverId],
               [OriginalRecordLastChangedUtc],
               [FailureReason])
        FROM [gda].[Q_DriverChangeRecords] q WITH (READPAST)
        WHERE q.[RetryCount] >= @MaxClaimAttempts
          AND q.[RetryCount] > 0
          AND (q.[ProcessingStatus] = 0
               OR q.[ProcessingStartTimeUtc] < @StaleThreshold);
    END

    -- The batch this call claimed is identified by OUTPUT, not by the timestamp it
    -- wrote. Re-reading the queue for ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    -- is NOT safe: GETUTCDATE() advances in ~3.33 ms steps, so two adapter instances
    -- claiming within one tick write the same value and each one's re-read matches
    -- BOTH batches. ProcessingStartTimeUtc is still stamped, because the stale-reclaim
    -- predicate above reads it; it is simply no longer how the batch is identified.
    --
    -- Only the ids are captured, and the rows are then projected by id. That diverges
    -- from spClaimQDeviceSubscriptionsBatch, which captures every column into its table
    -- variable, and the divergence is deliberate: an id-only capture carries no column
    -- list to keep in step with the queue table, and no collation to declare. The id
    -- set is exact - ROWLOCK, UPDLOCK, READPAST means no other claimer holds these rows
    -- - so the join is a projection of a set already fixed, not a second selection.
    --
    -- The ORDER BY on the way out is what holds the "in RecordCreationTimeUtc, id
    -- order" contract. An OUTPUT clause has no row order of its own, and the ORDER BY
    -- inside the CTE below chooses only WHICH rows are claimed.
    --
    -- Both ORDER BYs carry id as a tiebreaker. RecordCreationTimeUtc alone is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a TOP (@BatchSize) takes, and the
    -- order the batch comes back in, could differ between two runs of this procedure.
    DECLARE @ClaimedIds TABLE ([id] BIGINT NOT NULL PRIMARY KEY);

    -- Use a CTE to select ordered rows, then update them atomically.
    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_DriverChangeRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0  -- Available
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc, id
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,  -- InProgress
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END
    OUTPUT inserted.[id] INTO @ClaimedIds;

    -- Return the claimed records.
    SELECT q.*
    FROM [gda].[Q_DriverChangeRecords] q
    INNER JOIN @ClaimedIds c ON c.[id] = q.[id]
    ORDER BY q.[RecordCreationTimeUtc], q.[id];
END;
GO

GRANT EXECUTE ON [gda].[spClaimQDriverChangeRecordsBatch] TO [geotabdigadapter_client];
GO


-- ================================================================================
-- 6. GENERIC FAULT RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_GenericFaultRecords
CREATE TABLE [gda].[Q_GenericFaultRecords](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [Code] [int] NOT NULL,
    [FaultStateActive] [bit] NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_GenericFault_Created] DEFAULT (SYSUTCDATETIME()),
    [RecordLastChangedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_GenericFault_LastChanged] DEFAULT (SYSUTCDATETIME()),
    [ProcessingStatus] [tinyint] NOT NULL CONSTRAINT [DF_Q_GenericFault_ProcessingStatus] DEFAULT (0),
    [ProcessingStartTimeUtc] [datetime2](7) NULL,
    [RetryCount] [tinyint] NOT NULL CONSTRAINT [DF_Q_GenericFault_Retry] DEFAULT (0),
 CONSTRAINT [PK_Q_GenericFaultRecords] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_GenericFaultRecords_PendingWork]
ON [gda].[Q_GenericFaultRecords] ([id])
INCLUDE ([ThirdPartyId], [DateTime], [Code], [FaultStateActive])
WHERE ([ProcessingStatus] = 0);
GO

-- Two indexes for the claim procedure. The PendingWork index above serves neither
-- statement: it is filtered on [ProcessingStatus] = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that filter
-- - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take @BatchSize rows, so the cost grows with queue
-- depth - worst exactly when the queue has backed up and throughput matters most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the
-- DELETE TOP (@BatchSize) can never be filled, so unindexed every row is read before
-- concluding that none qualify. [RetryCount] > 0 is a deliberate SUPERSET of the
-- sweep's own [RetryCount] >= @MaxClaimAttempts - the configured ceiling is never
-- below 1 - so this predicate needs no revision if that floor moves, and the index
-- holds only rows a device-lookup hold or a stale reclaim has touched - few or none
-- on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,073 ms to 464 ms here and from 5,953 ms to 90 ms on PostgreSQL. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. Those SQL Server figures predate the
-- sweep's literal [RetryCount] > 0, without which it could not use the filtered index
-- at all (by itself that index measured +4.6%); with it, a claim on a steady
-- 1,000,000-row queue fell from 495 ms to 324 ms. They cost 4.3% on the insert path,
-- which retains ~25x headroom over the feed either way.
CREATE NONCLUSTERED INDEX [IX_Q_GenericFaultRecords_RecordCreationTimeUtc]
ON [gda].[Q_GenericFaultRecords] ([RecordCreationTimeUtc]);
GO

CREATE NONCLUSTERED INDEX [IX_Q_GenericFaultRecords_Exhausted]
ON [gda].[Q_GenericFaultRecords] ([id])
WHERE ([RetryCount] > 0);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_GenericFaultRecordsFail
CREATE TABLE [gda].[Q_GenericFaultRecordsFail](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [OriginalQueueId] [bigint] NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [Code] [int] NOT NULL,
    [FaultStateActive] [bit] NOT NULL,
    [OriginalRecordLastChangedUtc] [datetime2](7) NOT NULL,
    [FailureReason] [nvarchar](max) NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_GenericFaultFail_Created] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_Q_GenericFaultRecordsFail] PRIMARY KEY CLUSTERED ([RecordCreationTimeUtc] ASC, [id] ASC)
    ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
) ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
GO

CREATE NONCLUSTERED INDEX [IX_Q_GenericFaultRecordsFail_ThirdPartyId]
ON [gda].[Q_GenericFaultRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc])
ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQGenericFaultRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_GenericFaultRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is identified by the OUTPUT of the claiming UPDATE, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the GETUTCDATE() value just written, and
--     two instances claiming inside one ~3.33 ms tick each received BOTH batches - so
--     the atomic pop was stated here but not delivered. See the note in the body.
-- Parameters:
--   @BatchSize
--     Maximum number of records to claim in a single batch.
--   @StaleThresholdMinutes
--     Minutes after which an "in progress" record is considered stale and eligible for
--     reclaim.
--   @MaxClaimAttempts
--     Claims a record may accumulate before it is treated as poison and moved to
--     Q_GenericFaultRecordsFail. Optional; when NULL a record is reclaimed indefinitely, which
--     is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_GenericFaultRecords records, in RecordCreationTimeUtc, id order.
CREATE PROCEDURE [gda].[spClaimQGenericFaultRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT,
    @MaxClaimAttempts INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- RecordCreationTimeUtc is NOT carried across. It is the fail table's partitioning
    -- column and defaults to now, so the row lands in the current partition and is
    -- retired on the retention schedule like every other failure.
    -- Bounded by @BatchSize, like every other statement in this procedure. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to @BatchSize - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. There is no ORDER BY, because every qualifying row has the same destination
    -- and ordering would buy a sort and no guarantee.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply ProcessingStatus = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with RetryCount + 1; skipping
    -- every claimed row would mean it is never swept, which is the infinite retry this ceiling
    -- exists to end. Mirroring the claim's own predicate removes abandoned rows and protects
    -- live ones.
    --
    -- The literal [RetryCount] > 0 changes no result - the ceiling is never below 1 - and it is
    -- what lets the optimizer use the queue's filtered _Exhausted index (WHERE [RetryCount] > 0).
    -- A cached plan must be valid for every value of @MaxClaimAttempts, so a comparison with the
    -- parameter alone can never match a filtered index: without the literal this statement read
    -- the whole queue before every claim, the healthy case included - measured at 1,000,000 rows
    -- and 9,391 logical reads per claim on a 1,000,000-row queue, and the whole claim at 495 ms
    -- against 324 ms with it. READPAST belongs to the same change. Once the DELETE seeks the
    -- index it looks each row up in the clustered index, and without READPAST that lookup would
    -- wait on - and could deadlock with - a row another session has locked. A row it skips is
    -- taken by a later pass, as with the bound above.
    IF @MaxClaimAttempts IS NOT NULL
    BEGIN
        DELETE TOP (@BatchSize) q
        OUTPUT deleted.[id],
               deleted.[ThirdPartyId], deleted.[DateTime], deleted.[Code],
               deleted.[FaultStateActive],
               deleted.[RecordLastChangedUtc],
               CONCAT('Exceeded ', @MaxClaimAttempts, ' claim attempts.')
          INTO [gda].[Q_GenericFaultRecordsFail] (
               [OriginalQueueId],
               [ThirdPartyId], [DateTime], [Code], [FaultStateActive],
               [OriginalRecordLastChangedUtc],
               [FailureReason])
        FROM [gda].[Q_GenericFaultRecords] q WITH (READPAST)
        WHERE q.[RetryCount] >= @MaxClaimAttempts
          AND q.[RetryCount] > 0
          AND (q.[ProcessingStatus] = 0
               OR q.[ProcessingStartTimeUtc] < @StaleThreshold);
    END

    -- The batch this call claimed is identified by OUTPUT, not by the timestamp it
    -- wrote. Re-reading the queue for ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    -- is NOT safe: GETUTCDATE() advances in ~3.33 ms steps, so two adapter instances
    -- claiming within one tick write the same value and each one's re-read matches
    -- BOTH batches. ProcessingStartTimeUtc is still stamped, because the stale-reclaim
    -- predicate above reads it; it is simply no longer how the batch is identified.
    --
    -- Only the ids are captured, and the rows are then projected by id. That diverges
    -- from spClaimQDeviceSubscriptionsBatch, which captures every column into its table
    -- variable, and the divergence is deliberate: an id-only capture carries no column
    -- list to keep in step with the queue table, and no collation to declare. The id
    -- set is exact - ROWLOCK, UPDLOCK, READPAST means no other claimer holds these rows
    -- - so the join is a projection of a set already fixed, not a second selection.
    --
    -- The ORDER BY on the way out is what holds the "in RecordCreationTimeUtc, id
    -- order" contract. An OUTPUT clause has no row order of its own, and the ORDER BY
    -- inside the CTE below chooses only WHICH rows are claimed.
    --
    -- Both ORDER BYs carry id as a tiebreaker. RecordCreationTimeUtc alone is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a TOP (@BatchSize) takes, and the
    -- order the batch comes back in, could differ between two runs of this procedure.
    DECLARE @ClaimedIds TABLE ([id] BIGINT NOT NULL PRIMARY KEY);

    -- Use a CTE to select ordered rows, then update them atomically.
    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_GenericFaultRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0  -- Available
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc, id
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,  -- InProgress
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END
    OUTPUT inserted.[id] INTO @ClaimedIds;

    -- Return the claimed records.
    SELECT q.*
    FROM [gda].[Q_GenericFaultRecords] q
    INNER JOIN @ClaimedIds c ON c.[id] = q.[id]
    ORDER BY q.[RecordCreationTimeUtc], q.[id];
END;
GO

GRANT EXECUTE ON [gda].[spClaimQGenericFaultRecordsBatch] TO [geotabdigadapter_client];
GO


-- ================================================================================
-- 7. GENERIC STATUS RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_GenericStatusRecords
CREATE TABLE [gda].[Q_GenericStatusRecords](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [Code] [int] NOT NULL,
    [Value] [int] NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_GenericStatus_Created] DEFAULT (SYSUTCDATETIME()),
    [RecordLastChangedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_GenericStatus_LastChanged] DEFAULT (SYSUTCDATETIME()),
    [ProcessingStatus] [tinyint] NOT NULL CONSTRAINT [DF_Q_GenericStatus_ProcessingStatus] DEFAULT (0),
    [ProcessingStartTimeUtc] [datetime2](7) NULL,
    [RetryCount] [tinyint] NOT NULL CONSTRAINT [DF_Q_GenericStatus_Retry] DEFAULT (0),
 CONSTRAINT [PK_Q_GenericStatusRecords] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_GenericStatusRecords_PendingWork]
ON [gda].[Q_GenericStatusRecords] ([id])
INCLUDE ([ThirdPartyId], [DateTime], [Code], [Value])
WHERE ([ProcessingStatus] = 0);
GO

-- Two indexes for the claim procedure. The PendingWork index above serves neither
-- statement: it is filtered on [ProcessingStatus] = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that filter
-- - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take @BatchSize rows, so the cost grows with queue
-- depth - worst exactly when the queue has backed up and throughput matters most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the
-- DELETE TOP (@BatchSize) can never be filled, so unindexed every row is read before
-- concluding that none qualify. [RetryCount] > 0 is a deliberate SUPERSET of the
-- sweep's own [RetryCount] >= @MaxClaimAttempts - the configured ceiling is never
-- below 1 - so this predicate needs no revision if that floor moves, and the index
-- holds only rows a device-lookup hold or a stale reclaim has touched - few or none
-- on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,073 ms to 464 ms here and from 5,953 ms to 90 ms on PostgreSQL. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. Those SQL Server figures predate the
-- sweep's literal [RetryCount] > 0, without which it could not use the filtered index
-- at all (by itself that index measured +4.6%); with it, a claim on a steady
-- 1,000,000-row queue fell from 495 ms to 324 ms. They cost 4.3% on the insert path,
-- which retains ~25x headroom over the feed either way.
CREATE NONCLUSTERED INDEX [IX_Q_GenericStatusRecords_RecordCreationTimeUtc]
ON [gda].[Q_GenericStatusRecords] ([RecordCreationTimeUtc]);
GO

CREATE NONCLUSTERED INDEX [IX_Q_GenericStatusRecords_Exhausted]
ON [gda].[Q_GenericStatusRecords] ([id])
WHERE ([RetryCount] > 0);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_GenericStatusRecordsFail
CREATE TABLE [gda].[Q_GenericStatusRecordsFail](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [OriginalQueueId] [bigint] NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [Code] [int] NOT NULL,
    [Value] [int] NOT NULL,
    [OriginalRecordLastChangedUtc] [datetime2](7) NOT NULL,
    [FailureReason] [nvarchar](max) NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_GenericStatusFail_Created] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_Q_GenericStatusRecordsFail] PRIMARY KEY CLUSTERED ([RecordCreationTimeUtc] ASC, [id] ASC)
    ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
) ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
GO

CREATE NONCLUSTERED INDEX [IX_Q_GenericStatusRecordsFail_ThirdPartyId]
ON [gda].[Q_GenericStatusRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc])
ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQGenericStatusRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_GenericStatusRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is identified by the OUTPUT of the claiming UPDATE, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the GETUTCDATE() value just written, and
--     two instances claiming inside one ~3.33 ms tick each received BOTH batches - so
--     the atomic pop was stated here but not delivered. See the note in the body.
-- Parameters:
--   @BatchSize
--     Maximum number of records to claim in a single batch.
--   @StaleThresholdMinutes
--     Minutes after which an "in progress" record is considered stale and eligible for
--     reclaim.
--   @MaxClaimAttempts
--     Claims a record may accumulate before it is treated as poison and moved to
--     Q_GenericStatusRecordsFail. Optional; when NULL a record is reclaimed indefinitely, which
--     is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_GenericStatusRecords records, in RecordCreationTimeUtc, id order.
CREATE PROCEDURE [gda].[spClaimQGenericStatusRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT,
    @MaxClaimAttempts INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- RecordCreationTimeUtc is NOT carried across. It is the fail table's partitioning
    -- column and defaults to now, so the row lands in the current partition and is
    -- retired on the retention schedule like every other failure.
    -- Bounded by @BatchSize, like every other statement in this procedure. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to @BatchSize - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. There is no ORDER BY, because every qualifying row has the same destination
    -- and ordering would buy a sort and no guarantee.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply ProcessingStatus = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with RetryCount + 1; skipping
    -- every claimed row would mean it is never swept, which is the infinite retry this ceiling
    -- exists to end. Mirroring the claim's own predicate removes abandoned rows and protects
    -- live ones.
    --
    -- The literal [RetryCount] > 0 changes no result - the ceiling is never below 1 - and it is
    -- what lets the optimizer use the queue's filtered _Exhausted index (WHERE [RetryCount] > 0).
    -- A cached plan must be valid for every value of @MaxClaimAttempts, so a comparison with the
    -- parameter alone can never match a filtered index: without the literal this statement read
    -- the whole queue before every claim, the healthy case included - measured at 1,000,000 rows
    -- and 9,391 logical reads per claim on a 1,000,000-row queue, and the whole claim at 495 ms
    -- against 324 ms with it. READPAST belongs to the same change. Once the DELETE seeks the
    -- index it looks each row up in the clustered index, and without READPAST that lookup would
    -- wait on - and could deadlock with - a row another session has locked. A row it skips is
    -- taken by a later pass, as with the bound above.
    IF @MaxClaimAttempts IS NOT NULL
    BEGIN
        DELETE TOP (@BatchSize) q
        OUTPUT deleted.[id],
               deleted.[ThirdPartyId], deleted.[DateTime], deleted.[Code],
               deleted.[Value],
               deleted.[RecordLastChangedUtc],
               CONCAT('Exceeded ', @MaxClaimAttempts, ' claim attempts.')
          INTO [gda].[Q_GenericStatusRecordsFail] (
               [OriginalQueueId],
               [ThirdPartyId], [DateTime], [Code], [Value],
               [OriginalRecordLastChangedUtc],
               [FailureReason])
        FROM [gda].[Q_GenericStatusRecords] q WITH (READPAST)
        WHERE q.[RetryCount] >= @MaxClaimAttempts
          AND q.[RetryCount] > 0
          AND (q.[ProcessingStatus] = 0
               OR q.[ProcessingStartTimeUtc] < @StaleThreshold);
    END

    -- The batch this call claimed is identified by OUTPUT, not by the timestamp it
    -- wrote. Re-reading the queue for ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    -- is NOT safe: GETUTCDATE() advances in ~3.33 ms steps, so two adapter instances
    -- claiming within one tick write the same value and each one's re-read matches
    -- BOTH batches. ProcessingStartTimeUtc is still stamped, because the stale-reclaim
    -- predicate above reads it; it is simply no longer how the batch is identified.
    --
    -- Only the ids are captured, and the rows are then projected by id. That diverges
    -- from spClaimQDeviceSubscriptionsBatch, which captures every column into its table
    -- variable, and the divergence is deliberate: an id-only capture carries no column
    -- list to keep in step with the queue table, and no collation to declare. The id
    -- set is exact - ROWLOCK, UPDLOCK, READPAST means no other claimer holds these rows
    -- - so the join is a projection of a set already fixed, not a second selection.
    --
    -- The ORDER BY on the way out is what holds the "in RecordCreationTimeUtc, id
    -- order" contract. An OUTPUT clause has no row order of its own, and the ORDER BY
    -- inside the CTE below chooses only WHICH rows are claimed.
    --
    -- Both ORDER BYs carry id as a tiebreaker. RecordCreationTimeUtc alone is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a TOP (@BatchSize) takes, and the
    -- order the batch comes back in, could differ between two runs of this procedure.
    DECLARE @ClaimedIds TABLE ([id] BIGINT NOT NULL PRIMARY KEY);

    -- Use a CTE to select ordered rows, then update them atomically.
    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_GenericStatusRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0  -- Available
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc, id
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,  -- InProgress
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END
    OUTPUT inserted.[id] INTO @ClaimedIds;

    -- Return the claimed records.
    SELECT q.*
    FROM [gda].[Q_GenericStatusRecords] q
    INNER JOIN @ClaimedIds c ON c.[id] = q.[id]
    ORDER BY q.[RecordCreationTimeUtc], q.[id];
END;
GO

GRANT EXECUTE ON [gda].[spClaimQGenericStatusRecordsBatch] TO [geotabdigadapter_client];
GO


-- ================================================================================
-- 8. J1708 FAULT RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_J1708FaultRecords
CREATE TABLE [gda].[Q_J1708FaultRecords](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [MessageId] [tinyint] NOT NULL,
    [ParameterId] [smallint] NULL,
    [SubsystemId] [smallint] NULL,
    [FailureModeIdentifier] [tinyint] NOT NULL,
    [OccurrenceCount] [tinyint] NOT NULL,
    [FaultStateActive] [bit] NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_J1708Fault_Created] DEFAULT (SYSUTCDATETIME()),
    [RecordLastChangedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_J1708Fault_LastChanged] DEFAULT (SYSUTCDATETIME()),
    [ProcessingStatus] [tinyint] NOT NULL CONSTRAINT [DF_Q_J1708Fault_ProcessingStatus] DEFAULT (0),
    [ProcessingStartTimeUtc] [datetime2](7) NULL,
    [RetryCount] [tinyint] NOT NULL CONSTRAINT [DF_Q_J1708Fault_Retry] DEFAULT (0),
 CONSTRAINT [PK_Q_J1708FaultRecords] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_J1708FaultRecords_PendingWork]
ON [gda].[Q_J1708FaultRecords] ([id])
INCLUDE ([ThirdPartyId], [DateTime], [MessageId], [FailureModeIdentifier])
WHERE ([ProcessingStatus] = 0);
GO

-- Two indexes for the claim procedure. The PendingWork index above serves neither
-- statement: it is filtered on [ProcessingStatus] = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that filter
-- - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take @BatchSize rows, so the cost grows with queue
-- depth - worst exactly when the queue has backed up and throughput matters most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the
-- DELETE TOP (@BatchSize) can never be filled, so unindexed every row is read before
-- concluding that none qualify. [RetryCount] > 0 is a deliberate SUPERSET of the
-- sweep's own [RetryCount] >= @MaxClaimAttempts - the configured ceiling is never
-- below 1 - so this predicate needs no revision if that floor moves, and the index
-- holds only rows a device-lookup hold or a stale reclaim has touched - few or none
-- on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,073 ms to 464 ms here and from 5,953 ms to 90 ms on PostgreSQL. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. Those SQL Server figures predate the
-- sweep's literal [RetryCount] > 0, without which it could not use the filtered index
-- at all (by itself that index measured +4.6%); with it, a claim on a steady
-- 1,000,000-row queue fell from 495 ms to 324 ms. They cost 4.3% on the insert path,
-- which retains ~25x headroom over the feed either way.
CREATE NONCLUSTERED INDEX [IX_Q_J1708FaultRecords_RecordCreationTimeUtc]
ON [gda].[Q_J1708FaultRecords] ([RecordCreationTimeUtc]);
GO

CREATE NONCLUSTERED INDEX [IX_Q_J1708FaultRecords_Exhausted]
ON [gda].[Q_J1708FaultRecords] ([id])
WHERE ([RetryCount] > 0);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_J1708FaultRecordsFail
CREATE TABLE [gda].[Q_J1708FaultRecordsFail](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [OriginalQueueId] [bigint] NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [MessageId] [tinyint] NOT NULL,
    [ParameterId] [smallint] NULL,
    [SubsystemId] [smallint] NULL,
    [FailureModeIdentifier] [tinyint] NOT NULL,
    [OccurrenceCount] [tinyint] NOT NULL,
    [FaultStateActive] [bit] NOT NULL,
    [OriginalRecordLastChangedUtc] [datetime2](7) NOT NULL,
    [FailureReason] [nvarchar](max) NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_J1708FaultFail_Created] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_Q_J1708FaultRecordsFail] PRIMARY KEY CLUSTERED ([RecordCreationTimeUtc] ASC, [id] ASC)
    ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
) ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
GO

CREATE NONCLUSTERED INDEX [IX_Q_J1708FaultRecordsFail_ThirdPartyId]
ON [gda].[Q_J1708FaultRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc])
ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQJ1708FaultRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_J1708FaultRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is identified by the OUTPUT of the claiming UPDATE, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the GETUTCDATE() value just written, and
--     two instances claiming inside one ~3.33 ms tick each received BOTH batches - so
--     the atomic pop was stated here but not delivered. See the note in the body.
-- Parameters:
--   @BatchSize
--     Maximum number of records to claim in a single batch.
--   @StaleThresholdMinutes
--     Minutes after which an "in progress" record is considered stale and eligible for
--     reclaim.
--   @MaxClaimAttempts
--     Claims a record may accumulate before it is treated as poison and moved to
--     Q_J1708FaultRecordsFail. Optional; when NULL a record is reclaimed indefinitely, which
--     is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_J1708FaultRecords records, in RecordCreationTimeUtc, id order.
CREATE PROCEDURE [gda].[spClaimQJ1708FaultRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT,
    @MaxClaimAttempts INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- RecordCreationTimeUtc is NOT carried across. It is the fail table's partitioning
    -- column and defaults to now, so the row lands in the current partition and is
    -- retired on the retention schedule like every other failure.
    -- Bounded by @BatchSize, like every other statement in this procedure. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to @BatchSize - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. There is no ORDER BY, because every qualifying row has the same destination
    -- and ordering would buy a sort and no guarantee.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply ProcessingStatus = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with RetryCount + 1; skipping
    -- every claimed row would mean it is never swept, which is the infinite retry this ceiling
    -- exists to end. Mirroring the claim's own predicate removes abandoned rows and protects
    -- live ones.
    --
    -- The literal [RetryCount] > 0 changes no result - the ceiling is never below 1 - and it is
    -- what lets the optimizer use the queue's filtered _Exhausted index (WHERE [RetryCount] > 0).
    -- A cached plan must be valid for every value of @MaxClaimAttempts, so a comparison with the
    -- parameter alone can never match a filtered index: without the literal this statement read
    -- the whole queue before every claim, the healthy case included - measured at 1,000,000 rows
    -- and 9,391 logical reads per claim on a 1,000,000-row queue, and the whole claim at 495 ms
    -- against 324 ms with it. READPAST belongs to the same change. Once the DELETE seeks the
    -- index it looks each row up in the clustered index, and without READPAST that lookup would
    -- wait on - and could deadlock with - a row another session has locked. A row it skips is
    -- taken by a later pass, as with the bound above.
    IF @MaxClaimAttempts IS NOT NULL
    BEGIN
        DELETE TOP (@BatchSize) q
        OUTPUT deleted.[id],
               deleted.[ThirdPartyId], deleted.[DateTime], deleted.[MessageId],
               deleted.[ParameterId], deleted.[SubsystemId],
               deleted.[FailureModeIdentifier], deleted.[OccurrenceCount],
               deleted.[FaultStateActive],
               deleted.[RecordLastChangedUtc],
               CONCAT('Exceeded ', @MaxClaimAttempts, ' claim attempts.')
          INTO [gda].[Q_J1708FaultRecordsFail] (
               [OriginalQueueId],
               [ThirdPartyId], [DateTime], [MessageId], [ParameterId], [SubsystemId],
               [FailureModeIdentifier], [OccurrenceCount], [FaultStateActive],
               [OriginalRecordLastChangedUtc],
               [FailureReason])
        FROM [gda].[Q_J1708FaultRecords] q WITH (READPAST)
        WHERE q.[RetryCount] >= @MaxClaimAttempts
          AND q.[RetryCount] > 0
          AND (q.[ProcessingStatus] = 0
               OR q.[ProcessingStartTimeUtc] < @StaleThreshold);
    END

    -- The batch this call claimed is identified by OUTPUT, not by the timestamp it
    -- wrote. Re-reading the queue for ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    -- is NOT safe: GETUTCDATE() advances in ~3.33 ms steps, so two adapter instances
    -- claiming within one tick write the same value and each one's re-read matches
    -- BOTH batches. ProcessingStartTimeUtc is still stamped, because the stale-reclaim
    -- predicate above reads it; it is simply no longer how the batch is identified.
    --
    -- Only the ids are captured, and the rows are then projected by id. That diverges
    -- from spClaimQDeviceSubscriptionsBatch, which captures every column into its table
    -- variable, and the divergence is deliberate: an id-only capture carries no column
    -- list to keep in step with the queue table, and no collation to declare. The id
    -- set is exact - ROWLOCK, UPDLOCK, READPAST means no other claimer holds these rows
    -- - so the join is a projection of a set already fixed, not a second selection.
    --
    -- The ORDER BY on the way out is what holds the "in RecordCreationTimeUtc, id
    -- order" contract. An OUTPUT clause has no row order of its own, and the ORDER BY
    -- inside the CTE below chooses only WHICH rows are claimed.
    --
    -- Both ORDER BYs carry id as a tiebreaker. RecordCreationTimeUtc alone is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a TOP (@BatchSize) takes, and the
    -- order the batch comes back in, could differ between two runs of this procedure.
    DECLARE @ClaimedIds TABLE ([id] BIGINT NOT NULL PRIMARY KEY);

    -- Use a CTE to select ordered rows, then update them atomically.
    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_J1708FaultRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0  -- Available
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc, id
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,  -- InProgress
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END
    OUTPUT inserted.[id] INTO @ClaimedIds;

    -- Return the claimed records.
    SELECT q.*
    FROM [gda].[Q_J1708FaultRecords] q
    INNER JOIN @ClaimedIds c ON c.[id] = q.[id]
    ORDER BY q.[RecordCreationTimeUtc], q.[id];
END;
GO

GRANT EXECUTE ON [gda].[spClaimQJ1708FaultRecordsBatch] TO [geotabdigadapter_client];
GO


-- ================================================================================
-- 9. J1939 FAULT RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_J1939FaultRecords
CREATE TABLE [gda].[Q_J1939FaultRecords](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [SuspectParameterNumber] [int] NOT NULL,
    [FailureModeIdentifier] [tinyint] NOT NULL,
    [OccurrenceCount] [tinyint] NOT NULL,
    [SourceAddress] [tinyint] NOT NULL,
    [MalfunctionLamp] [bit] NULL,
    [RedStopLamp] [bit] NULL,
    [AmberWarningLamp] [bit] NULL,
    [ProtectWarningLamp] [bit] NULL,
    [FaultStateActive] [bit] NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_J1939Fault_Created] DEFAULT (SYSUTCDATETIME()),
    [RecordLastChangedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_J1939Fault_LastChanged] DEFAULT (SYSUTCDATETIME()),
    [ProcessingStatus] [tinyint] NOT NULL CONSTRAINT [DF_Q_J1939Fault_ProcessingStatus] DEFAULT (0),
    [ProcessingStartTimeUtc] [datetime2](7) NULL,
    [RetryCount] [tinyint] NOT NULL CONSTRAINT [DF_Q_J1939Fault_Retry] DEFAULT (0),
 CONSTRAINT [PK_Q_J1939FaultRecords] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_J1939FaultRecords_PendingWork]
ON [gda].[Q_J1939FaultRecords] ([id])
INCLUDE ([ThirdPartyId], [DateTime], [SuspectParameterNumber], [FailureModeIdentifier])
WHERE ([ProcessingStatus] = 0);
GO

-- Two indexes for the claim procedure. The PendingWork index above serves neither
-- statement: it is filtered on [ProcessingStatus] = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that filter
-- - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take @BatchSize rows, so the cost grows with queue
-- depth - worst exactly when the queue has backed up and throughput matters most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the
-- DELETE TOP (@BatchSize) can never be filled, so unindexed every row is read before
-- concluding that none qualify. [RetryCount] > 0 is a deliberate SUPERSET of the
-- sweep's own [RetryCount] >= @MaxClaimAttempts - the configured ceiling is never
-- below 1 - so this predicate needs no revision if that floor moves, and the index
-- holds only rows a device-lookup hold or a stale reclaim has touched - few or none
-- on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,073 ms to 464 ms here and from 5,953 ms to 90 ms on PostgreSQL. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. Those SQL Server figures predate the
-- sweep's literal [RetryCount] > 0, without which it could not use the filtered index
-- at all (by itself that index measured +4.6%); with it, a claim on a steady
-- 1,000,000-row queue fell from 495 ms to 324 ms. They cost 4.3% on the insert path,
-- which retains ~25x headroom over the feed either way.
CREATE NONCLUSTERED INDEX [IX_Q_J1939FaultRecords_RecordCreationTimeUtc]
ON [gda].[Q_J1939FaultRecords] ([RecordCreationTimeUtc]);
GO

CREATE NONCLUSTERED INDEX [IX_Q_J1939FaultRecords_Exhausted]
ON [gda].[Q_J1939FaultRecords] ([id])
WHERE ([RetryCount] > 0);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_J1939FaultRecordsFail
CREATE TABLE [gda].[Q_J1939FaultRecordsFail](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [OriginalQueueId] [bigint] NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [SuspectParameterNumber] [int] NOT NULL,
    [FailureModeIdentifier] [tinyint] NOT NULL,
    [OccurrenceCount] [tinyint] NOT NULL,
    [SourceAddress] [tinyint] NOT NULL,
    [MalfunctionLamp] [bit] NULL,
    [RedStopLamp] [bit] NULL,
    [AmberWarningLamp] [bit] NULL,
    [ProtectWarningLamp] [bit] NULL,
    [FaultStateActive] [bit] NOT NULL,
    [OriginalRecordLastChangedUtc] [datetime2](7) NOT NULL,
    [FailureReason] [nvarchar](max) NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_J1939FaultFail_Created] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_Q_J1939FaultRecordsFail] PRIMARY KEY CLUSTERED ([RecordCreationTimeUtc] ASC, [id] ASC)
    ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
) ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
GO

CREATE NONCLUSTERED INDEX [IX_Q_J1939FaultRecordsFail_ThirdPartyId]
ON [gda].[Q_J1939FaultRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc])
ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQJ1939FaultRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_J1939FaultRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is identified by the OUTPUT of the claiming UPDATE, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the GETUTCDATE() value just written, and
--     two instances claiming inside one ~3.33 ms tick each received BOTH batches - so
--     the atomic pop was stated here but not delivered. See the note in the body.
-- Parameters:
--   @BatchSize
--     Maximum number of records to claim in a single batch.
--   @StaleThresholdMinutes
--     Minutes after which an "in progress" record is considered stale and eligible for
--     reclaim.
--   @MaxClaimAttempts
--     Claims a record may accumulate before it is treated as poison and moved to
--     Q_J1939FaultRecordsFail. Optional; when NULL a record is reclaimed indefinitely, which
--     is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_J1939FaultRecords records, in RecordCreationTimeUtc, id order.
CREATE PROCEDURE [gda].[spClaimQJ1939FaultRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT,
    @MaxClaimAttempts INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- RecordCreationTimeUtc is NOT carried across. It is the fail table's partitioning
    -- column and defaults to now, so the row lands in the current partition and is
    -- retired on the retention schedule like every other failure.
    -- Bounded by @BatchSize, like every other statement in this procedure. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to @BatchSize - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. There is no ORDER BY, because every qualifying row has the same destination
    -- and ordering would buy a sort and no guarantee.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply ProcessingStatus = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with RetryCount + 1; skipping
    -- every claimed row would mean it is never swept, which is the infinite retry this ceiling
    -- exists to end. Mirroring the claim's own predicate removes abandoned rows and protects
    -- live ones.
    --
    -- The literal [RetryCount] > 0 changes no result - the ceiling is never below 1 - and it is
    -- what lets the optimizer use the queue's filtered _Exhausted index (WHERE [RetryCount] > 0).
    -- A cached plan must be valid for every value of @MaxClaimAttempts, so a comparison with the
    -- parameter alone can never match a filtered index: without the literal this statement read
    -- the whole queue before every claim, the healthy case included - measured at 1,000,000 rows
    -- and 9,391 logical reads per claim on a 1,000,000-row queue, and the whole claim at 495 ms
    -- against 324 ms with it. READPAST belongs to the same change. Once the DELETE seeks the
    -- index it looks each row up in the clustered index, and without READPAST that lookup would
    -- wait on - and could deadlock with - a row another session has locked. A row it skips is
    -- taken by a later pass, as with the bound above.
    IF @MaxClaimAttempts IS NOT NULL
    BEGIN
        DELETE TOP (@BatchSize) q
        OUTPUT deleted.[id],
               deleted.[ThirdPartyId], deleted.[DateTime],
               deleted.[SuspectParameterNumber], deleted.[FailureModeIdentifier],
               deleted.[OccurrenceCount], deleted.[SourceAddress],
               deleted.[MalfunctionLamp], deleted.[RedStopLamp],
               deleted.[AmberWarningLamp], deleted.[ProtectWarningLamp],
               deleted.[FaultStateActive],
               deleted.[RecordLastChangedUtc],
               CONCAT('Exceeded ', @MaxClaimAttempts, ' claim attempts.')
          INTO [gda].[Q_J1939FaultRecordsFail] (
               [OriginalQueueId],
               [ThirdPartyId], [DateTime], [SuspectParameterNumber],
               [FailureModeIdentifier], [OccurrenceCount], [SourceAddress],
               [MalfunctionLamp], [RedStopLamp], [AmberWarningLamp],
               [ProtectWarningLamp], [FaultStateActive],
               [OriginalRecordLastChangedUtc],
               [FailureReason])
        FROM [gda].[Q_J1939FaultRecords] q WITH (READPAST)
        WHERE q.[RetryCount] >= @MaxClaimAttempts
          AND q.[RetryCount] > 0
          AND (q.[ProcessingStatus] = 0
               OR q.[ProcessingStartTimeUtc] < @StaleThreshold);
    END

    -- The batch this call claimed is identified by OUTPUT, not by the timestamp it
    -- wrote. Re-reading the queue for ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    -- is NOT safe: GETUTCDATE() advances in ~3.33 ms steps, so two adapter instances
    -- claiming within one tick write the same value and each one's re-read matches
    -- BOTH batches. ProcessingStartTimeUtc is still stamped, because the stale-reclaim
    -- predicate above reads it; it is simply no longer how the batch is identified.
    --
    -- Only the ids are captured, and the rows are then projected by id. That diverges
    -- from spClaimQDeviceSubscriptionsBatch, which captures every column into its table
    -- variable, and the divergence is deliberate: an id-only capture carries no column
    -- list to keep in step with the queue table, and no collation to declare. The id
    -- set is exact - ROWLOCK, UPDLOCK, READPAST means no other claimer holds these rows
    -- - so the join is a projection of a set already fixed, not a second selection.
    --
    -- The ORDER BY on the way out is what holds the "in RecordCreationTimeUtc, id
    -- order" contract. An OUTPUT clause has no row order of its own, and the ORDER BY
    -- inside the CTE below chooses only WHICH rows are claimed.
    --
    -- Both ORDER BYs carry id as a tiebreaker. RecordCreationTimeUtc alone is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a TOP (@BatchSize) takes, and the
    -- order the batch comes back in, could differ between two runs of this procedure.
    DECLARE @ClaimedIds TABLE ([id] BIGINT NOT NULL PRIMARY KEY);

    -- Use a CTE to select ordered rows, then update them atomically.
    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_J1939FaultRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0  -- Available
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc, id
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,  -- InProgress
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END
    OUTPUT inserted.[id] INTO @ClaimedIds;

    -- Return the claimed records.
    SELECT q.*
    FROM [gda].[Q_J1939FaultRecords] q
    INNER JOIN @ClaimedIds c ON c.[id] = q.[id]
    ORDER BY q.[RecordCreationTimeUtc], q.[id];
END;
GO

GRANT EXECUTE ON [gda].[spClaimQJ1939FaultRecordsBatch] TO [geotabdigadapter_client];
GO


-- ================================================================================
-- 10. OBDII FAULT RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_ObdiiFaultRecords
CREATE TABLE [gda].[Q_ObdiiFaultRecords](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [Code] [varchar](10) NOT NULL,
    [FaultStateActive] [bit] NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_ObdiiFault_Created] DEFAULT (SYSUTCDATETIME()),
    [RecordLastChangedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_ObdiiFault_LastChanged] DEFAULT (SYSUTCDATETIME()),
    [ProcessingStatus] [tinyint] NOT NULL CONSTRAINT [DF_Q_ObdiiFault_ProcessingStatus] DEFAULT (0),
    [ProcessingStartTimeUtc] [datetime2](7) NULL,
    [RetryCount] [tinyint] NOT NULL CONSTRAINT [DF_Q_ObdiiFault_Retry] DEFAULT (0),
 CONSTRAINT [PK_Q_ObdiiFaultRecords] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_ObdiiFaultRecords_PendingWork]
ON [gda].[Q_ObdiiFaultRecords] ([id])
INCLUDE ([ThirdPartyId], [DateTime], [Code], [FaultStateActive])
WHERE ([ProcessingStatus] = 0);
GO

-- Two indexes for the claim procedure. The PendingWork index above serves neither
-- statement: it is filtered on [ProcessingStatus] = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that filter
-- - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take @BatchSize rows, so the cost grows with queue
-- depth - worst exactly when the queue has backed up and throughput matters most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the
-- DELETE TOP (@BatchSize) can never be filled, so unindexed every row is read before
-- concluding that none qualify. [RetryCount] > 0 is a deliberate SUPERSET of the
-- sweep's own [RetryCount] >= @MaxClaimAttempts - the configured ceiling is never
-- below 1 - so this predicate needs no revision if that floor moves, and the index
-- holds only rows a device-lookup hold or a stale reclaim has touched - few or none
-- on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,073 ms to 464 ms here and from 5,953 ms to 90 ms on PostgreSQL. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. Those SQL Server figures predate the
-- sweep's literal [RetryCount] > 0, without which it could not use the filtered index
-- at all (by itself that index measured +4.6%); with it, a claim on a steady
-- 1,000,000-row queue fell from 495 ms to 324 ms. They cost 4.3% on the insert path,
-- which retains ~25x headroom over the feed either way.
CREATE NONCLUSTERED INDEX [IX_Q_ObdiiFaultRecords_RecordCreationTimeUtc]
ON [gda].[Q_ObdiiFaultRecords] ([RecordCreationTimeUtc]);
GO

CREATE NONCLUSTERED INDEX [IX_Q_ObdiiFaultRecords_Exhausted]
ON [gda].[Q_ObdiiFaultRecords] ([id])
WHERE ([RetryCount] > 0);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_ObdiiFaultRecordsFail
CREATE TABLE [gda].[Q_ObdiiFaultRecordsFail](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [OriginalQueueId] [bigint] NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [Code] [varchar](10) NOT NULL,
    [FaultStateActive] [bit] NOT NULL,
    [OriginalRecordLastChangedUtc] [datetime2](7) NOT NULL,
    [FailureReason] [nvarchar](max) NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_ObdiiFaultFail_Created] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_Q_ObdiiFaultRecordsFail] PRIMARY KEY CLUSTERED ([RecordCreationTimeUtc] ASC, [id] ASC)
    ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
) ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
GO

CREATE NONCLUSTERED INDEX [IX_Q_ObdiiFaultRecordsFail_ThirdPartyId]
ON [gda].[Q_ObdiiFaultRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc])
ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQObdiiFaultRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_ObdiiFaultRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is identified by the OUTPUT of the claiming UPDATE, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the GETUTCDATE() value just written, and
--     two instances claiming inside one ~3.33 ms tick each received BOTH batches - so
--     the atomic pop was stated here but not delivered. See the note in the body.
-- Parameters:
--   @BatchSize
--     Maximum number of records to claim in a single batch.
--   @StaleThresholdMinutes
--     Minutes after which an "in progress" record is considered stale and eligible for
--     reclaim.
--   @MaxClaimAttempts
--     Claims a record may accumulate before it is treated as poison and moved to
--     Q_ObdiiFaultRecordsFail. Optional; when NULL a record is reclaimed indefinitely, which
--     is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_ObdiiFaultRecords records, in RecordCreationTimeUtc, id order.
CREATE PROCEDURE [gda].[spClaimQObdiiFaultRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT,
    @MaxClaimAttempts INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- RecordCreationTimeUtc is NOT carried across. It is the fail table's partitioning
    -- column and defaults to now, so the row lands in the current partition and is
    -- retired on the retention schedule like every other failure.
    -- Bounded by @BatchSize, like every other statement in this procedure. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to @BatchSize - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. There is no ORDER BY, because every qualifying row has the same destination
    -- and ordering would buy a sort and no guarantee.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply ProcessingStatus = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with RetryCount + 1; skipping
    -- every claimed row would mean it is never swept, which is the infinite retry this ceiling
    -- exists to end. Mirroring the claim's own predicate removes abandoned rows and protects
    -- live ones.
    --
    -- The literal [RetryCount] > 0 changes no result - the ceiling is never below 1 - and it is
    -- what lets the optimizer use the queue's filtered _Exhausted index (WHERE [RetryCount] > 0).
    -- A cached plan must be valid for every value of @MaxClaimAttempts, so a comparison with the
    -- parameter alone can never match a filtered index: without the literal this statement read
    -- the whole queue before every claim, the healthy case included - measured at 1,000,000 rows
    -- and 9,391 logical reads per claim on a 1,000,000-row queue, and the whole claim at 495 ms
    -- against 324 ms with it. READPAST belongs to the same change. Once the DELETE seeks the
    -- index it looks each row up in the clustered index, and without READPAST that lookup would
    -- wait on - and could deadlock with - a row another session has locked. A row it skips is
    -- taken by a later pass, as with the bound above.
    IF @MaxClaimAttempts IS NOT NULL
    BEGIN
        DELETE TOP (@BatchSize) q
        OUTPUT deleted.[id],
               deleted.[ThirdPartyId], deleted.[DateTime], deleted.[Code],
               deleted.[FaultStateActive],
               deleted.[RecordLastChangedUtc],
               CONCAT('Exceeded ', @MaxClaimAttempts, ' claim attempts.')
          INTO [gda].[Q_ObdiiFaultRecordsFail] (
               [OriginalQueueId],
               [ThirdPartyId], [DateTime], [Code], [FaultStateActive],
               [OriginalRecordLastChangedUtc],
               [FailureReason])
        FROM [gda].[Q_ObdiiFaultRecords] q WITH (READPAST)
        WHERE q.[RetryCount] >= @MaxClaimAttempts
          AND q.[RetryCount] > 0
          AND (q.[ProcessingStatus] = 0
               OR q.[ProcessingStartTimeUtc] < @StaleThreshold);
    END

    -- The batch this call claimed is identified by OUTPUT, not by the timestamp it
    -- wrote. Re-reading the queue for ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    -- is NOT safe: GETUTCDATE() advances in ~3.33 ms steps, so two adapter instances
    -- claiming within one tick write the same value and each one's re-read matches
    -- BOTH batches. ProcessingStartTimeUtc is still stamped, because the stale-reclaim
    -- predicate above reads it; it is simply no longer how the batch is identified.
    --
    -- Only the ids are captured, and the rows are then projected by id. That diverges
    -- from spClaimQDeviceSubscriptionsBatch, which captures every column into its table
    -- variable, and the divergence is deliberate: an id-only capture carries no column
    -- list to keep in step with the queue table, and no collation to declare. The id
    -- set is exact - ROWLOCK, UPDLOCK, READPAST means no other claimer holds these rows
    -- - so the join is a projection of a set already fixed, not a second selection.
    --
    -- The ORDER BY on the way out is what holds the "in RecordCreationTimeUtc, id
    -- order" contract. An OUTPUT clause has no row order of its own, and the ORDER BY
    -- inside the CTE below chooses only WHICH rows are claimed.
    --
    -- Both ORDER BYs carry id as a tiebreaker. RecordCreationTimeUtc alone is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a TOP (@BatchSize) takes, and the
    -- order the batch comes back in, could differ between two runs of this procedure.
    DECLARE @ClaimedIds TABLE ([id] BIGINT NOT NULL PRIMARY KEY);

    -- Use a CTE to select ordered rows, then update them atomically.
    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_ObdiiFaultRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0  -- Available
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc, id
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,  -- InProgress
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END
    OUTPUT inserted.[id] INTO @ClaimedIds;

    -- Return the claimed records.
    SELECT q.*
    FROM [gda].[Q_ObdiiFaultRecords] q
    INNER JOIN @ClaimedIds c ON c.[id] = q.[id]
    ORDER BY q.[RecordCreationTimeUtc], q.[id];
END;
GO

GRANT EXECUTE ON [gda].[spClaimQObdiiFaultRecordsBatch] TO [geotabdigadapter_client];
GO


-- ================================================================================
-- 11. VIN RECORDS
-- ================================================================================

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_VinRecords
CREATE TABLE [gda].[Q_VinRecords](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [VehicleIdentificationNumber] [varchar](17) NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_Vin_Created] DEFAULT (SYSUTCDATETIME()),
    [RecordLastChangedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_Vin_LastChanged] DEFAULT (SYSUTCDATETIME()),
    [ProcessingStatus] [tinyint] NOT NULL CONSTRAINT [DF_Q_Vin_ProcessingStatus] DEFAULT (0),
    [ProcessingStartTimeUtc] [datetime2](7) NULL,
    [RetryCount] [tinyint] NOT NULL CONSTRAINT [DF_Q_Vin_Retry] DEFAULT (0),
 CONSTRAINT [PK_Q_VinRecords] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_VinRecords_PendingWork]
ON [gda].[Q_VinRecords] ([id])
INCLUDE ([ThirdPartyId], [DateTime], [VehicleIdentificationNumber])
WHERE ([ProcessingStatus] = 0);
GO

-- Two indexes for the claim procedure. The PendingWork index above serves neither
-- statement: it is filtered on [ProcessingStatus] = 0, and both the claim and the
-- sweep also take rows whose claim has gone stale, which lie outside that filter
-- - so the OR cannot use it.
--
-- The _RecordCreationTimeUtc index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take @BatchSize rows, so the cost grows with queue
-- depth - worst exactly when the queue has backed up and throughput matters most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the
-- DELETE TOP (@BatchSize) can never be filled, so unindexed every row is read before
-- concluding that none qualify. [RetryCount] > 0 is a deliberate SUPERSET of the
-- sweep's own [RetryCount] >= @MaxClaimAttempts - the configured ceiling is never
-- below 1 - so this predicate needs no revision if that floor moves, and the index
-- holds only rows a device-lookup hold or a stale reclaim has touched - few or none
-- on a healthy queue.
--
-- Measured on a 6,000,000-row queue of this shape: the pair took a 5,000-row claim
-- from 5,073 ms to 464 ms here and from 5,953 ms to 90 ms on PostgreSQL. NEITHER
-- INDEX ALONE IS SUFFICIENT ON EITHER PROVIDER. Those SQL Server figures predate the
-- sweep's literal [RetryCount] > 0, without which it could not use the filtered index
-- at all (by itself that index measured +4.6%); with it, a claim on a steady
-- 1,000,000-row queue fell from 495 ms to 324 ms. They cost 4.3% on the insert path,
-- which retains ~25x headroom over the feed either way.
CREATE NONCLUSTERED INDEX [IX_Q_VinRecords_RecordCreationTimeUtc]
ON [gda].[Q_VinRecords] ([RecordCreationTimeUtc]);
GO

CREATE NONCLUSTERED INDEX [IX_Q_VinRecords_Exhausted]
ON [gda].[Q_VinRecords] ([id])
WHERE ([RetryCount] > 0);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_VinRecordsFail
CREATE TABLE [gda].[Q_VinRecordsFail](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [OriginalQueueId] [bigint] NOT NULL,
    [ThirdPartyId] [varchar](50) NOT NULL,
    [DateTime] [datetime2](7) NOT NULL,
    [VehicleIdentificationNumber] [varchar](17) NOT NULL,
    [OriginalRecordLastChangedUtc] [datetime2](7) NOT NULL,
    [FailureReason] [nvarchar](max) NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_VinFail_Created] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_Q_VinRecordsFail] PRIMARY KEY CLUSTERED ([RecordCreationTimeUtc] ASC, [id] ASC)
    ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
) ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
GO

CREATE NONCLUSTERED INDEX [IX_Q_VinRecordsFail_ThirdPartyId]
ON [gda].[Q_VinRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc])
ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQVinRecordsBatch
-- Description:
--     Atomically claims and retrieves a batch of Q_VinRecords records for processing.
--     Implements the "atomic pop" pattern to prevent duplicate processing in
--     multi-instance scenarios and to ensure crash recovery. Records that are "stale"
--     (stuck in progress beyond the threshold) are automatically reclaimed.
--
--     The claimed batch is identified by the OUTPUT of the claiming UPDATE, so the
--     rows returned are exactly the rows this call claimed. Before 5.2.0.0 the batch
--     was re-read from the queue filtered on the GETUTCDATE() value just written, and
--     two instances claiming inside one ~3.33 ms tick each received BOTH batches - so
--     the atomic pop was stated here but not delivered. See the note in the body.
-- Parameters:
--   @BatchSize
--     Maximum number of records to claim in a single batch.
--   @StaleThresholdMinutes
--     Minutes after which an "in progress" record is considered stale and eligible for
--     reclaim.
--   @MaxClaimAttempts
--     Claims a record may accumulate before it is treated as poison and moved to
--     Q_VinRecordsFail. Optional; when NULL a record is reclaimed indefinitely, which
--     is the behaviour before 5.2.0.0.
-- Returns: The claimed Q_VinRecords records, in RecordCreationTimeUtc, id order.
CREATE PROCEDURE [gda].[spClaimQVinRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT,
    @MaxClaimAttempts INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever. A stale row is reclaimed with RetryCount + 1 and retried on
    -- every pass, so without this a record the DIG API will never accept occupies a
    -- slot in every batch for the life of the deployment. Mirrors
    -- spClaimQDeviceSubscriptionsBatch, which has had this since 5.2.0.0.
    --
    -- RecordCreationTimeUtc is NOT carried across. It is the fail table's partitioning
    -- column and defaults to now, so the row lands in the current partition and is
    -- retired on the retention schedule like every other failure.
    -- Bounded by @BatchSize, like every other statement in this procedure. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to @BatchSize - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. There is no ORDER BY, because every qualifying row has the same destination
    -- and ordering would buy a sort and no guarantee.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply ProcessingStatus = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with RetryCount + 1; skipping
    -- every claimed row would mean it is never swept, which is the infinite retry this ceiling
    -- exists to end. Mirroring the claim's own predicate removes abandoned rows and protects
    -- live ones.
    --
    -- The literal [RetryCount] > 0 changes no result - the ceiling is never below 1 - and it is
    -- what lets the optimizer use the queue's filtered _Exhausted index (WHERE [RetryCount] > 0).
    -- A cached plan must be valid for every value of @MaxClaimAttempts, so a comparison with the
    -- parameter alone can never match a filtered index: without the literal this statement read
    -- the whole queue before every claim, the healthy case included - measured at 1,000,000 rows
    -- and 9,391 logical reads per claim on a 1,000,000-row queue, and the whole claim at 495 ms
    -- against 324 ms with it. READPAST belongs to the same change. Once the DELETE seeks the
    -- index it looks each row up in the clustered index, and without READPAST that lookup would
    -- wait on - and could deadlock with - a row another session has locked. A row it skips is
    -- taken by a later pass, as with the bound above.
    IF @MaxClaimAttempts IS NOT NULL
    BEGIN
        DELETE TOP (@BatchSize) q
        OUTPUT deleted.[id],
               deleted.[ThirdPartyId], deleted.[DateTime],
               deleted.[VehicleIdentificationNumber],
               deleted.[RecordLastChangedUtc],
               CONCAT('Exceeded ', @MaxClaimAttempts, ' claim attempts.')
          INTO [gda].[Q_VinRecordsFail] (
               [OriginalQueueId],
               [ThirdPartyId], [DateTime], [VehicleIdentificationNumber],
               [OriginalRecordLastChangedUtc],
               [FailureReason])
        FROM [gda].[Q_VinRecords] q WITH (READPAST)
        WHERE q.[RetryCount] >= @MaxClaimAttempts
          AND q.[RetryCount] > 0
          AND (q.[ProcessingStatus] = 0
               OR q.[ProcessingStartTimeUtc] < @StaleThreshold);
    END

    -- The batch this call claimed is identified by OUTPUT, not by the timestamp it
    -- wrote. Re-reading the queue for ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    -- is NOT safe: GETUTCDATE() advances in ~3.33 ms steps, so two adapter instances
    -- claiming within one tick write the same value and each one's re-read matches
    -- BOTH batches. ProcessingStartTimeUtc is still stamped, because the stale-reclaim
    -- predicate above reads it; it is simply no longer how the batch is identified.
    --
    -- Only the ids are captured, and the rows are then projected by id. That diverges
    -- from spClaimQDeviceSubscriptionsBatch, which captures every column into its table
    -- variable, and the divergence is deliberate: an id-only capture carries no column
    -- list to keep in step with the queue table, and no collation to declare. The id
    -- set is exact - ROWLOCK, UPDLOCK, READPAST means no other claimer holds these rows
    -- - so the join is a projection of a set already fixed, not a second selection.
    --
    -- The ORDER BY on the way out is what holds the "in RecordCreationTimeUtc, id
    -- order" contract. An OUTPUT clause has no row order of its own, and the ORDER BY
    -- inside the CTE below chooses only WHICH rows are claimed.
    --
    -- Both ORDER BYs carry id as a tiebreaker. RecordCreationTimeUtc alone is not
    -- unique - a bulk insert can land many rows on one value - and rows tied on it have
    -- no defined relative order, so which of them a TOP (@BatchSize) takes, and the
    -- order the batch comes back in, could differ between two runs of this procedure.
    DECLARE @ClaimedIds TABLE ([id] BIGINT NOT NULL PRIMARY KEY);

    -- Use a CTE to select ordered rows, then update them atomically.
    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_VinRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0  -- Available
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc, id
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,  -- InProgress
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END
    OUTPUT inserted.[id] INTO @ClaimedIds;

    -- Return the claimed records.
    SELECT q.*
    FROM [gda].[Q_VinRecords] q
    INNER JOIN @ClaimedIds c ON c.[id] = q.[id]
    ORDER BY q.[RecordCreationTimeUtc], q.[id];
END;
GO

GRANT EXECUTE ON [gda].[spClaimQVinRecordsBatch] TO [geotabdigadapter_client];
GO

/*** [END] SSMS-Generated Script Above ***/


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
CREATE TABLE [gda].[DIGInvalidRecords](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [GeotabGUID] [nvarchar](50) NOT NULL,
    [RecordType] [nvarchar](50) NOT NULL,
    [SerialNo] [nvarchar](50) NOT NULL,
    [RecordDateTime] [datetime2](7) NOT NULL,
    [BaseRecordJson] [nvarchar](max) NOT NULL,
    [Cause] [nvarchar](1000) NOT NULL,
    [TimeStamp] [datetime2](7) NOT NULL,
    [UserId] [nvarchar](100) NOT NULL,
    [RetrievedAtUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_DIGInvalidRecords_RetrievedAt] DEFAULT (SYSUTCDATETIME()),
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_DIGInvalidRecords_Created] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_DIGInvalidRecords] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_DIGInvalidRecords_GeotabGUID]
ON [gda].[DIGInvalidRecords] ([GeotabGUID]);
GO

CREATE NONCLUSTERED INDEX [IX_DIGInvalidRecords_RecordType_TimeStamp]
ON [gda].[DIGInvalidRecords] ([RecordType], [TimeStamp] DESC)
INCLUDE ([SerialNo], [Cause]);
GO

-- Supports retention. This table cannot be partitioned - its UNIQUE index on
-- GeotabGUID alone is the dedupe guarantee the InvalidRecordRetrievalService
-- relies on, and PostgreSQL requires the partition key in every unique index on
-- a partitioned table - so old records are removed by a batched DELETE on
-- RecordCreationTimeUtc instead. Without this index each batch would scan the
-- whole of the table it is trying to shrink.
CREATE NONCLUSTERED INDEX [IX_DIGInvalidRecords_RecordCreationTimeUtc]
ON [gda].[DIGInvalidRecords] ([RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: DIGInvalidRecordsCursor
-- Persists the pagination cursor for the InvalidRecordRetrievalService.
-- Single-row table enforced by CHECK constraint.
CREATE TABLE [gda].[DIGInvalidRecordsCursor](
    [id] [int] NOT NULL CONSTRAINT [DF_DIGInvalidRecordsCursor_Id] DEFAULT (1) CHECK ([id] = 1),
    [NextResultKey] [int] NOT NULL CONSTRAINT [DF_DIGInvalidRecordsCursor_NextResultKey] DEFAULT (0),
    [LastUpdatedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_DIGInvalidRecordsCursor_LastUpdated] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_DIGInvalidRecordsCursor] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

-- Insert the initial cursor row
INSERT INTO [gda].[DIGInvalidRecordsCursor] ([id], [NextResultKey], [LastUpdatedUtc])
VALUES (1, 0, GETUTCDATE());
GO


/*** [START] OEM Subscription Mode (added in 5.2.0.0) ***/
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Sequence: SeqDeviceSubscriptionOrdinal
--
-- The single ordering key shared by both subscription intakes. The delta intake
-- draws from it via the Q_DeviceSubscriptions.[OrderingOrdinal] column default; a
-- polling service draws ONE value per poll and stamps it on every staged row.
--
-- Values are monotonic but not gapless (the engine caches ranges, and a restart may
-- skip forward). Monotonicity is what the high-water mark requires; gaplessness is
-- not. Note also that ordinal order is DRAW order, not COMMIT order - two writers
-- can draw 5 and 6 and commit 6 first. The same is true of IDENTITY columns.
CREATE SEQUENCE [gda].[SeqDeviceSubscriptionOrdinal]
    AS [bigint]
    START WITH 1
    INCREMENT BY 1
    NO MAXVALUE
    NO CYCLE;
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: DeviceSubscriptions
--
-- The OEM control plane's state table. Separate from ProvisionedDevices by design;
-- the two control planes never share a table. Terminated rows are RETAINED
-- (status 0) rather than deleted, so re-enrollment is an update and the audit
-- answer is unambiguous.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [gda].[DeviceSubscriptions](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [ThirdPartyId] [varchar](50) COLLATE SQL_Latin1_General_CP1_CS_AS NOT NULL,
    [IdentifierType] [varchar](20) COLLATE SQL_Latin1_General_CP1_CS_AS NOT NULL CONSTRAINT [DF_DeviceSubscriptions_IdentifierType] DEFAULT ('Vin'),
    [GeotabSerialNumber] [varchar](50) COLLATE SQL_Latin1_General_CP1_CS_AS NOT NULL,
    [SubscriptionStatus] [tinyint] NOT NULL CONSTRAINT [DF_DeviceSubscriptions_SubscriptionStatus] DEFAULT (1),
    [SubPlan] [varchar](50) NULL,
    [LastAppliedOrdinal] [bigint] NOT NULL CONSTRAINT [DF_DeviceSubscriptions_LastAppliedOrdinal] DEFAULT (0),
    [SubscribedDateTimeUtc] [datetime2](7) NULL,
    [TerminatedDateTimeUtc] [datetime2](7) NULL,
    [TerminationReason] [varchar](200) NULL,
    [LastSeenInSnapshotUtc] [datetime2](7) NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_DeviceSubscriptions_Created] DEFAULT (SYSUTCDATETIME()),
    [RecordLastChangedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_DeviceSubscriptions_LastChanged] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_DeviceSubscriptions] PRIMARY KEY CLUSTERED
(
    [id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

-- UNFILTERED ON PURPOSE. This is the MERGE's join target. Filtering it would let a
-- re-subscribe insert a second row for an identifier that already has a terminated
-- one, and the upsert would stop being an upsert.
CREATE UNIQUE NONCLUSTERED INDEX [IX_DeviceSubscriptions_ThirdPartyId]
ON [gda].[DeviceSubscriptions] ([ThirdPartyId]);
GO

-- FILTERED ON PURPOSE. MyGeotab enforces one identifier per serial number for the
-- life of a subscription, but terminating a subscription removes that registration
-- entirely - so after terminate-then-reprovision a serial number can legitimately
-- acquire a DIFFERENT identifier. An unfiltered unique index would encode "one
-- identifier per serial number forever" and would wedge re-provisioning with no
-- recovery short of manual SQL.
CREATE UNIQUE NONCLUSTERED INDEX [IX_DeviceSubscriptions_GeotabSerialNumber_Active]
ON [gda].[DeviceSubscriptions] ([GeotabSerialNumber])
WHERE ([SubscriptionStatus] = 1);
GO

-- Serves the snapshot-mode passes of spMerge_stg_DeviceSubscriptions, which find the rows
-- a snapshot has left out by selecting on [SubscriptionStatus].
--
-- It does NOT cover the device-lookup cache query, and neither does the similarly shaped
-- IX_ProvisionedDevices_ThirdPartyId_Includes cover its own. Both cache queries also
-- select [RecordLastChangedUtc], which is in neither index. Adding the column to each was
-- measured at 50,000 rows and landed either side of zero - +7.3% here against -6.2%
-- there - on caches that reload on a multi-minute interval. Both INCLUDE lists are
-- therefore left as they are, deliberately.
CREATE NONCLUSTERED INDEX [IX_DeviceSubscriptions_Active]
ON [gda].[DeviceSubscriptions] ([SubscriptionStatus])
INCLUDE ([ThirdPartyId], [GeotabSerialNumber]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_DeviceSubscriptions
--
-- The delta intake, used by the email-driven process. An external process inserts
-- Subscribe and Terminate instructions; DeviceSubscriptionService claims, stages
-- and merges them.
--
-- Deliberately NOT unique on ThirdPartyId - a device can be subscribed, terminated
-- and resubscribed.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [gda].[Q_DeviceSubscriptions](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [OrderingOrdinal] [bigint] NOT NULL CONSTRAINT [DF_Q_DevSub_Ordinal] DEFAULT (NEXT VALUE FOR [gda].[SeqDeviceSubscriptionOrdinal]),
    -- Both values are legal, so a wrong one is silent rather than an error; the constraint at least closes the set.
    [OperationType] [tinyint] NOT NULL CONSTRAINT [CK_Q_DevSub_OperationType] CHECK ([OperationType] IN (0, 1)),
    [ThirdPartyId] [varchar](50) COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
    [IdentifierType] [varchar](20) COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
    [GeotabSerialNumber] [varchar](50) COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
    [SubPlan] [varchar](50) NULL,
    [SourceStatus] [varchar](30) NULL,
    [EffectiveDateTimeUtc] [datetime2](7) NULL,
    [TerminationReason] [varchar](200) NULL,
    [SourceReference] [varchar](500) NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_DevSub_Created] DEFAULT (SYSUTCDATETIME()),
    [RecordLastChangedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_DevSub_LastChanged] DEFAULT (SYSUTCDATETIME()),
    [ProcessingStatus] [tinyint] NOT NULL CONSTRAINT [DF_Q_DevSub_ProcessingStatus] DEFAULT (0),
    [ProcessingStartTimeUtc] [datetime2](7) NULL,
    [ClaimId] [uniqueidentifier] NULL,
    [RetryCount] [tinyint] NOT NULL CONSTRAINT [DF_Q_DevSub_Retry] DEFAULT (0),
 CONSTRAINT [PK_Q_DeviceSubscriptions] PRIMARY KEY CLUSTERED
(
    [id] ASC
)
) ON [PRIMARY]
GO

-- Keyed on OrderingOrdinal rather than id, because OrderingOrdinal is what the
-- claim procedure orders by and what the merge compares against the high-water mark.
CREATE NONCLUSTERED INDEX [IX_Q_DeviceSubscriptions_PendingWork]
ON [gda].[Q_DeviceSubscriptions] ([OrderingOrdinal])
INCLUDE ([OperationType], [ThirdPartyId], [IdentifierType], [GeotabSerialNumber], [SubPlan])
WHERE ([ProcessingStatus] = 0);
GO

-- Two indexes for the claim procedure, the same pair the other twelve queues
-- carry - but keyed on [OrderingOrdinal], because that is what THIS claim orders
-- by. An index on [RecordCreationTimeUtc] would serve nothing here. The
-- PendingWork index above serves neither statement: it is filtered on
-- [ProcessingStatus] = 0, and both the claim and the sweep also take rows whose
-- claim has gone stale, which lie outside that filter - so the OR cannot use it.
--
-- The _OrderingOrdinal index provides the claim's ORDER BY. Without it a
-- claim sorts the WHOLE queue to take @BatchSize rows, so the cost grows with queue
-- depth - worst exactly when the queue has backed up and throughput matters most.
--
-- The _Exhausted index serves the exhausted-row sweep, which runs before EVERY claim
-- and whose worst case is the HEALTHY one: with nothing exhausted the
-- DELETE TOP (@BatchSize) can never be filled, so unindexed every row is read before
-- concluding that none qualify. [RetryCount] > 0 is a deliberate SUPERSET of the
-- sweep's own [RetryCount] >= @MaxClaimAttempts - the configured ceiling is never
-- below 1 - so this predicate needs no revision if that floor moves, and the index
-- holds only rows a device-lookup hold or a stale reclaim has touched - few or none
-- on a healthy queue.
--
-- The pair was measured on a telemetry queue holding 6,000,000 rows: a 5,000-row
-- claim fell from 5,073 ms to 464 ms here and from 5,953 ms to 90 ms on
-- PostgreSQL, and NEITHER INDEX ALONE WAS SUFFICIENT ON EITHER PROVIDER. This
-- queue carries operator instructions and does not reach that depth; it takes the
-- pair so that one claim shape is indexed one way on all thirteen.
CREATE NONCLUSTERED INDEX [IX_Q_DeviceSubscriptions_OrderingOrdinal]
ON [gda].[Q_DeviceSubscriptions] ([OrderingOrdinal]);
GO

CREATE NONCLUSTERED INDEX [IX_Q_DeviceSubscriptions_Exhausted]
ON [gda].[Q_DeviceSubscriptions] ([id])
WHERE ([RetryCount] > 0);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: Q_DeviceSubscriptionsFail
--
-- Follows the Q_ProvisionDevicesFail shape: no ProcessingStatus /
-- ProcessingStartTimeUtc / ClaimId / RetryCount, RecordLastChangedUtc renamed to
-- OriginalRecordLastChangedUtc, and RecordCreationTimeUtc redefined to mean "when
-- the failure was recorded".
--
-- OriginalQueueId, OriginalOrderingOrdinal and OriginalRecordCreationTimeUtc are
-- ALL NULLABLE ON PURPOSE. A polling service stages directly and never creates a
-- queue row, so a row the merge rejects in snapshot mode has no queue identity to
-- record. Making these NOT NULL would leave poll-sourced rejections with nowhere
-- to land.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [gda].[Q_DeviceSubscriptionsFail](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [OriginalQueueId] [bigint] NULL,
    [OriginalOrderingOrdinal] [bigint] NULL,
    [OperationType] [tinyint] NOT NULL,
    [ThirdPartyId] [varchar](50) COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
    [IdentifierType] [varchar](20) COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
    [GeotabSerialNumber] [varchar](50) COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
    [SubPlan] [varchar](50) NULL,
    [SourceStatus] [varchar](30) NULL,
    [EffectiveDateTimeUtc] [datetime2](7) NULL,
    [TerminationReason] [varchar](200) NULL,
    [SourceReference] [varchar](500) NULL,
    [OriginalRecordCreationTimeUtc] [datetime2](7) NULL,
    [OriginalRecordLastChangedUtc] [datetime2](7) NULL,
    [FailureReason] [nvarchar](max) NOT NULL,
    [RecordCreationTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Q_DevSubFail_Created] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_Q_DeviceSubscriptionsFail] PRIMARY KEY CLUSTERED ([RecordCreationTimeUtc] ASC, [id] ASC)
    ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
) ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc])
GO

-- Both included columns are load-bearing, and the second one is not obvious.
-- An aligned index carries the partitioning column either way, but WHERE it carries it depends
-- on whether the index names it: name RecordCreationTimeUtc as an INCLUDE and it is an included
-- column; leave it out and SQL Server promotes it into the KEY, ahead of ThirdPartyId. A
-- leading RecordCreationTimeUtc means a lookup by ThirdPartyId -- the only thing this index
-- exists for -- can no longer seek, so the index stops doing its job while still looking
-- present in the catalog. That is what this index did before both columns were named here, and
-- it is why all twelve sibling fail-table indexes carry the same pair. Do not trim it back.
CREATE NONCLUSTERED INDEX [IX_Q_DeviceSubscriptionsFail_ThirdPartyId]
ON [gda].[Q_DeviceSubscriptionsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc])
ON [DateTimePartitionScheme_GeotabDigAdapter]([RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: stg_DeviceSubscriptions
--
-- Both intakes converge here. Unlike the MyGeotab API Adapter's staging tables,
-- this one has two potential writers (DeviceSubscriptionService and, later, a
-- polling service), so the merge scopes every operation to a BatchId and DELETEs
-- only its own rows rather than TRUNCATEing the table.
--
-- Removing TRUNCATE also removed the pattern's only self-healing step, so
-- StagedDateTimeUtc exists to let the service sweep orphaned batches left behind by
-- a crash between staging and merge.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [gda].[stg_DeviceSubscriptions](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [BatchId] [uniqueidentifier] NOT NULL,
    -- Both values are legal, so a wrong one is silent rather than an error; the constraint at least closes the set.
    [OperationType] [tinyint] NOT NULL CONSTRAINT [CK_stg_DevSub_OperationType] CHECK ([OperationType] IN (0, 1)),
    [Ordinal] [bigint] NOT NULL,
    [OriginalQueueId] [bigint] NULL,
    [ThirdPartyId] [varchar](50) COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
    [IdentifierType] [varchar](20) COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
    [GeotabSerialNumber] [varchar](50) COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
    [SubPlan] [varchar](50) NULL,
    [SourceStatus] [varchar](30) NULL,
    [EffectiveDateTimeUtc] [datetime2](7) NULL,
    [TerminationReason] [varchar](200) NULL,
    [SourceReference] [varchar](500) NULL,
    [StagedDateTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_stg_DevSub_Staged] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_stg_DeviceSubscriptions] PRIMARY KEY CLUSTERED
(
    [id] ASC
)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_stg_DeviceSubscriptions_BatchId]
ON [gda].[stg_DeviceSubscriptions] ([BatchId]);
GO

CREATE NONCLUSTERED INDEX [IX_stg_DeviceSubscriptions_StagedDateTimeUtc]
ON [gda].[stg_DeviceSubscriptions] ([StagedDateTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: DeviceSubscriptionHistory
--
-- Append-only audit. Written per APPLIED INSTRUCTION, not per state transition -
-- a transition-only table loses superseded instructions and plan changes, which are
-- two of the things it exists to record.
--
-- OperationType: 0 = Terminate, 1 = Subscribe
-- Outcome:       0 = Applied, 1 = NoOp, 2 = Superseded, 3 = Rejected
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [gda].[DeviceSubscriptionHistory](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [ThirdPartyId] [varchar](50) COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
    [IdentifierType] [varchar](20) COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
    [GeotabSerialNumber] [varchar](50) COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
    [SubPlan] [varchar](50) NULL,
    [SourceStatus] [varchar](30) NULL,
    [OperationType] [tinyint] NOT NULL,
    [Outcome] [tinyint] NOT NULL,
    [PreviousStatus] [tinyint] NULL,
    [NewStatus] [tinyint] NULL,
    [Ordinal] [bigint] NOT NULL,
    [EffectiveDateTimeUtc] [datetime2](7) NULL,
    [TerminationReason] [varchar](200) NULL,
    [AppliedDateTimeUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_DevSubHist_Applied] DEFAULT (SYSUTCDATETIME()),
    [SourceReference] [varchar](500) NULL,
 CONSTRAINT [PK_DeviceSubscriptionHistory] PRIMARY KEY CLUSTERED
(
    [id] ASC
)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_DeviceSubscriptionHistory_ThirdPartyId]
ON [gda].[DeviceSubscriptionHistory] ([ThirdPartyId], [AppliedDateTimeUtc] DESC);
GO

-- Read by the termination brake's rolling window on every merge. Without it the
-- window scan is the merge's hot spot.
CREATE NONCLUSTERED INDEX [IX_DeviceSubscriptionHistory_Terminations]
ON [gda].[DeviceSubscriptionHistory] ([AppliedDateTimeUtc])
WHERE ([NewStatus] = 0);
GO

-- Read by the merge's pass 3b for every staged Subscribe: the highest ordinal of a
-- Terminate already merged for an identifier, and for a serial number. Filtered to
-- the Applied and NoOp Terminates pass 3b reads, so Subscribes and Superseded and
-- Rejected instructions stay out of both. They still grow by one row for every such
-- Terminate merged, re-sent ones included.
CREATE NONCLUSTERED INDEX [IX_DeviceSubscriptionHistory_TerminateByIdentifier]
ON [gda].[DeviceSubscriptionHistory] ([ThirdPartyId], [Ordinal])
WHERE ([OperationType] = 0 AND [Outcome] IN (0, 1));
GO

CREATE NONCLUSTERED INDEX [IX_DeviceSubscriptionHistory_TerminateBySerial]
ON [gda].[DeviceSubscriptionHistory] ([GeotabSerialNumber], [Ordinal])
WHERE ([OperationType] = 0 AND [Outcome] IN (0, 1));
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: DeviceSubscriptionGuardState
--
-- Single-row table holding the termination brake's tripped state, following the
-- DIGInvalidRecordsCursor precedent. The trip is STICKY until an operator clears
-- it - without persistence, a restart would silently re-run the reconciliation the
-- brake just refused.
--
-- NOTE: this table is written by the CALLER, not by spMerge_stg_DeviceSubscriptions.
-- The merge evaluates the brake BEFORE applying anything, applies nothing, and
-- returns a sentinel; the caller then records the trip. This keeps the two
-- providers' control flow identical - a PostgreSQL function shares the caller's
-- transaction and cannot persist a trip that survives its own rollback, and there
-- is no value in the two implementations diverging on this point.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [gda].[DeviceSubscriptionGuardState](
    [id] [tinyint] NOT NULL CONSTRAINT [DF_DevSubGuard_Id] DEFAULT (1) CONSTRAINT [CK_DevSubGuard_Id] CHECK ([id] = 1),
    [IsTripped] [bit] NOT NULL CONSTRAINT [DF_DevSubGuard_IsTripped] DEFAULT (0),
    [TrippedDateTimeUtc] [datetime2](7) NULL,
    [TrippedReason] [varchar](500) NULL,
    [TrippedTerminationCount] [int] NULL,
    [TrippedWindowCount] [int] NULL,
    [RecordLastChangedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_DevSubGuard_LastChanged] DEFAULT (SYSUTCDATETIME()),
 CONSTRAINT [PK_DeviceSubscriptionGuardState] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

-- Insert the initial (untripped) guard row.
INSERT INTO [gda].[DeviceSubscriptionGuardState] ([id], [IsTripped], [RecordLastChangedUtc])
VALUES (1, 0, SYSUTCDATETIME());
GO

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQDeviceSubscriptionsBatch
--
-- Atomically claims and retrieves a batch of Q_DeviceSubscriptions records for
-- processing. Retarget of spClaimQProvisionDevicesBatch with three deliberate
-- divergences:
--
--   1. ORDER BY [OrderingOrdinal], [id] rather than ORDER BY [RecordCreationTimeUtc].
--      A multi-row INSERT stamps an identical default timestamp on every row, so
--      ordering by it is a tie with no tiebreaker. That is harmless for an
--      insert-only, order-independent queue; it is fatal for a queue carrying
--      Subscribe AND Terminate for the same key.
--   2. A ClaimId column, and a full-row capture. Returning the batch from the UPDATE
--      itself is NOT a divergence any more - all thirteen claim procedures do that,
--      and the twelve were rewritten to in 5.2.0.0. What still differs is the shape of
--      the capture: the twelve OUTPUT only the ids and project the rows by joining
--      back, whereas this one captures every column into a table variable, because the
--      merge test suite reads this result set positionally and the column order is
--      therefore part of the contract. ClaimId is stamped here and nowhere else.
--   3. A RetryCount cap. Nothing in the codebase reads RetryCount today, so a
--      permanently-failing row is reclaimed forever. Over-cap rows are moved to
--      Q_DeviceSubscriptionsFail before claiming.
--
-- Returns: The claimed Q_DeviceSubscriptions records, in application order.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [gda].[spClaimQDeviceSubscriptionsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT,
    @MaxClaimAttempts INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @StaleThreshold DATETIME2(7) = DATEADD(MINUTE, -@StaleThresholdMinutes, SYSUTCDATETIME());
    DECLARE @ClaimId UNIQUEIDENTIFIER = NEWID();

    -- Fail out rows that have exhausted their claim attempts, so they stop being
    -- reclaimed forever.
    -- Bounded by @BatchSize, like every other statement in this procedure. This one runs
    -- before EVERY claim, and being unbounded is worst exactly when the ceiling matters: a
    -- queue that has backed up, or the first run after an upgrade on a deployment with a
    -- large stuck population, would otherwise delete and re-insert an arbitrary number of
    -- rows in a single statement whose duration is unrelated to @BatchSize - and if that
    -- exceeds the command timeout it is retried from the start and can fail to make
    -- progress at all. Nothing is lost to the bound: the qualifying set grows only on a
    -- stale reclaim or a device-lookup hold, so what one pass leaves behind the next pass
    -- takes. There is no ORDER BY, because every qualifying row has the same destination
    -- and ordering would buy a sort and no guarantee.
    --
    -- It takes only rows the CLAIM below would itself be entitled to take: available, or claimed
    -- but stale. A row another instance is actively working on is therefore left alone - without
    -- that, the sweep could delete it mid-flight, the processing instance's own delete would hit
    -- an affected-row shortfall, and GenericEntityPersister turns that into an exception the
    -- service turns into Fatal, which is Process.Kill(). Reachable only with two instances of the
    -- same service, but the guard costs nothing.
    --
    -- NOT simply ProcessingStatus = 0. A row claimed by a process that then died stays at 1
    -- forever and is re-taken by the stale branch every 30 minutes with RetryCount + 1; skipping
    -- every claimed row would mean it is never swept, which is the infinite retry this ceiling
    -- exists to end. Mirroring the claim's own predicate removes abandoned rows and protects
    -- live ones.
    --
    -- The literal [RetryCount] > 0 changes no result - the ceiling is never below 1 - and it is
    -- what lets the optimizer use the queue's filtered _Exhausted index (WHERE [RetryCount] > 0).
    -- A cached plan must be valid for every value of @MaxClaimAttempts, so a comparison with the
    -- parameter alone can never match a filtered index: without the literal this statement read
    -- the whole queue before every claim, the healthy case included - measured at 1,000,000 rows
    -- and 9,391 logical reads per claim on a 1,000,000-row queue, and the whole claim at 495 ms
    -- against 324 ms with it. READPAST belongs to the same change. Once the DELETE seeks the
    -- index it looks each row up in the clustered index, and without READPAST that lookup would
    -- wait on - and could deadlock with - a row another session has locked. A row it skips is
    -- taken by a later pass, as with the bound above.
    IF @MaxClaimAttempts IS NOT NULL
    BEGIN
        DELETE TOP (@BatchSize) q
        OUTPUT deleted.[id], deleted.[OrderingOrdinal], deleted.[OperationType],
               deleted.[ThirdPartyId], deleted.[IdentifierType], deleted.[GeotabSerialNumber],
               deleted.[SubPlan], deleted.[SourceStatus], deleted.[EffectiveDateTimeUtc],
               deleted.[TerminationReason], deleted.[SourceReference],
               deleted.[RecordCreationTimeUtc], deleted.[RecordLastChangedUtc],
               CONCAT('Exceeded ', @MaxClaimAttempts, ' claim attempts.')
          INTO [gda].[Q_DeviceSubscriptionsFail] (
               [OriginalQueueId], [OriginalOrderingOrdinal], [OperationType],
               [ThirdPartyId], [IdentifierType], [GeotabSerialNumber],
               [SubPlan], [SourceStatus], [EffectiveDateTimeUtc],
               [TerminationReason], [SourceReference],
               [OriginalRecordCreationTimeUtc], [OriginalRecordLastChangedUtc],
               [FailureReason])
        FROM [gda].[Q_DeviceSubscriptions] q WITH (READPAST)
        WHERE q.[RetryCount] >= @MaxClaimAttempts
          AND q.[RetryCount] > 0
          AND (q.[ProcessingStatus] = 0
               OR q.[ProcessingStartTimeUtc] < @StaleThreshold);
    END

    -- OUTPUT has NO DEFINED ROW ORDER, so on its own it cannot satisfy the contract
    -- this procedure's header states ("in application order"). The ORDER BY in the
    -- CTE below only chooses WHICH rows are claimed, not the order they come back
    -- in. Land the claimed rows and sort them on the way out, matching the
    -- PostgreSQL function, which closes with ORDER BY "OrderingOrdinal", "id".
    --
    -- The identifier columns declare their collation EXPLICITLY. A table variable
    -- otherwise materialises the database default, which conflicts with the
    -- CS_AS-pinned base-table columns (error 468) on a CI_AS database - the trap
    -- that the merge procedure's temp tables already guard against the same way.
    --
    -- Column order must stay identical to Q_DeviceSubscriptions. The result set is
    -- the procedure-to-client contract, and the merge test suite reads it
    -- positionally.
    DECLARE @Claimed TABLE (
        [id] BIGINT,
        [OrderingOrdinal] BIGINT,
        [OperationType] TINYINT,
        [ThirdPartyId] VARCHAR(50) COLLATE SQL_Latin1_General_CP1_CS_AS,
        [IdentifierType] VARCHAR(20) COLLATE SQL_Latin1_General_CP1_CS_AS,
        [GeotabSerialNumber] VARCHAR(50) COLLATE SQL_Latin1_General_CP1_CS_AS,
        [SubPlan] VARCHAR(50),
        [SourceStatus] VARCHAR(30),
        [EffectiveDateTimeUtc] DATETIME2(7),
        [TerminationReason] VARCHAR(200),
        [SourceReference] VARCHAR(500),
        [RecordCreationTimeUtc] DATETIME2(7),
        [RecordLastChangedUtc] DATETIME2(7),
        [ProcessingStatus] TINYINT,
        [ProcessingStartTimeUtc] DATETIME2(7),
        [ClaimId] UNIQUEIDENTIFIER,
        [RetryCount] TINYINT
    );

    ;WITH claimed AS (
        SELECT TOP (@BatchSize) q.*
        FROM [gda].[Q_DeviceSubscriptions] q WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE q.[ProcessingStatus] = 0
           OR (q.[ProcessingStatus] = 1 AND q.[ProcessingStartTimeUtc] < @StaleThreshold)
        ORDER BY q.[OrderingOrdinal], q.[id]
    )
    UPDATE claimed
    SET [ProcessingStatus] = 1,
        [ProcessingStartTimeUtc] = @ProcessingStartTimeUtc,
        [ClaimId] = @ClaimId,
        [RetryCount] = CASE WHEN [ProcessingStatus] = 1 THEN [RetryCount] + 1 ELSE [RetryCount] END
    OUTPUT inserted.[id], inserted.[OrderingOrdinal], inserted.[OperationType],
           inserted.[ThirdPartyId], inserted.[IdentifierType], inserted.[GeotabSerialNumber],
           inserted.[SubPlan], inserted.[SourceStatus], inserted.[EffectiveDateTimeUtc],
           inserted.[TerminationReason], inserted.[SourceReference],
           inserted.[RecordCreationTimeUtc], inserted.[RecordLastChangedUtc],
           inserted.[ProcessingStatus], inserted.[ProcessingStartTimeUtc],
           inserted.[ClaimId], inserted.[RetryCount]
      INTO @Claimed;

    SELECT [id], [OrderingOrdinal], [OperationType], [ThirdPartyId], [IdentifierType],
           [GeotabSerialNumber], [SubPlan], [SourceStatus], [EffectiveDateTimeUtc],
           [TerminationReason], [SourceReference], [RecordCreationTimeUtc],
           [RecordLastChangedUtc], [ProcessingStatus], [ProcessingStartTimeUtc],
           [ClaimId], [RetryCount]
    FROM @Claimed
    ORDER BY [OrderingOrdinal], [id];
END
GO

GRANT EXECUTE ON [gda].[spClaimQDeviceSubscriptionsBatch] TO [geotabdigadapter_client];
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spMerge_stg_DeviceSubscriptions
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--		Upserts records from the stg_DeviceSubscriptions staging table to the
--		DeviceSubscriptions table and then clears the batch's staging rows. This is the only
--		writer to DeviceSubscriptions; both the queue-driven intake and any snapshot-driven
--		intake converge here so that the two cannot disagree on identical data.
--
--		In Delta mode (@Mode = 0), rows are applied as individual Subscribe and Terminate
--		instructions and rows absent from the batch are left untouched. In Snapshot mode
--		(@Mode = 1), the batch is the complete subscribed set: rows present are upserted to
--		Active and every Active row absent from the batch is set to 0 (Terminated), following
--		the SetEntityStatusDeletedForMissing… semantics used by the other merge procedures.
--
--		@ExpectedIdentifierTypes is a comma-separated allow-list. A scalar would forbid the
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
--		  owns the unit of work spanning stage -> merge -> queue-delete. SQL Server would
--		  permit this procedure to open its own, but the PostgreSQL counterpart is a function
--		  that cannot, and the two are maintained as a pair.
-- ==========================================================================================
CREATE PROCEDURE [gda].[spMerge_stg_DeviceSubscriptions]
    @BatchId UNIQUEIDENTIFIER,
    @Mode TINYINT,
    @SnapshotOrdinal BIGINT = NULL,
    @ExpectedIdentifierTypes VARCHAR(500) = 'Vin',
    @MaxTerminationsPerWindow INT = NULL,
    @TerminationWindowMinutes INT = 60
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @IsTripped BIT;
    DECLARE @StagedCount INT;
    DECLARE @ProposedTerminations INT = 0;
    DECLARE @WindowTerminations INT = 0;
    DECLARE @SubscribedCount INT = 0;
    DECLARE @TerminatedCount INT = 0;
    DECLARE @RejectedCount INT = 0;

    IF @Mode NOT IN (0, 1)
    BEGIN
        RAISERROR('@Mode must be 0 (Delta) or 1 (Snapshot).', 16, 1);
        RETURN;
    END

    IF @Mode = 1 AND @SnapshotOrdinal IS NULL
    BEGIN
        RAISERROR('@SnapshotOrdinal is required when @Mode = 1 (Snapshot).', 16, 1);
        RETURN;
    END

    -- Defence in depth. The ordinal high-water mark is what makes concurrent merges
    -- CORRECT; this lock only keeps them cheap, by avoiding pointless conflict.
    --
    -- Taken ONLY when the caller has opened a transaction. sp_getapplock with
    -- @LockOwner = 'Transaction' raises an error outright when @@TRANCOUNT = 0, so an
    -- unconditional call would make the procedure unusable outside a transaction -
    -- including from ad-hoc troubleshooting and test harnesses. Skipping it there is
    -- safe precisely because the lock is not what provides correctness. A
    -- session-scoped lock is deliberately not used: it would have to be released on
    -- every exit path, including the early returns below.
    IF @@TRANCOUNT > 0
    BEGIN
        DECLARE @LockResult INT;
        EXEC @LockResult = sp_getapplock
            @Resource = 'gda.spMerge_stg_DeviceSubscriptions',
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 30000;
        IF @LockResult < 0
        BEGIN
            RAISERROR('Could not acquire the device-subscription merge lock.', 16, 1);
            RETURN;
        END
    END

    ------------------------------------------------------------------------------
    -- Pass 0a: refuse outright if the brake is already tripped. Sticky until an
    -- operator clears gda.DeviceSubscriptionGuardState.
    ------------------------------------------------------------------------------
    SELECT @IsTripped = [IsTripped] FROM [gda].[DeviceSubscriptionGuardState] WHERE [id] = 1;

    IF ISNULL(@IsTripped, 0) = 1
    BEGIN
        SELECT CAST(4 AS TINYINT) AS [ResultCode], CAST(NULL AS BIGINT) AS [StagingId],
               CAST(NULL AS BIGINT) AS [QueueId], CAST(NULL AS BIGINT) AS [QueueOrdinal],
               CAST(NULL AS VARCHAR(50)) AS [Identifier], CAST(NULL AS VARCHAR(50)) AS [SerialNumber],
               CAST('Termination guard is tripped; no changes applied. Clear gda.DeviceSubscriptionGuardState to resume.' AS NVARCHAR(MAX)) AS [ResultMessage];
        RETURN;
    END

    SELECT @StagedCount = COUNT(*) FROM [gda].[stg_DeviceSubscriptions] WHERE [BatchId] = @BatchId;

    ------------------------------------------------------------------------------
    -- Pass 0b: zero staged rows in snapshot mode is a HARD ERROR, not "everything is
    -- absent". The merge deletes its own staging rows as its last act, so if a commit
    -- ACK is lost and the call is retried, the retry sees zero rows for this BatchId.
    -- Treating that as a full absence would wipe the fleet. This check lives here
    -- rather than in the caller precisely because the caller is the thing retrying.
    ------------------------------------------------------------------------------
    IF @Mode = 1 AND @StagedCount = 0
    BEGIN
        SELECT CAST(3 AS TINYINT) AS [ResultCode], CAST(NULL AS BIGINT) AS [StagingId],
               CAST(NULL AS BIGINT) AS [QueueId], CAST(NULL AS BIGINT) AS [QueueOrdinal],
               CAST(NULL AS VARCHAR(50)) AS [Identifier], CAST(NULL AS VARCHAR(50)) AS [SerialNumber],
               CAST('Snapshot mode invoked with zero staged rows for the supplied BatchId; refusing to reconcile. This is the signature of a retried merge whose staging rows were already consumed.' AS NVARCHAR(MAX)) AS [ResultMessage];
        RETURN;
    END

    IF @StagedCount = 0
    BEGIN
        SELECT CAST(0 AS TINYINT) AS [ResultCode], CAST(NULL AS BIGINT) AS [StagingId],
               CAST(NULL AS BIGINT) AS [QueueId], CAST(NULL AS BIGINT) AS [QueueOrdinal],
               CAST(NULL AS VARCHAR(50)) AS [Identifier], CAST(NULL AS VARCHAR(50)) AS [SerialNumber],
               CAST('No staged rows for the supplied BatchId; nothing to do.' AS NVARCHAR(MAX)) AS [ResultMessage];
        RETURN;
    END

    ------------------------------------------------------------------------------
    -- Pass 1: materialise, trim, and normalise this batch.
    --
    -- Identifier columns on the temp tables are COLLATE-pinned. Temp tables
    -- otherwise inherit tempdb's collation, which need not match this database's -
    -- that mismatch is precisely why the main adapter's merge procedures pin
    -- collation on their own temp tables. Without it, every join below between a
    -- temp table and a base table risks a collation-conflict error.
    --
    -- Trim only. NEVER case-fold: casing can be the only thing separating two valid
    -- identifiers, and the pinned collation makes comparison case-sensitive by design.
    ------------------------------------------------------------------------------
    DROP TABLE IF EXISTS #TMP_DeduplicatedStaging;
    CREATE TABLE #TMP_DeduplicatedStaging (
        [id] BIGINT, [OperationType] TINYINT, [Ordinal] BIGINT, [OriginalQueueId] BIGINT,
        [ThirdPartyId] VARCHAR(50) COLLATE SQL_Latin1_General_CP1_CS_AS,
        [IdentifierType] VARCHAR(20) COLLATE SQL_Latin1_General_CP1_CS_AS,
        [GeotabSerialNumber] VARCHAR(50) COLLATE SQL_Latin1_General_CP1_CS_AS,
        [SubPlan] VARCHAR(50), [SourceStatus] VARCHAR(30),
        [EffectiveDateTimeUtc] DATETIME2(7), [TerminationReason] VARCHAR(200),
        [SourceReference] VARCHAR(500)
    );

    INSERT INTO #TMP_DeduplicatedStaging
    SELECT s.[id], s.[OperationType], s.[Ordinal], s.[OriginalQueueId],
           NULLIF(LTRIM(RTRIM(s.[ThirdPartyId])), ''),
           NULLIF(LTRIM(RTRIM(s.[IdentifierType])), ''),
           NULLIF(LTRIM(RTRIM(s.[GeotabSerialNumber])), ''),
           s.[SubPlan], s.[SourceStatus], s.[EffectiveDateTimeUtc],
           s.[TerminationReason], s.[SourceReference]
    FROM [gda].[stg_DeviceSubscriptions] s
    WHERE s.[BatchId] = @BatchId;
    CREATE INDEX IX_TMP_DedupStg_ThirdPartyId ON #TMP_DeduplicatedStaging ([ThirdPartyId]);
    CREATE INDEX IX_TMP_DedupStg_Serial ON #TMP_DeduplicatedStaging ([GeotabSerialNumber]);

    DROP TABLE IF EXISTS #TMP_Rejected;
    CREATE TABLE #TMP_Rejected (
        [id] BIGINT, [OperationType] TINYINT, [Ordinal] BIGINT, [OriginalQueueId] BIGINT,
        [ThirdPartyId] VARCHAR(50) COLLATE SQL_Latin1_General_CP1_CS_AS,
        [IdentifierType] VARCHAR(20) COLLATE SQL_Latin1_General_CP1_CS_AS,
        [GeotabSerialNumber] VARCHAR(50) COLLATE SQL_Latin1_General_CP1_CS_AS,
        [SubPlan] VARCHAR(50), [SourceStatus] VARCHAR(30),
        [EffectiveDateTimeUtc] DATETIME2(7), [TerminationReason] VARCHAR(200),
        [SourceReference] VARCHAR(500), [Reason] NVARCHAR(MAX)
    );

    DROP TABLE IF EXISTS #TMP_Superseded;
    CREATE TABLE #TMP_Superseded (
        [id] BIGINT, [OperationType] TINYINT, [Ordinal] BIGINT, [OriginalQueueId] BIGINT,
        [ThirdPartyId] VARCHAR(50) COLLATE SQL_Latin1_General_CP1_CS_AS,
        [IdentifierType] VARCHAR(20) COLLATE SQL_Latin1_General_CP1_CS_AS,
        [GeotabSerialNumber] VARCHAR(50) COLLATE SQL_Latin1_General_CP1_CS_AS,
        [SubPlan] VARCHAR(50), [SourceStatus] VARCHAR(30),
        [EffectiveDateTimeUtc] DATETIME2(7), [TerminationReason] VARCHAR(200),
        [SourceReference] VARCHAR(500), [Reason] NVARCHAR(MAX)
    );

    ------------------------------------------------------------------------------
    -- Pass 1b: resolve serial-keyed Terminates.
    -- The termination channel is not guaranteed to carry the same identifier the
    -- activation did (it may carry an IMEI where the activation carried a VIN, or no
    -- identifier at all), so GeotabSerialNumber is the only reliable cross-channel
    -- join key. Resolve those rows to their identifier via the Active target row.
    ------------------------------------------------------------------------------
    UPDATE t
    SET t.[ThirdPartyId] = d.[ThirdPartyId],
        t.[IdentifierType] = ISNULL(t.[IdentifierType], d.[IdentifierType])
    FROM #TMP_DeduplicatedStaging t
    INNER JOIN [gda].[DeviceSubscriptions] d
        ON d.[GeotabSerialNumber] = t.[GeotabSerialNumber]
       AND d.[SubscriptionStatus] = 1
    WHERE t.[ThirdPartyId] IS NULL
      AND t.[GeotabSerialNumber] IS NOT NULL;

    ------------------------------------------------------------------------------
    -- Pass 1c: dedupe. The main adapter's merge procedures de-duplicate FIRST, and
    -- for good reason: two rows for one key make MERGE fail with error 8672 and
    -- abort the whole operation. Highest Ordinal wins, id as tiebreaker. Losers are
    -- retained as Superseded so the audit trail keeps them and their SourceReferences.
    ------------------------------------------------------------------------------
    ;WITH ranked AS (
        SELECT t.*, ROW_NUMBER() OVER (PARTITION BY t.[ThirdPartyId]
                                       ORDER BY t.[Ordinal] DESC, t.[id] DESC) AS rn
        FROM #TMP_DeduplicatedStaging t WHERE t.[ThirdPartyId] IS NOT NULL
    )
    INSERT INTO #TMP_Superseded
    SELECT r.[id], r.[OperationType], r.[Ordinal], r.[OriginalQueueId], r.[ThirdPartyId],
           r.[IdentifierType], r.[GeotabSerialNumber], r.[SubPlan], r.[SourceStatus],
           r.[EffectiveDateTimeUtc], r.[TerminationReason], r.[SourceReference],
           'Superseded within the same batch by a higher-ordinal instruction for the same identifier.'
    FROM ranked r WHERE r.rn > 1;

    DELETE t FROM #TMP_DeduplicatedStaging t INNER JOIN #TMP_Superseded s ON t.[id] = s.[id];

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
    ;WITH ranked AS (
        SELECT t.*, ROW_NUMBER() OVER (PARTITION BY t.[GeotabSerialNumber]
                                       ORDER BY t.[Ordinal] DESC, t.[id] DESC) AS rn
        FROM #TMP_DeduplicatedStaging t WHERE t.[GeotabSerialNumber] IS NOT NULL
    )
    INSERT INTO #TMP_Superseded
    SELECT r.[id], r.[OperationType], r.[Ordinal], r.[OriginalQueueId], r.[ThirdPartyId],
           r.[IdentifierType], r.[GeotabSerialNumber], r.[SubPlan], r.[SourceStatus],
           r.[EffectiveDateTimeUtc], r.[TerminationReason], r.[SourceReference],
           'Superseded within the same batch by a higher-ordinal instruction for the same serial number.'
    FROM ranked r WHERE r.rn > 1 AND r.[OperationType] = 1;

    DELETE t FROM #TMP_DeduplicatedStaging t INNER JOIN #TMP_Superseded s ON t.[id] = s.[id];

    ------------------------------------------------------------------------------
    -- Pass 2: validation. This lives in the merge rather than in the service so that
    -- the delta intake and any polling intake cannot disagree on identical data.
    ------------------------------------------------------------------------------

    -- Snapshot mode must NOT ignore status. A device returned as anything other than
    -- Subscribed must not be activated - that is over-transmission, which the brake
    -- by construction cannot catch because it counts terminations.
    IF @Mode = 1
    BEGIN
        INSERT INTO #TMP_Rejected
        SELECT t.*, CONCAT('Snapshot row carries SourceStatus ', ISNULL(t.[SourceStatus], '(null)'),
                           '; only Subscribed rows may be activated.')
        FROM #TMP_DeduplicatedStaging t
        WHERE ISNULL(t.[SourceStatus], 'Subscribed') <> 'Subscribed';
        DELETE t FROM #TMP_DeduplicatedStaging t INNER JOIN #TMP_Rejected r ON t.[id] = r.[id];
    END

    -- Subscribe with no serial number: nothing to transmit as.
    INSERT INTO #TMP_Rejected
    SELECT t.*, 'Subscribe instruction has no GeotabSerialNumber.'
    FROM #TMP_DeduplicatedStaging t WHERE t.[OperationType] = 1 AND t.[GeotabSerialNumber] IS NULL;
    DELETE t FROM #TMP_DeduplicatedStaging t INNER JOIN #TMP_Rejected r ON t.[id] = r.[id];

    -- Subscribe with no identifier and no resolvable one.
    INSERT INTO #TMP_Rejected
    SELECT t.*, 'Subscribe instruction has no ThirdPartyId.'
    FROM #TMP_DeduplicatedStaging t WHERE t.[OperationType] = 1 AND t.[ThirdPartyId] IS NULL;
    DELETE t FROM #TMP_DeduplicatedStaging t INNER JOIN #TMP_Rejected r ON t.[id] = r.[id];

    -- Identifier type outside the allow-list: fail closed, reconcile nothing.
    --
    -- An allow-list that resolves to NO entries means "not enforced", which is what
    -- the EXISTS test is for, and it mirrors the PostgreSQL function's
    -- v_allowed_types IS NOT NULL. Without it NOT IN over an empty set is true for
    -- every row, so a blank setting rejected every typed row here while disabling
    -- the check on PostgreSQL - the same configuration behaving oppositely on the
    -- two providers. Rejecting everything is not a coherent posture in any case: it
    -- would still accept every untyped row.
    --
    -- The allow-list is deliberately NOT materialised into a table variable. Doing
    -- so gives its column this database's default collation, which then conflicts
    -- with the CS_AS-pinned [IdentifierType] above (error 468) on any database whose
    -- default collation differs. Inline, STRING_SPLIT's collation is coercible and
    -- yields to the pinned column, so this works whatever the database collation is.
    INSERT INTO #TMP_Rejected
    SELECT t.*, CONCAT('IdentifierType ', t.[IdentifierType],
                       ' is not in the configured allow-list (', @ExpectedIdentifierTypes, ').')
    FROM #TMP_DeduplicatedStaging t
    WHERE t.[IdentifierType] IS NOT NULL
      AND EXISTS (
            SELECT 1 FROM STRING_SPLIT(ISNULL(@ExpectedIdentifierTypes, 'Vin'), ',')
            WHERE LTRIM(RTRIM(value)) <> '')
      AND UPPER(t.[IdentifierType]) NOT IN (
            SELECT UPPER(LTRIM(RTRIM(value))) FROM STRING_SPLIT(ISNULL(@ExpectedIdentifierTypes, 'Vin'), ',')
            WHERE LTRIM(RTRIM(value)) <> '');
    DELETE t FROM #TMP_DeduplicatedStaging t INNER JOIN #TMP_Rejected r ON t.[id] = r.[id];

    -- Serial number already belongs to a DIFFERENT ACTIVE identifier. Scoped to
    -- Active rows to match the filtered unique index; an unscoped test would reject
    -- the legitimate terminate-then-reprovision re-pairing.
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
    --    @Mode = 0                      mirrors pass 6, which applies staged
    --                                   Terminates in delta mode only.
    -- If any of those three changes, this must change with it.
    --
    -- SNAPSHOT MODE IS KNOWINGLY NOT COVERED, and the @Mode test is what confines
    -- this exemption to the case it was reasoned about. Snapshot mode clears a
    -- stored row by ABSENCE from the batch rather than by a staged Terminate, so
    -- the same repair shape is still rejected there: a fleet re-keyed to a new
    -- identifier type - the case this procedure's own header names - would have
    -- every Subscribe rejected against the row the snapshot was about to
    -- terminate. Nothing invokes snapshot mode today; DeviceSubscriptionService
    -- merges in delta mode only. Phase E owes a SECOND exemption mirroring pass 6's
    -- snapshot predicate the way this one mirrors pass 3, and must not simply widen
    -- this one, whose other two predicates are meaningless for an absent row.
    --
    -- Both sides of the two identifier comparisons below are CS_AS-pinned (the temp
    -- table declares the collation, the base table pins it per column), so this adds
    -- no coercion and cannot raise error 468 on a CI_AS database.
    INSERT INTO #TMP_Rejected
    SELECT t.*, CONCAT('GeotabSerialNumber ', t.[GeotabSerialNumber],
                       ' is already assigned to active subscription ', d.[ThirdPartyId], '.')
    FROM #TMP_DeduplicatedStaging t
    INNER JOIN [gda].[DeviceSubscriptions] d
        ON d.[GeotabSerialNumber] = t.[GeotabSerialNumber]
       AND d.[SubscriptionStatus] = 1
       AND d.[ThirdPartyId] <> t.[ThirdPartyId]
    WHERE t.[OperationType] = 1
      AND NOT EXISTS (
            SELECT 1
            FROM #TMP_DeduplicatedStaging t2
            WHERE @Mode = 0
              AND t2.[OperationType] = 0
              AND t2.[ThirdPartyId] = d.[ThirdPartyId]
              AND t2.[Ordinal] > d.[LastAppliedOrdinal]
              AND (t2.[GeotabSerialNumber] IS NULL
                   OR t2.[GeotabSerialNumber] = d.[GeotabSerialNumber]));
    DELETE t FROM #TMP_DeduplicatedStaging t INNER JOIN #TMP_Rejected r ON t.[id] = r.[id];

    -- Terminate whose supplied serial number contradicts the stored pairing.
    INSERT INTO #TMP_Rejected
    SELECT t.*, CONCAT('Terminate instruction supplies GeotabSerialNumber ', t.[GeotabSerialNumber],
                       ' but the stored subscription holds ', d.[GeotabSerialNumber], '.')
    FROM #TMP_DeduplicatedStaging t
    INNER JOIN [gda].[DeviceSubscriptions] d ON d.[ThirdPartyId] = t.[ThirdPartyId]
    WHERE t.[OperationType] = 0
      AND t.[GeotabSerialNumber] IS NOT NULL
      AND d.[GeotabSerialNumber] <> t.[GeotabSerialNumber];
    DELETE t FROM #TMP_DeduplicatedStaging t INNER JOIN #TMP_Rejected r ON t.[id] = r.[id];

    ------------------------------------------------------------------------------
    -- Pass 3: order filter. Discard anything at or below the target row's high-water
    -- mark. This is what makes a replayed or out-of-order instruction a no-op instead
    -- of a state change, and it is what stops a stale snapshot from terminating a
    -- device a fresher delta just subscribed.
    ------------------------------------------------------------------------------
    INSERT INTO #TMP_Superseded
    SELECT t.*, CONCAT('Ordinal ', t.[Ordinal], ' is at or below the already-applied ordinal ',
                       d.[LastAppliedOrdinal], ' for this subscription.')
    FROM #TMP_DeduplicatedStaging t
    INNER JOIN [gda].[DeviceSubscriptions] d ON d.[ThirdPartyId] = t.[ThirdPartyId]
    WHERE t.[Ordinal] <= d.[LastAppliedOrdinal];

    DELETE t FROM #TMP_DeduplicatedStaging t INNER JOIN #TMP_Superseded s ON t.[id] = s.[id];

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
    --
    -- GREATEST needs SQL Server 2022, so the two maxima are combined through a UNION
    -- ALL. Both sides of each comparison are CS_AS-pinned (the temp table declares
    -- the collation, the history table pins it per column), so this adds no coercion
    -- and cannot raise error 468 on a CI_AS database.
    ------------------------------------------------------------------------------
    INSERT INTO #TMP_Superseded
    SELECT t.*, CONCAT('Ordinal ', t.[Ordinal], ' is at or below the ordinal ', h.[TerminateOrdinal],
                       ' of a Terminate already merged for this identifier or serial number.')
    FROM #TMP_DeduplicatedStaging t
    CROSS APPLY (
        SELECT MAX(x.[Ordinal]) AS [TerminateOrdinal]
        FROM (
            SELECT hi.[Ordinal]
            FROM [gda].[DeviceSubscriptionHistory] hi
            WHERE hi.[ThirdPartyId] = t.[ThirdPartyId]
              AND hi.[OperationType] = 0 AND hi.[Outcome] IN (0, 1)
            UNION ALL
            SELECT hs.[Ordinal]
            FROM [gda].[DeviceSubscriptionHistory] hs
            WHERE hs.[GeotabSerialNumber] = t.[GeotabSerialNumber]
              AND hs.[OperationType] = 0 AND hs.[Outcome] IN (0, 1)
              AND (hs.[Outcome] = 0 OR hs.[ThirdPartyId] IS NULL)
        ) x
    ) h
    WHERE t.[OperationType] = 1
      AND t.[Ordinal] <= h.[TerminateOrdinal];

    DELETE t FROM #TMP_DeduplicatedStaging t INNER JOIN #TMP_Superseded s ON t.[id] = s.[id];

    ------------------------------------------------------------------------------
    -- Pass 4: the brake, evaluated BEFORE anything is applied.
    --
    -- A rolling window rather than a per-call cap: a per-call cap is defeated by
    -- BatchSize chunking (cap 50, batch size 100, four batches terminate 200).
    -- Active in BOTH modes - delta is the mode that ships first, and it is the one
    -- fed by hand-written INSERT statements.
    --
    -- On trip nothing is applied and the caller persists the guard state. Evaluating
    -- first and applying nothing removes any need for a rollback, and keeps this
    -- procedure's control flow identical to the PostgreSQL counterpart.
    ------------------------------------------------------------------------------
    IF @Mode = 0
    BEGIN
        SELECT @ProposedTerminations = COUNT(*)
        FROM #TMP_DeduplicatedStaging t
        INNER JOIN [gda].[DeviceSubscriptions] d ON d.[ThirdPartyId] = t.[ThirdPartyId]
        WHERE t.[OperationType] = 0 AND d.[SubscriptionStatus] = 1;
    END
    ELSE
    BEGIN
        SELECT @ProposedTerminations = COUNT(*)
        FROM [gda].[DeviceSubscriptions] d
        WHERE d.[SubscriptionStatus] = 1
          AND d.[LastAppliedOrdinal] < @SnapshotOrdinal
          AND NOT EXISTS (SELECT 1 FROM #TMP_DeduplicatedStaging t WHERE t.[ThirdPartyId] = d.[ThirdPartyId]);
    END

    IF @MaxTerminationsPerWindow IS NOT NULL AND @ProposedTerminations > 0
    BEGIN
        SELECT @WindowTerminations = COUNT(*)
        FROM [gda].[DeviceSubscriptionHistory]
        WHERE [NewStatus] = 0
          AND [AppliedDateTimeUtc] >= DATEADD(MINUTE, -@TerminationWindowMinutes, @Now);

        IF (@WindowTerminations + @ProposedTerminations) > @MaxTerminationsPerWindow
        BEGIN
            -- QueueId carries the rolling-window count and QueueOrdinal the proposed
            -- count. Neither has a queue row to refer to on a refusal, and the caller
            -- needs both numbers to record the trip. Keep in step with PostgreSQL.
            SELECT CAST(2 AS TINYINT) AS [ResultCode], CAST(NULL AS BIGINT) AS [StagingId],
                   CAST(@WindowTerminations AS BIGINT) AS [QueueId],
                   CAST(@ProposedTerminations AS BIGINT) AS [QueueOrdinal],
                   CAST(NULL AS VARCHAR(50)) AS [Identifier], CAST(NULL AS VARCHAR(50)) AS [SerialNumber],
                   CAST(CONCAT('Termination brake: this batch would terminate ', @ProposedTerminations,
                        ' subscription(s); ', @WindowTerminations, ' already terminated in the last ',
                        @TerminationWindowMinutes, ' minute(s); configured maximum is ',
                        @MaxTerminationsPerWindow, '. Nothing applied.') AS NVARCHAR(MAX)) AS [ResultMessage];

            -- Drop temporary tables.
            DROP TABLE #TMP_DeduplicatedStaging;
            DROP TABLE #TMP_Rejected;
            DROP TABLE #TMP_Superseded;
            RETURN;
        END
    END

    ------------------------------------------------------------------------------
    -- Pass 5: capture the pre-image BEFORE any mutation.
    -- MERGE ... OUTPUT deleted.* could supply this on SQL Server, but PostgreSQL's
    -- ON CONFLICT ... RETURNING yields the NEW tuple only. Both providers therefore
    -- capture into a temp table so the two procedures keep the same control flow.
    ------------------------------------------------------------------------------
    DROP TABLE IF EXISTS #TMP_PreImage;
    CREATE TABLE #TMP_PreImage (
        [ThirdPartyId] VARCHAR(50) COLLATE SQL_Latin1_General_CP1_CS_AS,
        [SubscriptionStatus] TINYINT,
        [SubPlan] VARCHAR(50),
        [GeotabSerialNumber] VARCHAR(50) COLLATE SQL_Latin1_General_CP1_CS_AS
    );

    INSERT INTO #TMP_PreImage
    SELECT d.[ThirdPartyId], d.[SubscriptionStatus], d.[SubPlan], d.[GeotabSerialNumber]
    FROM [gda].[DeviceSubscriptions] d
    WHERE d.[ThirdPartyId] IN (SELECT t.[ThirdPartyId] FROM #TMP_DeduplicatedStaging t WHERE t.[ThirdPartyId] IS NOT NULL)
       OR (@Mode = 1 AND d.[SubscriptionStatus] = 1);
    CREATE INDEX IX_TMP_PreImage_ThirdPartyId ON #TMP_PreImage ([ThirdPartyId]);

    ------------------------------------------------------------------------------
    -- Pass 6: TERMINATION PASS - runs BEFORE the upsert pass.
    -- Required by the filtered unique serial-number index: a serial number being
    -- re-paired in the same batch is still rejected by the index if the upsert runs
    -- first.
    ------------------------------------------------------------------------------
    IF @Mode = 0
    BEGIN
        UPDATE d
        SET d.[SubscriptionStatus] = 0,
            d.[TerminatedDateTimeUtc] = @Now,
            d.[TerminationReason] = ISNULL(t.[TerminationReason], d.[TerminationReason]),
            d.[LastAppliedOrdinal] = t.[Ordinal],
            d.[RecordLastChangedUtc] = @Now
        FROM [gda].[DeviceSubscriptions] d
        INNER JOIN #TMP_DeduplicatedStaging t ON d.[ThirdPartyId] = t.[ThirdPartyId]
        WHERE t.[OperationType] = 0 AND d.[SubscriptionStatus] = 1;
        SET @TerminatedCount = @@ROWCOUNT;
    END
    ELSE
    BEGIN
        -- Snapshot: terminate every Active row absent from the batch whose high-water
        -- mark predates this snapshot. The LastAppliedOrdinal comparison is the
        -- ordering predicate; it is inert until a polling service exists, because
        -- nothing else invokes snapshot mode.
        UPDATE d
        SET d.[SubscriptionStatus] = 0,
            d.[TerminatedDateTimeUtc] = @Now,
            d.[TerminationReason] = 'Absent from subscription snapshot.',
            d.[LastAppliedOrdinal] = @SnapshotOrdinal,
            d.[RecordLastChangedUtc] = @Now
        FROM [gda].[DeviceSubscriptions] d
        WHERE d.[SubscriptionStatus] = 1
          AND d.[LastAppliedOrdinal] < @SnapshotOrdinal
          AND NOT EXISTS (SELECT 1 FROM #TMP_DeduplicatedStaging t WHERE t.[ThirdPartyId] = d.[ThirdPartyId]);
        SET @TerminatedCount = @@ROWCOUNT;
    END

    ------------------------------------------------------------------------------
    -- Pass 7: UPSERT PASS.
    -- WITH (HOLDLOCK) is required on the MERGE target: without it, concurrent merges
    -- can both miss on the same key and race to INSERT.
    ------------------------------------------------------------------------------
    MERGE INTO [gda].[DeviceSubscriptions] WITH (HOLDLOCK) AS d
    USING (SELECT * FROM #TMP_DeduplicatedStaging WHERE [OperationType] = 1) AS t
        ON d.[ThirdPartyId] = t.[ThirdPartyId]
    WHEN MATCHED THEN UPDATE SET
        d.[IdentifierType]        = ISNULL(t.[IdentifierType], 'Vin'),
        d.[GeotabSerialNumber]    = t.[GeotabSerialNumber],
        d.[SubscriptionStatus]    = 1,
        d.[SubPlan]               = ISNULL(t.[SubPlan], d.[SubPlan]),
        d.[LastAppliedOrdinal]    = t.[Ordinal],
        d.[SubscribedDateTimeUtc] = ISNULL(d.[SubscribedDateTimeUtc], @Now),
        d.[LastSeenInSnapshotUtc] = CASE WHEN @Mode = 1 THEN @Now ELSE d.[LastSeenInSnapshotUtc] END,
        d.[RecordLastChangedUtc]  = @Now
    WHEN NOT MATCHED BY TARGET THEN INSERT (
        [ThirdPartyId], [IdentifierType], [GeotabSerialNumber], [SubscriptionStatus],
        [SubPlan], [LastAppliedOrdinal], [SubscribedDateTimeUtc], [LastSeenInSnapshotUtc],
        [RecordCreationTimeUtc], [RecordLastChangedUtc])
    VALUES (
        t.[ThirdPartyId], ISNULL(t.[IdentifierType], 'Vin'), t.[GeotabSerialNumber], 1,
        t.[SubPlan], t.[Ordinal], @Now,
        CASE WHEN @Mode = 1 THEN @Now ELSE NULL END, @Now, @Now);
    SET @SubscribedCount = @@ROWCOUNT;

    ------------------------------------------------------------------------------
    -- Pass 8: history, ONE ROW PER APPLIED INSTRUCTION.
    -- Transition-only history loses two things it exists to record: superseded
    -- instructions (there would be no record that a termination was ever sent) and
    -- plan changes (which alter no status and so would write nothing).
    ------------------------------------------------------------------------------
    -- An applied Terminate records the serial number it cleared, taken from the
    -- pre-image when the instruction named only an identifier, as the
    -- snapshot-absent rows below record theirs. Pass 3b reads it: without it, an
    -- older Subscribe pairing that serial number with a different identifier, merged
    -- after the Terminate, found nothing to stop it. A NoOp Terminate keeps the
    -- serial number it named, if any.
    INSERT INTO [gda].[DeviceSubscriptionHistory] (
        [ThirdPartyId], [IdentifierType], [GeotabSerialNumber], [SubPlan], [SourceStatus],
        [OperationType], [Outcome], [PreviousStatus], [NewStatus], [Ordinal],
        [EffectiveDateTimeUtc], [TerminationReason], [AppliedDateTimeUtc], [SourceReference])
    SELECT t.[ThirdPartyId], t.[IdentifierType],
           CASE WHEN t.[OperationType] = 0 AND p.[SubscriptionStatus] = 1
                THEN ISNULL(t.[GeotabSerialNumber], p.[GeotabSerialNumber])
                ELSE t.[GeotabSerialNumber] END,
           t.[SubPlan], t.[SourceStatus],
           t.[OperationType],
           CASE WHEN t.[OperationType] = 0 AND ISNULL(p.[SubscriptionStatus], 0) <> 1
                THEN CAST(1 AS TINYINT) ELSE CAST(0 AS TINYINT) END,
           p.[SubscriptionStatus],
           CASE WHEN t.[OperationType] = 0 AND ISNULL(p.[SubscriptionStatus], 0) <> 1 THEN NULL
                WHEN t.[OperationType] = 0 THEN CAST(0 AS TINYINT)
                ELSE CAST(1 AS TINYINT) END,
           t.[Ordinal], t.[EffectiveDateTimeUtc], t.[TerminationReason], @Now, t.[SourceReference]
    FROM #TMP_DeduplicatedStaging t
    LEFT JOIN #TMP_PreImage p ON p.[ThirdPartyId] = t.[ThirdPartyId];

    -- Snapshot-absent terminations have no staged instruction of their own.
    IF @Mode = 1 AND @TerminatedCount > 0
    BEGIN
        INSERT INTO [gda].[DeviceSubscriptionHistory] (
            [ThirdPartyId], [IdentifierType], [GeotabSerialNumber], [SubPlan],
            [OperationType], [Outcome], [PreviousStatus], [NewStatus], [Ordinal],
            [TerminationReason], [AppliedDateTimeUtc], [SourceReference])
        SELECT d.[ThirdPartyId], d.[IdentifierType], d.[GeotabSerialNumber], d.[SubPlan],
               CAST(0 AS TINYINT), CAST(0 AS TINYINT), CAST(1 AS TINYINT), CAST(0 AS TINYINT),
               @SnapshotOrdinal, d.[TerminationReason], @Now, 'snapshot:absent'
        FROM [gda].[DeviceSubscriptions] d
        WHERE d.[SubscriptionStatus] = 0
          AND d.[LastAppliedOrdinal] = @SnapshotOrdinal
          AND d.[TerminatedDateTimeUtc] = @Now;
    END

    -- Superseded and rejected instructions are recorded too, so the audit trail keeps
    -- their SourceReferences after the queue rows are deleted.
    INSERT INTO [gda].[DeviceSubscriptionHistory] (
        [ThirdPartyId], [IdentifierType], [GeotabSerialNumber], [SubPlan], [SourceStatus],
        [OperationType], [Outcome], [PreviousStatus], [NewStatus], [Ordinal],
        [EffectiveDateTimeUtc], [TerminationReason], [AppliedDateTimeUtc], [SourceReference])
    SELECT x.[ThirdPartyId], x.[IdentifierType], x.[GeotabSerialNumber], x.[SubPlan], x.[SourceStatus],
           x.[OperationType], x.[Outcome], NULL, NULL, x.[Ordinal],
           x.[EffectiveDateTimeUtc], x.[TerminationReason], @Now, x.[SourceReference]
    FROM (
        SELECT s.[ThirdPartyId], s.[IdentifierType], s.[GeotabSerialNumber], s.[SubPlan],
               s.[SourceStatus], s.[OperationType], CAST(2 AS TINYINT) AS [Outcome], s.[Ordinal],
               s.[EffectiveDateTimeUtc], s.[TerminationReason], s.[SourceReference]
        FROM #TMP_Superseded s
        UNION ALL
        SELECT r.[ThirdPartyId], r.[IdentifierType], r.[GeotabSerialNumber], r.[SubPlan],
               r.[SourceStatus], r.[OperationType], CAST(3 AS TINYINT) AS [Outcome], r.[Ordinal],
               r.[EffectiveDateTimeUtc], r.[TerminationReason], r.[SourceReference]
        FROM #TMP_Rejected r
    ) x;

    ------------------------------------------------------------------------------
    -- Pass 9: clear THIS batch's staging rows. Never TRUNCATE - a concurrent writer
    -- may hold rows under a different BatchId.
    ------------------------------------------------------------------------------
    DELETE FROM [gda].[stg_DeviceSubscriptions] WHERE [BatchId] = @BatchId;

    SELECT @RejectedCount = COUNT(*) FROM #TMP_Rejected;

    -- Rejected rows go back to the caller, which writes them to
    -- Q_DeviceSubscriptionsFail. The summary row is appended last.
    SELECT CAST(1 AS TINYINT) AS [ResultCode], r.[id] AS [StagingId],
           r.[OriginalQueueId] AS [QueueId], r.[Ordinal] AS [QueueOrdinal],
           r.[ThirdPartyId] AS [Identifier], r.[GeotabSerialNumber] AS [SerialNumber], r.[Reason] AS [ResultMessage]
    FROM #TMP_Rejected r
    UNION ALL
    SELECT CAST(0 AS TINYINT), CAST(NULL AS BIGINT), CAST(NULL AS BIGINT), CAST(NULL AS BIGINT),
           CAST(NULL AS VARCHAR(50)), CAST(NULL AS VARCHAR(50)),
           CAST(CONCAT('Merge completed. Subscribed/updated: ', @SubscribedCount,
                '; terminated: ', @TerminatedCount,
                '; rejected: ', @RejectedCount, '.') AS NVARCHAR(MAX));

    -- Drop temporary tables.
    DROP TABLE #TMP_DeduplicatedStaging;
    DROP TABLE #TMP_Rejected;
    DROP TABLE #TMP_Superseded;
    DROP TABLE #TMP_PreImage;
END
GO

GRANT EXECUTE ON [gda].[spMerge_stg_DeviceSubscriptions] TO [geotabdigadapter_client];
GO
/*** [END] OEM Subscription Mode ***/


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Table: DigPartitionInfo
--
-- Records the arguments spManageDigPartitions was first run with. Both are
-- immutable afterwards: changing the interval on a populated database would
-- leave two incompatible partition layouts side by side, and moving the minimum
-- date earlier would ask for boundaries below ones that already exist, which
-- splits an interior partition and moves data. The row is written by
-- spManageDigPartitions on its first execution, not by this script, so the
-- interval comes from configuration rather than from whenever the database
-- happened to be installed.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [gda].[DigPartitionInfo](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[InitialMinDateTimeUtc] [datetime2](7) NOT NULL,
	[InitialPartitionInterval] [nvarchar](50) NOT NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DigPartitionInfo] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spManageDigPartitions
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--   Creates the period partitions the partitioned gda tables need, through until
--   the end of the next calendar month, by splitting
--   DateTimePartitionFunction_GeotabDigAdapter forward. The
--   DatabaseMaintenanceService runs it periodically so that a partition always
--   exists before there is data for it.
--
--   Modelled on the MyGeotab API Adapter's [dbo].[spManagePartitions], with two
--   deliberate divergences:
--
--     1. No FileGroups or data files. The DIG partition scheme is mapped
--        ALL TO ([PRIMARY]), which is what lets a DIG-only deployment run on
--        Azure SQL Database and keeps these tables out of the API Adapter's
--        FileGroup sweep when the two share a database.
--     2. Creation resumes above the HIGHEST existing boundary rather than one day
--        after it. The function ships with a fixed 2000-01-01 sentinel boundary,
--        so resuming from "sentinel + one day" would try to create a boundary for
--        every period between 2000 and today.
--
--   ALTER PARTITION SCHEME ... NEXT USED is issued before EVERY split. Creating a
--   scheme with ALL TO ([PRIMARY]) marks PRIMARY as the next used FileGroup
--   exactly ONCE: the first SPLIT consumes it, and the second fails with
--   "does not have any next used filegroup" without this.
--
--   SQL Server redistributes rows as part of a SPLIT, so unlike PostgreSQL this
--   needs no recovery for records that pre-date the first period partition -
--   provided the first run's @MinDateTimeUtc is early enough to cover them.
--
-- Parameters:
--   @MinDateTimeUtc:    Earliest date to create partitions from, on first run.
--                       Pinned in DigPartitionInfo thereafter and may not move
--                       earlier. It should be no later than the oldest existing
--                       RecordCreationTimeUtc, or those rows stay in the
--                       open-ended leading partition and are reclaimed only as a
--                       single block.
--   @PartitionInterval: 'monthly', 'weekly' or 'daily'. Pinned on first run.
-- ==========================================================================================
CREATE PROCEDURE [gda].[spManageDigPartitions]
    @MinDateTimeUtc DATETIME2(7),
    @PartitionInterval NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    -- ISO weeks, so that a 'weekly' interval produces the same boundaries here as
    -- date_trunc('week', ...) produces on PostgreSQL.
    SET DATEFIRST 1;

    -- A guard against a mistyped @MinDateTimeUtc, not a tuning knob.
    DECLARE @maxPeriodsPerTable INT = 2000;

    DECLARE @partitionFunctionName NVARCHAR(128) = 'DateTimePartitionFunction_GeotabDigAdapter';
    DECLARE @partitionSchemeName   NVARCHAR(128) = 'DateTimePartitionScheme_GeotabDigAdapter';

    DECLARE @initialMinDateTimeUtc DATETIME2(7);
    DECLARE @initialPartitionInterval NVARCHAR(50);
    DECLARE @minDateUtc DATE = CONVERT(DATE, @MinDateTimeUtc);
    DECLARE @maxDateUtc DATE = EOMONTH(GETUTCDATE(), 1);
    DECLARE @minDateUtcString NVARCHAR(23) = CONVERT(NVARCHAR(23), @minDateUtc, 121);
    DECLARE @maxDateUtcString NVARCHAR(23) = CONVERT(NVARCHAR(23), @maxDateUtc, 121);
    DECLARE @latestBoundary DATETIME2(7);
    DECLARE @currentDateUtc DATE;
    DECLARE @periodCount INT;
    DECLARE @created INT = 0;
    DECLARE @sql NVARCHAR(MAX);

    DECLARE @storedProcedureName NVARCHAR(128) = OBJECT_NAME(@@PROCID);
    DECLARE @storedProcedureStartTime DATETIME = GETDATE();
    DECLARE @startTime DATETIME = GETDATE();
    DECLARE @endTime DATETIME;
    DECLARE @durationString VARCHAR(30);

    BEGIN TRY
        -- ==================================================================================
        RAISERROR ('Executing stored procedure ''%s''. @MinDateTimeUtc: %s; @maxDateUtc: %s', 0, 1, @storedProcedureName, @minDateUtcString, @maxDateUtcString) WITH NOWAIT;

        -- ==================================================================================
        -- STEP 1: Validate input parameter values.
        RAISERROR ('Step 1 [Validating input parameter values]...', 0, 1) WITH NOWAIT;

        IF @PartitionInterval NOT IN ('daily', 'weekly', 'monthly')
        BEGIN
            RAISERROR('ERROR: Invalid value for @PartitionInterval. Allowed values: monthly, weekly, daily.', 16, 1);
            RETURN;
        END

        IF @minDateUtc > @maxDateUtc
        BEGIN
            RAISERROR('ERROR: The @MinDateTimeUtc cannot be greater than @maxDateUtc. [@MinDateTimeUtc: %s; @maxDateUtc: %s]', 16, 1, @minDateUtcString, @maxDateUtcString);
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = @partitionFunctionName)
        BEGIN
            RAISERROR('ERROR: Partition function ''%s'' does not exist. It is created by the schema creation/upgrade script.', 16, 1, @partitionFunctionName);
            RETURN;
        END

        -- Pin the arguments on the first run; enforce them on every run thereafter.
        IF EXISTS (SELECT 1 FROM [gda].[DigPartitionInfo])
        BEGIN
            SELECT TOP(1) @initialMinDateTimeUtc = [InitialMinDateTimeUtc],
                          @initialPartitionInterval = [InitialPartitionInterval]
            FROM [gda].[DigPartitionInfo]
            ORDER BY [id];

            IF @minDateUtc < CONVERT(DATE, @initialMinDateTimeUtc)
            BEGIN
                DECLARE @initialMinDateTimeUtcString NVARCHAR(23) = CONVERT(NVARCHAR(23), @initialMinDateTimeUtc, 121);
                RAISERROR('ERROR: The @MinDateTimeUtc cannot be earlier than the value recorded in [gda].[DigPartitionInfo]. [@MinDateTimeUtc: %s; InitialMinDateTimeUtc: %s]', 16, 1, @minDateUtcString, @initialMinDateTimeUtcString);
                RETURN;
            END

            IF @PartitionInterval <> @initialPartitionInterval
            BEGIN
                RAISERROR('ERROR: The @PartitionInterval cannot differ from the value recorded in [gda].[DigPartitionInfo]. [@PartitionInterval: %s; InitialPartitionInterval: %s]', 16, 1, @PartitionInterval, @initialPartitionInterval);
                RETURN;
            END
        END
        ELSE
        BEGIN
            INSERT INTO [gda].[DigPartitionInfo] ([InitialMinDateTimeUtc], [InitialPartitionInterval], [RecordCreationTimeUtc])
            VALUES (@minDateUtc, @PartitionInterval, SYSUTCDATETIME());
        END

        -- Align the start of the series to the period.
        SET @currentDateUtc =
            CASE @PartitionInterval
                WHEN 'monthly' THEN DATEFROMPARTS(YEAR(@minDateUtc), MONTH(@minDateUtc), 1)
                WHEN 'weekly'  THEN DATEADD(DAY, 1 - DATEPART(WEEKDAY, @minDateUtc), @minDateUtc)
                ELSE @minDateUtc
            END;

        -- Resume above the highest existing boundary. Splitting at or below it
        -- would either duplicate a boundary or split an interior partition and
        -- move data.
        SELECT @latestBoundary = MAX(CONVERT(DATETIME2(7), prv.value))
        FROM sys.partition_range_values prv
        JOIN sys.partition_functions pf ON pf.function_id = prv.function_id
        WHERE pf.name = @partitionFunctionName;

        IF @latestBoundary IS NOT NULL AND @currentDateUtc <= CONVERT(DATE, @latestBoundary)
        BEGIN
            SET @currentDateUtc =
                CASE @PartitionInterval
                    WHEN 'monthly' THEN DATEADD(MONTH, 1, CONVERT(DATE, @latestBoundary))
                    WHEN 'weekly'  THEN DATEADD(WEEK,  1, CONVERT(DATE, @latestBoundary))
                    ELSE                DATEADD(DAY,   1, CONVERT(DATE, @latestBoundary))
                END;
        END

        SET @periodCount =
            CASE @PartitionInterval
                WHEN 'monthly' THEN DATEDIFF(MONTH, @currentDateUtc, @maxDateUtc) + 1
                WHEN 'weekly'  THEN DATEDIFF(WEEK,  @currentDateUtc, @maxDateUtc) + 1
                ELSE                DATEDIFF(DAY,   @currentDateUtc, @maxDateUtc) + 1
            END;

        IF @periodCount > @maxPeriodsPerTable
        BEGIN
            DECLARE @periodCountString NVARCHAR(20) = CONVERT(NVARCHAR(20), @periodCount);
            DECLARE @maxPeriodsString NVARCHAR(20) = CONVERT(NVARCHAR(20), @maxPeriodsPerTable);
            RAISERROR('ERROR: @MinDateTimeUtc implies %s periods, above the safety limit of %s. Supply a later @MinDateTimeUtc or a longer @PartitionInterval. [@MinDateTimeUtc: %s; @PartitionInterval: %s]', 16, 1, @periodCountString, @maxPeriodsString, @minDateUtcString, @PartitionInterval);
            RETURN;
        END

        SET @endTime = GETDATE();
        SET @durationString = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @startTime, @endTime));
        RAISERROR ('Step 1 completed. Duration: %s milliseconds', 0, 1, @durationString) WITH NOWAIT;
        SET @startTime = @endTime;


        -- ==================================================================================
        -- STEP 2: Create the missing boundaries.
        RAISERROR ('Step 2 [Creating partitions]...', 0, 1) WITH NOWAIT;

        WHILE @currentDateUtc <= @maxDateUtc
        BEGIN
            IF NOT EXISTS (
                SELECT 1
                FROM sys.partition_range_values prv
                JOIN sys.partition_functions pf ON pf.function_id = prv.function_id
                WHERE pf.name = @partitionFunctionName
                  AND CONVERT(DATETIME2(7), prv.value) = CONVERT(DATETIME2(7), @currentDateUtc))
            BEGIN
                -- Required before EVERY split - see the note in the header.
                SET @sql = N'ALTER PARTITION SCHEME ' + QUOTENAME(@partitionSchemeName) + N' NEXT USED [PRIMARY];';
                EXEC sp_executesql @sql;

                SET @sql = N'ALTER PARTITION FUNCTION ' + QUOTENAME(@partitionFunctionName)
                         + N'() SPLIT RANGE (''' + CONVERT(NVARCHAR(23), @currentDateUtc, 121) + N''');';
                EXEC sp_executesql @sql;

                SET @created = @created + 1;
                RAISERROR ('> Created partition boundary %s.', 0, 1, @sql) WITH NOWAIT;
            END

            SET @currentDateUtc =
                CASE @PartitionInterval
                    WHEN 'monthly' THEN DATEADD(MONTH, 1, @currentDateUtc)
                    WHEN 'weekly'  THEN DATEADD(WEEK,  1, @currentDateUtc)
                    ELSE                DATEADD(DAY,   1, @currentDateUtc)
                END;
        END

        SET @endTime = GETDATE();
        SET @durationString = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @startTime, @endTime));
        DECLARE @createdString VARCHAR(20) = CONVERT(VARCHAR, @created);
        RAISERROR ('Step 2 completed. %s partition boundary/boundaries created. Duration: %s milliseconds', 0, 1, @createdString, @durationString) WITH NOWAIT;


        -- ==================================================================================
        SET @endTime = GETDATE();
        SET @durationString = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @storedProcedureStartTime, @endTime));
        RAISERROR ('Stored procedure ''%s'' executed successfully. Total Duration: %s milliseconds', 0, 1, @storedProcedureName, @durationString) WITH NOWAIT;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

GRANT EXECUTE ON [gda].[spManageDigPartitions] TO [geotabdigadapter_client];
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spRetireDigPartitions
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--   Reclaims the space held by failure records older than a cutoff, by emptying
--   and removing whole partitions rather than by deleting rows. The
--   DatabaseMaintenanceService calls it with @Execute = 1 when automatic
--   retention is enabled; an operator can call it with the default @Execute = 0
--   to see exactly what would be removed, and nothing else.
--
--     EXEC [gda].[spRetireDigPartitions] @OlderThan = '2026-01-01';              -- report only
--     EXEC [gda].[spRetireDigPartitions] @OlderThan = '2026-01-01', @Execute = 1; -- perform it
--
--   THIS DELETES DATA and cannot be undone. Only partitions whose ENTIRE range
--   lies before the cutoff are emptied, so the current period is never affected,
--   but the records inside one are gone. Take a backup, or set
--   EnableAutomaticRetention to false, if the failure history has to be kept.
--
--   Each retirement is TRUNCATE TABLE ... WITH (PARTITIONS (n)) on every managed
--   table followed by one MERGE RANGE. That is a deliberate divergence from the
--   MyGeotab API Adapter's retirement utility, which switches each partition out
--   to a staging table and leaves it for the operator to archive and drop. The
--   two are doing different jobs: that utility exists to hand over the data,
--   whereas retention here is automatic and its purpose is to reclaim the space.
--   TRUNCATE needs no staging table to build, match indexes on, or leak, and it
--   does NOT reset the IDENTITY seed - so [id] keeps its practical uniqueness
--   across a retirement.
--
--   Under RANGE RIGHT the lowest boundary always bounds partition 1 from above,
--   and this always retires the lowest, so it never has to track partition
--   numbers shifting underneath it as boundaries are merged away. That holds for
--   one execution at a time, which is why execute mode takes an application lock
--   for each boundary it retires (see the loop).
--
--   The gda schema has no foreign keys, so - unlike the API Adapter's - this
--   needs no dependency ordering and no non-aligned index to drop and recreate.
--
-- Parameters:
--   @OlderThan: Partitions whose whole range precedes this are retirable.
--   @Execute:   0 (the default) reports; 1 performs.
--
-- Returns:
--   One result set: Object, Action, Detail. Columns are named explicitly because
--   the caller maps them by name.
-- ==========================================================================================
CREATE PROCEDURE [gda].[spRetireDigPartitions]
    @OlderThan DATETIME2(7),
    @Execute BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @partitionFunctionName NVARCHAR(128) = 'DateTimePartitionFunction_GeotabDigAdapter';
    DECLARE @partitionSchemeName   NVARCHAR(128) = 'DateTimePartitionScheme_GeotabDigAdapter';

    CREATE TABLE #report (
        [Seq]    INT IDENTITY(1,1) NOT NULL,
        [Object] NVARCHAR(300)     NOT NULL,
        [Action] NVARCHAR(200)     NOT NULL,
        [Detail] NVARCHAR(MAX)     NOT NULL
    );

    CREATE TABLE #tables (
        [Ordinal]    INT IDENTITY(1,1) NOT NULL,
        [ObjectId]   INT     NOT NULL,
        [SchemaName] SYSNAME NOT NULL,
        [TableName]  SYSNAME NOT NULL
    );

    BEGIN TRY
        -- ==================================================================================
        -- Validate input parameter values.
        IF @OlderThan IS NULL
        BEGIN
            RAISERROR('ERROR: @OlderThan is required. Example: EXEC [gda].[spRetireDigPartitions] @OlderThan = ''2026-01-01'';', 16, 1);
            RETURN;
        END

        IF @OlderThan > SYSUTCDATETIME()
        BEGIN
            RAISERROR('ERROR: @OlderThan is in the future. Refusing: this would remove partitions holding current records.', 16, 1);
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = @partitionFunctionName)
        BEGIN
            RAISERROR('ERROR: Partition function ''%s'' does not exist. It is created by the schema creation/upgrade script.', 16, 1, @partitionFunctionName);
            RETURN;
        END

        -- ==================================================================================
        -- The managed tables: everything in gda sitting on the DIG partition
        -- scheme. Discovered from the catalog, so a table added to the schema in a
        -- later release is picked up without editing this procedure.
        INSERT INTO #tables ([ObjectId], [SchemaName], [TableName])
        SELECT t.object_id, s.name, t.name
        FROM sys.tables t
        JOIN sys.schemas s          ON s.schema_id = t.schema_id
        JOIN sys.indexes i          ON i.object_id = t.object_id AND i.index_id IN (0, 1)
        JOIN sys.partition_schemes ps ON ps.data_space_id = i.data_space_id
        WHERE s.name = 'gda'
          AND ps.name = @partitionSchemeName
        ORDER BY t.name;

        DECLARE @tableCount INT = (SELECT COUNT(*) FROM #tables);

        IF @tableCount = 0
        BEGIN
            INSERT INTO #report ([Object], [Action], [Detail])
            VALUES (DB_NAME(), N'Nothing to do - no managed tables',
                    N'No table in the gda schema is placed on ' + @partitionSchemeName + N'.');
            SELECT [Object] AS [Object], [Action] AS [Action], [Detail] AS [Detail] FROM #report ORDER BY [Seq];
            RETURN;
        END

        IF @Execute = 1
            RAISERROR ('EXECUTE MODE: partitions entirely older than the cutoff are being removed. This deletes data.', 0, 1) WITH NOWAIT;
        ELSE
            RAISERROR ('REPORT ONLY: no changes are made.', 0, 1) WITH NOWAIT;

        -- ==================================================================================
        -- Retire the lowest qualifying boundary, repeatedly.
        DECLARE @ordinal INT = 0;
        DECLARE @pnum INT;
        DECLARE @boundary DATETIME2(7);
        DECLARE @boundaryCount INT;
        DECLARE @rows BIGINT;
        DECLARE @totalRows BIGINT = 0;
        DECLARE @candidates INT = 0;
        DECLARE @retired INT = 0;
        DECLARE @i INT;
        DECLARE @schemaName SYSNAME, @tableName SYSNAME;
        DECLARE @sql NVARCHAR(MAX);
        DECLARE @lockResult INT;

        WHILE 1 = 1
        BEGIN
            SET @ordinal = @ordinal + 1;

            -- Execute mode merges as it goes, so the boundary it is working on is
            -- always the lowest and its partition is always 1. Report mode changes
            -- nothing, so the Nth qualifying boundary is still the Nth.
            SET @pnum = CASE WHEN @Execute = 1 THEN 1 ELSE @ordinal END;

            -- Execute mode retires each boundary in ONE transaction, under an
            -- exclusive application lock, and reads the boundary only once it holds
            -- that lock. Without both, two overlapping executions - the service and
            -- an operator's call, say - each read the same lowest boundary. The first
            -- truncated partition 1 and merged the boundary away, which made
            -- partition 1 on every table span the NEXT period as well, and the
            -- second's truncation then deleted records inside the retention window.
            -- Under the lock the second reads the boundary after the first has
            -- committed, finds it already gone, and stops at the next one.
            IF @Execute = 1
            BEGIN
                BEGIN TRANSACTION;
                EXEC @lockResult = sp_getapplock @Resource = N'gda.spRetireDigPartitions', @LockMode = N'Exclusive',
                                                 @LockOwner = N'Transaction', @LockTimeout = 30000;
                IF @lockResult < 0
                    RAISERROR('ERROR: Could not acquire the partition retirement lock within 30 seconds; another retirement is running. This call retired nothing further.', 16, 1);
            END

            SET @boundary = NULL;
            SELECT @boundary = CONVERT(DATETIME2(7), prv.value)
            FROM sys.partition_range_values prv
            JOIN sys.partition_functions pf ON pf.function_id = prv.function_id
            WHERE pf.name = @partitionFunctionName
            ORDER BY prv.boundary_id
            OFFSET (@pnum - 1) ROWS FETCH NEXT 1 ROWS ONLY;

            IF @boundary IS NULL OR @boundary > @OlderThan
            BEGIN
                IF @Execute = 1
                    COMMIT TRANSACTION;
                BREAK;
            END

            -- SQL Server permits merging away the last remaining boundary, which
            -- would leave one unbounded partition that could never be retired
            -- again. Refuse: the caller wants space back, not an un-partitioned
            -- table.
            SELECT @boundaryCount = COUNT(*)
            FROM sys.partition_range_values prv
            JOIN sys.partition_functions pf ON pf.function_id = prv.function_id
            WHERE pf.name = @partitionFunctionName;

            IF @boundaryCount <= 1
            BEGIN
                INSERT INTO #report ([Object], [Action], [Detail])
                VALUES (@partitionFunctionName, N'STOPPED - last remaining boundary',
                        N'Removing the only boundary would leave a single unbounded partition that could never be retired. Run spManageDigPartitions to create the coming periods first.');
                IF @Execute = 1
                    COMMIT TRANSACTION;
                BREAK;
            END

            SELECT @rows = ISNULL(SUM(ps.row_count), 0)
            FROM sys.dm_db_partition_stats ps
            JOIN #tables t ON t.[ObjectId] = ps.object_id
            WHERE ps.index_id IN (0, 1)
              AND ps.partition_number = @pnum;

            SET @candidates = @candidates + 1;
            SET @totalRows = @totalRows + @rows;

            IF @Execute = 1
            BEGIN
                SET @i = 1;
                WHILE @i <= @tableCount
                BEGIN
                    SELECT @schemaName = [SchemaName], @tableName = [TableName]
                    FROM #tables WHERE [Ordinal] = @i;

                    SET @sql = N'TRUNCATE TABLE ' + QUOTENAME(@schemaName) + N'.' + QUOTENAME(@tableName)
                             + N' WITH (PARTITIONS (' + CONVERT(NVARCHAR(10), @pnum) + N'));';
                    EXEC sp_executesql @sql;

                    SET @i = @i + 1;
                END

                SET @sql = N'ALTER PARTITION FUNCTION ' + QUOTENAME(@partitionFunctionName)
                         + N'() MERGE RANGE (''' + CONVERT(NVARCHAR(23), @boundary, 121) + N''');';
                EXEC sp_executesql @sql;

                COMMIT TRANSACTION;

                SET @retired = @retired + 1;

                INSERT INTO #report ([Object], [Action], [Detail])
                VALUES (N'Partition ending ' + CONVERT(NVARCHAR(23), @boundary, 121),
                        N'RETIRED - truncated and merged',
                        CONVERT(NVARCHAR(20), @rows) + N' row(s) removed across ' + CONVERT(NVARCHAR(10), @tableCount) + N' table(s); boundary '
                        + CONVERT(NVARCHAR(23), @boundary, 121) + N' merged away.');
            END
            ELSE
            BEGIN
                INSERT INTO #report ([Object], [Action], [Detail])
                VALUES (N'Partition ending ' + CONVERT(NVARCHAR(23), @boundary, 121),
                        N'WOULD RETIRE - truncate and merge',
                        CONVERT(NVARCHAR(20), @rows) + N' row(s) would be removed across ' + CONVERT(NVARCHAR(10), @tableCount) + N' table(s).');
            END
        END

        -- ==================================================================================
        -- The leading partition is open-ended below, so anything older than the
        -- earliest boundary sits in it and is reclaimed only as a single block,
        -- when the cutoff finally passes that boundary. Report it rather than let
        -- it look like retention is doing nothing.
        DECLARE @lowestBoundary DATETIME2(7);
        SELECT @lowestBoundary = MIN(CONVERT(DATETIME2(7), prv.value))
        FROM sys.partition_range_values prv
        JOIN sys.partition_functions pf ON pf.function_id = prv.function_id
        WHERE pf.name = @partitionFunctionName;

        DECLARE @leadingRows BIGINT;
        SELECT @leadingRows = ISNULL(SUM(ps.row_count), 0)
        FROM sys.dm_db_partition_stats ps
        JOIN #tables t ON t.[ObjectId] = ps.object_id
        WHERE ps.index_id IN (0, 1)
          AND ps.partition_number = 1;

        IF @leadingRows > 0 AND @lowestBoundary > @OlderThan
        BEGIN
            INSERT INTO #report ([Object], [Action], [Detail])
            VALUES (N'Partition ending ' + CONVERT(NVARCHAR(23), @lowestBoundary, 121),
                    N'NOT RETIRABLE YET - leading partition',
                    CONVERT(NVARCHAR(20), @leadingRows) + N' row(s) pre-date the earliest boundary and share one open-ended partition, so they are reclaimed together once the cutoff passes '
                    + CONVERT(NVARCHAR(23), @lowestBoundary, 121) + N'. Nothing is wrong; this is telling you when they go.');
        END

        -- ==================================================================================
        -- Summary.
        INSERT INTO #report ([Object], [Action], [Detail])
        SELECT DB_NAME(),
               CASE WHEN @candidates = 0 THEN N'Nothing to retire - no partition lies entirely before ' + CONVERT(NVARCHAR(23), @OlderThan, 121)
                    WHEN @Execute = 1    THEN CONVERT(NVARCHAR(10), @retired) + N' partition(s) retired'
                    ELSE                      CONVERT(NVARCHAR(10), @candidates) + N' partition(s) would be retired' END,
               CASE WHEN @candidates = 0 THEN N'No changes were made.'
                    WHEN @Execute = 1    THEN CONVERT(NVARCHAR(20), @totalRows) + N' record(s) were permanently deleted.'
                    ELSE                      CONVERT(NVARCHAR(20), @totalRows) + N' record(s) would be permanently deleted. Re-run with @Execute = 1 to perform this. It cannot be undone.' END;

        SELECT [Object] AS [Object], [Action] AS [Action], [Detail] AS [Detail]
        FROM #report
        ORDER BY [Seq];
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

GRANT EXECUTE ON [gda].[spRetireDigPartitions] TO [geotabdigadapter_client];
GO

/*** [START] Database Maintenance (added in 5.2.0.0) ***/
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [gda].[DigMaintenanceLogs](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[MaintenanceTypeId] [smallint] NOT NULL,
	[StartTimeUtc] [datetime2](7) NOT NULL,
	[EndTimeUtc] [datetime2](7) NULL,
	[Success] [bit] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DigMaintenanceLogs] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

-- Serves the one query the service issues against this table: the latest row
-- of a given type.
CREATE NONCLUSTERED INDEX [IX_DigMaintenanceLogs_MaintenanceTypeId_StartTimeUtc]
ON [gda].[DigMaintenanceLogs] ([MaintenanceTypeId], [StartTimeUtc]);
GO

CREATE NONCLUSTERED INDEX [IX_DigMaintenanceLogs_RecordLastChangedUtc]
ON [gda].[DigMaintenanceLogs] ([RecordLastChangedUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create View: vwStatsForLevel1DigMaintenance
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--   The gda tables that have had more than 1000 modifications since their statistics were
--   last updated, with those modifications as a PERCENTAGE (0..100) of the table's rows.
--   The adapter updates the statistics of a table whose percentage exceeds 10.
--
--   THE SAME THRESHOLDS ARE CONSTANTS IN THE ADAPTER (DigMaintenanceThresholds) AND THE TWO
--   MUST AGREE. This view decides which tables are offered and the adapter decides which of
--   those it acts on, so a disagreement between them does not fail: it silently maintains
--   nothing. A unit test reads this script and compares them.
--
--   Mirrors the MyGeotab API Adapter's [dbo].[vwStatsForLevel1DBMaintenance] as that view
--   stands from its version 5.2.0.0, which made the same two corrections:
--     1. It is scoped to its own schema - gda here, dbo there - so in a database the two
--        adapters share, each is offered only its own tables. Before, the API Adapter's view
--        had no schema predicate and offered gda tables too.
--     2. Each table's row count is taken from its heap or clustered index alone, and its
--        statistics are aggregated separately. Joining the per-index partition statistics
--        to the statistics objects BEFORE summing repeats every row count once per
--        statistics object, which multiplies the row count by the number of indexes times
--        the number of statistics and divides the percentage by the same factor.
-- ==========================================================================================
CREATE VIEW [gda].[vwStatsForLevel1DigMaintenance]
AS
WITH GdaTables AS (
    SELECT tbl.[object_id],
        sch.[name] AS SchemaName,
        tbl.[name] AS TableName
    FROM sys.tables tbl
    INNER JOIN sys.schemas sch
		ON tbl.[schema_id] = sch.[schema_id]
    WHERE sch.[name] = 'gda'
),
TableRows AS (
    SELECT gt.[object_id],
        SUM(CASE WHEN ps.[index_id] IN (0, 1) THEN ps.[row_count] ELSE 0 END) AS RecordCount,
        SUM(ps.[used_page_count] * 8) AS UsedSpaceKB,
        SUM(ps.[reserved_page_count] * 8) AS ReservedSpaceKB,
        (SUM(ps.[reserved_page_count]) - SUM(ps.[used_page_count])) * 8 AS FreeSpaceKB
    FROM GdaTables gt
    INNER JOIN sys.dm_db_partition_stats ps
		ON ps.[object_id] = gt.[object_id]
    GROUP BY gt.[object_id]
),
TableModifications AS (
    SELECT gt.[object_id],
        MAX(sp.[modification_counter]) AS ModsSinceLastStatsUpdate,
        MAX(sp.[last_updated]) AS LastStatsUpdate
    FROM GdaTables gt
    INNER JOIN sys.stats st
		ON st.[object_id] = gt.[object_id]
    CROSS APPLY sys.dm_db_stats_properties(st.[object_id], st.[stats_id]) sp
    GROUP BY gt.[object_id]
),
TableStats AS (
    SELECT gt.SchemaName,
        gt.TableName,
        tr.RecordCount,
        tr.UsedSpaceKB,
        tr.ReservedSpaceKB,
        tr.FreeSpaceKB,
        tm.ModsSinceLastStatsUpdate,
        tm.LastStatsUpdate
    FROM GdaTables gt
    INNER JOIN TableRows tr
		ON tr.[object_id] = gt.[object_id]
    INNER JOIN TableModifications tm
		ON tm.[object_id] = gt.[object_id]
)
SELECT
    ROW_NUMBER() OVER (ORDER BY ModsSinceLastStatsUpdate DESC) AS RowId,
    SchemaName,
    TableName,
    RecordCount,
    UsedSpaceKB,
    ReservedSpaceKB,
    FreeSpaceKB,
    ModsSinceLastStatsUpdate,
    CASE
        WHEN RecordCount > 0 THEN (CAST(ModsSinceLastStatsUpdate AS FLOAT) / RecordCount) * 100
        ELSE 0
    END AS PctModsSinceLastStatsUpdate,
    LastStatsUpdate
FROM TableStats
WHERE ModsSinceLastStatsUpdate > 1000;
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create View: vwStatsForLevel2DigMaintenance
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--   The partitions of gda indexes that are more than 10 percent fragmented, with each one's
--   fragmentation as a PERCENTAGE (0..100). TotalPartitions and HighFragPartitions give the
--   number of partitions of the whole index and the number of those more than 30 percent
--   fragmented, and PctHighFragPartitions is the second as a PERCENTAGE (0..100) of the
--   first. The adapter rebuilds the whole index where PctHighFragPartitions exceeds 50,
--   and otherwise rebuilds a partition more than 30 percent fragmented and reorganizes one
--   more than 10 percent fragmented.
--
--   THE SAME THRESHOLDS ARE CONSTANTS IN THE ADAPTER (DigMaintenanceThresholds) AND THE TWO
--   MUST AGREE. A unit test reads this script and compares them.
--
--   Mirrors the MyGeotab API Adapter's [dbo].[vwStatsForLevel2DBMaintenance], scoped to the
--   gda schema. The scope is applied to what is READ, not only to what is returned:
--   sys.dm_db_index_physical_stats is called once per gda table rather than once for the
--   whole database, because it reads every index it is pointed at, and filtering its output
--   afterwards would still read every index of every other schema in a database the two
--   adapters share. It is read in 'LIMITED' mode: see the comment at the call.
-- ==========================================================================================
CREATE VIEW [gda].[vwStatsForLevel2DigMaintenance]
AS
WITH GdaTables AS (
    -- TOP stops the optimizer applying the schema filter after the index statistics have been read.
    -- Without it the plan applies the function below to every table in the database and discards the
    -- rows outside this schema afterwards - so it reads them all.
    SELECT TOP (2147483647) obj.[object_id],
        sch.[name] AS SchemaName,
        obj.[name] AS TableName
    FROM sys.objects obj
    INNER JOIN sys.schemas sch
		ON obj.[schema_id] = sch.[schema_id]
    WHERE sch.[name] = 'gda'
      AND obj.[type] = 'U'
),
RawPartitionStats AS (
    SELECT gt.SchemaName,
        gt.TableName,
        idx.[name] AS IndexName,
        ps.[partition_number] AS PartitionNumber,
        ps.[avg_fragmentation_in_percent] AS FragmentationPct,
        ps.[page_count] * 8 AS IndexSizeKB
    FROM GdaTables gt
    -- LIMITED reports each index's leaf level, which is what REORGANIZE and REBUILD act on, and reads only the pages
    -- above it. DETAILED returned a row for every upper level too, and the highest fragmentation among them - often a
    -- small upper level's - decided, so a healthy index could be offered for a rebuild.
    CROSS APPLY sys.dm_db_index_physical_stats(DB_ID(), gt.[object_id], NULL, NULL, 'LIMITED') ps
    INNER JOIN sys.indexes idx
		ON ps.[object_id] = idx.[object_id] AND ps.[index_id] = idx.[index_id]
    WHERE ps.[index_id] > 0 -- (exclude heaps)
      -- In-row data only, so that each row's fragmentation and size describe the same allocation unit.
      AND ps.[alloc_unit_type_desc] = N'IN_ROW_DATA'
),
PartitionStats AS (
    SELECT SchemaName,
        TableName,
        IndexName,
        PartitionNumber,
        MAX(FragmentationPct) AS FragmentationPct,
        MAX(IndexSizeKB) AS IndexSizeKB
    FROM RawPartitionStats
    GROUP BY SchemaName, TableName, IndexName, PartitionNumber
),
WithAggregates AS (
    -- The per-index totals are window aggregates over the same rows, so PartitionStats - and
    -- the index statistics function inside it - is read ONCE. SQL Server evaluates a CTE once
    -- per reference, and an aggregate CTE joined back to PartitionStats read every page of
    -- every index twice per run. COUNT(*) equals a count of distinct partitions here because
    -- PartitionStats already holds one row per partition of each index.
    SELECT SchemaName,
        TableName,
        IndexName,
        PartitionNumber,
        FragmentationPct,
        IndexSizeKB,
        COUNT(*) OVER (PARTITION BY SchemaName, TableName, IndexName) AS TotalPartitions,
        SUM(CASE WHEN FragmentationPct > 30 THEN 1 ELSE 0 END) OVER (PARTITION BY SchemaName, TableName, IndexName) AS HighFragPartitions
    FROM PartitionStats
)
SELECT ROW_NUMBER() OVER (ORDER BY TableName ASC, IndexName ASC, PartitionNumber ASC) AS RowId,
    SchemaName,
    TableName,
    IndexName,
    PartitionNumber,
    FragmentationPct,
    IndexSizeKB,
    TotalPartitions,
    HighFragPartitions,
    CASE
        WHEN TotalPartitions > 0 THEN (CAST(HighFragPartitions AS FLOAT) / TotalPartitions) * 100
        ELSE 0
    END AS PctHighFragPartitions
FROM WithAggregates
WHERE FragmentationPct > 10
    AND IndexSizeKB > 1;
GO

/*** [END] Database Maintenance ***/


/*** Database Version Update Below ***/
-- Insert a record into the MiddlewareVersionInfo table to reflect the current
-- database version.
INSERT INTO [gda].[MiddlewareVersionInfo] ([DatabaseVersion], [RecordCreationTimeUtc])
VALUES ('5.2.0.0', GETUTCDATE());
GO
