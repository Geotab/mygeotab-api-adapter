-- ================================================================================
-- DATABASE TYPE: SQL Server
-- 
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.4.0.0 to version 3.5.0.0.
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
INSERT INTO #TMP_UpgradeDatabaseVersionTable VALUES ('3.5.0.0');

DECLARE @requiredStartingDatabaseVersion NVARCHAR(50) = '3.4.0.0';
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
-- Create ChargeEvents2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ChargeEvents2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[ChargeIsEstimated] [bit] NOT NULL,
	[ChargeType] [nvarchar](50) NOT NULL,
	[DeviceId] [bigint] NOT NULL,
	[DurationTicks] [bigint] NOT NULL,
	[EndStateOfCharge] [float] NULL,
	[EnergyConsumedKwh] [float] NULL,
	[EnergyUsedSinceLastChargeKwh] [float] NULL,
	[Latitude] [float] NULL,
	[Longitude] [float] NULL,
	[MaxACVoltage] [float] NULL,
	[MeasuredBatteryEnergyInKwh] [float] NULL,
	[MeasuredBatteryEnergyOutKwh] [float] NULL,
	[MeasuredOnBoardChargerEnergyInKwh] [float] NULL,
	[MeasuredOnBoardChargerEnergyOutKwh] [float] NULL,
	[PeakPowerKw] [float] NULL,
	[StartStateOfCharge] [float] NULL,
	[StartTime] [datetime2](7) NOT NULL,
	[TripStop] [datetime2](7) NULL,
	[Version] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ChargeEvents2] PRIMARY KEY CLUSTERED 
(
	[id] ASC,
	[StartTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([StartTime])
) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([StartTime])
GO

CREATE NONCLUSTERED INDEX [IX_ChargeEvents2_DeviceId] ON [dbo].[ChargeEvents2]
(
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_ChargeEvents2_RecordLastChangedUtc] ON [dbo].[ChargeEvents2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_ChargeEvents2_StartTime_DeviceId] ON [dbo].[ChargeEvents2]
(
	[StartTime] ASC,
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([StartTime])
GO

CREATE UNIQUE NONCLUSTERED INDEX [UI_ChargeEvents2_Id] ON [dbo].[ChargeEvents2]
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

ALTER TABLE [dbo].[ChargeEvents2]  WITH NOCHECK ADD  CONSTRAINT [FK_ChargeEvents2_Devices2] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Devices2] ([id])
GO
ALTER TABLE [dbo].[ChargeEvents2] CHECK CONSTRAINT [FK_ChargeEvents2_Devices2]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_ChargeEvents2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_ChargeEvents2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[ChargeIsEstimated] [bit] NOT NULL,
	[ChargeType] [nvarchar](50) NOT NULL,
	[DeviceId] [bigint] NOT NULL,
	[DurationTicks] [bigint] NOT NULL,
	[EndStateOfCharge] [float] NULL,
	[EnergyConsumedKwh] [float] NULL,
	[EnergyUsedSinceLastChargeKwh] [float] NULL,
	[Latitude] [float] NULL,
	[Longitude] [float] NULL,
	[MaxACVoltage] [float] NULL,
	[MeasuredBatteryEnergyInKwh] [float] NULL,
	[MeasuredBatteryEnergyOutKwh] [float] NULL,
	[MeasuredOnBoardChargerEnergyInKwh] [float] NULL,
	[MeasuredOnBoardChargerEnergyOutKwh] [float] NULL,
	[PeakPowerKw] [float] NULL,
	[StartStateOfCharge] [float] NULL,
	[StartTime] [datetime2](7) NOT NULL,
	[TripStop] [datetime2](7) NULL,
	[Version] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_stg_ChargeEvents2_id_RecordLastChangedUtc] ON [dbo].[stg_ChargeEvents2]
(
	[id] ASC,
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_ChargeEvents2 stored procedure:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_ChargeEvents2 staging table to the ChargeEvents2
--   table and then truncates the staging table.
--
-- Notes:
--   - No transaction used as application should manage the transaction.
-- ==========================================================================================
CREATE PROCEDURE [dbo].[spMerge_stg_ChargeEvents2]
AS
BEGIN
    SET NOCOUNT ON;

    -- De-duplicate staging table by selecting the latest record per id.
    -- Uses ROW_NUMBER() partitioned by id, ordering by RecordLastChangedUtc descending.
    WITH DeduplicatedStaging AS (
        SELECT *
        FROM (
            SELECT
                stg.*,
                ROW_NUMBER() OVER (PARTITION BY stg.id ORDER BY stg.RecordLastChangedUtc DESC) AS rownum
            FROM dbo.stg_ChargeEvents2 stg
        ) AS sub
        WHERE rownum = 1
    )

    -- Perform upsert.
    MERGE dbo.ChargeEvents2 AS d
    USING DeduplicatedStaging AS s
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
		d.StartTime <> s.StartTime OR
        ISNULL(d.TripStop, '1900-01-01') <> ISNULL(s.TripStop, '1900-01-01') OR
        ISNULL(d.[Version], -1) <> ISNULL(s.[Version], -1)
        -- OR d.RecordLastChangedUtc <> s.RecordLastChangedUtc
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
    WHEN NOT MATCHED THEN
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

END
GO

-- Grant execute permission to the client user/role
GRANT EXECUTE ON [dbo].[spMerge_stg_ChargeEvents2] TO [geotabadapter_client];
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Modify Rules2 and stg_Rules2 tables:
-- Add a Condition column to the Rules2 table.
ALTER TABLE [dbo].[Rules2]
ADD [Condition] nvarchar(max) NULL;
GO

-- Add a Condition column to the stg_Rules2 table.
ALTER TABLE [dbo].[stg_Rules2]
ADD [Condition] nvarchar(max) NULL;
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Modify spMerge_stg_Rules2 stored procedure:
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
ALTER PROCEDURE [dbo].[spMerge_stg_Rules2]
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
		OR ISNULL(d.[Condition], '') <> ISNULL(s.[Condition], '')
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
		d.[Condition] = s.[Condition],
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
			[Condition], 
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
			s.[Condition], 
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
/*** [END] Part 2 of 3: Database Upgrades Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO [dbo].[MiddlewareVersionInfo2] ([DatabaseVersion], [RecordCreationTimeUtc])
SELECT UpgradeDatabaseVersion, GETUTCDATE()
FROM #TMP_UpgradeDatabaseVersionTable;
DROP TABLE IF EXISTS #TMP_UpgradeDatabaseVersionTable;
/*** [END] Part 3 of 3: Database Version Update Above ***/  
