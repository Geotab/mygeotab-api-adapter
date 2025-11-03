-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.12.0.0 to version 3.13.0.0.
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
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('3.13.0.0');

DO $$	 
DECLARE 
    required_starting_database_version TEXT DEFAULT '3.12.0.0';
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
-- Create upd_DVIRDefectUpdates2 table:
CREATE TABLE public."upd_DVIRDefectUpdates2" (
    "id" bigint NOT NULL,
    "DVIRLogId" uuid NOT NULL,
    "DVIRDefectId" uuid NOT NULL,
    "RepairDateTimeUtc" timestamp without time zone NULL,
    "RepairStatusId" smallint NULL,
    "RepairUserId" bigint NULL,
    "Remark" text NULL,
    "RemarkDateTimeUtc" timestamp without time zone NULL,
    "RemarkUserId" bigint NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);

ALTER TABLE public."upd_DVIRDefectUpdates2" OWNER TO geotabadapter_client;

CREATE SEQUENCE public."upd_DVIRDefectUpdates2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER SEQUENCE public."upd_DVIRDefectUpdates2_id_seq" OWNER TO geotabadapter_client;

ALTER SEQUENCE public."upd_DVIRDefectUpdates2_id_seq" OWNED BY public."upd_DVIRDefectUpdates2"."id";

ALTER TABLE ONLY public."upd_DVIRDefectUpdates2" ALTER COLUMN "id" SET DEFAULT nextval('public."upd_DVIRDefectUpdates2_id_seq"'::regclass);

ALTER TABLE ONLY public."upd_DVIRDefectUpdates2"
    ADD CONSTRAINT "PK_upd_DVIRDefectUpdates2" PRIMARY KEY ("id");


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create fail_DVIRDefectUpdateFailures2 table:
CREATE TABLE public."fail_DVIRDefectUpdateFailures2" (
    "id" bigint NOT NULL,
    "DVIRDefectUpdateId" bigint NOT NULL,
    "DVIRLogId" uuid NOT NULL,
    "DVIRDefectId" uuid NOT NULL,
    "RepairDateTimeUtc" timestamp without time zone NULL,
    "RepairStatusId" smallint NULL,
    "RepairUserId" bigint NULL,
    "Remark" text NULL,
    "RemarkDateTimeUtc" timestamp without time zone NULL,
    "RemarkUserId" bigint NULL,
    "FailureMessage" text NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);

ALTER TABLE public."fail_DVIRDefectUpdateFailures2" OWNER TO geotabadapter_client;

CREATE SEQUENCE public."fail_DVIRDefectUpdateFailures2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER SEQUENCE public."fail_DVIRDefectUpdateFailures2_id_seq" OWNER TO geotabadapter_client;

ALTER SEQUENCE public."fail_DVIRDefectUpdateFailures2_id_seq" OWNED BY public."fail_DVIRDefectUpdateFailures2"."id";

ALTER TABLE ONLY public."fail_DVIRDefectUpdateFailures2" ALTER COLUMN "id" SET DEFAULT nextval('public."fail_DVIRDefectUpdateFailures2_id_seq"'::regclass);

ALTER TABLE ONLY public."fail_DVIRDefectUpdateFailures2"
    ADD CONSTRAINT "PK_fail_DVIRDefectUpdateFailures2" PRIMARY KEY ("id");


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Modify spMerge_stg_Trips2 function - Add missing Distance column to UPDATE statement:
CREATE OR REPLACE FUNCTION public."spMerge_stg_Trips2"(
	)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description: 
