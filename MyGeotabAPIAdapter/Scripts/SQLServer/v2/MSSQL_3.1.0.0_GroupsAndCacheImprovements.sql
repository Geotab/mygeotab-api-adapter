-- ================================================================================
-- DATABASE TYPE: SQL Server
-- 
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.0.0.0 to version 3.1.0.0.
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
INSERT INTO #TMP_UpgradeDatabaseVersionTable VALUES ('3.1.0.0');

DECLARE @requiredStartingDatabaseVersion NVARCHAR(50) = '3.0.0.0';
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
-- Create Groups2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Groups2](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[Children] [nvarchar](max) NULL,
	[Color] [nvarchar](50) NULL,
	[Comments] [nvarchar](1024) NULL,
	[Name] [nvarchar](255) NULL,
	[Reference] [nvarchar](255) NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Groups2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Groups2_RecordLastChangedUtc] ON [dbo].[Groups2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Add Group-related columns to other tables:
ALTER TABLE [dbo].[Devices2]
ADD [Groups] [nvarchar](max) NULL;

ALTER TABLE [dbo].[Users2]
ADD [CompanyGroups] [nvarchar](max) NULL;

ALTER TABLE [dbo].[Zones2]
ADD [Groups] [nvarchar](max) NULL;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_Devices2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[stg_Devices2](
	[id] [bigint] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[ActiveFrom] [datetime2](7) NULL,
	[ActiveTo] [datetime2](7) NULL,
	[Comment] [nvarchar](1024) NULL,
	[DeviceType] [nvarchar](50) NOT NULL,
	[Groups] [nvarchar](max) NULL,
	[LicensePlate] [nvarchar](50) NULL,
	[LicenseState] [nvarchar](50) NULL,
	[Name] [nvarchar](100) NOT NULL,
	[ProductId] [int] NULL,
	[SerialNumber] [nvarchar](12) NULL,
	[VIN] [nvarchar](50) NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL
) ON [PRIMARY]
GO

CREATE INDEX IX_stg_Devices2_id_RecordLastChangedUtc
ON dbo.stg_Devices2 (id, RecordLastChangedUtc DESC);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_Devices2 stored procedure:
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
        -- OR d.RecordLastChangedUtc <> s.RecordLastChangedUtc
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


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_Diagnostics2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_Diagnostics2](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](100) NOT NULL,
	[GeotabGUIDString] [nvarchar](100) NOT NULL,
	[HasShimId] [bit] NOT NULL,
	[FormerShimGeotabGUIDString] [nvarchar](100) NULL,
	[ControllerId] [nvarchar](100) NULL,
	[DiagnosticCode] [int] NULL,
	[DiagnosticName] [nvarchar](255) NOT NULL,
	[DiagnosticSourceId] [nvarchar](50) NOT NULL,
	[DiagnosticSourceName] [nvarchar](255) NOT NULL,
	[DiagnosticUnitOfMeasureId] [nvarchar](50) NOT NULL,
	[DiagnosticUnitOfMeasureName] [nvarchar](255) NOT NULL,
	[OBD2DTC] [nvarchar](50) NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_stg_Diagnostics2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE INDEX IX_stg_Diagnostics2_GeotabGUIDString_RecordLastChangedUtc
