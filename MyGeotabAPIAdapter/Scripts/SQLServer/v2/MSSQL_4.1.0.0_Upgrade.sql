-- ================================================================================
-- DATABASE TYPE: SQL Server
--
-- DESCRIPTION:
--   The purpose of this script is to upgrade the MyGeotab API Adapter database
--   from version 4.0.1.0 to version 4.1.0.0.
--
--   Changes:
--   1. spMerge_stg_Diagnostics2: Added self-heal logic to restore missing
--      DiagnosticIds2 entries for active Diagnostics2 rows, independent of
--      MERGE output. Previously, DiagnosticIds2 rows could only be created
--      when the MERGE produced an INSERT or UPDATE action; if a DiagnosticIds2
--      row was missing but the parent Diagnostics2 row was unchanged, the
--      MERGE would not fire and the DiagnosticIds2 row would never be restored.
--   2. Users2 / stg_Users2: Added Designation column (nvarchar(50), nullable).
--   3. spMerge_stg_Users2: Updated to include Designation in MERGE logic.
--   4. Devices2 / stg_Devices2: Added CustomProperties column (nvarchar(max), nullable).
--   5. spMerge_stg_Devices2: Updated to include CustomProperties in MERGE logic.
--   6. Trips2 / stg_Trips2: Added EngineHours and Odometer columns (float, nullable).
--   7. spMerge_stg_Trips2: Updated to include EngineHours and Odometer in MERGE logic.
--   8. AuditLogs2: Added new partitioned table for Audit feed data.
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
INSERT INTO #TMP_UpgradeDatabaseVersionTable VALUES ('4.1.0.0');

