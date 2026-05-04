-- ================================================================================
-- DATABASE TYPE: SQL Server
-- 
-- NOTES: 
--   1: This script applies to the Geotab DIG Adapter database starting with
--	    application version 5.0.0.0. It does not apply to earlier versions of the
--      application. 
--   2: This script will not be updated. Any future schema changes will be included
--      in new incremental scripts tagged with the relevant application versions.
--   3: Be sure to alter the "USE [geotabadapterdb]" statement below if you have
--      changed the database name to something else.
--
-- DESCRIPTION: 
--   This script is intended for use in creating the database schema for version
--   5.0.0.0 of the Geotab DIG Adapter in an empty database.
-- ================================================================================

USE [geotabadapterdb]
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
 CONSTRAINT [PK_Q_ProvisionDevicesFail] PRIMARY KEY CLUSTERED 
(
    [id] ASC
)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_ProvisionDevicesFail_ThirdPartyId]
ON [gda].[Q_ProvisionDevicesFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc]);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQProvisionDevicesBatch
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Stored Procedure: spClaimQProvisionDevicesBatch
-- Description: 
--		Atomically claims and retrieves a batch of Q_ProvisionDevices records for processing.
--      Implements the "atomic pop" pattern to prevent duplicate processing in multi-instance
--      scenarios and to ensure crash recovery. Records that are "stale" (stuck in progress
--      beyond the threshold) are automatically reclaimed.
-- Parameters:
--   @BatchSize
--		Maximum number of records to claim in a single batch.
--   @StaleThresholdMinutes
--		Minutes after which an "in progress" record is considered stale and eligible for 
--		reclaim.
-- Returns: The claimed Q_ProvisionDevices records.
-- ==========================================================================================
CREATE PROCEDURE [gda].[spClaimQProvisionDevicesBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    -- Use a CTE to select ordered rows, then update them atomically.
    WITH BatchCTE AS (
        SELECT TOP (@BatchSize) 
            id,
            ProcessingStatus,
            ProcessingStartTimeUtc,
            RetryCount
        FROM [gda].[Q_ProvisionDevices] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0  -- Available
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc
    )
    UPDATE BatchCTE
    SET 
        ProcessingStatus = 1,  -- InProgress
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END;

    -- Return the claimed records.
    SELECT *
    FROM [gda].[Q_ProvisionDevices]
    WHERE ProcessingStatus = 1 
      AND ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    ORDER BY RecordCreationTimeUtc;
END;
GO

-- Grant execute permission to the client user/role
GRANT EXECUTE ON [gda].[spClaimQProvisionDevicesBatch] TO [geotabdigadapter_client];
GO


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
 CONSTRAINT [PK_Q_GpsRecordsFail] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_GpsRecordsFail_ThirdPartyId]