ON dbo.stg_Diagnostics2 (GeotabGUIDString, RecordLastChangedUtc DESC);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_Diagnostics2 stored procedure:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:  
--      Upserts records from the stg_Diagnostics2 staging table to the Diagnostics2 table and 
--		then truncates the staging table. If the @SetEntityStatusDeletedForMissingDiagnostics  
--      parameter is set to 1 (true), the EntityStatus column will be set to 0 (Deleted) for  
--      any records in the Diagnostics2 table for which there are no corresponding records 
--		with the same ids in the stg_Diagnostics2 table. Additionally, inserts into 
--		DiagnosticIds2 for inserts and updates to Diagnostics2 where there isn't already a 
--		record for the subject GeotabGUIDString + GeotabId combination.
--
-- Notes:
--      - No transaction used as application should manage the transaction.
-- ==========================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spMerge_stg_Diagnostics2]
    @SetEntityStatusDeletedForMissingDiagnostics BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Create temporary table to store merge output. Collation specified in case tempdb
	-- collation differs from that of this database.
	DROP TABLE IF EXISTS #TMP_MergeOutput;
    CREATE TABLE #TMP_MergeOutput (
        Action VARCHAR(10) COLLATE SQL_Latin1_General_CP1_CS_AS,
        GeotabGUIDString NVARCHAR(100) COLLATE SQL_Latin1_General_CP1_CS_AS,
		GeotabId NVARCHAR(100) COLLATE SQL_Latin1_General_CP1_CS_AS,
        HasShimId BIT,
        FormerShimGeotabGUIDString NVARCHAR(100) COLLATE SQL_Latin1_General_CP1_CS_AS,
		RecordLastChangedUtc DATETIME2(7) NOT NULL
    );
	CREATE INDEX IX_TMP_MergeOutput_GeotabGUIDString_GeotabId
	ON #TMP_MergeOutput (GeotabGUIDString, GeotabId);

    -- De-duplicate staging table by selecting the latest record per GeotabGUIDString  
    -- (GeotabGUIDString is used to uniquely identify MYG Diagnostics). Note that  
    -- RecordLastChangedUtc is set in the order in which results are retrieved via GetFeed.  
    WITH DeduplicatedStaging AS (
        SELECT *
        FROM (
            SELECT *,
                ROW_NUMBER() OVER (PARTITION BY GeotabGUIDString ORDER BY RecordLastChangedUtc DESC) AS rownum
            FROM dbo.stg_Diagnostics2
        ) AS sub
        WHERE rownum = 1
    )
    -- Perform upsert and store output in temporary table.
    MERGE INTO dbo.Diagnostics2 AS d
    USING DeduplicatedStaging AS s
    ON d.GeotabGUIDString = s.GeotabGUIDString
    WHEN MATCHED AND (
		d.GeotabId <> s.GeotabId
        OR d.HasShimId <> s.HasShimId
        OR ISNULL(d.FormerShimGeotabGUIDString, '') <> ISNULL(s.FormerShimGeotabGUIDString, '')
        OR ISNULL(d.ControllerId, '') <> ISNULL(s.ControllerId, '')
        OR ISNULL(d.DiagnosticCode, -1) <> ISNULL(s.DiagnosticCode, -1)
        OR d.DiagnosticName <> s.DiagnosticName
        OR d.DiagnosticSourceId <> s.DiagnosticSourceId
        OR d.DiagnosticSourceName <> s.DiagnosticSourceName
        OR d.DiagnosticUnitOfMeasureId <> s.DiagnosticUnitOfMeasureId
        OR d.DiagnosticUnitOfMeasureName <> s.DiagnosticUnitOfMeasureName
        OR ISNULL(d.OBD2DTC, '') <> ISNULL(s.OBD2DTC, '')
        OR d.EntityStatus <> s.EntityStatus
		-- OR d.RecordLastChangedUtc = s.RecordLastChangedUtc
    )
    THEN UPDATE SET
		d.GeotabId = s.GeotabId,
        d.HasShimId = s.HasShimId,
        d.FormerShimGeotabGUIDString = s.FormerShimGeotabGUIDString,
        d.ControllerId = s.ControllerId,
        d.DiagnosticCode = s.DiagnosticCode,
        d.DiagnosticName = s.DiagnosticName,
        d.DiagnosticSourceId = s.DiagnosticSourceId,
        d.DiagnosticSourceName = s.DiagnosticSourceName,
        d.DiagnosticUnitOfMeasureId = s.DiagnosticUnitOfMeasureId,
        d.DiagnosticUnitOfMeasureName = s.DiagnosticUnitOfMeasureName,
        d.OBD2DTC = s.OBD2DTC,
        d.EntityStatus = s.EntityStatus,
        d.RecordLastChangedUtc = s.RecordLastChangedUtc
    WHEN NOT MATCHED THEN  
        INSERT (
            GeotabGUIDString,
			GeotabId,
            HasShimId,
            FormerShimGeotabGUIDString,
            ControllerId,
            DiagnosticCode,
            DiagnosticName,
            DiagnosticSourceId,
            DiagnosticSourceName,
            DiagnosticUnitOfMeasureId,
            DiagnosticUnitOfMeasureName,
            OBD2DTC,
            EntityStatus,
            RecordLastChangedUtc
        )
        VALUES (
            s.GeotabGUIDString,
			s.GeotabId, 
            s.HasShimId,
            s.FormerShimGeotabGUIDString,
            s.ControllerId,
            s.DiagnosticCode,
            s.DiagnosticName,
            s.DiagnosticSourceId,
            s.DiagnosticSourceName,
            s.DiagnosticUnitOfMeasureId,
            s.DiagnosticUnitOfMeasureName,
            s.OBD2DTC,
            s.EntityStatus,
            s.RecordLastChangedUtc
        )
    OUTPUT $action, inserted.GeotabGUIDString, s.GeotabId, inserted.HasShimId, 
		inserted.FormerShimGeotabGUIDString, inserted.RecordLastChangedUtc INTO #TMP_MergeOutput;
    
    -- Insert into DiagnosticIds2 for inserts and updates to Diagnostics2 where there isn't 
	-- already a record for the subject GeotabGUIDString + GeotabId combination.
    INSERT INTO dbo.DiagnosticIds2 (GeotabGUIDString, GeotabId, HasShimId, FormerShimGeotabGUIDString, RecordLastChangedUtc)
    SELECT GeotabGUIDString, GeotabId, HasShimId, FormerShimGeotabGUIDString, RecordLastChangedUtc
    FROM #TMP_MergeOutput
    WHERE Action IN ('INSERT', 'UPDATE')
    AND NOT EXISTS (
        SELECT 1
        FROM dbo.DiagnosticIds2 di
        WHERE di.GeotabGUIDString = #TMP_MergeOutput.GeotabGUIDString 
			AND di.GeotabId = #TMP_MergeOutput.GeotabId
    );
    
    -- If @SetEntityStatusDeletedForMissingDiagnostics is 1 (true), set EntityStatus to 0 (Deleted)
    -- for any records in Diagnostics2 where there is no corresponding record with the same GeotabGUIDString
    -- in stg_Diagnostics2.
    IF @SetEntityStatusDeletedForMissingDiagnostics = 1
    BEGIN
        UPDATE d
        SET EntityStatus = 0,
            RecordLastChangedUtc = GETUTCDATE()
        FROM dbo.Diagnostics2 d
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.stg_Diagnostics2 s
            WHERE s.GeotabGUIDString = d.GeotabGUIDString
        );
    END;
    
    -- Clear staging table.
    TRUNCATE TABLE dbo.stg_Diagnostics2;
    
    -- Drop temporary table
    DROP TABLE IF EXISTS #TMP_MergeOutput;
