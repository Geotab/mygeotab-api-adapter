-- ================================================================================
-- DATABASE TYPE: SQL Server
--
-- DESCRIPTION:
--   The purpose of this script is to upgrade the MyGeotab API Adapter database
--   from version 5.1.0.0 to version 5.2.0.0.
--
--   Changes:
--   1. Corrects the vwStatsForLevel1DBMaintenance view, which understated each
--      table's percentage of modifications since its statistics were last
--      updated. It summed row counts across a join of the per-index partition
--      statistics to the statistics objects, which repeated every row count once
--      per statistics object - multiplying it by the number of indexes times the
--      number of statistics. View body only; no table is altered.
--   2. Limits both maintenance views, vwStatsForLevel1DBMaintenance and
--      vwStatsForLevel2DBMaintenance, to the dbo schema. Neither filtered by
--      schema, so on a database shared with the Geotab DIG Adapter this adapter
--      maintained the DIG Adapter's gda schema as well - and the Level 2 view read
--      the physical statistics of every index in the database to do it. Level 2
--      now reads them table by table, for dbo tables only. View bodies only.
--   3. Corrects spManagePartitions, whose check for whether a partition boundary
--      already existed looked in every partition function in the database. On a
--      database shared with the Geotab DIG Adapter, which partitions its own tables
--      on the same monthly boundaries and creates each month's boundary first, the
--      procedure skipped its own boundaries, reported success, and left every new
--      row of the adapter's tables in one ever-growing partition. The check now
--      looks only in this adapter's partition function. The procedure is patched
--      in place - one statement - so that the data file path each installation has
--      set in its @filePath variable is kept. (The corresponding PostgreSQL release
--      scopes that provider's spManagePartitions function to the public schema, for
--      a different shared-database failure.)
--
-- NOTES:
--   1: This script cannot be run against any database version other than that
--		specified above.
--   2: Be sure to alter the "USE [geotabadapterdb]" statement below if you have
--      changed the database name to something else.
-- ================================================================================

USE [geotabadapterdb]
GO

/*** [START] Part 1 of 3: Database Version Validation Below ***/
-- Store upgrade database version in a temporary table.
DROP TABLE IF EXISTS #TMP_UpgradeDatabaseVersionTable;
CREATE TABLE #TMP_UpgradeDatabaseVersionTable (UpgradeDatabaseVersion NVARCHAR(50));
INSERT INTO #TMP_UpgradeDatabaseVersionTable VALUES ('5.2.0.0');

DECLARE @requiredStartingDatabaseVersion NVARCHAR(50) = '5.1.0.0';
DECLARE @actualStartingDatabaseVersion NVARCHAR(50);

SELECT TOP 1 @actualStartingDatabaseVersion = DatabaseVersion
FROM dbo.MiddlewareVersionInfo2
ORDER BY RecordCreationTimeUtc DESC;

IF @actualStartingDatabaseVersion IS NULL
BEGIN
	RAISERROR('ERROR: No row was found in the dbo.MiddlewareVersionInfo2 table. This script can only be run against a MyGeotab API Adapter database of version %s.', 16, 1, @requiredStartingDatabaseVersion);
	SET NOEXEC ON;
END

IF @actualStartingDatabaseVersion <> @requiredStartingDatabaseVersion
BEGIN
	RAISERROR('ERROR: This script can only be run against the expected database version. [Expected: %s; Actual: %s]', 16, 1, @requiredStartingDatabaseVersion, @actualStartingDatabaseVersion);
	SET NOEXEC ON;
END
/*** [END] Part 1 of 3: Database Version Validation Above ***/



