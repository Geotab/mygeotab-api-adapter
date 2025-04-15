-- ================================================================================
-- DATABASE TYPE: SQL Server
-- 
-- NOTES: 
--   1: This script applies to the MyGeotab API Adapter database starting with
--	    application version 3.0.0.0. It does not apply to earlier versions of the
--      application. 
--   2: This script will be updated with future schema changes such that someone
--      starting with a future version of the application can use this script as a
--      starting point without having to apply any earlier cumulative updates.
--   3: Be sure to alter the "USE [geotabadapterdb]" statement below if you have
--      changed the database name to something else.
--
-- DESCRIPTION: 
--   This script is intended for use in creating the database schema in an empty 
--   database starting from version 3.0.0.0 of the MyGeotab API Adapter and including
--   any cumulative schema changes up to the current application version at time of
--   download.
-- ================================================================================

USE [geotabadapterdb]
GO

/*** [START] SSMS-Generated Script ***/ 
/****** Object:  Table [dbo].[FaultDataLocations2]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FaultDataLocations2](
	[id] [bigint] NOT NULL,
	[DeviceId] [bigint] NOT NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[Latitude] [float] NULL,
	[Longitude] [float] NULL,
	[Speed] [real] NULL,
	[Bearing] [real] NULL,
	[Direction] [nvarchar](3) NULL,
	[LongLatProcessed] [bit] NOT NULL,
	[LongLatReason] [tinyint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_FaultDataLocations2] PRIMARY KEY CLUSTERED 
(
	[id] ASC,
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Table [dbo].[StatusDataLocations2]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StatusDataLocations2](
	[id] [bigint] NOT NULL,
	[DeviceId] [bigint] NOT NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[Latitude] [float] NULL,
	[Longitude] [float] NULL,
	[Speed] [real] NULL,
	[Bearing] [real] NULL,
	[Direction] [nvarchar](3) NULL,
	[LongLatProcessed] [bit] NOT NULL,
	[LongLatReason] [tinyint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_StatusDataLocations2] PRIMARY KEY CLUSTERED 
(
	[id] ASC,
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  View [dbo].[vwStatsForLocationInterpolationProgress]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ==========================================================================================
-- Description:	Provides information related to location interpolation progress.
-- ==========================================================================================
CREATE VIEW [dbo].[vwStatsForLocationInterpolationProgress]
AS
SELECT 
    ROW_NUMBER() OVER (ORDER BY [Table]) AS RowId,
    [Table],
    Total,
    LongLatProcessedTotal,
    CASE 
        WHEN Total > 0 THEN (LongLatProcessedTotal * 100.0) / Total 
        ELSE 0 
    END AS LongLatProcessedPercentage
FROM (
    SELECT 
        'StatusDataLocations2' AS [Table],
        COUNT(*) AS Total,
        SUM(CASE WHEN [LongLatProcessed] = 1 THEN 1 ELSE 0 END) AS LongLatProcessedTotal
    FROM dbo.StatusDataLocations2 WITH (NOLOCK)
    UNION ALL
    SELECT 
        'FaultDataLocations2' AS [Table],
        COUNT(*) AS Total,
        SUM(CASE WHEN [LongLatProcessed] = 1 THEN 1 ELSE 0 END) AS LongLatProcessedTotal
    FROM dbo.FaultDataLocations2 WITH (NOLOCK)
) AS InterpolationProgress;
GO
/****** Object:  View [dbo].[vwStatsForLevel1DBMaintenance]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ==========================================================================================
-- Description:	Returns a list of tables that have had at least 1000 modifications since the
--				last time statistics were updated.
-- ==========================================================================================
CREATE VIEW [dbo].[vwStatsForLevel1DBMaintenance]
AS
WITH TableStats AS (
    SELECT sch.name AS SchemaName,
        tbl.name AS TableName,
        SUM(ps.row_count) AS RecordCount,
        SUM(ps.used_page_count * 8) AS UsedSpaceKB,
        SUM(ps.reserved_page_count * 8) AS ReservedSpaceKB,
        (SUM(ps.reserved_page_count) - SUM(ps.used_page_count)) * 8 AS FreeSpaceKB,
        MAX(sp.modification_counter) AS ModsSinceLastStatsUpdate,
        MAX(sp.last_updated) AS LastStatsUpdate
    FROM sys.tables tbl
    INNER JOIN sys.schemas sch 
		ON tbl.schema_id = sch.schema_id
    INNER JOIN sys.dm_db_partition_stats ps 
		ON tbl.object_id = ps.object_id
    INNER JOIN sys.stats st 
		ON tbl.object_id = st.object_id
    CROSS APPLY sys.dm_db_stats_properties(tbl.object_id, st.stats_id) sp
    GROUP BY sch.name, tbl.name
)
SELECT 
    ROW_NUMBER() OVER (ORDER BY ModsSinceLastStatsUpdate DESC) AS RowId,
    SchemaName,
    TableName,
    RecordCount,
    UsedSpaceKB,
    ReservedSpaceKB,
    FreeSpaceKB,
    ModsSinceLastStatsUpdate,
    CASE 
        WHEN RecordCount > 0 THEN (CAST(ModsSinceLastStatsUpdate AS FLOAT) / RecordCount) * 100
        ELSE 0
    END AS PctModsSinceLastStatsUpdate,
    LastStatsUpdate
FROM TableStats
WHERE ModsSinceLastStatsUpdate > 1000;
GO
/****** Object:  View [dbo].[vwStatsForLevel2DBMaintenance]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ==========================================================================================
-- Description:	Returns a list of indexes that are at least 1KB in size with fragmentation of
--				over 10 percent. Includes stats by individual partition as well as
--				TotalPartitions and HighFragPartitions columns which show the number of 
--				partitions in the subject table as well as the number of partitions with 
--				fragmentation of over 30% for the subject index. These can help in
--				determining whether to reorgainize or rebuild an entire index or just 
--				specific partitions.
-- ==========================================================================================
CREATE VIEW [dbo].[vwStatsForLevel2DBMaintenance]
AS
WITH RawPartitionStats AS (
    SELECT sch.name AS SchemaName,
        obj.name AS TableName,
        idx.name AS IndexName,
        ps.partition_number AS PartitionNumber,
        ps.avg_fragmentation_in_percent AS FragmentationPct,
        ps.page_count * 8 AS IndexSizeKB
    FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'DETAILED') ps
    INNER JOIN sys.indexes idx 
		ON ps.object_id = idx.object_id AND ps.index_id = idx.index_id
    INNER JOIN sys.objects obj 
		ON idx.object_id = obj.object_id
    INNER JOIN sys.schemas sch 
		ON obj.schema_id = sch.schema_id
    WHERE ps.index_id > 0 -- (exclude heap indexes)
),
PartitionStats AS (
    SELECT SchemaName,
        TableName,
        IndexName,
        PartitionNumber,
        MAX(FragmentationPct) AS FragmentationPct,
        MAX(IndexSizeKB) AS IndexSizeKB
    FROM RawPartitionStats
    GROUP BY SchemaName, TableName, IndexName, PartitionNumber
),
AggregateStats AS (
    SELECT SchemaName,
        TableName,
        IndexName,
        COUNT(DISTINCT PartitionNumber) AS TotalPartitions,
        SUM(CASE WHEN FragmentationPct > 30 THEN 1 ELSE 0 END) AS HighFragPartitions
    FROM PartitionStats
    GROUP BY SchemaName, TableName, IndexName
)
SELECT ROW_NUMBER() OVER (ORDER BY ps.TableName ASC, ps.IndexName ASC, ps.PartitionNumber ASC) AS RowId,
    ps.SchemaName,
    ps.TableName,
    ps.IndexName,
    ps.PartitionNumber,
    ps.FragmentationPct,
    ps.IndexSizeKB,
    ag.TotalPartitions,
    ag.HighFragPartitions,
    CASE 
        WHEN ag.TotalPartitions > 0 THEN (CAST(ag.HighFragPartitions AS FLOAT) / ag.TotalPartitions) * 100
        ELSE 0
    END AS PctHighFragPartitions
FROM 
    PartitionStats ps
INNER JOIN 
    AggregateStats ag
    ON ps.SchemaName = ag.SchemaName
    AND ps.TableName = ag.TableName
    AND ps.IndexName = ag.IndexName
WHERE 
    ps.FragmentationPct > 10
    AND ps.IndexSizeKB > 1;
GO
/****** Object:  Table [dbo].[DBMaintenanceLogs2]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DBMaintenanceLogs2](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[MaintenanceTypeId] [tinyint] NOT NULL,
	[StartTimeUtc] [datetime2](7) NOT NULL,
	[EndTimeUtc] [datetime2](7) NULL,
	[Success] [bit] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DBMaintenanceLogs2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Devices2]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Devices2](
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
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Devices2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DiagnosticIds2]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DiagnosticIds2](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabGUIDString] [nvarchar](100) NOT NULL,
	[GeotabId] [nvarchar](100) NOT NULL,
	[HasShimId] [bit] NOT NULL,
	[FormerShimGeotabGUIDString] [nvarchar](100) NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DiagnosticIds2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UK_DiagnosticIds2] UNIQUE NONCLUSTERED 
(
	[GeotabGUIDString] ASC,
	[GeotabId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Diagnostics2]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Diagnostics2](
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
 CONSTRAINT [PK_Diagnostics2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EntityMetadata2]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EntityMetadata2](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[DeviceId] [bigint] NOT NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[EntityType] [tinyint] NOT NULL,
	[EntityId] [bigint] NOT NULL,
	[IsDeleted] [bit] NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_EntityMetadata2] PRIMARY KEY CLUSTERED 
(
	[id] ASC,
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Table [dbo].[FaultData2]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FaultData2](
	[id] [bigint] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[AmberWarningLamp] [bit] NULL,
	[ClassCode] [nvarchar](50) NULL,
	[ControllerId] [nvarchar](100) NOT NULL,
	[ControllerName] [nvarchar](255) NULL,
	[Count] [int] NOT NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeviceId] [bigint] NOT NULL,
	[DiagnosticId] [bigint] NOT NULL,
	[DismissDateTime] [datetime2](7) NULL,
	[DismissUserId] [bigint] NULL,
	[FailureModeCode] [int] NULL,
	[FailureModeId] [nvarchar](50) NOT NULL,
	[FailureModeName] [nvarchar](255) NULL,
	[FaultLampState] [nvarchar](50) NULL,
	[FaultState] [nvarchar](50) NULL,
	[MalfunctionLamp] [bit] NULL,
	[ProtectWarningLamp] [bit] NULL,
	[RedStopLamp] [bit] NULL,
	[Severity] [nvarchar](50) NULL,
	[SourceAddress] [int] NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_FaultData2] PRIMARY KEY CLUSTERED 
(
	[id] ASC,
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Table [dbo].[Groups2]    Script Date: 2025-03-01 4:38:17 PM ******/
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
/****** Object:  Table [dbo].[LogRecords2]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LogRecords2](
	[id] [bigint] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeviceId] [bigint] NOT NULL,
	[Latitude] [float] NOT NULL,
	[Longitude] [float] NOT NULL,
	[Speed] [real] NOT NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_LogRecords2] PRIMARY KEY CLUSTERED 
(
	[id] ASC,
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Table [dbo].[MiddlewareVersionInfo2]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MiddlewareVersionInfo2](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[DatabaseVersion] [nvarchar](50) NOT NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_MiddlewareVersionInfo2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MyGeotabVersionInfo2]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MyGeotabVersionInfo2](
	[DatabaseName] [nvarchar](58) NOT NULL,
	[Server] [nvarchar](50) NOT NULL,
	[DatabaseVersion] [nvarchar](50) NOT NULL,
	[ApplicationBuild] [nvarchar](50) NOT NULL,
	[ApplicationBranch] [nvarchar](50) NOT NULL,
	[ApplicationCommit] [nvarchar](50) NOT NULL,
	[GoTalkVersion] [nvarchar](50) NOT NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[OServiceTracking2]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OServiceTracking2](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[ServiceId] [nvarchar](50) NOT NULL,
	[AdapterVersion] [nvarchar](50) NULL,
	[AdapterMachineName] [nvarchar](100) NULL,
	[EntitiesLastProcessedUtc] [datetime2](7) NULL,
	[LastProcessedFeedVersion] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_OServiceTracking2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Rules2]    Script Date: 2025-03-13 3:23:51 PM ******/
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
/****** Object:  Table [dbo].[StatusData2]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StatusData2](
	[id] [bigint] NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[Data] [float] NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeviceId] [bigint] NOT NULL,
	[DiagnosticId] [bigint] NOT NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_StatusData2] PRIMARY KEY CLUSTERED 
(
	[id] ASC,
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Table [dbo].[stg_Devices2]    Script Date: 2025-03-13 3:23:51 PM ******/
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
/****** Object:  Table [dbo].[stg_Diagnostics2]    Script Date: 2025-03-13 3:23:51 PM ******/
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
/****** Object:  Table [dbo].[stg_Groups2]    Script Date: 2025-03-18 3:00:00 PM ******/
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
/****** Object:  Table [dbo].[stg_Rules2]    Script Date: 2025-03-13 3:23:51 PM ******/
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
/****** Object:  Table [dbo].[stg_Trips2]    Script Date: 2025-03-28 1:49:00 AM ******/
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
/****** Object:  Table [dbo].[stg_Users2]    Script Date: 2025-03-13 3:23:51 PM ******/
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
/****** Object:  Table [dbo].[stg_Zones2]    Script Date: 2025-03-13 3:23:51 PM ******/
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
/****** Object:  Table [dbo].[stg_ZoneTypes2]    Script Date: 2025-03-13 3:23:51 PM ******/
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
/****** Object:  Table [dbo].[Trips2]    Script Date: 2025-03-28 1:46:00 AM ******/
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
/****** Object:  Table [dbo].[Users2]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users2](
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
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Users2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Zones2]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Zones2](
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
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Zones2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ZoneTypes2]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ZoneTypes2](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](100) NOT NULL,
	[Comment] [nvarchar](255) NULL,
	[Name] [nvarchar](255) NOT NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ZoneTypes2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Index [IX_DBMaintenanceLogs2_RecordLastChangedUtc]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_DBMaintenanceLogs2_RecordLastChangedUtc] ON [dbo].[DBMaintenanceLogs2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Devices2_RecordLastChangedUtc]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_Devices2_RecordLastChangedUtc] ON [dbo].[Devices2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Diagnostics2_RecordLastChangedUtc]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_Diagnostics2_RecordLastChangedUtc] ON [dbo].[Diagnostics2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UI_Diagnostics2_GeotabGUIDString]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UI_Diagnostics2_GeotabGUIDString] ON [dbo].[Diagnostics2]
(
	[GeotabGUIDString] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UI_EntityMetadata2_DeviceId_DateTime_EntityType]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [UI_EntityMetadata2_DeviceId_DateTime_EntityType] ON [dbo].[EntityMetadata2]
(
	[DeviceId] ASC,
	[DateTime] ASC,
	[EntityType] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Index [IX_FaultData2_DateTime]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_FaultData2_DateTime] ON [dbo].[FaultData2]
(
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Index [IX_FaultData2_DeviceId_DateTime]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_FaultData2_DeviceId_DateTime] ON [dbo].[FaultData2]
(
	[DeviceId] ASC,
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Index [UI_FaultData2_Id]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UI_FaultData2_Id] ON [dbo].[FaultData2]
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_FaultDataLocations2_DateTime_DeviceId_Filtered]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_FaultDataLocations2_DateTime_DeviceId_Filtered] ON [dbo].[FaultDataLocations2]
(
	[DateTime] ASC,
	[DeviceId] ASC
)
WHERE ([LongLatProcessed]=(0))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Index [IX_FaultDataLocations2_id_LongLatProcessed]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_FaultDataLocations2_id_LongLatProcessed] ON [dbo].[FaultDataLocations2]
(
	[id] ASC,
	[LongLatProcessed] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Index [IX_FaultDataLocations2_LongLatProcessed_DateTime_id]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_FaultDataLocations2_LongLatProcessed_DateTime_id] ON [dbo].[FaultDataLocations2]
(
	[LongLatProcessed] ASC,
	[DateTime] ASC,
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Index [UI_FaultDataLocations2_Id]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UI_FaultDataLocations2_Id] ON [dbo].[FaultDataLocations2]
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Groups2_RecordLastChangedUtc]    Script Date: 2025-03-01 4:38:25 PM ******/
CREATE NONCLUSTERED INDEX [IX_Groups2_RecordLastChangedUtc] ON [dbo].[Groups2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_LogRecords2_DateTime]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_LogRecords2_DateTime] ON [dbo].[LogRecords2]
(
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Index [UI_LogRecords2_DeviceId_DateTime]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UI_LogRecords2_DeviceId_DateTime] ON [dbo].[LogRecords2]
(
	[DeviceId] ASC,
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Index [UI_LogRecords2_Id]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UI_LogRecords2_Id] ON [dbo].[LogRecords2]
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_MyGeotabVersionInfo2_RecordCreationTimeUtc]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_MyGeotabVersionInfo2_RecordCreationTimeUtc] ON [dbo].[MyGeotabVersionInfo2]
(
	[RecordCreationTimeUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_OServiceTracking2_RecordLastChangedUtc]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_OServiceTracking2_RecordLastChangedUtc] ON [dbo].[OServiceTracking2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Rules2_RecordLastChangedUtc]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_Rules2_RecordLastChangedUtc] 
