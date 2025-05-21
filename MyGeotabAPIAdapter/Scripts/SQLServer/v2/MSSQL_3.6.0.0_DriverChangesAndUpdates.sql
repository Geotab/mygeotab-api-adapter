-- ================================================================================
-- DATABASE TYPE: SQL Server
-- 
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.5.0.0 to version 3.6.0.0.
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
INSERT INTO #TMP_UpgradeDatabaseVersionTable VALUES ('3.6.0.0');

DECLARE @requiredStartingDatabaseVersion NVARCHAR(50) = '3.5.0.0';
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
-- Create DriverChanges2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DriverChanges2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeviceId] [bigint] NOT NULL,
	[DriverId] [bigint] NULL,
	[Type] [nvarchar](50) NOT NULL,
	[Version] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DriverChanges2] PRIMARY KEY CLUSTERED 
(
	[DateTime] ASC,
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DriverChanges2_DeviceId] ON [dbo].[DriverChanges2]
(
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_DriverChanges2_DriverId] ON [dbo].[DriverChanges2]
(
	[DriverId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_DriverChanges2_RecordLastChangedUtc] ON [dbo].[DriverChanges2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_DriverChanges2_Type] ON [dbo].[DriverChanges2]
(
	[Type] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_DriverChanges2_DateTime_Device_Type] ON [dbo].[DriverChanges2]
(
	[DateTime] ASC,
	[DeviceId] ASC,
	[Type] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE NONCLUSTERED INDEX [IX_DriverChanges2_DateTime_Driver_Type] ON [dbo].[DriverChanges2]
(
	[DateTime] ASC,
	[DriverId] ASC,
	[Type] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO

CREATE UNIQUE NONCLUSTERED INDEX [UI_DriverChanges2_Id] ON [dbo].[DriverChanges2]
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

ALTER TABLE [dbo].[DriverChanges2]  WITH NOCHECK ADD  CONSTRAINT [FK_DriverChanges2_Devices2] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Devices2] ([id])
GO
ALTER TABLE [dbo].[DriverChanges2] CHECK CONSTRAINT [FK_DriverChanges2_Devices2]
GO
ALTER TABLE [dbo].[DriverChanges2]  WITH NOCHECK ADD  CONSTRAINT [FK_DriverChanges2_Users2] FOREIGN KEY([DriverId])
REFERENCES [dbo].[Users2] ([id])
GO
ALTER TABLE [dbo].[DriverChanges2] CHECK CONSTRAINT [FK_DriverChanges2_Users2]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_DriverChanges2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_DriverChanges2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeviceId] [bigint] NOT NULL,
	[DriverId] [bigint] NULL,
	[Type] [nvarchar](50) NOT NULL,
	[Version] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_stg_DriverChanges2_id_RecordLastChangedUtc] ON [dbo].[stg_DriverChanges2]
(
	[id] ASC,
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_DriverChanges2 stored procedure:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_DriverChanges2 staging table to the DriverChanges2
--   table and then truncates the staging table. Handles changes to the DateTime 
--   (partitioning key) by deleting the existing record and inserting the new version.
--
-- Notes:
--  - Uses a multi-step process (DELETE movers + MERGE) within a transaction.
-- ==========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spMerge_stg_DriverChanges2]
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
	FROM dbo.stg_DriverChanges2
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
			FROM dbo.stg_DriverChanges2 stg
		) AS sub
		WHERE sub.rownum = 1;

        -- Identify records where StartTime has changed.
        INSERT INTO #TMP_MovedRecordIds (id)
        SELECT s.id
        FROM #TMP_DeduplicatedStagingData s
        INNER JOIN dbo.DriverChanges2 d ON s.id = d.id
        WHERE s.[DateTime] <> d.[DateTime];

        -- Delete the old versions of these records from the target table.
        DELETE d
        FROM dbo.ChargeEvents2 AS d
        INNER JOIN #TMP_MovedRecordIds m ON d.id = m.id;

		-- Perform upsert.
		MERGE INTO dbo.DriverChanges2 AS d
		USING #TMP_DeduplicatedStagingData AS s
		-- id is unique key and logical key for matching. 
		ON d.id = s.id
		WHEN MATCHED AND (
			ISNULL(d.GeotabId, '') <> ISNULL(s.GeotabId, '') OR
			-- DateTime not evaluated because movers were deleted.
			ISNULL(d.DeviceId, -1) <> ISNULL(s.DeviceId, -1) OR
			ISNULL(d.DriverId, -1) <> ISNULL(s.DriverId, -1) OR
			ISNULL(d.[Type], '') <> ISNULL(s.[Type], '') OR
			ISNULL(d.[Version], -1) <> ISNULL(s.[Version], -1) 
			-- RecordLastChangedUtc not evaluated as it should never match. 
		)
		THEN UPDATE SET
			d.GeotabId = s.GeotabId,
			d.[DateTime] = s.[DateTime],
			d.DeviceId = s.DeviceId,
			d.DriverId = s.DriverId,
			d.[Type] = s.[Type],
			d.[Version] = s.[Version],
			d.RecordLastChangedUtc = s.RecordLastChangedUtc
		WHEN NOT MATCHED BY TARGET THEN
			-- Inserts new records AND records whose StartTime changed (deleted above).
			INSERT (
				id,
				GeotabId,
				[DateTime],
				DeviceId,
				DriverId,
				[Type],
				[Version],
				RecordLastChangedUtc
			)
			VALUES (
				s.id,
				s.GeotabId,
				s.[DateTime],
				s.DeviceId,
				s.DriverId,
				s.[Type],
				s.[Version],
				s.RecordLastChangedUtc
			);

		-- Clear staging table.
		TRUNCATE TABLE dbo.stg_DriverChanges2;
	
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
GRANT EXECUTE ON [dbo].[spMerge_stg_DriverChanges2] TO [geotabadapter_client];
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Devices2 stored procedure:
-- (Comment additions only)
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
	-- id is unique key and logical key for matching. 
    ON d.id = s.id 
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

-- Grant execute permission to the client user/role
GRANT EXECUTE ON [dbo].[spMerge_stg_Devices2] TO [geotabadapter_client];
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Groups2 stored procedure:
-- (Comment additions only)
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description: 
--		Upserts records from the stg_Groups2 staging table to the Groups2 table and then
--		truncates the staging table. If the @SetEntityStatusDeletedForMissingGroups 
--		parameter is set to 1 (true), the EntityStatus column will be set to 0 (Deleted) for 
--		any records in the Groups2 table for which there are no corresponding records with 
--		the same ids in the stg_Groups2 table.
--
-- Notes:
--		- No transaction used as application should manage the transaction.
-- ==========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spMerge_stg_Groups2]
    @SetEntityStatusDeletedForMissingGroups BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    -- De-duplicate staging table by selecting the latest record per GeotabId (id is 
	-- auto-generated on insert). Note that RecordLastChangedUtc is set in the order in which 
	-- results are retrieved via GetFeed.
    WITH DeduplicatedStaging AS (
        SELECT *
        FROM (
            SELECT *,
                ROW_NUMBER() OVER (PARTITION BY GeotabId ORDER BY RecordLastChangedUtc DESC) AS rownum
            FROM dbo.stg_Groups2
        ) AS sub
        WHERE rownum = 1
    )
    
    -- Perform upsert.
    MERGE INTO dbo.Groups2 AS d
    USING DeduplicatedStaging AS s
	-- id is unique key, but GeotabId is the logical key for matching.
    ON d.GeotabId = s.GeotabId 
    WHEN MATCHED AND (
		-- id not evaluated bacause it is database-generated on insert.
        ISNULL(d.Children, '') <> ISNULL(s.Children, '')
		OR ISNULL(d.Color, '') <> ISNULL(s.Color, '')
		OR ISNULL(d.Comments, '') <> ISNULL(s.Comments, '')
		OR ISNULL(d.Name, '') <> ISNULL(s.Name, '')
		OR ISNULL(d.Reference, '') <> ISNULL(s.Reference, '')
        OR d.EntityStatus <> s.EntityStatus
        -- RecordLastChangedUtc not evaluated as it should never match. 
    )
    THEN UPDATE SET
        d.Children = s.Children,
		d.Color = s.Color,
		d.Comments = s.Comments,
        d.Name = s.Name,
		d.Reference = s.Reference,
        d.EntityStatus = s.EntityStatus,
        d.RecordLastChangedUtc = s.RecordLastChangedUtc
    WHEN NOT MATCHED THEN 
        INSERT (
			-- id is database-generated on insert.
            GeotabId, 
            Children, 
			Color, 
			Comments, 
            Name, 
			Reference, 
            EntityStatus, 
            RecordLastChangedUtc
        )
        VALUES (
			-- id is database-generated on insert.
            s.GeotabId, 
            s.Children, 
			s.Color, 
			s.Comments, 
            s.Name, 
			s.Reference, 
            s.EntityStatus, 
            s.RecordLastChangedUtc
        );
    
    -- If @SetEntityStatusDeletedForMissingGroups is 1 (true), set EntityStatus to 0 (Deleted)
    -- for any records in Groups2 where there is no corresponding record with the same GeotabId
	-- in stg_Groups2.
    IF @SetEntityStatusDeletedForMissingGroups = 1
    BEGIN
        UPDATE d
        SET EntityStatus = 0,
            RecordLastChangedUtc = GETUTCDATE()
        FROM dbo.Groups2 d
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.stg_Groups2 s
            WHERE s.GeotabId = d.GeotabId
        );
    END;
    
    -- Clear staging table.
    TRUNCATE TABLE dbo.stg_Groups2;
