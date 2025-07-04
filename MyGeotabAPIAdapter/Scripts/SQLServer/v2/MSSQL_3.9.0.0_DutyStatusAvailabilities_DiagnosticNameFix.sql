-- ================================================================================
-- DATABASE TYPE: SQL Server
-- 
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.8.0.0 to version 3.9.0.0.
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
INSERT INTO #TMP_UpgradeDatabaseVersionTable VALUES ('3.9.0.0');

DECLARE @requiredStartingDatabaseVersion NVARCHAR(50) = '3.8.0.0';
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
-- Create DutyStatusAvailabilities2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DutyStatusAvailabilities2](
	[id] [bigint] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[DriverId] [bigint] NOT NULL,
	[CycleAvailabilities] [nvarchar](max) NULL,
	[CycleDrivingTicks] [bigint] NULL,
	[CycleTicks] [bigint] NULL,
	[CycleRestTicks] [bigint] NULL,
	[DrivingBreakDurationTicks] [bigint] NULL,
	[DrivingTicks] [bigint] NULL,
	[DutyTicks] [bigint] NULL,
	[DutySinceCycleRestTicks] [bigint] NULL,
	[Is16HourExemptionAvailable] [bit] NULL,
	[IsAdverseDrivingApplied] [bit] NULL,
	[IsAdverseDrivingExemptionAvailable] [bit] NULL,
	[IsOffDutyDeferralExemptionAvailable] [bit] NULL,
	[IsRailroadExemptionAvailable] [bit] NULL,
	[Recap] [nvarchar](max) NULL,
	[RestTicks] [bigint] NULL,
	[WorkdayTicks] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DutyStatusAvailabilities2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_DutyStatusAvailabilities2_DriverId] ON [dbo].[DutyStatusAvailabilities2]
