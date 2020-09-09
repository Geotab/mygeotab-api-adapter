/* Clean Database */ 
-- delete from [dbo].[Conditions];
-- delete from [dbo].[ConfigFeedVersions];
-- delete from [dbo].[DVIRDefectRemarks];
-- delete from [dbo].[DVIRDefects];
-- delete from [dbo].[DVIRLogs];
-- delete from [dbo].[Devices];
-- delete from [dbo].[Diagnostics];
-- delete from [dbo].[ExceptionEvents];
-- delete from [dbo].[FaultData];
-- delete from [dbo].[LogRecords];
-- delete from [dbo].[MyGeotabVersionInfo];
-- delete from [dbo].[Rules];
-- delete from [dbo].[StatusData];
-- delete from [dbo].[Trips];
-- delete from [dbo].[Users];
-- delete from [dbo].[Zones];

/* Check counts */
select 'Conditions' as "TableName", count(0) as "RecordCount" from [dbo].[Conditions]
union all
select 'ConfigFeedVersions', count(0) from [dbo].[ConfigFeedVersions]
union all
select 'DVIRDefectRemarks', count(0) from [dbo].[DVIRDefectRemarks]
union all
select 'DVIRDefects', count(0) from [dbo].[DVIRDefects]
union all
select 'DVIRLogs', count(0) from [dbo].[DVIRLogs]
union all
select 'Devices', count(0) from [dbo].[Devices]
union all
select 'Diagnostics', count(0) from [dbo].[Diagnostics]
union all
select 'ExceptionEvents', count(0) from [dbo].[ExceptionEvents]
union all
select 'FaultData', count(0) from [dbo].[FaultData]
union all
select 'LogRecords', count(0) from [dbo].[LogRecords]
union all
select 'MyGeotabVersionInfo', count(0) from [dbo].[MyGeotabVersionInfo]
union all
select 'Rules', count(0) from [dbo].[Rules]
union all
select 'StatusData', count(0) from [dbo].[StatusData]
union all
select 'Trips', count(0) from [dbo].[Trips]
union all
select 'Users', count(0) from [dbo].[Users]
union all
select 'Zones', count(0) from [dbo].[Zones]
order by "TableName";