END;
GO

-- Grant execute permission to the client user/role
GRANT EXECUTE ON [dbo].[spMerge_stg_Groups2] TO [geotabadapter_client];
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Rules2 stored procedure:
-- (Fixed match on GeotabId vs. id and comment additions)
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description: 
--		Upserts records from the stg_Rules2 staging table to the Rules2 table and then
--		truncates the staging table. 
--
-- Notes:
--		- No transaction used as application should manage the transaction.
-- ==========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spMerge_stg_Rules2]
	@SetEntityStatusDeletedForMissingRules BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- De-duplicate staging table by selecting the latest record per GeotabId (id is 
	-- auto-generated on insert). Note that RecordLastChangedUtc is set in the order in which 
	-- results are retrieved via GetFeed.
    WITH DeduplicatedStaging AS (
        SELECT *
        FROM (
            SELECT *,
                ROW_NUMBER() OVER (PARTITION BY GeotabId ORDER BY RecordLastChangedUtc DESC) AS rownum
            FROM dbo.stg_Rules2
        ) AS sub
        WHERE rownum = 1
    )

	-- Perform upsert.
    MERGE INTO dbo.Rules2 AS d
    USING DeduplicatedStaging AS s
	-- id is unique key, but GeotabId is the logical key for matching.
    ON d.GeotabId = s.GeotabId
    WHEN MATCHED AND (
		-- id not evaluated bacause it is database-generated on insert.
        ISNULL(d.ActiveFrom, '2000-01-01') <> ISNULL(s.ActiveFrom, '2000-01-01')
		OR ISNULL(d.ActiveTo, '2000-01-01') <> ISNULL(s.ActiveTo, '2000-01-01')
		OR ISNULL(d.BaseType, '') <> ISNULL(s.BaseType, '')
		OR ISNULL(d.Comment, '') <> ISNULL(s.Comment, '')
		OR ISNULL(d.[Condition], '') <> ISNULL(s.[Condition], '')
        OR ISNULL(d.Groups, '') <> ISNULL(s.Groups, '')
		OR ISNULL(d.Name, '') <> ISNULL(s.Name, '')
		OR ISNULL(d.Version, -1) <> ISNULL(s.Version, -1)
        OR d.EntityStatus <> s.EntityStatus
        -- RecordLastChangedUtc not evaluated as it should never match. 
    )
    THEN UPDATE SET
        d.ActiveFrom = s.ActiveFrom,
        d.ActiveTo = s.ActiveTo,
        d.BaseType = s.BaseType,
        d.Comment = s.Comment,
		d.[Condition] = s.[Condition],
        d.Groups = s.Groups,
        d.Name = s.Name,
        d.Version = s.Version,
        d.EntityStatus = s.EntityStatus,
        d.RecordLastChangedUtc = s.RecordLastChangedUtc
    WHEN NOT MATCHED THEN 
        INSERT (
			-- id is database-generated on insert.
			GeotabId, 
			ActiveFrom, 
			ActiveTo, 
			BaseType,
			Comment, 
			[Condition], 
			Groups, 
			Name, 
			Version, 
			EntityStatus, 
			RecordLastChangedUtc
		)
        VALUES (
			-- id is database-generated on insert.
			s.GeotabId, 
			s.ActiveFrom, 
			s.ActiveTo, 
			s.BaseType, 
			s.Comment, 
			s.[Condition], 
			s.Groups, 
			s.Name, 
			s.Version,  
			s.EntityStatus, 
			s.RecordLastChangedUtc
		);

    -- If @SetEntityStatusDeletedForMissingRules is 1 (true) set the EntityStatus to 
	-- 0 (Deleted) for any records in the Rules2 table for which there is no corresponding
	-- record with the same GeotabId in the stg_Rules2 table.
    IF @SetEntityStatusDeletedForMissingRules = 1
    BEGIN
		UPDATE d
		SET EntityStatus = 0,
			RecordLastChangedUtc = GETUTCDATE()
		FROM dbo.Rules2 d
		WHERE NOT EXISTS (
			SELECT 1 FROM dbo.stg_Rules2 s
			WHERE s.GeotabId = d.GeotabId
		);
    END;

    -- Clear staging table.
    TRUNCATE TABLE dbo.stg_Rules2;
END;
GO

-- Grant execute permission to the client user/role
GRANT EXECUTE ON [dbo].[spMerge_stg_Rules2] TO [geotabadapter_client];
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Users2 stored procedure:
-- (Comment additions only)
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description: 
--		Upserts records from the stg_Users2 staging table to the Users2 table and then
--		truncates the staging table. If the @SetEntityStatusDeletedForMissingUsers 
--		parameter is set to 1 (true), the EntityStatus column will be set to 0 (Deleted) for 
--		any records in the Users2 table for which there are no corresponding records with 
--		the same ids in the stg_Users2 table.
--
-- Notes:
--		- No transaction used as application should manage the transaction.
-- ==========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spMerge_stg_Users2]
    @SetEntityStatusDeletedForMissingUsers BIT = 0
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
            FROM dbo.stg_Users2
        ) AS sub
        WHERE rownum = 1
    )

    -- Perform upsert.
    MERGE INTO dbo.Users2 AS d
    USING DeduplicatedStaging AS s
	-- id is unique key and logical key for matching. 
    ON d.id = s.id 
    WHEN MATCHED AND (
        d.GeotabId <> s.GeotabId
        OR d.ActiveFrom <> s.ActiveFrom
        OR d.ActiveTo <> s.ActiveTo
		OR ISNULL(d.CompanyGroups, '') <> ISNULL(s.CompanyGroups, '')
		OR ISNULL(d.EmployeeNo, '') <> ISNULL(s.EmployeeNo, '')
		OR ISNULL(d.FirstName, '') <> ISNULL(s.FirstName, '')
		OR ISNULL(d.HosRuleSet, '') <> ISNULL(s.HosRuleSet, '')
        OR d.IsDriver <> s.IsDriver
		OR ISNULL(d.LastAccessDate, '2000-01-01') <> ISNULL(s.LastAccessDate, '2000-01-01')
		OR ISNULL(d.LastName, '') <> ISNULL(s.LastName, '')
        OR d.Name <> s.Name
        OR d.EntityStatus <> s.EntityStatus
        -- RecordLastChangedUtc not evaluated as it should never match. 
	)
	THEN UPDATE SET
        d.GeotabId = s.GeotabId,
        d.ActiveFrom = s.ActiveFrom,
        d.ActiveTo = s.ActiveTo,
		d.CompanyGroups = s.CompanyGroups,
        d.EmployeeNo = s.EmployeeNo,
        d.FirstName = s.FirstName,
        d.HosRuleSet = s.HosRuleSet,
        d.IsDriver = s.IsDriver,
        d.LastAccessDate = s.LastAccessDate,
        d.LastName = s.LastName,
        d.Name = s.Name,
        d.EntityStatus = s.EntityStatus,
        d.RecordLastChangedUtc = s.RecordLastChangedUtc
    WHEN NOT MATCHED THEN 
        INSERT (
            id, 
            GeotabId, 
            ActiveFrom, 
            ActiveTo, 
			CompanyGroups, 
            EmployeeNo, 
            FirstName, 
            HosRuleSet, 
            IsDriver, 
            LastAccessDate, 
            LastName, 
            Name, 
            EntityStatus, 
            RecordLastChangedUtc
        )
        VALUES (
            s.id, 
            s.GeotabId, 
            s.ActiveFrom, 
            s.ActiveTo, 
			s.CompanyGroups, 
            s.EmployeeNo, 
            s.FirstName, 
            s.HosRuleSet, 
            s.IsDriver, 
            s.LastAccessDate, 
            s.LastName, 
            s.Name, 
            s.EntityStatus, 
            s.RecordLastChangedUtc
        );

    -- If @SetEntityStatusDeletedForMissingUsers is 1 (true) set the EntityStatus to 
	-- 0 (Deleted) for any records in the Users2 table for which there is no corresponding
	-- record with the same id in the stg_Users2 table.
    IF @SetEntityStatusDeletedForMissingUsers = 1
    BEGIN
        UPDATE d
        SET EntityStatus = 0,
            RecordLastChangedUtc = GETUTCDATE()
        FROM dbo.Users2 d
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.stg_Users2 s
            WHERE s.id = d.id
        );
    END;

    -- Clear staging table.
    TRUNCATE TABLE dbo.stg_Users2;
