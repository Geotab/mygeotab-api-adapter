-- ================================================================================
-- DATABASE TYPE: SQL Server
--
-- DESCRIPTION:
--   The purpose of this script is to upgrade the MyGeotab API Adapter database
--   from version 5.0.0.0 to version 5.1.0.0.
--
--   Changes:
--   1. Adds the ShipmentLogs2 partitioned table (including the
--      FK_ShipmentLogs2_Devices2 foreign key and supporting indexes) to support the
--      new ShipmentLog feed.
--   2. Adds the stg_ShipmentLogs2 staging table.
--   3. Adds the spMerge_stg_ShipmentLogs2 stored procedure used to upsert staged
--      ShipmentLog records into the ShipmentLogs2 table.
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
INSERT INTO #TMP_UpgradeDatabaseVersionTable VALUES ('5.1.0.0');

DECLARE @requiredStartingDatabaseVersion NVARCHAR(50) = '5.0.0.0';
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
-- Create ShipmentLogs2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShipmentLogs2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[ActiveFrom] [datetime2](7) NULL,
	[ActiveTo] [datetime2](7) NULL,
	[Commodity] [nvarchar](255) NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeviceId] [bigint] NULL,
	[DocumentNumber] [nvarchar](max) NULL,
	[DriverId] [bigint] NULL,
	[ShipperName] [nvarchar](max) NULL,
	[Version] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ShipmentLogs2] PRIMARY KEY CLUSTERED
(
	[DateTime] ASC,
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_ShipmentLogs2_DeviceId] ON [dbo].[ShipmentLogs2]
(
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_ShipmentLogs2_DriverId] ON [dbo].[ShipmentLogs2]
(
	[DriverId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_ShipmentLogs2_RecordLastChangedUtc] ON [dbo].[ShipmentLogs2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_ShipmentLogs2_DateTime_Device] ON [dbo].[ShipmentLogs2]
(
	[DateTime] ASC,
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_ShipmentLogs2_DateTime_Driver] ON [dbo].[ShipmentLogs2]
(
	[DateTime] ASC,
	[DriverId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE UNIQUE NONCLUSTERED INDEX [UI_ShipmentLogs2_Id] ON [dbo].[ShipmentLogs2]
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

ALTER TABLE [dbo].[ShipmentLogs2]  WITH NOCHECK ADD  CONSTRAINT [FK_ShipmentLogs2_Devices2] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Devices2] ([id])
GO
ALTER TABLE [dbo].[ShipmentLogs2] CHECK CONSTRAINT [FK_ShipmentLogs2_Devices2]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_ShipmentLogs2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_ShipmentLogs2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[ActiveFrom] [datetime2](7) NULL,
	[ActiveTo] [datetime2](7) NULL,
	[Commodity] [nvarchar](255) NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeviceId] [bigint] NULL,
	[DocumentNumber] [nvarchar](max) NULL,
	[DriverId] [bigint] NULL,
	[ShipperName] [nvarchar](max) NULL,
	[Version] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_stg_ShipmentLogs2_id_RecordLastChangedUtc] ON [dbo].[stg_ShipmentLogs2]
(
	[id] ASC,
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_ShipmentLogs2 stored procedure:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_ShipmentLogs2 staging table to the ShipmentLogs2
--   table and then truncates the staging table. Handles changes to the DateTime
--   (partitioning key) by deleting the existing record and inserting the new version.
--
-- Notes:
--  - Uses a multi-step process (DELETE movers + MERGE) within a transaction.
-- ==========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spMerge_stg_ShipmentLogs2]
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
	FROM dbo.stg_ShipmentLogs2
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
			FROM dbo.stg_ShipmentLogs2 stg
		) AS sub
		WHERE sub.rownum = 1;

        -- Identify records where DateTime has changed.
        INSERT INTO #TMP_MovedRecordIds (id)
        SELECT s.id
        FROM #TMP_DeduplicatedStagingData s
        INNER JOIN dbo.ShipmentLogs2 d ON s.id = d.id
        WHERE s.[DateTime] <> d.[DateTime];

        -- Delete the old versions of these records from the target table.
        DELETE d
        FROM dbo.ShipmentLogs2 AS d
        INNER JOIN #TMP_MovedRecordIds m ON d.id = m.id;

		-- Perform upsert.
		MERGE INTO dbo.ShipmentLogs2 AS d
		USING #TMP_DeduplicatedStagingData AS s
		-- id is unique key and logical key for matching.
		ON d.id = s.id
		WHEN MATCHED AND (
			ISNULL(d.GeotabId, '') <> ISNULL(s.GeotabId, '') OR
			ISNULL(d.ActiveFrom, '1900-01-01') <> ISNULL(s.ActiveFrom, '1900-01-01') OR
			ISNULL(d.ActiveTo, '1900-01-01') <> ISNULL(s.ActiveTo, '1900-01-01') OR
			ISNULL(d.Commodity, '') <> ISNULL(s.Commodity, '') OR
			-- DateTime not evaluated because movers were deleted.
			ISNULL(d.DeviceId, -1) <> ISNULL(s.DeviceId, -1) OR
			ISNULL(d.DocumentNumber, '') <> ISNULL(s.DocumentNumber, '') OR
			ISNULL(d.DriverId, -1) <> ISNULL(s.DriverId, -1) OR
			ISNULL(d.ShipperName, '') <> ISNULL(s.ShipperName, '') OR
			ISNULL(d.[Version], -1) <> ISNULL(s.[Version], -1)
			-- RecordLastChangedUtc not evaluated as it should never match.
		)
		THEN UPDATE SET
            d.GeotabId = s.GeotabId,
            d.ActiveFrom = s.ActiveFrom,
            d.ActiveTo = s.ActiveTo,
            d.Commodity = s.Commodity,
            d.[DateTime] = s.[DateTime],
            d.DeviceId = s.DeviceId,
            d.DocumentNumber = s.DocumentNumber,
            d.DriverId = s.DriverId,
            d.ShipperName = s.ShipperName,
            d.[Version] = s.[Version],
            d.RecordLastChangedUtc = s.RecordLastChangedUtc
		WHEN NOT MATCHED BY TARGET THEN
			-- Inserts new records AND records whose DateTime changed (deleted above).
			INSERT (
                id,
				GeotabId,
				ActiveFrom,
				ActiveTo,
				Commodity,
                [DateTime],
				DeviceId,
				DocumentNumber,
				DriverId,
				ShipperName,
				[Version],
				RecordLastChangedUtc
			)
			VALUES (
                s.id,
				s.GeotabId,
				s.ActiveFrom,
				s.ActiveTo,
				s.Commodity,
                s.[DateTime],
				s.DeviceId,
				s.DocumentNumber,
				s.DriverId,
				s.ShipperName,
				s.[Version],
				s.RecordLastChangedUtc
			);

		-- Clear staging table.
		TRUNCATE TABLE dbo.stg_ShipmentLogs2;

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
GRANT EXECUTE ON [dbo].[spMerge_stg_ShipmentLogs2] TO [geotabadapter_client];
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
