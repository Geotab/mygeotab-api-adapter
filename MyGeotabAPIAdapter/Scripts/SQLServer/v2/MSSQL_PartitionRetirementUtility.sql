-- ================================================================================
-- DATABASE TYPE: SQL Server
--
-- NOTES:
--   1: This is a STANDALONE, ON-DEMAND utility script. It is NOT part of database
--      creation or upgrade and is never required for normal adapter operation.
--      Installing it only adds three stored procedures; it does not read or modify
--      any data at installation time.
--   2: Be sure to alter the "USE [geotabadapterdb]" statement below if you have
--      changed the database name to something else.
--   3: The procedures implement the partition-retirement procedure documented in
--      README Section 3.6 (Automated Database Maintenance > Longer-Term Data
--      Retention Strategy). Step numbers in messages refer to the numbered steps
--      of that section's SQL Server procedure.
--   4: Requirements: SQL Server 2019 or later; run as a member of db_owner.
--      Removing files/filegroups additionally requires ALTER permission on the
--      database. The connected-session check requires VIEW SERVER STATE; when
--      that permission is not held, execution refuses unless
--      @AcknowledgeSessionCheckUnavailable = 1 is passed (in which case YOU are
--      responsible for ensuring the adapter is stopped and no other sessions are
--      connected).
--
-- DESCRIPTION:
--   Retires (removes from the live tables) whole partition periods older than a
--   cutoff date, using metadata only — the procedures discover the partitioned
--   tables, partition boundaries, filegroups, non-aligned indexes, constraints
--   and foreign keys at execution time, so they work for monthly, weekly or
--   daily partition intervals and for any database at schema version 3.13.0.0
--   or later.
--
--   Three procedures are installed:
--
--     1. [dbo].[spAdapterPartitionRetirement_Preview] @OlderThan
--          Always read-only. Reports what the other two procedures would do:
--          retirable periods, per-table row counts, non-aligned indexes and
--          constraints, foreign keys that must be dropped/recreated, staging
--          tables awaiting archive, and filegroups that can be removed.
--
--     2. [dbo].[spAdapterPartitionRetirement_SwitchOut] @OlderThan, @Execute = 0,
--          @AcknowledgeSessionCheckUnavailable = 0
--          README steps 2-5. For each partitioned table holding rows in a
--          retirable period: creates a staging table on the partition's
--          filegroup (cloning the table's actual clustered structure — clustered
--          primary key, clustered unique constraint or plain clustered index),
--          drops whatever blocks the switch (recording full definitions first):
--            - foreign keys that reference a table being switched (recreated
--              WITH CHECK afterwards — both sides of each foreign-key pair
--              retire the same periods in the same run),
--            - non-aligned PRIMARY KEY / UNIQUE constraints (via ALTER TABLE),
--            - non-aligned nonclustered indexes,
--          SWITCHes the partitions out (metadata-only), and recreates everything
--          it dropped. The staged rows REMAIN IN THE DATABASE in
--          [<Table>_retired_<period>] staging tables — this procedure never
--          deletes data.
--
--     3. [dbo].[spAdapterPartitionRetirement_Cleanup] @OlderThan,
--          @ConfirmDataArchivedOrNotNeeded = 0, @Execute = 0,
--          @AcknowledgeSessionCheckUnavailable = 0
--          README steps 6-8. Drops the staging tables (REFUSES unless
--          @ConfirmDataArchivedOrNotNeeded = 1 — back up or export the staged
--          data first if it must be kept; additionally refuses any staging
--          table that still holds rows dated on or after @OlderThan), merges
--          the emptied partition boundaries (only after verifying the partition
--          is empty in EVERY table on the partition function), and removes the
--          freed files and filegroups.
--
--   Safety properties:
--     - @Execute defaults to 0 on both modifying procedures: they only PRINT the
--       exact statements they would run. Set @Execute = 1 to perform them.
--     - Dropping staging tables (the only destructive action) additionally
--       requires @ConfirmDataArchivedOrNotNeeded = 1, and is refused table-by-
--       table if the staging data extends to @OlderThan or later. Archiving the
--       staged data is always a deliberate, manual decision — see README step 6.
--     - When executing, the procedures refuse to run while any other session is
--       connected to the database (stop the adapter first — see the README
--       section's order of operations, step 1). If the session check cannot be
--       performed (VIEW SERVER STATE not held), execution refuses unless
--       @AcknowledgeSessionCheckUnavailable = 1.
--     - Every phase is re-runnable: an execution interrupted at any point can be
--       re-run with the same parameters and will complete the remaining work.
--       Before a foreign key, constraint or non-aligned index is dropped, its
--       full definition is saved as an extended property on its table (name
--       prefixes 'PartitionRetirement_Fk_', 'PartitionRetirement_Cns_' and
--       'PartitionRetirement_Idx_') and the property is removed only after the
--       object is recreated, so an interrupted run can always restore them.
--       ALL definitions are captured and validated BEFORE the first drop.
--     - Pre-flight refusals protect shapes this utility must not touch: tables
--       enabled for CDC, change tracking or replication; a partition function
--       backing more than one partition scheme; computed columns (that table is
--       skipped and reported); live rows outside the retirement window that
--       reference rows inside it via a foreign key (the referenced table's
--       switches are skipped and reported rather than orphaning references).
--     - Period suffixes are generated culture-independently (session language
--       cannot affect them), and a boundary whose period suffix would collide
--       with another boundary's (possible when the initial partition layout was
--       created mid-month) deterministically falls back to a day-precision
--       suffix.
--     - Only whole periods strictly older than @OlderThan are touched. The
--       current and future periods can never be affected.
-- ================================================================================

