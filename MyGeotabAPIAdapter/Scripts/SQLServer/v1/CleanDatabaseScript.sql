-- ================================================================================
-- DATABASE TYPE: SQL Server
-- 
-- NOTES: 
--   1: This script applies to OLDER versions of the MyGeotab API Adapter database 
--      (prior to application version 3.0.0). It does not apply to later versions 
--      of the application except in any cases where v1 and v2 versions of a new
--      feed may be implemented. 
--   2: Be sure to alter the "USE [geotabadapterdb]" statement below if you have
--      changed the database name to something else.
--   3: This script is updated as new tables are added to the database (i.e. there
--      is only one version of this script).
--
-- DESCRIPTION: 
--   This script is intended for manual use in the following situations:
-- 
--     1: Obtaining record counts for each of the database tables:
--          - Simply execute the script as-is to obtain record counts.
--     2: Clearing all database tables and reseeding identity values:
--          - Uncomment the commented-out lines below and then execute the script
--            to clear all tables of data. 
-- ================================================================================

USE [geotabadapterdb]
GO

--/*** [START] Clean Database ***/ 
--BEGIN TRANSACTION;

--BEGIN TRY
---- Step 1: Capture foreign key constraints
--DECLARE @sqlDrop NVARCHAR(MAX) = N'';
--DECLARE @sqlCreate NVARCHAR(MAX) = N'';

--SELECT 
--    @sqlDrop += 'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + 
--                ' DROP CONSTRAINT ' + QUOTENAME(fk.name) + '; '
--    ,@sqlCreate += 'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + 
--                    ' ADD CONSTRAINT ' + QUOTENAME(fk.name) + ' FOREIGN KEY (' + 
--                    STRING_AGG(QUOTENAME(fc.name), ', ') + ') REFERENCES ' + 
--                    QUOTENAME(rs.name) + '.' + QUOTENAME(rt.name) + ' (' + 
--                    STRING_AGG(QUOTENAME(rc.name), ', ') + '); '
--FROM sys.foreign_keys AS fk
--INNER JOIN sys.foreign_key_columns AS fkc
--    ON fk.object_id = fkc.constraint_object_id
--INNER JOIN sys.tables AS t
--    ON fkc.parent_object_id = t.object_id
--INNER JOIN sys.columns AS fc
--    ON fkc.parent_object_id = fc.object_id AND fkc.parent_column_id = fc.column_id
--INNER JOIN sys.tables AS rt
--    ON fkc.referenced_object_id = rt.object_id
--INNER JOIN sys.columns AS rc
--    ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
--INNER JOIN sys.schemas AS s
--    ON t.schema_id = s.schema_id
--INNER JOIN sys.schemas AS rs
--    ON rt.schema_id = rs.schema_id
--GROUP BY s.name, t.name, fk.name, rs.name, rt.name;

---- Step 2: Drop foreign key constraints:
--EXEC sp_executesql @sqlDrop;

---- Step 3: Truncate tables and reseed indexes: 
--truncate table [dbo].[BinaryData];
--truncate table [dbo].[ChargeEvents];
--truncate table [dbo].[Conditions];
--truncate table [dbo].[DebugData];
--truncate table [dbo].[Devices];
--truncate table [dbo].[DeviceStatusInfo];
--truncate table [dbo].[Diagnostics];
--truncate table [dbo].[DriverChanges];
--truncate table [dbo].[DutyStatusAvailabilities];
--truncate table [dbo].[DutyStatusLogs];
--truncate table [dbo].[DVIRDefectRemarks];
--truncate table [dbo].[DVIRDefects];
--truncate table [dbo].[DVIRDefectUpdates];
--truncate table [dbo].[DVIRLogs];
--truncate table [dbo].[ExceptionEvents];
--truncate table [dbo].[FailedDVIRDefectUpdates];
--truncate table [dbo].[FailedOVDSServerCommands];
--truncate table [dbo].[FaultData];
--truncate table [dbo].[Groups];
--truncate table [dbo].[LogRecords];
--truncate table [dbo].[MyGeotabVersionInfo];
--truncate table [dbo].[OServiceTracking];
--truncate table [dbo].[OVDSServerCommands];
--truncate table [dbo].[Rules];
--truncate table [dbo].[StatusData];
--truncate table [dbo].[Trips];
--truncate table [dbo].[Users];
--truncate table [dbo].[Zones];
--truncate table [dbo].[ZoneTypes];
--DBCC CHECKIDENT ('dbo.BinaryData', RESEED, 0);
--DBCC CHECKIDENT ('dbo.ChargeEvents', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Conditions', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DebugData', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Devices', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DeviceStatusInfo', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Diagnostics', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DriverChanges', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DutyStatusAvailabilities', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DutyStatusLogs', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DVIRDefectRemarks', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DVIRDefects', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DVIRDefectUpdates', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DVIRLogs', RESEED, 0);
--DBCC CHECKIDENT ('dbo.ExceptionEvents', RESEED, 0);
--DBCC CHECKIDENT ('dbo.FailedDVIRDefectUpdates', RESEED, 0);
--DBCC CHECKIDENT ('dbo.FailedOVDSServerCommands', RESEED, 0);
--DBCC CHECKIDENT ('dbo.FaultData', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Groups', RESEED, 0);
--DBCC CHECKIDENT ('dbo.LogRecords', RESEED, 0);
--DBCC CHECKIDENT ('dbo.OServiceTracking', RESEED, 0);
--DBCC CHECKIDENT ('dbo.OVDSServerCommands', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Rules', RESEED, 0);
--DBCC CHECKIDENT ('dbo.StatusData', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Trips', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Users', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Zones', RESEED, 0);
--DBCC CHECKIDENT ('dbo.ZoneTypes', RESEED, 0);

---- Step 4: Recreate foreign key constraints:
--EXEC sp_executesql @sqlCreate;

---- Commit transaction
--COMMIT TRANSACTION;

--PRINT 'Tables truncated and constraints recreated successfully.';
--END TRY
--BEGIN CATCH
---- Rollback transaction if an error occurs
--ROLLBACK TRANSACTION;

---- Print error message
--PRINT 'An error occurred. Transaction rolled back.';
--PRINT ERROR_MESSAGE();
--END CATCH;
--/*** [END] Clean Database ***/ 




/*** Check counts ***/
set nocount on;
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