/*** [START] Part 2 of 3: Database Upgrades Below ***/
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spManagePartitions stored procedure (change 3 above):
--
-- Patched IN PLACE rather than re-created. The procedure's body declares a @filePath
-- variable that each installation sets to its own data file directory; re-creating
-- the procedure from this script would put the default back, and the next month's
-- FileGroup files would be created in the wrong directory, or fail to be created.
-- So the one statement that changes is replaced in the definition the database
-- already holds, and everything else in it - @filePath included - is kept. The new
-- statement is the one MSSQL_0.0.0.1_spManagePartitions.sql creates.
--
-- If the stored definition does not contain the old statement exactly once, the
-- procedure has been customised: the script stops here, before the database version
-- is updated. Make the change by hand as the upgrade guide describes, then run the
-- script again. Running it against a procedure that already has the change is
-- harmless.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
DECLARE @spManagePartitionsDefinition NVARCHAR(MAX) = OBJECT_DEFINITION(OBJECT_ID(N'[dbo].[spManagePartitions]'));
DECLARE @oldExistenceCheck NVARCHAR(200) = N'IF NOT EXISTS (SELECT * FROM sys.partition_range_values WHERE value = @partitionValue)';
DECLARE @newExistenceCheck NVARCHAR(MAX) = N'IF NOT EXISTS (
				-- Look only in the partition function this procedure manages. Another component
				-- sharing the database - the Geotab DIG Adapter, for example - can hold a partition
				-- function with the same boundary values, and counting those would skip this one.
				SELECT 1
				FROM sys.partition_range_values prv
				INNER JOIN sys.partition_functions pf
					ON pf.function_id = prv.function_id
				WHERE pf.name = @partitionFunctionName
					AND prv.value = @partitionValue
			)';
DECLARE @occurrences INT;
DECLARE @keywordPosition INT;
DECLARE @alterError NVARCHAR(2048);

IF @spManagePartitionsDefinition IS NULL
BEGIN
	RAISERROR('ERROR: The dbo.spManagePartitions stored procedure was not found, or its definition could not be read. The database was not upgraded.', 16, 1);
	SET NOEXEC ON;
END
ELSE IF CHARINDEX(N'AND prv.value = @partitionValue', @spManagePartitionsDefinition) > 0 AND CHARINDEX(@oldExistenceCheck, @spManagePartitionsDefinition) = 0
BEGIN
	RAISERROR('> spManagePartitions already looks only in its own partition function; left unchanged.', 0, 1) WITH NOWAIT;
END
ELSE
BEGIN
	SET @occurrences = (DATALENGTH(@spManagePartitionsDefinition) - DATALENGTH(REPLACE(@spManagePartitionsDefinition, @oldExistenceCheck, N''))) / DATALENGTH(@oldExistenceCheck);
	IF @occurrences <> 1
	BEGIN
		RAISERROR('ERROR: The dbo.spManagePartitions stored procedure does not contain the statement this upgrade replaces exactly once (found %d), so it appears to have been customised. The database was not upgraded; see the upgrade guide.', 16, 1, @occurrences);
		SET NOEXEC ON;
	END
	ELSE
	BEGIN
		SET @spManagePartitionsDefinition = REPLACE(@spManagePartitionsDefinition, @oldExistenceCheck, @newExistenceCheck);
		-- SQL Server stores a procedure's text as CREATE PROCEDURE even after an ALTER, so the
		-- stored text always starts that way; it must say ALTER to be executed again.
		SET @keywordPosition = CHARINDEX(N'CREATE PROCEDURE [dbo].[spManagePartitions]', @spManagePartitionsDefinition);
		IF @keywordPosition = 0
		BEGIN
			-- Stored in some other form - deployed with CREATE OR ALTER, as CREATE PROC, or without the
			-- brackets - so it has been customised; executing its text as it stands would only fail with
			-- "already exists".
			RAISERROR('ERROR: The dbo.spManagePartitions stored procedure is not stored in the form this upgrade expects (its definition does not contain CREATE PROCEDURE [dbo].[spManagePartitions]), so it appears to have been customised. The database was not upgraded; see the upgrade guide.', 16, 1);
			SET NOEXEC ON;
		END
		ELSE
		BEGIN
			SET @spManagePartitionsDefinition = STUFF(@spManagePartitionsDefinition, @keywordPosition, LEN(N'CREATE'), N'ALTER');
			BEGIN TRY
				EXEC sp_executesql @spManagePartitionsDefinition;
				RAISERROR('> spManagePartitions now looks only in its own partition function.', 0, 1) WITH NOWAIT;
			END TRY
			BEGIN CATCH
				SET @alterError = ERROR_MESSAGE();
				RAISERROR('ERROR: Updating dbo.spManagePartitions failed: %s The database was not upgraded.', 16, 1, @alterError);
				SET NOEXEC ON;
			END CATCH
		END
	END