USE [geotabadapterdb]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- ================================================================================
-- spAdapterPartitionRetirement_Preview
--   Read-only report of the current partition-retirement state and of the actions
--   the SwitchOut and Cleanup procedures would take for the supplied @OlderThan.
-- ================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spAdapterPartitionRetirement_Preview]
    @OlderThan datetime2(7)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @partitionFunctionName sysname = N'DateTimePartitionFunction_MyGeotabApiAdapter';
    DECLARE @partitionSchemeName sysname = N'DateTimePartitionScheme_MyGeotabApiAdapter';
    DECLARE @partitionFunctionId int;
    DECLARE @interval nvarchar(50);
    DECLARE @firstBoundary datetime2(7);
    DECLARE @msg nvarchar(4000);

    -- ======================================================================================
    -- Validate.
    IF @OlderThan IS NULL
    BEGIN
        RAISERROR('ERROR: @OlderThan is required. Example: EXEC [dbo].[spAdapterPartitionRetirement_Preview] @OlderThan = ''2023-01-01'';', 16, 1);
        RETURN;
    END;
    IF @OlderThan > SYSUTCDATETIME()
    BEGIN
        RAISERROR('ERROR: @OlderThan is in the future. Refusing: this would report on current or future partitions as retirable.', 16, 1);
        RETURN;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = @partitionFunctionName)
    BEGIN
        RAISERROR('ERROR: Partition function ''%s'' does not exist in this database. This utility only applies to a partitioned MyGeotab API Adapter database.', 16, 1, @partitionFunctionName);
        RETURN;
    END;
    IF EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = @partitionFunctionName AND boundary_value_on_right = 0)
    BEGIN
        RAISERROR('ERROR: Partition function ''%s'' is not RANGE RIGHT. This utility only supports the partition layout created by spManagePartitions.', 16, 1, @partitionFunctionName);
        RETURN;
    END;
    SELECT @partitionFunctionId = function_id FROM sys.partition_functions WHERE name = @partitionFunctionName;
    IF (SELECT COUNT(*) FROM sys.partition_schemes WHERE function_id = @partitionFunctionId) > 1
    BEGIN
        RAISERROR('ERROR: Partition function ''%s'' backs more than one partition scheme. This utility only supports the single-scheme layout created by the adapter - merging boundaries would also affect the other scheme''s tables. Follow the manual procedure in README Section 3.6 (Longer-Term Data Retention Strategy).', 16, 1, @partitionFunctionName);
        RETURN;
    END;

    SELECT TOP (1) @interval = LOWER([InitialPartitionInterval]) FROM [dbo].[DBPartitionInfo2] ORDER BY [id];
    SET @interval = ISNULL(@interval, N'monthly');
    SELECT @firstBoundary = MIN(CONVERT(datetime2(7), prv.value))
    FROM sys.partition_range_values prv
    WHERE prv.function_id = @partitionFunctionId;

    -- ======================================================================================
    -- 1: Configuration.
    SELECT
        N'Configuration' AS [Report],
        DB_NAME() AS [Database],
        @partitionFunctionName AS [PartitionFunction],
        (SELECT fanout FROM sys.partition_functions WHERE name = @partitionFunctionName) AS [PartitionCount],
        @interval AS [PartitionInterval],
        (SELECT TOP (1) [InitialMinDateTimeUTC] FROM [dbo].[DBPartitionInfo2] ORDER BY [id]) AS [InitialMinDateTimeUTC],
        @firstBoundary AS [FirstBoundary],
        @OlderThan AS [OlderThan],
        SYSUTCDATETIME() AS [CurrentUtcTime];

    -- ======================================================================================
    -- 2: Other sessions connected to this database (these block execution; stop the
    --    adapter and close other connections first — README order of operations, step 1).
    IF HAS_PERMS_BY_NAME(NULL, NULL, 'VIEW SERVER STATE') = 1
    BEGIN
        SELECT
            N'Other sessions connected (must be 0 rows to execute)' AS [Report],
            s.session_id, s.login_name, s.host_name, s.program_name, s.status
        FROM sys.dm_exec_sessions s
        WHERE s.is_user_process = 1 AND s.session_id <> @@SPID AND s.database_id = DB_ID();
    END
    ELSE
    BEGIN
        PRINT 'WARNING: VIEW SERVER STATE permission not held - cannot report sessions connected to this database. SwitchOut/Cleanup execution will refuse unless @AcknowledgeSessionCheckUnavailable = 1 is passed.';
    END;

    -- ======================================================================================
    -- Build the period plan shared with SwitchOut/Cleanup. Period suffixes are generated
    -- culture-independently (CONVERT style 112 - session language cannot affect them). A
    -- monthly boundary whose month contains more than one boundary (possible when the
    -- initial layout was created mid-month) deterministically falls back to a day-precision
    -- suffix; the fallback is computed over ALL boundaries of the function so a boundary's
    -- suffix never changes as @OlderThan moves.
    CREATE TABLE #periods (
        [Suffix] nvarchar(30) COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY,
        [IsCatchAll] bit NOT NULL,
        [PeriodStart] datetime2(7) NULL,      -- NULL for the catch-all partition
        [PeriodEnd] datetime2(7) NOT NULL,    -- exclusive upper bound
        [PartitionNumber] int NOT NULL,
        [FilegroupName] sysname COLLATE DATABASE_DEFAULT NOT NULL
    );

    INSERT INTO #periods ([Suffix], [IsCatchAll], [PeriodStart], [PeriodEnd], [PartitionNumber], [FilegroupName])
    SELECT
        CASE
            WHEN @interval = N'monthly' AND b.boundaries_in_month = 1 THEN LEFT(CONVERT(nvarchar(8), b.boundary_value, 112), 6)
            ELSE CONVERT(nvarchar(8), b.boundary_value, 112)
        END,
        0,
        b.boundary_value,
        b.next_boundary_value,
        b.ordinal + 1,
        fg.name
    FROM (
        SELECT
            CONVERT(datetime2(7), prv.value) AS boundary_value,
            CONVERT(datetime2(7), LEAD(prv.value) OVER (ORDER BY CONVERT(datetime2(7), prv.value))) AS next_boundary_value,
            ROW_NUMBER() OVER (ORDER BY CONVERT(datetime2(7), prv.value)) AS ordinal,
            COUNT(*) OVER (PARTITION BY LEFT(CONVERT(nvarchar(8), CONVERT(datetime2(7), prv.value), 112), 6)) AS boundaries_in_month
        FROM sys.partition_range_values prv
        WHERE prv.function_id = @partitionFunctionId
    ) b
    JOIN sys.partition_schemes ps ON ps.name = @partitionSchemeName
    JOIN sys.destination_data_spaces dds ON dds.partition_scheme_id = ps.data_space_id AND dds.destination_id = b.ordinal + 1
    JOIN sys.filegroups fg ON fg.data_space_id = dds.data_space_id
    WHERE b.next_boundary_value IS NOT NULL AND b.next_boundary_value <= @OlderThan;

    IF @firstBoundary <= @OlderThan
    BEGIN
        INSERT INTO #periods ([Suffix], [IsCatchAll], [PeriodStart], [PeriodEnd], [PartitionNumber], [FilegroupName])
        SELECT N'pre_' + CONVERT(nvarchar(8), @firstBoundary, 112), 1, NULL, @firstBoundary, 1, fg.name
        FROM sys.partition_schemes ps
        JOIN sys.destination_data_spaces dds ON dds.partition_scheme_id = ps.data_space_id AND dds.destination_id = 1
        JOIN sys.filegroups fg ON fg.data_space_id = dds.data_space_id
        WHERE ps.name = @partitionSchemeName;
    END;

    -- Rows per partitioned table per partition number.
    CREATE TABLE #tableRows (
        [object_id] int NOT NULL,
        [TableName] sysname COLLATE DATABASE_DEFAULT NOT NULL,
        [PartitionNumber] int NOT NULL,
        [Rows] bigint NOT NULL,
        PRIMARY KEY ([object_id], [PartitionNumber])
    );
    INSERT INTO #tableRows ([object_id], [TableName], [PartitionNumber], [Rows])
    SELECT t.object_id, t.name, p.partition_number, SUM(p.rows)
    FROM sys.tables t
    JOIN sys.indexes i ON i.object_id = t.object_id AND i.index_id IN (0, 1)
    JOIN sys.partition_schemes ps ON ps.data_space_id = i.data_space_id AND ps.name = @partitionSchemeName
    JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id = i.index_id
    GROUP BY t.object_id, t.name, p.partition_number;


    -- ======================================================================================
    -- 3: Retirable periods (entirely older than @OlderThan) and their current state.
    SELECT
        N'Retirable periods' AS [Report],
        pd.[Suffix] AS [Period],
        pd.[PeriodStart], pd.[PeriodEnd], pd.[FilegroupName],
        pd.[PartitionNumber] AS [CurrentPartitionNumber],
        ISNULL(r.[TablesWithRows], 0) AS [TablesWithRows],
        ISNULL(r.[TotalRows], 0) AS [TotalRowsInLiveTables],
        CASE
            WHEN ISNULL(r.[TotalRows], 0) > 0 THEN N'Awaiting SwitchOut'
            WHEN s.[StagingTables] > 0 THEN N'Switched out - staging awaiting archive + Cleanup'
            ELSE N'Empty - Cleanup can merge/remove'
        END AS [Status]
    FROM #periods pd
    OUTER APPLY (
        SELECT COUNT(*) AS [TablesWithRows], SUM(tr.[Rows]) AS [TotalRows]
        FROM #tableRows tr
        WHERE tr.[PartitionNumber] = pd.[PartitionNumber] AND tr.[Rows] > 0
    ) r
    OUTER APPLY (
        SELECT COUNT(*) AS [StagingTables]
        FROM sys.tables st
        WHERE st.name LIKE N'%[_]retired[_]' + pd.[Suffix]
    ) s
    ORDER BY pd.[IsCatchAll] DESC, pd.[PeriodStart];

    -- ======================================================================================
    -- 4: Per-table detail for retirable periods (only tables holding rows).
    SELECT
        N'Per-table rows in retirable periods' AS [Report],
        pd.[Suffix] AS [Period],
        tr.[TableName],
        tr.[Rows],
        pd.[FilegroupName],
        QUOTENAME(tr.[TableName] + N'_retired_' + pd.[Suffix]) AS [StagingTableName]
    FROM #periods pd
    JOIN #tableRows tr ON tr.[PartitionNumber] = pd.[PartitionNumber] AND tr.[Rows] > 0
    ORDER BY pd.[IsCatchAll] DESC, pd.[PeriodStart], tr.[TableName];

    -- ======================================================================================
    -- 5: Non-aligned indexes and constraints on the partitioned tables (SwitchOut drops
    --    and recreates these - README steps 3 and 5), foreign keys it must drop/recreate,
    --    tables it will refuse or skip, and any pending recreations from an interrupted run.
    SELECT
        N'Non-aligned indexes/constraints (dropped/recreated by SwitchOut)' AS [Report],
        t.name AS [TableName],
        i.name AS [ObjectName],
        CASE
            WHEN i.is_primary_key = 1 THEN N'PRIMARY KEY constraint'
            WHEN i.is_unique_constraint = 1 THEN N'UNIQUE constraint'
            ELSE N'Index'
        END AS [ObjectType],
        ds.name AS [DataSpace],
        i.is_unique AS [IsUnique],
        CASE WHEN fk.key_index_id IS NOT NULL THEN 1 ELSE 0 END AS [EnforcesForeignKey]
    FROM sys.tables t
    JOIN sys.indexes ti ON ti.object_id = t.object_id AND ti.index_id IN (0, 1)
    JOIN sys.partition_schemes tps ON tps.data_space_id = ti.data_space_id AND tps.name = @partitionSchemeName
    JOIN sys.indexes i ON i.object_id = t.object_id AND i.index_id > 1 AND i.type = 2
    JOIN sys.data_spaces ds ON ds.data_space_id = i.data_space_id AND ds.type = 'FG'
    OUTER APPLY (
        SELECT TOP (1) fk2.key_index_id
        FROM sys.foreign_keys fk2
        WHERE fk2.referenced_object_id = t.object_id AND fk2.key_index_id = i.index_id
    ) fk
    ORDER BY t.name, i.name;

    SELECT
        N'Foreign keys dropped/recreated WITH CHECK by SwitchOut (referenced table holds retirable rows)' AS [Report],
        fk.name AS [ForeignKeyName],
        OBJECT_NAME(fk.parent_object_id) AS [ReferencingTable],
        OBJECT_NAME(fk.referenced_object_id) AS [ReferencedTable]
    FROM sys.foreign_keys fk
    WHERE fk.referenced_object_id IN (
        SELECT tr.[object_id]
        FROM #tableRows tr
        JOIN #periods pd ON pd.[PartitionNumber] = tr.[PartitionNumber]
        WHERE tr.[Rows] > 0
    )
    ORDER BY fk.name;

    SELECT
        N'Tables with CDC / change tracking / replication (SwitchOut will refuse these)' AS [Report],
        t.name AS [TableName],
        t.is_tracked_by_cdc AS [CDC],
        CASE WHEN ctt.object_id IS NOT NULL THEN 1 ELSE 0 END AS [ChangeTracking],
        t.is_replicated AS [Replicated],
        t.is_published AS [Published],
        t.is_merge_published AS [MergePublished]
    FROM sys.tables t
    JOIN sys.indexes ti ON ti.object_id = t.object_id AND ti.index_id IN (0, 1)
    JOIN sys.partition_schemes tps ON tps.data_space_id = ti.data_space_id AND tps.name = @partitionSchemeName
    LEFT JOIN sys.change_tracking_tables ctt ON ctt.object_id = t.object_id
    WHERE t.is_tracked_by_cdc = 1 OR t.is_replicated = 1 OR t.is_published = 1 OR t.is_merge_published = 1 OR ctt.object_id IS NOT NULL
    ORDER BY t.name;

    SELECT
        N'PENDING recreations from an interrupted run (SwitchOut will restore these)' AS [Report],
        t.name AS [TableName],
        ep.name AS [MarkerName],
        CONVERT(nvarchar(4000), ep.value) AS [SavedCreateStatement]
    FROM sys.extended_properties ep
    JOIN sys.tables t ON t.object_id = ep.major_id
    WHERE ep.class = 1 AND ep.minor_id = 0 AND ep.name LIKE N'PartitionRetirement[_]%';

    -- ======================================================================================
    -- 6: Staging tables currently present (awaiting archive + Cleanup). Built with the
    --    same base-table join and suffix parse as Cleanup's candidate list, so this report
    --    and Cleanup's decisions can never disagree; anything '_retired_'-like that does
    --    NOT parse is listed separately as unrecognized.
    SELECT
        N'Staging tables present (archive if needed, then run Cleanup)' AS [Report],
        st.name AS [StagingTableName],
        (
            SELECT SUM(p.rows) FROM sys.partitions p
            JOIN sys.indexes i ON i.object_id = p.object_id AND i.index_id = p.index_id
            WHERE p.object_id = st.object_id AND i.index_id IN (0, 1)
        ) AS [Rows],
        ds.name AS [Filegroup],
        x.[PeriodEnd] AS [ParsedPeriodEnd],
        CASE WHEN x.[PeriodEnd] <= @OlderThan THEN N'Cleanup candidate at this @OlderThan' ELSE N'Not a candidate at this @OlderThan' END AS [CleanupStatus]
    FROM sys.tables st
    JOIN sys.tables bt
        ON st.name LIKE bt.name + N'[_]retired[_]%' AND st.schema_id = bt.schema_id
    JOIN sys.indexes bi ON bi.object_id = bt.object_id AND bi.index_id IN (0, 1)
    JOIN sys.partition_schemes bps ON bps.data_space_id = bi.data_space_id AND bps.name = @partitionSchemeName
    JOIN sys.indexes sti ON sti.object_id = st.object_id AND sti.index_id IN (0, 1)
    JOIN sys.data_spaces ds ON ds.data_space_id = sti.data_space_id
    CROSS APPLY (SELECT SUBSTRING(st.name, LEN(bt.name) + LEN(N'_retired_') + 1, 30) AS [Sfx]) sfx
    CROSS APPLY (
        SELECT CASE
            WHEN sfx.[Sfx] LIKE N'pre[_][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
                THEN TRY_CONVERT(datetime2(7), CONCAT(SUBSTRING(sfx.[Sfx], 5, 4), N'-', SUBSTRING(sfx.[Sfx], 9, 2), N'-', SUBSTRING(sfx.[Sfx], 11, 2)))
            WHEN sfx.[Sfx] LIKE N'[0-9][0-9][0-9][0-9][0-9][0-9]' AND @interval = N'monthly'
                THEN DATEADD(MONTH, 1, TRY_CONVERT(datetime2(7), CONCAT(LEFT(sfx.[Sfx], 4), N'-', RIGHT(sfx.[Sfx], 2), N'-01')))
            WHEN sfx.[Sfx] LIKE N'[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' AND @interval = N'monthly'
                THEN DATEADD(MONTH, 1, TRY_CONVERT(datetime2(7), CONCAT(LEFT(sfx.[Sfx], 4), N'-', SUBSTRING(sfx.[Sfx], 5, 2), N'-', RIGHT(sfx.[Sfx], 2))))
            WHEN sfx.[Sfx] LIKE N'[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' AND @interval = N'weekly'
                THEN DATEADD(WEEK, 1, TRY_CONVERT(datetime2(7), CONCAT(LEFT(sfx.[Sfx], 4), N'-', SUBSTRING(sfx.[Sfx], 5, 2), N'-', RIGHT(sfx.[Sfx], 2))))
            WHEN sfx.[Sfx] LIKE N'[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' AND @interval = N'daily'
                THEN DATEADD(DAY, 1, TRY_CONVERT(datetime2(7), CONCAT(LEFT(sfx.[Sfx], 4), N'-', SUBSTRING(sfx.[Sfx], 5, 2), N'-', RIGHT(sfx.[Sfx], 2))))
            ELSE NULL
        END AS [PeriodEnd]
    ) x
    WHERE x.[PeriodEnd] IS NOT NULL
    ORDER BY st.name;

    SELECT
        N'UNRECOGNIZED ''_retired_''-pattern tables (Cleanup will NOT touch these)' AS [Report],
        st.name AS [TableName]
    FROM sys.tables st
    WHERE st.name LIKE N'%[_]retired[_]%'
      AND NOT EXISTS (
          SELECT 1
          FROM sys.tables bt
          JOIN sys.indexes bi ON bi.object_id = bt.object_id AND bi.index_id IN (0, 1)
          JOIN sys.partition_schemes bps ON bps.data_space_id = bi.data_space_id AND bps.name = @partitionSchemeName
          CROSS APPLY (SELECT SUBSTRING(st.name, LEN(bt.name) + LEN(N'_retired_') + 1, 30) AS [Sfx]) sfx
          WHERE st.name LIKE bt.name + N'[_]retired[_]%' AND st.schema_id = bt.schema_id
            AND (
                sfx.[Sfx] LIKE N'pre[_][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
                OR sfx.[Sfx] LIKE N'[0-9][0-9][0-9][0-9][0-9][0-9]'
                OR sfx.[Sfx] LIKE N'[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
            )
      )
    ORDER BY st.name;

    -- ======================================================================================
    -- 7: Filegroups already unmapped and unallocated (Cleanup will remove these). Scoped
    --    to filegroups created by spManagePartitions: the name pattern AND a file whose
    --    logical name is '<FilegroupName>_DataFile' - never true for customer filegroups.
    --    Emptiness is judged on allocation units (covers LOB-only allocation), not just
    --    sys.indexes.
    SELECT
        N'Filegroups removable by Cleanup (unmapped and empty)' AS [Report],
        fg.name AS [FilegroupName],
        f.name AS [LogicalFileName],
        f.physical_name AS [PhysicalFileName]
    FROM sys.filegroups fg
    LEFT JOIN sys.database_files f ON f.data_space_id = fg.data_space_id
    WHERE fg.name LIKE N'FG[_]%'
      AND fg.name LIKE N'%[0-9][0-9][0-9][0-9][0-9][0-9]'
      AND fg.is_default = 0
      AND EXISTS (SELECT 1 FROM sys.database_files fa WHERE fa.data_space_id = fg.data_space_id AND fa.name = fg.name + N'_DataFile')
      AND NOT EXISTS (SELECT 1 FROM sys.destination_data_spaces dds WHERE dds.data_space_id = fg.data_space_id)
      AND NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.data_space_id = fg.data_space_id)
      AND NOT EXISTS (SELECT 1 FROM sys.allocation_units au WHERE au.data_space_id = fg.data_space_id AND au.total_pages > 0)
    ORDER BY fg.name;

    DROP TABLE #periods;
    DROP TABLE #tableRows;

    PRINT 'Preview complete. This procedure made no changes.';
    PRINT 'Next: run [spAdapterPartitionRetirement_SwitchOut] @OlderThan = ''<date>'' to see the exact statements (add @Execute = 1 to perform them).';
END;
GO


