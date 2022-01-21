/* Clean Database */ 
-- delete from [dbo].[BinaryDataT];
-- delete from [dbo].[BinaryTypesT];
-- delete from [dbo].[ControllersT];
-- delete from [dbo].[DriverChangesT];
-- delete from [dbo].[DriverChangeTypesT];
-- delete from [dbo].[FaultDataT];
-- delete from [dbo].[LogRecordsT];
-- delete from [dbo].[ODbErrors];
-- delete from [dbo].[OProcessorTracking];
-- delete from [dbo].[StatusDataT];
-- delete from [dbo].[UsersT];
-- delete from [dbo].[DevicesT];
-- delete from [dbo].[DiagnosticsT];
--DBCC CHECKIDENT ('dbo.BinaryDataT', RESEED, 0);
--DBCC CHECKIDENT ('dbo.BinaryTypesT', RESEED, 0);
--DBCC CHECKIDENT ('dbo.ControllersT', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DevicesT', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DiagnosticsT', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DriverChangesT', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DriverChangeTypesT', RESEED, 0);
--DBCC CHECKIDENT ('dbo.FaultDataT', RESEED, 0);
--DBCC CHECKIDENT ('dbo.LogRecordsT', RESEED, 0);
--DBCC CHECKIDENT ('dbo.ODbErrors', RESEED, 0);
--DBCC CHECKIDENT ('dbo.OProcessorTracking', RESEED, 0);
--DBCC CHECKIDENT ('dbo.StatusDataT', RESEED, 0);
--DBCC CHECKIDENT ('dbo.UsersT', RESEED, 0);

set nocount on;
/* Check counts */
select 'BinaryDataT' as "TableName", SUM(st.row_count) as "RecordCount" FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'BinaryDataT'
union all
select 'BinaryTypesT', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'BinaryTypesT'
union all
select 'ControllersT', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'ControllersT'
union all
select 'DevicesT', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'DevicesT'
union all
select 'DiagnosticsT', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'DiagnosticsT'
union all
select 'DriverChangesT', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'DriverChangesT'
union all
select 'DriverChangeTypesT', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'DriverChangeTypesT'
union all
select 'FaultDataT', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'FaultDataT'
union all
select 'LogRecordsT', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'LogRecordsT'
union all
select 'ODbErrors', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'ODbErrors'
union all
select 'OProcessorTracking', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'OProcessorTracking'
union all
select 'StatusDataT', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'StatusDataT'
union all
select 'UsersT', SUM(st.row_count) FROM sys.dm_db_partition_stats st WHERE object_name(object_id) = 'UsersT'
order by "TableName";
