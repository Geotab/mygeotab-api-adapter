-- ================================================================================
-- DATABASE TYPE: SQL Server
-- 
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.7.0.0 to version 3.8.0.0.
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
INSERT INTO #TMP_UpgradeDatabaseVersionTable VALUES ('3.8.0.0');

DECLARE @requiredStartingDatabaseVersion NVARCHAR(50) = '3.7.0.0';
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
-- Modify Devices2 table (add TmpTrailerId and TmpTrailerGeotabId columns and UI_Devices2_TmpTrailerId index):
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Add a TmpTrailerGeotabId column to the Devices2 table.
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Devices2]') AND name = N'TmpTrailerGeotabId')
BEGIN
    ALTER TABLE [dbo].[Devices2]
    ADD [TmpTrailerGeotabId] [nvarchar](50) NULL;
    PRINT 'Column [TmpTrailerGeotabId] added to [dbo].[Devices2].';
END
ELSE
BEGIN
    PRINT 'Column [TmpTrailerGeotabId] already exists in [dbo].[Devices2].';
END
GO

-- Add a TmpTrailerId column to the Devices2 table.
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Devices2]') AND name = N'TmpTrailerId')
BEGIN
    ALTER TABLE [dbo].[Devices2]
    ADD [TmpTrailerId] [uniqueidentifier] NULL;
    PRINT 'Column [TmpTrailerId] added to [dbo].[Devices2].';
END
ELSE
BEGIN
    PRINT 'Column [TmpTrailerId] already exists in [dbo].[Devices2].';
END
GO

-- Ensure the column TmpTrailerId was added and, if so, add a unique index on it.
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Devices2]') AND name = N'TmpTrailerId')
BEGIN
    PRINT 'Column [TmpTrailerId] does not exist in [dbo].[Devices2]. Please add the column first.';
END
ELSE
BEGIN
    -- Check if the unique index already exists before trying to create it.
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Devices2]') AND name = N'UI_Devices2_TmpTrailerId')
    BEGIN
        -- Create the unique nonclustered index for non-null values.
        CREATE UNIQUE NONCLUSTERED INDEX [UI_Devices2_TmpTrailerId]
        ON [dbo].[Devices2]([TmpTrailerId] ASC)
        WHERE [TmpTrailerId] IS NOT NULL;

        PRINT 'Unique index [UI_Devices2_TmpTrailerId] created on [dbo].[Devices2](TmpTrailerId) for non-null values.';
    END
    ELSE
    BEGIN
        PRINT 'Index [UI_Devices2_TmpTrailerId] already exists on [dbo].[Devices2].';
    END
END
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Modify stg_Devices2 table (add TmpTrailerId and TmpTrailerGeotabId columns):
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Add a TmpTrailerGeotabId column to the stg_Devices2 table.
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[stg_Devices2]') AND name = N'TmpTrailerGeotabId')
BEGIN
    ALTER TABLE [dbo].[stg_Devices2]
    ADD [TmpTrailerGeotabId] [nvarchar](50) NULL;
    PRINT 'Column [TmpTrailerGeotabId] added to [dbo].[stg_Devices2].';
END
ELSE
BEGIN
    PRINT 'Column [TmpTrailerGeotabId] already exists in [dbo].[stg_Devices2].';
END
GO

-- Add a TmpTrailerId column to the stg_Devices2 table.
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[stg_Devices2]') AND name = N'TmpTrailerId')
BEGIN
    ALTER TABLE [dbo].[stg_Devices2]
    ADD [TmpTrailerId] [uniqueidentifier] NULL;
    PRINT 'Column [TmpTrailerId] added to [dbo].[stg_Devices2].';
END
ELSE
BEGIN
    PRINT 'Column [TmpTrailerId] already exists in [dbo].[stg_Devices2].';
END
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Modify spMerge_stg_Devices2 stored procedure (to include new columns):
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description: 
--		Upserts records from the stg_Devices2 staging table to the Devices2 table and then
--		truncates the staging table. If the @SetEntityStatusDeletedForMissingDevices 
--		parameter is set to 1 (true), the EntityStatus column will be set to 0 (Deleted) for 
--		any records in the Devices2 table for which there are no corresponding records with 
--		the same ids in the stg_Devices2 table.
--
-- Notes:
--		- No transaction used as application should manage the transaction.
-- ==========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spMerge_stg_Devices2]
	@SetEntityStatusDeletedForMissingDevices BIT = 0