END;
GO

-- Grant execute permission to the client user/role
GRANT EXECUTE ON [dbo].[spMerge_stg_Users2] TO [geotabadapter_client];
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Zones2 stored procedure:
-- (Comment additions only)
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description: 
--		Upserts records from the stg_Zones2 staging table to the Zones2 table and then
--		truncates the staging table. If the @SetEntityStatusDeletedForMissingZones 
--		parameter is set to 1 (true), the EntityStatus column will be set to 0 (Deleted) for 
--		any records in the Zones2 table for which there are no corresponding records with 
--		the same ids in the stg_Zones2 table.
--
-- Notes:
--		- No transaction used as application should manage the transaction.
-- ==========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spMerge_stg_Zones2]
	@SetEntityStatusDeletedForMissingZones BIT = 0
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
            FROM dbo.stg_Zones2
        ) AS sub
        WHERE rownum = 1
    )

	-- Perform upsert.
    MERGE INTO dbo.Zones2 AS d
    USING DeduplicatedStaging AS s
	-- id is unique key and logical key for matching. 
    ON d.id = s.id
    WHEN MATCHED AND (
		d.GeotabId <> s.GeotabId
		OR ISNULL(d.ActiveFrom, '2000-01-01') <> ISNULL(s.ActiveFrom, '2000-01-01')
		OR ISNULL(d.ActiveTo, '2000-01-01') <> ISNULL(s.ActiveTo, '2000-01-01')
		OR ISNULL(d.CentroidLatitude, -1.0) <> ISNULL(s.CentroidLatitude, -1.0)
		OR ISNULL(d.CentroidLongitude, -1.0) <> ISNULL(s.CentroidLongitude, -1.0)
		OR ISNULL(d.Comment, '') <> ISNULL(s.Comment, '')
		OR ISNULL(d.Displayed, 0) <> ISNULL(s.Displayed, 0)
		OR ISNULL(d.ExternalReference, '') <> ISNULL(s.ExternalReference, '')
		OR ISNULL(d.Groups, '') <> ISNULL(s.Groups, '')
		OR ISNULL(d.MustIdentifyStops, 0) <> ISNULL(s.MustIdentifyStops, 0)
		OR d.Name <> s.Name
		OR ISNULL(d.Points, '') <> ISNULL(s.Points, '')
		OR d.ZoneTypeIds <> s.ZoneTypeIds
		OR ISNULL(d.Version, -1) <> ISNULL(s.Version, -1)
		OR d.EntityStatus <> s.EntityStatus
		-- RecordLastChangedUtc not evaluated as it should never match. 
	) 
	THEN UPDATE SET
		d.GeotabId = s.GeotabId,
		d.ActiveFrom = s.ActiveFrom,
		d.ActiveTo = s.ActiveTo,
		d.CentroidLatitude = s.CentroidLatitude,
		d.CentroidLongitude = s.CentroidLongitude,
		d.Comment = s.Comment,
		d.Displayed = s.Displayed,
		d.ExternalReference = s.ExternalReference,
		d.Groups = s.Groups,
		d.MustIdentifyStops = s.MustIdentifyStops,			
		d.Name = s.Name,
		d.Points = s.Points,
		d.ZoneTypeIds = s.ZoneTypeIds,
		d.Version = s.Version,
		d.EntityStatus = s.EntityStatus,
		d.RecordLastChangedUtc = s.RecordLastChangedUtc
    WHEN NOT MATCHED THEN 
        INSERT (
			id, 
			GeotabId, 
			ActiveFrom, 
			ActiveTo, 
			CentroidLatitude,
			CentroidLongitude,
			Comment, 
			Displayed, 
			ExternalReference, 
			Groups, 
			MustIdentifyStops, 
			Name, 
			Points, 
			ZoneTypeIds, 
			Version, 
			EntityStatus, 
			RecordLastChangedUtc
		)
        VALUES (
			s.id, 
			s.GeotabId, 
			s.ActiveFrom, 
			s.ActiveTo, 
			s.CentroidLatitude,
			s.CentroidLongitude,
			s.Comment, 
			s.Displayed, 
			s.ExternalReference, 
			s.Groups, 
			s.MustIdentifyStops, 
			s.Name, 
			s.Points, 
			s.ZoneTypeIds, 
			s.Version, 
			s.EntityStatus, 
			s.RecordLastChangedUtc
		);

    -- If @SetEntityStatusDeletedForMissingZones is 1 (true) set the EntityStatus to 
	-- 0 (Deleted) for any records in the Zones2 table for which there is no corresponding
	-- record with the same id in the stg_Zones2 table.
    IF @SetEntityStatusDeletedForMissingZones = 1
    BEGIN
		UPDATE d
		SET EntityStatus = 0,
			RecordLastChangedUtc = GETUTCDATE()
		FROM dbo.Zones2 d
		WHERE NOT EXISTS (
			SELECT 1 FROM dbo.stg_Zones2 s
			WHERE s.id = d.id
		);
    END;

    -- Clear staging table.
    TRUNCATE TABLE dbo.stg_Zones2;
END;
GO

-- Grant execute permission to the client user/role
GRANT EXECUTE ON [dbo].[spMerge_stg_Zones2] TO [geotabadapter_client];
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_ZoneTypes2 stored procedure:
-- (Comment additions only)
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description: 
--		Upserts records from the stg_ZoneTypes2 staging table to the ZoneTypes2 table and then
--		truncates the staging table. If the @SetEntityStatusDeletedForMissingZoneTypes 
--		parameter is set to 1 (true), the EntityStatus column will be set to 0 (Deleted) for 
--		any records in the ZoneTypes2 table for which there are no corresponding records with 
--		the same ids in the stg_ZoneTypes2 table.
--
-- Notes:
--		- No transaction used as application should manage the transaction.
-- ==========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spMerge_stg_ZoneTypes2]
    @SetEntityStatusDeletedForMissingZoneTypes BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    -- De-duplicate staging table by selecting the latest record per GeotabId (id is 
	-- auto-generated on insert). Note that RecordLastChangedUtc is set in the order in which 
	-- results are retrieved via GetFeed.
    WITH DeduplicatedStaging AS (
        SELECT *
        FROM (
            SELECT *,
                ROW_NUMBER() OVER (PARTITION BY GeotabId ORDER BY RecordLastChangedUtc DESC) AS rownum
            FROM dbo.stg_ZoneTypes2
        ) AS sub
        WHERE rownum = 1
    )
    
    -- Perform upsert.
    MERGE INTO dbo.ZoneTypes2 AS d
    USING DeduplicatedStaging AS s
	-- id is unique key, but GeotabId is the logical key for matching.
    ON d.GeotabId = s.GeotabId
    WHEN MATCHED AND (
		-- id not evaluated bacause it is database-generated on insert.
        ISNULL(d.Comment, '') <> ISNULL(s.Comment, '')
        OR d.Name <> s.Name
        OR d.EntityStatus <> s.EntityStatus
        -- RecordLastChangedUtc not evaluated as it should never match. 
    )
    THEN UPDATE SET
        d.Comment = s.Comment,
        d.Name = s.Name,
        d.EntityStatus = s.EntityStatus,
        d.RecordLastChangedUtc = s.RecordLastChangedUtc
    WHEN NOT MATCHED THEN 
        INSERT (
			-- id is database-generated on insert.
            GeotabId, 
            Comment, 
            Name, 
            EntityStatus, 
            RecordLastChangedUtc
        )
        VALUES (
			-- id is database-generated on insert.
            s.GeotabId, 
            s.Comment, 
            s.Name, 
            s.EntityStatus, 
            s.RecordLastChangedUtc
        );
    
    -- If @SetEntityStatusDeletedForMissingZoneTypes is 1 (true), set EntityStatus to 0 (Deleted)
    -- for any records in ZoneTypes2 where there is no corresponding record with the same GeotabId
	-- in stg_ZoneTypes2.
    IF @SetEntityStatusDeletedForMissingZoneTypes = 1
    BEGIN
        UPDATE d
        SET EntityStatus = 0,
            RecordLastChangedUtc = GETUTCDATE()
        FROM dbo.ZoneTypes2 d
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.stg_ZoneTypes2 s
            WHERE s.GeotabId = d.GeotabId
        );
    END;
    
    -- Clear staging table.
    TRUNCATE TABLE dbo.stg_ZoneTypes2;
END;
GO

