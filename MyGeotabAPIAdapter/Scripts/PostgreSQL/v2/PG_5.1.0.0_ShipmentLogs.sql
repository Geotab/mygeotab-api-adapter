-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION:
--   The purpose of this script is to upgrade the MyGeotab API Adapter database
--   from version 5.0.0.0 to version 5.1.0.0.
--
--   Changes:
--   1. Adds the ShipmentLogs2 partitioned table (including the
--      FK_ShipmentLogs2_Devices2 foreign key and supporting indexes) to support the
--      new ShipmentLog feed.
--   2. Adds the stg_ShipmentLogs2 staging table.
--   3. Adds the spMerge_stg_ShipmentLogs2 function used to upsert staged
--      ShipmentLog records into the ShipmentLogs2 table.
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
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('5.1.0.0');

DO $$
DECLARE
    required_starting_database_version TEXT DEFAULT '5.0.0.0';
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
-- Create ShipmentLogs2 table:
CREATE TABLE public."ShipmentLogs2" (
    "id" uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone,
    "ActiveTo" timestamp without time zone,
    "Commodity" character varying(255),
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" bigint,
    "DocumentNumber" text,
    "DriverId" bigint,
    "ShipperName" text,
    "Version" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_ShipmentLogs2" PRIMARY KEY ("DateTime", "id")
) PARTITION BY RANGE ("DateTime");

ALTER TABLE IF EXISTS public."ShipmentLogs2"
    OWNER TO geotabadapter_client;

ALTER TABLE public."ShipmentLogs2"
    ADD CONSTRAINT "FK_ShipmentLogs2_Devices2" FOREIGN KEY ("DeviceId")
        REFERENCES public."Devices2" ("id");

-- Index on "id" alone to support the id-based mover-detection join and delete in
-- spMerge_stg_ShipmentLogs2 (the primary key leads with "DateTime", so it cannot
-- service id-only lookups).
CREATE INDEX "IX_ShipmentLogs2_Id" ON public."ShipmentLogs2" USING btree ("id") WITH (deduplicate_items='true');

CREATE INDEX "IX_ShipmentLogs2_DeviceId" ON public."ShipmentLogs2" ("DeviceId");

CREATE INDEX "IX_ShipmentLogs2_DriverId" ON public."ShipmentLogs2" ("DriverId");

CREATE INDEX "IX_ShipmentLogs2_RecordLastChangedUtc" ON public."ShipmentLogs2" ("RecordLastChangedUtc");

CREATE INDEX "IX_ShipmentLogs2_DateTime_Device" ON public."ShipmentLogs2" ("DateTime", "DeviceId");

