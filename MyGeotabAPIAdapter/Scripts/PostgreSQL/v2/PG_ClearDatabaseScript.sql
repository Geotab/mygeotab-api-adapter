-- ================================================================================
-- DATABASE TYPE: PostgreSQL
-- 
-- NOTES: 
--   1: This script applies to the MyGeotab API Adapter database starting with
--	    application version 3.0.0. It does not apply to earlier versions of the
--      application. 
--   2: This script is updated as new tables are added to the database (i.e. there
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

-- /* Clean Database */ 
-- delete from public."MyGeotabVersionInfo2";
-- delete from public."DBMaintenanceLogs2";
-- delete from public."OServiceTracking2";
-- ALTER TABLE public."BinaryData2" DISABLE TRIGGER ALL;
-- delete from public."BinaryData2";
-- ALTER TABLE public."BinaryData2" ENABLE TRIGGER ALL;
-- ALTER TABLE public."ExceptionEvents2" DISABLE TRIGGER ALL;
-- delete from public."ExceptionEvents2";
-- ALTER TABLE public."ExceptionEvents2" ENABLE TRIGGER ALL;
-- ALTER TABLE public."Trips2" DISABLE TRIGGER ALL;
-- delete from public."Trips2";
-- ALTER TABLE public."Trips2" ENABLE TRIGGER ALL;
-- ALTER TABLE public."StatusDataLocations2" DISABLE TRIGGER ALL;
-- delete from public."StatusDataLocations2";
-- ALTER TABLE public."StatusDataLocations2" ENABLE TRIGGER ALL;
-- ALTER TABLE public."StatusData2" DISABLE TRIGGER ALL;
-- delete from public."StatusData2";
-- ALTER TABLE public."StatusData2" ENABLE TRIGGER ALL;
-- ALTER TABLE public."FaultDataLocations2" DISABLE TRIGGER ALL;
-- delete from public."FaultDataLocations2";
-- ALTER TABLE public."FaultDataLocations2" ENABLE TRIGGER ALL;
-- ALTER TABLE public."FaultData2" DISABLE TRIGGER ALL;
-- delete from public."FaultData2";
-- ALTER TABLE public."FaultData2" ENABLE TRIGGER ALL;
-- ALTER TABLE public."DiagnosticIds2" DISABLE TRIGGER ALL;
-- delete from public."DiagnosticIds2";
-- ALTER TABLE public."DiagnosticIds2" ENABLE TRIGGER ALL;
-- ALTER TABLE public."Diagnostics2" DISABLE TRIGGER ALL;
-- delete from public."Diagnostics2";
-- ALTER TABLE public."Diagnostics2" ENABLE TRIGGER ALL;
-- ALTER TABLE public."EntityMetadata2" DISABLE TRIGGER ALL;
-- delete from public."EntityMetadata2";
-- ALTER TABLE public."EntityMetadata2" ENABLE TRIGGER ALL;
-- ALTER TABLE public."LogRecords2" DISABLE TRIGGER ALL;
-- delete from public."LogRecords2";
-- ALTER TABLE public."LogRecords2" ENABLE TRIGGER ALL;
-- ALTER TABLE public."Devices2" DISABLE TRIGGER ALL;
-- delete from public."Devices2";
-- ALTER TABLE public."Devices2" ENABLE TRIGGER ALL;
-- ALTER TABLE public."Groups2" DISABLE TRIGGER ALL;
-- delete from public."Groups2";
-- ALTER TABLE public."Groups2" ENABLE TRIGGER ALL;
-- ALTER TABLE public."Rules2" DISABLE TRIGGER ALL;
-- delete from public."Rules2";
-- ALTER TABLE public."Rules2" ENABLE TRIGGER ALL;
-- ALTER TABLE public."Users2" DISABLE TRIGGER ALL;
-- delete from public."Users2";
-- ALTER TABLE public."Users2" ENABLE TRIGGER ALL;
-- ALTER TABLE public."Zones2" DISABLE TRIGGER ALL;
-- delete from public."Zones2";
-- ALTER TABLE public."Zones2" ENABLE TRIGGER ALL;
-- ALTER TABLE public."ZoneTypes2" DISABLE TRIGGER ALL;
-- delete from public."ZoneTypes2";
-- ALTER TABLE public."ZoneTypes2" ENABLE TRIGGER ALL;
-- ALTER SEQUENCE public."DBMaintenanceLogs2_id_seq" RESTART;
-- ALTER SEQUENCE public."DiagnosticIds2_id_seq" RESTART;
-- ALTER SEQUENCE public."Diagnostics2_id_seq" RESTART;
-- ALTER SEQUENCE public."EntityMetadata2_id_seq" RESTART;
-- ALTER SEQUENCE public."Groups2_id_seq" RESTART;
-- ALTER SEQUENCE public."Rules2_id_seq" RESTART;
-- ALTER SEQUENCE public."Trips2_id_seq" RESTART;
-- ALTER SEQUENCE public."ZoneTypes2_id_seq" RESTART;
-- ALTER SEQUENCE public."OServiceTracking2_id_seq" RESTART;


/* Check counts */
--ANALYZE;
WITH partitioned_tables AS (
    SELECT part.relname AS parent_table,
        child.relname AS partition_table
    FROM pg_partitioned_table p
    JOIN pg_class part ON p.partrelid = part.oid
    JOIN pg_inherits i ON part.oid = i.inhparent
    JOIN pg_class child ON i.inhrelid = child.oid
    WHERE part.relname NOT LIKE 'pg_%' 
		AND part.relname NOT LIKE 'sql_%'
		AND part.relname NOT LIKE 'stg_%' 
		AND child.relname NOT LIKE 'pg_%'
		AND child.relname NOT LIKE 'sql_%'
		AND child.relname NOT LIKE 'stg_%'
),
table_counts AS (
    SELECT relname AS table_name,
        coalesce(SUM(n_live_tup), 0) AS record_count
    FROM pg_stat_all_tables
    WHERE relname NOT LIKE 'pg_%'
		AND relname NOT LIKE 'sql_%'
		AND relname NOT LIKE 'stg_%'
    GROUP BY relname
)
SELECT
    CASE
        WHEN pt.parent_table IS NOT NULL THEN pt.parent_table
        ELSE tc.table_name
    END AS TableName,
    SUM(tc.record_count) AS RecordCount
FROM table_counts tc
LEFT JOIN partitioned_tables pt ON tc.table_name = pt.partition_table
GROUP BY TableName
ORDER BY TableName;


/* Check counts by partition */
--ANALYZE;
SELECT parent_table.relname AS parent_table,
    child_table.relname AS partition,
    pg_catalog.pg_get_expr(child_table.relpartbound, child_table.oid) AS partition_bound,
    COALESCE(stats.n_live_tup, 0) AS record_count
FROM pg_inherits AS pi
JOIN pg_class AS parent_table 
    ON pi.inhparent = parent_table.oid
JOIN pg_class AS child_table 
    ON pi.inhrelid = child_table.oid
LEFT JOIN pg_stat_all_tables AS stats
    ON stats.relname = child_table.relname
WHERE parent_table.relkind = 'p'
    AND child_table.relkind = 'r'
ORDER BY parent_table.relname, child_table.relname;