AS
BEGIN
	SET NOCOUNT ON;

    -- De-duplicate staging table by selecting the latest record per id. Note that 
	-- RecordLastChangedUtc is set in the order in which results are retrieved via GetFeed.
    WITH DeduplicatedStaging AS (
        SELECT *
        FROM (
            SELECT *,
                ROW_NUMBER() OVER (PARTITION BY id ORDER BY RecordLastChangedUtc DESC) AS rownum
            FROM dbo.stg_Devices2
        ) AS sub
        WHERE rownum = 1
    )

	-- Perform upsert.
    MERGE INTO dbo.Devices2 AS d
    USING DeduplicatedStaging AS s
    ON d.id = s.id -- id is unique key and logical key for matching. 
    WHEN MATCHED AND (
        d.GeotabId <> s.GeotabId
		OR ISNULL(d.ActiveFrom, '2000-01-01') <> ISNULL(s.ActiveFrom, '2000-01-01')
		OR ISNULL(d.ActiveTo, '2000-01-01') <> ISNULL(s.ActiveTo, '2000-01-01')
		OR ISNULL(d.Comment, '') <> ISNULL(s.Comment, '')
        OR d.DeviceType <> s.DeviceType
		OR ISNULL(d.Groups, '') <> ISNULL(s.Groups, '')
		OR ISNULL(d.LicensePlate, '') <> ISNULL(s.LicensePlate, '')
		OR ISNULL(d.LicenseState, '') <> ISNULL(s.LicenseState, '')
        OR d.Name <> s.Name
		OR ISNULL(d.ProductId, -1) <> ISNULL(s.ProductId, -1)
		OR ISNULL(d.SerialNumber, '') <> ISNULL(s.SerialNumber, '')
		OR ISNULL(d.VIN, '') <> ISNULL(s.VIN, '')
        OR d.EntityStatus <> s.EntityStatus
        OR ISNULL(d.TmpTrailerGeotabId, '') <> ISNULL(s.TmpTrailerGeotabId, '')
        OR ISNULL(d.TmpTrailerId, CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier)) <> ISNULL(s.TmpTrailerId, CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier))
        -- RecordLastChangedUtc not evaluated as it should never match. 
    )
    THEN UPDATE SET
        d.GeotabId = s.GeotabId,
        d.ActiveFrom = s.ActiveFrom,
        d.ActiveTo = s.ActiveTo,
        d.Comment = s.Comment,
        d.DeviceType = s.DeviceType,
		d.Groups = s.Groups,
        d.LicensePlate = s.LicensePlate,
        d.LicenseState = s.LicenseState,
        d.Name = s.Name,
        d.ProductId = s.ProductId,
        d.SerialNumber = s.SerialNumber,
        d.VIN = s.VIN,
        d.EntityStatus = s.EntityStatus,
        d.TmpTrailerGeotabId = s.TmpTrailerGeotabId,
        d.TmpTrailerId = s.TmpTrailerId,
        d.RecordLastChangedUtc = s.RecordLastChangedUtc
    WHEN NOT MATCHED THEN 
        INSERT (
			id, 
			GeotabId, 
			ActiveFrom, 
			ActiveTo, 
			Comment, 
			DeviceType, 
			Groups, 
			LicensePlate, 
			LicenseState, 
			Name, 
			ProductId, 
			SerialNumber, 
			VIN, 
			EntityStatus, 
            TmpTrailerGeotabId,
            TmpTrailerId,
			RecordLastChangedUtc
		)
        VALUES (
			s.id, 
			s.GeotabId, 
			s.ActiveFrom, 
			s.ActiveTo, 
			s.Comment, 
			s.DeviceType, 
			s.Groups, 
			s.LicensePlate, 
			s.LicenseState, 
			s.Name, 
			s.ProductId, 
			s.SerialNumber, 
			s.VIN, 
			s.EntityStatus, 
            s.TmpTrailerGeotabId,
            s.TmpTrailerId,
			s.RecordLastChangedUtc
		);

    -- If @SetEntityStatusDeletedForMissingDevices is 1 (true) set the EntityStatus to 
	-- 0 (Deleted) for any records in the Devices2 table for which there is no corresponding
	-- record with the same id in the stg_Devices2 table.
    IF @SetEntityStatusDeletedForMissingDevices = 1
    BEGIN
		UPDATE d
		SET EntityStatus = 0,
			RecordLastChangedUtc = GETUTCDATE()
		FROM dbo.Devices2 d
		WHERE NOT EXISTS (
			SELECT 1 FROM dbo.stg_Devices2 s
			WHERE s.id = d.id
		);
    END;

    -- Clear staging table.
    TRUNCATE TABLE dbo.stg_Devices2;
END;
GO

ALTER AUTHORIZATION ON [dbo].[spMerge_stg_Devices2] TO SCHEMA OWNER 
GO
GRANT EXECUTE ON [dbo].[spMerge_stg_Devices2] TO [geotabadapter_client] AS [dbo]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create DVIRLogs2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DVIRLogs2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[AuthorityAddress] [nvarchar](255) NULL,
	[AuthorityName] [nvarchar](255) NULL,
	[CertifiedByUserId] [bigint] NULL,
	[CertifiedDate] [datetime2](7) NULL,
	[CertifyRemark] [nvarchar](max) NULL,	
	[DateTime] [datetime2](7) NOT NULL,
	[DeviceId] [bigint] NOT NULL,
	[DriverId] [bigint] NULL,
	[DriverRemark] [nvarchar](max) NULL,
	[DurationTicks] [bigint] NULL,
	[EngineHours] [real] NULL,
	[IsSafeToOperate] [bit] NULL,
	[LoadHeight] [real] NULL,
	[LoadWidth] [real] NULL,
	[LocationLatitude] [float] NULL,
	[LocationLongitude] [float] NULL,
	[LogType] [nvarchar](50) NULL,	
	[Odometer] [float] NULL,
	[RepairDate] [datetime2](7) NULL,
	[RepairedByUserId] [bigint] NULL,
	[RepairRemark] [nvarchar](max) NULL,
	[Version] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DVIRLogs2] PRIMARY KEY CLUSTERED 
