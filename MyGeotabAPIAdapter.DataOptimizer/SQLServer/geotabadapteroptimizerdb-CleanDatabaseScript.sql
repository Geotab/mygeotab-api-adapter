--/* Clean Database */ 
-- truncate table [dbo].[BinaryDataT];
-- delete from [dbo].[BinaryTypesT];
-- delete from [dbo].[ControllersT];
-- truncate table [dbo].[DriverChangesT];
-- delete from [dbo].[DriverChangeTypesT];
-- truncate table [dbo].[FaultDataT];
-- truncate table [dbo].[LogRecordsT];
-- truncate table [dbo].[ODbErrors];
-- truncate table [dbo].[OProcessorTracking];
-- truncate table [dbo].[StatusDataT];
-- delete from [dbo].[UsersT];
-- delete from [dbo].[DevicesT];
-- truncate table [dbo].[DiagnosticIdsT];
-- delete from [dbo].[DiagnosticsT];
--DBCC CHECKIDENT ('dbo.BinaryDataT', RESEED, 0);
--DBCC CHECKIDENT ('dbo.BinaryTypesT', RESEED, 0);
--DBCC CHECKIDENT ('dbo.ControllersT', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DevicesT', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DiagnosticsT', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DiagnosticIdsT', RESEED, 0);
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
