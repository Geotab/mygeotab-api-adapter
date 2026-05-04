-- ================================================================================
-- DATABASE TYPE: SQL Server
-- 
-- NOTES: 
--   1: This script applies to the Geotab DIG Adapter database starting with
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
--    -- Step 1: Capture foreign key constraints (Restricted to gda schema)
--    DECLARE @sqlDrop NVARCHAR(MAX) = N'';
--    DECLARE @sqlCreate NVARCHAR(MAX) = N'';

--    SELECT 
--        @sqlDrop += 'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + 
--                    ' DROP CONSTRAINT ' + QUOTENAME(fk.name) + '; '
--        ,@sqlCreate += 'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + 
--                       ' ADD CONSTRAINT ' + QUOTENAME(fk.name) + ' FOREIGN KEY (' + 
--                       STRING_AGG(QUOTENAME(fc.name), ', ') + ') REFERENCES ' + 
--                       QUOTENAME(rs.name) + '.' + QUOTENAME(rt.name) + ' (' + 
--                       STRING_AGG(QUOTENAME(rc.name), ', ') + '); '
--    FROM sys.foreign_keys AS fk
--    INNER JOIN sys.foreign_key_columns AS fkc
--        ON fk.object_id = fkc.constraint_object_id
--    INNER JOIN sys.tables AS t
--        ON fkc.parent_object_id = t.object_id
--    INNER JOIN sys.columns AS fc
--        ON fkc.parent_object_id = fc.object_id AND fkc.parent_column_id = fc.column_id
--    INNER JOIN sys.tables AS rt
--        ON fkc.referenced_object_id = rt.object_id
--    INNER JOIN sys.columns AS rc
--        ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
--    INNER JOIN sys.schemas AS s
--        ON t.schema_id = s.schema_id
--    INNER JOIN sys.schemas AS rs
--        ON rt.schema_id = rs.schema_id
--    WHERE t.schema_id = SCHEMA_ID('gda')
--    GROUP BY s.name, t.name, fk.name, rs.name, rt.name;

--    -- Step 2: Drop foreign key constraints:
--    EXEC sp_executesql @sqlDrop;

--    -- Step 3: Truncate tables and reseed indexes:
--    -- Core tables (Note: MiddlewareVersionInfo is NOT truncated - it tracks DB version history)
--    TRUNCATE TABLE [gda].[OServiceTracking];
--    TRUNCATE TABLE [gda].[ProvisionedDevices];
--    -- Device provisioning tables
--    TRUNCATE TABLE [gda].[Q_ProvisionDevices];
--    TRUNCATE TABLE [gda].[Q_ProvisionDevicesFail];
--    -- Telemetry record queue and fail tables (11 record types)
--    TRUNCATE TABLE [gda].[Q_GpsRecords];
--    TRUNCATE TABLE [gda].[Q_GpsRecordsFail];
--    TRUNCATE TABLE [gda].[Q_AccelerationRecords];
--    TRUNCATE TABLE [gda].[Q_AccelerationRecordsFail];
--    TRUNCATE TABLE [gda].[Q_BinaryRecords];
--    TRUNCATE TABLE [gda].[Q_BinaryRecordsFail];
--    TRUNCATE TABLE [gda].[Q_BluetoothRecords];
--    TRUNCATE TABLE [gda].[Q_BluetoothRecordsFail];
--    TRUNCATE TABLE [gda].[Q_DriverChangeRecords];
--    TRUNCATE TABLE [gda].[Q_DriverChangeRecordsFail];
--    TRUNCATE TABLE [gda].[Q_GenericFaultRecords];
--    TRUNCATE TABLE [gda].[Q_GenericFaultRecordsFail];
--    TRUNCATE TABLE [gda].[Q_GenericStatusRecords];
--    TRUNCATE TABLE [gda].[Q_GenericStatusRecordsFail];
--    TRUNCATE TABLE [gda].[Q_J1708FaultRecords];
--    TRUNCATE TABLE [gda].[Q_J1708FaultRecordsFail];
--    TRUNCATE TABLE [gda].[Q_J1939FaultRecords];
--    TRUNCATE TABLE [gda].[Q_J1939FaultRecordsFail];
--    TRUNCATE TABLE [gda].[Q_ObdiiFaultRecords];
--    TRUNCATE TABLE [gda].[Q_ObdiiFaultRecordsFail];
--    TRUNCATE TABLE [gda].[Q_VinRecords];
--    TRUNCATE TABLE [gda].[Q_VinRecordsFail];
--    -- Invalid records tables
--    TRUNCATE TABLE [gda].[DIGInvalidRecords];
--    TRUNCATE TABLE [gda].[DIGInvalidRecordsCursor];

--    -- Reseed identity columns
--    -- Core tables (Note: MiddlewareVersionInfo is NOT reseeded - it tracks DB version history)
--    DBCC CHECKIDENT ('gda.OServiceTracking', RESEED, 0);
--    DBCC CHECKIDENT ('gda.ProvisionedDevices', RESEED, 0);
--    -- Device provisioning tables
--    DBCC CHECKIDENT ('gda.Q_ProvisionDevices', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_ProvisionDevicesFail', RESEED, 0);
--    -- Telemetry record queue and fail tables (11 record types)
--    DBCC CHECKIDENT ('gda.Q_GpsRecords', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_GpsRecordsFail', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_AccelerationRecords', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_AccelerationRecordsFail', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_BinaryRecords', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_BinaryRecordsFail', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_BluetoothRecords', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_BluetoothRecordsFail', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_DriverChangeRecords', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_DriverChangeRecordsFail', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_GenericFaultRecords', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_GenericFaultRecordsFail', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_GenericStatusRecords', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_GenericStatusRecordsFail', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_J1708FaultRecords', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_J1708FaultRecordsFail', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_J1939FaultRecords', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_J1939FaultRecordsFail', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_ObdiiFaultRecords', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_ObdiiFaultRecordsFail', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_VinRecords', RESEED, 0);
--    DBCC CHECKIDENT ('gda.Q_VinRecordsFail', RESEED, 0);
--    -- Invalid records tables
--    DBCC CHECKIDENT ('gda.DIGInvalidRecords', RESEED, 0);
--    -- Note: DIGInvalidRecordsCursor does not have an IDENTITY column

--    -- Step 4: Re-add sentinel records:
--    -- Re-insert the DIGInvalidRecordsCursor row (required for service operation)
--    INSERT INTO [gda].[DIGInvalidRecordsCursor] ([id], [NextResultKey], [LastUpdatedUtc])
--    VALUES (1, 0, GETUTCDATE());

--    -- Step 5: Recreate foreign key constraints:
--    EXEC sp_executesql @sqlCreate;

--    -- Commit transaction
--    COMMIT TRANSACTION;

--    PRINT 'Tables truncated and constraints recreated successfully.';
--END TRY
--BEGIN CATCH
--    -- Rollback transaction if an error occurs
--    ROLLBACK TRANSACTION;

--    -- Print error message
--    PRINT 'An error occurred. Transaction rolled back.';
--    PRINT ERROR_MESSAGE();
--END CATCH;
--/*** [END] Clean Database ***/




/* Check counts */
--EXEC sp_updatestats;
SELECT t.name AS TableName,
    SUM(p.rows) AS RecordCount
FROM sys.tables t
JOIN sys.partitions p ON t.object_id = p.object_id
WHERE t.type = 'U' -- User tables only
    AND t.schema_id = SCHEMA_ID('gda')
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
    WHERE A.schema_id = SCHEMA_ID('gda')
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