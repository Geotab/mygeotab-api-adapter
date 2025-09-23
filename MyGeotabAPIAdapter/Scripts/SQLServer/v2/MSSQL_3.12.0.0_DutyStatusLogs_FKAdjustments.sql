-- ================================================================================
-- DATABASE TYPE: SQL Server
-- 
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.11.0.0 to version 3.12.0.0.
--
-- NOTES: 
--   1: This script cannot be run against any database version other than that 
--		specified above. 
--   2: Be sure to alter the "USE [geotabadapterdb]" statement below if you have
--      changed the database name to something else.
-- ================================================================================

USE [geotabadapterdb]
GO

/*** [START] Part 1 of 3: Database Version Validation Below ***/ 
-- Store upgrade database version in a temporary table.
DROP TABLE IF EXISTS #TMP_UpgradeDatabaseVersionTable;
CREATE TABLE #TMP_UpgradeDatabaseVersionTable (UpgradeDatabaseVersion NVARCHAR(50));
INSERT INTO #TMP_UpgradeDatabaseVersionTable VALUES ('3.12.0.0');

DECLARE @requiredStartingDatabaseVersion NVARCHAR(50) = '3.11.0.0';
DECLARE @actualStartingDatabaseVersion NVARCHAR(50);

SELECT TOP 1 @actualStartingDatabaseVersion = DatabaseVersion
FROM dbo.MiddlewareVersionInfo2
ORDER BY RecordCreationTimeUtc DESC;

IF @actualStartingDatabaseVersion <> @requiredStartingDatabaseVersion
BEGIN
	RAISERROR('ERROR: This script can only be run against the expected database version. [Expected: %s; Actual: %s]', 16, 1, @requiredStartingDatabaseVersion, @actualStartingDatabaseVersion);
	RETURN;
END
/*** [END] Part 1 of 3: Database Version Validation Above ***/ 



/*** [START] Part 2 of 3: Database Upgrades Below ***/ 
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Remove the foreign keys associated with Users2.id and Rules2.id to fix the FK violation issue:
ALTER TABLE [dbo].[DeviceStatusInfo2] DROP CONSTRAINT [FK_DeviceStatusInfo2_Users2];
ALTER TABLE [dbo].[DriverChanges2] DROP CONSTRAINT [FK_DriverChanges2_Users2];
ALTER TABLE [dbo].[DutyStatusAvailabilities2] DROP CONSTRAINT [FK_DutyStatusAvailabilities2_Users2];
ALTER TABLE [dbo].[DVIRDefectRemarks2] DROP CONSTRAINT [FK_DVIRDefectRemarks2_Users2];
ALTER TABLE [dbo].[DVIRDefects2] DROP CONSTRAINT [FK_DVIRDefects2_Users2];
ALTER TABLE [dbo].[DVIRLogs2] DROP CONSTRAINT [FK_DVIRLogs2_Users2];
ALTER TABLE [dbo].[DVIRLogs2] DROP CONSTRAINT [FK_DVIRLogs2_Users2_2];
ALTER TABLE [dbo].[DVIRLogs2] DROP CONSTRAINT [FK_DVIRLogs2_Users2_3];
ALTER TABLE [dbo].[ExceptionEvents2] DROP CONSTRAINT [FK_ExceptionEvents2_Rules2];
ALTER TABLE [dbo].[ExceptionEvents2] DROP CONSTRAINT [FK_ExceptionEvents2_Users2];
ALTER TABLE [dbo].[FaultData2] DROP CONSTRAINT [FK_FaultData2_Users2];
ALTER TABLE [dbo].[Trips2] DROP CONSTRAINT [FK_Trips2_Users2];

