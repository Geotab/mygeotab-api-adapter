/* Clean Database */ 
-- delete from [dbo].[Conditions];
-- delete from [dbo].[ConfigFeedVersions];
-- delete from [dbo].[DVIRDefectRemarks];
-- delete from [dbo].[DVIRDefects];
-- delete from [dbo].[DVIRLogs];
-- delete from [dbo].[Devices];
-- delete from [dbo].[Diagnostics];
-- delete from [dbo].[DutyStatusAvailabilities];
-- delete from [dbo].[ExceptionEvents];
-- delete from [dbo].[FaultData];
-- delete from [dbo].[LogRecords];
-- delete from [dbo].[MyGeotabVersionInfo];
-- delete from [dbo].[Rules];
-- delete from [dbo].[StatusData];
-- delete from [dbo].[Trips];
-- delete from [dbo].[Users];
-- delete from [dbo].[Zones];
-- delete from [dbo].[ZoneTypes];
--DBCC CHECKIDENT ('dbo.Conditions', RESEED, 0);
--DBCC CHECKIDENT ('dbo.ConfigFeedVersions', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DVIRDefectRemarks', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DVIRDefects', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DVIRLogs', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Devices', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Diagnostics', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DutyStatusAvailabilities', RESEED, 0);
--DBCC CHECKIDENT ('dbo.ExceptionEvents', RESEED, 0);
--DBCC CHECKIDENT ('dbo.FaultData', RESEED, 0);
--DBCC CHECKIDENT ('dbo.LogRecords', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Rules', RESEED, 0);
--DBCC CHECKIDENT ('dbo.StatusData', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Trips', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Users', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Zones', RESEED, 0);
--DBCC CHECKIDENT ('dbo.ZoneTypes', RESEED, 0);


/* Check counts */
select 'Conditions' as "TableName", SUM(st.row_count) as "RecordCount" FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'Conditions'
union all
select 'ConfigFeedVersions', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'ConfigFeedVersions'
union all
select 'DVIRDefectRemarks', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'DVIRDefectRemarks'
union all
select 'DVIRDefects', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'DVIRDefects'
union all
select 'DVIRLogs', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'DVIRLogs'
union all
select 'Devices', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'Devices'
union all
select 'Diagnostics', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'Diagnostics'
union all
select 'DutyStatusAvailabilities', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'DutyStatusAvailabilities'
union all
select 'ExceptionEvents', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'ExceptionEvents'
union all
select 'FaultData', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'FaultData'
union all
select 'LogRecords', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'LogRecords'
union all
select 'MyGeotabVersionInfo', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'MyGeotabVersionInfo'
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
