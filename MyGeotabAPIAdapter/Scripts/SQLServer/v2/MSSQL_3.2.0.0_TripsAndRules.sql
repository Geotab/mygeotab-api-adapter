-- ================================================================================
-- DATABASE TYPE: SQL Server
-- 
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.1.0.0 to version 3.2.0.0.
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
INSERT INTO #TMP_UpgradeDatabaseVersionTable VALUES ('3.2.0.0');

DECLARE @requiredStartingDatabaseVersion NVARCHAR(50) = '3.1.0.0';
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
-- Remove indexes that aren't needed and could cause issues.
IF OBJECT_ID('PK_stg_Devices2', 'PK') IS NOT NULL 
    ALTER TABLE [dbo].[stg_Devices2] DROP CONSTRAINT PK_stg_Devices2;
IF OBJECT_ID('PK_stg_Users2', 'PK') IS NOT NULL 
    ALTER TABLE [dbo].[stg_Users2] DROP CONSTRAINT PK_stg_Users2;
IF OBJECT_ID('PK_stg_Zones2', 'PK') IS NOT NULL 
    ALTER TABLE [dbo].[stg_Zones2] DROP CONSTRAINT PK_stg_Zones2;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Rules2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Rules2](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[ActiveFrom] [datetime2](7) NULL,
	[ActiveTo] [datetime2](7) NULL,
	[BaseType] [nvarchar](50) NULL,
	[Comment] [nvarchar](max) NULL,
	[Groups] [nvarchar](max) NULL,
	[Name] [nvarchar](255) NULL,
	[Version] [bigint] NOT NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Rules2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Rules2_RecordLastChangedUtc] 
ON [dbo].[Rules2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Trips2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Trips2](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[AfterHoursDistance] [real] NULL,
	[AfterHoursDrivingDurationTicks] [bigint] NULL,
	[AfterHoursEnd] [bit] NULL,
	[AfterHoursStart] [bit] NULL,
	[AfterHoursStopDurationTicks] [bigint] NULL,
	[AverageSpeed] [real] NULL,
	[DeletedDateTime] [datetime2](7) NULL,
	[DeviceId] [bigint] NOT NULL,
	[Distance] [real] NOT NULL,
	[DriverId] [bigint] NULL,
	[DrivingDurationTicks] [bigint] NOT NULL,
	[IdlingDurationTicks] [bigint] NULL,
	[MaximumSpeed] [real] NULL,
	[NextTripStart] [datetime2](7) NOT NULL,
	[SpeedRange1] [int] NULL,
	[SpeedRange1DurationTicks] [bigint] NULL,
	[SpeedRange2] [int] NULL,
	[SpeedRange2DurationTicks] [bigint] NULL,
	[SpeedRange3] [int] NULL,
	[SpeedRange3DurationTicks] [bigint] NULL,
	[Start] [datetime2](7) NOT NULL,
	[Stop] [datetime2](7) NOT NULL,
	[StopDurationTicks] [bigint] NOT NULL,
	[StopPointX] [float] NULL,
	[StopPointY] [float] NULL,
	[WorkDistance] [real] NULL,
	[WorkDrivingDurationTicks] [bigint] NULL,
	[WorkStopDurationTicks] [bigint] NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Trips2] PRIMARY KEY NONCLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UK_Trips2_DeviceId_Start_EntityStatus] UNIQUE NONCLUSTERED 
(
	[DeviceId] ASC,
	[Start] ASC,
	[EntityStatus] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([Start])
GO

CREATE CLUSTERED INDEX [CI_Trips2_Start_Id] ON [dbo].[Trips2]
(
	[Start] ASC,
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([Start])
GO

CREATE NONCLUSTERED INDEX [IX_Trips2_NextTripStart] ON [dbo].[Trips2]
(
	[NextTripStart] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Trips2_RecordLastChangedUtc] ON [dbo].[Trips2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Trips2]  WITH NOCHECK ADD  CONSTRAINT [FK_Trips2_Devices2] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Devices2] ([id])
GO
ALTER TABLE [dbo].[Trips2] CHECK CONSTRAINT [FK_Trips2_Devices2]
GO

ALTER TABLE [dbo].[Trips2]  WITH NOCHECK ADD  CONSTRAINT [FK_Trips2_Users2] FOREIGN KEY([DriverId])
REFERENCES [dbo].[Users2] ([id])
GO
ALTER TABLE [dbo].[Trips2] CHECK CONSTRAINT [FK_Trips2_Users2]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_Rules2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_Rules2](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[ActiveFrom] [datetime2](7) NULL,
	[ActiveTo] [datetime2](7) NULL,
	[BaseType] [nvarchar](50) NULL,
	[Comment] [nvarchar](max) NULL,
	[Groups] [nvarchar](max) NULL,
	[Name] [nvarchar](255) NULL,
	[Version] [bigint] NOT NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_stg_Rules2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_stg_Rules2_GeotabId_RecordLastChangedUtc] 