-- Grant execute permission to the client user/role
GRANT EXECUTE ON [dbo].[spMerge_stg_ZoneTypes2] TO [geotabadapter_client];
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Trips2 stored procedure:
-- (Comment additions only)
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description: 
--		Upserts records from the stg_Trips2 staging table to the Trips2 table and then
--		truncates the staging table. 
--
-- Notes:
--		- No transaction used as application should manage the transaction.
-- ==========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spMerge_stg_Trips2]
AS
BEGIN
    SET NOCOUNT ON;

    -- De-duplicate staging table by selecting the latest record per natural key (DeviceId + Start). 
	-- Note that RecordLastChangedUtc is set in the order in which results are retrieved via GetFeed.
    WITH DeduplicatedStaging AS (
        SELECT *
        FROM (
            SELECT *,
                ROW_NUMBER() OVER (PARTITION BY DeviceId, [Start] ORDER BY RecordLastChangedUtc DESC) AS rownum
            FROM dbo.stg_Trips2
        ) AS sub
        WHERE rownum = 1
    )

	-- Perform upsert.
    MERGE dbo.Trips2 AS d
    USING DeduplicatedStaging AS s
	-- id is unique key, but DeviceId + Start is the logical key for matching.
    ON d.DeviceId = s.DeviceId
       AND d.[Start] = s.[Start]
	WHEN MATCHED AND (
		-- id is database-generated on insert.
		-- GeotabId excluded because it is NOT a unique identifier for a Trip and each update
		-- for a Trip will have a different GeotabId.
		ISNULL(d.AfterHoursDistance, -1.0) <> ISNULL(s.AfterHoursDistance, -1.0)
		OR ISNULL(d.AfterHoursDrivingDurationTicks, -1) <> ISNULL(s.AfterHoursDrivingDurationTicks, -1)
		OR ISNULL(d.AfterHoursEnd, 0) <> ISNULL(s.AfterHoursEnd, 0)
		OR ISNULL(d.AfterHoursStart, 0) <> ISNULL(s.AfterHoursStart, 0)
		OR ISNULL(d.AfterHoursStopDurationTicks, -1) <> ISNULL(s.AfterHoursStopDurationTicks, -1)
		OR ISNULL(d.AverageSpeed, -1.0) <> ISNULL(s.AverageSpeed, -1.0)
		OR ISNULL(d.DeletedDateTime, '2000-01-01') <> ISNULL(s.DeletedDateTime, '2000-01-01')
		OR d.Distance <> s.Distance
		OR ISNULL(d.DriverId, -1) <> ISNULL(s.DriverId, -1)
		OR d.DrivingDurationTicks <> s.DrivingDurationTicks
		OR ISNULL(d.IdlingDurationTicks, -1) <> ISNULL(s.IdlingDurationTicks, -1)
		OR ISNULL(d.MaximumSpeed, -1.0) <> ISNULL(s.MaximumSpeed, -1.0)
		OR d.NextTripStart <> s.NextTripStart
		OR ISNULL(d.SpeedRange1, -1) <> ISNULL(s.SpeedRange1, -1)
		OR ISNULL(d.SpeedRange1DurationTicks, -1) <> ISNULL(s.SpeedRange1DurationTicks, -1)
		OR ISNULL(d.SpeedRange2, -1) <> ISNULL(s.SpeedRange2, -1)
		OR ISNULL(d.SpeedRange2DurationTicks, -1) <> ISNULL(s.SpeedRange2DurationTicks, -1)
		OR ISNULL(d.SpeedRange3, -1) <> ISNULL(s.SpeedRange3, -1)
		OR ISNULL(d.SpeedRange3DurationTicks, -1) <> ISNULL(s.SpeedRange3DurationTicks, -1)
		OR d.[Stop] <> s.[Stop]
		OR d.StopDurationTicks <> s.StopDurationTicks
		OR ISNULL(d.StopPointX, -1.0) <> ISNULL(s.StopPointX, -1.0)
		OR ISNULL(d.StopPointY, -1.0) <> ISNULL(s.StopPointY, -1.0)
		OR ISNULL(d.WorkDistance, -1.0) <> ISNULL(s.WorkDistance, -1.0)
		OR ISNULL(d.WorkDrivingDurationTicks, -1) <> ISNULL(s.WorkDrivingDurationTicks, -1)
		OR ISNULL(d.WorkStopDurationTicks, -1) <> ISNULL(s.WorkStopDurationTicks, -1)
		OR d.EntityStatus <> s.EntityStatus
		-- RecordLastChangedUtc not evaluated as it should never match. 
	)   
	THEN UPDATE SET 
		-- GeotabId gets updated to that of the latest update for the subject Trip.
		d.GeotabId = s.GeotabId,
		d.AfterHoursDistance = s.AfterHoursDistance,
		d.AfterHoursDrivingDurationTicks = s.AfterHoursDrivingDurationTicks,
		d.AfterHoursEnd = s.AfterHoursEnd,
		d.AfterHoursStart = s.AfterHoursStart,
		d.AfterHoursStopDurationTicks = s.AfterHoursStopDurationTicks,
		d.AverageSpeed = s.AverageSpeed,
		d.DeletedDateTime = s.DeletedDateTime,
		d.Distance = s.Distance,
		d.DriverId = s.DriverId,
		d.DrivingDurationTicks = s.DrivingDurationTicks,
		d.IdlingDurationTicks = s.IdlingDurationTicks,
		d.MaximumSpeed = s.MaximumSpeed,
		d.NextTripStart = s.NextTripStart,
		d.SpeedRange1 = s.SpeedRange1,
		d.SpeedRange1DurationTicks = s.SpeedRange1DurationTicks,
		d.SpeedRange2 = s.SpeedRange2,
		d.SpeedRange2DurationTicks = s.SpeedRange2DurationTicks,
		d.SpeedRange3 = s.SpeedRange3,
		d.SpeedRange3DurationTicks = s.SpeedRange3DurationTicks,
		d.[Stop] = s.[Stop],
		d.StopDurationTicks = s.StopDurationTicks,
		d.StopPointX = s.StopPointX,
		d.StopPointY = s.StopPointY,
		d.WorkDistance = s.WorkDistance,
		d.WorkDrivingDurationTicks = s.WorkDrivingDurationTicks,
		d.WorkStopDurationTicks = s.WorkStopDurationTicks,
		d.EntityStatus = s.EntityStatus,
		d.RecordLastChangedUtc = s.RecordLastChangedUtc
    WHEN NOT MATCHED THEN
        INSERT (
            GeotabId,
            AfterHoursDistance,
            AfterHoursDrivingDurationTicks,
            AfterHoursEnd,
            AfterHoursStart,
            AfterHoursStopDurationTicks,
            AverageSpeed,
            DeletedDateTime,
            DeviceId,
            Distance,
            DriverId,
            DrivingDurationTicks,
            IdlingDurationTicks,
            MaximumSpeed,
            NextTripStart,
            SpeedRange1,
            SpeedRange1DurationTicks,
            SpeedRange2,
            SpeedRange2DurationTicks,
            SpeedRange3,
            SpeedRange3DurationTicks,
            [Start],
            [Stop],
            StopDurationTicks,
            StopPointX,
            StopPointY,
            WorkDistance,
            WorkDrivingDurationTicks,
            WorkStopDurationTicks,
            EntityStatus,
            RecordLastChangedUtc
        )
        VALUES (
            s.GeotabId,
            s.AfterHoursDistance,
            s.AfterHoursDrivingDurationTicks,
            s.AfterHoursEnd,
            s.AfterHoursStart,
            s.AfterHoursStopDurationTicks,
            s.AverageSpeed,
            s.DeletedDateTime,
            s.DeviceId,
            s.Distance,
            s.DriverId,
            s.DrivingDurationTicks,
            s.IdlingDurationTicks,
            s.MaximumSpeed,
            s.NextTripStart,
            s.SpeedRange1,
            s.SpeedRange1DurationTicks,
            s.SpeedRange2,
            s.SpeedRange2DurationTicks,
            s.SpeedRange3,
            s.SpeedRange3DurationTicks,
            s.[Start],
            s.[Stop],
            s.StopDurationTicks,
            s.StopPointX,
            s.StopPointY,
            s.WorkDistance,
            s.WorkDrivingDurationTicks,
            s.WorkStopDurationTicks,
            s.EntityStatus,
            s.RecordLastChangedUtc
        );
   
    -- Clear staging table.
    TRUNCATE TABLE [dbo].[stg_Trips2];
END
GO

-- Grant execute permission to the client user/role
GRANT EXECUTE ON [dbo].[spMerge_stg_Trips2] TO [geotabadapter_client];
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Re-order columns in PK_ChargeEvents2 to leverage partition pruning:
-- ======================================================================
-- Script to change the Clustered Primary Key for dbo.ChargeEvents2
-- From: (id ASC, StartTime ASC)
-- To:   (StartTime ASC, id ASC)
-- To align with partitioning on StartTime.
-- ======================================================================