-- ================================================================================
-- spAdapterPartitionRetirement_SwitchOut
--   README steps 2-5: capture + drop whatever blocks the switches (foreign keys,
--   non-aligned constraints, non-aligned indexes), create staging tables cloning
--   each table's actual clustered structure, SWITCH the partitions out, then
--   recreate everything that was dropped (foreign keys WITH CHECK, last). Never
--   deletes data: switched-out rows remain in the database in
--   [<Table>_retired_<period>] staging tables.
--
--   Order of work (all definitions are captured and validated BEFORE the first
--   drop, and every dropped object leaves a marker until it is recreated):
--     Phase 0 - pre-flight guards (refuse or skip-and-report; nothing modified).
--     Phase 1 - capture definitions + write markers:
--                 'PartitionRetirement_Fk_<name>'  (on the referencing table)
--                 'PartitionRetirement_Cns_<name>' (PRIMARY KEY / UNIQUE constraint)
--                 'PartitionRetirement_Idx_<name>' (plain nonclustered index)
--     Phase 2 - drop foreign keys, then constraints, then indexes.
--     Phase 3 - per table + period: create staging table, SWITCH partition out.
--     Phase 4 - recreate constraints and indexes, then foreign keys WITH CHECK;
--               remove each marker as its object is restored. This phase also
--               restores objects left missing by an interrupted earlier run.
-- ================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spAdapterPartitionRetirement_SwitchOut]
    @OlderThan datetime2(7),
    @Execute bit = 0,
    @AcknowledgeSessionCheckUnavailable bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @partitionFunctionName sysname = N'DateTimePartitionFunction_MyGeotabApiAdapter';
    DECLARE @partitionSchemeName sysname = N'DateTimePartitionScheme_MyGeotabApiAdapter';
    DECLARE @partitionFunctionId int;
    DECLARE @interval nvarchar(50);
    DECLARE @firstBoundary datetime2(7);
    DECLARE @retireBefore datetime2(7);
    DECLARE @msg nvarchar(4000);
    DECLARE @sql nvarchar(max);
    DECLARE @sessionListFull nvarchar(max);
    DECLARE @sessionList nvarchar(2000);

    BEGIN TRY
        -- ======================================================================================
        -- Validate.
        IF @OlderThan IS NULL
        BEGIN
            RAISERROR('ERROR: @OlderThan is required. Example: EXEC [dbo].[spAdapterPartitionRetirement_SwitchOut] @OlderThan = ''2023-01-01'';', 16, 1);
            RETURN;
        END;
        IF @OlderThan > SYSUTCDATETIME()
        BEGIN
            RAISERROR('ERROR: @OlderThan is in the future. Refusing: this would retire current or future partitions.', 16, 1);
            RETURN;
        END;
        IF NOT EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = @partitionFunctionName)
        BEGIN
            RAISERROR('ERROR: Partition function ''%s'' does not exist in this database. This utility only applies to a partitioned MyGeotab API Adapter database.', 16, 1, @partitionFunctionName);
            RETURN;
        END;
        IF EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = @partitionFunctionName AND boundary_value_on_right = 0)
        BEGIN
            RAISERROR('ERROR: Partition function ''%s'' is not RANGE RIGHT. This utility only supports the partition layout created by spManagePartitions.', 16, 1, @partitionFunctionName);
            RETURN;
        END;
        SELECT @partitionFunctionId = function_id FROM sys.partition_functions WHERE name = @partitionFunctionName;
        IF (SELECT COUNT(*) FROM sys.partition_schemes WHERE function_id = @partitionFunctionId) > 1
        BEGIN
            RAISERROR('ERROR: Partition function ''%s'' backs more than one partition scheme. This utility only supports the single-scheme layout created by the adapter - merging boundaries would also affect the other scheme''s tables. Follow the manual procedure in README Section 3.6 (Longer-Term Data Retention Strategy).', 16, 1, @partitionFunctionName);
            RETURN;
        END;

        IF @Execute = 1
        BEGIN
            PRINT '*** EXECUTE MODE: the statements below are being executed. ***';
        END
        ELSE
        BEGIN
            PRINT '*** DRY RUN: no changes are made. The statements below are what @Execute = 1 would run. ***';
        END;

        SELECT TOP (1) @interval = LOWER([InitialPartitionInterval]) FROM [dbo].[DBPartitionInfo2] ORDER BY [id];
        SET @interval = ISNULL(@interval, N'monthly');
        SELECT @firstBoundary = MIN(CONVERT(datetime2(7), prv.value))
        FROM sys.partition_range_values prv
        WHERE prv.function_id = @partitionFunctionId;

        -- ======================================================================================
        -- Session gate (README order of operations, step 1: stop the adapter). Only enforced
        -- when executing - a dry run is read-only and safe at any time. If the check cannot
        -- be performed, execution refuses unless explicitly acknowledged: note that db_owner
        -- alone does NOT include VIEW SERVER STATE.
        IF @Execute = 1
        BEGIN
            IF HAS_PERMS_BY_NAME(NULL, NULL, 'VIEW SERVER STATE') = 0
            BEGIN
                IF @AcknowledgeSessionCheckUnavailable = 0
                BEGIN
                    RAISERROR('ERROR: VIEW SERVER STATE permission not held - cannot verify that no other sessions are connected. Either grant VIEW SERVER STATE to this login, or - after ensuring the adapter is stopped and nothing else is connected (README Section 3.6, Longer-Term Data Retention Strategy, order of operations step 1) - re-run with @AcknowledgeSessionCheckUnavailable = 1.', 16, 1);
                    RETURN;
                END;
                PRINT 'WARNING: VIEW SERVER STATE permission not held - proceeding WITHOUT the connected-session check on your @AcknowledgeSessionCheckUnavailable = 1 acknowledgement. You are responsible for ensuring the adapter is stopped and no other sessions are connected.';
            END
            ELSE
            BEGIN
                SET @sessionListFull = NULL;
                SELECT @sessionListFull = STRING_AGG(CONVERT(nvarchar(max), CONCAT(N'session_id=', s.session_id, N' login=', s.login_name, N' host=', s.host_name, N' program=', LEFT(s.program_name, 60))), N' | ')
                FROM sys.dm_exec_sessions s
                WHERE s.is_user_process = 1 AND s.session_id <> @@SPID AND s.database_id = DB_ID();
                IF @sessionListFull IS NOT NULL
                BEGIN
                    SET @sessionList = LEFT(@sessionListFull, 1800);
                    RAISERROR('ERROR: Other sessions are connected to this database: %s. Stop the adapter and close other connections first (README Section 3.6, Longer-Term Data Retention Strategy, order of operations step 1).', 16, 1, @sessionList);
                    RETURN;
                END;
            END;
        END;

        -- ======================================================================================
        -- Build the work plan. Suffix generation is culture-independent with a deterministic
        -- day-precision fallback for monthly boundaries sharing a month - see the identical
        -- logic in Preview for details.
        CREATE TABLE #periods (
            [Suffix] nvarchar(30) COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY,
            [IsCatchAll] bit NOT NULL,
            [PeriodStart] datetime2(7) NULL,
            [PeriodEnd] datetime2(7) NOT NULL,
            [PartitionNumber] int NOT NULL,
            [FilegroupName] sysname COLLATE DATABASE_DEFAULT NOT NULL
        );
        INSERT INTO #periods ([Suffix], [IsCatchAll], [PeriodStart], [PeriodEnd], [PartitionNumber], [FilegroupName])
        SELECT
            CASE
                WHEN @interval = N'monthly' AND b.boundaries_in_month = 1 THEN LEFT(CONVERT(nvarchar(8), b.boundary_value, 112), 6)
                ELSE CONVERT(nvarchar(8), b.boundary_value, 112)
            END,
            0, b.boundary_value, b.next_boundary_value, b.ordinal + 1, fg.name
        FROM (
            SELECT
                CONVERT(datetime2(7), prv.value) AS boundary_value,
                CONVERT(datetime2(7), LEAD(prv.value) OVER (ORDER BY CONVERT(datetime2(7), prv.value))) AS next_boundary_value,
                ROW_NUMBER() OVER (ORDER BY CONVERT(datetime2(7), prv.value)) AS ordinal,
                COUNT(*) OVER (PARTITION BY LEFT(CONVERT(nvarchar(8), CONVERT(datetime2(7), prv.value), 112), 6)) AS boundaries_in_month
            FROM sys.partition_range_values prv
            WHERE prv.function_id = @partitionFunctionId
        ) b
        JOIN sys.partition_schemes ps ON ps.name = @partitionSchemeName
        JOIN sys.destination_data_spaces dds ON dds.partition_scheme_id = ps.data_space_id AND dds.destination_id = b.ordinal + 1
        JOIN sys.filegroups fg ON fg.data_space_id = dds.data_space_id
        WHERE b.next_boundary_value IS NOT NULL AND b.next_boundary_value <= @OlderThan;

        IF @firstBoundary <= @OlderThan
        BEGIN
            INSERT INTO #periods ([Suffix], [IsCatchAll], [PeriodStart], [PeriodEnd], [PartitionNumber], [FilegroupName])
            SELECT N'pre_' + CONVERT(nvarchar(8), @firstBoundary, 112), 1, NULL, @firstBoundary, 1, fg.name
            FROM sys.partition_schemes ps
            JOIN sys.destination_data_spaces dds ON dds.partition_scheme_id = ps.data_space_id AND dds.destination_id = 1
            JOIN sys.filegroups fg ON fg.data_space_id = dds.data_space_id
            WHERE ps.name = @partitionSchemeName;
        END;

        -- Exclusive upper bound of everything this run retires: every row with a
        -- partitioning-column value below this leaves the live tables (the periods are
        -- contiguous), and every row at or above it stays.
        SELECT @retireBefore = MAX([PeriodEnd]) FROM #periods;


        -- Work items: one row per (table, period) where the live partition holds rows.
        CREATE TABLE #work (
            [WorkId] int IDENTITY(1, 1) PRIMARY KEY,
            [object_id] int NOT NULL,
            [SchemaName] sysname COLLATE DATABASE_DEFAULT NOT NULL,
            [TableName] sysname COLLATE DATABASE_DEFAULT NOT NULL,
            [Suffix] nvarchar(30) COLLATE DATABASE_DEFAULT NOT NULL,
            [IsCatchAll] bit NOT NULL,
            [PeriodStart] datetime2(7) NULL,
            [FilegroupName] sysname COLLATE DATABASE_DEFAULT NOT NULL,
            [Rows] bigint NOT NULL,
            [StagingName] sysname COLLATE DATABASE_DEFAULT NOT NULL,
            [StagingRows] bigint NULL          -- NULL = staging table does not exist
        );
        INSERT INTO #work ([object_id], [SchemaName], [TableName], [Suffix], [IsCatchAll], [PeriodStart], [FilegroupName], [Rows], [StagingName], [StagingRows])
        SELECT
            t.object_id, SCHEMA_NAME(t.schema_id), t.name, pd.[Suffix], pd.[IsCatchAll], pd.[PeriodStart], pd.[FilegroupName],
            SUM(p.rows),
            t.name + N'_retired_' + pd.[Suffix],
            (
                SELECT SUM(sp.rows)
                FROM sys.tables st
                JOIN sys.indexes si ON si.object_id = st.object_id AND si.index_id IN (0, 1)
                JOIN sys.partitions sp ON sp.object_id = st.object_id AND sp.index_id = si.index_id
                WHERE st.schema_id = t.schema_id AND st.name = t.name + N'_retired_' + pd.[Suffix]
            )
        FROM #periods pd
        JOIN sys.partition_schemes ps ON ps.name = @partitionSchemeName
        JOIN sys.indexes i ON i.data_space_id = ps.data_space_id AND i.index_id IN (0, 1)
        JOIN sys.tables t ON t.object_id = i.object_id
        JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id = i.index_id AND p.partition_number = pd.[PartitionNumber]
        GROUP BY t.object_id, t.schema_id, t.name, pd.[Suffix], pd.[IsCatchAll], pd.[PeriodStart], pd.[FilegroupName]
        HAVING SUM(p.rows) > 0;

        -- Tables excluded from this run, with the reason - reported in the summary. Their
        -- rows stay live, so the affected periods' boundary merges stay safely blocked in
        -- Cleanup until the cause is resolved.
        CREATE TABLE #skips (
            [object_id] int NOT NULL PRIMARY KEY,
            [TableName] sysname COLLATE DATABASE_DEFAULT NOT NULL,
            [Reason] nvarchar(2000) COLLATE DATABASE_DEFAULT NOT NULL
        );

        -- ======================================================================================
        -- Phase 0 - pre-flight guards. Nothing is modified in this phase.

        -- Impossible state guard: staging already holds rows while the live partition also
        -- holds rows. A completed SWITCH empties the partition, so this cannot result from
        -- an interrupted run - investigate manually before proceeding.
        IF EXISTS (SELECT 1 FROM #work WHERE [StagingRows] > 0)
        BEGIN
            SELECT @msg = STRING_AGG(CONVERT(nvarchar(200), QUOTENAME([StagingName])), N', ') FROM #work WHERE [StagingRows] > 0;
            RAISERROR('ERROR: Staging table(s) %s already contain rows while the corresponding live partition also contains rows. This state cannot result from an interrupted run of this utility - investigate manually (README Section 3.6, Longer-Term Data Retention Strategy, step 2).', 16, 1, @msg);
            RETURN;
        END;

        -- CDC / change tracking / replication make SWITCH (or the index drops it needs)
        -- fail or misbehave partway through - refuse up front, before anything is dropped.
        SET @msg = NULL;
        SELECT @msg = STRING_AGG(CONVERT(nvarchar(200), t.name), N', ')
        FROM (SELECT DISTINCT [object_id] FROM #work) w
        JOIN sys.tables t ON t.object_id = w.[object_id]
        LEFT JOIN sys.change_tracking_tables ctt ON ctt.object_id = t.object_id
        WHERE t.is_tracked_by_cdc = 1 OR t.is_replicated = 1 OR t.is_published = 1 OR t.is_merge_published = 1 OR ctt.object_id IS NOT NULL;
        IF @msg IS NOT NULL
        BEGIN
            RAISERROR('ERROR: Table(s) %s are enabled for CDC, change tracking or replication, which prevents partition switching. Disable these features for the affected tables (or follow the manual procedure in README Section 3.6, Longer-Term Data Retention Strategy) and re-run.', 16, 1, @msg);
            RETURN;
        END;

        -- Existing-but-empty staging tables are reused by the switch phase, so their column
        -- shape must still match the live table exactly (a mismatch would fail the SWITCH -
        -- typically after schema changes between runs). Refuse with instructions.
        SET @msg = NULL;
        SELECT @msg = STRING_AGG(CONVERT(nvarchar(200), QUOTENAME(w.[StagingName])), N', ')
        FROM (SELECT DISTINCT [object_id], [SchemaName], [StagingName] FROM #work WHERE [StagingRows] = 0) w
        CROSS APPLY (SELECT st.object_id AS staging_object_id FROM sys.tables st WHERE st.name = w.[StagingName] AND st.schema_id = SCHEMA_ID(w.[SchemaName])) s
        WHERE EXISTS (
            SELECT c1.column_id, c1.name, c1.user_type_id, c1.max_length, c1.precision, c1.scale, c1.is_nullable, c1.collation_name
            FROM sys.columns c1 WHERE c1.object_id = w.[object_id]
            EXCEPT
            SELECT c2.column_id, c2.name, c2.user_type_id, c2.max_length, c2.precision, c2.scale, c2.is_nullable, c2.collation_name
            FROM sys.columns c2 WHERE c2.object_id = s.staging_object_id
        ) OR EXISTS (
            SELECT c2.column_id, c2.name, c2.user_type_id, c2.max_length, c2.precision, c2.scale, c2.is_nullable, c2.collation_name
            FROM sys.columns c2 WHERE c2.object_id = s.staging_object_id
            EXCEPT
            SELECT c1.column_id, c1.name, c1.user_type_id, c1.max_length, c1.precision, c1.scale, c1.is_nullable, c1.collation_name
            FROM sys.columns c1 WHERE c1.object_id = w.[object_id]
        );
        IF @msg IS NOT NULL
        BEGIN
            RAISERROR('ERROR: Existing empty staging table(s) %s no longer match their live table''s column shape (the schema likely changed since an earlier interrupted run). Drop the stale staging table(s) and re-run.', 16, 1, @msg);
            RETURN;
        END;

        -- Per-table skips: computed columns (the staging clone cannot reproduce them) and
        -- heaps (no clustered structure to clone). The rest of the run proceeds without
        -- these tables.
        INSERT INTO #skips ([object_id], [TableName], [Reason])
        SELECT DISTINCT w.[object_id], w.[TableName],
            N'Table has computed column(s), which the staging-table clone does not support. Follow the manual procedure in README Section 3.6 (Longer-Term Data Retention Strategy) for this table.'
        FROM #work w
        WHERE EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = w.[object_id] AND c.is_computed = 1);

        INSERT INTO #skips ([object_id], [TableName], [Reason])
        SELECT DISTINCT w.[object_id], w.[TableName],
            N'Table has no clustered index (heap), which the staging-table clone does not support. Follow the manual procedure in README Section 3.6 (Longer-Term Data Retention Strategy) for this table.'
        FROM #work w
        WHERE NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id = w.[object_id] AND i.index_id = 1)
          AND NOT EXISTS (SELECT 1 FROM #skips sk WHERE sk.[object_id] = w.[object_id]);

        -- Cross-period foreign-key reference check: a row that STAYS live must never
        -- reference a row that this run would switch out (recreating the foreign key WITH
        -- CHECK would fail). When that would happen, the REFERENCED table is skipped (its
        -- foreign keys are then left in place, which also blocks its switches - Msg 4967 -
        -- so it must be excluded up front). A skipped or absent-from-this-run referencing
        -- table keeps ALL its rows live, so the check then covers its entire row set; skips
        -- can therefore cascade along foreign-key chains until stable.
        DECLARE @skipAdded bit = 1, @fkObjId int, @fkName sysname, @childObjId int, @parentObjId int,
                @childSchema sysname, @childTable sysname, @parentSchema sysname, @parentTable sysname,
                @joinPred nvarchar(max), @childPartCol sysname, @parentPartCol sysname,
                @childHasWork bit, @childSkipped bit, @violates bit;
        WHILE @skipAdded = 1
        BEGIN
            SET @skipAdded = 0;
            DECLARE fkCheckCursor CURSOR LOCAL FAST_FORWARD FOR
                SELECT fk.object_id, fk.name, fk.parent_object_id, fk.referenced_object_id,
                       SCHEMA_NAME(ct.schema_id), ct.name, SCHEMA_NAME(pt.schema_id), pt.name
                FROM sys.foreign_keys fk
                JOIN sys.tables ct ON ct.object_id = fk.parent_object_id
                JOIN sys.tables pt ON pt.object_id = fk.referenced_object_id
                WHERE fk.is_disabled = 0
                  AND fk.referenced_object_id IN (SELECT DISTINCT [object_id] FROM #work)
                  AND fk.referenced_object_id NOT IN (SELECT [object_id] FROM #skips);
            OPEN fkCheckCursor;
            FETCH NEXT FROM fkCheckCursor INTO @fkObjId, @fkName, @childObjId, @parentObjId, @childSchema, @childTable, @parentSchema, @parentTable;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                SELECT @joinPred = STRING_AGG(CONVERT(nvarchar(max), CONCAT(N'c.', QUOTENAME(cc.name), N' = p.', QUOTENAME(pc.name))), N' AND ')
                FROM sys.foreign_key_columns fkc
                JOIN sys.columns cc ON cc.object_id = fkc.parent_object_id AND cc.column_id = fkc.parent_column_id
                JOIN sys.columns pc ON pc.object_id = fkc.referenced_object_id AND pc.column_id = fkc.referenced_column_id
                WHERE fkc.constraint_object_id = @fkObjId;

                SELECT @parentPartCol = c.name
                FROM sys.index_columns ic
                JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                WHERE ic.object_id = @parentObjId AND ic.index_id IN (0, 1) AND ic.partition_ordinal = 1;

                SET @childPartCol = NULL;
                SELECT @childPartCol = c.name
                FROM sys.indexes i
                JOIN sys.partition_schemes ps ON ps.data_space_id = i.data_space_id AND ps.name = @partitionSchemeName
                JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.partition_ordinal = 1
                JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                WHERE i.object_id = @childObjId AND i.index_id IN (0, 1);

                SET @childSkipped = CASE WHEN EXISTS (SELECT 1 FROM #skips sk WHERE sk.[object_id] = @childObjId) THEN 1 ELSE 0 END;

                -- Rows of the referencing table that will still be live after this run:
                -- everything at/after the cutoff, or ALL rows when the referencing table is
                -- not partitioned on the adapter scheme or is itself skipped.
                SET @sql = CONCAT(
                    N'SELECT @v = CASE WHEN EXISTS (SELECT 1 FROM ', QUOTENAME(@childSchema), N'.', QUOTENAME(@childTable), N' c JOIN ',
                    QUOTENAME(@parentSchema), N'.', QUOTENAME(@parentTable), N' p ON ', @joinPred,
                    N' WHERE p.', QUOTENAME(@parentPartCol), N' < @cutoff',
                    CASE WHEN @childPartCol IS NOT NULL AND @childSkipped = 0 THEN CONCAT(N' AND c.', QUOTENAME(@childPartCol), N' >= @cutoff') ELSE N'' END,
                    N') THEN 1 ELSE 0 END;');
                EXEC sp_executesql @sql, N'@cutoff datetime2(7), @v bit OUTPUT', @cutoff = @retireBefore, @v = @violates OUTPUT;

                IF @violates = 1
                BEGIN
                    INSERT INTO #skips ([object_id], [TableName], [Reason])
                    SELECT @parentObjId, @parentTable,
                        CONCAT(N'Rows in ', QUOTENAME(@childTable), N' that would remain live reference rows this run would switch out (foreign key ', QUOTENAME(@fkName),
                               N'). Recreating the foreign key WITH CHECK would fail, so this table is skipped. Re-run later once those references age past the cutoff, or handle this table via the manual procedure in README Section 3.6 (Longer-Term Data Retention Strategy).');
                    SET @skipAdded = 1;
                END;
                FETCH NEXT FROM fkCheckCursor INTO @fkObjId, @fkName, @childObjId, @parentObjId, @childSchema, @childTable, @parentSchema, @parentTable;
            END;
            CLOSE fkCheckCursor;
            DEALLOCATE fkCheckCursor;
        END;

        -- Apply the skips to the work plan and tell the operator.
        DELETE w FROM #work w WHERE EXISTS (SELECT 1 FROM #skips sk WHERE sk.[object_id] = w.[object_id]);
        IF EXISTS (SELECT 1 FROM #skips)
        BEGIN
            DECLARE @skipTbl sysname, @skipReason nvarchar(2000);
            DECLARE skipCursor CURSOR LOCAL FAST_FORWARD FOR SELECT [TableName], [Reason] FROM #skips ORDER BY [TableName];
            OPEN skipCursor;
            FETCH NEXT FROM skipCursor INTO @skipTbl, @skipReason;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                PRINT CONCAT(N'-- SKIPPING table ', QUOTENAME(@skipTbl), N': ', @skipReason);
                FETCH NEXT FROM skipCursor INTO @skipTbl, @skipReason;
            END;
            CLOSE skipCursor;
            DEALLOCATE skipCursor;
        END;


        -- ======================================================================================
        -- Phase 1 - capture the full definition of everything that must be dropped, and
        -- validate ALL of it BEFORE the first drop. Classes (also the drop order):
        --   1 = foreign keys referencing a table being switched (marker on the referencing
        --       table; recreated WITH CHECK - or WITH NOCHECK if they were untrusted - LAST,
        --       after the indexes/constraints they depend on are back);
        --   2 = non-aligned PRIMARY KEY / UNIQUE constraints (ALTER TABLE DROP/ADD CONSTRAINT);
        --   3 = non-aligned nonclustered indexes (DROP INDEX / CREATE INDEX).
        -- Recreate statements preserve key order, INCLUDE columns, filters, index options,
        -- FILLFACTOR and DATA_COMPRESSION.
        CREATE TABLE #captures (
            [CaptureId] int IDENTITY(1, 1) PRIMARY KEY,
            [Class] tinyint NOT NULL,
            [object_id] int NOT NULL,
            [SchemaName] sysname COLLATE DATABASE_DEFAULT NOT NULL,
            [TableName] sysname COLLATE DATABASE_DEFAULT NOT NULL,
            [ObjectName] sysname COLLATE DATABASE_DEFAULT NOT NULL,
            [MarkerName] nvarchar(300) COLLATE DATABASE_DEFAULT NOT NULL,
            [DropStmt] nvarchar(max) COLLATE DATABASE_DEFAULT NOT NULL,
            [CreateStmt] nvarchar(max) COLLATE DATABASE_DEFAULT NOT NULL
        );

        -- Class 1: enabled foreign keys whose referenced table is being switched. (Disabled
        -- foreign keys do not block SWITCH and are left untouched.)
        INSERT INTO #captures ([Class], [object_id], [SchemaName], [TableName], [ObjectName], [MarkerName], [DropStmt], [CreateStmt])
        SELECT
            1,
            fk.parent_object_id,
            SCHEMA_NAME(ct.schema_id),
            ct.name,
            fk.name,
            N'PartitionRetirement_Fk_' + fk.name,
            CONCAT(N'ALTER TABLE ', QUOTENAME(SCHEMA_NAME(ct.schema_id)), N'.', QUOTENAME(ct.name), N' DROP CONSTRAINT ', QUOTENAME(fk.name), N';'),
            CONCAT(
                CONVERT(nvarchar(max), N'ALTER TABLE '), QUOTENAME(SCHEMA_NAME(ct.schema_id)), N'.', QUOTENAME(ct.name),
                CASE WHEN fk.is_not_trusted = 1 THEN N' WITH NOCHECK ADD CONSTRAINT ' ELSE N' WITH CHECK ADD CONSTRAINT ' END,
                QUOTENAME(fk.name), N' FOREIGN KEY (',
                (
                    SELECT STRING_AGG(CONVERT(nvarchar(300), QUOTENAME(cc.name)), N', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id)
                    FROM sys.foreign_key_columns fkc
                    JOIN sys.columns cc ON cc.object_id = fkc.parent_object_id AND cc.column_id = fkc.parent_column_id
                    WHERE fkc.constraint_object_id = fk.object_id
                ),
                N') REFERENCES ', QUOTENAME(SCHEMA_NAME(pt.schema_id)), N'.', QUOTENAME(pt.name), N' (',
                (
                    SELECT STRING_AGG(CONVERT(nvarchar(300), QUOTENAME(pc.name)), N', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id)
                    FROM sys.foreign_key_columns fkc
                    JOIN sys.columns pc ON pc.object_id = fkc.referenced_object_id AND pc.column_id = fkc.referenced_column_id
                    WHERE fkc.constraint_object_id = fk.object_id
                ),
                N')',
                CASE fk.delete_referential_action WHEN 1 THEN N' ON DELETE CASCADE' WHEN 2 THEN N' ON DELETE SET NULL' WHEN 3 THEN N' ON DELETE SET DEFAULT' ELSE N'' END,
                CASE fk.update_referential_action WHEN 1 THEN N' ON UPDATE CASCADE' WHEN 2 THEN N' ON UPDATE SET NULL' WHEN 3 THEN N' ON UPDATE SET DEFAULT' ELSE N'' END,
                CASE WHEN fk.is_not_for_replication = 1 THEN N' NOT FOR REPLICATION' ELSE N'' END,
                N';'
            )
        FROM sys.foreign_keys fk
        JOIN sys.tables ct ON ct.object_id = fk.parent_object_id
        JOIN sys.tables pt ON pt.object_id = fk.referenced_object_id
        WHERE fk.is_disabled = 0
          AND fk.referenced_object_id IN (SELECT DISTINCT [object_id] FROM #work);

        -- Classes 2 + 3: non-aligned constraints and indexes on the tables being switched.
        INSERT INTO #captures ([Class], [object_id], [SchemaName], [TableName], [ObjectName], [MarkerName], [DropStmt], [CreateStmt])
        SELECT
            CASE WHEN i.is_primary_key = 1 OR i.is_unique_constraint = 1 THEN 2 ELSE 3 END,
            i.object_id,
            SCHEMA_NAME(t.schema_id),
            t.name,
            i.name,
            CASE WHEN i.is_primary_key = 1 OR i.is_unique_constraint = 1 THEN N'PartitionRetirement_Cns_' ELSE N'PartitionRetirement_Idx_' END + i.name,
            CASE
                WHEN i.is_primary_key = 1 OR i.is_unique_constraint = 1
                    THEN CONCAT(N'ALTER TABLE ', QUOTENAME(SCHEMA_NAME(t.schema_id)), N'.', QUOTENAME(t.name), N' DROP CONSTRAINT ', QUOTENAME(i.name), N';')
                ELSE CONCAT(N'DROP INDEX ', QUOTENAME(i.name), N' ON ', QUOTENAME(SCHEMA_NAME(t.schema_id)), N'.', QUOTENAME(t.name), N';')
            END,
            CONCAT(
                CASE
                    WHEN i.is_primary_key = 1 THEN CONVERT(nvarchar(max), CONCAT(N'ALTER TABLE ', QUOTENAME(SCHEMA_NAME(t.schema_id)), N'.', QUOTENAME(t.name), N' ADD CONSTRAINT ', QUOTENAME(i.name), N' PRIMARY KEY NONCLUSTERED ('))
                    WHEN i.is_unique_constraint = 1 THEN CONVERT(nvarchar(max), CONCAT(N'ALTER TABLE ', QUOTENAME(SCHEMA_NAME(t.schema_id)), N'.', QUOTENAME(t.name), N' ADD CONSTRAINT ', QUOTENAME(i.name), N' UNIQUE NONCLUSTERED ('))
                    ELSE CONVERT(nvarchar(max), CONCAT(N'CREATE ', CASE WHEN i.is_unique = 1 THEN N'UNIQUE ' ELSE N'' END, N'NONCLUSTERED INDEX ', QUOTENAME(i.name), N' ON ', QUOTENAME(SCHEMA_NAME(t.schema_id)), N'.', QUOTENAME(t.name), N' ('))
                END,
                (
                    SELECT STRING_AGG(CONVERT(nvarchar(300), QUOTENAME(c.name) + CASE WHEN ic.is_descending_key = 1 THEN N' DESC' ELSE N' ASC' END), N', ') WITHIN GROUP (ORDER BY ic.key_ordinal)
                    FROM sys.index_columns ic
                    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                    WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
                ),
                N')',
                CASE WHEN i.is_primary_key = 0 AND i.is_unique_constraint = 0 THEN
                    ISNULL(
                        N' INCLUDE (' + (
                            SELECT STRING_AGG(CONVERT(nvarchar(300), QUOTENAME(c.name)), N', ') WITHIN GROUP (ORDER BY ic.index_column_id)
                            FROM sys.index_columns ic
                            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                            WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 1
                        ) + N')', N'')
                    + ISNULL(N' WHERE ' + i.filter_definition, N'')
                ELSE N'' END,
                N' WITH (PAD_INDEX = ', CASE WHEN i.is_padded = 1 THEN N'ON' ELSE N'OFF' END,
                N', STATISTICS_NORECOMPUTE = ', CASE WHEN s.no_recompute = 1 THEN N'ON' ELSE N'OFF' END,
                N', SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = ', CASE WHEN i.ignore_dup_key = 1 THEN N'ON' ELSE N'OFF' END,
                CASE WHEN i.is_primary_key = 0 AND i.is_unique_constraint = 0 THEN N', DROP_EXISTING = OFF' ELSE N'' END,
                N', ONLINE = OFF, ALLOW_ROW_LOCKS = ', CASE WHEN i.allow_row_locks = 1 THEN N'ON' ELSE N'OFF' END,
                N', ALLOW_PAGE_LOCKS = ', CASE WHEN i.allow_page_locks = 1 THEN N'ON' ELSE N'OFF' END,
                N', OPTIMIZE_FOR_SEQUENTIAL_KEY = ', CASE WHEN i.optimize_for_sequential_key = 1 THEN N'ON' ELSE N'OFF' END,
                CASE WHEN i.fill_factor > 0 THEN N', FILLFACTOR = ' + CONVERT(nvarchar(3), i.fill_factor) ELSE N'' END,
                CASE WHEN ipc.data_compression_desc IS NOT NULL AND ipc.data_compression_desc <> N'NONE' THEN N', DATA_COMPRESSION = ' + ipc.data_compression_desc ELSE N'' END,
                N') ON ', QUOTENAME(ds.name), N';'
            )
        FROM (SELECT DISTINCT [object_id] FROM #work) w
        JOIN sys.tables t ON t.object_id = w.[object_id]
        JOIN sys.indexes i ON i.object_id = w.[object_id] AND i.index_id > 1 AND i.type = 2
        JOIN sys.data_spaces ds ON ds.data_space_id = i.data_space_id AND ds.type = 'FG'
        LEFT JOIN sys.stats s ON s.object_id = i.object_id AND s.stats_id = i.index_id
        OUTER APPLY (
            SELECT TOP (1) p.data_compression_desc
            FROM sys.partitions p
            WHERE p.object_id = i.object_id AND p.index_id = i.index_id
        ) ipc;

        -- Validate EVERY capture before anything is dropped: definitions must fit the
        -- extended-property recovery marker (7,500-byte cap - enforced at 3,700 characters)
        -- and marker names must fit sysname (128 characters).
        SET @msg = NULL;
        SELECT @msg = STRING_AGG(CONVERT(nvarchar(200), QUOTENAME([ObjectName])), N', ') FROM #captures WHERE LEN([CreateStmt]) > 3700;
        IF @msg IS NOT NULL
        BEGIN
            RAISERROR('ERROR: The definition(s) of %s are too long to save in a recovery marker. Nothing has been dropped. Follow the manual procedure in README Section 3.6 (Longer-Term Data Retention Strategy, step 3) for the affected table(s).', 16, 1, @msg);
            RETURN;
        END;
        SET @msg = NULL;
        SELECT @msg = STRING_AGG(CONVERT(nvarchar(200), QUOTENAME([ObjectName])), N', ') FROM #captures WHERE LEN([MarkerName]) > 128;
        IF @msg IS NOT NULL
        BEGIN
            RAISERROR('ERROR: The name(s) of %s are too long to form a recovery marker name. Nothing has been dropped. Follow the manual procedure in README Section 3.6 (Longer-Term Data Retention Strategy, step 3) for the affected table(s).', 16, 1, @msg);
            RETURN;
        END;

        DECLARE @workCount int = (SELECT COUNT(*) FROM #work);
        DECLARE @periodCount int = (SELECT COUNT(*) FROM #periods);
        SET @msg = CONCAT(N'Plan: ', @periodCount, N' retirable period(s) older than ', CONVERT(nvarchar(23), @OlderThan, 121), N'; ', @workCount, N' table-partition switch(es); ',
            (SELECT COUNT(*) FROM #captures WHERE [Class] = 1), N' foreign key(s), ',
            (SELECT COUNT(*) FROM #captures WHERE [Class] = 2), N' constraint(s) and ',
            (SELECT COUNT(*) FROM #captures WHERE [Class] = 3), N' non-aligned index(es) to drop/recreate',
            CASE WHEN EXISTS (SELECT 1 FROM #skips) THEN CONCAT(N'; ', (SELECT COUNT(*) FROM #skips), N' table(s) SKIPPED (see above)') ELSE N'' END, N'.');
        RAISERROR('%s', 0, 1, @msg) WITH NOWAIT;


        -- ======================================================================================
        -- Phase 2 - drop foreign keys, then constraints, then indexes. Each object's
        -- recreate statement is saved as an extended-property marker on its table BEFORE
        -- the drop; a marker that already exists with an outdated definition (from an
        -- interrupted run before a schema change) is refreshed from the live object.
        DECLARE @capClass tinyint, @capObjId int, @capSchema sysname, @capTable sysname,
                @capObject sysname, @capMarker nvarchar(300), @capDrop nvarchar(max), @capCreate nvarchar(max);
        DECLARE @markerNameSys sysname, @markerValue nvarchar(4000), @existingMarkerValue nvarchar(4000);
        DECLARE dropCursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT [Class], [object_id], [SchemaName], [TableName], [ObjectName], [MarkerName], [DropStmt], [CreateStmt]
            FROM #captures
            ORDER BY [Class], [TableName], [ObjectName];
        OPEN dropCursor;
        FETCH NEXT FROM dropCursor INTO @capClass, @capObjId, @capSchema, @capTable, @capObject, @capMarker, @capDrop, @capCreate;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @markerNameSys = CONVERT(sysname, @capMarker);
            SET @markerValue = CONVERT(nvarchar(4000), @capCreate);
            PRINT CONCAT(N'-- [', @capTable, N'] README step 3: drop ',
                CASE @capClass WHEN 1 THEN N'foreign key' WHEN 2 THEN N'non-aligned constraint' ELSE N'non-aligned index' END,
                N' (definition saved as extended property ', QUOTENAME(@markerNameSys), N')');
            PRINT @capDrop;
            IF @Execute = 1
            BEGIN
                SET @existingMarkerValue = NULL;
                SELECT @existingMarkerValue = CONVERT(nvarchar(4000), ep.value)
                FROM sys.extended_properties ep
                WHERE ep.class = 1 AND ep.major_id = @capObjId AND ep.minor_id = 0 AND ep.name = @markerNameSys;
                IF @existingMarkerValue IS NULL
                BEGIN
                    EXEC sys.sp_addextendedproperty @name = @markerNameSys, @value = @markerValue,
                        @level0type = N'SCHEMA', @level0name = @capSchema, @level1type = N'TABLE', @level1name = @capTable;
                END
                ELSE IF @existingMarkerValue <> @markerValue
                BEGIN
                    EXEC sys.sp_updateextendedproperty @name = @markerNameSys, @value = @markerValue,
                        @level0type = N'SCHEMA', @level0name = @capSchema, @level1type = N'TABLE', @level1name = @capTable;
                    SET @msg = CONCAT(N'    refreshed stale marker ', QUOTENAME(@markerNameSys), N' from the live definition');
                    RAISERROR('%s', 0, 1, @msg) WITH NOWAIT;
                END;
                EXEC sp_executesql @capDrop;
                SET @msg = CONCAT(N'    dropped ', QUOTENAME(@capObject));
                RAISERROR('%s', 0, 1, @msg) WITH NOWAIT;
            END;
            FETCH NEXT FROM dropCursor INTO @capClass, @capObjId, @capSchema, @capTable, @capObject, @capMarker, @capDrop, @capCreate;
        END;
        CLOSE dropCursor;
        DEALLOCATE dropCursor;

        -- ======================================================================================
        -- Phase 3 - per table + period: create the staging table on the partition's
        -- filegroup (cloning the live table's actual clustered structure and, when present,
        -- large-object placement and compression - a SWITCH requires them to match), then
        -- SWITCH the partition out (metadata-only). The partition number is derived at
        -- execution time - never reused from a previous run.
        DECLARE @wObjId int, @wSchema sysname, @wTable sysname, @wSuffix nvarchar(30), @wIsCatchAll bit,
                @wPeriodStart datetime2(7), @wFgName sysname, @wRows bigint, @wStagingName sysname, @wStagingRows bigint;
        DECLARE @colList nvarchar(max), @keyColList nvarchar(max), @clusteredDDL nvarchar(max), @postCreateDDL nvarchar(max);
        DECLARE @ciIsPk bit, @ciIsUq bit, @ciIsUnique bit, @ciCompression nvarchar(60), @hasLob bit, @pnum int;
        DECLARE workCursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT [object_id], [SchemaName], [TableName], [Suffix], [IsCatchAll], [PeriodStart], [FilegroupName], [Rows], [StagingName], [StagingRows]
            FROM #work
            ORDER BY [TableName], [IsCatchAll] DESC, [PeriodStart];
        OPEN workCursor;
        FETCH NEXT FROM workCursor INTO @wObjId, @wSchema, @wTable, @wSuffix, @wIsCatchAll, @wPeriodStart, @wFgName, @wRows, @wStagingName, @wStagingRows;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- README step 2: staging table. Reused if an interrupted run already created it
            -- (shape verified in phase 0).
            IF @wStagingRows IS NULL
            BEGIN
                SELECT @colList = STRING_AGG(CONVERT(nvarchar(max), CONCAT(
                    N'    ', QUOTENAME(c.name), N' ',
                    CASE
                        WHEN tp.name IN (N'nvarchar', N'nchar') THEN CONCAT(tp.name, N'(', CASE WHEN c.max_length = -1 THEN N'max' ELSE CONVERT(nvarchar(10), c.max_length / 2) END, N')')
                        WHEN tp.name IN (N'varchar', N'char', N'varbinary', N'binary') THEN CONCAT(tp.name, N'(', CASE WHEN c.max_length = -1 THEN N'max' ELSE CONVERT(nvarchar(10), c.max_length) END, N')')
                        WHEN tp.name IN (N'decimal', N'numeric') THEN CONCAT(tp.name, N'(', c.precision, N',', c.scale, N')')
                        WHEN tp.name IN (N'datetime2', N'datetimeoffset', N'time') THEN CONCAT(tp.name, N'(', c.scale, N')')
                        ELSE tp.name
                    END,
                    CASE WHEN c.collation_name IS NOT NULL THEN N' COLLATE ' + c.collation_name ELSE N'' END,
                    CASE WHEN c.is_nullable = 1 THEN N' NULL' ELSE N' NOT NULL' END)), N',' + NCHAR(13) + NCHAR(10)) WITHIN GROUP (ORDER BY c.column_id)
                FROM sys.columns c
                JOIN sys.types tp ON tp.user_type_id = c.user_type_id
                WHERE c.object_id = @wObjId;

                SELECT
                    @ciIsPk = i.is_primary_key,
                    @ciIsUq = i.is_unique_constraint,
                    @ciIsUnique = i.is_unique,
                    @ciCompression = ipc.data_compression_desc
                FROM sys.indexes i
                OUTER APPLY (
                    SELECT TOP (1) p.data_compression_desc
                    FROM sys.partitions p
                    WHERE p.object_id = i.object_id AND p.index_id = i.index_id AND p.rows > 0
                    ORDER BY p.partition_number
                ) ipc
                WHERE i.object_id = @wObjId AND i.index_id = 1;
                SET @ciCompression = ISNULL(@ciCompression, N'NONE');

                SELECT @keyColList = STRING_AGG(CONVERT(nvarchar(300), QUOTENAME(c.name) + CASE WHEN ic.is_descending_key = 1 THEN N' DESC' ELSE N' ASC' END), N', ') WITHIN GROUP (ORDER BY ic.key_ordinal)
                FROM sys.index_columns ic
                JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                WHERE ic.object_id = @wObjId AND ic.index_id = 1 AND ic.key_ordinal > 0;

                SET @hasLob = CASE WHEN EXISTS (
                    SELECT 1 FROM sys.columns c
                    JOIN sys.types tp ON tp.user_type_id = c.user_type_id
                    WHERE c.object_id = @wObjId
                      AND (c.max_length = -1 OR tp.name IN (N'text', N'ntext', N'image', N'xml'))
                ) THEN 1 ELSE 0 END;

                -- Clone the live table's clustered structure: clustered PRIMARY KEY and
                -- clustered UNIQUE constraints are declared inline; a plain clustered index
                -- is created immediately after the table.
                SET @clusteredDDL = N'';
                SET @postCreateDDL = NULL;
                IF @ciIsPk = 1
                    SET @clusteredDDL = CONCAT(N' CONSTRAINT ', QUOTENAME(N'PK_' + @wStagingName), N' PRIMARY KEY CLUSTERED (', @keyColList, N')',
                        CASE WHEN @ciCompression <> N'NONE' THEN CONCAT(N' WITH (DATA_COMPRESSION = ', @ciCompression, N')') ELSE N'' END, NCHAR(13) + NCHAR(10));
                ELSE IF @ciIsUq = 1
                    SET @clusteredDDL = CONCAT(N' CONSTRAINT ', QUOTENAME(N'UQ_' + @wStagingName), N' UNIQUE CLUSTERED (', @keyColList, N')',
                        CASE WHEN @ciCompression <> N'NONE' THEN CONCAT(N' WITH (DATA_COMPRESSION = ', @ciCompression, N')') ELSE N'' END, NCHAR(13) + NCHAR(10));
                ELSE
                    SET @postCreateDDL = CONCAT(
                        N'CREATE ', CASE WHEN @ciIsUnique = 1 THEN N'UNIQUE ' ELSE N'' END, N'CLUSTERED INDEX ', QUOTENAME(N'CI_' + @wStagingName),
                        N' ON ', QUOTENAME(@wSchema), N'.', QUOTENAME(@wStagingName), N' (', @keyColList, N')',
                        CASE WHEN @ciCompression <> N'NONE' THEN CONCAT(N' WITH (DATA_COMPRESSION = ', @ciCompression, N')') ELSE N'' END,
                        N' ON ', QUOTENAME(@wFgName), N';');

                SET @sql = CONCAT(
                    N'CREATE TABLE ', QUOTENAME(@wSchema), N'.', QUOTENAME(@wStagingName), N' (', NCHAR(13) + NCHAR(10),
                    @colList,
                    CASE WHEN @clusteredDDL <> N'' THEN N',' + NCHAR(13) + NCHAR(10) ELSE NCHAR(13) + NCHAR(10) END,
                    @clusteredDDL,
                    N') ON ', QUOTENAME(@wFgName),
                    CASE WHEN @hasLob = 1 THEN N' TEXTIMAGE_ON ' + QUOTENAME(@wFgName) ELSE N'' END,
                    N';');
                PRINT CONCAT(N'-- [', @wTable, N'] README step 2: staging table for period ', @wSuffix, N' (', @wRows, N' rows) on filegroup ', QUOTENAME(@wFgName));
                PRINT @sql;
                IF @postCreateDDL IS NOT NULL PRINT @postCreateDDL;
                IF @Execute = 1
                BEGIN
                    EXEC sp_executesql @sql;
                    IF @postCreateDDL IS NOT NULL EXEC sp_executesql @postCreateDDL;
                    SET @msg = CONCAT(N'    created staging table ', QUOTENAME(@wStagingName), N' on ', QUOTENAME(@wFgName));
                    RAISERROR('%s', 0, 1, @msg) WITH NOWAIT;
                END;
            END
            ELSE
            BEGIN
                PRINT CONCAT(N'-- [', @wTable, N'] README step 2: staging table ', QUOTENAME(@wStagingName), N' already exists (empty, shape verified) - reusing it.');
            END;

            -- README step 4: SWITCH the partition out (metadata-only).
            IF @wIsCatchAll = 1
                SET @pnum = 1;
            ELSE
                SET @pnum = 1 + (
                    SELECT COUNT(*)
                    FROM sys.partition_range_values prv
                    WHERE prv.function_id = @partitionFunctionId AND CONVERT(datetime2(7), prv.value) <= @wPeriodStart
                );

            PRINT CONCAT(N'-- [', @wTable, N'] README step 4: switch partition ', @pnum, N' (period ', @wSuffix, N') out');
            PRINT CONCAT(N'ALTER TABLE ', QUOTENAME(@wSchema), N'.', QUOTENAME(@wTable), N' SWITCH PARTITION ', @pnum, N' TO ', QUOTENAME(@wSchema), N'.', QUOTENAME(@wStagingName), N';');
            IF @Execute = 1
            BEGIN
                SET @sql = CONCAT(N'ALTER TABLE ', QUOTENAME(@wSchema), N'.', QUOTENAME(@wTable), N' SWITCH PARTITION @p TO ', QUOTENAME(@wSchema), N'.', QUOTENAME(@wStagingName), N';');
                EXEC sp_executesql @sql, N'@p int', @p = @pnum;
                SET @msg = CONCAT(N'    switched partition ', @pnum, N' (', @wRows, N' rows) to ', QUOTENAME(@wStagingName));
                RAISERROR('%s', 0, 1, @msg) WITH NOWAIT;
            END;

            FETCH NEXT FROM workCursor INTO @wObjId, @wSchema, @wTable, @wSuffix, @wIsCatchAll, @wPeriodStart, @wFgName, @wRows, @wStagingName, @wStagingRows;
        END;
        CLOSE workCursor;
        DEALLOCATE workCursor;


        -- ======================================================================================
        -- Phase 4 - recreate everything that was dropped and remove each marker as its
        -- object is restored: constraints and indexes first, foreign keys LAST (they depend
        -- on the unique indexes/constraints being back; WITH CHECK revalidation scans the
        -- referencing table, so duration scales with its size). Driven by the markers, so
        -- this also restores objects left missing by an interrupted earlier run - on ANY
        -- table, whether or not it has switches in this run.
        DECLARE @recObjId int, @recSchema sysname, @recTable sysname, @recMarker sysname, @recStmt nvarchar(4000), @recObjectName sysname;
        DECLARE recreateCursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT src.[object_id], src.[SchemaName], src.[TableName], src.[MarkerName], src.[CreateStmt]
            FROM (
                SELECT ep.major_id AS [object_id], SCHEMA_NAME(t.schema_id) AS [SchemaName], t.name AS [TableName],
                       CONVERT(sysname, ep.name) AS [MarkerName], CONVERT(nvarchar(4000), ep.value) AS [CreateStmt]
                FROM sys.extended_properties ep
                JOIN sys.tables t ON t.object_id = ep.major_id
                WHERE ep.class = 1 AND ep.minor_id = 0 AND ep.name LIKE N'PartitionRetirement[_]%'
                UNION
                -- Dry run only: markers are not written, so include the recreations that
                -- execution would perform from the definitions captured in phase 1.
                SELECT c.[object_id], c.[SchemaName], c.[TableName], CONVERT(sysname, c.[MarkerName]), CONVERT(nvarchar(4000), c.[CreateStmt])
                FROM #captures c
                WHERE NOT EXISTS (
                    SELECT 1 FROM sys.extended_properties ep2
                    WHERE ep2.class = 1 AND ep2.major_id = c.[object_id] AND ep2.minor_id = 0 AND ep2.name = c.[MarkerName]
                )
            ) src
            ORDER BY CASE WHEN src.[MarkerName] LIKE N'PartitionRetirement[_]Fk[_]%' THEN 2 ELSE 1 END, src.[TableName], src.[MarkerName];
        OPEN recreateCursor;
        FETCH NEXT FROM recreateCursor INTO @recObjId, @recSchema, @recTable, @recMarker, @recStmt;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @recObjectName = CONVERT(sysname, CASE
                WHEN @recMarker LIKE N'PartitionRetirement[_]Fk[_]%' THEN SUBSTRING(@recMarker, 24, 128)
                WHEN @recMarker LIKE N'PartitionRetirement[_]Cns[_]%' THEN SUBSTRING(@recMarker, 25, 128)
                WHEN @recMarker LIKE N'PartitionRetirement[_]Idx[_]%' THEN SUBSTRING(@recMarker, 25, 128)
                ELSE SUBSTRING(@recMarker, 21, 128)
            END);
            PRINT CONCAT(N'-- [', @recTable, N'] README step 5: recreate ', QUOTENAME(@recObjectName), N' and remove marker ', QUOTENAME(@recMarker));
            PRINT @recStmt;
            IF @Execute = 1
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id = @recObjId AND i.name = @recObjectName)
                   AND NOT EXISTS (SELECT 1 FROM sys.objects o WHERE o.parent_object_id = @recObjId AND o.name = @recObjectName)
                BEGIN
                    SET @msg = CONCAT(N'    recreating ', QUOTENAME(@recObjectName), N' (duration scales with table size)...');
                    RAISERROR('%s', 0, 1, @msg) WITH NOWAIT;
                    EXEC sp_executesql @recStmt;
                END;
                EXEC sys.sp_dropextendedproperty @name = @recMarker,
                    @level0type = N'SCHEMA', @level0name = @recSchema, @level1type = N'TABLE', @level1name = @recTable;
                SET @msg = CONCAT(N'    ', QUOTENAME(@recObjectName), N' in place; marker removed');
                RAISERROR('%s', 0, 1, @msg) WITH NOWAIT;
            END;
            FETCH NEXT FROM recreateCursor INTO @recObjId, @recSchema, @recTable, @recMarker, @recStmt;
        END;
        CLOSE recreateCursor;
        DEALLOCATE recreateCursor;

        -- ======================================================================================
        -- Result summary.
        SELECT
            CASE WHEN @Execute = 1 THEN N'Switched out' ELSE N'Would switch out (dry run)' END AS [Action],
            w.[TableName], w.[Suffix] AS [Period], w.[Rows], QUOTENAME(w.[StagingName]) AS [StagingTable], w.[FilegroupName]
        FROM #work w
        ORDER BY w.[TableName], w.[IsCatchAll] DESC, w.[PeriodStart];

        IF EXISTS (SELECT 1 FROM #skips)
        BEGIN
            SELECT N'SKIPPED tables (rows stay live; affected boundary merges stay blocked)' AS [Report],
                sk.[TableName], sk.[Reason]
            FROM #skips sk ORDER BY sk.[TableName];
        END;

        IF @workCount = 0
        BEGIN
            PRINT 'Nothing to switch out: no partitioned table holds rows in a period entirely older than @OlderThan (or every such table was skipped - see above).';
        END
        ELSE IF @Execute = 1
        BEGIN
            PRINT 'Switch-out complete. The staged rows remain in the database in the [<Table>_retired_<period>] tables listed above.';
            PRINT 'Next: back up / export the staging tables if the data must be kept (README step 6), then run [spAdapterPartitionRetirement_Cleanup] with @ConfirmDataArchivedOrNotNeeded = 1 to drop them, merge the emptied boundaries and remove the freed files/filegroups (README steps 6-8).';
        END
        ELSE
        BEGIN
            PRINT 'DRY RUN complete - no changes were made. Re-run with @Execute = 1 to perform the statements above.';
        END;

        DROP TABLE #periods;
        DROP TABLE #work;
        DROP TABLE #skips;
        DROP TABLE #captures;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH;
END;
GO


-- ================================================================================
-- spAdapterPartitionRetirement_Cleanup
--   README steps 6-8: drop the staging tables (requires explicit confirmation that
--   the staged data has been archived or is not needed, and refuses any staging
--   table whose data extends to @OlderThan or later), verify each retired
--   period's partition is empty in EVERY table on the partition function, MERGE
--   the emptied boundaries, and remove the freed files and filegroups.
-- ================================================================================
CREATE OR ALTER PROCEDURE [dbo].[spAdapterPartitionRetirement_Cleanup]
    @OlderThan datetime2(7),
    @ConfirmDataArchivedOrNotNeeded bit = 0,
    @Execute bit = 0,
    @AcknowledgeSessionCheckUnavailable bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @partitionFunctionName sysname = N'DateTimePartitionFunction_MyGeotabApiAdapter';
    DECLARE @partitionSchemeName sysname = N'DateTimePartitionScheme_MyGeotabApiAdapter';
    DECLARE @partitionFunctionId int;
    DECLARE @interval nvarchar(50);
    DECLARE @msg nvarchar(4000);
    DECLARE @sql nvarchar(max);
    DECLARE @sessionListFull nvarchar(max);
    DECLARE @sessionList nvarchar(2000);

    BEGIN TRY
        -- ======================================================================================
        -- Validate.
        IF @OlderThan IS NULL
        BEGIN
            RAISERROR('ERROR: @OlderThan is required. Example: EXEC [dbo].[spAdapterPartitionRetirement_Cleanup] @OlderThan = ''2023-01-01'';', 16, 1);
            RETURN;
        END;
        IF @OlderThan > SYSUTCDATETIME()
        BEGIN
            RAISERROR('ERROR: @OlderThan is in the future. Refusing: this would affect current or future partitions.', 16, 1);
            RETURN;
        END;
        IF NOT EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = @partitionFunctionName)
        BEGIN
            RAISERROR('ERROR: Partition function ''%s'' does not exist in this database. This utility only applies to a partitioned MyGeotab API Adapter database.', 16, 1, @partitionFunctionName);
            RETURN;
        END;
        IF EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = @partitionFunctionName AND boundary_value_on_right = 0)
        BEGIN
            RAISERROR('ERROR: Partition function ''%s'' is not RANGE RIGHT. This utility only supports the partition layout created by spManagePartitions.', 16, 1, @partitionFunctionName);
            RETURN;
        END;
        SELECT @partitionFunctionId = function_id FROM sys.partition_functions WHERE name = @partitionFunctionName;
        IF (SELECT COUNT(*) FROM sys.partition_schemes WHERE function_id = @partitionFunctionId) > 1
        BEGIN
            RAISERROR('ERROR: Partition function ''%s'' backs more than one partition scheme. This utility only supports the single-scheme layout created by the adapter - merging boundaries would also affect the other scheme''s tables. Follow the manual procedure in README Section 3.6 (Longer-Term Data Retention Strategy).', 16, 1, @partitionFunctionName);
            RETURN;
        END;

        -- An interrupted SwitchOut leaves recovery markers for objects it has not yet
        -- recreated. Cleanup must not proceed past them: the operator would otherwise be
        -- told to restart the adapter with indexes, constraints or foreign keys missing.
        IF EXISTS (
            SELECT 1 FROM sys.extended_properties ep
            WHERE ep.class = 1 AND ep.minor_id = 0 AND ep.name LIKE N'PartitionRetirement[_]%'
        )
        BEGIN
            IF @Execute = 1
            BEGIN
                RAISERROR('ERROR: Pending recovery marker(s) from an interrupted SwitchOut exist (extended properties named ''PartitionRetirement_...''). Re-run [spAdapterPartitionRetirement_SwitchOut] with the same @OlderThan first - it will restore the missing indexes/constraints/foreign keys - then run Cleanup.', 16, 1);
                RETURN;
            END;
            PRINT 'WARNING: Pending recovery marker(s) from an interrupted SwitchOut exist. Execution would REFUSE at this point - re-run [spAdapterPartitionRetirement_SwitchOut] with the same @OlderThan first to restore the missing objects.';
        END;

        IF @Execute = 1
        BEGIN
            PRINT '*** EXECUTE MODE: the statements below are being executed. ***';
        END
        ELSE
        BEGIN
            PRINT '*** DRY RUN: no changes are made. The statements below are what @Execute = 1 would run. ***';
        END;

        SELECT TOP (1) @interval = LOWER([InitialPartitionInterval]) FROM [dbo].[DBPartitionInfo2] ORDER BY [id];
        SET @interval = ISNULL(@interval, N'monthly');

        -- ======================================================================================
        -- README step 6 candidates: staging tables created by SwitchOut whose period lies
        -- entirely before @OlderThan. The suffix encodes the period: 'pre_yyyyMMdd' (rows
        -- before the first boundary), 'yyyyMM' (monthly) or 'yyyyMMdd' (weekly/daily, and
        -- the day-precision fallback for monthly boundaries sharing a month - parsed
        -- conservatively as one whole month). Name-derived period ends are advisory only:
        -- every drop is additionally gated on the staging table's ACTUAL data below.
        CREATE TABLE #stagingDrops (
            [SchemaName] sysname COLLATE DATABASE_DEFAULT NOT NULL,
            [StagingName] sysname COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY,
            [Rows] bigint NOT NULL,
            [PeriodEnd] datetime2(7) NOT NULL,
            [PartitionColumn] sysname COLLATE DATABASE_DEFAULT NOT NULL,
            [Unsafe] bit NOT NULL DEFAULT 0
        );
        INSERT INTO #stagingDrops ([SchemaName], [StagingName], [Rows], [PeriodEnd], [PartitionColumn])
        SELECT
            SCHEMA_NAME(st.schema_id),
            st.name,
            (
                SELECT SUM(p.rows) FROM sys.partitions p
                JOIN sys.indexes i ON i.object_id = p.object_id AND i.index_id = p.index_id
                WHERE p.object_id = st.object_id AND i.index_id IN (0, 1)
            ),
            x.[PeriodEnd],
            pcol.name
        FROM sys.tables st
        JOIN sys.tables bt
            ON st.name LIKE bt.name + N'[_]retired[_]%' AND st.schema_id = bt.schema_id
        JOIN sys.indexes bi ON bi.object_id = bt.object_id AND bi.index_id IN (0, 1)
        JOIN sys.partition_schemes bps ON bps.data_space_id = bi.data_space_id AND bps.name = @partitionSchemeName
        JOIN sys.index_columns bic ON bic.object_id = bt.object_id AND bic.index_id = bi.index_id AND bic.partition_ordinal = 1
        JOIN sys.columns pcol ON pcol.object_id = bt.object_id AND pcol.column_id = bic.column_id
        CROSS APPLY (SELECT SUBSTRING(st.name, LEN(bt.name) + LEN(N'_retired_') + 1, 30) AS [Sfx]) sfx
        CROSS APPLY (
            SELECT CASE
                WHEN sfx.[Sfx] LIKE N'pre[_][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
                    THEN TRY_CONVERT(datetime2(7), CONCAT(SUBSTRING(sfx.[Sfx], 5, 4), N'-', SUBSTRING(sfx.[Sfx], 9, 2), N'-', SUBSTRING(sfx.[Sfx], 11, 2)))
                WHEN sfx.[Sfx] LIKE N'[0-9][0-9][0-9][0-9][0-9][0-9]' AND @interval = N'monthly'
                    THEN DATEADD(MONTH, 1, TRY_CONVERT(datetime2(7), CONCAT(LEFT(sfx.[Sfx], 4), N'-', RIGHT(sfx.[Sfx], 2), N'-01')))
                WHEN sfx.[Sfx] LIKE N'[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' AND @interval = N'monthly'
                    THEN DATEADD(MONTH, 1, TRY_CONVERT(datetime2(7), CONCAT(LEFT(sfx.[Sfx], 4), N'-', SUBSTRING(sfx.[Sfx], 5, 2), N'-', RIGHT(sfx.[Sfx], 2))))
                WHEN sfx.[Sfx] LIKE N'[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' AND @interval = N'weekly'
                    THEN DATEADD(WEEK, 1, TRY_CONVERT(datetime2(7), CONCAT(LEFT(sfx.[Sfx], 4), N'-', SUBSTRING(sfx.[Sfx], 5, 2), N'-', RIGHT(sfx.[Sfx], 2))))
                WHEN sfx.[Sfx] LIKE N'[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' AND @interval = N'daily'
                    THEN DATEADD(DAY, 1, TRY_CONVERT(datetime2(7), CONCAT(LEFT(sfx.[Sfx], 4), N'-', SUBSTRING(sfx.[Sfx], 5, 2), N'-', RIGHT(sfx.[Sfx], 2))))
                ELSE NULL
            END AS [PeriodEnd]
        ) x
        WHERE x.[PeriodEnd] IS NOT NULL AND x.[PeriodEnd] <= @OlderThan;

        -- Data gate over every candidate: whatever the name arithmetic says, a staging
        -- table holding ANY row dated on or after @OlderThan is never dropped.
        DECLARE @gSchema sysname, @gStaging sysname, @gPartCol sysname, @gUnsafe bit;
        DECLARE gateCursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT [SchemaName], [StagingName], [PartitionColumn] FROM #stagingDrops WHERE [Rows] > 0;
        OPEN gateCursor;
        FETCH NEXT FROM gateCursor INTO @gSchema, @gStaging, @gPartCol;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @sql = CONCAT(N'SELECT @u = CASE WHEN EXISTS (SELECT 1 FROM ', QUOTENAME(@gSchema), N'.', QUOTENAME(@gStaging),
                N' WHERE ', QUOTENAME(@gPartCol), N' >= @cutoff) THEN 1 ELSE 0 END;');
            EXEC sp_executesql @sql, N'@cutoff datetime2(7), @u bit OUTPUT', @cutoff = @OlderThan, @u = @gUnsafe OUTPUT;
            IF @gUnsafe = 1
            BEGIN
                UPDATE #stagingDrops SET [Unsafe] = 1 WHERE [StagingName] = @gStaging;
                PRINT CONCAT(N'-- README step 6: REFUSING to drop ', QUOTENAME(@gStaging), N' - it contains at least one row dated on or after @OlderThan (', CONVERT(nvarchar(23), @OlderThan, 121), N'). Investigate before dropping it manually.');
            END;
            FETCH NEXT FROM gateCursor INTO @gSchema, @gStaging, @gPartCol;
        END;
        CLOSE gateCursor;
        DEALLOCATE gateCursor;


        -- ======================================================================================
        -- README step 7 candidates: boundaries whose period lies entirely before @OlderThan.
        CREATE TABLE #merges (
            [BoundaryValue] datetime2(7) NOT NULL PRIMARY KEY,
            [PeriodEnd] datetime2(7) NOT NULL,
            [FilegroupName] sysname COLLATE DATABASE_DEFAULT NOT NULL
        );
        INSERT INTO #merges ([BoundaryValue], [PeriodEnd], [FilegroupName])
        SELECT b.boundary_value, b.next_boundary_value, fg.name
        FROM (
            SELECT
                CONVERT(datetime2(7), prv.value) AS boundary_value,
                CONVERT(datetime2(7), LEAD(prv.value) OVER (ORDER BY CONVERT(datetime2(7), prv.value))) AS next_boundary_value,
                ROW_NUMBER() OVER (ORDER BY CONVERT(datetime2(7), prv.value)) AS ordinal
            FROM sys.partition_range_values prv
            WHERE prv.function_id = @partitionFunctionId
        ) b
        JOIN sys.partition_schemes ps ON ps.name = @partitionSchemeName
        JOIN sys.destination_data_spaces dds ON dds.partition_scheme_id = ps.data_space_id AND dds.destination_id = b.ordinal + 1
        JOIN sys.filegroups fg ON fg.data_space_id = dds.data_space_id
        WHERE b.next_boundary_value IS NOT NULL AND b.next_boundary_value <= @OlderThan;

        -- ======================================================================================
        -- Refusal gate for the only destructive action (README step 6). Back up or export
        -- the staging tables first if the data must be kept. (Staging tables refused by the
        -- data gate above are excluded - they are never dropped by this procedure.)
        IF EXISTS (SELECT 1 FROM #stagingDrops WHERE [Unsafe] = 0) AND @ConfirmDataArchivedOrNotNeeded = 0
        BEGIN
            SELECT
                N'Staging tables that Cleanup would DROP (data will be permanently deleted)' AS [Report],
                QUOTENAME([StagingName]) AS [StagingTable], [Rows]
            FROM #stagingDrops WHERE [Unsafe] = 0 ORDER BY [StagingName];

            IF @Execute = 1
            BEGIN
                RAISERROR('ERROR: Refusing to drop staging tables without confirmation. Back up or export the staged data if it must be kept (README Section 3.6, Longer-Term Data Retention Strategy, step 6), then re-run with @ConfirmDataArchivedOrNotNeeded = 1.', 16, 1);
                RETURN;
            END;
            PRINT 'NOTE: Execution would REFUSE at this point - the staging tables above still exist and @ConfirmDataArchivedOrNotNeeded = 0.';
            PRINT '      Back up or export the staged data if it must be kept (README step 6), then re-run with @ConfirmDataArchivedOrNotNeeded = 1.';
        END;

        -- ======================================================================================
        -- Session gate (README order of operations, step 1). Only enforced when executing.
        -- If the check cannot be performed, execution refuses unless explicitly
        -- acknowledged: note that db_owner alone does NOT include VIEW SERVER STATE.
        IF @Execute = 1
        BEGIN
            IF HAS_PERMS_BY_NAME(NULL, NULL, 'VIEW SERVER STATE') = 0
            BEGIN
                IF @AcknowledgeSessionCheckUnavailable = 0
                BEGIN
                    RAISERROR('ERROR: VIEW SERVER STATE permission not held - cannot verify that no other sessions are connected. Either grant VIEW SERVER STATE to this login, or - after ensuring the adapter is stopped and nothing else is connected (README Section 3.6, Longer-Term Data Retention Strategy, order of operations step 1) - re-run with @AcknowledgeSessionCheckUnavailable = 1.', 16, 1);
                    RETURN;
                END;
                PRINT 'WARNING: VIEW SERVER STATE permission not held - proceeding WITHOUT the connected-session check on your @AcknowledgeSessionCheckUnavailable = 1 acknowledgement. You are responsible for ensuring the adapter is stopped and no other sessions are connected.';
            END
            ELSE
            BEGIN
                SET @sessionListFull = NULL;
                SELECT @sessionListFull = STRING_AGG(CONVERT(nvarchar(max), CONCAT(N'session_id=', s.session_id, N' login=', s.login_name, N' host=', s.host_name, N' program=', LEFT(s.program_name, 60))), N' | ')
                FROM sys.dm_exec_sessions s
                WHERE s.is_user_process = 1 AND s.session_id <> @@SPID AND s.database_id = DB_ID();
                IF @sessionListFull IS NOT NULL
                BEGIN
                    SET @sessionList = LEFT(@sessionListFull, 1800);
                    RAISERROR('ERROR: Other sessions are connected to this database: %s. Stop the adapter and close other connections first (README Section 3.6, Longer-Term Data Retention Strategy, order of operations step 1).', 16, 1, @sessionList);
                    RETURN;
                END;
            END;
        END;

        -- ======================================================================================
        -- README step 6: drop the staging tables (only those that passed the data gate).
        -- Must precede file/filegroup removal - the staging tables live on the filegroups
        -- being removed.
        DECLARE @schemaName sysname, @stagingName sysname, @stgRows bigint;
        DECLARE stagingCursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT [SchemaName], [StagingName], [Rows] FROM #stagingDrops WHERE [Unsafe] = 0 ORDER BY [StagingName];
        OPEN stagingCursor;
        FETCH NEXT FROM stagingCursor INTO @schemaName, @stagingName, @stgRows;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            PRINT CONCAT(N'-- README step 6: drop staging table (', @stgRows, N' rows will be permanently deleted)');
            PRINT CONCAT(N'DROP TABLE ', QUOTENAME(@schemaName), N'.', QUOTENAME(@stagingName), N';');
            IF @Execute = 1 AND @ConfirmDataArchivedOrNotNeeded = 1
            BEGIN
                SET @sql = CONCAT(N'DROP TABLE ', QUOTENAME(@schemaName), N'.', QUOTENAME(@stagingName), N';');
                EXEC sp_executesql @sql;
                SET @msg = CONCAT(N'    dropped ', QUOTENAME(@stagingName));
                RAISERROR('%s', 0, 1, @msg) WITH NOWAIT;
            END;
            FETCH NEXT FROM stagingCursor INTO @schemaName, @stagingName, @stgRows;
        END;
        CLOSE stagingCursor;
        DEALLOCATE stagingCursor;

        -- ======================================================================================
        -- README step 7: for each retirable boundary (ascending), verify the partition is
        -- empty in EVERY table on the partition function (any scheme - MERGE physically
        -- moves rows in all of them), then MERGE it. A period that still holds rows is
        -- skipped and reported - run SwitchOut for it first.
        -- The session gate is re-checked once here: the merge loop is the phase most
        -- exposed to a mid-run adapter restart (e.g. by a service manager).
        IF @Execute = 1 AND HAS_PERMS_BY_NAME(NULL, NULL, 'VIEW SERVER STATE') = 1
        BEGIN
            SET @sessionListFull = NULL;
            SELECT @sessionListFull = STRING_AGG(CONVERT(nvarchar(max), CONCAT(N'session_id=', s.session_id, N' login=', s.login_name)), N' | ')
            FROM sys.dm_exec_sessions s
            WHERE s.is_user_process = 1 AND s.session_id <> @@SPID AND s.database_id = DB_ID();
            IF @sessionListFull IS NOT NULL
            BEGIN
                SET @sessionList = LEFT(@sessionListFull, 1800);
                RAISERROR('ERROR: Session(s) connected mid-run before the boundary merges: %s. The staging-table drops above have completed; boundary merges have NOT started. Stop the adapter / close the connections and re-run Cleanup.', 16, 1, @sessionList);
                RETURN;
            END;
        END;

        CREATE TABLE #skipped ([BoundaryValue] datetime2(7) NOT NULL, [Blockers] nvarchar(4000) COLLATE DATABASE_DEFAULT NOT NULL);
        DECLARE @boundary datetime2(7), @mergeFg sysname, @pnum int, @blockers nvarchar(4000);
        DECLARE mergeCursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT [BoundaryValue], [FilegroupName] FROM #merges ORDER BY [BoundaryValue];
        OPEN mergeCursor;
        FETCH NEXT FROM mergeCursor INTO @boundary, @mergeFg;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Partition number derived at execution time - it shifts after every merge.
            SET @pnum = 1 + (
                SELECT COUNT(*)
                FROM sys.partition_range_values prv
                WHERE prv.function_id = @partitionFunctionId AND CONVERT(datetime2(7), prv.value) <= @boundary
            );

            SET @blockers = NULL;
            SELECT @blockers = STRING_AGG(CONVERT(nvarchar(200), CONCAT(t.name, N' (', p.rows, N' rows)')), N', ')
            FROM sys.tables t
            JOIN sys.indexes i ON i.object_id = t.object_id AND i.index_id IN (0, 1)
            JOIN sys.partition_schemes ps ON ps.data_space_id = i.data_space_id AND ps.function_id = @partitionFunctionId
            JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id = i.index_id AND p.partition_number = @pnum
            WHERE p.rows > 0;

            IF @blockers IS NOT NULL
            BEGIN
                INSERT INTO #skipped VALUES (@boundary, @blockers);
                SET @msg = CONCAT(N'-- README step 7: SKIPPING boundary ', CONVERT(nvarchar(23), @boundary, 121), N' - partition ', @pnum, N' still holds rows in: ', @blockers, N'. Run [spAdapterPartitionRetirement_SwitchOut] for this period first.');
                PRINT @msg;
            END
            ELSE
            BEGIN
                PRINT CONCAT(N'-- README step 7: partition ', @pnum, N' verified empty in every table on the partition function; merge boundary');
                PRINT CONCAT(N'ALTER PARTITION FUNCTION ', QUOTENAME(@partitionFunctionName), N'() MERGE RANGE (''', CONVERT(nvarchar(23), @boundary, 121), N''');');
                IF @Execute = 1
                BEGIN
                    SET @sql = CONCAT(N'ALTER PARTITION FUNCTION ', QUOTENAME(@partitionFunctionName), N'() MERGE RANGE (''', CONVERT(nvarchar(23), @boundary, 121), N''');');
                    EXEC sp_executesql @sql;
                    SET @msg = CONCAT(N'    merged boundary ', CONVERT(nvarchar(23), @boundary, 121), N' (filegroup ', QUOTENAME(@mergeFg), N' may now be unmapped - the file/filegroup sweep below verifies)');
                    RAISERROR('%s', 0, 1, @msg) WITH NOWAIT;
                END;
            END;

            FETCH NEXT FROM mergeCursor INTO @boundary, @mergeFg;
        END;
        CLOSE mergeCursor;
        DEALLOCATE mergeCursor;


        -- ======================================================================================
        -- README step 8: remove files and filegroups that no longer back any partition and
        -- hold no objects. Evaluated live (after the merges above), so this also completes
        -- removals left behind by an interrupted earlier run. Scoped to filegroups created
        -- by spManagePartitions: the FG_..._yyyyMM name pattern AND a file whose logical
        -- name is '<FilegroupName>_DataFile' (that pairing is never true for customer
        -- filegroups), with the whole month before @OlderThan. Emptiness is judged on
        -- allocation units - which also covers large-object-only allocation - not just on
        -- sys.indexes.
        DECLARE @fgName2 sysname, @fileName sysname;
        DECLARE fgCursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT fg.name
            FROM sys.filegroups fg
            WHERE fg.name LIKE N'FG[_]%'
              AND fg.name LIKE N'%[0-9][0-9][0-9][0-9][0-9][0-9]'
              AND fg.is_default = 0
              AND TRY_CONVERT(datetime2(7), CONCAT(LEFT(RIGHT(fg.name, 6), 4), N'-', RIGHT(fg.name, 2), N'-01')) IS NOT NULL
              AND DATEADD(MONTH, 1, TRY_CONVERT(datetime2(7), CONCAT(LEFT(RIGHT(fg.name, 6), 4), N'-', RIGHT(fg.name, 2), N'-01'))) <= @OlderThan
              AND EXISTS (SELECT 1 FROM sys.database_files fa WHERE fa.data_space_id = fg.data_space_id AND fa.name = fg.name + N'_DataFile')
              AND NOT EXISTS (SELECT 1 FROM sys.destination_data_spaces dds WHERE dds.data_space_id = fg.data_space_id)
              AND NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.data_space_id = fg.data_space_id)
              AND NOT EXISTS (SELECT 1 FROM sys.allocation_units au WHERE au.data_space_id = fg.data_space_id AND au.total_pages > 0)
            ORDER BY fg.name;
        OPEN fgCursor;
        FETCH NEXT FROM fgCursor INTO @fgName2;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE fileCursor CURSOR LOCAL FAST_FORWARD FOR
                SELECT f.name
                FROM sys.database_files f
                JOIN sys.filegroups fg ON fg.data_space_id = f.data_space_id
                WHERE fg.name = @fgName2;
            OPEN fileCursor;
            FETCH NEXT FROM fileCursor INTO @fileName;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                PRINT CONCAT(N'-- README step 8: remove file ', QUOTENAME(@fileName), N' (filegroup ', QUOTENAME(@fgName2), N')');
                PRINT CONCAT(N'ALTER DATABASE CURRENT REMOVE FILE ', QUOTENAME(@fileName), N';');
                IF @Execute = 1
                BEGIN
                    SET @sql = CONCAT(N'ALTER DATABASE CURRENT REMOVE FILE ', QUOTENAME(@fileName), N';');
                    BEGIN TRY
                        EXEC sp_executesql @sql;
                    END TRY
                    BEGIN CATCH
                        IF ERROR_NUMBER() = 5042
                        BEGIN
                            SET @msg = CONCAT(N'    file ', QUOTENAME(@fileName), N' not yet empty - running DBCC SHRINKFILE EMPTYFILE and retrying (README step 8 note)');
                            RAISERROR('%s', 0, 1, @msg) WITH NOWAIT;
                            DBCC SHRINKFILE(@fileName, EMPTYFILE) WITH NO_INFOMSGS;
                            EXEC sp_executesql @sql;
                        END
                        ELSE
                        BEGIN
                            THROW;
                        END;
                    END CATCH;
                    SET @msg = CONCAT(N'    removed file ', QUOTENAME(@fileName));
                    RAISERROR('%s', 0, 1, @msg) WITH NOWAIT;
                END;
                FETCH NEXT FROM fileCursor INTO @fileName;
            END;
            CLOSE fileCursor;
            DEALLOCATE fileCursor;

            PRINT CONCAT(N'ALTER DATABASE CURRENT REMOVE FILEGROUP ', QUOTENAME(@fgName2), N';');
            IF @Execute = 1
            BEGIN
                SET @sql = CONCAT(N'ALTER DATABASE CURRENT REMOVE FILEGROUP ', QUOTENAME(@fgName2), N';');
                EXEC sp_executesql @sql;
                SET @msg = CONCAT(N'    removed filegroup ', QUOTENAME(@fgName2));
                RAISERROR('%s', 0, 1, @msg) WITH NOWAIT;
            END;
            FETCH NEXT FROM fgCursor INTO @fgName2;
        END;
        CLOSE fgCursor;
        DEALLOCATE fgCursor;

        -- In a dry run the merges have not actually happened, so also report the filegroups
        -- that would become removable once the printed merges execute. This prediction is
        -- best-effort: exotic cross-schema layouts can make it over- or under-list - the
        -- live sweep above re-evaluates everything against the real catalogs at execution
        -- time and is the only authority on what is removed.
        IF @Execute = 0
        BEGIN
            SELECT
                N'Filegroups removable after the above merges execute (best-effort prediction)' AS [Report],
                m.[FilegroupName], f.name AS [LogicalFileName], f.physical_name AS [PhysicalFileName]
            FROM #merges m
            JOIN sys.filegroups fg ON fg.name = m.[FilegroupName]
            LEFT JOIN sys.database_files f ON f.data_space_id = fg.data_space_id
            WHERE NOT EXISTS (SELECT 1 FROM #skipped sk WHERE sk.[BoundaryValue] = m.[BoundaryValue])
              AND EXISTS (SELECT 1 FROM sys.database_files fa WHERE fa.data_space_id = fg.data_space_id AND fa.name = fg.name + N'_DataFile')
              AND NOT EXISTS (
                  -- Still allocated by an object other than the staging tables dropped above.
                  SELECT 1 FROM sys.indexes i
                  WHERE i.data_space_id = fg.data_space_id
                    AND OBJECT_NAME(i.object_id) NOT IN (SELECT [StagingName] FROM #stagingDrops WHERE [Unsafe] = 0)
              )
              AND NOT EXISTS (
                  -- Still mapped to a partition other than the ones being merged above.
                  SELECT 1 FROM sys.destination_data_spaces dds
                  WHERE dds.data_space_id = fg.data_space_id
                    AND dds.destination_id NOT IN (
                        SELECT 1 + (
                            SELECT COUNT(*)
                            FROM sys.partition_range_values prv
                            WHERE prv.function_id = @partitionFunctionId AND CONVERT(datetime2(7), prv.value) <= m2.[BoundaryValue]
                        )
                        FROM #merges m2
                        WHERE NOT EXISTS (SELECT 1 FROM #skipped sk2 WHERE sk2.[BoundaryValue] = m2.[BoundaryValue])
                    )
              )
            ORDER BY m.[FilegroupName];
            PRINT 'NOTE: The filegroup list above is a dry-run prediction. The executing sweep re-derives removability from the live catalogs after the merges and staging drops have actually happened.';
        END;

        -- ======================================================================================
        -- Result summary.
        SELECT
            CASE
                WHEN [Unsafe] = 1 THEN N'REFUSED - staging data extends to @OlderThan or later'
                WHEN @Execute = 1 AND @ConfirmDataArchivedOrNotNeeded = 1 THEN N'Dropped'
                ELSE N'Would drop (requires @ConfirmDataArchivedOrNotNeeded = 1)'
            END AS [Action],
            QUOTENAME([StagingName]) AS [StagingTable], [Rows]
        FROM #stagingDrops ORDER BY [Unsafe] DESC, [StagingName];

        SELECT
            CASE
                WHEN EXISTS (SELECT 1 FROM #skipped sk WHERE sk.[BoundaryValue] = m.[BoundaryValue]) THEN N'SKIPPED - partition not empty (run SwitchOut first)'
                WHEN @Execute = 1 THEN N'Merged'
                ELSE N'Would merge (dry run)'
            END AS [Action],
            m.[BoundaryValue], m.[FilegroupName],
            (SELECT sk.[Blockers] FROM #skipped sk WHERE sk.[BoundaryValue] = m.[BoundaryValue]) AS [BlockedBy]
        FROM #merges m ORDER BY m.[BoundaryValue];

        IF @Execute = 1
        BEGIN
            PRINT 'Cleanup complete. Restart the adapter when all retirement work is finished (README step 9).';
        END
        ELSE
        BEGIN
            PRINT 'DRY RUN complete - no changes were made. Re-run with @Execute = 1 (and @ConfirmDataArchivedOrNotNeeded = 1 to drop staging tables) to perform the statements above.';
        END;

        DROP TABLE #stagingDrops;
        DROP TABLE #merges;
        DROP TABLE #skipped;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH;
END;
GO

PRINT 'Partition-retirement utility installed: [spAdapterPartitionRetirement_Preview], [spAdapterPartitionRetirement_SwitchOut], [spAdapterPartitionRetirement_Cleanup].';
PRINT 'Start with: EXEC [dbo].[spAdapterPartitionRetirement_Preview] @OlderThan = ''<cutoff date>'';';
GO