(
	[DateTime] ASC,
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRLogs2_CertifiedByUserId] ON [dbo].[DVIRLogs2]
(
	[CertifiedByUserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRLogs2_DeviceId] ON [dbo].[DVIRLogs2]
(
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRLogs2_DriverId] ON [dbo].[DVIRLogs2]
(
	[DriverId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRLogs2_RepairedByUserId] ON [dbo].[DVIRLogs2]
(
	[RepairedByUserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRLogs2_RecordLastChangedUtc] ON [dbo].[DVIRLogs2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRLogs2_DateTime_CertifiedByUser] ON [dbo].[DVIRLogs2]
(
	[DateTime] ASC,
	[CertifiedByUserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRLogs2_DateTime_Device] ON [dbo].[DVIRLogs2]
(
	[DateTime] ASC,
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRLogs2_DateTime_Driver] ON [dbo].[DVIRLogs2]
(
	[DateTime] ASC,
	[DriverId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRLogs2_DateTime_RepairedByUser] ON [dbo].[DVIRLogs2]
(
	[DateTime] ASC,
	[RepairedByUserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE UNIQUE NONCLUSTERED INDEX [UI_DVIRLogs2_Id] ON [dbo].[DVIRLogs2]
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

ALTER TABLE [dbo].[DVIRLogs2]  WITH NOCHECK ADD  CONSTRAINT [FK_DVIRLogs2_Devices2] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Devices2] ([id])
GO
ALTER TABLE [dbo].[DVIRLogs2] CHECK CONSTRAINT [FK_DVIRLogs2_Devices2]
GO

ALTER TABLE [dbo].[DVIRLogs2]  WITH NOCHECK ADD  CONSTRAINT [FK_DVIRLogs2_Users2] FOREIGN KEY([DriverId])
REFERENCES [dbo].[Users2] ([id])
GO
ALTER TABLE [dbo].[DVIRLogs2] CHECK CONSTRAINT [FK_DVIRLogs2_Users2]
GO

ALTER TABLE [dbo].[DVIRLogs2]  WITH NOCHECK ADD  CONSTRAINT [FK_DVIRLogs2_Users2_2] FOREIGN KEY([CertifiedByUserId])
REFERENCES [dbo].[Users2] ([id])
GO
ALTER TABLE [dbo].[DVIRLogs2] CHECK CONSTRAINT [FK_DVIRLogs2_Users2_2]
GO

ALTER TABLE [dbo].[DVIRLogs2]  WITH NOCHECK ADD  CONSTRAINT [FK_DVIRLogs2_Users2_3] FOREIGN KEY([RepairedByUserId])
REFERENCES [dbo].[Users2] ([id])
GO
ALTER TABLE [dbo].[DVIRLogs2] CHECK CONSTRAINT [FK_DVIRLogs2_Users2_3]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_DVIRLogs2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_DVIRLogs2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[AuthorityAddress] [nvarchar](255) NULL,
	[AuthorityName] [nvarchar](255) NULL,
	[CertifiedByUserId] [bigint] NULL,
	[CertifiedDate] [datetime2](7) NULL,
	[CertifyRemark] [nvarchar](max) NULL,	
	[DateTime] [datetime2](7) NOT NULL,
	[DeviceId] [bigint] NOT NULL,
	[DriverId] [bigint] NULL,
	[DriverRemark] [nvarchar](max) NULL,
	[DurationTicks] [bigint] NULL,
	[EngineHours] [real] NULL,
	[IsSafeToOperate] [bit] NULL,
	[LoadHeight] [real] NULL,
	[LoadWidth] [real] NULL,
	[LocationLatitude] [float] NULL,
	[LocationLongitude] [float] NULL,
	[LogType] [nvarchar](50) NULL,	
	[Odometer] [float] NULL,
	[RepairDate] [datetime2](7) NULL,
	[RepairedByUserId] [bigint] NULL,
	[RepairRemark] [nvarchar](max) NULL,
	[Version] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_stg_DVIRLogs2_id_RecordLastChangedUtc] ON [dbo].[stg_DVIRLogs2]
(
	[id] ASC,
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_DVIRLogs2 stored procedure:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_DVIRLogs2 staging table to the DVIRLogs2
--   table and then truncates the staging table. Handles changes to the DateTime 
--   (partitioning key) by deleting the existing record and inserting the new version.
--
-- Notes:
--  - Uses a multi-step process (DELETE movers + MERGE) within a transaction.
-- ==========================================================================================
CREATE PROCEDURE [dbo].[spMerge_stg_DVIRLogs2]
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
	FROM dbo.stg_DVIRLogs2
	WHERE 1 = 0;

	BEGIN TRY
		BEGIN TRANSACTION;

		-- De-duplicate staging table by selecting the latest record per id.
		-- Uses ROW_NUMBER() partitioned by id, ordering by RecordLastChangedUtc descending.
		INSERT INTO #TMP_DeduplicatedStagingData
		SELECT *
		FROM (
			SELECT
				stg.*,
				ROW_NUMBER() OVER (PARTITION BY stg.id ORDER BY stg.RecordLastChangedUtc DESC) AS rownum
			FROM dbo.stg_DVIRLogs2 stg
		) AS sub
		WHERE sub.rownum = 1;

        -- Identify records where DateTime has changed.
        INSERT INTO #TMP_MovedRecordIds (id)
        SELECT s.id
        FROM #TMP_DeduplicatedStagingData s
        INNER JOIN dbo.DVIRLogs2 d ON s.id = d.id
        WHERE s.[DateTime] <> d.[DateTime];

        -- Delete the old versions of these records from the target table.
        DELETE d
        FROM dbo.DVIRLogs2 AS d
        INNER JOIN #TMP_MovedRecordIds m ON d.id = m.id;

		-- Perform upsert.
		MERGE INTO dbo.DVIRLogs2 AS d
		USING #TMP_DeduplicatedStagingData AS s
		-- id is unique key and logical key for matching. 
		ON d.id = s.id
		WHEN MATCHED AND (
			ISNULL(d.GeotabId, '') <> ISNULL(s.GeotabId, '') OR
			ISNULL(d.AuthorityAddress, '') <> ISNULL(s.AuthorityAddress, '') OR
			ISNULL(d.AuthorityName, '') <> ISNULL(s.AuthorityName, '') OR
			ISNULL(d.CertifiedByUserId, -1) <> ISNULL(s.CertifiedByUserId, -1) OR
			ISNULL(d.CertifiedDate, '1900-01-01') <> ISNULL(s.CertifiedDate, '1900-01-01') OR
			ISNULL(d.CertifyRemark, '') <> ISNULL(s.CertifyRemark, '') OR
			-- DateTime not evaluated because movers were deleted.
			ISNULL(d.DeviceId, -1) <> ISNULL(s.DeviceId, -1) OR
			ISNULL(d.DriverId, -1) <> ISNULL(s.DriverId, -1) OR
			ISNULL(d.DriverRemark, '') <> ISNULL(s.DriverRemark, '') OR
			ISNULL(d.DurationTicks, -1) <> ISNULL(s.DurationTicks, -1) OR
			ISNULL(d.EngineHours, -1.0) <> ISNULL(s.EngineHours, -1.0) OR
			ISNULL(d.IsSafeToOperate, 0) <> ISNULL(s.IsSafeToOperate, 0) OR
			ISNULL(d.LoadHeight, -1.0) <> ISNULL(s.LoadHeight, -1.0) OR
			ISNULL(d.LoadWidth, -1.0) <> ISNULL(s.LoadWidth, -1.0) OR
			ISNULL(d.LocationLatitude, -999.0) <> ISNULL(s.LocationLatitude, -999.0) OR
			ISNULL(d.LocationLongitude, -999.0) <> ISNULL(s.LocationLongitude, -999.0) OR
			ISNULL(d.LogType, '') <> ISNULL(s.LogType, '') OR
			ISNULL(d.Odometer, -999.0) <> ISNULL(s.Odometer, -999.0) OR
			ISNULL(d.RepairDate, '1900-01-01') <> ISNULL(s.RepairDate, '1900-01-01') OR
			ISNULL(d.RepairedByUserId, -1) <> ISNULL(s.RepairedByUserId, -1) OR
			ISNULL(d.RepairRemark, '') <> ISNULL(s.RepairRemark, '') OR
			ISNULL(d.Version, -1) <> ISNULL(s.Version, -1) 
			-- RecordLastChangedUtc not evaluated as it should never match. 
		)
		THEN UPDATE SET
            d.GeotabId = s.GeotabId,
            d.AuthorityAddress = s.AuthorityAddress,
            d.AuthorityName = s.AuthorityName,
            d.CertifiedByUserId = s.CertifiedByUserId,
            d.CertifiedDate = s.CertifiedDate,
            d.CertifyRemark = s.CertifyRemark,
            d.[DateTime] = s.[DateTime],
            d.DeviceId = s.DeviceId,
            d.DriverId = s.DriverId,
            d.DriverRemark = s.DriverRemark,
            d.DurationTicks = s.DurationTicks,
            d.EngineHours = s.EngineHours,
            d.IsSafeToOperate = s.IsSafeToOperate,
            d.LoadHeight = s.LoadHeight,
            d.LoadWidth = s.LoadWidth,
            d.LocationLatitude = s.LocationLatitude,
            d.LocationLongitude = s.LocationLongitude,
            d.LogType = s.LogType,
            d.Odometer = s.Odometer,
            d.RepairDate = s.RepairDate,
            d.RepairedByUserId = s.RepairedByUserId,
            d.RepairRemark = s.RepairRemark,
            d.[Version] = s.[Version],
            d.RecordLastChangedUtc = s.RecordLastChangedUtc
		WHEN NOT MATCHED BY TARGET THEN
			-- Inserts new records AND records whose DateTime changed (deleted above).
			INSERT (
                id, 
				GeotabId, 
				AuthorityAddress, 
				AuthorityName, 
				CertifiedByUserId, 
				CertifiedDate, 
				CertifyRemark,
                [DateTime], 
				DeviceId, 
				DriverId, 
				DriverRemark, 
				DurationTicks, 
				EngineHours, 
				IsSafeToOperate,
                LoadHeight, 
				LoadWidth, 
				LocationLatitude, 
				LocationLongitude, 
				LogType, 
				Odometer, 
				RepairDate,
                RepairedByUserId, 
				RepairRemark, 
				[Version], 
				RecordLastChangedUtc
			)
			VALUES (
                s.id, 
				s.GeotabId, 
				s.AuthorityAddress, 
				s.AuthorityName, 
				s.CertifiedByUserId, 
				s.CertifiedDate, 
				s.CertifyRemark,
                s.[DateTime], 
				s.DeviceId, 
				s.DriverId, 
				s.DriverRemark, 
				s.DurationTicks, 
				s.EngineHours, 
				s.IsSafeToOperate,
                s.LoadHeight, 
				s.LoadWidth, 
				s.LocationLatitude, 
				s.LocationLongitude, 
				s.LogType, 
				s.Odometer, 
				s.RepairDate,
                s.RepairedByUserId, 
				s.RepairRemark, 
				s.[Version], 
				s.RecordLastChangedUtc
			);

		-- Clear staging table.
		TRUNCATE TABLE dbo.stg_DVIRLogs2;
	
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

-- Grant execute permissions to the specified role/user
GRANT EXECUTE ON [dbo].[spMerge_stg_DVIRLogs2] TO [geotabadapter_client];
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create DefectSeverities2 table:
-- Create DefectSeverities2 lookup table and populate it with
-- values of the Geotab.Checkmate.ObjectModel.DefectSeverity enum.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DefectSeverities2](
    [id] [smallint] NOT NULL,
    [Name] [nvarchar](50) NOT NULL,
    CONSTRAINT [PK_DefectSeverities2] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [UK_DefectSeverities2_Name] UNIQUE NONCLUSTERED ([Name] ASC)
) ON [PRIMARY];
GO

INSERT INTO [dbo].[DefectSeverities2] ([id], [Name]) VALUES
(-1, 'Unregulated'),
(0, 'Normal'),
(1, 'Critical');


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create RepairStatuses2 table:
-- Create RepairStatuses2 lookup table and populate it with
-- values of the Geotab.Checkmate.ObjectModel.RepairStatusType enum.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RepairStatuses2](
    [id] [smallint] NOT NULL,
    [Name] [nvarchar](50) NOT NULL,
    CONSTRAINT [PK_RepairStatuses2] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [UK_RepairStatuses2_Name] UNIQUE NONCLUSTERED ([Name] ASC)
) ON [PRIMARY];
GO

INSERT INTO [dbo].[RepairStatuses2] ([id], [Name]) VALUES
(0, 'NotRepaired'),
(1, 'Repaired'),
(2, 'NotNecessary');


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create DVIRDefects2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DVIRDefects2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[DVIRLogId] [uniqueidentifier] NOT NULL,
	[DVIRLogDateTime] [datetime2](7) NOT NULL,
	[DefectListAssetType] [nvarchar](50) NULL,
	[DefectListId] [nvarchar](50) NULL,
	[DefectListName] [nvarchar](255) NULL,
	[PartId] [nvarchar](50) NULL,
	[PartName] [nvarchar](255) NULL,
	[DefectId] [nvarchar](50) NULL,
	[DefectName] [nvarchar](255) NULL,
	[DefectSeverityId] [smallint] NULL,
	[RepairDateTime] [datetime2](7) NULL,
	[RepairStatusId] [smallint] NULL,
	[RepairUserId] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DVIRDefects2] PRIMARY KEY CLUSTERED 
(
	[DVIRLogDateTime] ASC,
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DVIRLogDateTime])
) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DVIRLogDateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRDefects2_DVIRLogId] ON [dbo].[DVIRDefects2]
(
	[DVIRLogId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DVIRLogDateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRDefects2_DefectSeverityId] ON [dbo].[DVIRDefects2]
(
	[DefectSeverityId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DVIRLogDateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRDefects2_RepairStatusId] ON [dbo].[DVIRDefects2]
(
	[RepairStatusId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DVIRLogDateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRDefects2_RepairUserId] ON [dbo].[DVIRDefects2]
(
	[RepairUserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DVIRLogDateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRDefects2_RecordLastChangedUtc] ON [dbo].[DVIRDefects2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DVIRLogDateTime])
GO

CREATE UNIQUE NONCLUSTERED INDEX [UI_DVIRDefects2_Id] ON [dbo].[DVIRDefects2]
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

ALTER TABLE [dbo].[DVIRDefects2]  WITH NOCHECK ADD  CONSTRAINT [FK_DVIRDefects2_DVIRLogs2] FOREIGN KEY([DVIRLogId])
REFERENCES [dbo].[DVIRLogs2] ([id])
GO
ALTER TABLE [dbo].[DVIRDefects2] CHECK CONSTRAINT [FK_DVIRDefects2_DVIRLogs2]
GO

ALTER TABLE [dbo].[DVIRDefects2]  WITH NOCHECK ADD  CONSTRAINT [FK_DVIRDefects2_DefectSeverities2] FOREIGN KEY([DefectSeverityId])
REFERENCES [dbo].[DefectSeverities2] ([id])
GO
ALTER TABLE [dbo].[DVIRDefects2] CHECK CONSTRAINT [FK_DVIRDefects2_DefectSeverities2]
GO

ALTER TABLE [dbo].[DVIRDefects2]  WITH NOCHECK ADD  CONSTRAINT [FK_DVIRDefects2_RepairStatuses2] FOREIGN KEY([RepairStatusId])
REFERENCES [dbo].[RepairStatuses2] ([id])
GO
ALTER TABLE [dbo].[DVIRDefects2] CHECK CONSTRAINT [FK_DVIRDefects2_RepairStatuses2]
GO

ALTER TABLE [dbo].[DVIRDefects2]  WITH NOCHECK ADD  CONSTRAINT [FK_DVIRDefects2_Users2] FOREIGN KEY([RepairUserId])
REFERENCES [dbo].[Users2] ([id])
GO
ALTER TABLE [dbo].[DVIRDefects2] CHECK CONSTRAINT [FK_DVIRDefects2_Users2]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_DVIRDefects2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_DVIRDefects2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[DVIRLogId] [uniqueidentifier] NOT NULL,
	[DVIRLogDateTime] [datetime2](7) NOT NULL,
	[DefectListAssetType] [nvarchar](50) NULL,
	[DefectListId] [nvarchar](50) NULL,
	[DefectListName] [nvarchar](255) NULL,
	[PartId] [nvarchar](50) NULL,
	[PartName] [nvarchar](255) NULL,
	[DefectId] [nvarchar](50) NULL,
	[DefectName] [nvarchar](255) NULL,
	[DefectSeverityId] [smallint] NULL,
	[RepairDateTime] [datetime2](7) NULL,
	[RepairStatusId] [smallint] NULL,
	[RepairUserId] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_stg_DVIRDefects2_id_RecordLastChangedUtc] ON [dbo].[stg_DVIRDefects2]
(
	[id] ASC,
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_DVIRDefects2 stored procedure:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--     Upserts records from the stg_DVIRDefects2 staging table to the DVIRDefects2
--     table and then truncates the staging table. Handles de-duplication of staging records 
--     based on id and RecordLastChangedUtc. Handles changes to the DVIRLogDateTime 
--     (partitioning key) by deleting the existing record and inserting the new version.
--
-- Notes:
--   - Uses a multi-step process (DELETE movers + MERGE) within a transaction.
-- ==========================================================================================
CREATE PROCEDURE [dbo].[spMerge_stg_DVIRDefects2]
AS
BEGIN
    SET NOCOUNT ON;

	-- Create temporary table to store IDs of any records where DVIRLogDateTime has changed.
	DROP TABLE IF EXISTS #TMP_MovedRecordIds;
	CREATE TABLE #TMP_MovedRecordIds (id uniqueidentifier PRIMARY KEY);

	-- Create temporary table to store the de-duplicated staging table data. Add a rownum
	-- column so that it is not necessary to list all columns when populating this table.
	DROP TABLE IF EXISTS #TMP_DeduplicatedStagingData;
	SELECT *, 
		CAST(NULL AS INT) AS rownum  
	INTO #TMP_DeduplicatedStagingData
	FROM dbo.stg_DVIRDefects2
	WHERE 1 = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

		-- De-duplicate staging table by selecting the latest record per id.
		-- Uses ROW_NUMBER() partitioned by id, ordering by RecordLastChangedUtc descending.
		INSERT INTO #TMP_DeduplicatedStagingData
		SELECT *
		FROM (
			SELECT
				stg.*,
				ROW_NUMBER() OVER (PARTITION BY stg.id ORDER BY stg.RecordLastChangedUtc DESC) AS rownum
			FROM dbo.stg_DVIRDefects2 stg
		) AS sub
		WHERE sub.rownum = 1;

        -- Identify records where DVIRLogDateTime has changed.
        INSERT INTO #TMP_MovedRecordIds (id)
        SELECT s.id
        FROM #TMP_DeduplicatedStagingData s
        INNER JOIN dbo.DVIRDefects2 d ON s.id = d.id
        WHERE s.DVIRLogDateTime <> d.DVIRLogDateTime;

        -- Delete the old versions of these records from the target table.
        DELETE d
        FROM dbo.DVIRDefects2 AS d
        INNER JOIN #TMP_MovedRecordIds m ON d.id = m.id;

        -- Perform upsert.
        MERGE INTO dbo.DVIRDefects2 AS d
        USING #TMP_DeduplicatedStagingData AS s
        ON d.id = s.id
        WHEN MATCHED AND (
            ISNULL(d.GeotabId, '') <> ISNULL(s.GeotabId, '') OR
            ISNULL(d.DVIRLogId, '00000000-0000-0000-0000-000000000000') <> ISNULL(s.DVIRLogId, '00000000-0000-0000-0000-000000000000') OR
            -- DVIRLogDateTime not evaluated because movers were deleted.
            ISNULL(d.DefectListAssetType, '') <> ISNULL(s.DefectListAssetType, '') OR
            ISNULL(d.DefectListId, '') <> ISNULL(s.DefectListId, '') OR
            ISNULL(d.DefectListName, '') <> ISNULL(s.DefectListName, '') OR
            ISNULL(d.PartId, '') <> ISNULL(s.PartId, '') OR
            ISNULL(d.PartName, '') <> ISNULL(s.PartName, '') OR
            ISNULL(d.DefectId, '') <> ISNULL(s.DefectId, '') OR
            ISNULL(d.DefectName, '') <> ISNULL(s.DefectName, '') OR
            ISNULL(d.DefectSeverityId, -99) <> ISNULL(s.DefectSeverityId, -99) OR
            ISNULL(d.RepairDateTime, '1900-01-01') <> ISNULL(s.RepairDateTime, '1900-01-01') OR
            ISNULL(d.RepairStatusId, -99) <> ISNULL(s.RepairStatusId, -99) OR
            ISNULL(d.RepairUserId, -1) <> ISNULL(s.RepairUserId, -1)
            -- RecordLastChangedUtc not evaluated as it should never match. 
        )
        THEN UPDATE SET
            d.GeotabId = s.GeotabId,
            d.DVIRLogId = s.DVIRLogId,
            d.DVIRLogDateTime = s.DVIRLogDateTime,
            d.DefectListAssetType = s.DefectListAssetType,
            d.DefectListId = s.DefectListId,
            d.DefectListName = s.DefectListName,
            d.PartId = s.PartId,
            d.PartName = s.PartName,
            d.DefectId = s.DefectId,
            d.DefectName = s.DefectName,
            d.DefectSeverityId = s.DefectSeverityId,
            d.RepairDateTime = s.RepairDateTime,
            d.RepairStatusId = s.RepairStatusId,
            d.RepairUserId = s.RepairUserId,
            d.RecordLastChangedUtc = s.RecordLastChangedUtc
        WHEN NOT MATCHED BY TARGET THEN
			-- Inserts new records AND records whose DVIRLogDateTime changed (deleted above).
            INSERT (
                [id],
                [GeotabId],
                [DVIRLogId],
                [DVIRLogDateTime],
                [DefectListAssetType],
                [DefectListId],
                [DefectListName],
                [PartId],
                [PartName],
                [DefectId],
                [DefectName],
                [DefectSeverityId],
                [RepairDateTime],
                [RepairStatusId],
                [RepairUserId],
                [RecordLastChangedUtc]
            )
            VALUES (
                s.[id],
                s.[GeotabId],
                s.[DVIRLogId],
                s.[DVIRLogDateTime],
                s.[DefectListAssetType],
                s.[DefectListId],
                s.[DefectListName],
                s.[PartId],
                s.[PartName],
                s.[DefectId],
                s.[DefectName],
                s.[DefectSeverityId],
                s.[RepairDateTime],
                s.[RepairStatusId],
                s.[RepairUserId],
                s.[RecordLastChangedUtc]
            );

        -- Clear staging table.
        TRUNCATE TABLE dbo.stg_DVIRDefects2;
    
        -- Drop temporary tables.
        DROP TABLE IF EXISTS #TMP_MovedDefectIds;
        DROP TABLE IF EXISTS #TMP_DeduplicatedStagingData;
        
		-- Commit transaction if all steps successful.
        COMMIT TRANSACTION;
        
    END TRY
    BEGIN CATCH
		-- Rollback transaction if an error occurred.
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
		-- Ensure temporary table cleanup on error.
        DROP TABLE IF EXISTS #TMP_MovedDefectIds;
        DROP TABLE IF EXISTS #TMP_DeduplicatedStagingData;
        
		-- Rethrow the error.
        THROW;
    END CATCH
END;
GO

-- Grant execute permissions to the specified role/user
GRANT EXECUTE ON [dbo].[spMerge_stg_DVIRDefects2] TO [geotabadapter_client];
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create DVIRDefectRemarks2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DVIRDefectRemarks2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[DVIRDefectId] [uniqueidentifier] NOT NULL,
	[DVIRLogDateTime] [datetime2](7) NOT NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[RemarkUserId] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DVIRDefectRemarks2] PRIMARY KEY CLUSTERED 
(
	[DVIRLogDateTime] ASC,
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DVIRLogDateTime])
) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DVIRLogDateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRDefectRemarks2_DVIRDefectId] ON [dbo].[DVIRDefectRemarks2]
(
	[DVIRDefectId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DVIRLogDateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRDefectRemarks2_RemarkUserId] ON [dbo].[DVIRDefectRemarks2]
(
	[RemarkUserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DVIRLogDateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DVIRDefectRemarks2_RecordLastChangedUtc] ON [dbo].[DVIRDefectRemarks2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DVIRLogDateTime])
GO

CREATE UNIQUE NONCLUSTERED INDEX [UI_DVIRDefectRemarks2_Id] ON [dbo].[DVIRDefectRemarks2]
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

ALTER TABLE [dbo].[DVIRDefectRemarks2]  WITH NOCHECK ADD  CONSTRAINT [FK_DVIRDefectRemarks2_DVIRDefects2] FOREIGN KEY([DVIRDefectId])
REFERENCES [dbo].[DVIRDefects2] ([id])
GO
ALTER TABLE [dbo].[DVIRDefectRemarks2] CHECK CONSTRAINT [FK_DVIRDefectRemarks2_DVIRDefects2]
GO

ALTER TABLE [dbo].[DVIRDefectRemarks2]  WITH NOCHECK ADD  CONSTRAINT [FK_DVIRDefectRemarks2_Users2] FOREIGN KEY([RemarkUserId])
REFERENCES [dbo].[Users2] ([id])
GO
ALTER TABLE [dbo].[DVIRDefectRemarks2] CHECK CONSTRAINT [FK_DVIRDefectRemarks2_Users2]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_DVIRDefectRemarks2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_DVIRDefectRemarks2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[DVIRDefectId] [uniqueidentifier] NOT NULL,
	[DVIRLogDateTime] [datetime2](7) NOT NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[RemarkUserId] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_stg_DVIRDefectRemarks2_id_RecordLastChangedUtc] ON [dbo].[stg_DVIRDefectRemarks2]
