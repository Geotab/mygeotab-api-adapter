-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.9.0.0 to version 3.10.0.0.
--
-- NOTES: 
--   1: This script cannot be run against any database version other than that 
--		specified above. 
--   2: Be sure to connect to the "geotabadapterdb" before executing. 
-- ================================================================================


/*** [START] Part 1 of 3: Database Version Validation Below ***/ 
-- Store upgrade database version in a temporary table.
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
CREATE TEMPORARY TABLE "TMP_UpgradeDatabaseVersionTable" ("UpgradeDatabaseVersion" VARCHAR(50));
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('3.10.0.0');

DO $$	 
DECLARE 
    required_starting_database_version TEXT DEFAULT '3.9.0.0';
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
-- Add columns for new properties to FaultData2 table:
ALTER TABLE public."FaultData2"
ADD COLUMN "EffectOnComponent" TEXT;

ALTER TABLE public."FaultData2"
ADD COLUMN "FaultDescription" TEXT;

ALTER TABLE public."FaultData2"
ADD COLUMN "FlashCodeId" character varying(255);

ALTER TABLE public."FaultData2"
ADD COLUMN "FlashCodeName" character varying(255);

ALTER TABLE public."FaultData2"
ADD COLUMN "Recommendation" TEXT;

ALTER TABLE public."FaultData2"
ADD COLUMN "RiskOfBreakdown" double precision;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create FuelAndEnergyUsed2 table:
CREATE TABLE public."FuelAndEnergyUsed2"(
	"id" uuid NOT NULL,
	"GeotabId" character varying(50) NOT NULL,
	"DateTime" timestamp without time zone NOT NULL,
	"DeviceId" bigint NOT NULL,
	"TotalEnergyUsedKwh" double precision NULL,
	"TotalFuelUsed" double precision NULL,
	"TotalIdlingEnergyUsedKwh" double precision NULL,
	"TotalIdlingFuelUsedL" double precision NULL,
	"Version" bigint NULL,
	"RecordLastChangedUtc" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_FuelAndEnergyUsed2" PRIMARY KEY ("DateTime", "id")
) PARTITION BY RANGE ("DateTime");

ALTER TABLE IF EXISTS public."FuelAndEnergyUsed2"
    OWNER to geotabadapter_client;

CREATE INDEX "IX_FuelAndEnergyUsed2_DeviceId" ON public."FuelAndEnergyUsed2" ("DeviceId");

CREATE INDEX "IX_FuelAndEnergyUsed2_RecordLastChangedUtc" ON public."FuelAndEnergyUsed2" ("RecordLastChangedUtc");

CREATE INDEX "IX_FuelAndEnergyUsed2_DateTime_Device" ON public."FuelAndEnergyUsed2" ("DateTime" ASC, "DeviceId" ASC);

ALTER TABLE public."FuelAndEnergyUsed2"
    ADD CONSTRAINT "FK_FuelAndEnergyUsed2_Devices2" FOREIGN KEY ("DeviceId")
    REFERENCES public."Devices2" ("id");
	
GRANT ALL ON TABLE public."FuelAndEnergyUsed2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_FuelAndEnergyUsed2 table:
CREATE TABLE public."stg_FuelAndEnergyUsed2"(
	"id" uuid NOT NULL,
	"GeotabId" character varying(50) NOT NULL,
	"DateTime" timestamp without time zone NOT NULL,
	"DeviceId" bigint NOT NULL,
	"TotalEnergyUsedKwh" double precision NULL,
	"TotalFuelUsed" double precision NULL,
	"TotalIdlingEnergyUsedKwh" double precision NULL,
	"TotalIdlingFuelUsedL" double precision NULL,
	"Version" bigint NULL,
	"RecordLastChangedUtc" timestamp without time zone NOT NULL
);

ALTER TABLE IF EXISTS public."stg_FuelAndEnergyUsed2"
    OWNER to geotabadapter_client;

CREATE INDEX "IX_stg_FuelAndEnergyUsed2_id_RecordLastChangedUtc" ON public."stg_FuelAndEnergyUsed2" ("id" ASC, "RecordLastChangedUtc" ASC);