-- **********************************************************************
-- ** WARNING: HIGH IMPACT OPERATION! **
-- **********************************************************************
-- 1. BACKUP YOUR DATABASE before running this script.
-- 2. This script rebuilds the clustered index for dbo.ChargeEvents2.
-- 3. Expect SIGNIFICANT DOWNTIME, BLOCKING, resource usage (CPU, IO),
--    and TRANSACTION LOG GROWTH, proportional to table size.
-- 4. Plan and execute during a maintenance window.
-- 5. TEST THOROUGHLY in a non-production environment first.
-- 6. Identify and script any INBOUND Foreign Keys referencing this table
--    (see Step 1 and Step 7).
-- **********************************************************************

BEGIN TRANSACTION;

BEGIN TRY

    -- Step 1: Drop Foreign Key constraints FROM OTHER tables referencing dbo.ChargeEvents2 (if any)
    -- Example:
    -- IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_SomeOtherTable_ChargeEvents2' AND parent_object_id = OBJECT_ID('dbo.SomeOtherTable'))
    -- BEGIN
    --     ALTER TABLE [dbo].[SomeOtherTable] DROP CONSTRAINT [FK_SomeOtherTable_ChargeEvents2];
    --     PRINT 'Dropped FK constraint FK_SomeOtherTable_ChargeEvents2';
    -- END
    -- GO

    -- Step 2: Drop ALL existing Non-Clustered Indexes on dbo.ChargeEvents2.
    --         They need to be rebuilt after the clustered index changes.
    PRINT 'Step 2: Dropping Non-Clustered Indexes...';
    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UI_ChargeEvents2_Id' AND object_id = OBJECT_ID('dbo.ChargeEvents2'))
    BEGIN
        DROP INDEX [UI_ChargeEvents2_Id] ON [dbo].[ChargeEvents2]; -- WITH (ONLINE = OFF); -- ONLINE might be possible depending on edition/context
        PRINT 'Dropped index UI_ChargeEvents2_Id';
    END

    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChargeEvents2_DeviceId' AND object_id = OBJECT_ID('dbo.ChargeEvents2'))
    BEGIN
        DROP INDEX [IX_ChargeEvents2_DeviceId] ON [dbo].[ChargeEvents2]; -- WITH (ONLINE = OFF);
        PRINT 'Dropped index IX_ChargeEvents2_DeviceId';
    END

    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChargeEvents2_RecordLastChangedUtc' AND object_id = OBJECT_ID('dbo.ChargeEvents2'))
    BEGIN
        DROP INDEX [IX_ChargeEvents2_RecordLastChangedUtc] ON [dbo].[ChargeEvents2]; -- WITH (ONLINE = OFF);
        PRINT 'Dropped index IX_ChargeEvents2_RecordLastChangedUtc';
    END

    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChargeEvents2_StartTime_DeviceId' AND object_id = OBJECT_ID('dbo.ChargeEvents2'))
    BEGIN
        DROP INDEX [IX_ChargeEvents2_StartTime_DeviceId] ON [dbo].[ChargeEvents2]; -- WITH (ONLINE = OFF);
        PRINT 'Dropped index IX_ChargeEvents2_StartTime_DeviceId';
    END
    PRINT 'Finished dropping Non-Clustered Indexes.';

    -- Step 3: Drop the existing Clustered Primary Key constraint.
    PRINT 'Step 3: Dropping existing Clustered Primary Key...';
    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_ChargeEvents2' AND object_id = OBJECT_ID('dbo.ChargeEvents2') AND type_desc = 'CLUSTERED')
    BEGIN
        -- Dropping a clustered PK might require WITH (ONLINE = ON, MOVE TO...) syntax depending on LOB data etc.
        -- Basic drop:
        ALTER TABLE [dbo].[ChargeEvents2] DROP CONSTRAINT [PK_ChargeEvents2]; -- WITH (ONLINE = OFF); -- Test ONLINE = ON if needed
        PRINT 'Dropped constraint PK_ChargeEvents2.';
    END
    ELSE
    BEGIN
         PRINT 'Constraint PK_ChargeEvents2 not found or not clustered.';
    END
    PRINT 'Finished dropping Clustered Primary Key.';

    -- Step 4: Add the new Clustered Primary Key constraint with the correct column order.
    --         Ensure it is created ON the partition scheme.
    PRINT 'Step 4: Creating new Clustered Primary Key (StartTime, id)...';
    ALTER TABLE [dbo].[ChargeEvents2] ADD CONSTRAINT [PK_ChargeEvents2] PRIMARY KEY CLUSTERED
    (
        [StartTime] ASC,
        [id] ASC
    )
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF /*, ONLINE = OFF */) -- Test ONLINE = ON if needed & supported
    ON [DateTimePartitionScheme_MyGeotabApiAdapter]([StartTime]); -- Critical: Must be ON the partition scheme
    PRINT 'Finished creating new Clustered Primary Key.';

    -- Step 5: Recreate ALL Non-Clustered Indexes exactly as they were before.
    PRINT 'Step 5: Recreating Non-Clustered Indexes...';
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UI_ChargeEvents2_Id' AND object_id = OBJECT_ID('dbo.ChargeEvents2'))
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX [UI_ChargeEvents2_Id] ON [dbo].[ChargeEvents2]
        (
            [id] ASC
        )
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        ON [PRIMARY]; -- This index enforces global uniqueness for 'id', place on PRIMARY (or other suitable FG)
        PRINT 'Recreated index UI_ChargeEvents2_Id ON [PRIMARY]';
    END

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChargeEvents2_DeviceId' AND object_id = OBJECT_ID('dbo.ChargeEvents2'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_ChargeEvents2_DeviceId] ON [dbo].[ChargeEvents2]
        (
            [DeviceId] ASC
        )
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        ON [PRIMARY];
        PRINT 'Recreated index IX_ChargeEvents2_DeviceId ON [PRIMARY]';
    END

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChargeEvents2_RecordLastChangedUtc' AND object_id = OBJECT_ID('dbo.ChargeEvents2'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_ChargeEvents2_RecordLastChangedUtc] ON [dbo].[ChargeEvents2]
        (
            [RecordLastChangedUtc] ASC
        )
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        ON [PRIMARY];
        PRINT 'Recreated index IX_ChargeEvents2_RecordLastChangedUtc ON [PRIMARY]';
    END

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChargeEvents2_StartTime_DeviceId' AND object_id = OBJECT_ID('dbo.ChargeEvents2'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_ChargeEvents2_StartTime_DeviceId] ON [dbo].[ChargeEvents2]
        (
            [StartTime] ASC,
            [DeviceId] ASC
        )
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        ON [DateTimePartitionScheme_MyGeotabApiAdapter]([StartTime]); -- Critical: This index should be partition aligned
        PRINT 'Recreated index IX_ChargeEvents2_StartTime_DeviceId ON Partition Scheme';
    END
    PRINT 'Finished recreating Non-Clustered Indexes.';

    -- Step 6: Recreate Foreign Key constraints FROM OTHER tables referencing dbo.ChargeEvents2 (if any).
    --         Use the same constraint names as before.
    -- Example:
    -- IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_SomeOtherTable_ChargeEvents2' AND parent_object_id = OBJECT_ID('dbo.SomeOtherTable'))
    -- BEGIN
    --     ALTER TABLE [dbo].[SomeOtherTable] WITH CHECK ADD CONSTRAINT [FK_SomeOtherTable_ChargeEvents2] FOREIGN KEY([ReferencingColumn])
    --     REFERENCES [dbo].[ChargeEvents2] ([id]); -- Or ([StartTime], [id]) if PK is composite in FK definition
    --
    --     ALTER TABLE [dbo].[SomeOtherTable] CHECK CONSTRAINT [FK_SomeOtherTable_ChargeEvents2];
    --     PRINT 'Recreated FK constraint FK_SomeOtherTable_ChargeEvents2';
    -- END

    -- If all steps successful
    PRINT 'All steps completed successfully.';
    COMMIT TRANSACTION;

END TRY
BEGIN CATCH
    -- If any error occurred
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT 'ERROR: An error occurred. Transaction rolled back.';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Procedure: ' + ISNULL(ERROR_PROCEDURE(), '-');
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10));

    -- Rethrow the error for higher-level handling if needed.
    THROW;
END CATCH;
GO