(
	[id] ASC,
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_DVIRDefectRemarks2 stored procedure:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--     Upserts records from the stg_DVIRDefectRemarks2 staging table to the DVIRDefectRemarks2
--     table and then truncates the staging table. Handles de-duplication of staging records 
--     based on id and RecordLastChangedUtc. Handles changes to the DVIRLogDateTime 
--     (partitioning key) by deleting the existing record and inserting the new version.
--
-- Notes:
--   - Uses a multi-step process (DELETE movers + MERGE) within a transaction.
-- ==========================================================================================
CREATE PROCEDURE [dbo].[spMerge_stg_DVIRDefectRemarks2]
AS
BEGIN
    SET NOCOUNT ON;

    -- Create temporary table to store IDs of any records where DVIRLogDateTime has changed.
    DROP TABLE IF EXISTS #TMP_MovedRecordIds;
    CREATE TABLE #TMP_MovedRecordIds (id uniqueidentifier PRIMARY KEY);

	-- Create temporary table to store the de-duplicated staging table data. Add a rownum
	-- column so that it is not necessary to list all columns when populating this table.
    DROP TABLE IF EXISTS #TMP_DeduplicatedStagingData;
    SELECT *,
        CAST(NULL AS INT) AS rownum
    INTO #TMP_DeduplicatedStagingData
    FROM dbo.stg_DVIRDefectRemarks2
    WHERE 1 = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

		-- De-duplicate staging table by selecting the latest record per id.
		-- Uses ROW_NUMBER() partitioned by id, ordering by RecordLastChangedUtc descending.
        INSERT INTO #TMP_DeduplicatedStagingData
        SELECT *
        FROM (
            SELECT
                stg.*,
                ROW_NUMBER() OVER (PARTITION BY stg.id ORDER BY stg.RecordLastChangedUtc DESC) AS rownum
            FROM dbo.stg_DVIRDefectRemarks2 stg
        ) AS sub
        WHERE sub.rownum = 1;

        -- Identify records where DVIRLogDateTime has changed.
        INSERT INTO #TMP_MovedRecordIds (id)
        SELECT s.id
        FROM #TMP_DeduplicatedStagingData s
        INNER JOIN dbo.DVIRDefectRemarks2 d ON s.id = d.id
        WHERE s.DVIRLogDateTime <> d.DVIRLogDateTime;

        -- Delete the old versions of these records from the target table.
        DELETE d
        FROM dbo.DVIRDefectRemarks2 AS d
        INNER JOIN #TMP_MovedRecordIds m ON d.id = m.id;

        -- Perform upsert.
        MERGE INTO dbo.DVIRDefectRemarks2 AS d
        USING #TMP_DeduplicatedStagingData AS s
        ON d.id = s.id
        WHEN MATCHED AND (
            ISNULL(d.GeotabId, '') <> ISNULL(s.GeotabId, '') OR
            ISNULL(d.DVIRDefectId, '00000000-0000-0000-0000-000000000000') <> ISNULL(s.DVIRDefectId, '00000000-0000-0000-0000-000000000000') OR
            -- DVIRLogDateTime not evaluated because movers were deleted.
            ISNULL(d.[DateTime], '1900-01-01') <> ISNULL(s.[DateTime], '1900-01-01') OR
			ISNULL(d.Remark, '') <> ISNULL(s.Remark, '') OR
            ISNULL(d.RemarkUserId, -1) <> ISNULL(s.RemarkUserId, -1)
            -- RecordLastChangedUtc not evaluated as it should never match. 
        )
        THEN UPDATE SET
            d.GeotabId = s.GeotabId,
            d.DVIRDefectId = s.DVIRDefectId,
            d.DVIRLogDateTime = s.DVIRLogDateTime,
            d.[DateTime] = s.[DateTime],
            d.Remark = s.Remark,
            d.RemarkUserId = s.RemarkUserId,
            d.RecordLastChangedUtc = s.RecordLastChangedUtc
        WHEN NOT MATCHED BY TARGET THEN
            -- Inserts new records AND records whose DVIRLogDateTime changed (deleted above).
            INSERT (
                [id],
                [GeotabId],
                [DVIRDefectId],
                [DVIRLogDateTime],
                [DateTime],
                [Remark],
                [RemarkUserId],
                [RecordLastChangedUtc]
            )
            VALUES (
                s.[id],
                s.[GeotabId],
                s.[DVIRDefectId],
                s.[DVIRLogDateTime],
                s.[DateTime],
                s.[Remark],
                s.[RemarkUserId],
                s.[RecordLastChangedUtc]
            );

        -- Clear staging table.
        TRUNCATE TABLE dbo.stg_DVIRDefectRemarks2;
    
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

-- Grant execute permissions to the specified role/user
GRANT EXECUTE ON [dbo].[spMerge_stg_DVIRDefectRemarks2] TO [geotabadapter_client];
GO
/*** [END] Part 2 of 3: Database Upgrades Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO [dbo].[MiddlewareVersionInfo2] ([DatabaseVersion], [RecordCreationTimeUtc])
SELECT UpgradeDatabaseVersion, GETUTCDATE()
FROM #TMP_UpgradeDatabaseVersionTable;
DROP TABLE IF EXISTS #TMP_UpgradeDatabaseVersionTable;
/*** [END] Part 3 of 3: Database Version Update Above ***/  
