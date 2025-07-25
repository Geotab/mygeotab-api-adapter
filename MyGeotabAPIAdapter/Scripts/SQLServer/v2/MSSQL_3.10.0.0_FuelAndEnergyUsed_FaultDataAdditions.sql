-- ================================================================================
-- DATABASE TYPE: SQL Server
-- 
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.9.0.0 to version 3.10.0.0.
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
INSERT INTO #TMP_UpgradeDatabaseVersionTable VALUES ('3.10.0.0');

DECLARE @requiredStartingDatabaseVersion NVARCHAR(50) = '3.9.0.0';
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
-- Add columns for new properties to FaultData2 table:
ALTER TABLE [dbo].[FaultData2]
ADD [EffectOnComponent] nvarchar(MAX) NULL;

ALTER TABLE [dbo].[FaultData2]
ADD [FaultDescription] nvarchar(MAX) NULL;

ALTER TABLE [dbo].[FaultData2]
ADD [FlashCodeId] nvarchar(255) NULL;

ALTER TABLE [dbo].[FaultData2]
ADD [FlashCodeName] nvarchar(255) NULL;

ALTER TABLE [dbo].[FaultData2]
ADD [Recommendation] nvarchar(MAX) NULL;

ALTER TABLE [dbo].[FaultData2]
ADD [RiskOfBreakdown] float NULL;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create FuelAndEnergyUsed2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FuelAndEnergyUsed2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeviceId] [bigint] NOT NULL,
	[TotalEnergyUsedKwh] [float] NULL,
	[TotalFuelUsed] [float] NULL,
	[TotalIdlingEnergyUsedKwh] [float] NULL,
	[TotalIdlingFuelUsedL] [float] NULL,
	[Version] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_FuelAndEnergyUsed2] PRIMARY KEY CLUSTERED 
(
	[DateTime] ASC,
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO	

CREATE NONCLUSTERED INDEX [IX_FuelAndEnergyUsed2_DeviceId] ON [dbo].[FuelAndEnergyUsed2]
(
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_FuelAndEnergyUsed2_RecordLastChangedUtc] ON [dbo].[FuelAndEnergyUsed2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_FuelAndEnergyUsed2_DateTime_Device] ON [dbo].[FuelAndEnergyUsed2]
(
	[DateTime] ASC,
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

ALTER TABLE [dbo].[FuelAndEnergyUsed2]  WITH NOCHECK ADD  CONSTRAINT [FK_FuelAndEnergyUsed2_Devices2] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Devices2] ([id])
GO
ALTER TABLE [dbo].[FuelAndEnergyUsed2] CHECK CONSTRAINT [FK_FuelAndEnergyUsed2_Devices2]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_FuelAndEnergyUsed2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_FuelAndEnergyUsed2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeviceId] [bigint] NOT NULL,
	[TotalEnergyUsedKwh] [float] NULL,
	[TotalFuelUsed] [float] NULL,
	[TotalIdlingEnergyUsedKwh] [float] NULL,
	[TotalIdlingFuelUsedL] [float] NULL,
	[Version] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_stg_FuelAndEnergyUsed2_id_RecordLastChangedUtc] ON [dbo].[stg_FuelAndEnergyUsed2]
(
	[id] ASC,
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_FuelAndEnergyUsed2 stored procedure:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_FuelAndEnergyUsed2 staging table to the FuelAndEnergyUsed2
--   table and then truncates the staging table. Handles changes to the DateTime 
--   (partitioning key) by deleting the existing record and inserting the new version.
--
-- Notes:
--  - Uses a multi-step process (DELETE movers + MERGE) within a transaction.
-- ==========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spMerge_stg_FuelAndEnergyUsed2]
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
	FROM dbo.stg_FuelAndEnergyUsed2
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
			FROM dbo.stg_FuelAndEnergyUsed2 stg
		) AS sub
		WHERE sub.rownum = 1;

        -- Identify records where StartTime has changed.
        INSERT INTO #TMP_MovedRecordIds (id)
        SELECT s.id
        FROM #TMP_DeduplicatedStagingData s
        INNER JOIN dbo.FuelAndEnergyUsed2 d ON s.id = d.id
        WHERE s.[DateTime] <> d.[DateTime];

        -- Delete the old versions of these records from the target table.
        DELETE d
        FROM dbo.FuelAndEnergyUsed2 AS d
        INNER JOIN #TMP_MovedRecordIds m ON d.id = m.id;

		-- Perform upsert.
		MERGE INTO dbo.FuelAndEnergyUsed2 AS d
		USING #TMP_DeduplicatedStagingData AS s
		-- id is unique key and logical key for matching. 
		ON d.id = s.id
		WHEN MATCHED AND (
			ISNULL(d.GeotabId, '') <> ISNULL(s.GeotabId, '') OR
			-- DateTime not evaluated because movers were deleted.
			ISNULL(d.DeviceId, -1) <> ISNULL(s.DeviceId, -1) OR
			ISNULL(d.TotalEnergyUsedKwh, -1.0) <> ISNULL(s.TotalEnergyUsedKwh, -1.0) OR
			ISNULL(d.TotalFuelUsed, -1.0) <> ISNULL(s.TotalFuelUsed, -1.0) OR
			ISNULL(d.TotalIdlingEnergyUsedKwh, -1.0) <> ISNULL(s.TotalIdlingEnergyUsedKwh, -1.0) OR
			ISNULL(d.TotalIdlingFuelUsedL, -1.0) <> ISNULL(s.TotalIdlingFuelUsedL, -1.0) OR
			ISNULL(d.[Version], -1) <> ISNULL(s.[Version], -1) 
			-- RecordLastChangedUtc not evaluated as it should never match. 
		)
		THEN UPDATE SET
			d.GeotabId = s.GeotabId,
			d.[DateTime] = s.[DateTime],
			d.DeviceId = s.DeviceId,
			d.TotalEnergyUsedKwh = s.TotalEnergyUsedKwh,
			d.TotalFuelUsed = s.TotalFuelUsed,
			d.TotalIdlingEnergyUsedKwh = s.TotalIdlingEnergyUsedKwh,
			d.TotalIdlingFuelUsedL = s.TotalIdlingFuelUsedL,
			d.[Version] = s.[Version],
			d.RecordLastChangedUtc = s.RecordLastChangedUtc
		WHEN NOT MATCHED BY TARGET THEN
			-- Inserts new records AND records whose DateTime changed (deleted above).
			INSERT (
				id,
				GeotabId,
				[DateTime],
				DeviceId,
				TotalEnergyUsedKwh,
				TotalFuelUsed,
				TotalIdlingEnergyUsedKwh,
				TotalIdlingFuelUsedL,
				[Version],
				RecordLastChangedUtc
			)
			VALUES (
				s.id,
				s.GeotabId,
				s.[DateTime],
				s.DeviceId,
				s.TotalEnergyUsedKwh,
				s.TotalFuelUsed,
				s.TotalIdlingEnergyUsedKwh,
				s.TotalIdlingFuelUsedL,				
				s.[Version],
				s.RecordLastChangedUtc
			);

		-- Clear staging table.
		TRUNCATE TABLE dbo.stg_FuelAndEnergyUsed2;
	
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
GRANT EXECUTE ON [dbo].[spMerge_stg_FuelAndEnergyUsed2] TO [geotabadapter_client];
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
