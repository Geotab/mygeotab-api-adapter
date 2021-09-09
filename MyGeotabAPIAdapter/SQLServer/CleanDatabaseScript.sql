/* Clean Database */ 
-- truncate table [dbo].[Conditions];
-- truncate table [dbo].[ConfigFeedVersions];
-- truncate table [dbo].[Devices];
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
-- truncate table [dbo].[OVDSServerCommands];
-- truncate table [dbo].[Rules];
-- truncate table [dbo].[StatusData];
-- truncate table [dbo].[Trips];
-- truncate table [dbo].[Users];
-- truncate table [dbo].[Zones];
-- truncate table [dbo].[ZoneTypes];
--DBCC CHECKIDENT ('dbo.Conditions', RESEED, 0);
--DBCC CHECKIDENT ('dbo.ConfigFeedVersions', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Devices', RESEED, 0);
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
--DBCC CHECKIDENT ('dbo.OVDSServerCommands', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Rules', RESEED, 0);
--DBCC CHECKIDENT ('dbo.StatusData', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Trips', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Users', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Zones', RESEED, 0);
--DBCC CHECKIDENT ('dbo.ZoneTypes', RESEED, 0);

set nocount on;
/* Check counts */
select 'Conditions' as "TableName", SUM(st.row_count) as "RecordCount" FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'Conditions'
union all
select 'ConfigFeedVersions', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'ConfigFeedVersions'
union all
select 'Devices', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'Devices'
union all
select 'DriverChanges', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'DriverChanges'
union all
select 'Diagnostics', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'Diagnostics'
union all
select 'DutyStatusAvailabilities', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'DutyStatusAvailabilities'
union all
select 'DVIRDefectRemarks', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'DVIRDefectRemarks'
union all
select 'DVIRDefects', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'DVIRDefects'
union all
select 'DVIRDefectUpdates', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'DVIRDefectUpdates'
union all
select 'DVIRLogs', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'DVIRLogs'
union all
select 'ExceptionEvents', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'ExceptionEvents'
union all
select 'FailedDVIRDefectUpdates', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'FailedDVIRDefectUpdates'
union all
select 'FailedOVDSServerCommands', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'FailedOVDSServerCommands'
union all
select 'FaultData', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'FaultData'
union all
select 'LogRecords', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'LogRecords'
union all
select 'MyGeotabVersionInfo', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'MyGeotabVersionInfo'
union all
select 'OVDSServerCommands', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'OVDSServerCommands'
union all
select 'Rules', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'Rules'
union all
select 'StatusData', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'StatusData'
union all
select 'Trips', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'Trips'
union all
select 'Users', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'Users'
union all
select 'Zones', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'Zones'
union all
select 'ZoneTypes', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'ZoneTypes'
order by "TableName";