-- Optional: Verify index structure afterwards
-- SELECT
--     i.name AS IndexName,
--     i.type_desc AS IndexType,
--     i.is_primary_key,
--     i.is_unique_constraint,
--     ds.name AS PartitionSchemeOrFileGroupName,
--     ds.type_desc AS DSType,
--     STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS KeyColumns
-- FROM sys.indexes i
-- JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
-- JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
-- JOIN sys.data_spaces ds ON i.data_space_id = ds.data_space_id
-- WHERE i.object_id = OBJECT_ID('dbo.ChargeEvents2')
-- GROUP BY i.name, i.type_desc, i.is_primary_key, i.is_unique_constraint, ds.name, ds.type_desc, i.index_id
-- ORDER BY i.index_id;
-- GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_ChargeEvents2 stored procedure:
-- Modified to handle potential StartTime (partitioning key) changes by deleting the
-- existing record and inserting the new version.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_ChargeEvents2 staging table to the ChargeEvents2
--   table and then truncates the staging table. Handles changes to the StartTime 
--   (partitioning key) by deleting the existing record and inserting the new version.
--
-- Notes:
--  - Uses a multi-step process (DELETE movers + MERGE) within a transaction.
-- ==========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spMerge_stg_ChargeEvents2]
AS
BEGIN
    SET NOCOUNT ON;

	-- Create temporary table to store IDs of any records where StartTime has changed.
	DROP TABLE IF EXISTS #TMP_MovedRecordIds;
	CREATE TABLE #TMP_MovedRecordIds (id uniqueidentifier PRIMARY KEY);
	
	-- Create temporary table to store the de-duplicated staging table data. Add a rownum
	-- column so that it is not necessary to list all columns when populating this table.
	DROP TABLE IF EXISTS #TMP_DeduplicatedStagingData;
	SELECT *, 
		CAST(NULL AS INT) AS rownum  
	INTO #TMP_DeduplicatedStagingData
	FROM dbo.stg_ChargeEvents2
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
			FROM dbo.stg_ChargeEvents2 stg
		) AS sub
		WHERE sub.rownum = 1;
		
        -- Identify records where StartTime has changed.
        INSERT INTO #TMP_MovedRecordIds (id)
        SELECT s.id
        FROM #TMP_DeduplicatedStagingData s
        INNER JOIN dbo.ChargeEvents2 d ON s.id = d.id
        WHERE s.StartTime <> d.StartTime;

        -- Delete the old versions of these records from the target table.
        DELETE d
        FROM dbo.ChargeEvents2 AS d
        INNER JOIN #TMP_MovedRecordIds m ON d.id = m.id;		

		-- Perform upsert.
		MERGE dbo.ChargeEvents2 AS d
		USING #TMP_DeduplicatedStagingData AS s
		-- id is unique key and logical key for matching.
		ON d.id = s.id
		WHEN MATCHED AND (
			d.GeotabId <> s.GeotabId OR
			d.ChargeIsEstimated <> s.ChargeIsEstimated OR
			d.ChargeType <> s.ChargeType OR 
			d.DeviceId <> s.DeviceId OR
			d.DurationTicks <> s.DurationTicks OR
			ISNULL(d.EndStateOfCharge, -1.0) <> ISNULL(s.EndStateOfCharge, -1.0) OR
			ISNULL(d.EnergyConsumedKwh, -1.0) <> ISNULL(s.EnergyConsumedKwh, -1.0) OR
			ISNULL(d.EnergyUsedSinceLastChargeKwh, -1.0) <> ISNULL(s.EnergyUsedSinceLastChargeKwh, -1.0) OR
			ISNULL(d.Latitude, -999.0) <> ISNULL(s.Latitude, -999.0) OR -- Using -999 as placeholder for Lat/Lon
			ISNULL(d.Longitude, -999.0) <> ISNULL(s.Longitude, -999.0) OR
			ISNULL(d.MaxACVoltage, -1.0) <> ISNULL(s.MaxACVoltage, -1.0) OR
			ISNULL(d.MeasuredBatteryEnergyInKwh, -1.0) <> ISNULL(s.MeasuredBatteryEnergyInKwh, -1.0) OR
			ISNULL(d.MeasuredBatteryEnergyOutKwh, -1.0) <> ISNULL(s.MeasuredBatteryEnergyOutKwh, -1.0) OR
			ISNULL(d.MeasuredOnBoardChargerEnergyInKwh, -1.0) <> ISNULL(s.MeasuredOnBoardChargerEnergyInKwh, -1.0) OR
			ISNULL(d.MeasuredOnBoardChargerEnergyOutKwh, -1.0) <> ISNULL(s.MeasuredOnBoardChargerEnergyOutKwh, -1.0) OR
			ISNULL(d.PeakPowerKw, -1.0) <> ISNULL(s.PeakPowerKw, -1.0) OR
			ISNULL(d.StartStateOfCharge, -1.0) <> ISNULL(s.StartStateOfCharge, -1.0) OR
			-- StartTime not evaluated because movers were deleted.
			ISNULL(d.TripStop, '1900-01-01') <> ISNULL(s.TripStop, '1900-01-01') OR
			ISNULL(d.[Version], -1) <> ISNULL(s.[Version], -1)
			-- RecordLastChangedUtc not evaluated as it should never match.
		)
		THEN UPDATE SET
			d.GeotabId = s.GeotabId,
			d.ChargeIsEstimated = s.ChargeIsEstimated,
			d.ChargeType = s.ChargeType,
			d.DeviceId = s.DeviceId,
			d.DurationTicks = s.DurationTicks,
			d.EndStateOfCharge = s.EndStateOfCharge,
			d.EnergyConsumedKwh = s.EnergyConsumedKwh,
			d.EnergyUsedSinceLastChargeKwh = s.EnergyUsedSinceLastChargeKwh,
			d.Latitude = s.Latitude,
			d.Longitude = s.Longitude,
			d.MaxACVoltage = s.MaxACVoltage,
			d.MeasuredBatteryEnergyInKwh = s.MeasuredBatteryEnergyInKwh,
			d.MeasuredBatteryEnergyOutKwh = s.MeasuredBatteryEnergyOutKwh,
			d.MeasuredOnBoardChargerEnergyInKwh = s.MeasuredOnBoardChargerEnergyInKwh,
			d.MeasuredOnBoardChargerEnergyOutKwh = s.MeasuredOnBoardChargerEnergyOutKwh,
			d.PeakPowerKw = s.PeakPowerKw,
			d.StartStateOfCharge = s.StartStateOfCharge,
			d.StartTime = s.StartTime,
			d.TripStop = s.TripStop,
			d.[Version] = s.[Version],
			d.RecordLastChangedUtc = s.RecordLastChangedUtc
		WHEN NOT MATCHED BY TARGET THEN
			-- Inserts new records AND records whose StartTime changed (deleted above).
			INSERT (
				id,
				GeotabId,
				ChargeIsEstimated,
				ChargeType,
				DeviceId,
				DurationTicks,
				EndStateOfCharge,
				EnergyConsumedKwh,
				EnergyUsedSinceLastChargeKwh,
				Latitude,
				Longitude,
				MaxACVoltage,
				MeasuredBatteryEnergyInKwh,
				MeasuredBatteryEnergyOutKwh,
				MeasuredOnBoardChargerEnergyInKwh,
				MeasuredOnBoardChargerEnergyOutKwh,
				PeakPowerKw,
				StartStateOfCharge,
				StartTime,
				TripStop,
				[Version],
				RecordLastChangedUtc
			)
			VALUES (
				s.id,
				s.GeotabId,
				s.ChargeIsEstimated,
				s.ChargeType,
				s.DeviceId,
				s.DurationTicks,
				s.EndStateOfCharge,
				s.EnergyConsumedKwh,
				s.EnergyUsedSinceLastChargeKwh,
				s.Latitude,
				s.Longitude,
				s.MaxACVoltage,
				s.MeasuredBatteryEnergyInKwh,
				s.MeasuredBatteryEnergyOutKwh,
				s.MeasuredOnBoardChargerEnergyInKwh,
				s.MeasuredOnBoardChargerEnergyOutKwh,
				s.PeakPowerKw,
				s.StartStateOfCharge,
				s.StartTime,
				s.TripStop,
				s.[Version],
				s.RecordLastChangedUtc
			);

		-- Clear staging table.
		TRUNCATE TABLE dbo.stg_ChargeEvents2;

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
GRANT EXECUTE ON [dbo].[spMerge_stg_ChargeEvents2] TO [geotabadapter_client];
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Re-order columns in PK_ExceptionEvents2 to leverage partition pruning:
-- ======================================================================
-- Script to change the Clustered Primary Key for dbo.ExceptionEvents2
-- From: (id ASC, ActiveFrom ASC)
-- To:   (ActiveFrom ASC, id ASC)
-- To align with partitioning on ActiveFrom.
-- ======================================================================

-- **********************************************************************
-- ** WARNING: HIGH IMPACT OPERATION! **
-- **********************************************************************
-- 1. BACKUP YOUR DATABASE before running this script.
-- 2. This script rebuilds the clustered index for dbo.ExceptionEvents2.
-- 3. Expect SIGNIFICANT DOWNTIME, BLOCKING, resource usage (CPU, IO),
--    and TRANSACTION LOG GROWTH, proportional to table size.
-- 4. Plan and execute during a maintenance window.
-- 5. TEST THOROUGHLY in a non-production environment first.
-- 6. Identify and script any INBOUND Foreign Keys referencing this table
--    (see Step 1 and Step 7). Outbound FKs are not affected.
-- **********************************************************************

