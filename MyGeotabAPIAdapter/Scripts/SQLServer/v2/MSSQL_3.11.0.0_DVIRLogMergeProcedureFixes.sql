-- ================================================================================
-- DATABASE TYPE: SQL Server
-- 
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.10.0.0 to version 3.11.0.0.
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
INSERT INTO #TMP_UpgradeDatabaseVersionTable VALUES ('3.11.0.0');

DECLARE @requiredStartingDatabaseVersion NVARCHAR(50) = '3.10.0.0';
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
-- Update spMerge_stg_DVIRDefects2 stored procedure:
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
CREATE OR ALTER PROCEDURE [dbo].[spMerge_stg_DVIRDefects2]
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
		
		-- Save into a temporary table any DVIRDefectRemarks2 records that are children
		-- of DVIRDefects2 records that are moving.
		DROP TABLE IF EXISTS #TMP_DVIRDefectRemarks2ToReattach;
        SELECT r.*
        INTO #TMP_DVIRDefectRemarks2ToReattach
        FROM dbo.DVIRDefectRemarks2 r
        INNER JOIN #TMP_MovedRecordIds m ON r.DVIRDefectId = m.id; 
		
		-- Detach (delete) any DVIRDefectRemarks2 records that are children of DVIRDefects2 records that are moving.
        DELETE r
        FROM dbo.DVIRDefectRemarks2 AS r
        INNER JOIN #TMP_DVIRDefectRemarks2ToReattach m ON r.id = m.id;		

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

        -- Re-attach (insert) any DVIRDefectRemarks2 records that are children of DVIRDefects2 records that moved.
        INSERT INTO dbo.DVIRDefectRemarks2 (
            [id], [GeotabId], [DVIRDefectId], [DVIRLogDateTime], [DateTime], 
            [Remark], [RemarkUserId], [RecordLastChangedUtc]
        )
        SELECT 
            r.id, 
            r.GeotabId, 
            r.DVIRDefectId, 
            s.DVIRLogDateTime, -- Use the NEW partition key from the staged parent
            r.[DateTime],
            r.Remark, 
            r.RemarkUserId, 
            r.RecordLastChangedUtc
        FROM #TMP_DVIRDefectRemarks2ToReattach r
        INNER JOIN #TMP_DeduplicatedStagingData s ON r.DVIRDefectId = s.id;

        -- Clear staging table.
        TRUNCATE TABLE dbo.stg_DVIRDefects2;
    
        -- Drop temporary tables.
        DROP TABLE IF EXISTS #TMP_MovedRecordIds;
        DROP TABLE IF EXISTS #TMP_DeduplicatedStagingData;
		DROP TABLE IF EXISTS #TMP_DVIRDefectRemarks2ToReattach;
        
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
		DROP TABLE IF EXISTS #TMP_DVIRDefectRemarks2ToReattach;
        
        -- Rethrow the error.
        THROW;
    END CATCH
END;
GO

