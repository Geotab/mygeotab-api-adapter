-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.13.0.0 to version 3.14.0.0.
--
-- NOTES: 
--   1: This script cannot be run against any database version other than that 
--		specified above. 
--   2: Be sure to connect to the "geotabadapterdb" before executing. 
-- ================================================================================


/*** [START] Part 1 of 3: Database Version Validation Below ***/ 
-- Store upgrade database version in a temporary table.
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
CREATE TEMPORARY TABLE "TMP_UpgradeDatabaseVersionTable" ("UpgradeDatabaseVersion" character varying(50));
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('3.14.0.0');

DO $$	 
DECLARE 
    required_starting_database_version TEXT DEFAULT '3.13.0.0';
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
        -- Loop through all partitioned tables.
        FOR parent_table IN
            SELECT pt.partrelid::regclass AS parent_table FROM pg_catalog.pg_partitioned_table pt
        LOOP
            -- Strip quotes and schema prefix from the parent table name.
            SELECT REPLACE(parent_table, '"', '') INTO parent_table_stripped;
            SELECT REPLACE(parent_table_stripped, schema_prefix, '') INTO parent_table_stripped;

            -- Get the owner of the parent table.
            SELECT pg_user.usename 
            INTO parent_table_owner
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_catalog.pg_user ON c.relowner = pg_user.usesysid
            WHERE c.relname = parent_table_stripped;

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

				-- Create partition table if it doesn't exist.
				IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE tablename = partition_name) THEN
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

            IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE tablename = partition_name_default) THEN
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
/*** [END] Part 2 of 3: Database Upgrades (tables, sequences, views) Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc")
SELECT "UpgradeDatabaseVersion", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "TMP_UpgradeDatabaseVersionTable";
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
/*** [END] Part 3 of 3: Database Version Update Above ***/