BEGIN TRANSACTION;

BEGIN TRY

    -- Step 1: Drop Foreign Key constraints FROM OTHER tables referencing dbo.ExceptionEvents2 (if any)
    --         You MUST identify these constraints yourself! They are INBOUND FKs.
    -- Example:
    -- IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_SomeOtherTable_ExceptionEvents2' AND parent_object_id = OBJECT_ID('dbo.SomeOtherTable'))
    -- BEGIN
    --     ALTER TABLE [dbo].[SomeOtherTable] DROP CONSTRAINT [FK_SomeOtherTable_ExceptionEvents2];
    --     PRINT 'Dropped FK constraint FK_SomeOtherTable_ExceptionEvents2';
    -- END
    -- GO

    -- Step 2: Drop ALL existing Non-Clustered Indexes on dbo.ExceptionEvents2
    --         They need to be rebuilt after the clustered index changes.
    PRINT 'Step 2: Dropping Non-Clustered Indexes...';
    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UI_ExceptionEvents2_Id' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        DROP INDEX [UI_ExceptionEvents2_Id] ON [dbo].[ExceptionEvents2]; -- WITH (ONLINE = OFF);
        PRINT 'Dropped index UI_ExceptionEvents2_Id';
    END

    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_DeviceId' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        DROP INDEX [IX_ExceptionEvents2_DeviceId] ON [dbo].[ExceptionEvents2]; -- WITH (ONLINE = OFF);
        PRINT 'Dropped index IX_ExceptionEvents2_DeviceId';
    END

    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_DriverId' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        DROP INDEX [IX_ExceptionEvents2_DriverId] ON [dbo].[ExceptionEvents2]; -- WITH (ONLINE = OFF);
        PRINT 'Dropped index IX_ExceptionEvents2_DriverId';
    END

    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_RecordLastChangedUtc' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        DROP INDEX [IX_ExceptionEvents2_RecordLastChangedUtc] ON [dbo].[ExceptionEvents2]; -- WITH (ONLINE = OFF);
        PRINT 'Dropped index IX_ExceptionEvents2_RecordLastChangedUtc';
    END

    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_RuleId' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        DROP INDEX [IX_ExceptionEvents2_RuleId] ON [dbo].[ExceptionEvents2]; -- WITH (ONLINE = OFF);
        PRINT 'Dropped index IX_ExceptionEvents2_RuleId';
    END

    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_State' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        DROP INDEX [IX_ExceptionEvents2_State] ON [dbo].[ExceptionEvents2]; -- WITH (ONLINE = OFF);
        PRINT 'Dropped index IX_ExceptionEvents2_State';
    END

    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_TimeRange' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        DROP INDEX [IX_ExceptionEvents2_TimeRange] ON [dbo].[ExceptionEvents2]; -- WITH (ONLINE = OFF);
        PRINT 'Dropped index IX_ExceptionEvents2_TimeRange';
    END

    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_TimeRange_Driver_State' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        DROP INDEX [IX_ExceptionEvents2_TimeRange_Driver_State] ON [dbo].[ExceptionEvents2]; -- WITH (ONLINE = OFF);
        PRINT 'Dropped index IX_ExceptionEvents2_TimeRange_Driver_State';
    END

    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_TimeRange_Rule_Driver_State' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        DROP INDEX [IX_ExceptionEvents2_TimeRange_Rule_Driver_State] ON [dbo].[ExceptionEvents2]; -- WITH (ONLINE = OFF);
        PRINT 'Dropped index IX_ExceptionEvents2_TimeRange_Rule_Driver_State';
    END

    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_TimeRange_Rule_State' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        DROP INDEX [IX_ExceptionEvents2_TimeRange_Rule_State] ON [dbo].[ExceptionEvents2]; -- WITH (ONLINE = OFF);
        PRINT 'Dropped index IX_ExceptionEvents2_TimeRange_Rule_State';
    END
    PRINT 'Finished dropping Non-Clustered Indexes.';

    -- Step 3: Drop the existing Clustered Primary Key constraint
    PRINT 'Step 3: Dropping existing Clustered Primary Key...';
    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_ExceptionEvents2' AND object_id = OBJECT_ID('dbo.ExceptionEvents2') AND type_desc = 'CLUSTERED')
    BEGIN
        ALTER TABLE [dbo].[ExceptionEvents2] DROP CONSTRAINT [PK_ExceptionEvents2]; -- WITH (ONLINE = OFF); -- Test ONLINE = ON if needed
        PRINT 'Dropped constraint PK_ExceptionEvents2.';
    END
    ELSE
    BEGIN
         PRINT 'Constraint PK_ExceptionEvents2 not found or not clustered.';
    END
    PRINT 'Finished dropping Clustered Primary Key.';

    -- Step 4: Add the new Clustered Primary Key constraint with the correct column order (ActiveFrom, id)
    --         Ensure it is created ON the partition scheme.
    PRINT 'Step 4: Creating new Clustered Primary Key (ActiveFrom, id)...';
    ALTER TABLE [dbo].[ExceptionEvents2] ADD CONSTRAINT [PK_ExceptionEvents2] PRIMARY KEY CLUSTERED
    (
        [ActiveFrom] ASC,
        [id] ASC
    )
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF /*, ONLINE = OFF */) -- Test ONLINE = ON if needed & supported
    ON [DateTimePartitionScheme_MyGeotabApiAdapter]([ActiveFrom]); -- Critical: Must be ON the partition scheme
    PRINT 'Finished creating new Clustered Primary Key.';

    -- Step 5: Recreate ALL Non-Clustered Indexes exactly as they were defined before
    PRINT 'Step 5: Recreating Non-Clustered Indexes...';

    -- Unique index on ID (ON PRIMARY)
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UI_ExceptionEvents2_Id' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX [UI_ExceptionEvents2_Id] ON [dbo].[ExceptionEvents2]
        (   [id] ASC )
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        ON [PRIMARY];
        PRINT 'Recreated index UI_ExceptionEvents2_Id ON [PRIMARY]';
    END

    -- Indexes ON PRIMARY
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_DeviceId' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_DeviceId] ON [dbo].[ExceptionEvents2]
        (   [DeviceId] ASC  )
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        ON [PRIMARY];
        PRINT 'Recreated index IX_ExceptionEvents2_DeviceId ON [PRIMARY]';
    END

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_DriverId' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_DriverId] ON [dbo].[ExceptionEvents2]
        (   [DriverId] ASC  )
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        ON [PRIMARY];
        PRINT 'Recreated index IX_ExceptionEvents2_DriverId ON [PRIMARY]';
    END

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_RecordLastChangedUtc' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_RecordLastChangedUtc] ON [dbo].[ExceptionEvents2]
        (   [RecordLastChangedUtc] ASC  )
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        ON [PRIMARY];
        PRINT 'Recreated index IX_ExceptionEvents2_RecordLastChangedUtc ON [PRIMARY]';
    END

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_RuleId' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_RuleId] ON [dbo].[ExceptionEvents2]
        (   [RuleId] ASC    )
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        ON [PRIMARY];
        PRINT 'Recreated index IX_ExceptionEvents2_RuleId ON [PRIMARY]';
    END

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_State' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_State] ON [dbo].[ExceptionEvents2]
        (   [State] ASC )
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        ON [PRIMARY];
        PRINT 'Recreated index IX_ExceptionEvents2_State ON [PRIMARY]';
    END

    -- Indexes ON the Partition Scheme
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_TimeRange' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_TimeRange] ON [dbo].[ExceptionEvents2]
        (   [ActiveFrom] ASC, [ActiveTo] ASC )
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        ON [DateTimePartitionScheme_MyGeotabApiAdapter]([ActiveFrom]);
        PRINT 'Recreated index IX_ExceptionEvents2_TimeRange ON Partition Scheme';
    END

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_TimeRange_Driver_State' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_TimeRange_Driver_State] ON [dbo].[ExceptionEvents2]
        (   [ActiveFrom] ASC, [ActiveTo] ASC, [DriverId] ASC, [State] ASC )
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        ON [DateTimePartitionScheme_MyGeotabApiAdapter]([ActiveFrom]);
        PRINT 'Recreated index IX_ExceptionEvents2_TimeRange_Driver_State ON Partition Scheme';
    END

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_TimeRange_Rule_Driver_State' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_TimeRange_Rule_Driver_State] ON [dbo].[ExceptionEvents2]
        (   [ActiveFrom] ASC, [ActiveTo] ASC, [RuleId] ASC, [DriverId] ASC, [State] ASC )
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        ON [DateTimePartitionScheme_MyGeotabApiAdapter]([ActiveFrom]);
        PRINT 'Recreated index IX_ExceptionEvents2_TimeRange_Rule_Driver_State ON Partition Scheme';
    END

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExceptionEvents2_TimeRange_Rule_State' AND object_id = OBJECT_ID('dbo.ExceptionEvents2'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_TimeRange_Rule_State] ON [dbo].[ExceptionEvents2]
        (   [ActiveFrom] ASC, [ActiveTo] ASC, [RuleId] ASC, [State] ASC )
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        ON [DateTimePartitionScheme_MyGeotabApiAdapter]([ActiveFrom]);
        PRINT 'Recreated index IX_ExceptionEvents2_TimeRange_Rule_State ON Partition Scheme';
    END

    PRINT 'Finished recreating Non-Clustered Indexes.';

    -- Step 6: Recreate Foreign Key constraints FROM OTHER tables referencing dbo.ExceptionEvents2 (if any)
    --         Use the same constraint names as before.
    -- Example:
    -- IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_SomeOtherTable_ExceptionEvents2' AND parent_object_id = OBJECT_ID('dbo.SomeOtherTable'))
    -- BEGIN
    --     ALTER TABLE [dbo].[SomeOtherTable] WITH CHECK ADD CONSTRAINT [FK_SomeOtherTable_ExceptionEvents2] FOREIGN KEY([ReferencingColumn])
    --     REFERENCES [dbo].[ExceptionEvents2] ([id]); -- Or ([ActiveFrom], [id]) if FK uses composite key
    --
    --     ALTER TABLE [dbo].[SomeOtherTable] CHECK CONSTRAINT [FK_SomeOtherTable_ExceptionEvents2];
    --     PRINT 'Recreated FK constraint FK_SomeOtherTable_ExceptionEvents2';
    -- END

    -- If all steps successful
    PRINT 'All steps completed successfully.';
    COMMIT TRANSACTION;