ON [gda].[Q_GpsRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQGpsRecordsBatch
CREATE PROCEDURE [gda].[spClaimQGpsRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_GpsRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END;

    SELECT * FROM [gda].[Q_GpsRecords]
    WHERE ProcessingStatus = 1 AND ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    ORDER BY RecordCreationTimeUtc;
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
 CONSTRAINT [PK_Q_AccelerationRecordsFail] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_AccelerationRecordsFail_ThirdPartyId]
ON [gda].[Q_AccelerationRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQAccelerationRecordsBatch
CREATE PROCEDURE [gda].[spClaimQAccelerationRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_AccelerationRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END;

    SELECT * FROM [gda].[Q_AccelerationRecords]
    WHERE ProcessingStatus = 1 AND ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    ORDER BY RecordCreationTimeUtc;
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

CREATE NONCLUSTERED INDEX [IX_Q_BinaryRecords_PendingWork]
ON [gda].[Q_BinaryRecords] ([id])
WHERE ([ProcessingStatus] = 0);
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
 CONSTRAINT [PK_Q_BinaryRecordsFail] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_BinaryRecordsFail_ThirdPartyId]
ON [gda].[Q_BinaryRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQBinaryRecordsBatch
CREATE PROCEDURE [gda].[spClaimQBinaryRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_BinaryRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END;

    SELECT * FROM [gda].[Q_BinaryRecords]
    WHERE ProcessingStatus = 1 AND ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    ORDER BY RecordCreationTimeUtc;
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
 CONSTRAINT [PK_Q_BluetoothRecordsFail] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_BluetoothRecordsFail_ThirdPartyId]
ON [gda].[Q_BluetoothRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQBluetoothRecordsBatch
CREATE PROCEDURE [gda].[spClaimQBluetoothRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_BluetoothRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END;

    SELECT * FROM [gda].[Q_BluetoothRecords]
    WHERE ProcessingStatus = 1 AND ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    ORDER BY RecordCreationTimeUtc;
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
 CONSTRAINT [PK_Q_DriverChangeRecordsFail] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_DriverChangeRecordsFail_ThirdPartyId]
ON [gda].[Q_DriverChangeRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQDriverChangeRecordsBatch
CREATE PROCEDURE [gda].[spClaimQDriverChangeRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_DriverChangeRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END;

    SELECT * FROM [gda].[Q_DriverChangeRecords]
    WHERE ProcessingStatus = 1 AND ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    ORDER BY RecordCreationTimeUtc;
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
 CONSTRAINT [PK_Q_GenericFaultRecordsFail] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_GenericFaultRecordsFail_ThirdPartyId]
ON [gda].[Q_GenericFaultRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQGenericFaultRecordsBatch
CREATE PROCEDURE [gda].[spClaimQGenericFaultRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_GenericFaultRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END;

    SELECT * FROM [gda].[Q_GenericFaultRecords]
    WHERE ProcessingStatus = 1 AND ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    ORDER BY RecordCreationTimeUtc;
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
 CONSTRAINT [PK_Q_GenericStatusRecordsFail] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_GenericStatusRecordsFail_ThirdPartyId]
ON [gda].[Q_GenericStatusRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQGenericStatusRecordsBatch
CREATE PROCEDURE [gda].[spClaimQGenericStatusRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_GenericStatusRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END;

    SELECT * FROM [gda].[Q_GenericStatusRecords]
    WHERE ProcessingStatus = 1 AND ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    ORDER BY RecordCreationTimeUtc;
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
 CONSTRAINT [PK_Q_J1708FaultRecordsFail] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_J1708FaultRecordsFail_ThirdPartyId]
ON [gda].[Q_J1708FaultRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQJ1708FaultRecordsBatch
CREATE PROCEDURE [gda].[spClaimQJ1708FaultRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_J1708FaultRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END;

    SELECT * FROM [gda].[Q_J1708FaultRecords]
    WHERE ProcessingStatus = 1 AND ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    ORDER BY RecordCreationTimeUtc;
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
 CONSTRAINT [PK_Q_J1939FaultRecordsFail] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_J1939FaultRecordsFail_ThirdPartyId]
ON [gda].[Q_J1939FaultRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQJ1939FaultRecordsBatch
CREATE PROCEDURE [gda].[spClaimQJ1939FaultRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_J1939FaultRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END;

    SELECT * FROM [gda].[Q_J1939FaultRecords]
    WHERE ProcessingStatus = 1 AND ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    ORDER BY RecordCreationTimeUtc;
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
 CONSTRAINT [PK_Q_ObdiiFaultRecordsFail] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_ObdiiFaultRecordsFail_ThirdPartyId]
ON [gda].[Q_ObdiiFaultRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQObdiiFaultRecordsBatch
CREATE PROCEDURE [gda].[spClaimQObdiiFaultRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_ObdiiFaultRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END;

    SELECT * FROM [gda].[Q_ObdiiFaultRecords]
    WHERE ProcessingStatus = 1 AND ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    ORDER BY RecordCreationTimeUtc;
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
 CONSTRAINT [PK_Q_VinRecordsFail] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Q_VinRecordsFail_ThirdPartyId]
ON [gda].[Q_VinRecordsFail] ([ThirdPartyId])
INCLUDE ([FailureReason], [RecordCreationTimeUtc]);
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Stored Procedure: spClaimQVinRecordsBatch
CREATE PROCEDURE [gda].[spClaimQVinRecordsBatch]
    @BatchSize INT,
    @StaleThresholdMinutes INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProcessingStartTimeUtc DATETIME2 = GETUTCDATE();
    DECLARE @StaleThreshold DATETIME2 = DATEADD(MINUTE, -@StaleThresholdMinutes, GETUTCDATE());

    WITH BatchCTE AS (
        SELECT TOP (@BatchSize)
            id, ProcessingStatus, ProcessingStartTimeUtc, RetryCount
        FROM [gda].[Q_VinRecords] WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE ProcessingStatus = 0
           OR (ProcessingStatus = 1 AND ProcessingStartTimeUtc < @StaleThreshold)
        ORDER BY RecordCreationTimeUtc
    )
    UPDATE BatchCTE
    SET
        ProcessingStatus = 1,
        ProcessingStartTimeUtc = @ProcessingStartTimeUtc,
        RetryCount = CASE WHEN ProcessingStatus = 1 THEN RetryCount + 1 ELSE RetryCount END;

    SELECT * FROM [gda].[Q_VinRecords]
    WHERE ProcessingStatus = 1 AND ProcessingStartTimeUtc = @ProcessingStartTimeUtc
    ORDER BY RecordCreationTimeUtc;
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


/*** Database Version Update Below ***/
-- Insert a record into the MiddlewareVersionInfo table to reflect the current
-- database version.
INSERT INTO [gda].[MiddlewareVersionInfo] ([DatabaseVersion], [RecordCreationTimeUtc])
VALUES ('5.0.0.0', GETUTCDATE());