CREATE INDEX "IX_ShipmentLogs2_DateTime_Driver" ON public."ShipmentLogs2" ("DateTime" ASC, "DriverId" ASC);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_ShipmentLogs2 table:
CREATE TABLE public."stg_ShipmentLogs2" (
    "id" uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone,
    "ActiveTo" timestamp without time zone,
    "Commodity" character varying(255),
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" bigint,
    "DocumentNumber" text,
    "DriverId" bigint,
    "ShipperName" text,
    "Version" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

ALTER TABLE IF EXISTS public."stg_ShipmentLogs2"
    OWNER TO geotabadapter_client;
CREATE INDEX "IX_stg_ShipmentLogs2_id_RecordLastChangedUtc" ON public."stg_ShipmentLogs2" ("id" ASC, "RecordLastChangedUtc" ASC);
GRANT ALL ON TABLE public."stg_ShipmentLogs2" TO geotabadapter_client;

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_ShipmentLogs2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_ShipmentLogs2"()
    RETURNS void
    LANGUAGE plpgsql
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_ShipmentLogs2 staging table to the ShipmentLogs2
--   table and then truncates the staging table. Handles changes to the DateTime
--   (partitioning key) by deleting the existing record and inserting the new version.
--
-- Notes:
--   - Uses a multi-step process (DELETE movers + INSERT ON CONFLICT) within a transaction block.
-- ==========================================================================================
BEGIN
    -- Create temporary table to store IDs of any records where DateTime has changed.
    DROP TABLE IF EXISTS "TMP_MovedRecordIds";
    CREATE TEMP TABLE "TMP_MovedRecordIds" (
        "id" uuid PRIMARY KEY
	);

    -- De-duplicate staging table by selecting the latest record per "id" using
	-- DISTINCT ON, ordered by "RecordLastChangedUtc" descending.
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("id") *
    FROM public."stg_ShipmentLogs2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;

    -- Add an index to the temporary table on the column used for conflict resolution.
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id");

    -- Identify records where DateTime has changed.
    INSERT INTO "TMP_MovedRecordIds" ("id")
    SELECT s."id"
    FROM "TMP_DeduplicatedStaging" s
    INNER JOIN public."ShipmentLogs2" d ON s."id" = d."id"
    WHERE s."DateTime" IS DISTINCT FROM d."DateTime";

    -- Delete the old versions of these "mover" records from the target table.
    DELETE FROM public."ShipmentLogs2" AS d
    USING "TMP_MovedRecordIds" m
    WHERE d."id" = m."id";

    -- Perform upsert.
    -- Inserts new records AND records whose DateTime changed (deleted above).
    INSERT INTO public."ShipmentLogs2" AS d (
        "id",
		"GeotabId",
		"ActiveFrom",
		"ActiveTo",
		"Commodity",
		"DateTime",
		"DeviceId",
		"DocumentNumber",
		"DriverId",
		"ShipperName",
		"Version",
		"RecordLastChangedUtc"
    )
    SELECT
        s."id",
		s."GeotabId",
		s."ActiveFrom",
		s."ActiveTo",
		s."Commodity",
		s."DateTime",
		s."DeviceId",
		s."DocumentNumber",
		s."DriverId",
		s."ShipperName",
		s."Version",
		s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("DateTime", "id")
	DO UPDATE SET
		-- "id" and "DateTime" excluded since they are subject of ON CONFLICT.
		-- If only "DateTime" changed, the original record will have been deleted
		-- and a new one will be inserted instead of updating the existing record.
        "GeotabId" = EXCLUDED."GeotabId",
        "ActiveFrom" = EXCLUDED."ActiveFrom",
        "ActiveTo" = EXCLUDED."ActiveTo",
        "Commodity" = EXCLUDED."Commodity",
        "DeviceId" = EXCLUDED."DeviceId",
        "DocumentNumber" = EXCLUDED."DocumentNumber",
        "DriverId" = EXCLUDED."DriverId",
        "ShipperName" = EXCLUDED."ShipperName",
        "Version" = EXCLUDED."Version",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId" OR
		d."ActiveFrom" IS DISTINCT FROM EXCLUDED."ActiveFrom" OR
		d."ActiveTo" IS DISTINCT FROM EXCLUDED."ActiveTo" OR
		d."Commodity" IS DISTINCT FROM EXCLUDED."Commodity" OR
		d."DeviceId" IS DISTINCT FROM EXCLUDED."DeviceId" OR
		d."DocumentNumber" IS DISTINCT FROM EXCLUDED."DocumentNumber" OR
		d."DriverId" IS DISTINCT FROM EXCLUDED."DriverId" OR
		d."ShipperName" IS DISTINCT FROM EXCLUDED."ShipperName" OR
		d."Version" IS DISTINCT FROM EXCLUDED."Version";
		-- RecordLastChangedUtc not evaluated as it should never match.

    -- Clear staging table.
    TRUNCATE TABLE public."stg_ShipmentLogs2";

    -- Clean up temporary tables.
	DROP TABLE IF EXISTS "TMP_MovedRecordIds";
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";

EXCEPTION
	WHEN OTHERS THEN
		-- Ensure temporary table cleanup on error.
		DROP TABLE IF EXISTS "TMP_MovedRecordIds";
		DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";

		-- Re-raise the original error to be caught by the calling application, if necessary.
		RAISE;
END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_ShipmentLogs2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_ShipmentLogs2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_ShipmentLogs2"() FROM PUBLIC;

/*** [END] Part 2 of 3: Database Upgrades Above ***/



/*** [START] Part 3 of 3: Database Version Update Below ***/
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc")
SELECT "UpgradeDatabaseVersion", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "TMP_UpgradeDatabaseVersionTable";
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
/*** [END] Part 3 of 3: Database Version Update Above ***/