END
GO

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Recreate View: vwStatsForLevel1DBMaintenance (changes 1 and 2 above).
--
-- DROPped and re-CREATEd rather than altered, so that the stored definition - SQL
-- Server keeps the whole CREATE batch, leading comment included - is byte-identical
-- to the one MSSQL_CumulativeSchemaCreation.sql creates.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
DROP VIEW IF EXISTS [dbo].[vwStatsForLevel1DBMaintenance];
GO

-- ==========================================================================================
-- Description:	Returns a list of tables that have had at least 1000 modifications since the
--				last time statistics were updated.
--
--				Each table's row count is taken from its heap or clustered index alone, and
--				its statistics are aggregated separately. Before version 5.2.0.0 this view
--				joined the per-index partition statistics to the statistics objects before
--				summing, which repeated every row count once per statistics object: the row
--				count was multiplied by the number of indexes times the number of statistics,
--				and PctModsSinceLastStatsUpdate was divided by the same factor.
--
--				Only tables in the dbo schema are returned. A database shared with the
--				Geotab DIG Adapter also holds its gda schema, which it maintains itself.
-- ==========================================================================================
CREATE VIEW [dbo].[vwStatsForLevel1DBMaintenance]
AS
WITH UserTables AS (
    SELECT tbl.object_id,
        sch.name AS SchemaName,
        tbl.name AS TableName
    FROM sys.tables tbl
    INNER JOIN sys.schemas sch
		ON tbl.schema_id = sch.schema_id
    WHERE sch.name = 'dbo'
),
TableRows AS (
    SELECT ut.object_id,
        SUM(CASE WHEN ps.index_id IN (0, 1) THEN ps.row_count ELSE 0 END) AS RecordCount,
        SUM(ps.used_page_count * 8) AS UsedSpaceKB,
        SUM(ps.reserved_page_count * 8) AS ReservedSpaceKB,
        (SUM(ps.reserved_page_count) - SUM(ps.used_page_count)) * 8 AS FreeSpaceKB
    FROM UserTables ut
    INNER JOIN sys.dm_db_partition_stats ps
		ON ps.object_id = ut.object_id
    GROUP BY ut.object_id
),
TableModifications AS (
    SELECT ut.object_id,
        MAX(sp.modification_counter) AS ModsSinceLastStatsUpdate,
        MAX(sp.last_updated) AS LastStatsUpdate
    FROM UserTables ut
    INNER JOIN sys.stats st
		ON st.object_id = ut.object_id
    CROSS APPLY sys.dm_db_stats_properties(st.object_id, st.stats_id) sp
    GROUP BY ut.object_id
),
TableStats AS (
    SELECT ut.SchemaName,
        ut.TableName,
        tr.RecordCount,
        tr.UsedSpaceKB,
        tr.ReservedSpaceKB,
        tr.FreeSpaceKB,
        tm.ModsSinceLastStatsUpdate,
        tm.LastStatsUpdate
    FROM UserTables ut
    INNER JOIN TableRows tr
		ON tr.object_id = ut.object_id
    INNER JOIN TableModifications tm
		ON tm.object_id = ut.object_id
)
SELECT
    ROW_NUMBER() OVER (ORDER BY ModsSinceLastStatsUpdate DESC) AS RowId,
    SchemaName,
    TableName,
    RecordCount,
    UsedSpaceKB,
    ReservedSpaceKB,
    FreeSpaceKB,
    ModsSinceLastStatsUpdate,
    CASE
        WHEN RecordCount > 0 THEN (CAST(ModsSinceLastStatsUpdate AS FLOAT) / RecordCount) * 100
        ELSE 0
    END AS PctModsSinceLastStatsUpdate,
    LastStatsUpdate