-- Create DutyStatusLogs2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DutyStatusLogs2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[Annotations] [nvarchar](max) NULL,
	[CoDrivers] [nvarchar](max) NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeferralMinutes] [int] NULL,
	--[DeferralStatusId] [smallint] NULL,
	[DeferralStatus] [nvarchar](50) NULL,
	[DeviceId] [bigint] NULL,
	[DistanceSinceValidCoordinates] [real] NULL,
	[DriverId] [bigint] NULL,
	[EditDateTime] [datetime2](7) NULL,
	[EditRequestedByUserId] [bigint] NULL,
	[EngineHours] [float] NULL,
	[EventCheckSum] [bigint] NULL,
	[EventCode] [int] NULL,
	[EventRecordStatus] [int] NULL,
	[EventType] [int] NULL,
	[IsHidden] [bit] NULL,
	[IsIgnored] [bit] NULL,
	[IsTransitioning] [bit] NULL,
	[Location] [nvarchar](max) NULL,
	[LocationX] [float] NULL,
	[LocationY] [float] NULL,
	[Malfunction] [nvarchar](50) NULL,
	[Odometer] [float] NULL,
	[Origin] [nvarchar](50) NULL,
	[ParentId] [nvarchar](50) NULL,
	[Sequence] [bigint] NULL,
	[State] [nvarchar](50) NULL,
	[Status] [nvarchar](50) NULL,
	[UserHosRuleSet] [nvarchar](max) NULL,
	[VerifyDateTime] [datetime2](7) NULL,
	[Version] [bigint] NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DutyStatusLogs2] PRIMARY KEY CLUSTERED 
