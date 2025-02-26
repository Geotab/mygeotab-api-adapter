USE [geotabadapterdb]
GO
/****** Object:  Table [dbo].[BinaryData]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BinaryData](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[BinaryType] [nvarchar](50) NULL,
	[ControllerId] [nvarchar](50) NOT NULL,
	[Data] [nvarchar](1024) NOT NULL,
	[DateTime] [datetime2](7) NULL,
	[DeviceId] [nvarchar](50) NULL,
	[Version] [nvarchar](50) NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_BinaryData] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ChargeEvents]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ChargeEvents](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[ChargeIsEstimated] [bit] NOT NULL,
	[ChargeType] [nvarchar](50) NOT NULL,
	[StartTime] [datetime2](7) NOT NULL,
	[DeviceId] [nvarchar](50) NOT NULL,
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
	[TripStop] [datetime2](7) NULL,
	[Version] [bigint] NOT NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ChargeEvent] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Conditions]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Conditions](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[ParentId] [nvarchar](50) NULL,
	[RuleId] [nvarchar](50) NULL,
	[ConditionType] [nvarchar](50) NOT NULL,
	[DeviceId] [nvarchar](50) NULL,
	[DiagnosticId] [nvarchar](100) NULL,
	[DriverId] [nvarchar](50) NULL,
	[Value] [float] NULL,
	[WorkTimeId] [nvarchar](50) NULL,
	[ZoneId] [nvarchar](50) NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Conditions] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DebugData]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DebugData](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[Data] [nvarchar](max) NOT NULL,
	[DateTime] [datetime2](7) NULL,
	[DebugReasonId] [bigint] NULL,
	[DebugReasonName] [nvarchar](255) NULL,
	[DeviceId] [nvarchar](50) NULL,
	[DriverId] [nvarchar](50) NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DebugData] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Devices]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Devices](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
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
 CONSTRAINT [PK_Devices] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceStatusInfo]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceStatusInfo](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[Bearing] [float] NOT NULL,
	[CurrentStateDuration] [nvarchar](50) NOT NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeviceId] [nvarchar](50) NOT NULL,
	[DriverId] [nvarchar](50) NOT NULL,
	[IsDeviceCommunicating] [bit] NOT NULL,
	[IsDriving] [bit] NOT NULL,
	[IsHistoricLastDriver] [bit] NOT NULL,
	[Latitude] [float] NOT NULL,
	[Longitude] [float] NOT NULL,
	[Speed] [real] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DeviceStatusInfo] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Diagnostics]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Diagnostics](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](100) NOT NULL,
	[GeotabGUIDString] [nvarchar](100) NOT NULL,
	[HasShimId] [bit] NOT NULL,
	[FormerShimGeotabGUID] [nvarchar](100) NULL,
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
 CONSTRAINT [PK_Diagnostics] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DriverChanges]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DriverChanges](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[DateTime] [datetime2](7) NULL,
	[DeviceId] [nvarchar](50) NOT NULL,
	[DriverId] [nvarchar](50) NOT NULL,
	[Type] [nvarchar](50) NOT NULL,
	[Version] [bigint] NOT NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DriverChanges] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DutyStatusAvailabilities]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DutyStatusAvailabilities](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[DriverId] [nvarchar](50) NOT NULL,
	[CycleAvailabilities] [nvarchar](max) NULL,
	[CycleTicks] [bigint] NULL,
	[CycleRestTicks] [bigint] NULL,
	[DrivingTicks] [bigint] NULL,
	[DutyTicks] [bigint] NULL,
	[DutySinceCycleRestTicks] [bigint] NULL,
	[Is16HourExemptionAvailable] [bit] NULL,
	[IsAdverseDrivingExemptionAvailable] [bit] NULL,
	[IsOffDutyDeferralExemptionAvailable] [bit] NULL,
	[Recap] [nvarchar](max) NULL,
	[RestTicks] [bigint] NULL,
	[WorkdayTicks] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DutyStatusAvailabilities] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DutyStatusLogs]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DutyStatusLogs](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[Annotations] [nvarchar](max) NULL,
	[CoDrivers] [nvarchar](max) NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeferralMinutes] [int] NULL,
	[DeferralStatus] [nvarchar](50) NULL,
	[DeviceId] [nvarchar](50) NULL,
	[DistanceSinceValidCoordinates] [real] NULL,
	[DriverId] [nvarchar](50) NULL,
	[EditDateTime] [datetime2](7) NULL,
	[EditRequestedByUserId] [nvarchar](50) NULL,
	[EngineHours] [float] NULL,
	[EventCheckSum] [bigint] NULL,
	[EventCode] [int] NULL,
	[EventRecordStatus] [int] NULL,
	[EventType] [int] NULL,
	[IsHidden] [bit] NULL,
	[IsIgnored] [bit] NULL,
	[IsTransitioning] [bit] NULL,
	[Location] [nvarchar](max) NULL,
	[LocationX] [float] NULL,
	[LocationY] [float] NULL,
	[Malfunction] [nvarchar](50) NULL,
	[Odometer] [float] NULL,
	[Origin] [nvarchar](50) NULL,
	[ParentId] [nvarchar](50) NULL,
	[Sequence] [bigint] NULL,
	[State] [nvarchar](50) NULL,
	[Status] [nvarchar](50) NULL,
	[UserHosRuleSet] [nvarchar](max) NULL,
	[VerifyDateTime] [datetime2](7) NULL,
	[Version] [bigint] NOT NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DutyStatusLogs] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DVIRDefectRemarks]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DVIRDefectRemarks](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[DVIRDefectId] [nvarchar](50) NOT NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[Remark] [nvarchar](max) NULL,
	[RemarkUserId] [nvarchar](50) NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DVIRDefectRemarks] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DVIRDefects]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DVIRDefects](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[DVIRLogId] [nvarchar](50) NOT NULL,
	[DefectListAssetType] [nvarchar](50) NULL,
	[DefectListId] [nvarchar](50) NULL,
	[DefectListName] [nvarchar](255) NULL,
	[PartId] [nvarchar](50) NULL,
	[PartName] [nvarchar](255) NULL,
	[DefectId] [nvarchar](50) NULL,
	[DefectName] [nvarchar](255) NULL,
	[DefectSeverity] [nvarchar](50) NULL,
	[RepairDateTime] [datetime2](7) NULL,
	[RepairStatus] [nvarchar](50) NULL,
	[RepairUserId] [nvarchar](50) NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DVIRDefects] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DVIRDefectUpdates]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DVIRDefectUpdates](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[DVIRLogId] [nvarchar](50) NOT NULL,
	[DVIRDefectId] [nvarchar](50) NOT NULL,
	[RepairDateTime] [datetime2](7) NULL,
	[RepairStatus] [nvarchar](50) NULL,
	[RepairUserId] [nvarchar](50) NULL,
	[Remark] [nvarchar](max) NULL,
	[RemarkDateTime] [datetime2](7) NULL,
	[RemarkUserId] [nvarchar](50) NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DVIRDefectUpdates] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DVIRLogs]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DVIRLogs](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[CertifiedByUserId] [nvarchar](50) NULL,
	[CertifiedDate] [datetime2](7) NULL,
	[CertifyRemark] [nvarchar](max) NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeviceId] [nvarchar](50) NULL,
	[DriverId] [nvarchar](50) NULL,
	[DriverRemark] [nvarchar](max) NULL,
	[IsSafeToOperate] [bit] NULL,
	[LocationLatitude] [float] NULL,
	[LocationLongitude] [float] NULL,
	[LogType] [nvarchar](50) NULL,
	[RepairDate] [datetime2](7) NULL,
	[RepairedByUserId] [nvarchar](50) NULL,
	[TrailerId] [nvarchar](50) NULL,
	[TrailerName] [nvarchar](255) NULL,
	[Version] [bigint] NOT NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DVIRLogs] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ExceptionEvents]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ExceptionEvents](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[ActiveFrom] [datetime2](7) NULL,
	[ActiveTo] [datetime2](7) NULL,
	[DeviceId] [nvarchar](50) NULL,
	[Distance] [real] NULL,
	[DriverId] [nvarchar](50) NULL,
	[DurationTicks] [bigint] NULL,
	[LastModifiedDateTime] [datetime2](7) NOT NULL,
	[RuleId] [nvarchar](50) NULL,
	[State] [int] NOT NULL,
	[Version] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ExceptionEvents] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FailedDVIRDefectUpdates]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FailedDVIRDefectUpdates](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[DVIRDefectUpdateId] [bigint] NOT NULL,
	[DVIRLogId] [nvarchar](50) NOT NULL,
	[DVIRDefectId] [nvarchar](50) NOT NULL,
	[RepairDateTime] [datetime2](7) NULL,
	[RepairStatus] [nvarchar](50) NULL,
	[RepairUserId] [nvarchar](50) NULL,
	[Remark] [nvarchar](max) NULL,
	[RemarkDateTime] [datetime2](7) NULL,
	[RemarkUserId] [nvarchar](50) NULL,
	[FailureMessage] [nvarchar](max) NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_FailedDVIRDefectUpdates] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FailedOVDSServerCommands]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FailedOVDSServerCommands](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[OVDSServerCommandId] [bigint] NOT NULL,
	[Command] [nvarchar](max) NOT NULL,
	[FailureMessage] [nvarchar](max) NOT NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_FailedOVDSServerCommands] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FaultData]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FaultData](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[AmberWarningLamp] [bit] NULL,
	[ClassCode] [nvarchar](50) NULL,
	[ControllerId] [nvarchar](100) NOT NULL,
	[ControllerName] [nvarchar](255) NULL,
	[Count] [int] NOT NULL,
	[DateTime] [datetime2](7) NULL,
	[DeviceId] [nvarchar](50) NOT NULL,
	[DiagnosticId] [nvarchar](100) NOT NULL,
	[DismissDateTime] [datetime2](7) NULL,
	[DismissUserId] [nvarchar](50) NULL,
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
 CONSTRAINT [PK_FaultData] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Groups]    Script Date: 2025-02-06 4:38:17 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Groups](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[Children] [nvarchar](max) NULL,
	[Color] [nvarchar](50) NULL,
	[Comments] [nvarchar](1024) NULL,
	[Name] [nvarchar](255) NULL,
	[Reference] [nvarchar](255) NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Groups] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[LogRecords]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LogRecords](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[DateTime] [datetime2](7) NOT NULL,
	[DeviceId] [nvarchar](50) NOT NULL,
	[Latitude] [float] NOT NULL,
	[Longitude] [float] NOT NULL,
	[Speed] [real] NOT NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_LogRecords] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MyGeotabVersionInfo]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MyGeotabVersionInfo](
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
/****** Object:  Table [dbo].[OServiceTracking]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OServiceTracking](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[ServiceId] [nvarchar](50) NOT NULL,
	[AdapterVersion] [nvarchar](50) NULL,
	[AdapterMachineName] [nvarchar](100) NULL,
	[EntitiesLastProcessedUtc] [datetime2](7) NULL,
	[LastProcessedFeedVersion] [bigint] NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_OServiceTracking] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[OVDSServerCommands]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OVDSServerCommands](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[Command] [nvarchar](max) NOT NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_OVDSServerCommands] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Rules]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Rules](
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
 CONSTRAINT [PK_Rules] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[StatusData]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StatusData](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[Data] [float] NULL,
	[DateTime] [datetime2](7) NULL,
	[DeviceId] [nvarchar](50) NOT NULL,
	[DiagnosticId] [nvarchar](100) NOT NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_StatusData] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Trips]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Trips](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](50) NOT NULL,
	[AfterHoursDistance] [real] NULL,
	[AfterHoursDrivingDurationTicks] [bigint] NULL,
	[AfterHoursEnd] [bit] NULL,
	[AfterHoursStart] [bit] NULL,
	[AfterHoursStopDurationTicks] [bigint] NULL,
	[AverageSpeed] [real] NULL,
	[DeviceId] [nvarchar](50) NOT NULL,
	[Distance] [real] NOT NULL,
	[DriverId] [nvarchar](50) NOT NULL,
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
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Trips] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
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
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Zones]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Zones](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
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
 CONSTRAINT [PK_Zones] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ZoneTypes]    Script Date: 2024-04-04 10:09:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ZoneTypes](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[GeotabId] [nvarchar](100) NOT NULL,
	[Comment] [nvarchar](255) NULL,
	[Name] [nvarchar](255) NOT NULL,
	[EntityStatus] [int] NOT NULL,
	[RecordLastChangedUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ZoneTypes] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Index [IX_BinaryData_DateTime]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_BinaryData_DateTime] ON [dbo].[BinaryData]
(
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_ChargeEvent_TripStop]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_ChargeEvent_TripStop] ON [dbo].[ChargeEvents]
(
	[TripStop] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Conditions_RecordLastChangedUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_Conditions_RecordLastChangedUtc] ON [dbo].[Conditions]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_DebugData_DateTime]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_DebugData_DateTime] ON [dbo].[DebugData]
(
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Devices_RecordLastChangedUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_Devices_RecordLastChangedUtc] ON [dbo].[Devices]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_DeviceStatusInfo_RecordLastChangedUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_DeviceStatusInfo_RecordLastChangedUtc] ON [dbo].[DeviceStatusInfo]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Diagnostics_RecordLastChangedUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_Diagnostics_RecordLastChangedUtc] ON [dbo].[Diagnostics]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_DriverChanges_RecordCreationTimeUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_DriverChanges_RecordCreationTimeUtc] ON [dbo].[DriverChanges]
(
	[RecordCreationTimeUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_DutyStatusAvailabilities_RecordLastChangedUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_DutyStatusAvailabilities_RecordLastChangedUtc] ON [dbo].[DutyStatusAvailabilities]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_DutyStatusLogs_DateTime]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_DutyStatusLogs_DateTime] ON [dbo].[DutyStatusLogs]
(
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_DVIRDefectRemarks_RecordLastChangedUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_DVIRDefectRemarks_RecordLastChangedUtc] ON [dbo].[DVIRDefectRemarks]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_DVIRDefects_RecordLastChangedUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_DVIRDefects_RecordLastChangedUtc] ON [dbo].[DVIRDefects]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_DVIRDefectUpdates_RecordCreationTimeUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_DVIRDefectUpdates_RecordCreationTimeUtc] ON [dbo].[DVIRDefectUpdates]
(
	[RecordCreationTimeUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_DVIRLogs_DateTime]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_DVIRLogs_DateTime] ON [dbo].[DVIRLogs]
(
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_ExceptionEvents_RecordLastChangedUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_ExceptionEvents_RecordLastChangedUtc] ON [dbo].[ExceptionEvents]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_FaultData_DateTime]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_FaultData_DateTime] ON [dbo].[FaultData]
(
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Devices_RecordLastChangedUtc]    Script Date: 2025-02-06 4:38:25 PM ******/
CREATE NONCLUSTERED INDEX [IX_Groups_RecordLastChangedUtc] ON [dbo].[Groups]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_LogRecords_DateTime]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_LogRecords_DateTime] ON [dbo].[LogRecords]
(
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_MyGeotabVersionInfo_RecordCreationTimeUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_MyGeotabVersionInfo_RecordCreationTimeUtc] ON [dbo].[MyGeotabVersionInfo]
(
	[RecordCreationTimeUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_OServiceTracking_RecordLastChangedUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_OServiceTracking_RecordLastChangedUtc] ON [dbo].[OServiceTracking]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Rules_RecordLastChangedUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_Rules_RecordLastChangedUtc] ON [dbo].[Rules]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_StatusData_DateTime]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_StatusData_DateTime] ON [dbo].[StatusData]