(
	[DriverId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[DutyStatusAvailabilities2]  WITH NOCHECK ADD  CONSTRAINT [FK_DutyStatusAvailabilities2_Users2] FOREIGN KEY([DriverId])
REFERENCES [dbo].[Users2] ([id])
GO
ALTER TABLE [dbo].[DutyStatusAvailabilities2] CHECK CONSTRAINT [FK_DutyStatusAvailabilities2_Users2]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_DutyStatusAvailabilities2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_DutyStatusAvailabilities2](
	[id] [bigint] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[DriverId] [bigint] NOT NULL,
	[CycleAvailabilities] [nvarchar](max) NULL,
	[CycleDrivingTicks] [bigint] NULL,
	[CycleTicks] [bigint] NULL,
	[CycleRestTicks] [bigint] NULL,
	[DrivingBreakDurationTicks] [bigint] NULL,
	[DrivingTicks] [bigint] NULL,
	[DutyTicks] [bigint] NULL,
	[DutySinceCycleRestTicks] [bigint] NULL,
	[Is16HourExemptionAvailable] [bit] NULL,
	[IsAdverseDrivingApplied] [bit] NULL,
	[IsAdverseDrivingExemptionAvailable] [bit] NULL,
	[IsOffDutyDeferralExemptionAvailable] [bit] NULL,
	[IsRailroadExemptionAvailable] [bit] NULL,
	[Recap] [nvarchar](max) NULL,
	[RestTicks] [bigint] NULL,
	[WorkdayTicks] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_stg_DutyStatusAvailabilities2_id_RecordLastChangedUtc] ON [dbo].[stg_DutyStatusAvailabilities2]
(
	[id] ASC,
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_DutyStatusAvailabilities2 stored procedure:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--     Upserts records from the stg_DutyStatusAvailabilities2 staging table to the
--     DutyStatusAvailabilities2 table and then truncates the staging table.
--
-- Notes:
--		- No transaction used as application should manage the transaction.
-- ==========================================================================================
CREATE PROCEDURE [dbo].[spMerge_stg_DutyStatusAvailabilities2]
AS
BEGIN
    SET NOCOUNT ON;

    -- De-duplicate staging table by selecting the latest record per id.
    -- Note that RecordLastChangedUtc is set in the order in which results are retrieved via GetFeed.
    WITH DeduplicatedStaging AS (
        SELECT *
        FROM (
            SELECT *,
                ROW_NUMBER() OVER (PARTITION BY id ORDER BY RecordLastChangedUtc DESC) AS rownum
            FROM dbo.stg_DutyStatusAvailabilities2
        ) AS sub
        WHERE rownum = 1
    )

    -- Perform upsert.
    MERGE dbo.DutyStatusAvailabilities2 AS d
    USING DeduplicatedStaging AS s
	-- id is unique key and logical key for matching. 
    ON d.id = s.id
    WHEN MATCHED AND (
        ISNULL(d.GeotabId, '') <> ISNULL(s.GeotabId, '') OR
        d.DriverId <> s.DriverId OR
        ISNULL(d.CycleAvailabilities, '') <> ISNULL(s.CycleAvailabilities, '') OR
        ISNULL(d.CycleDrivingTicks, -1) <> ISNULL(s.CycleDrivingTicks, -1) OR
        ISNULL(d.CycleTicks, -1) <> ISNULL(s.CycleTicks, -1) OR
        ISNULL(d.CycleRestTicks, -1) <> ISNULL(s.CycleRestTicks, -1) OR
        ISNULL(d.DrivingBreakDurationTicks, -1) <> ISNULL(s.DrivingBreakDurationTicks, -1) OR
        ISNULL(d.DrivingTicks, -1) <> ISNULL(s.DrivingTicks, -1) OR
        ISNULL(d.DutyTicks, -1) <> ISNULL(s.DutyTicks, -1) OR
        ISNULL(d.DutySinceCycleRestTicks, -1) <> ISNULL(s.DutySinceCycleRestTicks, -1) OR
        ISNULL(d.Is16HourExemptionAvailable, 0) <> ISNULL(s.Is16HourExemptionAvailable, 0) OR
        ISNULL(d.IsAdverseDrivingApplied, 0) <> ISNULL(s.IsAdverseDrivingApplied, 0) OR
        ISNULL(d.IsAdverseDrivingExemptionAvailable, 0) <> ISNULL(s.IsAdverseDrivingExemptionAvailable, 0) OR
        ISNULL(d.IsOffDutyDeferralExemptionAvailable, 0) <> ISNULL(s.IsOffDutyDeferralExemptionAvailable, 0) OR
        ISNULL(d.IsRailroadExemptionAvailable, 0) <> ISNULL(s.IsRailroadExemptionAvailable, 0) OR
        ISNULL(d.Recap, '') <> ISNULL(s.Recap, '') OR
        ISNULL(d.RestTicks, -1) <> ISNULL(s.RestTicks, -1) OR
        ISNULL(d.WorkdayTicks, -1) <> ISNULL(s.WorkdayTicks, -1)
        -- RecordLastChangedUtc not evaluated as it should never match.
    )
    THEN UPDATE SET
        d.GeotabId = s.GeotabId,
        d.DriverId = s.DriverId,
        d.CycleAvailabilities = s.CycleAvailabilities,
        d.CycleDrivingTicks = s.CycleDrivingTicks,
        d.CycleTicks = s.CycleTicks,
        d.CycleRestTicks = s.CycleRestTicks,
        d.DrivingBreakDurationTicks = s.DrivingBreakDurationTicks,
        d.DrivingTicks = s.DrivingTicks,
        d.DutyTicks = s.DutyTicks,
        d.DutySinceCycleRestTicks = s.DutySinceCycleRestTicks,
        d.Is16HourExemptionAvailable = s.Is16HourExemptionAvailable,
        d.IsAdverseDrivingApplied = s.IsAdverseDrivingApplied,
        d.IsAdverseDrivingExemptionAvailable = s.IsAdverseDrivingExemptionAvailable,
        d.IsOffDutyDeferralExemptionAvailable = s.IsOffDutyDeferralExemptionAvailable,
        d.IsRailroadExemptionAvailable = s.IsRailroadExemptionAvailable,
        d.Recap = s.Recap,
        d.RestTicks = s.RestTicks,
        d.WorkdayTicks = s.WorkdayTicks,
        d.RecordLastChangedUtc = s.RecordLastChangedUtc
    WHEN NOT MATCHED THEN
        INSERT (
            id, GeotabId, DriverId, CycleAvailabilities, CycleDrivingTicks,
            CycleTicks, CycleRestTicks, DrivingBreakDurationTicks, DrivingTicks,
            DutyTicks, DutySinceCycleRestTicks, Is16HourExemptionAvailable,
            IsAdverseDrivingApplied, IsAdverseDrivingExemptionAvailable,
            IsOffDutyDeferralExemptionAvailable, IsRailroadExemptionAvailable,
            Recap, RestTicks, WorkdayTicks, RecordLastChangedUtc
        )
        VALUES (
            s.id, s.GeotabId, s.DriverId, s.CycleAvailabilities, s.CycleDrivingTicks,
            s.CycleTicks, s.CycleRestTicks, s.DrivingBreakDurationTicks, s.DrivingTicks,
            s.DutyTicks, s.DutySinceCycleRestTicks, s.Is16HourExemptionAvailable,
            s.IsAdverseDrivingApplied, s.IsAdverseDrivingExemptionAvailable,
            s.IsOffDutyDeferralExemptionAvailable, s.IsRailroadExemptionAvailable,
            s.Recap, s.RestTicks, s.WorkdayTicks, s.RecordLastChangedUtc
        );

    -- Clear staging table.
    TRUNCATE TABLE [dbo].[stg_DutyStatusAvailabilities2];
END
GO

-- Grant execute permissions to the specified role/user
GRANT EXECUTE ON [dbo].[spMerge_stg_DutyStatusAvailabilities2] TO [geotabadapter_client];
GO

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Adjust length of DiagnosticName column in stg_Diagnostics2 and Diagnostics2 tables:
ALTER TABLE dbo.stg_Diagnostics2
ALTER COLUMN DiagnosticName NVARCHAR(MAX) NOT NULL;
ALTER TABLE dbo.Diagnostics2
ALTER COLUMN DiagnosticName NVARCHAR(MAX) NOT NULL;
/*** [END] Part 2 of 3: Database Upgrades Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO [dbo].[MiddlewareVersionInfo2] ([DatabaseVersion], [RecordCreationTimeUtc])
SELECT UpgradeDatabaseVersion, GETUTCDATE()
FROM #TMP_UpgradeDatabaseVersionTable;
DROP TABLE IF EXISTS #TMP_UpgradeDatabaseVersionTable;
/*** [END] Part 3 of 3: Database Version Update Above ***/  