DECLARE @requiredStartingDatabaseVersion NVARCHAR(50) = '4.0.1.0';
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
-- Update spMerge_stg_Diagnostics2 stored procedure:
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
--		record for the subject GeotabGUIDString + GeotabId combination. Also performs
--		self-healing to restore any missing DiagnosticIds2 entries for active Diagnostics2
--		rows.
--
-- Notes:
--      - No transaction used as application should manage the transaction.
-- ==========================================================================================
ALTER   PROCEDURE [dbo].[spMerge_stg_Diagnostics2]
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

    -- Self-heal: Restore missing DiagnosticIds2 entries for any active Diagnostics2
    -- rows. This covers the case where DiagnosticIds2 rows were deleted but the
    -- parent Diagnostics2 rows remain unchanged and thus were not captured by the
    -- MERGE output above.
    INSERT INTO dbo.DiagnosticIds2 (GeotabGUIDString, GeotabId, HasShimId, FormerShimGeotabGUIDString, RecordLastChangedUtc)
    SELECT d.GeotabGUIDString, d.GeotabId, d.HasShimId, d.FormerShimGeotabGUIDString, GETUTCDATE()
    FROM dbo.Diagnostics2 d
    WHERE d.EntityStatus = 1
    AND NOT EXISTS (
        SELECT 1 FROM dbo.DiagnosticIds2 di
        WHERE di.GeotabGUIDString = d.GeotabGUIDString
            AND di.GeotabId = d.GeotabId
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
-- <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Add Designation column to Users2 and stg_Users2:
ALTER TABLE [dbo].[Users2] ADD [Designation] [nvarchar](50) NULL;
GO
ALTER TABLE [dbo].[stg_Users2] ADD [Designation] [nvarchar](50) NULL;
GO

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Users2 stored procedure:
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
ALTER PROCEDURE [dbo].[spMerge_stg_Users2]
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
		OR ISNULL(d.Designation, '') <> ISNULL(s.Designation, '')
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
        d.Designation = s.Designation,
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
            Designation,
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
            s.Designation,
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
-- <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Add CustomProperties column to Devices2 and stg_Devices2:
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Devices2') AND name = 'CustomProperties')
BEGIN
    ALTER TABLE [dbo].[Devices2] ADD [CustomProperties] [nvarchar](max) NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.stg_Devices2') AND name = 'CustomProperties')
BEGIN
    ALTER TABLE [dbo].[stg_Devices2] ADD [CustomProperties] [nvarchar](max) NULL;
END
GO

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Devices2 stored procedure:
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
        OR ISNULL(d.CustomProperties, '') <> ISNULL(s.CustomProperties, '')
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
        d.CustomProperties = s.CustomProperties,
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
            id, GeotabId, ActiveFrom, ActiveTo, Comment, CustomProperties, DeviceType, Groups,
            LicensePlate, LicenseState, Name, ProductId, SerialNumber, VIN,
            EntityStatus, TmpTrailerGeotabId, TmpTrailerId, RecordLastChangedUtc
        )
        VALUES (
            s.id, s.GeotabId, s.ActiveFrom, s.ActiveTo, s.Comment, s.CustomProperties, s.DeviceType, s.Groups,
            s.LicensePlate, s.LicenseState, s.Name, s.ProductId, s.SerialNumber, s.VIN,
            s.EntityStatus, s.TmpTrailerGeotabId, s.TmpTrailerId, s.RecordLastChangedUtc
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
-- <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Add EngineHours and Odometer columns to Trips2 and stg_Trips2:
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Trips2') AND name = 'EngineHours')
BEGIN
    ALTER TABLE [dbo].[Trips2] ADD [EngineHours] [float] NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Trips2') AND name = 'Odometer')
BEGIN
    ALTER TABLE [dbo].[Trips2] ADD [Odometer] [float] NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.stg_Trips2') AND name = 'EngineHours')
BEGIN
    ALTER TABLE [dbo].[stg_Trips2] ADD [EngineHours] [float] NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.stg_Trips2') AND name = 'Odometer')
BEGIN
    ALTER TABLE [dbo].[stg_Trips2] ADD [Odometer] [float] NULL;
END
GO

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Trips2 stored procedure:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
		OR ISNULL(d.EngineHours, -1.0) <> ISNULL(s.EngineHours, -1.0)
		OR ISNULL(d.IdlingDurationTicks, -1) <> ISNULL(s.IdlingDurationTicks, -1)
		OR ISNULL(d.MaximumSpeed, -1.0) <> ISNULL(s.MaximumSpeed, -1.0)
		OR d.NextTripStart <> s.NextTripStart
		OR ISNULL(d.Odometer, -1.0) <> ISNULL(s.Odometer, -1.0)
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
		d.EngineHours = s.EngineHours,
		d.IdlingDurationTicks = s.IdlingDurationTicks,
		d.MaximumSpeed = s.MaximumSpeed,
		d.NextTripStart = s.NextTripStart,
		d.Odometer = s.Odometer,
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
            EngineHours,
            IdlingDurationTicks,
            MaximumSpeed,
            NextTripStart,
            Odometer,
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
            s.EngineHours,
            s.IdlingDurationTicks,
            s.MaximumSpeed,
            s.NextTripStart,
            s.Odometer,
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
ALTER AUTHORIZATION ON [dbo].[spMerge_stg_Trips2] TO SCHEMA OWNER
GO
GRANT EXECUTE ON [dbo].[spMerge_stg_Trips2] TO [geotabadapter_client] AS [dbo]
GO
-- <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create AuditLogs2 table:
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditLogs2' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[AuditLogs2](
        [id] [uniqueidentifier] NOT NULL,
        [GeotabId] [nvarchar](50) NOT NULL,
        [Comment] [nvarchar](max) NULL,
        [DateTime] [datetime2](7) NOT NULL,
        [Name] [nvarchar](255) NULL,
        [UserName] [nvarchar](255) NULL,
        [Version] [bigint] NULL,
        [RecordCreationTimeUtc] [datetime2](7) NOT NULL,
     CONSTRAINT [PK_AuditLogs2] PRIMARY KEY CLUSTERED
    (
        [id] ASC,
        [DateTime] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
    ) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
END
GO
-- <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
/*** [END] Part 2 of 3: Database Upgrades Above ***/



/*** [START] Part 3 of 3: Database Version Update Below ***/
INSERT INTO [dbo].[MiddlewareVersionInfo2] ([DatabaseVersion], [RecordCreationTimeUtc])
SELECT UpgradeDatabaseVersion, GETUTCDATE()
FROM #TMP_UpgradeDatabaseVersionTable;
DROP TABLE IF EXISTS #TMP_UpgradeDatabaseVersionTable;
/*** [END] Part 3 of 3: Database Version Update Above ***/
