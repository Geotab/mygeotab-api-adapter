-- ================================================================================
-- DATABASE TYPE: SQL Server
-- 
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.2.0.0 to version 3.3.0.0.
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
INSERT INTO #TMP_UpgradeDatabaseVersionTable VALUES ('3.3.0.0');

DECLARE @requiredStartingDatabaseVersion NVARCHAR(50) = '3.2.0.0';
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
-- Create ExceptionEvents2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ExceptionEvents2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[ActiveFrom] [datetime2](7) NOT NULL,
	[ActiveTo] [datetime2](7) NULL,
	[DeviceId] [bigint] NOT NULL,
	[Distance] [real] NULL,
	[DriverId] [bigint] NULL,
	[DurationTicks] [bigint] NULL,
	[LastModifiedDateTime] [datetime2](7) NULL,
	[RuleId] [bigint] NOT NULL,
	[State] [int] NULL,
	[Version] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ExceptionEvents2] PRIMARY KEY CLUSTERED 
(
	[id] ASC,
	[ActiveFrom] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([ActiveFrom])
) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([ActiveFrom])
GO

CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_DeviceId] ON [dbo].[ExceptionEvents2]
(
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_DriverId] ON [dbo].[ExceptionEvents2]
(
	[DriverId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_RecordLastChangedUtc] ON [dbo].[ExceptionEvents2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_RuleId] ON [dbo].[ExceptionEvents2]
(
	[RuleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_State] ON [dbo].[ExceptionEvents2]
(
	[State] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_TimeRange] ON [dbo].[ExceptionEvents2]
(
	[ActiveFrom] ASC,
	[ActiveTo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([ActiveFrom])
GO

CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_TimeRange_Driver_State] ON [dbo].[ExceptionEvents2]
(
	[ActiveFrom] ASC,
	[ActiveTo] ASC,
	[DriverId] ASC,
	[State] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([ActiveFrom])
GO

CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_TimeRange_Rule_Driver_State] ON [dbo].[ExceptionEvents2]
(
	[ActiveFrom] ASC,
	[ActiveTo] ASC,
	[RuleId] ASC,
	[DriverId] ASC,
	[State] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([ActiveFrom])
GO

CREATE NONCLUSTERED INDEX [IX_ExceptionEvents2_TimeRange_Rule_State] ON [dbo].[ExceptionEvents2]
(
	[ActiveFrom] ASC,
	[ActiveTo] ASC,
	[RuleId] ASC,
	[State] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([ActiveFrom])
GO

CREATE UNIQUE NONCLUSTERED INDEX [UI_ExceptionEvents2_Id] ON [dbo].[ExceptionEvents2]
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

ALTER TABLE [dbo].[ExceptionEvents2]  WITH NOCHECK ADD  CONSTRAINT [FK_ExceptionEvents2_Devices2] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Devices2] ([id])
GO
ALTER TABLE [dbo].[ExceptionEvents2] CHECK CONSTRAINT [FK_ExceptionEvents2_Devices2]
GO
ALTER TABLE [dbo].[ExceptionEvents2]  WITH NOCHECK ADD  CONSTRAINT [FK_ExceptionEvents2_Rules2] FOREIGN KEY([RuleId])
REFERENCES [dbo].[Rules2] ([id])
GO
ALTER TABLE [dbo].[ExceptionEvents2] CHECK CONSTRAINT [FK_ExceptionEvents2_Rules2]
GO
ALTER TABLE [dbo].[ExceptionEvents2]  WITH NOCHECK ADD  CONSTRAINT [FK_ExceptionEvents2_Users2] FOREIGN KEY([DriverId])
REFERENCES [dbo].[Users2] ([id])
GO
ALTER TABLE [dbo].[ExceptionEvents2] CHECK CONSTRAINT [FK_ExceptionEvents2_Users2]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_ExceptionEvents2 table:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stg_ExceptionEvents2](
	[id] [uniqueidentifier] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[ActiveFrom] [datetime2](7) NOT NULL,
	[ActiveTo] [datetime2](7) NULL,
	[DeviceId] [bigint] NOT NULL,
	[Distance] [real] NULL,
	[DriverId] [bigint] NULL,
	[DurationTicks] [bigint] NULL,
	[LastModifiedDateTime] [datetime2](7) NULL,
	[RuleGeotabId] [nvarchar](50) NOT NULL,
	[RuleId] [bigint],
	[State] [int] NULL,
	[Version] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_stg_ExceptionEvents2_id_RecordLastChangedUtc] ON [dbo].[stg_ExceptionEvents2]
(
	[id] ASC,
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_ExceptionEvents2 stored procedure:
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ==========================================================================================
-- Description:
--   Upserts records from the stg_ExceptionEvents2 staging table to the ExceptionEvents2
--   table and then truncates the staging table.
--
-- Notes:
--   - No transaction used as application should manage the transaction.
-- ==========================================================================================
CREATE PROCEDURE [dbo].[spMerge_stg_ExceptionEvents2]
AS
BEGIN
    SET NOCOUNT ON;

    -- De-duplicate staging table by selecting the latest record per id.
    -- Uses ROW_NUMBER() partitioned by id, ordering by RecordLastChangedUtc descending. Also 
	-- retrieve RuleId by using the RuleGeotabId to find the corresponding id in the Rules2 table.
    WITH DeduplicatedStaging AS (
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
    )

	-- Perform upsert.
	MERGE dbo.ExceptionEvents2 AS d
	USING DeduplicatedStaging AS s
	ON d.id = s.id
	WHEN MATCHED AND (
		ISNULL(d.GeotabId, '') <> ISNULL(s.GeotabId, '') OR
		ISNULL(d.ActiveFrom, '1900-01-01') <> ISNULL(s.ActiveFrom, '1900-01-01') OR
		ISNULL(d.ActiveTo, '1900-01-01') <> ISNULL(s.ActiveTo, '1900-01-01') OR
		ISNULL(d.DeviceId, -1) <> ISNULL(s.DeviceId, -1) OR
		ISNULL(d.Distance, -1.0) <> ISNULL(s.Distance, -1.0) OR
		ISNULL(d.DriverId, -1) <> ISNULL(s.DriverId, -1) OR
		ISNULL(d.DurationTicks, -1) <> ISNULL(s.DurationTicks, -1) OR
		ISNULL(d.LastModifiedDateTime, '1900-01-01') <> ISNULL(s.LastModifiedDateTime, '1900-01-01') OR
		ISNULL(d.RuleId, -1) <> ISNULL(s.LookedUpRuleId, -1) OR
		ISNULL(d.[State], -1) <> ISNULL(s.[State], -1) OR
		ISNULL(d.[Version], -1) <> ISNULL(s.[Version], -1)
		-- OR d.RecordLastChangedUtc = s.RecordLastChangedUtc
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
	WHEN NOT MATCHED THEN
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

END
GO

GRANT EXECUTE ON [dbo].[spMerge_stg_ExceptionEvents2] TO [geotabadapter_client];


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Grant permissions on stored procedures:
GRANT EXECUTE ON [dbo].[spFaultData2WithLagLeadLongLatBatch] TO geotabadapter_client;
GRANT EXECUTE ON [dbo].[spManagePartitions] TO geotabadapter_client;
GRANT EXECUTE ON [dbo].[spMerge_stg_Devices2] TO geotabadapter_client;
GRANT EXECUTE ON [dbo].[spMerge_stg_Diagnostics2] TO geotabadapter_client;
GRANT EXECUTE ON [dbo].[spMerge_stg_ExceptionEvents2] TO [geotabadapter_client];
GRANT EXECUTE ON [dbo].[spMerge_stg_Groups2] TO geotabadapter_client;
GRANT EXECUTE ON [dbo].[spMerge_stg_Rules2] TO geotabadapter_client;
GRANT EXECUTE ON [dbo].[spMerge_stg_Users2] TO geotabadapter_client;
GRANT EXECUTE ON [dbo].[spMerge_stg_Trips2] TO geotabadapter_client;
GRANT EXECUTE ON [dbo].[spMerge_stg_Zones2] TO geotabadapter_client;
GRANT EXECUTE ON [dbo].[spMerge_stg_ZoneTypes2] TO geotabadapter_client;
GRANT EXECUTE ON [dbo].[spStatusData2WithLagLeadLongLatBatch] TO geotabadapter_client;
/*** [END] Part 2 of 3: Database Upgrades Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO [dbo].[MiddlewareVersionInfo2] ([DatabaseVersion], [RecordCreationTimeUtc])
SELECT UpgradeDatabaseVersion, GETUTCDATE()
FROM #TMP_UpgradeDatabaseVersionTable;
DROP TABLE IF EXISTS #TMP_UpgradeDatabaseVersionTable;
/*** [END] Part 3 of 3: Database Version Update Above ***/  
