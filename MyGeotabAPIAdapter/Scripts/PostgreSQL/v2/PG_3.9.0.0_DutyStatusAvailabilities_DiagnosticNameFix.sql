-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.8.0.0 to version 3.9.0.0.
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
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('3.9.0.0');

DO $$	 
DECLARE 
    required_starting_database_version TEXT DEFAULT '3.8.0.0';
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
-- Create DutyStatusAvailabilities2 table:
CREATE TABLE public."DutyStatusAvailabilities2" (
    "id" bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DriverId" bigint NOT NULL,
    "CycleAvailabilities" text,
    "CycleDrivingTicks" bigint,
    "CycleTicks" bigint,
    "CycleRestTicks" bigint,
    "DrivingBreakDurationTicks" bigint,
    "DrivingTicks" bigint,
    "DutyTicks" bigint,
    "DutySinceCycleRestTicks" bigint,
    "Is16HourExemptionAvailable" boolean,
    "IsAdverseDrivingApplied" boolean,
    "IsAdverseDrivingExemptionAvailable" boolean,
    "IsOffDutyDeferralExemptionAvailable" boolean,
    "IsRailroadExemptionAvailable" boolean,
    "Recap" text,
    "RestTicks" bigint,
    "WorkdayTicks" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_DutyStatusAvailabilities2" PRIMARY KEY ("id")
);

ALTER TABLE IF EXISTS public."DutyStatusAvailabilities2"
    OWNER to geotabadapter_client;

CREATE INDEX "IX_DutyStatusAvailabilities2_DriverId" ON public."DutyStatusAvailabilities2" ("DriverId");

ALTER TABLE public."DutyStatusAvailabilities2"
	ADD CONSTRAINT "FK_DutyStatusAvailabilities2_Users2" FOREIGN KEY ("DriverId")
	REFERENCES public."Users2" (id);