(
	[DateTime] ASC,
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DutyStatusLogs2_DeviceId] ON [dbo].[DutyStatusLogs2]
(
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DutyStatusLogs2_DriverId] ON [dbo].[DutyStatusLogs2]
(
	[DriverId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DutyStatusLogs2_EditRequestedByUserId] ON [dbo].[DutyStatusLogs2]
(
	[EditRequestedByUserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DutyStatusLogs2_RecordCreationTimeUtc] ON [dbo].[DutyStatusLogs2]
(
	[RecordCreationTimeUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DutyStatusLogs2_DateTime_Device] ON [dbo].[DutyStatusLogs2]
(
	[DateTime] ASC,
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DutyStatusLogs2_DateTime_Driver] ON [dbo].[DutyStatusLogs2]
(
	[DateTime] ASC,
	[DriverId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DutyStatusLogs2_DateTime_EditRequestedByUser] ON [dbo].[DutyStatusLogs2]
(
	[DateTime] ASC,
	[EditRequestedByUserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE UNIQUE NONCLUSTERED INDEX [UI_DutyStatusLogs2_Id] ON [dbo].[DutyStatusLogs2]
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_DutyStatusLogs2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_DutyStatusLogs2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[Annotations] [nvarchar](max) NULL,
	[CoDrivers] [nvarchar](max) NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeferralMinutes] [int] NULL,
	--[DeferralStatusId] [smallint] NULL,
	[DeferralStatus] [nvarchar](50) NULL,
	[DeviceId] [bigint] NULL,
	[DistanceSinceValidCoordinates] [real] NULL,
	[DriverId] [bigint] NULL,
	[EditDateTime] [datetime2](7) NULL,
	[EditRequestedByUserId] [bigint] NULL,
	[EngineHours] [float] NULL,
	[EventCheckSum] [bigint] NULL,
	[EventCode] [int] NULL,
	[EventRecordStatus] [int] NULL,
	[EventType] [int] NULL,
	[IsHidden] [bit] NULL,
	[IsIgnored] [bit] NULL,
	[IsTransitioning] [bit] NULL,
	[Location] [nvarchar](max) NULL,
	[LocationX] [float] NULL,
	[LocationY] [float] NULL,
	[Malfunction] [nvarchar](50) NULL,
	[Odometer] [float] NULL,
	[Origin] [nvarchar](50) NULL,
	[ParentId] [nvarchar](50) NULL,
	[Sequence] [bigint] NULL,
	[State] [nvarchar](50) NULL,
	[Status] [nvarchar](50) NULL,
	[UserHosRuleSet] [nvarchar](max) NULL,
	[VerifyDateTime] [datetime2](7) NULL,
	[Version] [bigint] NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_stg_DutyStatusLogs2_RecordCreationTimeUtc] ON [dbo].[stg_DutyStatusLogs2]
(
	[id] ASC,
	[RecordCreationTimeUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_DutyStatusLogs2 stored procedure:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_DutyStatusLogs2 staging table to the DutyStatusLogs2
--   table and then truncates the staging table. Handles changes to the DateTime 
--   (partitioning key) by deleting the existing record and inserting the new version.
--
-- Notes:
--  - Uses a multi-step process (DELETE movers + MERGE) within a transaction.
-- ==========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spMerge_stg_DutyStatusLogs2]
AS
BEGIN
    SET NOCOUNT ON;

	-- Create temporary table to store IDs of any records where DateTime has changed.
	DROP TABLE IF EXISTS #TMP_MovedRecordIds;
	CREATE TABLE #TMP_MovedRecordIds (id uniqueidentifier PRIMARY KEY);
	
	-- Create temporary table to store the de-duplicated staging table data. Add a rownum
	-- column so that it is not necessary to list all columns when populating this table.
	DROP TABLE IF EXISTS #TMP_DeduplicatedStagingData;
	SELECT *, 
		CAST(NULL AS INT) AS rownum  
	INTO #TMP_DeduplicatedStagingData
	FROM dbo.stg_DutyStatusLogs2
	WHERE 1 = 0;

	BEGIN TRY
		BEGIN TRANSACTION;

		-- De-duplicate staging table by selecting the latest record per id.
		-- Uses ROW_NUMBER() partitioned by id, ordering by RecordCreationTimeUtc descending.
		INSERT INTO #TMP_DeduplicatedStagingData
		SELECT *
		FROM (
			SELECT
				stg.*,
				ROW_NUMBER() OVER (PARTITION BY stg.id ORDER BY stg.RecordCreationTimeUtc DESC) AS rownum
			FROM dbo.stg_DutyStatusLogs2 stg
		) AS sub
		WHERE sub.rownum = 1;

        -- Identify records where DateTime has changed.
        INSERT INTO #TMP_MovedRecordIds (id)
        SELECT s.id
        FROM #TMP_DeduplicatedStagingData s
        INNER JOIN dbo.DutyStatusLogs2 d ON s.id = d.id
        WHERE s.[DateTime] <> d.[DateTime];

        -- Delete the old versions of these records from the target table.
        DELETE d
        FROM dbo.DutyStatusLogs2 AS d
        INNER JOIN #TMP_MovedRecordIds m ON d.id = m.id;

		-- Perform upsert.
		MERGE INTO dbo.DutyStatusLogs2 AS d
		USING #TMP_DeduplicatedStagingData AS s
		-- id is unique key and logical key for matching. 
		ON d.id = s.id
		WHEN MATCHED AND (
			ISNULL(d.GeotabId, '') <> ISNULL(s.GeotabId, '') OR
			ISNULL(d.Annotations, '') <> ISNULL(s.Annotations, '') OR
			ISNULL(d.CoDrivers, '') <> ISNULL(s.CoDrivers, '') OR
			-- DateTime not evaluated because movers were deleted.
			ISNULL(d.DeferralMinutes, -1) <> ISNULL(s.DeferralMinutes, -1) OR
			ISNULL(d.DeferralStatus, '') <> ISNULL(s.DeferralStatus, '') OR			
			-- ISNULL(d.DeferralStatusId, -1) <> ISNULL(s.DeferralStatusId, -1) OR
			ISNULL(d.DeviceId, -1) <> ISNULL(s.DeviceId, -1) OR
			ISNULL(d.DistanceSinceValidCoordinates, -1.0) <> ISNULL(s.DistanceSinceValidCoordinates, -1.0) OR
			ISNULL(d.DriverId, -1) <> ISNULL(s.DriverId, -1) OR
			ISNULL(d.EditDateTime, '1900-01-01') <> ISNULL(s.EditDateTime, '1900-01-01') OR
			ISNULL(d.EditRequestedByUserId, -1) <> ISNULL(s.EditRequestedByUserId, -1) OR
			ISNULL(d.EngineHours, -999.0) <> ISNULL(s.EngineHours, -999.0) OR
			ISNULL(d.EventCheckSum, -1) <> ISNULL(s.EventCheckSum, -1) OR
			ISNULL(d.EventCode, -1) <> ISNULL(s.EventCode, -1) OR
			ISNULL(d.EventRecordStatus, -1) <> ISNULL(s.EventRecordStatus, -1) OR
			ISNULL(d.EventType, -1) <> ISNULL(s.EventType, -1) OR
			ISNULL(d.IsHidden, 0) <> ISNULL(s.IsHidden, 0) OR
			ISNULL(d.IsIgnored, 0) <> ISNULL(s.IsIgnored, 0) OR
			ISNULL(d.IsTransitioning, 0) <> ISNULL(s.IsTransitioning, 0) OR
			ISNULL(d.Location, '') <> ISNULL(s.Location, '') OR
			ISNULL(d.LocationX, -999.0) <> ISNULL(s.LocationX, -999.0) OR
			ISNULL(d.LocationY, -999.0) <> ISNULL(s.LocationY, -999.0) OR
			ISNULL(d.Malfunction, '') <> ISNULL(s.Malfunction, '') OR
			ISNULL(d.Odometer, -999.0) <> ISNULL(s.Odometer, -999.0) OR
			ISNULL(d.Origin, '') <> ISNULL(s.Origin, '') OR
			ISNULL(d.ParentId, -1) <> ISNULL(s.ParentId, -1) OR
			ISNULL(d.Sequence, -1) <> ISNULL(s.Sequence, -1) OR
			ISNULL(d.State, '') <> ISNULL(s.State, '') OR
			ISNULL(d.Status, '') <> ISNULL(s.Status, '') OR
			ISNULL(d.UserHosRuleSet, '') <> ISNULL(s.UserHosRuleSet, '') OR
			ISNULL(d.VerifyDateTime, '1900-01-01') <> ISNULL(s.VerifyDateTime, '1900-01-01') OR
			ISNULL(d.[Version], -1) <> ISNULL(s.[Version], -1) 
			-- RecordCreationTimeUtc not evaluated as it should never match. 
		)
		THEN UPDATE SET
            d.GeotabId = s.GeotabId,
            d.Annotations = s.Annotations,
            d.CoDrivers = s.CoDrivers,
            d.[DateTime] = s.[DateTime],
			d.DeferralMinutes = s.DeferralMinutes,
			d.DeferralStatus = s.DeferralStatus,
			-- d.DeferralStatusId = s.DeferralStatusId,
            d.DeviceId = s.DeviceId,
			d.DistanceSinceValidCoordinates = s.DistanceSinceValidCoordinates,
            d.DriverId = s.DriverId,
            d.EditDateTime = s.EditDateTime,
            d.EditRequestedByUserId = s.EditRequestedByUserId,
            d.EngineHours = s.EngineHours,
            d.EventCheckSum = s.EventCheckSum,
            d.EventCode = s.EventCode,
            d.EventRecordStatus = s.EventRecordStatus,
            d.EventType = s.EventType,
            d.IsHidden = s.IsHidden,			
            d.IsIgnored = s.IsIgnored,
            d.IsTransitioning = s.IsTransitioning,
            d.Location = s.Location,
            d.LocationX = s.LocationX,
            d.LocationY = s.LocationY,
            d.Malfunction = s.Malfunction,
            d.Odometer = s.Odometer,
			d.Origin = s.Origin,
            d.ParentId = s.ParentId,
            d.Sequence = s.Sequence,
            d.State = s.State,
            d.Status = s.Status,
            d.UserHosRuleSet = s.UserHosRuleSet,
			d.VerifyDateTime = s.VerifyDateTime,
            d.[Version] = s.[Version],
            d.RecordCreationTimeUtc = s.RecordCreationTimeUtc
		WHEN NOT MATCHED BY TARGET THEN
			-- Inserts new records AND records whose DateTime changed (deleted above).
			INSERT (
                id,
				GeotabId,
				Annotations, 
				CoDrivers, 
                [DateTime],
				DeferralMinutes, 
				DeferralStatus,
				-- DeferralStatusId, 
				DeviceId,
				DistanceSinceValidCoordinates,
				DriverId, 
				EditDateTime, 
				EditRequestedByUserId, 
				EngineHours, 
				EventCheckSum,
				EventCode,
				EventRecordStatus,
				EventType,
				IsHidden,
				IsIgnored,
				IsTransitioning,
                Location, 
				LocationX, 
				LocationY, 
				Malfunction, 
				Odometer, 
				Origin,
                ParentId, 
				Sequence, 
				State,
				Status,
				UserHosRuleSet,
				VerifyDateTime,
				[Version],
				RecordCreationTimeUtc
			)
			VALUES (
                s.id,
				s.GeotabId,
				s.Annotations, 
				s.CoDrivers, 
                s.[DateTime],
				s.DeferralMinutes, 
				s.DeferralStatus,
				-- s.DeferralStatusId,
				s.DeviceId,
				s.DistanceSinceValidCoordinates,
				s.DriverId, 
				s.EditDateTime, 
				s.EditRequestedByUserId, 
				s.EngineHours, 
				s.EventCheckSum,
				s.EventCode,
				s.EventRecordStatus,
				s.EventType,
				s.IsHidden,
				s.IsIgnored,
				s.IsTransitioning,
                s.Location, 
				s.LocationX, 
				s.LocationY, 
				s.Malfunction, 
				s.Odometer, 
				s.Origin,
                s.ParentId, 
				s.Sequence, 
				s.State,
				s.Status,
				s.UserHosRuleSet,
				s.VerifyDateTime,
				s.[Version],
				s.RecordCreationTimeUtc
			);

		-- Clear staging table.
		TRUNCATE TABLE dbo.stg_DutyStatusLogs2;
	
		-- Drop temporary tables.
		DROP TABLE IF EXISTS #TMP_MovedRecordIds;
		DROP TABLE IF EXISTS #TMP_DeduplicatedStagingData;
		
		-- Commit transaction if all steps successful.
        COMMIT TRANSACTION;
		
	END TRY
	BEGIN CATCH
		-- Rollback transaction if an error occurred.
        IF @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
			
		-- Ensure temporary table cleanup on error.
		DROP TABLE IF EXISTS #TMP_MovedRecordIds;
		DROP TABLE IF EXISTS #TMP_DeduplicatedStagingData;
		
        -- Rethrow the error.
        THROW;
	END CATCH
END;
GO

-- Grant execute permission to the client user/role
GRANT EXECUTE ON [dbo].[spMerge_stg_DutyStatusLogs2] TO [geotabadapter_client];
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Add a sentinel record to represent "NoUserId".
INSERT INTO [dbo].[Users2] (
    [id], [GeotabId], [ActiveFrom], [ActiveTo], [CompanyGroups], 
	[EmployeeNo], [FirstName], [HosRuleSet], [IsDriver], [LastAccessDate], 
	[LastName], [Name], [EntityStatus], [RecordLastChangedUtc]
)
VALUES (
    -1, 'NoUserId', '1912-06-23 00:00:00', '2099-12-31 00:00:00', NULL, 
	NULL, 'No', NULL, 0, NULL, 
	'User', 'NoUser', 1, '1912-06-23 00:00:00'
);

-- Add a sentinel record to represent "NoDriverId".
INSERT INTO [dbo].[Users2] (
    [id], [GeotabId], [ActiveFrom], [ActiveTo], [CompanyGroups], 
	[EmployeeNo], [FirstName], [HosRuleSet], [IsDriver], [LastAccessDate], 
	[LastName], [Name], [EntityStatus], [RecordLastChangedUtc]
)
VALUES (
    -2, 'NoDriverId', '1912-06-23 00:00:00', '2099-12-31 00:00:00', NULL, 
	NULL, 'No', NULL, 0, NULL, 
	'Driver', 'NoDriver', 1, '1912-06-23 00:00:00'
);

-- Add a sentinel record to represent "UnknownDriverId".
INSERT INTO [dbo].[Users2] (
    [id], [GeotabId], [ActiveFrom], [ActiveTo], [CompanyGroups], 
	[EmployeeNo], [FirstName], [HosRuleSet], [IsDriver], [LastAccessDate], 
	[LastName], [Name], [EntityStatus], [RecordLastChangedUtc]
)
VALUES (
    -3, 'UnknownDriverId', '1912-06-23 00:00:00', '2099-12-31 00:00:00', NULL, 
	NULL, 'Unknown', NULL, 1, NULL, 
	'Driver', 'UnknownDriver', 1, '1912-06-23 00:00:00'
);

-- Add a sentinel record to represent "NoDeviceId".
INSERT INTO [dbo].[Devices2] (
    [id], [GeotabId], [ActiveFrom], [ActiveTo], [Comment],
    [DeviceType], [Groups], [LicensePlate], [LicenseState], [Name],
    [ProductId], [SerialNumber], [VIN], [EntityStatus], [RecordLastChangedUtc],
    [TmpTrailerGeotabId], [TmpTrailerId]
)
VALUES (
    -1, 'NoDeviceId', '1912-06-23 00:00:00', '2099-12-31 00:00:00', NULL,
    'None', NULL, NULL, NULL, 'NoDevice',
    NULL, NULL, NULL, 1, '1912-06-23 00:00:00',
    NULL, NULL
);

-- Add a sentinel record to represent "NoRuleId".
SET IDENTITY_INSERT [dbo].[Rules2] ON;
INSERT INTO [dbo].[Rules2] (
    [id], [GeotabId], [ActiveFrom], [ActiveTo], [BaseType],
    [Comment], [Condition], [Groups], [Name], [Version], 
	[EntityStatus], [RecordLastChangedUtc]
)
VALUES (
    -1, 'NoRuleId', '1912-06-23 00:00:00', '2099-12-31 00:00:00', NULL,
    NULL, NULL, NULL, 'NoRule', 0,
    1, '1912-06-23 00:00:00'
);
SET IDENTITY_INSERT [dbo].[Rules2] OFF;

-- Add a sentinel record to represent "NoZoneId".
INSERT INTO [dbo].[Zones2] (
    [id], [GeotabId], [ActiveFrom], [ActiveTo], [CentroidLatitude],
    [CentroidLongitude], [Comment], [Displayed], [ExternalReference], [Groups],
    [MustIdentifyStops], [Name], [Points], [ZoneTypeIds], [Version],
    [EntityStatus], [RecordLastChangedUtc]
)
VALUES (
    -1, 'NoZoneId', '1912-06-23 00:00:00', '2099-12-31 00:00:00', NULL,
    NULL, NULL, 0, NULL, NULL,
    0, 'NoZone', NULL, 'None', 0,
    1, '1912-06-23 00:00:00'
);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Make ExceptionEvents2.RuleId nullable:
ALTER TABLE [dbo].[ExceptionEvents2]
ALTER COLUMN [RuleId] [bigint] NULL;
/*** [END] Part 2 of 3: Database Upgrades Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO [dbo].[MiddlewareVersionInfo2] ([DatabaseVersion], [RecordCreationTimeUtc])
SELECT UpgradeDatabaseVersion, GETUTCDATE()
FROM #TMP_UpgradeDatabaseVersionTable;
DROP TABLE IF EXISTS #TMP_UpgradeDatabaseVersionTable;
/*** [END] Part 3 of 3: Database Version Update Above ***/  
