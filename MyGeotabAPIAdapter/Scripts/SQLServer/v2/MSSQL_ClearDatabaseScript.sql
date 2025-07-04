-- ================================================================================
-- DATABASE TYPE: SQL Server
-- 
-- NOTES: 
--   1: This script applies to the MyGeotab API Adapter database starting with
--	    application version 3.0.0. It does not apply to earlier versions of the
--      application. 
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
--truncate table [dbo].[DBMaintenanceLogs2];
--truncate table [dbo].[BinaryData2];
--truncate table [dbo].[ChargeEvents2];
--truncate table [dbo].[DutyStatusAvailabilities2];
--truncate table [dbo].[DVIRDefectRemarks2];
--truncate table [dbo].[DVIRDefects2];
--truncate table [dbo].[DVIRLogs2];
--truncate table [dbo].[Devices2];
--truncate table [dbo].[DeviceStatusInfo2];
--truncate table [dbo].[DiagnosticIds2];
--truncate table [dbo].[Diagnostics2];
--truncate table [dbo].[DriverChanges2];
--truncate table [dbo].[EntityMetadata2];
--truncate table [dbo].[ExceptionEvents2];
--truncate table [dbo].[FaultData2];
--truncate table [dbo].[FaultDataLocations2];
--truncate table [dbo].[Groups2];
--truncate table [dbo].[LogRecords2];
--truncate table [dbo].[MyGeotabVersionInfo2];
--truncate table [dbo].[OServiceTracking2];
--truncate table [dbo].[Rules2];
--truncate table [dbo].[StatusData2];
--truncate table [dbo].[StatusDataLocations2];
--truncate table [dbo].[Trips2];
--truncate table [dbo].[Users2];
--truncate table [dbo].[Zones2];
--truncate table [dbo].[ZoneTypes2];
--DBCC CHECKIDENT ('dbo.DBMaintenanceLogs2', RESEED, 0);
--DBCC CHECKIDENT ('dbo.DiagnosticIds2', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Diagnostics2', RESEED, 0);
--DBCC CHECKIDENT ('dbo.EntityMetadata2', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Groups2', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Rules2', RESEED, 0);
--DBCC CHECKIDENT ('dbo.Trips2', RESEED, 0);
--DBCC CHECKIDENT ('dbo.ZoneTypes2', RESEED, 0);
--DBCC CHECKIDENT ('dbo.OServiceTracking2', RESEED, 0);

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




/* Check counts */
--EXEC sp_updatestats;
SELECT t.name AS TableName,
    SUM(p.rows) AS RecordCount
FROM sys.tables t
JOIN sys.partitions p ON t.object_id = p.object_id
WHERE t.type = 'U' -- User tables only
    AND p.index_id IN (0, 1) -- 0 = heap, 1 = clustered index
	AND t.name NOT LIKE 'stg_%'
GROUP BY t.name
ORDER BY t.name;


/* Check counts by partition */
--EXEC sp_updatestats;
WITH PartitionInfo AS (
    SELECT (SCHEMA_NAME(A.schema_id) + '.' + A.Name) AS TableName,  
        B.partition_number AS PartitionNumber,
        B.row_count AS RecordCount,
        FG.name AS FileGroupName,
        ROW_NUMBER() OVER (PARTITION BY SCHEMA_NAME(A.schema_id), A.Name, B.partition_number ORDER BY B.partition_number) AS RowNum
    FROM sys.dm_db_partition_stats B 
    LEFT JOIN sys.objects A 
        ON A.object_id = B.object_id
    LEFT JOIN sys.partitions P 
        ON P.object_id = B.object_id 
			AND P.partition_id = B.partition_id
    LEFT JOIN sys.allocation_units AU
        ON P.partition_id = AU.container_id
    LEFT JOIN sys.data_spaces DS
        ON AU.data_space_id = DS.data_space_id
    LEFT JOIN sys.filegroups FG
        ON DS.data_space_id = FG.data_space_id
    WHERE SCHEMA_NAME(A.schema_id) <> 'sys' 
        AND (B.index_id = 0 OR B.index_id = 1)  -- 0 = heap, 1 = clustered index
		AND A.Name NOT LIKE 'stg_%'
)
SELECT TableName,
    PartitionNumber,
    RecordCount,
    FileGroupName
FROM PartitionInfo
WHERE RowNum = 1
ORDER BY TableName, PartitionNumber;