GRANT ALL ON TABLE public."DutyStatusAvailabilities2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_DutyStatusAvailabilities2 table:
CREATE TABLE public."stg_DutyStatusAvailabilities2" (
    "id" bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DriverId" bigint NOT NULL,
    "CycleAvailabilities" text,
    "CycleDrivingTicks" bigint,
    "CycleTicks" bigint,
    "CycleRestTicks" bigint,
    "DrivingBreakDurationTicks" bigint,
    "DrivingTicks" bigint,
    "DutyTicks" bigint,
    "DutySinceCycleRestTicks" bigint,
    "Is16HourExemptionAvailable" boolean,
    "IsAdverseDrivingApplied" boolean,
    "IsAdverseDrivingExemptionAvailable" boolean,
    "IsOffDutyDeferralExemptionAvailable" boolean,
    "IsRailroadExemptionAvailable" boolean,
    "Recap" text,
    "RestTicks" bigint,
    "WorkdayTicks" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

ALTER TABLE IF EXISTS public."stg_DutyStatusAvailabilities2"
    OWNER to geotabadapter_client;

CREATE INDEX "IX_stg_DutyStatusAvailabilities2_id_RecordLastChangedUtc" ON public."stg_DutyStatusAvailabilities2" ("id" ASC, "RecordLastChangedUtc" ASC);

GRANT ALL ON TABLE public."stg_DutyStatusAvailabilities2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_DutyStatusAvailabilities2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_DutyStatusAvailabilities2"()
    RETURNS void
    LANGUAGE plpgsql
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_DutyStatusAvailabilities2 staging table to the
--   DutyStatusAvailabilities2 table and then truncates the staging table.
--
-- Notes:
--   - No transaction used as application should manage the transaction.
-- ==========================================================================================
BEGIN
    -- De-duplicate staging table by selecting the latest record per "id".
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("id") *
    FROM public."stg_DutyStatusAvailabilities2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id");

    -- Perform upsert.
    INSERT INTO public."DutyStatusAvailabilities2" AS d (
        "id", "GeotabId", "DriverId", "CycleAvailabilities", "CycleDrivingTicks",
        "CycleTicks", "CycleRestTicks", "DrivingBreakDurationTicks", "DrivingTicks",
        "DutyTicks", "DutySinceCycleRestTicks", "Is16HourExemptionAvailable",
        "IsAdverseDrivingApplied", "IsAdverseDrivingExemptionAvailable",
        "IsOffDutyDeferralExemptionAvailable", "IsRailroadExemptionAvailable",
        "Recap", "RestTicks", "WorkdayTicks", "RecordLastChangedUtc"
    )
    SELECT
        s."id", s."GeotabId", s."DriverId", s."CycleAvailabilities", s."CycleDrivingTicks",
        s."CycleTicks", s."CycleRestTicks", s."DrivingBreakDurationTicks", s."DrivingTicks",
        s."DutyTicks", s."DutySinceCycleRestTicks", s."Is16HourExemptionAvailable",
        s."IsAdverseDrivingApplied", s."IsAdverseDrivingExemptionAvailable",
        s."IsOffDutyDeferralExemptionAvailable", s."IsRailroadExemptionAvailable",
        s."Recap", s."RestTicks", s."WorkdayTicks", s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("id")
    DO UPDATE SET
		-- "id" excluded since it is subject of ON CONFLICT.
        "GeotabId" = EXCLUDED."GeotabId",
        "DriverId" = EXCLUDED."DriverId",
        "CycleAvailabilities" = EXCLUDED."CycleAvailabilities",
        "CycleDrivingTicks" = EXCLUDED."CycleDrivingTicks",
        "CycleTicks" = EXCLUDED."CycleTicks",
        "CycleRestTicks" = EXCLUDED."CycleRestTicks",
        "DrivingBreakDurationTicks" = EXCLUDED."DrivingBreakDurationTicks",
        "DrivingTicks" = EXCLUDED."DrivingTicks",
        "DutyTicks" = EXCLUDED."DutyTicks",
        "DutySinceCycleRestTicks" = EXCLUDED."DutySinceCycleRestTicks",
        "Is16HourExemptionAvailable" = EXCLUDED."Is16HourExemptionAvailable",
        "IsAdverseDrivingApplied" = EXCLUDED."IsAdverseDrivingApplied",
        "IsAdverseDrivingExemptionAvailable" = EXCLUDED."IsAdverseDrivingExemptionAvailable",
        "IsOffDutyDeferralExemptionAvailable" = EXCLUDED."IsOffDutyDeferralExemptionAvailable",
        "IsRailroadExemptionAvailable" = EXCLUDED."IsRailroadExemptionAvailable",
        "Recap" = EXCLUDED."Recap",
        "RestTicks" = EXCLUDED."RestTicks",
        "WorkdayTicks" = EXCLUDED."WorkdayTicks",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId" OR
        d."DriverId" IS DISTINCT FROM EXCLUDED."DriverId" OR
        d."CycleAvailabilities" IS DISTINCT FROM EXCLUDED."CycleAvailabilities" OR
        d."CycleDrivingTicks" IS DISTINCT FROM EXCLUDED."CycleDrivingTicks" OR
        d."CycleTicks" IS DISTINCT FROM EXCLUDED."CycleTicks" OR
        d."CycleRestTicks" IS DISTINCT FROM EXCLUDED."CycleRestTicks" OR
        d."DrivingBreakDurationTicks" IS DISTINCT FROM EXCLUDED."DrivingBreakDurationTicks" OR
        d."DrivingTicks" IS DISTINCT FROM EXCLUDED."DrivingTicks" OR
        d."DutyTicks" IS DISTINCT FROM EXCLUDED."DutyTicks" OR
        d."DutySinceCycleRestTicks" IS DISTINCT FROM EXCLUDED."DutySinceCycleRestTicks" OR
        d."Is16HourExemptionAvailable" IS DISTINCT FROM EXCLUDED."Is16HourExemptionAvailable" OR
        d."IsAdverseDrivingApplied" IS DISTINCT FROM EXCLUDED."IsAdverseDrivingApplied" OR
        d."IsAdverseDrivingExemptionAvailable" IS DISTINCT FROM EXCLUDED."IsAdverseDrivingExemptionAvailable" OR
        d."IsOffDutyDeferralExemptionAvailable" IS DISTINCT FROM EXCLUDED."IsOffDutyDeferralExemptionAvailable" OR
        d."IsRailroadExemptionAvailable" IS DISTINCT FROM EXCLUDED."IsRailroadExemptionAvailable" OR
        d."Recap" IS DISTINCT FROM EXCLUDED."Recap" OR
        d."RestTicks" IS DISTINCT FROM EXCLUDED."RestTicks" OR
        d."WorkdayTicks" IS DISTINCT FROM EXCLUDED."WorkdayTicks";
        -- RecordLastChangedUtc not evaluated as it should never match.

    -- Clear staging table.
    TRUNCATE TABLE public."stg_DutyStatusAvailabilities2";

    -- Clean up temporary table.
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";

EXCEPTION
    WHEN OTHERS THEN
        -- Ensure temporary table cleanup on error.
        DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
        
        -- Re-raise the original error to be caught by the calling application, if necessary.
        RAISE;
END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_DutyStatusAvailabilities2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_DutyStatusAvailabilities2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_DutyStatusAvailabilities2"() FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Adjust length of DiagnosticName column in stg_Diagnostics2 and Diagnostics2 tables:
ALTER TABLE public."stg_Diagnostics2"
ALTER COLUMN "DiagnosticName" TYPE TEXT,
ALTER COLUMN "DiagnosticName" SET NOT NULL;
ALTER TABLE public."Diagnostics2"
ALTER COLUMN "DiagnosticName" TYPE TEXT,
ALTER COLUMN "DiagnosticName" SET NOT NULL;
/*** [END] Part 2 of 3: Database Upgrades (tables, sequences, views) Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc")
SELECT "UpgradeDatabaseVersion", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "TMP_UpgradeDatabaseVersionTable";
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
/*** [END] Part 3 of 3: Database Version Update Above ***/
