-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 5.1.0.0 to version 5.2.0.0.
--
-- NOTES: 
--   1: This script cannot be run against any database version other than that 
--		specified above. 
--   2: Be sure to connect to the "geotabadapterdb" before executing. 
-- ================================================================================


/*** [START] Transaction wrapper ***/
-- The whole upgrade runs in one transaction so that the version guard below actually
-- enforces. PostgreSQL DDL is transactional, so a RAISE EXCEPTION aborts every change
-- made by this script regardless of how the client is invoked -- psql without
-- ON_ERROR_STOP=1 otherwise reports the error and carries on to the next statement.
BEGIN;
/*** [END] Transaction wrapper ***/

/*** [START] Part 1 of 3: Database Version Validation Below ***/ 
-- Store upgrade database version in a temporary table.
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
CREATE TEMPORARY TABLE "TMP_UpgradeDatabaseVersionTable" ("UpgradeDatabaseVersion" character varying(50));
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('5.2.0.0');

DO $$	 
DECLARE 
    required_starting_database_version TEXT DEFAULT '5.1.0.0';
    actual_starting_database_version TEXT;

BEGIN
	SELECT "DatabaseVersion" 
	INTO actual_starting_database_version
	FROM public."MiddlewareVersionInfo2"
	ORDER BY "RecordCreationTimeUtc" DESC
	LIMIT 1;
	
	IF actual_starting_database_version IS DISTINCT FROM required_starting_database_version THEN
		RAISE EXCEPTION 'ERROR: This script can only be run against the expected database version. [Expected: %, Actual: %]', 
			required_starting_database_version, actual_starting_database_version;
	END IF;
END $$;
/*** [END] Part 1 of 3: Database Version Validation Above ***/ 



/*** [START] Part 2 of 3: Database Upgrades Below ***/
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spManagePartitions function:
-- Scopes the partitioned-table loop, the owner lookup and the
-- partition-existence checks to the public schema. Without this, a partitioned
-- table belonging to another component in the same database - the Geotab DIG
-- Adapter's gda schema, for example - is picked up by the loop, and the
-- generated statement becomes CREATE TABLE public."other.Table_yyyyMM"
-- PARTITION OF public."other.Table", which raises an error and aborts the whole
-- function before ANY of the adapter's own partitions are created. The adapter
-- runs this on every startup and partition management cannot be disabled, so
-- that would leave the adapter unable to maintain its partitions at all.
--
-- This definition is kept byte-identical to the one in
-- PG_0.0.0.1_spManagePartitions.sql, which is what a new installation executes.
CREATE OR REPLACE FUNCTION public."spManagePartitions"(
	"MinDateTimeUTC" timestamp without time zone,
	"PartitionInterval" text)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description: 
-- Manages partitions dynamically based on the specified interval. The PartitionInterval 
-- parameter determines whether partitions are created on a 'monthly', 'weekly', or 'daily' 
-- basis. 
--
-- The MinDateTimeUTC parameter value is used as a starting point the first time this
-- function is executed. On subsequent executions, partition creation will start from
-- the last existing partition date to ensure no gaps. 
--
-- Partitions will always be created through until the end of the next calendar period (month, 
-- week, or day) from the DateTime at which this procedure is executed. Executing this 
-- function periodically (e.g. daily, weekly, etc.) will ensure that partitions are created 
-- before any data needs to be added.
--
-- A row waiting in a table's DEFAULT partition that belongs in a partition this
-- function creates is moved into that partition as part of the same run (Step 2).
-- ==========================================================================================