ON [dbo].[Rules2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_StatusData2_DateTime]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_StatusData2_DateTime] ON [dbo].[StatusData2]
(
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Index [IX_StatusData2_DeviceId_DateTime]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_StatusData2_DeviceId_DateTime] ON [dbo].[StatusData2]
(
	[DeviceId] ASC,
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Index [UI_StatusData2_Id]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UI_StatusData2_Id] ON [dbo].[StatusData2]
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_StatusDataLocations2_DateTime_DeviceId_Filtered]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_StatusDataLocations2_DateTime_DeviceId_Filtered] ON [dbo].[StatusDataLocations2]
(
	[DateTime] ASC,
	[DeviceId] ASC
)
WHERE ([LongLatProcessed]=(0))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Index [IX_StatusDataLocations2_id_LongLatProcessed]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_StatusDataLocations2_id_LongLatProcessed] ON [dbo].[StatusDataLocations2]
(
	[id] ASC,
	[LongLatProcessed] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Index [IX_StatusDataLocations2_LongLatProcessed_DateTime_id]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_StatusDataLocations2_LongLatProcessed_DateTime_id] ON [dbo].[StatusDataLocations2]
(
	[LongLatProcessed] ASC,
	[DateTime] ASC,
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([DateTime])
GO
/****** Object:  Index [UI_StatusDataLocations2_Id]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UI_StatusDataLocations2_Id] ON [dbo].[StatusDataLocations2]
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_stg_Devices2_id_RecordLastChangedUtc]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_stg_Devices2_id_RecordLastChangedUtc] ON [dbo].[stg_Devices2]
(
	[id] ASC,
	[RecordLastChangedUtc] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_stg_Diagnostics2_GeotabGUIDString_RecordLastChangedUtc]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_stg_Diagnostics2_GeotabGUIDString_RecordLastChangedUtc] ON [dbo].[stg_Diagnostics2]
(
	[GeotabGUIDString] ASC,
	[RecordLastChangedUtc] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_stg_Groups2_GeotabId_RecordLastChangedUtc]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_stg_Groups2_GeotabId_RecordLastChangedUtc] ON [dbo].[stg_Groups2]
(
	[GeotabId] ASC,
	[RecordLastChangedUtc] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_stg_Rules2_GeotabId_RecordLastChangedUtc]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_stg_Rules2_GeotabId_RecordLastChangedUtc] 
ON [dbo].[stg_Rules2]
(
	[GeotabId] ASC,
	[RecordLastChangedUtc] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_stg_Trips2_DeviceId_Start_EntityStatus]    Script Date: 2025-04-03 13:13:00 PM ******/
CREATE NONCLUSTERED INDEX [IX_stg_Trips2_DeviceId_Start_EntityStatus] ON [dbo].[stg_Trips2]
(
	[DeviceId] ASC,
	[Start] ASC,
	[EntityStatus] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_stg_Users2_id_RecordLastChangedUtc]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_stg_Users2_id_RecordLastChangedUtc] ON [dbo].[stg_Users2]
(
	[id] ASC,
	[RecordLastChangedUtc] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_stg_Zones2_id_RecordLastChangedUtc]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_stg_Zones2_id_RecordLastChangedUtc] ON [dbo].[stg_Zones2]