FROM TableStats
WHERE ModsSinceLastStatsUpdate > 1000;
GO
ALTER AUTHORIZATION ON [dbo].[vwStatsForLevel1DBMaintenance] TO  SCHEMA OWNER 
GO

-- A client that carries on after an error - SQL Server Management Studio, or sqlcmd
-- without -b - would otherwise go on to Part 3 and record 5.2.0.0 over a view that
-- failed to be created, or that could not be dropped and still has its old body. The
-- version then refuses a re-run. Refuse the stamp instead: the database stays at
-- 5.1.0.0, and this script can be run again once the cause is fixed. A definition
-- the running login may not read comes back NULL, and NOT LIKE on NULL is unknown,
-- which the IF would treat as a pass - so an unreadable definition is refused too.
IF OBJECT_ID(N'[dbo].[vwStatsForLevel1DBMaintenance]', N'V') IS NULL
   OR ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'[dbo].[vwStatsForLevel1DBMaintenance]', N'V')), N'') NOT LIKE N'%index_id IN (0, 1) THEN ps.row_count%'
BEGIN
    RAISERROR('ERROR: dbo.vwStatsForLevel1DBMaintenance was not created again with its 5.2.0.0 definition; see the error above. The database version has not been changed. Fix the cause and run this script again.', 16, 1);
    SET NOEXEC ON;
END
GO

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Recreate View: vwStatsForLevel2DBMaintenance (change 2 above).
--
-- DROPped and re-CREATEd for the same reason as vwStatsForLevel1DBMaintenance above:
-- its stored definition must be byte-identical to the cumulative script's.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
DROP VIEW IF EXISTS [dbo].[vwStatsForLevel2DBMaintenance];
GO

-- ==========================================================================================
-- Description:	Returns a list of indexes that are at least 1KB in size with fragmentation of
--				over 10 percent. Includes stats by individual partition as well as
--				TotalPartitions and HighFragPartitions columns which show the number of 
--				partitions in the subject table as well as the number of partitions with 
--				fragmentation of over 30% for the subject index. These can help in
--				determining whether to reorgainize or rebuild an entire index or just 
--				specific partitions.
--
--				Only indexes on tables in the dbo schema are returned, and the index
--				statistics are read table by table so that nothing outside dbo is read at
--				all. A database shared with the Geotab DIG Adapter also holds its gda
--				schema, which it maintains itself.
-- ==========================================================================================
CREATE VIEW [dbo].[vwStatsForLevel2DBMaintenance]
AS
WITH DboTables AS (
    -- TOP stops the optimizer applying the schema filter after the index statistics have been read.
    -- Without it the plan applies the function below to every table in the database and discards the
    -- rows outside this schema afterwards - so it reads them all.
    SELECT TOP (2147483647) obj.object_id,
        sch.name AS SchemaName,
        obj.name AS TableName
    FROM sys.objects obj
    INNER JOIN sys.schemas sch
		ON obj.schema_id = sch.schema_id
    WHERE sch.name = 'dbo'
        AND obj.type = 'U'
),
RawPartitionStats AS (
    SELECT dt.SchemaName,
        dt.TableName,
        idx.name AS IndexName,
        ps.partition_number AS PartitionNumber,
        ps.avg_fragmentation_in_percent AS FragmentationPct,
        ps.page_count * 8 AS IndexSizeKB
    FROM DboTables dt
    -- LIMITED reports each index's leaf level, which is what REORGANIZE and REBUILD act on, and reads only the pages
    -- above it. DETAILED returned a row for every upper level too, and the highest fragmentation among them - often a
    -- small upper level's - decided, so a healthy index could be offered for a rebuild.
    CROSS APPLY sys.dm_db_index_physical_stats(DB_ID(), dt.object_id, NULL, NULL, 'LIMITED') ps
    INNER JOIN sys.indexes idx
		ON ps.object_id = idx.object_id AND ps.index_id = idx.index_id
    WHERE ps.index_id > 0 -- (exclude heap indexes)
      -- In-row data only, so that each row's fragmentation and size describe the same allocation unit.
      AND ps.alloc_unit_type_desc = N'IN_ROW_DATA'
),
PartitionStats AS (
    SELECT SchemaName,
        TableName,
        IndexName,
        PartitionNumber,
        MAX(FragmentationPct) AS FragmentationPct,
        MAX(IndexSizeKB) AS IndexSizeKB
    FROM RawPartitionStats
    GROUP BY SchemaName, TableName, IndexName, PartitionNumber
),
WithAggregates AS (
    -- The per-index totals are window aggregates over the same rows, so PartitionStats - and
    -- the index statistics function inside it - is read ONCE. SQL Server evaluates a CTE once
    -- per reference, and an aggregate CTE joined back to PartitionStats read every page of
    -- every index twice per run. COUNT(*) equals a count of distinct partitions here because
    -- PartitionStats already holds one row per partition of each index.
    SELECT SchemaName,
        TableName,
        IndexName,
        PartitionNumber,
        FragmentationPct,
        IndexSizeKB,
        COUNT(*) OVER (PARTITION BY SchemaName, TableName, IndexName) AS TotalPartitions,
        SUM(CASE WHEN FragmentationPct > 30 THEN 1 ELSE 0 END) OVER (PARTITION BY SchemaName, TableName, IndexName) AS HighFragPartitions
    FROM PartitionStats
)
SELECT ROW_NUMBER() OVER (ORDER BY TableName ASC, IndexName ASC, PartitionNumber ASC) AS RowId,
    SchemaName,
    TableName,
    IndexName,
    PartitionNumber,
    FragmentationPct,
    IndexSizeKB,
    TotalPartitions,
    HighFragPartitions,
    CASE
        WHEN TotalPartitions > 0 THEN (CAST(HighFragPartitions AS FLOAT) / TotalPartitions) * 100
        ELSE 0
    END AS PctHighFragPartitions
