-- ================================================================================
-- DATABASE TYPE: SQL Server
-- 
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.6.0.0 to version 3.7.0.0.
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
INSERT INTO #TMP_UpgradeDatabaseVersionTable VALUES ('3.7.0.0');

DECLARE @requiredStartingDatabaseVersion NVARCHAR(50) = '3.6.0.0';
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
-- Create DeviceStatusInfo2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceStatusInfo2](
	[id] [bigint] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[Bearing] [float] NOT NULL,
	[CurrentStateDuration] [nvarchar](50) NOT NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeviceId] [bigint] NOT NULL,
	[DriverId] [bigint] NULL,
	[IsDeviceCommunicating] [bit] NOT NULL,
	[IsDriving] [bit] NOT NULL,
	[IsHistoricLastDriver] [bit] NOT NULL,
	[Latitude] [float] NOT NULL,
	[Longitude] [float] NOT NULL,
	[Speed] [real] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DeviceStatusInfo2] PRIMARY KEY NONCLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_DeviceStatusInfo2_DeviceId] ON [dbo].[DeviceStatusInfo2]
(
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_DeviceStatusInfo2_DriverId] ON [dbo].[DeviceStatusInfo2]
(
	[DriverId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[DeviceStatusInfo2]  WITH NOCHECK ADD  CONSTRAINT [FK_DeviceStatusInfo2_Devices2] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Devices2] ([id])
GO
ALTER TABLE [dbo].[DeviceStatusInfo2] CHECK CONSTRAINT [FK_DeviceStatusInfo2_Devices2]
GO
ALTER TABLE [dbo].[DeviceStatusInfo2]  WITH NOCHECK ADD  CONSTRAINT [FK_DeviceStatusInfo2_Users2] FOREIGN KEY([DriverId])
REFERENCES [dbo].[Users2] ([id])
GO
ALTER TABLE [dbo].[DeviceStatusInfo2] CHECK CONSTRAINT [FK_DeviceStatusInfo2_Users2]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_DeviceStatusInfo2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_DeviceStatusInfo2](
	[id] [bigint] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[Bearing] [float] NOT NULL,
	[CurrentStateDuration] [nvarchar](50) NOT NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeviceId] [bigint] NOT NULL,
	[DriverId] [bigint] NULL,
	[IsDeviceCommunicating] [bit] NOT NULL,
	[IsDriving] [bit] NOT NULL,
	[IsHistoricLastDriver] [bit] NOT NULL,
	[Latitude] [float] NOT NULL,
	[Longitude] [float] NOT NULL,
	[Speed] [real] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL
) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_stg_DeviceStatusInfo2_id_RecordLastChangedUtc] ON [dbo].[stg_DeviceStatusInfo2]
(
	[id] ASC,
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_DeviceStatusInfo2 stored procedure:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_DeviceStatusInfo2 staging table to the DeviceStatusInfo2
--   table and then truncates the staging table.
--
-- Notes:
--   - No transaction used as application should manage the transaction.
-- ==========================================================================================
CREATE PROCEDURE [dbo].[spMerge_stg_DeviceStatusInfo2]
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
            FROM dbo.stg_DeviceStatusInfo2
        ) AS sub
        WHERE rownum = 1
    )

    -- Perform upsert.
    MERGE dbo.DeviceStatusInfo2 AS d
    USING DeduplicatedStaging AS s
    ON d.id = s.id -- id is unique key and logical key for matching. 
    WHEN MATCHED AND (
		d.GeotabId <> s.GeotabId
		OR d.Bearing <> s.Bearing
		OR d.CurrentStateDuration <> s.CurrentStateDuration
		OR d.[DateTime] <> s.[DateTime]
		OR d.DeviceId <> s.DeviceId
		OR ISNULL(d.DriverId, -1) <> ISNULL(s.DriverId, -1)
		OR d.IsDeviceCommunicating <> s.IsDeviceCommunicating
		OR d.IsDriving <> s.IsDriving
		OR d.IsHistoricLastDriver <> s.IsHistoricLastDriver
		OR d.Latitude <> s.Latitude
		OR d.Longitude <> s.Longitude
		OR d.Speed <> s.Speed
        -- RecordLastChangedUtc not evaluated as it should never match. 
    )
    THEN UPDATE SET
        d.GeotabId = s.GeotabId,
        d.Bearing = s.Bearing,
        d.CurrentStateDuration = s.CurrentStateDuration,
        d.[DateTime] = s.[DateTime],
        d.DeviceId = s.DeviceId,
        d.DriverId = s.DriverId,
        d.IsDeviceCommunicating = s.IsDeviceCommunicating,
        d.IsDriving = s.IsDriving,
        d.IsHistoricLastDriver = s.IsHistoricLastDriver,
        d.Latitude = s.Latitude,
        d.Longitude = s.Longitude,
        d.Speed = s.Speed,
        d.RecordLastChangedUtc = s.RecordLastChangedUtc
    WHEN NOT MATCHED THEN
        INSERT (
            id,
            GeotabId,
            Bearing,
            CurrentStateDuration,
            [DateTime],
            DeviceId,
            DriverId,
            IsDeviceCommunicating,
            IsDriving,
            IsHistoricLastDriver,
            Latitude,
            Longitude,
            Speed,
            RecordLastChangedUtc
        )
        VALUES (
            s.id,
            s.GeotabId,
            s.Bearing,
            s.CurrentStateDuration,
            s.[DateTime],
            s.DeviceId,
            s.DriverId,
            s.IsDeviceCommunicating,
            s.IsDriving,
            s.IsHistoricLastDriver,
            s.Latitude,
            s.Longitude,
            s.Speed,
            s.RecordLastChangedUtc
        );

    -- Clear staging table.
    TRUNCATE TABLE dbo.stg_DeviceStatusInfo2;

END
GO

-- Grant execute permission to the client user/role
GRANT EXECUTE ON [dbo].[spMerge_stg_DeviceStatusInfo2] TO [geotabadapter_client];
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