END TRY
BEGIN CATCH
    -- If any error occurred
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT 'ERROR: An error occurred. Transaction rolled back.';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Procedure: ' + ISNULL(ERROR_PROCEDURE(), '-');
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10));

    -- Rethrow the error for higher-level handling if needed
    THROW;
END CATCH;
GO

-- Optional: Verify index structure afterwards
-- SELECT
--     i.name AS IndexName,
--     i.type_desc AS IndexType,
--     i.is_primary_key,
--     i.is_unique_constraint,
--     ds.name AS PartitionSchemeOrFileGroupName,
--     ds.type_desc AS DSType,
--     STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS KeyColumns
-- FROM sys.indexes i
-- JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
-- JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
-- JOIN sys.data_spaces ds ON i.data_space_id = ds.data_space_id
-- WHERE i.object_id = OBJECT_ID('dbo.ExceptionEvents2')
-- GROUP BY i.name, i.type_desc, i.is_primary_key, i.is_unique_constraint, ds.name, ds.type_desc, i.index_id
-- ORDER BY i.index_id;
-- GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_ExceptionEvents2 stored procedure:
-- Modified to handle potential ActiveFrom (partitioning key) changes by deleting the
-- existing record and inserting the new version.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_ExceptionEvents2 staging table to the ExceptionEvents2
--   table and then truncates the staging table. Handles changes to the ActiveFrom 
--   (partitioning key) by deleting the existing record and inserting the new version.
--
-- Notes:
--  - Uses a multi-step process (DELETE movers + MERGE) within a transaction.
-- ==========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spMerge_stg_ExceptionEvents2]
AS
BEGIN
    SET NOCOUNT ON;

	-- Create temporary table to store IDs of any records where ActiveFrom has changed.
	DROP TABLE IF EXISTS #TMP_MovedRecordIds;
	CREATE TABLE #TMP_MovedRecordIds (id uniqueidentifier PRIMARY KEY);
	
	-- Create temporary table to store the de-duplicated staging table data. Add a rownum
	-- column so that it is not necessary to list all columns when populating this table.
	-- Also add a LookedUpRuleId column for the same reason.
	DROP TABLE IF EXISTS #TMP_DeduplicatedStagingData;
	SELECT *, 
		CAST(NULL AS BIGINT) AS LookedUpRuleId, 
		CAST(NULL AS INT) AS rownum  
	INTO #TMP_DeduplicatedStagingData
	FROM dbo.stg_ExceptionEvents2
	WHERE 1 = 0;

	BEGIN TRY
		BEGIN TRANSACTION;

		-- De-duplicate staging table by selecting the latest record per id.
		-- Uses ROW_NUMBER() partitioned by id, ordering by RecordLastChangedUtc descending. Also 
		-- retrieve RuleId by using the RuleGeotabId to find the corresponding id in the Rules2 table.
		INSERT INTO #TMP_DeduplicatedStagingData
		SELECT *
		FROM (
			SELECT
				stg.*,
				r.id AS LookedUpRuleId,
				ROW_NUMBER() OVER (PARTITION BY stg.id ORDER BY stg.RecordLastChangedUtc DESC) AS rownum
			FROM dbo.stg_ExceptionEvents2 stg
			LEFT JOIN dbo.Rules2 r 
				ON stg.RuleGeotabId = r.GeotabId		
		) AS sub
		WHERE rownum = 1

        -- Identify records where ActiveFrom has changed.
        INSERT INTO #TMP_MovedRecordIds (id)
        SELECT s.id
        FROM #TMP_DeduplicatedStagingData s
        INNER JOIN dbo.ExceptionEvents2 d ON s.id = d.id
        WHERE s.ActiveFrom <> d.ActiveFrom;

        -- Delete the old versions of these records from the target table.
        DELETE d
        FROM dbo.ExceptionEvents2 AS d
        INNER JOIN #TMP_MovedRecordIds m ON d.id = m.id;	

		-- Perform upsert.
		MERGE dbo.ExceptionEvents2 AS d
		USING #TMP_DeduplicatedStagingData AS s
		-- id is unique key and logical key for matching.
		ON d.id = s.id
		WHEN MATCHED AND (
			ISNULL(d.GeotabId, '') <> ISNULL(s.GeotabId, '') OR
			-- ActiveFrom not evaluated because movers were deleted.
			ISNULL(d.ActiveTo, '1900-01-01') <> ISNULL(s.ActiveTo, '1900-01-01') OR
			ISNULL(d.DeviceId, -1) <> ISNULL(s.DeviceId, -1) OR
			ISNULL(d.Distance, -1.0) <> ISNULL(s.Distance, -1.0) OR
			ISNULL(d.DriverId, -1) <> ISNULL(s.DriverId, -1) OR
			ISNULL(d.DurationTicks, -1) <> ISNULL(s.DurationTicks, -1) OR
			ISNULL(d.LastModifiedDateTime, '1900-01-01') <> ISNULL(s.LastModifiedDateTime, '1900-01-01') OR
			ISNULL(d.RuleId, -1) <> ISNULL(s.LookedUpRuleId, -1) OR
			ISNULL(d.[State], -1) <> ISNULL(s.[State], -1) OR
			ISNULL(d.[Version], -1) <> ISNULL(s.[Version], -1)
			-- RecordLastChangedUtc not evaluated as it should never match. 
		)
		THEN UPDATE SET
			d.GeotabId = s.GeotabId,
			d.ActiveFrom = s.ActiveFrom,
			d.ActiveTo = s.ActiveTo,
			d.DeviceId = s.DeviceId,
			d.Distance = s.Distance,
			d.DriverId = s.DriverId,
			d.DurationTicks = s.DurationTicks,
			d.LastModifiedDateTime = s.LastModifiedDateTime,
			d.RuleId = s.LookedUpRuleId,
			d.[State] = s.[State],
			d.[Version] = s.[Version],
			d.RecordLastChangedUtc = s.RecordLastChangedUtc
		WHEN NOT MATCHED BY TARGET THEN
			-- Inserts new records AND records whose ActiveFrom changed (deleted above).
			INSERT (
				id,
				GeotabId,
				ActiveFrom,
				ActiveTo,
				DeviceId,
				Distance,
				DriverId,
				DurationTicks,
				LastModifiedDateTime,
				RuleId,
				[State],
				[Version],
				RecordLastChangedUtc
			)
			VALUES (
				s.id,
				s.GeotabId,
				s.ActiveFrom,
				s.ActiveTo,
				s.DeviceId,
				s.Distance,
				s.DriverId,
				s.DurationTicks,
				s.LastModifiedDateTime,
				s.LookedUpRuleId,
				s.[State],
				s.[Version],
				s.RecordLastChangedUtc
			);

		-- Clear staging table.
		TRUNCATE TABLE dbo.stg_ExceptionEvents2;

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
GRANT EXECUTE ON [dbo].[spMerge_stg_ExceptionEvents2] TO [geotabadapter_client];
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