(
	[id] ASC,
	[RecordLastChangedUtc] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_stg_ZoneTypes2_GeotabId_RecordLastChangedUtc]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_stg_ZoneTypes2_GeotabId_RecordLastChangedUtc] ON [dbo].[stg_ZoneTypes2]
(
	[GeotabId] ASC,
	[RecordLastChangedUtc] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [CI_Trips2_Start_Id]    Script Date: 2025-03-27 9:51:00 PM ******/
CREATE CLUSTERED INDEX [CI_Trips2_Start_Id] ON [dbo].[Trips2]
(
	[Start] ASC,
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [DateTimePartitionScheme_MyGeotabApiAdapter]([Start])
GO
/****** Object:  Index [IX_Trips2_NextTripStart]    Script Date: 2025-03-27 9:51:00 PM ******/
CREATE NONCLUSTERED INDEX [IX_Trips2_NextTripStart] ON [dbo].[Trips2]
(
	[NextTripStart] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Trips2_RecordLastChangedUtc]    Script Date: 2025-03-27 9:51:00 PM ******/
CREATE NONCLUSTERED INDEX [IX_Trips2_RecordLastChangedUtc] ON [dbo].[Trips2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Users2_RecordLastChangedUtc]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_Users2_RecordLastChangedUtc] ON [dbo].[Users2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Zones2_RecordLastChangedUtc]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_Zones2_RecordLastChangedUtc] ON [dbo].[Zones2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ZoneTypes2_RecordLastChangedUtc]    Script Date: 2025-03-13 3:23:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_ZoneTypes2_RecordLastChangedUtc] ON [dbo].[ZoneTypes2]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[EntityMetadata2] ADD  CONSTRAINT [DF__EntityMet__IsDel__6B24EA82]  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[LogRecords2] ADD  CONSTRAINT [DF_LogRecords2_Latitude]  DEFAULT ((0)) FOR [Latitude]
GO
ALTER TABLE [dbo].[LogRecords2] ADD  CONSTRAINT [DF_LogRecords2_Longitude]  DEFAULT ((0)) FOR [Longitude]
GO
ALTER TABLE [dbo].[LogRecords2] ADD  CONSTRAINT [DF_LogRecords2_Speed]  DEFAULT ((0)) FOR [Speed]
GO
ALTER TABLE [dbo].[DiagnosticIds2]  WITH CHECK ADD  CONSTRAINT [FK_DiagnosticIds2_Diagnostics2] FOREIGN KEY([GeotabGUIDString])
REFERENCES [dbo].[Diagnostics2] ([GeotabGUIDString])
GO
ALTER TABLE [dbo].[DiagnosticIds2] CHECK CONSTRAINT [FK_DiagnosticIds2_Diagnostics2]
GO
ALTER TABLE [dbo].[DiagnosticIds2]  WITH CHECK ADD  CONSTRAINT [FK_DiagnosticIds2_Diagnostics21] FOREIGN KEY([FormerShimGeotabGUIDString])
REFERENCES [dbo].[Diagnostics2] ([GeotabGUIDString])
GO
ALTER TABLE [dbo].[DiagnosticIds2] CHECK CONSTRAINT [FK_DiagnosticIds2_Diagnostics21]
GO
ALTER TABLE [dbo].[FaultData2]  WITH NOCHECK ADD  CONSTRAINT [FK_FaultData2_Devices2] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Devices2] ([id])
GO
ALTER TABLE [dbo].[FaultData2] CHECK CONSTRAINT [FK_FaultData2_Devices2]
GO
ALTER TABLE [dbo].[FaultData2]  WITH NOCHECK ADD  CONSTRAINT [FK_FaultData2_DiagnosticIds2] FOREIGN KEY([DiagnosticId])
REFERENCES [dbo].[DiagnosticIds2] ([id])
GO
ALTER TABLE [dbo].[FaultData2] CHECK CONSTRAINT [FK_FaultData2_DiagnosticIds2]
GO
ALTER TABLE [dbo].[FaultData2]  WITH NOCHECK ADD  CONSTRAINT [FK_FaultData2_Users2] FOREIGN KEY([DismissUserId])
REFERENCES [dbo].[Users2] ([id])
GO
ALTER TABLE [dbo].[FaultData2] CHECK CONSTRAINT [FK_FaultData2_Users2]
GO
ALTER TABLE [dbo].[FaultDataLocations2]  WITH NOCHECK ADD  CONSTRAINT [FK_FaultDataLocations2_FaultData2] FOREIGN KEY([id])
REFERENCES [dbo].[FaultData2] ([id])
GO
ALTER TABLE [dbo].[FaultDataLocations2] CHECK CONSTRAINT [FK_FaultDataLocations2_FaultData2]
GO
ALTER TABLE [dbo].[LogRecords2]  WITH NOCHECK ADD  CONSTRAINT [FK_LogRecords2_Devices2] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Devices2] ([id])
GO
ALTER TABLE [dbo].[LogRecords2] CHECK CONSTRAINT [FK_LogRecords2_Devices2]
GO
ALTER TABLE [dbo].[StatusData2]  WITH NOCHECK ADD  CONSTRAINT [FK_StatusData2_Devices2] FOREIGN KEY([DeviceId])
REFERENCES [dbo].[Devices2] ([id])
GO
ALTER TABLE [dbo].[StatusData2] CHECK CONSTRAINT [FK_StatusData2_Devices2]
GO
ALTER TABLE [dbo].[StatusData2]  WITH NOCHECK ADD  CONSTRAINT [FK_StatusData2_DiagnosticIds2] FOREIGN KEY([DiagnosticId])
REFERENCES [dbo].[DiagnosticIds2] ([id])
GO
ALTER TABLE [dbo].[StatusData2] CHECK CONSTRAINT [FK_StatusData2_DiagnosticIds2]
GO
ALTER TABLE [dbo].[StatusDataLocations2]  WITH NOCHECK ADD  CONSTRAINT [FK_StatusDataLocations2_StatusData2] FOREIGN KEY([id])
REFERENCES [dbo].[StatusData2] ([id])
GO
ALTER TABLE [dbo].[StatusDataLocations2] CHECK CONSTRAINT [FK_StatusDataLocations2_StatusData2]
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
/****** Object:  StoredProcedure [dbo].[spFaultData2WithLagLeadLongLatBatch]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description: Returns a batch of FaultData2 records with additional
--              metadata about the LogRecords2 table. Each returned record
--              also contains the DateTime, Latitude and Longitude values of the LogRecord2
--              records with DateTimes immediately before (or equal to) and after the 
--              DateTime of the FaultData2 record. This result set is intended to be used
--              for interpolation of location coordinates, speed, bearing and compass
--              direction for the subject FaultData2 records.
--
-- Parameters:
--		@MaxDaysPerBatch: The maximum number of days over which unprocessed FaultData records 
--			in a batch can span.
--		@MaxBatchSize: The maximum number of unprocessed FaultData records to retrieve for 
--			interpolation per batch.
--		@BufferMinutes: When getting the DateTime range of a batch of unprocessed FaultData 
--			records, this buffer is applied to either end of the DateTime range when 
--			selecting LogRecords to use for interpolation such that lag LogRecords can be 
--			obtained for records that are “early” in the batch and lead LogRecords can be 
--			obtained for records that are “late” in the batch.
-- ==========================================================================================
CREATE PROCEDURE [dbo].[spFaultData2WithLagLeadLongLatBatch]
	@MaxDaysPerBatch INT,
	@MaxBatchSize INT,
	@BufferMinutes INT
AS
BEGIN
	-- Use READ UNCOMMITTED to reduce contention. No writes are performed in this procedure
	-- and new uncommitted data should not adversely affect results.
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
    DECLARE
		-- Constants:
		@minAllowed_maxDaysPerBatchValue INT = 1,
		@maxAllowed_maxDaysPerBatchValue INT = 10,
		@minAllowed_maxBatchSizeValue INT = 10000,
		@maxAllowed_maxBatchSizeValue INT = 500000,
		@minAllowed_bufferMinutesValue INT = 10,
		@maxAllowed_bufferMinutesValue INT = 1440,
		
		-- The maximum number of days that can be spanned in a batch.
		@maxDaysPerBatchValue INT = @MaxDaysPerBatch,
		-- The maximum number of records to return.
        @maxBatchSizeValue INT = @MaxBatchSize,
		-- Buffer period, in minutes, for fetching encompassing values.
        @bufferMinutesValue INT = @BufferMinutes,

		-- Variables:
		@faultData2PrimaryPartitionMaxDateTime DATETIME2,
        @logRecords2MinDateTime DATETIME2,
        @logRecords2MaxDateTime DATETIME2,
		@storedProcedureName NVARCHAR(128) = OBJECT_NAME(@@PROCID),
		@unprocessedFaultDataMinDateTime DATETIME2,
		@unprocessedFaultDataMaxAllowedDateTime DATETIME2,
		@unprocessedFaultData2BatchMinDateTime DATETIME2,
		@unprocessedFaultData2BatchMaxDateTime DATETIME2,
		@bufferedUnprocessedFaultData2BatchMinDateTime DATETIME2,
		@bufferedUnprocessedFaultData2BatchMaxDateTime DATETIME2,
		@storedProcedureStart_time DATETIME,
        @start_time DATETIME,
        @end_time DATETIME,
        @start_time_string VARCHAR(30),
		@duration_string VARCHAR(30),
        @record_count INT;

    SET NOCOUNT ON;

	BEGIN TRY
		-- ======================================================================================
		-- Log start of stored procedure execution.
		SET @storedProcedureStart_time = GETDATE();
		SET @start_time = GETDATE();
		SET @start_time_string = CONVERT(VARCHAR, @start_time, 121);
		RAISERROR ('Executing stored procedure ''%s''. Start: %s', 0, 1, @storedProcedureName, @start_time_string) WITH NOWAIT;		
		RAISERROR ('> @maxDaysPerBatch: %d', 0, 1, @maxDaysPerBatchValue) WITH NOWAIT;
		RAISERROR ('> @maxBatchSize: %d', 0, 1, @maxBatchSizeValue) WITH NOWAIT;
		RAISERROR ('> @bufferMinutes: %d', 0, 1, @bufferMinutesValue) WITH NOWAIT;


	    -- ======================================================================================
		-- STEP 1: Validate input parameter values.
		RAISERROR ('Step 1 [Validating input parameter values]...', 0, 1) WITH NOWAIT;
		
		-- MaxDaysPerBatch
		IF @maxDaysPerBatchValue < @minAllowed_maxDaysPerBatchValue OR @maxDaysPerBatchValue > @maxAllowed_maxDaysPerBatchValue
		BEGIN
			RAISERROR('ERROR: @MaxDaysPerBatch (%d) is out of the allowed range [%d, %d].', 16, 1, 
				@maxDaysPerBatchValue, @minAllowed_maxDaysPerBatchValue, @maxAllowed_maxDaysPerBatchValue);
			RETURN;		
		END;
		
		-- MaxBatchSize
		IF @maxBatchSizeValue < @minAllowed_maxBatchSizeValue OR @maxBatchSizeValue > @maxAllowed_maxBatchSizeValue
		BEGIN
			RAISERROR('ERROR: @MaxBatchSize (%d) is out of the allowed range [%d, %d].', 16, 1, 
				@maxBatchSizeValue, @minAllowed_maxBatchSizeValue, @maxAllowed_maxBatchSizeValue);
			RETURN;		
		END;

		-- BufferMinutes
		IF @bufferMinutesValue < @minAllowed_bufferMinutesValue OR @bufferMinutesValue > @maxAllowed_bufferMinutesValue
		BEGIN
			RAISERROR('ERROR: @BufferMinutes (%d) is out of the allowed range [%d, %d].', 16, 1, 
				@bufferMinutesValue, @minAllowed_bufferMinutesValue, @maxAllowed_bufferMinutesValue);
			RETURN;		
		END;
		
		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('Step 1 completed. Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 2: 
		-- Get the max DateTime value from the PRIMARY partition in the FaultData2 table.
		WITH PartitionMinMax AS (
			SELECT 
				P.object_id,
				P.partition_number,
				MIN(S.DateTime) AS MinDateTime,
				MAX(S.DateTime) AS MaxDateTime
			FROM 
				sys.partitions P
			JOIN 
				[dbo].[FaultData2] S
			ON 
				P.object_id = OBJECT_ID('dbo.FaultData2') 
				AND P.partition_number = $PARTITION.DateTimePartitionFunction_MyGeotabApiAdapter(S.DateTime)
			WHERE 
				P.index_id IN (0, 1)  -- 0 = heap, 1 = clustered index
			GROUP BY 
				P.object_id, P.partition_number
		)
		SELECT @faultData2PrimaryPartitionMaxDateTime = ISNULL(
			(
				SELECT MAX(PartitionMinMax.MaxDateTime)
				FROM (
					SELECT DISTINCT
						PartitionMinMax.MaxDateTime
					FROM 
						sys.partitions P
					JOIN 
						sys.objects T 
						ON P.object_id = T.object_id
					JOIN 
						PartitionMinMax 
						ON P.object_id = PartitionMinMax.object_id 
						AND P.partition_number = PartitionMinMax.partition_number
					JOIN 
						sys.allocation_units AU 
						ON P.partition_id = AU.container_id
					JOIN 
						sys.data_spaces DS 
						ON AU.data_space_id = DS.data_space_id
					JOIN 
						sys.filegroups FG 
						ON DS.data_space_id = FG.data_space_id
					WHERE 
						T.name = 'FaultData2'
						AND FG.name = 'PRIMARY'
						AND AU.type = 1  -- Only include IN_ROW_DATA to avoid duplication.
				) AS PartitionMinMax
			), '1900-01-01'); -- Default value if the query result is NULL

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		DECLARE @faultData2PrimaryPartitionMaxDateTime_string VARCHAR(30) = FORMAT(@faultData2PrimaryPartitionMaxDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');
		RAISERROR ('STEP 2 Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		RAISERROR ('> @faultData2PrimaryPartitionMaxDateTime: %s', 0, 1, @faultData2PrimaryPartitionMaxDateTime_string) WITH NOWAIT;
		SET @start_time = @end_time;

	
		-- ======================================================================================
		-- STEP 3: 
		-- Get min and max DateTime values by Device for unprocessed FaultDataLocations2 
		-- records. Also get associated min and max DateTime values for LogRecords2 records where
		-- the DateTimes of the LogRecords are greater than or equal to the min DateTimes of the 
		-- unprocessed FaultDataLocations2 records. Do this ONLY for Devices that have both 
		-- FaultData and LogRecords. Exclude data from the PRIMARY partition.
		DROP TABLE IF EXISTS #TMP_DeviceDataMinMaxDateTimes;

		WITH FaultDataMinMax AS (
			SELECT 
				fdl.DeviceId,
				MIN(fdl.DateTime) AS DeviceFaultData2MinDateTime,
				MAX(fdl.DateTime) AS DeviceFaultData2MaxDateTime
			FROM dbo.FaultDataLocations2 fdl
			WHERE fdl.DateTime > @faultData2PrimaryPartitionMaxDateTime
			  AND fdl.LongLatProcessed = 0
			GROUP BY fdl.DeviceId
		),
		FilteredLogRecords AS (
			SELECT 
				lr.DeviceId,
				MIN(lr.DateTime) AS DeviceLogRecords2MinDateTime,
				MAX(lr.DateTime) AS DeviceLogRecords2MaxDateTime
			FROM dbo.LogRecords2 lr
			WHERE lr.DateTime >= (SELECT MIN(DeviceFaultData2MinDateTime) FROM FaultDataMinMax)
			GROUP BY lr.DeviceId
		)
		SELECT 
			fd.DeviceId,
			fd.DeviceFaultData2MinDateTime,
			fd.DeviceFaultData2MaxDateTime,
			flr.DeviceLogRecords2MinDateTime,
			flr.DeviceLogRecords2MaxDateTime
		INTO #TMP_DeviceDataMinMaxDateTimes
		FROM FaultDataMinMax fd
		INNER JOIN FilteredLogRecords flr
			ON fd.DeviceId = flr.DeviceId;

		-- Log.
		SET @record_count = @@ROWCOUNT;
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('STEP 3 (Create #TMP_DeviceDataMinMaxDateTimes) Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		RAISERROR ('> Record Count: %d', 0, 1, @record_count) WITH NOWAIT;	
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 3A: 
		-- Add indexes to temporary table.
		CREATE INDEX IX_TMP_DeviceDataMinMaxDateTimes_DeviceId 
		ON #TMP_DeviceDataMinMaxDateTimes (DeviceId);

		CREATE INDEX IX_TMP_DeviceDataMinMaxDateTimes_DeviceLogRecords2MinDateTime 
		ON #TMP_DeviceDataMinMaxDateTimes (DeviceLogRecords2MinDateTime);

		CREATE INDEX IX_TMP_DeviceDataMinMaxDateTimes_DeviceLogRecords2MaxDateTime 
		ON #TMP_DeviceDataMinMaxDateTimes (DeviceLogRecords2MaxDateTime);

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('STEP 3A (Index #TMP_DeviceDataMinMaxDateTimes) Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 4:
		-- Get the DateTime of the unprocessed FaultDataLocations2 record with the lowest 
		-- DateTime value that is greater than the max DateTime value from the PRIMARY partition 
		-- in the FaultDataLocations2 table.
		SELECT @unprocessedFaultDataMinDateTime = MIN(ddmm.DeviceFaultData2MinDateTime)
		FROM #TMP_DeviceDataMinMaxDateTimes ddmm;

		-- Determine the maximum allowed DateTime for the current batch based on adding @maxDaysPerBatchValue
		-- to the @unprocessedFaultDataMinDateTime.
		SELECT @unprocessedFaultDataMaxAllowedDateTime = DATEADD(SECOND, -1, DATEADD(DAY, @maxDaysPerBatchValue
		, CAST(CAST((@unprocessedFaultDataMinDateTime) AS DATE) AS DATETIME)));

		-- Get the minimun DateTime value of any LogRecord.
		SELECT @logRecords2MinDateTime = MIN(ddmm.DeviceLogRecords2MinDateTime)
		FROM #TMP_DeviceDataMinMaxDateTimes ddmm;

		-- Get the maximum DateTime value of any LogRecord.
		SELECT @logRecords2MaxDateTime = MAX(ddmm.DeviceLogRecords2MaxDateTime)
		FROM #TMP_DeviceDataMinMaxDateTimes ddmm;

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		DECLARE @unprocessedFaultDataMinDateTime_string VARCHAR(30) = FORMAT(@unprocessedFaultDataMinDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');
		DECLARE @unprocessedFaultDataMaxAllowedDateTime_string VARCHAR(30) = FORMAT(@unprocessedFaultDataMaxAllowedDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');
		DECLARE @logRecords2MinDateTime_string VARCHAR(30) = FORMAT(@logRecords2MinDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');
		DECLARE @logRecords2MaxDateTime_string VARCHAR(30) = FORMAT(@logRecords2MaxDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');
		RAISERROR ('STEP 4 Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		RAISERROR ('> @unprocessedFaultDataMinDateTime: %s', 0, 1, @unprocessedFaultDataMinDateTime_string) WITH NOWAIT;
		RAISERROR ('> @unprocessedFaultDataMaxAllowedDateTime: %s', 0, 1, @unprocessedFaultDataMaxAllowedDateTime_string) WITH NOWAIT;
		RAISERROR ('> @logRecords2MinDateTime: %s', 0, 1, @logRecords2MinDateTime_string) WITH NOWAIT;
		RAISERROR ('> @logRecords2MaxDateTime: %s', 0, 1, @logRecords2MaxDateTime_string) WITH NOWAIT;
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 5:
		-- Get up to the first @maxBatchSizeValue of FaultDataLocations2 records within the 
		-- @unprocessedFaultDataMinDateTime to @unprocessedFaultDataMaxAllowedDateTime DateTime 
		-- range. Also make sure not to exceed the @logRecords2MaxDateTime.
		SELECT TOP (@maxBatchSizeValue) 
			fdl.id
		INTO #TMP_BatchFaultDataLocationIds
		FROM FaultDataLocations2 fdl
		INNER JOIN #TMP_DeviceDataMinMaxDateTimes ddmmdt
			ON fdl.DeviceId = ddmmdt.DeviceId 
		WHERE fdl.LongLatProcessed = 0 
			AND fdl.DateTime BETWEEN (@unprocessedFaultDataMinDateTime) AND (@unprocessedFaultDataMaxAllowedDateTime)
			AND fdl.DateTime <= @logRecords2MaxDateTime;

		-- Log.
		SET @record_count = @@ROWCOUNT;
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('STEP 5 (Create #TMP_BatchFaultDataLocationIds) Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		RAISERROR ('> Record Count: %d', 0, 1, @record_count) WITH NOWAIT;	
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 5A: 
		-- Add index to temporary table.
		CREATE INDEX IX_TMP_BatchFaultDataLocationIds_id
		ON #TMP_BatchFaultDataLocationIds (id);

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('STEP 5A (Index #TMP_BatchFaultDataLocationIds) Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 6:
		-- Get the batch of unprocessed FaultData2 records.
		DROP TABLE IF EXISTS #TMP_UnprocessedFaultData2Batch;

		SELECT 
			bfdl.id,
			fd.GeotabId,
			fdl.DateTime,
			fdl.DeviceId,
			@logRecords2MinDateTime AS LogRecords2MinDateTime,
			@logRecords2MaxDateTime AS LogRecords2MaxDateTime,
			dlrmm.DeviceLogRecords2MinDateTime,
			dlrmm.DeviceLogRecords2MaxDateTime
		INTO #TMP_UnprocessedFaultData2Batch
		FROM #TMP_BatchFaultDataLocationIds bfdl
		LEFT JOIN dbo.FaultDataLocations2 fdl 
			ON bfdl.id = fdl.id
		LEFT JOIN #TMP_DeviceDataMinMaxDateTimes dlrmm 
			ON fdl.DeviceId = dlrmm.DeviceId
		LEFT JOIN dbo.FaultData2 fd
			ON bfdl.id = fd.id
		WHERE fd.DateTime BETWEEN @unprocessedFaultDataMinDateTime AND @unprocessedFaultDataMaxAllowedDateTime;

		-- Log.
		SET @record_count = @@ROWCOUNT;
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('STEP 6 (Create TMP_UnprocessedFaultData2Batch) Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		RAISERROR ('> Record Count: %d', 0, 1, @record_count) WITH NOWAIT;	
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 6A:
		-- Add indexes to the temporary table.
		CREATE INDEX IX_TMP_UnprocessedFaultData2Batch_id 
		ON #TMP_UnprocessedFaultData2Batch (id);

		CREATE INDEX IX_TMP_UnprocessedFaultData2Batch_DateTime 
		ON #TMP_UnprocessedFaultData2Batch (DateTime);

		CREATE INDEX IX_TMP_UnprocessedFaultData2Batch_DeviceId_DateTime 
		ON #TMP_UnprocessedFaultData2Batch (DeviceId, DateTime);

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('STEP 6A (Index TMP_UnprocessedFaultData2Batch) Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 7:
		-- Get the min and max DateTime values from #TMP_UnprocessedFaultData2Batch.
		SELECT @unprocessedFaultData2BatchMinDateTime = MIN(DateTime)
		FROM #TMP_UnprocessedFaultData2Batch;

		SELECT @unprocessedFaultData2BatchMaxDateTime = MAX(DateTime)
		FROM #TMP_UnprocessedFaultData2Batch;

		SELECT @bufferedUnprocessedFaultData2BatchMinDateTime = DATEADD(MINUTE, -@bufferMinutesValue, @unprocessedFaultData2BatchMinDateTime);
		SELECT @bufferedUnprocessedFaultData2BatchMaxDateTime = DATEADD(MINUTE, @bufferMinutesValue, @unprocessedFaultData2BatchMaxDateTime);

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		DECLARE @unprocessedFaultData2BatchMinDateTime_string VARCHAR(30) = FORMAT(@unprocessedFaultData2BatchMinDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');
		DECLARE @unprocessedFaultData2BatchMaxDateTime_string VARCHAR(30) = FORMAT(@unprocessedFaultData2BatchMaxDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');
		DECLARE @bufferedUnprocessedFaultData2BatchMinDateTime_string VARCHAR(30) = FORMAT(@bufferedUnprocessedFaultData2BatchMinDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');
		DECLARE @bufferedUnprocessedFaultData2BatchMaxDateTime_string VARCHAR(30) = FORMAT(@bufferedUnprocessedFaultData2BatchMaxDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');	
		RAISERROR ('STEP 7 Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		RAISERROR ('> @unprocessedFaultData2BatchMinDateTime: %s', 0, 1, @unprocessedFaultData2BatchMinDateTime_string) WITH NOWAIT;
		RAISERROR ('> @@unprocessedFaultData2BatchMaxDateTime: %s', 0, 1, @unprocessedFaultData2BatchMaxDateTime_string) WITH NOWAIT;
		RAISERROR ('> @bufferedUnprocessedFaultData2BatchMinDateTime: %s', 0, 1, @bufferedUnprocessedFaultData2BatchMinDateTime_string) WITH NOWAIT;
		RAISERROR ('> @bufferedUnprocessedFaultData2BatchMaxDateTime: %s', 0, 1, @bufferedUnprocessedFaultData2BatchMaxDateTime_string) WITH NOWAIT;		
		SET @start_time = @end_time;


		-- ======================================================================================
		-- Combine steps using CTEs: 
		WITH LogRecordsForUnprocessedFaultData2Batch AS (
			-- STEP 8: 
			-- Get all records from the LogRecords2 table that fall between 
			-- @bufferedUnprocessedFaultData2BatchMinDateTime and 
			-- @bufferedUnprocessedFaultData2BatchMaxDateTime to capture additional LogRecords needed 
			-- for interpolation of the FaultData records near the beginning and end of the batch.		
			SELECT *
			FROM LogRecords2 lr
			WHERE DateTime BETWEEN @bufferedUnprocessedFaultData2BatchMinDateTime 
				AND @bufferedUnprocessedFaultData2BatchMaxDateTime		
		),
		LogRecordsWithLeads AS (
			-- STEP 9: 
			-- Get all records from LogRecordsForUnprocessedFaultData2Batch and add the
			-- LeadDateTime, LeadLatitude and LeadLongitude to each record.
			SELECT *,
				LEAD(DateTime) OVER (PARTITION BY DeviceId ORDER BY DateTime) AS LeadDateTime,
				LEAD(Latitude) OVER (PARTITION BY DeviceId ORDER BY DateTime) AS LeadLatitude,
				LEAD(Longitude) OVER (PARTITION BY DeviceId ORDER BY DateTime) AS LeadLongitude
			FROM LogRecordsForUnprocessedFaultData2Batch lrb
		)
		-- STEP 10:
		-- Join the LogRecordsWithLeads to the records in the #TMP_UnprocessedFaultData2Batch where 
		-- the DeviceIds match and the DateTime of the FaultData2 record falls within the 
		-- DateTime-to-LeadDateTime range of the LogRecord2 record. Filter-out any duplicate values 
		-- which may be present in cases where a LogRecord-FaultData record pair have the exact 
		-- same DateTime values. In such cases, discard the "lead" LogRecord and keep the "lag" 
		-- LogRecord as its location coordinates will be reflective of the actual location of the 
		-- subject Device at the timestamp. Note that any records from the #TMP_UnprocessedFaultData2Batch
		-- that don't have matching LogRecordsWithLeads value (and would not otherwise be excluded
		-- by the duplicate check) will be included. This is to avoid the situation in which the
		-- combination of data + @maxDaysPerBatchValue + @maxBatchSizeValue + @bufferMinutesValue might result
		-- in no records being returned and therefore rendeing this procedure effectively unable 
		-- to process any further data.  
		SELECT fdb.id,
			fdb.GeotabId,
			fdb.DateTime AS FaultDataDateTime,
			fdb.DeviceId,
			CASE
				WHEN lrwl1.DeviceId IS NOT NULL THEN lrwl1.DateTime
				WHEN lrwl2.DeviceId IS NOT NULL THEN lrwl2.DateTime
				WHEN lrwl3.DeviceId IS NOT NULL THEN lrwl3.DateTime
				ELSE NULL 
			END AS LagDateTime,
			CASE 
				WHEN lrwl1.DeviceId IS NOT NULL THEN lrwl1.Latitude
				WHEN lrwl2.DeviceId IS NOT NULL THEN lrwl2.Latitude
				WHEN lrwl3.DeviceId IS NOT NULL THEN lrwl3.Latitude
				ELSE NULL
			END AS LagLatitude,
			CASE 
				WHEN lrwl1.DeviceId IS NOT NULL THEN lrwl1.Longitude
				WHEN lrwl2.DeviceId IS NOT NULL THEN lrwl2.Longitude
				WHEN lrwl3.DeviceId IS NOT NULL THEN lrwl3.Longitude
				ELSE NULL
			END AS LagLongitude,
			CASE
				WHEN lrwl1.DeviceId IS NOT NULL THEN lrwl1.Speed
				WHEN lrwl2.DeviceId IS NOT NULL THEN lrwl2.Speed
				WHEN lrwl3.DeviceId IS NOT NULL THEN lrwl3.Speed
				ELSE NULL
			END AS LagSpeed,
			CASE 
				WHEN lrwl1.DeviceId IS NOT NULL THEN lrwl1.LeadDateTime
				WHEN lrwl2.DeviceId IS NOT NULL THEN lrwl2.LeadDateTime
				WHEN lrwl3.DeviceId IS NOT NULL THEN lrwl3.LeadDateTime
				ELSE NULL
			END AS LeadDateTime,
			CASE 
				WHEN lrwl1.DeviceId IS NOT NULL THEN lrwl1.LeadLatitude
				WHEN lrwl2.DeviceId IS NOT NULL THEN lrwl2.LeadLatitude
				WHEN lrwl3.DeviceId IS NOT NULL THEN lrwl3.LeadLatitude
				ELSE NULL
			END AS LeadLatitude,
			CASE 
				WHEN lrwl1.DeviceId IS NOT NULL THEN lrwl1.LeadLongitude
				WHEN lrwl2.DeviceId IS NOT NULL THEN lrwl2.LeadLongitude
				WHEN lrwl3.DeviceId IS NOT NULL THEN lrwl3.LeadLongitude
				ELSE NULL
			END AS LeadLongitude,
			fdb.LogRecords2MinDateTime,
			fdb.LogRecords2MaxDateTime
		FROM #TMP_UnprocessedFaultData2Batch fdb
		LEFT JOIN LogRecordsWithLeads lrwl1 
			ON fdb.DeviceId = lrwl1.DeviceId
				AND fdb.DateTime = lrwl1.DateTime
				AND fdb.DateTime <= lrwl1.LeadDateTime
		LEFT JOIN LogRecordsWithLeads lrwl2 
			ON fdb.DeviceId = lrwl2.DeviceId
				AND fdb.DateTime > lrwl2.DateTime
				AND fdb.DateTime <= lrwl2.LeadDateTime
		LEFT JOIN LogRecordsWithLeads lrwl3 
			ON fdb.DeviceId = lrwl3.DeviceId
				AND fdb.DateTime >= lrwl3.DateTime
				AND lrwl3.LeadDateTime IS NULL
		ORDER BY fdb.DateTime;
		
		-- Log.
		SET @record_count = @@ROWCOUNT;
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('STEPS 8-10 (LogRecordsWithLeads and final output) Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		RAISERROR ('> Record Count: %d', 0, 1, @record_count) WITH NOWAIT;	
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 11: 
		-- Drop temporary tables.
		DROP TABLE IF EXISTS #TMP_DeviceDataMinMaxDateTimes;
		DROP TABLE IF EXISTS #TMP_BatchFaultDataLocationIds;
		DROP TABLE IF EXISTS #TMP_UnprocessedFaultData2Batch;

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('STEP 11 (Drop temporary tables) Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		SET @start_time = @end_time;
		

		-- ======================================================================================
		-- Log end of stored procedure execution.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @storedProcedureStart_time, @end_time));
		RAISERROR ('Stored procedure ''%s'' executed successfully. Total Duration: %s milliseconds', 0, 1, @storedProcedureName, @duration_string) WITH NOWAIT;	
    END TRY
    BEGIN CATCH
		-- Ensure temporary table cleanup on error.
		DROP TABLE IF EXISTS #TMP_DeviceDataMinMaxDateTimes;
		DROP TABLE IF EXISTS #TMP_BatchFaultDataLocationIds;
		DROP TABLE IF EXISTS #TMP_UnprocessedFaultData2Batch;

        -- Rethrow the error.
        THROW;
    END CATCH
END;
GO
/****** Object:  StoredProcedure [dbo].[spMerge_stg_Devices2]    Script Date: 2025-03-13 3:23:51 PM ******/
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
CREATE   PROCEDURE [dbo].[spMerge_stg_Devices2]
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
/****** Object:  StoredProcedure [dbo].[spMerge_stg_Diagnostics2]    Script Date: 2025-03-13 3:23:51 PM ******/
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
CREATE   PROCEDURE [dbo].[spMerge_stg_Diagnostics2]
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
/****** Object:  StoredProcedure [dbo].[spMerge_stg_Groups2]    Script Date: 2025-03-18 3:00:00 PM ******/
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
/****** Object:  StoredProcedure [dbo].[spMerge_stg_Rules2]    Script Date: 2025-03-18 3:00:00 PM ******/
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
/****** Object:  StoredProcedure [dbo].[spMerge_stg_Trips2]    Script Date: 2025-03-27 9:55:00 PM ******/
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
/****** Object:  StoredProcedure [dbo].[spMerge_stg_Users2]    Script Date: 2025-03-13 3:23:51 PM ******/
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
CREATE   PROCEDURE [dbo].[spMerge_stg_Users2]
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
/****** Object:  StoredProcedure [dbo].[spMerge_stg_Zones2]    Script Date: 2025-03-13 3:23:51 PM ******/
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
CREATE   PROCEDURE [dbo].[spMerge_stg_Zones2]
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
/****** Object:  StoredProcedure [dbo].[spMerge_stg_ZoneTypes2]    Script Date: 2025-03-13 3:23:51 PM ******/
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
CREATE   PROCEDURE [dbo].[spMerge_stg_ZoneTypes2]
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
/****** Object:  StoredProcedure [dbo].[spStatusData2WithLagLeadLongLatBatch]    Script Date: 2025-03-13 3:23:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description: Returns a batch of StatusData2 records with additional
--              metadata about the LogRecords2 table. Each returned record
--              also contains the DateTime, Latitude and Longitude values of the LogRecord2
--              records with DateTimes immediately before (or equal to) and after the 
--              DateTime of the StatusData2 record. This result set is intended to be used
--              for interpolation of location coordinates, speed, bearing and compass
--              direction for the subject StatusData2 records.
--
-- Parameters:
--		@MaxDaysPerBatch: The maximum number of days over which unprocessed StatusData records 
--			in a batch can span.
--		@MaxBatchSize: The maximum number of unprocessed StatusData records to retrieve for 
--			interpolation per batch.
--		@BufferMinutes: When getting the DateTime range of a batch of unprocessed StatusData 
--			records, this buffer is applied to either end of the DateTime range when 
--			selecting LogRecords to use for interpolation such that lag LogRecords can be 
--			obtained for records that are “early” in the batch and lead LogRecords can be 
--			obtained for records that are “late” in the batch.
-- ==========================================================================================
CREATE PROCEDURE [dbo].[spStatusData2WithLagLeadLongLatBatch]
	@MaxDaysPerBatch INT,
	@MaxBatchSize INT,
	@BufferMinutes INT
AS
BEGIN
	-- Use READ UNCOMMITTED to reduce contention. No writes are performed in this procedure
	-- and new uncommitted data should not adversely affect results.
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	DECLARE
		-- Constants:
		@minAllowed_maxDaysPerBatchValue INT = 1,
		@maxAllowed_maxDaysPerBatchValue INT = 10,
		@minAllowed_maxBatchSizeValue INT = 10000,
		@maxAllowed_maxBatchSizeValue INT = 500000,
		@minAllowed_bufferMinutesValue INT = 10,
		@maxAllowed_bufferMinutesValue INT = 1440,
		
		-- The maximum number of days that can be spanned in a batch.
		@maxDaysPerBatchValue INT = @MaxDaysPerBatch,
		-- The maximum number of records to return.
		@maxBatchSizeValue INT = @MaxBatchSize,
		-- Buffer period, in minutes, for fetching encompassing values.
		@bufferMinutesValue INT = @BufferMinutes,

		-- Variables:
		@statusData2PrimaryPartitionMaxDateTime DATETIME2,
        @logRecords2MinDateTime DATETIME2,
        @logRecords2MaxDateTime DATETIME2,
		@unprocessedStatusDataMinDateTime DATETIME2,
		@unprocessedStatusDataMaxAllowedDateTime DATETIME2,
		@unprocessedStatusData2BatchMinDateTime DATETIME2,
		@unprocessedStatusData2BatchMaxDateTime DATETIME2,
		@bufferedUnprocessedStatusData2BatchMinDateTime DATETIME2,
		@bufferedUnprocessedStatusData2BatchMaxDateTime DATETIME2,		
		@storedProcedureName NVARCHAR(128) = OBJECT_NAME(@@PROCID),
		@storedProcedureStart_time DATETIME,
        @start_time DATETIME,
        @end_time DATETIME,
        @start_time_string VARCHAR(30),
		@duration_string VARCHAR(30),
        @record_count INT;

	SET NOCOUNT ON;

	BEGIN TRY
		-- ======================================================================================
		-- Log start of stored procedure execution.
		SET @storedProcedureStart_time = GETDATE();
		SET @start_time = GETDATE();
		SET @start_time_string = CONVERT(VARCHAR, @start_time, 121);
		RAISERROR ('Executing stored procedure ''%s''. Start: %s', 0, 1, @storedProcedureName, @start_time_string) WITH NOWAIT;		
		RAISERROR ('> @maxDaysPerBatch: %d', 0, 1, @maxDaysPerBatchValue) WITH NOWAIT;
		RAISERROR ('> @maxBatchSize: %d', 0, 1, @maxBatchSizeValue) WITH NOWAIT;
		RAISERROR ('> @bufferMinutes: %d', 0, 1, @bufferMinutesValue) WITH NOWAIT;


	    -- ======================================================================================
		-- STEP 1: Validate input parameter values.
		RAISERROR ('Step 1 [Validating input parameter values]...', 0, 1) WITH NOWAIT;
		
		-- MaxDaysPerBatch
		IF @maxDaysPerBatchValue < @minAllowed_maxDaysPerBatchValue OR @maxDaysPerBatchValue > @maxAllowed_maxDaysPerBatchValue
		BEGIN
			RAISERROR('ERROR: @MaxDaysPerBatch (%d) is out of the allowed range [%d, %d].', 16, 1, 
				@maxDaysPerBatchValue, @minAllowed_maxDaysPerBatchValue, @maxAllowed_maxDaysPerBatchValue);
			RETURN;		
		END;
		
		-- MaxBatchSize
		IF @maxBatchSizeValue < @minAllowed_maxBatchSizeValue OR @maxBatchSizeValue > @maxAllowed_maxBatchSizeValue
		BEGIN
			RAISERROR('ERROR: @MaxBatchSize (%d) is out of the allowed range [%d, %d].', 16, 1, 
				@maxBatchSizeValue, @minAllowed_maxBatchSizeValue, @maxAllowed_maxBatchSizeValue);
			RETURN;		
		END;

		-- BufferMinutes
		IF @bufferMinutesValue < @minAllowed_bufferMinutesValue OR @bufferMinutesValue > @maxAllowed_bufferMinutesValue
		BEGIN
			RAISERROR('ERROR: @BufferMinutes (%d) is out of the allowed range [%d, %d].', 16, 1, 
				@bufferMinutesValue, @minAllowed_bufferMinutesValue, @maxAllowed_bufferMinutesValue);
			RETURN;		
		END;

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('Step 1 completed. Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		SET @start_time = @end_time;
		

		-- ======================================================================================
		-- STEP 2: 
		-- Get the max DateTime value from the PRIMARY partition in the StatusData2 table.
		WITH PartitionMinMax AS (
			SELECT 
				P.object_id,
				P.partition_number,
				MIN(S.DateTime) AS MinDateTime,
				MAX(S.DateTime) AS MaxDateTime
			FROM 
				sys.partitions P
			JOIN 
				[dbo].[StatusData2] S
			ON 
				P.object_id = OBJECT_ID('dbo.StatusData2') 
				AND P.partition_number = $PARTITION.DateTimePartitionFunction_MyGeotabApiAdapter(S.DateTime)
			WHERE 
				P.index_id IN (0, 1)  -- 0 = heap, 1 = clustered index
			GROUP BY 
				P.object_id, P.partition_number
		)
		SELECT @statusData2PrimaryPartitionMaxDateTime = ISNULL(
			(
				SELECT MAX(PartitionMinMax.MaxDateTime)
				FROM (
					SELECT DISTINCT
						PartitionMinMax.MaxDateTime
					FROM 
						sys.partitions P
					JOIN 
						sys.objects T 
						ON P.object_id = T.object_id
					JOIN 
						PartitionMinMax 
						ON P.object_id = PartitionMinMax.object_id 
						AND P.partition_number = PartitionMinMax.partition_number
					JOIN 
						sys.allocation_units AU 
						ON P.partition_id = AU.container_id
					JOIN 
						sys.data_spaces DS 
						ON AU.data_space_id = DS.data_space_id
					JOIN 
						sys.filegroups FG 
						ON DS.data_space_id = FG.data_space_id
					WHERE 
						T.name = 'StatusData2'
						AND FG.name = 'PRIMARY'
						AND AU.type = 1  -- Only include IN_ROW_DATA to avoid duplication.
				) AS PartitionMinMax
			), '1900-01-01'); -- Default value if the query result is NULL

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		DECLARE @statusData2PrimaryPartitionMaxDateTime_string VARCHAR(30) = FORMAT(@statusData2PrimaryPartitionMaxDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');
		RAISERROR ('STEP 2 Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		RAISERROR ('> @statusData2PrimaryPartitionMaxDateTime: %s', 0, 1, @statusData2PrimaryPartitionMaxDateTime_string) WITH NOWAIT;
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 3: 
		-- Get min and max DateTime values by Device for unprocessed StatusDataLocations2 
		-- records. Also get associated min and max DateTime values for LogRecords2 records where
		-- the DateTimes of the LogRecords are greater than or equal to the min DateTimes of the 
		-- unprocessed StatusDataLocations2 records. Do this ONLY for Devices that have both 
		-- StatusData and LogRecords. Exclude data from the PRIMARY partition.
		DROP TABLE IF EXISTS #TMP_DeviceDataMinMaxDateTimes;

		WITH StatusDataMinMax AS (
			SELECT 
				sdl.DeviceId,
				MIN(sdl.DateTime) AS DeviceStatusData2MinDateTime,
				MAX(sdl.DateTime) AS DeviceStatusData2MaxDateTime
			FROM dbo.StatusDataLocations2 sdl
			WHERE sdl.DateTime > @statusData2PrimaryPartitionMaxDateTime
			  AND sdl.LongLatProcessed = 0
			GROUP BY sdl.DeviceId
		),
		FilteredLogRecords AS (
			SELECT 
				lr.DeviceId,
				MIN(lr.DateTime) AS DeviceLogRecords2MinDateTime,
				MAX(lr.DateTime) AS DeviceLogRecords2MaxDateTime
			FROM dbo.LogRecords2 lr
			WHERE lr.DateTime >= (SELECT MIN(DeviceStatusData2MinDateTime) FROM StatusDataMinMax)
			GROUP BY lr.DeviceId
		)
		SELECT 
			sd.DeviceId,
			sd.DeviceStatusData2MinDateTime,
			sd.DeviceStatusData2MaxDateTime,
			slr.DeviceLogRecords2MinDateTime,
			slr.DeviceLogRecords2MaxDateTime
		INTO #TMP_DeviceDataMinMaxDateTimes
		FROM StatusDataMinMax sd
		INNER JOIN FilteredLogRecords slr
			ON sd.DeviceId = slr.DeviceId;

		-- Log.
		SET @record_count = @@ROWCOUNT;
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('STEP 3 (Create #TMP_DeviceDataMinMaxDateTimes) Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		RAISERROR ('> Record Count: %d', 0, 1, @record_count) WITH NOWAIT;	
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 3A: 
		-- Add indexes to temporary table.
		CREATE INDEX IX_TMP_DeviceDataMinMaxDateTimes_DeviceId 
		ON #TMP_DeviceDataMinMaxDateTimes (DeviceId);

		CREATE INDEX IX_TMP_DeviceDataMinMaxDateTimes_DeviceLogRecords2MinDateTime 
		ON #TMP_DeviceDataMinMaxDateTimes (DeviceLogRecords2MinDateTime);

		CREATE INDEX IX_TMP_DeviceDataMinMaxDateTimes_DeviceLogRecords2MaxDateTime 
		ON #TMP_DeviceDataMinMaxDateTimes (DeviceLogRecords2MaxDateTime);

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('STEP 3A (Index #TMP_DeviceDataMinMaxDateTimes) Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 4:
		-- Get the DateTime of the unprocessed StatusDataLocations2 record with the lowest 
		-- DateTime value that is greater than the max DateTime value from the PRIMARY partition 
		-- in the StatusDataLocations2 table.
		SELECT @unprocessedStatusDataMinDateTime = MIN(ddmm.DeviceStatusData2MinDateTime)
		FROM #TMP_DeviceDataMinMaxDateTimes ddmm;

		-- Determine the maximum allowed DateTime for the current batch based on adding @maxDaysPerBatchValue
		-- to the @unprocessedStatusDataMinDateTime. The purpose of this is to limit the number of 
		-- partitions that must be scanned as well as the potential number of LogRecords that
		-- may be returned in subsequent queries - which may be insignificant with smaller fleets,
		-- but can have a huge impact with larger fleets and their associated data volumes.
		SELECT @unprocessedStatusDataMaxAllowedDateTime = DATEADD(SECOND, -1, DATEADD(DAY, @maxDaysPerBatchValue
		, CAST(CAST((@unprocessedStatusDataMinDateTime) AS DATE) AS DATETIME)));

		-- Get the minimun DateTime value of any LogRecord.
		SELECT @logRecords2MinDateTime = MIN(ddmm.DeviceLogRecords2MinDateTime)
		FROM #TMP_DeviceDataMinMaxDateTimes ddmm;

		-- Get the maximum DateTime value of any LogRecord.
		SELECT @logRecords2MaxDateTime = MAX(ddmm.DeviceLogRecords2MaxDateTime)
		FROM #TMP_DeviceDataMinMaxDateTimes ddmm;

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		DECLARE @unprocessedStatusDataMinDateTime_string VARCHAR(30) = FORMAT(@unprocessedStatusDataMinDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');
		DECLARE @unprocessedStatusDataMaxAllowedDateTime_string VARCHAR(30) = FORMAT(@unprocessedStatusDataMaxAllowedDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');
		DECLARE @logRecords2MinDateTime_string VARCHAR(30) = FORMAT(@logRecords2MinDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');
		DECLARE @logRecords2MaxDateTime_string VARCHAR(30) = FORMAT(@logRecords2MaxDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');
		RAISERROR ('STEP 4 Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		RAISERROR ('> @unprocessedStatusDataMinDateTime: %s', 0, 1, @unprocessedStatusDataMinDateTime_string) WITH NOWAIT;
		RAISERROR ('> @unprocessedStatusDataMaxAllowedDateTime: %s', 0, 1, @unprocessedStatusDataMaxAllowedDateTime_string) WITH NOWAIT;
		RAISERROR ('> @logRecords2MinDateTime: %s', 0, 1, @logRecords2MinDateTime_string) WITH NOWAIT;
		RAISERROR ('> @logRecords2MaxDateTime: %s', 0, 1, @logRecords2MaxDateTime_string) WITH NOWAIT;
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 5:
		-- Get the ids of up to the first @maxBatchSizeValue of StatusDataLocations2 records within the 
		-- @unprocessedStatusDataMinDateTime to @unprocessedStatusDataMaxAllowedDateTime DateTime 
		-- range. Also make sure not to exceed the @logRecords2MaxDateTime.
		DROP TABLE IF EXISTS #TMP_BatchStatusDataLocationIds;

		SELECT TOP (@maxBatchSizeValue) 
			sdl.id
		INTO #TMP_BatchStatusDataLocationIds
		FROM dbo.StatusDataLocations2 sdl
		INNER JOIN #TMP_DeviceDataMinMaxDateTimes ddmmdt
			ON sdl.DeviceId = ddmmdt.DeviceId 
		WHERE sdl.LongLatProcessed = 0 
			AND sdl.DateTime BETWEEN (@unprocessedStatusDataMinDateTime) AND (@unprocessedStatusDataMaxAllowedDateTime)
			AND sdl.DateTime <= @logRecords2MaxDateTime;

		-- Log.
		SET @record_count = @@ROWCOUNT;
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('STEP 5 (Create #TMP_BatchStatusDataLocationIds) Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		RAISERROR ('> Record Count: %d', 0, 1, @record_count) WITH NOWAIT;	
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 5A: 
		-- Add index to temporary table.
		CREATE INDEX IX_TMP_BatchStatusDataLocationIds_id
		ON #TMP_BatchStatusDataLocationIds (id);

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('STEP 5A (Index #TMP_BatchStatusDataLocationIds) Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 6:
		-- Get the batch of unprocessed StatusData2 records.
		DROP TABLE IF EXISTS #TMP_UnprocessedStatusData2Batch;

		SELECT bsdl.id,
			sd.GeotabId,
			sdl.DateTime,
			sdl.DeviceId,
			@logRecords2MinDateTime AS LogRecords2MinDateTime,
			@logRecords2MaxDateTime AS LogRecords2MaxDateTime,
			dlrmm.DeviceLogRecords2MinDateTime,
			dlrmm.DeviceLogRecords2MaxDateTime
		INTO #TMP_UnprocessedStatusData2Batch
		FROM #TMP_BatchStatusDataLocationIds bsdl
		LEFT JOIN dbo.StatusDataLocations2 sdl
			ON bsdl.id = sdl.id
		LEFT JOIN #TMP_DeviceDataMinMaxDateTimes dlrmm
			ON sdl.DeviceId = dlrmm.DeviceId
		LEFT JOIN dbo.StatusData2 sd
			ON bsdl.id = sd.id
		WHERE sd.DateTime BETWEEN (@unprocessedStatusDataMinDateTime) AND (@unprocessedStatusDataMaxAllowedDateTime);

		-- Log.
		SET @record_count = @@ROWCOUNT;
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('STEP 6 (Create #TMP_UnprocessedStatusData2Batch) Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		RAISERROR ('> Record Count: %d', 0, 1, @record_count) WITH NOWAIT;	
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 6A: 
		-- Add indexes to temporary table.
		CREATE INDEX IX_TMP_UnprocessedStatusData2Batch_id
		ON #TMP_UnprocessedStatusData2Batch (id);

		CREATE INDEX IX_TMP_UnprocessedStatusData2Batch_DateTime
		ON #TMP_UnprocessedStatusData2Batch (DateTime);

		CREATE INDEX IX_TMP_UnprocessedStatusData2Batch_DeviceId_DateTime
		ON #TMP_UnprocessedStatusData2Batch (DeviceId, DateTime);

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('STEP 6A (Index #TMP_UnprocessedStatusData2Batch) Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		SET @start_time = @end_time;


		-- ======================================================================================
		-- STEP 7:
		-- Get the min and max DateTime values from the UnprocessedStatusData2Batch.
		SELECT @unprocessedStatusData2BatchMinDateTime = MIN(DateTime),
			@unprocessedStatusData2BatchMaxDateTime = MAX(DateTime)
		FROM #TMP_UnprocessedStatusData2Batch;

		SELECT @bufferedUnprocessedStatusData2BatchMinDateTime = DATEADD(MINUTE, -@bufferMinutesValue, @unprocessedStatusData2BatchMinDateTime);
		SELECT @bufferedUnprocessedStatusData2BatchMaxDateTime = DATEADD(MINUTE, @bufferMinutesValue, @unprocessedStatusData2BatchMaxDateTime);

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		DECLARE @unprocessedStatusData2BatchMinDateTime_string VARCHAR(30) = FORMAT(@unprocessedStatusData2BatchMinDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');
		DECLARE @unprocessedStatusData2BatchMaxDateTime_string VARCHAR(30) = FORMAT(@unprocessedStatusData2BatchMaxDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');
		DECLARE @bufferedUnprocessedStatusData2BatchMinDateTime_string VARCHAR(30) = FORMAT(@bufferedUnprocessedStatusData2BatchMinDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');
		DECLARE @bufferedUnprocessedStatusData2BatchMaxDateTime_string VARCHAR(30) = FORMAT(@bufferedUnprocessedStatusData2BatchMaxDateTime, 'yyyy-MM-dd HH:mm:ss.fffffff');	
		RAISERROR ('STEP 7 Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		RAISERROR ('> @unprocessedStatusData2BatchMinDateTime: %s', 0, 1, @unprocessedStatusData2BatchMinDateTime_string) WITH NOWAIT;
		RAISERROR ('> @unprocessedStatusData2BatchMaxDateTime: %s', 0, 1, @unprocessedStatusData2BatchMaxDateTime_string) WITH NOWAIT;
		RAISERROR ('> @bufferedUnprocessedStatusData2BatchMinDateTime: %s', 0, 1, @bufferedUnprocessedStatusData2BatchMinDateTime_string) WITH NOWAIT;
		RAISERROR ('> @bufferedUnprocessedStatusData2BatchMaxDateTime: %s', 0, 1, @bufferedUnprocessedStatusData2BatchMaxDateTime_string) WITH NOWAIT;		
		SET @start_time = @end_time;

		
		-- ======================================================================================
		-- Combine steps using CTEs: 
		WITH LogRecordsForUnprocessedStatusData2Batch AS (
			-- STEP 8: 
			-- Get all records from the LogRecords2 table that fall between 
			-- @bufferedUnprocessedStatusData2BatchMinDateTime and 
			-- @bufferedUnprocessedStatusData2BatchMaxDateTime to capture additional LogRecords needed 
			-- for interpolation of the StatusData records near the beginning and end of the batch.		
			SELECT *
			FROM LogRecords2 lr
			WHERE DateTime BETWEEN @bufferedUnprocessedStatusData2BatchMinDateTime 
				AND @bufferedUnprocessedStatusData2BatchMaxDateTime		
		),
		LogRecordsWithLeads AS (
			-- STEP 9: 
			-- Get all records from LogRecordsForUnprocessedStatusData2Batch and add the
			-- LeadDateTime, LeadLatitude and LeadLongitude to each record.
			SELECT *,
				LEAD(DateTime) OVER (PARTITION BY DeviceId ORDER BY DateTime) AS LeadDateTime,
				LEAD(Latitude) OVER (PARTITION BY DeviceId ORDER BY DateTime) AS LeadLatitude,
				LEAD(Longitude) OVER (PARTITION BY DeviceId ORDER BY DateTime) AS LeadLongitude
			FROM LogRecordsForUnprocessedStatusData2Batch lrb
		)
		-- STEP 10:
		-- Join the LogRecordsWithLeads to the records in the #TMP_UnprocessedStatusData2Batch where 
		-- the DeviceIds match and the DateTime of the StatusData2 record falls within the 
		-- DateTime-to-LeadDateTime range of the LogRecord2 record. Filter-out any duplicate values 
		-- which may be present in cases where a LogRecord-StatusData record pair have the exact 
		-- same DateTime values. In such cases, discard the "lead" LogRecord and keep the "lag" 
		-- LogRecord as its location coordinates will be reflective of the actual location of the 
		-- subject Device at the timestamp. Note that any records from the #TMP_UnprocessedStatusData2Batch
		-- that don't have matching LogRecordsWithLeads value (and would not otherwise be excluded
		-- by the duplicate check) will be included. This is to avoid the situation in which the
		-- combination of data + @maxDaysPerBatchValue + @maxBatchSizeValue + @bufferMinutesValue might result
		-- in no records being returned and therefore rendeing this procedure effectively unable 
		-- to process any further data.  
		SELECT sdb.id,
			sdb.GeotabId,
			sdb.DateTime AS StatusDataDateTime,
			sdb.DeviceId,
			CASE
				WHEN lrwl1.DeviceId IS NOT NULL THEN lrwl1.DateTime
				WHEN lrwl2.DeviceId IS NOT NULL THEN lrwl2.DateTime
				WHEN lrwl3.DeviceId IS NOT NULL THEN lrwl3.DateTime
				ELSE NULL 
			END AS LagDateTime,
			CASE 
				WHEN lrwl1.DeviceId IS NOT NULL THEN lrwl1.Latitude
				WHEN lrwl2.DeviceId IS NOT NULL THEN lrwl2.Latitude
				WHEN lrwl3.DeviceId IS NOT NULL THEN lrwl3.Latitude
				ELSE NULL
			END AS LagLatitude,
			CASE 
				WHEN lrwl1.DeviceId IS NOT NULL THEN lrwl1.Longitude
				WHEN lrwl2.DeviceId IS NOT NULL THEN lrwl2.Longitude
				WHEN lrwl3.DeviceId IS NOT NULL THEN lrwl3.Longitude
				ELSE NULL
			END AS LagLongitude,
			CASE
				WHEN lrwl1.DeviceId IS NOT NULL THEN lrwl1.Speed
				WHEN lrwl2.DeviceId IS NOT NULL THEN lrwl2.Speed
				WHEN lrwl3.DeviceId IS NOT NULL THEN lrwl3.Speed
				ELSE NULL
			END AS LagSpeed,
			CASE 
				WHEN lrwl1.DeviceId IS NOT NULL THEN lrwl1.LeadDateTime
				WHEN lrwl2.DeviceId IS NOT NULL THEN lrwl2.LeadDateTime
				WHEN lrwl3.DeviceId IS NOT NULL THEN lrwl3.LeadDateTime
				ELSE NULL
			END AS LeadDateTime,
			CASE 
				WHEN lrwl1.DeviceId IS NOT NULL THEN lrwl1.LeadLatitude
				WHEN lrwl2.DeviceId IS NOT NULL THEN lrwl2.LeadLatitude
				WHEN lrwl3.DeviceId IS NOT NULL THEN lrwl3.LeadLatitude
				ELSE NULL
			END AS LeadLatitude,
			CASE 
				WHEN lrwl1.DeviceId IS NOT NULL THEN lrwl1.LeadLongitude
				WHEN lrwl2.DeviceId IS NOT NULL THEN lrwl2.LeadLongitude
				WHEN lrwl3.DeviceId IS NOT NULL THEN lrwl3.LeadLongitude
				ELSE NULL
			END AS LeadLongitude,
			sdb.LogRecords2MinDateTime,
			sdb.LogRecords2MaxDateTime
		FROM #TMP_UnprocessedStatusData2Batch sdb
		LEFT JOIN LogRecordsWithLeads lrwl1 
			ON sdb.DeviceId = lrwl1.DeviceId
				AND sdb.DateTime = lrwl1.DateTime
				AND sdb.DateTime <= lrwl1.LeadDateTime
		LEFT JOIN LogRecordsWithLeads lrwl2 
			ON sdb.DeviceId = lrwl2.DeviceId
				AND sdb.DateTime > lrwl2.DateTime
				AND sdb.DateTime <= lrwl2.LeadDateTime
		LEFT JOIN LogRecordsWithLeads lrwl3 
			ON sdb.DeviceId = lrwl3.DeviceId
				AND sdb.DateTime >= lrwl3.DateTime
				AND lrwl3.LeadDateTime IS NULL
		ORDER BY sdb.DateTime;
		
		-- Log.
		SET @record_count = @@ROWCOUNT;
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('STEPS 8-10 (LogRecordsWithLeads and final output) Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		RAISERROR ('> Record Count: %d', 0, 1, @record_count) WITH NOWAIT;	
		SET @start_time = @end_time;		


		-- ======================================================================================
		-- STEP 11: 
		-- Drop temporary tables.
		DROP TABLE IF EXISTS #TMP_DeviceDataMinMaxDateTimes;
		DROP TABLE IF EXISTS #TMP_BatchStatusDataLocationIds;
		DROP TABLE IF EXISTS #TMP_UnprocessedStatusData2Batch;

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('STEP 11 (Drop temporary tables) Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		SET @start_time = @end_time;


		-- ======================================================================================
		-- Log end of stored procedure execution.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @storedProcedureStart_time, @end_time));
		RAISERROR ('Stored procedure ''%s'' executed successfully. Total Duration: %s milliseconds', 0, 1, @storedProcedureName, @duration_string) WITH NOWAIT;	
    END TRY
    BEGIN CATCH
		-- Ensure temporary table cleanup on error.
		DROP TABLE IF EXISTS #TMP_DeviceDataMinMaxDateTimes;
		DROP TABLE IF EXISTS #TMP_BatchStatusDataLocationIds;
		DROP TABLE IF EXISTS #TMP_UnprocessedStatusData2Batch;

        -- Rethrow the error
        THROW;
    END CATCH
END;
GO
/*** [END] SSMS-Generated Script ***/ 



/*** [START] v3.3.0.0 Updates ***/
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
/*** [END] v3.3.0.0 Updates ***/  



/*** [START] Database Version Update ***/
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO [dbo].[MiddlewareVersionInfo2] ([DatabaseVersion], [RecordCreationTimeUtc])
VALUES ('3.3.0.0', GETUTCDATE());
/*** [END] Database Version Update ***/