-- Grant execute permission to the client user/role
GRANT EXECUTE ON [dbo].[spMerge_stg_DVIRDefects2] TO [geotabadapter_client];
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_DVIRLogs2 stored procedure:
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
--  - Handles moving child (DVIRDefects2) and grandchild (DVIRDefectRemarks2) records as well
-- 	  to prevent foreign key violations.
-- ==========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spMerge_stg_DVIRLogs2]
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
		
		-- Save into a temporary table any DVIRDefectRemarks2 records that are grandchildren 
		-- of DVIRLogs2 records that are moving.
		DROP TABLE IF EXISTS #TMP_DVIRDefectRemarks2ToReattach;
		SELECT r.*
        INTO #TMP_DVIRDefectRemarks2ToReattach
        FROM dbo.DVIRDefectRemarks2 r
        INNER JOIN dbo.DVIRDefects2 d ON r.DVIRDefectId = d.id
        INNER JOIN #TMP_MovedRecordIds m ON d.DVIRLogId = m.id;
		
		-- Save into a temporary table any DVIRDefects2 records that are children 
		-- of DVIRLogs2 records that are moving.
		DROP TABLE IF EXISTS #TMP_DVIRDefects2ToReattach
		SELECT d.*
        INTO #TMP_DVIRDefects2ToReattach
        FROM dbo.DVIRDefects2 d
        INNER JOIN #TMP_MovedRecordIds m ON d.DVIRLogId = m.id;		
		
		-- Detach (delete) any DVIRDefectRemarks2 records that are grandchildren
		-- of DVIRLogs2 records that are moving.
        DELETE r
        FROM dbo.DVIRDefectRemarks2 AS r
        INNER JOIN #TMP_DVIRDefectRemarks2ToReattach m ON r.id = m.id;

		-- Detach (delete) any DVIRDefects2 records that are children
		-- of DVIRLogs2 records that are moving.
		DELETE d
        FROM dbo.DVIRDefects2 AS d
        INNER JOIN #TMP_DVIRDefects2ToReattach m ON d.id = m.id;

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

		-- Re-attach (insert) any DVIRDefects2 records that are children of DVIRLogs2 records that moved.
        INSERT INTO dbo.DVIRDefects2 (
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
        SELECT
            r.id, 
			r.GeotabId, 
			r.DVIRLogId, 
			s.[DateTime], -- Use the NEW partition key from the staged parent
			r.DefectListAssetType, 
			r.DefectListId,
            r.DefectListName, 
			r.PartId, 
			r.PartName, 
			r.DefectId, 
			r.DefectName, 
			r.DefectSeverityId,
            r.RepairDateTime, 
			r.RepairStatusId, 
			r.RepairUserId, 
			r.RecordLastChangedUtc
        FROM #TMP_DVIRDefects2ToReattach r
        INNER JOIN #TMP_DeduplicatedStagingData s ON r.DVIRLogId = s.id;		

		-- Re-attach (insert) any DVIRDefectRemarks2 records that are grandchildren of DVIRLogs2 records that moved.
        INSERT INTO dbo.DVIRDefectRemarks2 (
            [id], 
			[GeotabId], 
			[DVIRDefectId], 
			[DVIRLogDateTime], 
			[DateTime],
            [Remark], 
			[RemarkUserId], 
			[RecordLastChangedUtc]
        )
        SELECT
            r.id, 
			r.GeotabId, 
			r.DVIRDefectId, 
			s.[DateTime], -- Use the NEW partition key from the staged parent
			r.[DateTime],
            r.Remark, 
			r.RemarkUserId, 
			r.RecordLastChangedUtc
        FROM #TMP_DVIRDefectRemarks2ToReattach r
        INNER JOIN #TMP_DVIRDefects2ToReattach d ON r.DVIRDefectId = d.id
        INNER JOIN #TMP_DeduplicatedStagingData s ON d.DVIRLogId = s.id;

		-- Clear staging table.
		TRUNCATE TABLE dbo.stg_DVIRLogs2;
	
		-- Drop temporary tables.
		DROP TABLE IF EXISTS #TMP_MovedRecordIds;
		DROP TABLE IF EXISTS #TMP_DeduplicatedStagingData;
        DROP TABLE IF EXISTS #TMP_DVIRDefects2ToReattach;
        DROP TABLE IF EXISTS #TMP_DVIRDefectRemarks2ToReattach;
		
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
        DROP TABLE IF EXISTS #TMP_DVIRDefects2ToReattach;
        DROP TABLE IF EXISTS #TMP_DVIRDefectRemarks2ToReattach;
		
        -- Rethrow the error.
        THROW;
	END CATCH
END;
GO

-- Grant execute permission to the client user/role
GRANT EXECUTE ON [dbo].[spMerge_stg_DVIRLogs2] TO [geotabadapter_client];
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