--	 Upserts records from the stg_Trips2 staging table to the Trips2 table and then
--	 truncates the staging table. 
--
-- Notes:
--   - No transaction used as application should manage the transaction.
-- ==========================================================================================
BEGIN
    -- De-duplicate staging table by selecting the latest record per natural key (DeviceId, Start).
	-- Uses DISTINCT ON to keep only the latest record per DeviceId + Start.
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
	SELECT DISTINCT ON ("DeviceId", "Start") *
	FROM public."stg_Trips2"
	ORDER BY "DeviceId", "Start", "RecordLastChangedUtc" DESC;
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("DeviceId", "Start");

    -- Perform upsert.
    INSERT INTO public."Trips2" AS d (
        "GeotabId",
        "AfterHoursDistance",
        "AfterHoursDrivingDurationTicks",
        "AfterHoursEnd",
        "AfterHoursStart",
        "AfterHoursStopDurationTicks",
        "AverageSpeed",
        "DeletedDateTime",
        "DeviceId",
        "Distance",
        "DriverId",
        "DrivingDurationTicks",
        "IdlingDurationTicks",
        "MaximumSpeed",
        "NextTripStart",
        "SpeedRange1",
        "SpeedRange1DurationTicks",
        "SpeedRange2",
        "SpeedRange2DurationTicks",
        "SpeedRange3",
        "SpeedRange3DurationTicks",
        "Start",
        "Stop",
        "StopDurationTicks",
        "StopPointX",
        "StopPointY",
        "WorkDistance",
        "WorkDrivingDurationTicks",
        "WorkStopDurationTicks",
        "EntityStatus",
        "RecordLastChangedUtc"
    )
    SELECT 
        s."GeotabId",
        s."AfterHoursDistance",
        s."AfterHoursDrivingDurationTicks",
        s."AfterHoursEnd",
        s."AfterHoursStart",
        s."AfterHoursStopDurationTicks",
        s."AverageSpeed",
        s."DeletedDateTime",
        s."DeviceId",
        s."Distance",
        s."DriverId",
        s."DrivingDurationTicks",
        s."IdlingDurationTicks",
        s."MaximumSpeed",
        s."NextTripStart",
        s."SpeedRange1",
        s."SpeedRange1DurationTicks",
        s."SpeedRange2",
        s."SpeedRange2DurationTicks",
        s."SpeedRange3",
        s."SpeedRange3DurationTicks",
        s."Start",
        s."Stop",
        s."StopDurationTicks",
        s."StopPointX",
        s."StopPointY",
        s."WorkDistance",
        s."WorkDrivingDurationTicks",
        s."WorkStopDurationTicks",
        s."EntityStatus",
        s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("DeviceId", "Start", "EntityStatus")
    DO UPDATE SET
		-- "id" is database-generated on insert.
		-- "GeotabId" is NOT a unique identifier for a Trip and each update
		-- for a Trip will have a different "GeotabId".
		-- "DeviceId", "Start" and "EntityStatus" excluded since they are subject of ON CONFLICT.
        "GeotabId" = EXCLUDED."GeotabId",
        "AfterHoursDistance" = EXCLUDED."AfterHoursDistance",
        "AfterHoursDrivingDurationTicks" = EXCLUDED."AfterHoursDrivingDurationTicks",
        "AfterHoursEnd" = EXCLUDED."AfterHoursEnd",
        "AfterHoursStart" = EXCLUDED."AfterHoursStart",
        "AfterHoursStopDurationTicks" = EXCLUDED."AfterHoursStopDurationTicks",
        "AverageSpeed" = EXCLUDED."AverageSpeed",
        "DeletedDateTime" = EXCLUDED."DeletedDateTime",
		"Distance" = EXCLUDED."Distance",
        "DriverId" = EXCLUDED."DriverId",
        "DrivingDurationTicks" = EXCLUDED."DrivingDurationTicks",
        "IdlingDurationTicks" = EXCLUDED."IdlingDurationTicks",
        "MaximumSpeed" = EXCLUDED."MaximumSpeed",
        "NextTripStart" = EXCLUDED."NextTripStart",
        "SpeedRange1" = EXCLUDED."SpeedRange1",
        "SpeedRange1DurationTicks" = EXCLUDED."SpeedRange1DurationTicks",
        "SpeedRange2" = EXCLUDED."SpeedRange2",
        "SpeedRange2DurationTicks" = EXCLUDED."SpeedRange2DurationTicks",
        "SpeedRange3" = EXCLUDED."SpeedRange3",
        "SpeedRange3DurationTicks" = EXCLUDED."SpeedRange3DurationTicks",
        "Stop" = EXCLUDED."Stop",
        "StopDurationTicks" = EXCLUDED."StopDurationTicks",
        "StopPointX" = EXCLUDED."StopPointX",
        "StopPointY" = EXCLUDED."StopPointY",
        "WorkDistance" = EXCLUDED."WorkDistance",
        "WorkDrivingDurationTicks" = EXCLUDED."WorkDrivingDurationTicks",
        "WorkStopDurationTicks" = EXCLUDED."WorkStopDurationTicks",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId"
        OR d."AfterHoursDistance" IS DISTINCT FROM EXCLUDED."AfterHoursDistance"
        OR d."AfterHoursDrivingDurationTicks" IS DISTINCT FROM EXCLUDED."AfterHoursDrivingDurationTicks"
        OR d."AfterHoursEnd" IS DISTINCT FROM EXCLUDED."AfterHoursEnd"
        OR d."AfterHoursStart" IS DISTINCT FROM EXCLUDED."AfterHoursStart"
        OR d."AfterHoursStopDurationTicks" IS DISTINCT FROM EXCLUDED."AfterHoursStopDurationTicks"
        OR d."AverageSpeed" IS DISTINCT FROM EXCLUDED."AverageSpeed"
        OR d."DeletedDateTime" IS DISTINCT FROM EXCLUDED."DeletedDateTime"
        OR d."Distance" IS DISTINCT FROM EXCLUDED."Distance"
        OR d."DriverId" IS DISTINCT FROM EXCLUDED."DriverId"
        OR d."DrivingDurationTicks" IS DISTINCT FROM EXCLUDED."DrivingDurationTicks"
        OR d."IdlingDurationTicks" IS DISTINCT FROM EXCLUDED."IdlingDurationTicks"
        OR d."MaximumSpeed" IS DISTINCT FROM EXCLUDED."MaximumSpeed"
        OR d."NextTripStart" IS DISTINCT FROM EXCLUDED."NextTripStart"
        OR d."SpeedRange1" IS DISTINCT FROM EXCLUDED."SpeedRange1"
        OR d."SpeedRange1DurationTicks" IS DISTINCT FROM EXCLUDED."SpeedRange1DurationTicks"
        OR d."SpeedRange2" IS DISTINCT FROM EXCLUDED."SpeedRange2"
        OR d."SpeedRange2DurationTicks" IS DISTINCT FROM EXCLUDED."SpeedRange2DurationTicks"
        OR d."SpeedRange3" IS DISTINCT FROM EXCLUDED."SpeedRange3"
        OR d."SpeedRange3DurationTicks" IS DISTINCT FROM EXCLUDED."SpeedRange3DurationTicks"
        OR d."Stop" IS DISTINCT FROM EXCLUDED."Stop"
        OR d."StopDurationTicks" IS DISTINCT FROM EXCLUDED."StopDurationTicks"
        OR d."StopPointX" IS DISTINCT FROM EXCLUDED."StopPointX"
        OR d."StopPointY" IS DISTINCT FROM EXCLUDED."StopPointY"
        OR d."WorkDistance" IS DISTINCT FROM EXCLUDED."WorkDistance"
        OR d."WorkDrivingDurationTicks" IS DISTINCT FROM EXCLUDED."WorkDrivingDurationTicks"
        OR d."WorkStopDurationTicks" IS DISTINCT FROM EXCLUDED."WorkStopDurationTicks";
        -- RecordLastChangedUtc not evaluated as it should never match.

    -- Clear staging table.
    TRUNCATE TABLE public."stg_Trips2";

    -- Drop temporary table.
    DROP TABLE "TMP_DeduplicatedStaging";
END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_Trips2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_Trips2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_Trips2"() FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Change ownership of all tables, including partitions, to geotabadapter_client
-- to capture any that were not set previously. 
DO $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN 
        SELECT schemaname, tablename 
        FROM pg_tables 
        WHERE schemaname = 'public'
    LOOP
        EXECUTE 'ALTER TABLE public.' || quote_ident(r.tablename) || ' OWNER TO geotabadapter_client';
        RAISE NOTICE 'Changed owner of table: %', r.tablename;
    END LOOP;
END $$;
/*** [END] Part 2 of 3: Database Upgrades (tables, sequences, views) Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc")
SELECT "UpgradeDatabaseVersion", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "TMP_UpgradeDatabaseVersionTable";
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
/*** [END] Part 3 of 3: Database Version Update Above ***/