END;
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_Groups2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_Groups2](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[Children] [nvarchar](max) NULL,
	[Color] [nvarchar](50) NULL,
	[Comments] [nvarchar](1024) NULL,
	[Name] [nvarchar](255) NULL,
	[Reference] [nvarchar](255) NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_stg_Groups2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
CREATE INDEX IX_stg_Groups2_GeotabId_RecordLastChangedUtc
ON dbo.stg_Groups2 (GeotabId, RecordLastChangedUtc DESC);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_Users2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_Users2](
	[id] [bigint] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[ActiveFrom] [datetime2](7) NOT NULL,
	[ActiveTo] [datetime2](7) NOT NULL,
	[CompanyGroups] [nvarchar](max) NULL,
	[EmployeeNo] [nvarchar](50) NULL,
	[FirstName] [nvarchar](255) NULL,
	[HosRuleSet] [nvarchar](max) NULL,
	[IsDriver] [bit] NOT NULL,
	[LastAccessDate] [datetime2](7) NULL,
	[LastName] [nvarchar](255) NULL,
	[Name] [nvarchar](255) NOT NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE INDEX IX_stg_Users2_id_RecordLastChangedUtc
ON dbo.stg_Users2 (id, RecordLastChangedUtc DESC);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_Groups2 stored procedure:
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
    ON d.GeotabId = s.GeotabId
    WHEN MATCHED AND (
        ISNULL(d.Children, '') <> ISNULL(s.Children, '')
		OR ISNULL(d.Color, '') <> ISNULL(s.Color, '')
		OR ISNULL(d.Comments, '') <> ISNULL(s.Comments, '')
		OR ISNULL(d.Name, '') <> ISNULL(s.Name, '')
		OR ISNULL(d.Reference, '') <> ISNULL(s.Reference, '')
        OR d.EntityStatus <> s.EntityStatus
        -- OR d.RecordLastChangedUtc = s.RecordLastChangedUtc
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


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_Users2 stored procedure:
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
        -- OR d.RecordLastChangedUtc = s.RecordLastChangedUtc
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


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_ZoneTypes2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[stg_ZoneTypes2](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](100) NOT NULL,
	[Comment] [nvarchar](255) NULL,
	[Name] [nvarchar](255) NOT NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_stg_ZoneTypes2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE INDEX IX_stg_ZoneTypes2_GeotabId_RecordLastChangedUtc
ON dbo.stg_ZoneTypes2 (GeotabId, RecordLastChangedUtc DESC);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_ZoneTypes2 stored procedure:
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
    ON d.GeotabId = s.GeotabId
    WHEN MATCHED AND (
        ISNULL(d.Comment, '') <> ISNULL(s.Comment, '')
        OR d.Name <> s.Name
        OR d.EntityStatus <> s.EntityStatus
        -- OR d.RecordLastChangedUtc = s.RecordLastChangedUtc
    )
    THEN UPDATE SET
        d.Comment = s.Comment,
        d.Name = s.Name,
        d.EntityStatus = s.EntityStatus,
        d.RecordLastChangedUtc = s.RecordLastChangedUtc
    WHEN NOT MATCHED THEN 
        INSERT (
            GeotabId, 
            Comment, 
            Name, 
            EntityStatus, 
            RecordLastChangedUtc
        )
        VALUES (
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


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_Zones2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_Zones2](
	[id] [bigint] NOT NULL,
	[GeotabId] [nvarchar](100) NOT NULL,
	[ActiveFrom] [datetime2](7) NULL,
	[ActiveTo] [datetime2](7) NULL,
	[CentroidLatitude] [float] NULL,
	[CentroidLongitude] [float] NULL,
	[Comment] [nvarchar](500) NULL,
	[Displayed] [bit] NULL,
	[ExternalReference] [nvarchar](255) NULL,
	[Groups] [nvarchar](max) NULL,
	[MustIdentifyStops] [bit] NULL,
	[Name] [nvarchar](255) NOT NULL,
	[Points] [nvarchar](max) NULL,
	[ZoneTypeIds] [nvarchar](max) NOT NULL,
	[Version] [bigint] NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE INDEX IX_stg_Zones2_id_RecordLastChangedUtc
ON dbo.stg_Zones2 (id, RecordLastChangedUtc DESC);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_Zones2 stored procedure:
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
		-- OR d.RecordLastChangedUtc <> s.RecordLastChangedUtc
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



/*** [END] Part 2 of 3: Database Upgrades Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO [dbo].[MiddlewareVersionInfo2] ([DatabaseVersion], [RecordCreationTimeUtc])
SELECT UpgradeDatabaseVersion, GETUTCDATE()
FROM #TMP_UpgradeDatabaseVersionTable;
DROP TABLE IF EXISTS #TMP_UpgradeDatabaseVersionTable;
/*** [END] Part 3 of 3: Database Version Update Above ***/  