GRANT ALL ON TABLE public."stg_FuelAndEnergyUsed2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_FuelAndEnergyUsed2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_FuelAndEnergyUsed2"()
    RETURNS void
    LANGUAGE plpgsql
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_FuelAndEnergyUsed2 staging table to the FuelAndEnergyUsed2
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

    -- De-duplicate staging table by selecting the latest record per "id".
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("id") *
    FROM public."stg_FuelAndEnergyUsed2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;

    -- Add an index to the temporary table on the column used for conflict resolution.
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id");

    -- Identify records where DateTime has changed.
    INSERT INTO "TMP_MovedRecordIds" ("id")
    SELECT s."id"
    FROM "TMP_DeduplicatedStaging" s
    INNER JOIN public."FuelAndEnergyUsed2" d ON s."id" = d."id"
    WHERE s."DateTime" IS DISTINCT FROM d."DateTime";

    -- Delete the old versions of these records from the target table.
    DELETE FROM public."FuelAndEnergyUsed2" AS d
    USING "TMP_MovedRecordIds" m
    WHERE d."id" = m."id";

    -- Perform upsert.
    -- Inserts new records AND records whose DateTime changed (deleted above).
    INSERT INTO public."FuelAndEnergyUsed2" AS d 
	(
        "id", 
		"GeotabId", 
		"DateTime", 
		"DeviceId", 
		"TotalEnergyUsedKwh", 
		"TotalFuelUsed",
        "TotalIdlingEnergyUsedKwh", 
		"TotalIdlingFuelUsedL", 
		"Version", 
		"RecordLastChangedUtc"
    )
    SELECT
        s."id", 
		s."GeotabId", 
		s."DateTime", 
		s."DeviceId", 
		s."TotalEnergyUsedKwh", 
		s."TotalFuelUsed",
        s."TotalIdlingEnergyUsedKwh", 
		s."TotalIdlingFuelUsedL", 
		s."Version", 
		s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("DateTime", "id")
    DO UPDATE SET
		-- "id" and "DateTime" excluded since they are subject of ON CONFLICT.
		-- If only "DateTime" changed, the original record will have been deleted
		-- and a new one will be inserted instead of updating the existing record.		
        "GeotabId" = EXCLUDED."GeotabId",
        "DeviceId" = EXCLUDED."DeviceId",
        "TotalEnergyUsedKwh" = EXCLUDED."TotalEnergyUsedKwh",
        "TotalFuelUsed" = EXCLUDED."TotalFuelUsed",
        "TotalIdlingEnergyUsedKwh" = EXCLUDED."TotalIdlingEnergyUsedKwh",
        "TotalIdlingFuelUsedL" = EXCLUDED."TotalIdlingFuelUsedL",
        "Version" = EXCLUDED."Version",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc" -- Always update the timestamp
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId" OR
        d."DeviceId" IS DISTINCT FROM EXCLUDED."DeviceId" OR
        d."TotalEnergyUsedKwh" IS DISTINCT FROM EXCLUDED."TotalEnergyUsedKwh" OR
        d."TotalFuelUsed" IS DISTINCT FROM EXCLUDED."TotalFuelUsed" OR
        d."TotalIdlingEnergyUsedKwh" IS DISTINCT FROM EXCLUDED."TotalIdlingEnergyUsedKwh" OR
        d."TotalIdlingFuelUsedL" IS DISTINCT FROM EXCLUDED."TotalIdlingFuelUsedL" OR
        d."Version" IS DISTINCT FROM EXCLUDED."Version";
        -- RecordLastChangedUtc not evaluated as it should never match.

    -- Clear staging table.
    TRUNCATE TABLE public."stg_FuelAndEnergyUsed2";

    -- Clean up temporary tables.
	DROP TABLE IF EXISTS "TMP_MovedRecordIds";
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";

EXCEPTION
    WHEN OTHERS THEN
        -- Ensure temporary tables are cleaned up on error.
		DROP TABLE IF EXISTS "TMP_MovedRecordIds";
        DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
        
        -- Re-raise the original error to be caught by the calling application.
        RAISE;
END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_FuelAndEnergyUsed2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_FuelAndEnergyUsed2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_FuelAndEnergyUsed2"() FROM PUBLIC;
/*** [END] Part 2 of 3: Database Upgrades (tables, sequences, views) Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc")
SELECT "UpgradeDatabaseVersion", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "TMP_UpgradeDatabaseVersionTable";
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
/*** [END] Part 3 of 3: Database Version Update Above ***/