ON [dbo].[stg_Rules2]
(
	[GeotabId] ASC,
	[RecordLastChangedUtc] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_Trips2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_Trips2](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[AfterHoursDistance] [real] NULL,
	[AfterHoursDrivingDurationTicks] [bigint] NULL,
	[AfterHoursEnd] [bit] NULL,
	[AfterHoursStart] [bit] NULL,
	[AfterHoursStopDurationTicks] [bigint] NULL,
	[AverageSpeed] [real] NULL,
	[DeletedDateTime] [datetime2](7) NULL,
	[DeviceId] [bigint] NOT NULL,
	[Distance] [real] NOT NULL,
	[DriverId] [bigint] NULL,
	[DrivingDurationTicks] [bigint] NOT NULL,
	[IdlingDurationTicks] [bigint] NULL,
	[MaximumSpeed] [real] NULL,
	[NextTripStart] [datetime2](7) NOT NULL,
	[SpeedRange1] [int] NULL,
	[SpeedRange1DurationTicks] [bigint] NULL,
	[SpeedRange2] [int] NULL,
	[SpeedRange2DurationTicks] [bigint] NULL,
	[SpeedRange3] [int] NULL,
	[SpeedRange3DurationTicks] [bigint] NULL,
	[Start] [datetime2](7) NOT NULL,
	[Stop] [datetime2](7) NOT NULL,
	[StopDurationTicks] [bigint] NOT NULL,
	[StopPointX] [float] NULL,
	[StopPointY] [float] NULL,
	[WorkDistance] [real] NULL,
	[WorkDrivingDurationTicks] [bigint] NULL,
	[WorkStopDurationTicks] [bigint] NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_stg_Trips2] PRIMARY KEY NONCLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_stg_Trips2_DeviceId_Start_EntityStatus] ON [dbo].[stg_Trips2]
(
	[DeviceId] ASC,
	[Start] ASC,
	[EntityStatus] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_Rules2 stored procedure:
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

    -- De-duplicate staging table by selecting the latest record per natural key (DeviceId + Start). 
	-- Note that RecordLastChangedUtc is set in the order in which results are retrieved via GetFeed.
    WITH DeduplicatedStaging AS (
        SELECT *
        FROM (
            SELECT *,
                ROW_NUMBER() OVER (PARTITION BY id ORDER BY RecordLastChangedUtc DESC) AS rownum
            FROM dbo.stg_Rules2
        ) AS sub
        WHERE rownum = 1
    )

	-- Perform upsert.
    MERGE INTO dbo.Rules2 AS d
    USING DeduplicatedStaging AS s
    ON d.GeotabId = s.GeotabId
    WHEN MATCHED AND (
        ISNULL(d.ActiveFrom, '2000-01-01') <> ISNULL(s.ActiveFrom, '2000-01-01')
		OR ISNULL(d.ActiveTo, '2000-01-01') <> ISNULL(s.ActiveTo, '2000-01-01')
		OR ISNULL(d.BaseType, '') <> ISNULL(s.BaseType, '')
		OR ISNULL(d.Comment, '') <> ISNULL(s.Comment, '')
        OR ISNULL(d.Groups, '') <> ISNULL(s.Groups, '')
		OR ISNULL(d.Name, '') <> ISNULL(s.Name, '')
		OR ISNULL(d.Version, -1) <> ISNULL(s.Version, -1)
        OR d.EntityStatus <> s.EntityStatus
        -- OR d.RecordLastChangedUtc <> s.RecordLastChangedUtc
    )
    THEN UPDATE SET
        d.ActiveFrom = s.ActiveFrom,
        d.ActiveTo = s.ActiveTo,
        d.BaseType = s.BaseType,
        d.Comment = s.Comment,
        d.Groups = s.Groups,
        d.Name = s.Name,
        d.Version = s.Version,
        d.EntityStatus = s.EntityStatus,
        d.RecordLastChangedUtc = s.RecordLastChangedUtc
    WHEN NOT MATCHED THEN 
        INSERT (
			GeotabId, 
			ActiveFrom, 
			ActiveTo, 
			BaseType,
			Comment, 
			Groups, 
			Name, 
			Version, 
			EntityStatus, 
			RecordLastChangedUtc
		)
        VALUES (
			s.GeotabId, 
			s.ActiveFrom, 
			s.ActiveTo, 
			s.BaseType, 
			s.Comment, 
			s.Groups, 
			s.Name, 
			s.Version,  
			s.EntityStatus, 
			s.RecordLastChangedUtc
		);

    -- If @SetEntityStatusDeletedForMissingRules is 1 (true) set the EntityStatus to 
	-- 0 (Deleted) for any records in the Rules2 table for which there is no corresponding
	-- record with the same id in the stg_Rules2 table.
    IF @SetEntityStatusDeletedForMissingRules = 1
    BEGIN
		UPDATE d
		SET EntityStatus = 0,
			RecordLastChangedUtc = GETUTCDATE()
		FROM dbo.Rules2 d
		WHERE NOT EXISTS (
			SELECT 1 FROM dbo.stg_Rules2 s
			WHERE s.id = d.id
		);
    END;

    -- Clear staging table.
    TRUNCATE TABLE dbo.stg_Rules2;
END;
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_Trips2 stored procedure:
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
CREATE PROCEDURE [dbo].[spMerge_stg_Trips2]
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
    ON d.DeviceId = s.DeviceId
       AND d.[Start] = s.[Start]
	WHEN MATCHED AND (
		-- d.GeotabId <> s.GeotabId -- Note: GeotabId is NOT a unique identifier for a Trip.
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
		-- OR d.RecordLastChangedUtc = s.RecordLastChangedUtc
	)   
	THEN UPDATE SET 
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


/*** [END] Part 2 of 3: Database Upgrades Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO [dbo].[MiddlewareVersionInfo2] ([DatabaseVersion], [RecordCreationTimeUtc])
SELECT UpgradeDatabaseVersion, GETUTCDATE()
FROM #TMP_UpgradeDatabaseVersionTable;
DROP TABLE IF EXISTS #TMP_UpgradeDatabaseVersionTable;
/*** [END] Part 3 of 3: Database Version Update Above ***/  