DECLARE
    initial_min_datetime_utc TIMESTAMP;
	initial_min_datetime_utc_string TEXT;
	min_datetime_utc_string TEXT;
    initial_partition_interval TEXT;
    min_date_utc DATE := "MinDateTimeUTC"::DATE;
	max_date_utc TIMESTAMP := date_trunc('month', (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '2 months') - INTERVAL '1 day';
    partition_interval INTERVAL;

    parent_table TEXT;
    parent_table_stripped TEXT;
    parent_table_owner TEXT;
    schema_prefix TEXT := 'public.';
    partition_name TEXT;
    partition_name_default TEXT;
    partition_start DATE;
    partition_end DATE;
    default_string TEXT := 'default';
    sql_string TEXT;
    sql_default TEXT;
    function_name TEXT := 'spManagePartitions';
    function_start_time TIMESTAMP;
    start_time TIMESTAMP;
    start_time_string TEXT;
    end_time TIMESTAMP;
    duration_string TEXT;
    range_start DATE;
    range_end DATE;
    ordered_tables TEXT[];
    table_index INTEGER;
    partition_key_column TEXT;
    stash_table TEXT;
    stashed_tables TEXT[] := '{}';
    default_has_rows BOOLEAN;
    rows_moved BIGINT;

BEGIN
    -- ======================================================================================
    -- Log start of function execution.
    function_start_time := CLOCK_TIMESTAMP();
    start_time := CLOCK_TIMESTAMP();
    start_time_string := TO_CHAR(start_time, 'YYYY-MM-DD HH24:MI:SS');
    RAISE NOTICE 'Executing function ''%'' Start: %', function_name, start_time_string;
    RAISE NOTICE 'min_date_utc: % ', min_date_utc;
    RAISE NOTICE 'max_date_utc: % ', max_date_utc;

    -- ======================================================================================
    -- STEP 1: Validate input parameter values.
    RAISE NOTICE 'Step 1 [Validating input parameter values]...';

    -- min_date_utc cannot be greater than max_date_utc.
    IF min_date_utc > max_date_utc THEN
        RAISE EXCEPTION 'ERROR: The min_date_utc cannot be greater than max_date_utc. [min_date_utc: %, max_date_utc: %]',
                        min_date_utc, max_date_utc;
    END IF;

    -- Determine partition interval.
    IF "PartitionInterval" = 'monthly' THEN
        partition_interval := '1 month';
    ELSIF "PartitionInterval" = 'weekly' THEN
        partition_interval := '1 week';
    ELSIF "PartitionInterval" = 'daily' THEN
        partition_interval := '1 day';
    ELSE
        RAISE EXCEPTION 'ERROR: Invalid PartitionInterval value. Accepted values are: monthly, weekly, daily.';
    END IF;

	-- Check if the DBPartitionInfo2 table contains any rows. If so, validate inputs against the table.
    IF EXISTS (SELECT 1 FROM public."DBPartitionInfo2") THEN
        -- Retrieve the existing metadata.
        SELECT "InitialMinDateTimeUTC", "InitialPartitionInterval"
        INTO initial_min_datetime_utc, initial_partition_interval
        FROM public."DBPartitionInfo2"
        ORDER BY id
        LIMIT 1;

        -- The value supplied to the "MinDateTimeUTC" parameter cannot be earlier than the initial_min_datetime_utc.
        IF min_date_utc < initial_min_datetime_utc THEN
            initial_min_datetime_utc_string := TO_CHAR(initial_min_datetime_utc, 'YYYY-MM-DD HH24:MI:SS');
            min_datetime_utc_string := TO_CHAR(min_date_utc, 'YYYY-MM-DD HH24:MI:SS');
            RAISE EXCEPTION 'ERROR: The MinDateTimeUTC cannot be earlier than initial_min_datetime_utc. [min_datetime_utc: %, initial_min_datetime_utc: %]', min_datetime_utc_string, initial_min_datetime_utc_string;
        END IF;

        -- The value supplied to the PartitionInterval parameter cannot differ from the initial_partition_interval.
        IF "PartitionInterval" <> initial_partition_interval THEN
            RAISE EXCEPTION 'ERROR: The PartitionInterval cannot differ from initial_partition_interval. [PartitionInterval: %, initial_partition_interval: %]', "PartitionInterval", initial_partition_interval;
        END IF;
    ELSE
        -- Insert a row into DBPartitionInfo2 if this is the first time this procedure is being executed.
        INSERT INTO public."DBPartitionInfo2" ("InitialMinDateTimeUTC", "InitialPartitionInterval", "RecordCreationTimeUtc")
        VALUES (min_date_utc, "PartitionInterval", CURRENT_TIMESTAMP AT TIME ZONE 'UTC');
    END IF;

    -- Log.
    end_time := CLOCK_TIMESTAMP();
    duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000;
    RAISE NOTICE 'Step 1 completed. Duration: % milliseconds', duration_string;
    start_time := end_time;

    -- ======================================================================================
    -- Step 2: Create partitions based on the determined interval.
    RAISE NOTICE 'Step 2 [Creating partitions]...';

    BEGIN
        -- PostgreSQL refuses to create a partition while the table's DEFAULT
        -- partition holds a row that belongs in it ("updated partition constraint
        -- for default partition ... would be violated by some row"). A row lands in
        -- DEFAULT when it is dated after the last partition - a device whose clock
        -- runs ahead, or data that kept arriving after this function had been
        -- failing for longer than the partitions created ahead of time lasted - and
        -- one such row would stop every later run, the one at startup included. So
        -- the rows in each DEFAULT partition that fall inside the periods this run
        -- covers are set aside in a temporary table first, and filed into their
        -- partitions once those exist, after the loop below. Those are exactly the
        -- rows that belong in a partition about to be created: a row whose partition
        -- already exists is in that partition, not in DEFAULT.
        --
        -- DEFAULT is not detached instead, as the Geotab DIG Adapter does, because
        -- some of these tables are referenced by a foreign key - DVIRDefects2
        -- references DVIRLogs2, and DVIRDefectRemarks2 references DVIRDefects2 - and
        -- PostgreSQL refuses to detach a partition holding a row that another table
        -- references. Rows are set aside from a referencing table before the table it
        -- references, and filed back in the opposite order, so that no foreign key is
        -- ever violated. Every foreign key between these tables includes the partition
        -- key, so a referencing row always falls in the same period as the row it
        -- references, and is set aside with it. The function runs as one transaction,
        -- so a failure anywhere rolls all of this back with it.
        SELECT MIN(s)::DATE, (MAX(s) + partition_interval)::DATE
        INTO range_start, range_end
        FROM generate_series(
            CASE
                WHEN "PartitionInterval" = 'monthly' THEN date_trunc('month', min_date_utc)
                WHEN "PartitionInterval" = 'weekly' THEN date_trunc('week', min_date_utc)
                WHEN "PartitionInterval" = 'daily' THEN min_date_utc
            END,
            max_date_utc,
            partition_interval
        ) s;

        -- The adapter's partitioned tables, each placed after every table that
        -- references it through a foreign key: ordered by the length of the longest
        -- chain of such references leading to it. UNION rather than UNION ALL, so
        -- that a cycle of foreign keys cannot multiply the rows the walk produces.
        WITH RECURSIVE foreign_keys AS (
            SELECT DISTINCT con.confrelid AS referenced, con.conrelid AS referencing
            FROM pg_catalog.pg_constraint con
            WHERE con.contype = 'f'
              AND con.conrelid <> con.confrelid
              AND con.conrelid IN (SELECT partrelid FROM pg_catalog.pg_partitioned_table)
              AND con.confrelid IN (SELECT partrelid FROM pg_catalog.pg_partitioned_table)
        ), chains AS (
            SELECT pt.partrelid AS relid, 0 AS hops
            FROM pg_catalog.pg_partitioned_table pt
            UNION
            SELECT fk.referenced, c.hops + 1
            FROM foreign_keys fk
            JOIN chains c ON c.relid = fk.referencing
            WHERE c.hops < 32
        )
        SELECT array_agg(pc.relname::TEXT ORDER BY t.hops, pc.relname)
        INTO ordered_tables
        FROM (SELECT relid, MAX(hops) AS hops FROM chains GROUP BY relid) t
        JOIN pg_catalog.pg_class pc ON pc.oid = t.relid
        JOIN pg_catalog.pg_namespace pn ON pn.oid = pc.relnamespace
        WHERE pn.nspname = 'public';

        FOR table_index IN 1 .. COALESCE(array_length(ordered_tables, 1), 0) LOOP
            parent_table_stripped := ordered_tables[table_index];
            partition_name_default := parent_table_stripped || '_' || default_string;

            CONTINUE WHEN NOT EXISTS (SELECT 1 FROM pg_tables WHERE tablename = partition_name_default AND schemaname = 'public');

            -- Only a table RANGE-partitioned on a single date or timestamp column can
            -- hold rows for these periods. Any other partitioned table in public - one
            -- keyed on an expression or on a text column, say - is left exactly as this
            -- function has always left it.
            SELECT a.attname
            INTO partition_key_column
            FROM pg_catalog.pg_partitioned_table pt
            JOIN pg_catalog.pg_class c ON c.oid = pt.partrelid
            JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_catalog.pg_attribute a ON a.attrelid = pt.partrelid AND a.attnum = pt.partattrs[0]
            WHERE c.relname = parent_table_stripped
              AND n.nspname = 'public'
              AND pt.partstrat = 'r'
              AND pt.partnatts = 1
              AND a.atttypid IN ('date'::regtype, 'timestamp'::regtype, 'timestamptz'::regtype);

            CONTINUE WHEN partition_key_column IS NULL;

            EXECUTE 'SELECT EXISTS (SELECT 1 FROM ' || schema_prefix || '"' || partition_name_default
                    || '" WHERE "' || partition_key_column || '" >= $1 AND "' || partition_key_column || '" < $2)'
            INTO default_has_rows
            USING range_start, range_end;

            IF default_has_rows THEN
                stash_table := 'spManagePartitions_' || parent_table_stripped;
                EXECUTE 'DROP TABLE IF EXISTS pg_temp."' || stash_table || '"';
                EXECUTE 'CREATE TEMPORARY TABLE "' || stash_table || '" (LIKE ' || schema_prefix || '"'
                        || parent_table_stripped || '") ON COMMIT DROP';
                EXECUTE 'WITH moved AS (DELETE FROM ' || schema_prefix || '"' || partition_name_default || '" d'
                        || ' WHERE d."' || partition_key_column || '" >= $1 AND d."' || partition_key_column || '" < $2'
                        || ' RETURNING d.*) INSERT INTO pg_temp."' || stash_table || '" SELECT * FROM moved'
                USING range_start, range_end;
                GET DIAGNOSTICS rows_moved = ROW_COUNT;
                stashed_tables := array_append(stashed_tables, parent_table_stripped);
                RAISE NOTICE '> Set aside % row(s) from partition % that belong in partitions about to be created.', rows_moved, partition_name_default;
            END IF;
        END LOOP;

        -- Loop through the adapter's partitioned tables.
        --
        -- The 'public' filter is required, not defensive. This function assumes
        -- every partitioned table it finds lives in 'public': it strips that
        -- prefix by text replacement below and re-qualifies the name with it
        -- again when building the CREATE TABLE statement. A partitioned table in
        -- any other schema therefore survives the strip with its schema still
        -- attached, and the statement becomes
        -- CREATE TABLE public."other.Table_yyyyMM" PARTITION OF public."other.Table",
        -- which raises an error and aborts the whole function - taking the
        -- adapter's own partition creation down with it. Since the adapter runs
        -- this on every startup, and partition management cannot be disabled,
        -- another product partitioning a table in the same database would stop
        -- the adapter maintaining its partitions at all.
        FOR parent_table IN
            SELECT pt.partrelid::regclass AS parent_table
            FROM pg_catalog.pg_partitioned_table pt
            JOIN pg_catalog.pg_class pc ON pc.oid = pt.partrelid
            JOIN pg_catalog.pg_namespace pn ON pn.oid = pc.relnamespace
            WHERE pn.nspname = 'public'
        LOOP
            -- Strip quotes and schema prefix from the parent table name.
            SELECT REPLACE(parent_table, '"', '') INTO parent_table_stripped;
            SELECT REPLACE(parent_table_stripped, schema_prefix, '') INTO parent_table_stripped;

            -- Get the owner of the parent table. Matched on name alone, this
            -- would pick an arbitrary row when another schema holds a table of
            -- the same name, so it is qualified for the same reason as above.
            SELECT pg_user.usename
            INTO parent_table_owner
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_catalog.pg_user ON c.relowner = pg_user.usesysid
            WHERE c.relname = parent_table_stripped
              AND n.nspname = 'public';

			-- Loop through the partitions in the determined range.
			FOR partition_start IN
				SELECT generate_series(
					CASE
						WHEN "PartitionInterval" = 'monthly' THEN date_trunc('month', min_date_utc)
						WHEN "PartitionInterval" = 'weekly' THEN date_trunc('week', min_date_utc)
						WHEN "PartitionInterval" = 'daily' THEN min_date_utc
					END,
					max_date_utc,
					partition_interval
				)::DATE
			LOOP
				-- Set partition end date.
				partition_end := partition_start + partition_interval;

				-- Generate partition name based on interval.
				partition_name := parent_table_stripped || '_' || 
					CASE 
						WHEN "PartitionInterval" = 'monthly' THEN to_char(partition_start, 'YYYYMM')
						WHEN "PartitionInterval" = 'weekly' THEN to_char(partition_start, 'IYYY-"W"IW')
						WHEN "PartitionInterval" = 'daily' THEN to_char(partition_start, 'YYYYMMDD')
					END;

				-- Create partition table if it doesn't exist. Schema-qualified so
				-- that a like-named partition in another schema cannot make this
				-- skip a partition the adapter actually needs.
				IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE tablename = partition_name AND schemaname = 'public') THEN
					sql_string = 'CREATE TABLE ' || schema_prefix || '"' || partition_name 
								 || '" PARTITION OF ' || schema_prefix || '"' || parent_table_stripped || '" FOR VALUES FROM (''' 
								 || partition_start || ''') TO (''' || partition_end || ''');';

					EXECUTE sql_string;
					RAISE NOTICE '> Creating new partition: %. %', partition_name, sql_string;

					-- Assign ownership to match the parent table.
					sql_string = 'ALTER TABLE ' || schema_prefix || '"' || partition_name || '" OWNER TO ' || parent_table_owner || ';';
					EXECUTE sql_string;
					RAISE NOTICE '> Assigned ownership of partition % to %', partition_name, parent_table_owner;
				ELSE
					RAISE NOTICE 'Partition % already exists, skipping creation.', partition_name;
				END IF;
            END LOOP;

            -- Create default partition table.
            partition_name_default := parent_table_stripped || '_' || default_string;

            IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE tablename = partition_name_default AND schemaname = 'public') THEN
                sql_default = 'CREATE TABLE ' || schema_prefix || '"' || partition_name_default 
                              || '" PARTITION OF ' || schema_prefix || '"' || parent_table_stripped || '" DEFAULT;';
                EXECUTE sql_default;
                RAISE NOTICE '> Creating new partition: % ', sql_default;

                -- Assign ownership to match the parent table.
                sql_string = 'ALTER TABLE ' || schema_prefix || '"' || partition_name_default || '" OWNER TO ' || parent_table_owner || ';';
                EXECUTE sql_string;
                RAISE NOTICE '> Assigned ownership of partition % to %', partition_name_default, parent_table_owner;
            ELSE
                RAISE NOTICE 'Partition % already exists, skipping creation.', partition_name_default;
            END IF;

        END LOOP;

        -- File the rows set aside above into the partitions that now exist. They
        -- are inserted into the PARENT table, so that routing decides where each one
        -- lands, and a referenced table's rows go back before those of the tables
        -- that reference it.
        FOR table_index IN REVERSE COALESCE(array_length(stashed_tables, 1), 0) .. 1 LOOP
            parent_table_stripped := stashed_tables[table_index];
            stash_table := 'spManagePartitions_' || parent_table_stripped;
            EXECUTE 'INSERT INTO ' || schema_prefix || '"' || parent_table_stripped
                    || '" SELECT * FROM pg_temp."' || stash_table || '"';
            GET DIAGNOSTICS rows_moved = ROW_COUNT;
            EXECUTE 'DROP TABLE pg_temp."' || stash_table || '"';
            RAISE NOTICE '> Filed % row(s) set aside from the DEFAULT partition of % into its partitions.', rows_moved, parent_table_stripped;
        END LOOP;

        -- Log.
        end_time := CLOCK_TIMESTAMP();
        duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000;
        RAISE NOTICE 'Step 2 completed. Duration: % milliseconds', duration_string;
        start_time := end_time;
    END;

    -- ======================================================================================
    -- Log end of function execution.
    end_time := CLOCK_TIMESTAMP();
    duration_string := EXTRACT(EPOCH FROM (end_time - function_start_time)) * 1000 || ' milliseconds';
    RAISE NOTICE 'Function ''%'' executed successfully. Total Duration: %', function_name, duration_string;
END;
$BODY$;

ALTER FUNCTION public."spManagePartitions"(timestamp without time zone, text)
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spManagePartitions"(timestamp without time zone, text) TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spManagePartitions"(timestamp without time zone, text) FROM PUBLIC;

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Replace View: vwStatsForLevel2DBMaintenance
-- Reads each index ONCE. The pgstattuple_stats CTE called public.pgstattuple()
-- once for free_space and again for table_len, and pgstattuple reads the whole
-- index on every call - so each Level 2 run read every index in the schema
-- twice, with the other adapter services paused. Both columns now come from a
-- single call. The view's columns are unchanged, so it is replaced in place,
-- keeping its owner and privileges.
CREATE OR REPLACE VIEW public."vwStatsForLevel2DBMaintenance" AS
 WITH index_info AS (
         SELECT i.schemaname,
            i.relname AS tablename,
            i.indexrelname AS indexname,
            c.oid AS index_oid
           FROM (pg_stat_user_indexes i
             JOIN pg_class c ON ((i.indexrelid = c.oid)))
          WHERE (i.schemaname = 'public'::name)
        ), pgstattuple_stats AS (
         SELECT idx.index_oid,
            pst.free_space,
            pst.table_len
           FROM (index_info idx
             CROSS JOIN LATERAL public.pgstattuple((idx.index_oid)::regclass) pst)
        ), index_sizes AS (
         SELECT index_info.index_oid,
            pg_relation_size((index_info.index_oid)::regclass) AS total_size
           FROM index_info
        ), results AS (
         SELECT i.indexname,
            i.schemaname,
            i.tablename,
            pg_size_pretty(s.total_size) AS index_size,
            s.total_size AS index_size_bytes,
            ((ps.free_space)::double precision / (NULLIF(ps.table_len, 0))::double precision) AS bloat_ratio
           FROM ((index_info i
             JOIN pgstattuple_stats ps ON ((i.index_oid = ps.index_oid)))
             JOIN index_sizes s ON ((i.index_oid = s.index_oid)))
        ), orderedrows AS (
         SELECT results.schemaname AS "SchemaName",
            results.tablename AS "TableName",
            results.indexname AS "IndexName",
            results.index_size AS "IndexSizeText",
            results.index_size_bytes AS "IndexSize",
            COALESCE(results.bloat_ratio, (0.0)::double precision) AS "IndexBloatRatio"
           FROM results
          ORDER BY results.index_size_bytes DESC, results.bloat_ratio DESC
        )
 SELECT row_number() OVER () AS "RowId",
    "SchemaName",
    "TableName",
    "IndexName",
    "IndexSizeText",
    "IndexSize",
    "IndexBloatRatio"
   FROM orderedrows;

ALTER VIEW public."vwStatsForLevel2DBMaintenance" OWNER TO geotabadapter_client;


/*** [END] Part 2 of 3: Database Upgrades (tables, sequences, views) Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc")
SELECT "UpgradeDatabaseVersion", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "TMP_UpgradeDatabaseVersionTable";
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
/*** [END] Part 3 of 3: Database Version Update Above ***/

/*** [START] Transaction wrapper ***/
COMMIT;
/*** [END] Transaction wrapper ***/