(
	[DateTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Trips_RecordCreationTimeUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_Trips_RecordCreationTimeUtc] ON [dbo].[Trips]
(
	[RecordCreationTimeUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Users_RecordLastChangedUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_Users_RecordLastChangedUtc] ON [dbo].[Users]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_Zones_RecordLastChangedUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_Zones_RecordLastChangedUtc] ON [dbo].[Zones]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_ZoneTypes_RecordLastChangedUtc]    Script Date: 2024-04-04 10:09:23 AM ******/
CREATE NONCLUSTERED INDEX [IX_ZoneTypes_RecordLastChangedUtc] ON [dbo].[ZoneTypes]
(
	[RecordLastChangedUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE [dbo].[DeviceStatusInfo] ADD  CONSTRAINT [DF__DeviceStatusInfo__Bearing]  DEFAULT ((0)) FOR [Bearing]
GO
ALTER TABLE [dbo].[DeviceStatusInfo] ADD  CONSTRAINT [DF__DeviceStatusInfo__Latitude]  DEFAULT ((0)) FOR [Latitude]
GO
ALTER TABLE [dbo].[DeviceStatusInfo] ADD  CONSTRAINT [DF__DeviceStatusInfo__Longitude]  DEFAULT ((0)) FOR [Longitude]
GO
ALTER TABLE [dbo].[DeviceStatusInfo] ADD  CONSTRAINT [DF__DeviceStatusInfo__Speed]  DEFAULT ((0)) FOR [Speed]
GO
ALTER TABLE [dbo].[LogRecords] ADD  CONSTRAINT [DF__LogRecord__Latit__2E1BDC42]  DEFAULT ((0)) FOR [Latitude]
GO
ALTER TABLE [dbo].[LogRecords] ADD  CONSTRAINT [DF__LogRecord__Longi__2F10007B]  DEFAULT ((0)) FOR [Longitude]
GO
ALTER TABLE [dbo].[LogRecords] ADD  CONSTRAINT [DF__LogRecord__Speed__300424B4]  DEFAULT ((0)) FOR [Speed]
GO
