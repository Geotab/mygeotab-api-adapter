-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- NOTES:
--   1: This script applies to the Geotab DIG Adapter database starting with
--      application version 3.0.0. It does not apply to earlier versions of the
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

-- /*** [START] Clean Database ***/
-- -- Step 1: Delete data from all tables:
-- -- Note: MiddlewareVersionInfo is NOT truncated - it tracks DB version history

-- -- Core tables
-- DELETE FROM gda."OServiceTracking";
-- DELETE FROM gda."ProvisionedDevices";

-- -- Device provisioning tables
-- DELETE FROM gda."Q_ProvisionDevices";
-- DELETE FROM gda."Q_ProvisionDevicesFail";

-- -- Telemetry record queue and fail tables (11 record types)
-- DELETE FROM gda."Q_GpsRecords";
-- DELETE FROM gda."Q_GpsRecordsFail";
-- DELETE FROM gda."Q_AccelerationRecords";
-- DELETE FROM gda."Q_AccelerationRecordsFail";
-- DELETE FROM gda."Q_BinaryRecords";
-- DELETE FROM gda."Q_BinaryRecordsFail";
-- DELETE FROM gda."Q_BluetoothRecords";
-- DELETE FROM gda."Q_BluetoothRecordsFail";
-- DELETE FROM gda."Q_DriverChangeRecords";
-- DELETE FROM gda."Q_DriverChangeRecordsFail";
-- DELETE FROM gda."Q_GenericFaultRecords";
-- DELETE FROM gda."Q_GenericFaultRecordsFail";
-- DELETE FROM gda."Q_GenericStatusRecords";
-- DELETE FROM gda."Q_GenericStatusRecordsFail";
-- DELETE FROM gda."Q_J1708FaultRecords";
-- DELETE FROM gda."Q_J1708FaultRecordsFail";
-- DELETE FROM gda."Q_J1939FaultRecords";
-- DELETE FROM gda."Q_J1939FaultRecordsFail";
-- DELETE FROM gda."Q_ObdiiFaultRecords";
-- DELETE FROM gda."Q_ObdiiFaultRecordsFail";
-- DELETE FROM gda."Q_VinRecords";
-- DELETE FROM gda."Q_VinRecordsFail";

-- -- Invalid records tables
-- DELETE FROM gda."DIGInvalidRecords";
-- DELETE FROM gda."DIGInvalidRecordsCursor";

-- -- Step 2: Restart identity sequences:
-- -- Note: MiddlewareVersionInfo is NOT reseeded - it tracks DB version history
-- -- Core tables
-- ALTER SEQUENCE gda."OServiceTracking_id_seq" RESTART;
-- ALTER SEQUENCE gda."ProvisionedDevices_id_seq" RESTART;

-- -- Device provisioning tables
-- ALTER SEQUENCE gda."Q_ProvisionDevices_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_ProvisionDevicesFail_id_seq" RESTART;

-- -- Telemetry record queue and fail tables (11 record types)
-- ALTER SEQUENCE gda."Q_GpsRecords_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_GpsRecordsFail_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_AccelerationRecords_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_AccelerationRecordsFail_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_BinaryRecords_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_BinaryRecordsFail_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_BluetoothRecords_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_BluetoothRecordsFail_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_DriverChangeRecords_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_DriverChangeRecordsFail_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_GenericFaultRecords_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_GenericFaultRecordsFail_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_GenericStatusRecords_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_GenericStatusRecordsFail_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_J1708FaultRecords_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_J1708FaultRecordsFail_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_J1939FaultRecords_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_J1939FaultRecordsFail_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_ObdiiFaultRecords_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_ObdiiFaultRecordsFail_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_VinRecords_id_seq" RESTART;
-- ALTER SEQUENCE gda."Q_VinRecordsFail_id_seq" RESTART;

-- -- Invalid records tables
-- ALTER SEQUENCE gda."DIGInvalidRecords_id_seq" RESTART;
-- -- Note: DIGInvalidRecordsCursor does not have an IDENTITY column

-- -- Step 3: Re-add sentinel records:
-- -- Re-insert the DIGInvalidRecordsCursor row (required for service operation)
-- INSERT INTO gda."DIGInvalidRecordsCursor" ("id", "NextResultKey", "LastUpdatedUtc")
-- VALUES (1, 0, (now() AT TIME ZONE 'UTC'));

-- /*** [END] Clean Database ***/




/* Check counts */
-- ANALYZE;
WITH table_counts AS (
    SELECT
        relname AS table_name,
        COALESCE(n_live_tup, 0) AS record_count
    FROM pg_stat_all_tables
    WHERE schemaname = 'gda'
      AND relname NOT LIKE 'stg_%'
)
SELECT
    table_name AS "TableName",
    record_count AS "RecordCount"
FROM table_counts
ORDER BY table_name;


/* Check counts by partition (if applicable) */
-- ANALYZE;
SELECT
    parent_table.relname AS parent_table,
    child_table.relname AS partition,
    pg_catalog.pg_get_expr(child_table.relpartbound, child_table.oid) AS partition_bound,
    COALESCE(stats.n_live_tup, 0) AS record_count
FROM pg_inherits AS pi
JOIN pg_class AS parent_table
    ON pi.inhparent = parent_table.oid
JOIN pg_class AS child_table
    ON pi.inhrelid = child_table.oid
JOIN pg_namespace AS ns
    ON parent_table.relnamespace = ns.oid
LEFT JOIN pg_stat_all_tables AS stats
    ON stats.relname = child_table.relname
WHERE ns.nspname = 'gda'
  AND parent_table.relkind = 'p'
  AND child_table.relkind = 'r'
ORDER BY parent_table.relname, child_table.relname;