FROM WithAggregates
WHERE FragmentationPct > 10
    AND IndexSizeKB > 1;
GO
ALTER AUTHORIZATION ON [dbo].[vwStatsForLevel2DBMaintenance] TO  SCHEMA OWNER 
GO

-- A client that carries on after an error - SQL Server Management Studio, or sqlcmd
-- without -b - would otherwise go on to Part 3 and record 5.2.0.0 over a view that
-- failed to be created, or that could not be dropped and still has its old body. The
-- version then refuses a re-run. Refuse the stamp instead: the database stays at
-- 5.1.0.0, and this script can be run again once the cause is fixed. A definition
-- the running login may not read comes back NULL, and NOT LIKE on NULL is unknown,
-- which the IF would treat as a pass - so an unreadable definition is refused too.
IF OBJECT_ID(N'[dbo].[vwStatsForLevel2DBMaintenance]', N'V') IS NULL
   OR ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'[dbo].[vwStatsForLevel2DBMaintenance]', N'V')), N'') NOT LIKE N'%TOP (2147483647) obj.object_id%'
BEGIN
    RAISERROR('ERROR: dbo.vwStatsForLevel2DBMaintenance was not created again with its 5.2.0.0 definition; see the error above. The database version has not been changed. Fix the cause and run this script again.', 16, 1);
    SET NOEXEC ON;
END
GO
/*** [END] Part 2 of 3: Database Upgrades (tables, sequences, views) Above ***/



/*** [START] Part 3 of 3: Database Version Update Below ***/
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO [dbo].[MiddlewareVersionInfo2] ([DatabaseVersion], [RecordCreationTimeUtc])
SELECT UpgradeDatabaseVersion, GETUTCDATE()
FROM #TMP_UpgradeDatabaseVersionTable;
DROP TABLE IF EXISTS #TMP_UpgradeDatabaseVersionTable;
/*** [END] Part 3 of 3: Database Version Update Above ***/

/*** [START] Restore execution state (no-op on a successful run) ***/
SET NOEXEC OFF;
GO
/*** [END] Restore execution state ***/
