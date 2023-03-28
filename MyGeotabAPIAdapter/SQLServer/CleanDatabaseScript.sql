/* Clean Database */ 
-- truncate table [dbo].[BinaryData];
-- truncate table [dbo].[Conditions];
-- truncate table [dbo].[DebugData];
-- truncate table [dbo].[Devices];
-- truncate table [dbo].[DeviceStatusInfo];
-- truncate table [dbo].[Diagnostics];
-- truncate table [dbo].[DriverChanges];
-- truncate table [dbo].[DutyStatusAvailabilities];
-- truncate table [dbo].[DVIRDefectRemarks];
-- truncate table [dbo].[DVIRDefects];
-- truncate table [dbo].[DVIRDefectUpdates];
-- truncate table [dbo].[DVIRLogs];
-- truncate table [dbo].[ExceptionEvents];
-- truncate table [dbo].[FailedDVIRDefectUpdates];
-- truncate table [dbo].[FailedOVDSServerCommands];
-- truncate table [dbo].[FaultData];
-- truncate table [dbo].[LogRecords];
-- truncate table [dbo].[MyGeotabVersionInfo];
-- truncate table [dbo].[OServiceTracking];
-- truncate table [dbo].[OVDSServerCommands];
-- truncate table [dbo].[Rules];
-- truncate table [dbo].[StatusData];
-- truncate table [dbo].[Trips];
-- truncate table [dbo].[Users];
-- truncate table [dbo].[Zones];
-- truncate table [dbo].[ZoneTypes];
--DBCC CHECKIDENT ('dbo.BinaryData', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Conditions', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DebugData', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Devices', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DeviceStatusInfo', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Diagnostics', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DriverChanges', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DutyStatusAvailabilities', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DVIRDefectRemarks', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DVIRDefects', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DVIRDefectUpdates', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DVIRLogs', RESEED, 0);
--DBCC CHECKIDENT ('dbo.ExceptionEvents', RESEED, 0);
--DBCC CHECKIDENT ('dbo.FailedDVIRDefectUpdates', RESEED, 0);
--DBCC CHECKIDENT ('dbo.FailedOVDSServerCommands', RESEED, 0);
--DBCC CHECKIDENT ('dbo.FaultData', RESEED, 0);
--DBCC CHECKIDENT ('dbo.LogRecords', RESEED, 0);
--DBCC CHECKIDENT ('dbo.OServiceTracking', RESEED, 0);
--DBCC CHECKIDENT ('dbo.OVDSServerCommands', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Rules', RESEED, 0);
--DBCC CHECKIDENT ('dbo.StatusData', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Trips', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Users', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Zones', RESEED, 0);
--DBCC CHECKIDENT ('dbo.ZoneTypes', RESEED, 0);

set nocount on;
/* Check counts */
SELECT 
    (SCHEMA_NAME(A.schema_id) + '.' + A.Name) as "TableName",  
    B.row_count as "RecordCount"
FROM  
    sys.dm_db_partition_stats B 
LEFT JOIN 
    sys.objects A 
    ON A.object_id = B.object_id 
WHERE 
    SCHEMA_NAME(A.schema_id) <> 'sys' 
    AND (B.index_id = '0' OR B.index_id = '1') 
ORDER BY 
    A.name 