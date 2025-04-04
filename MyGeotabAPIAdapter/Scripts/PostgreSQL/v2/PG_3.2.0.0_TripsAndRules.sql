-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.1.0.0 to version 3.2.0.0.
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
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('3.2.0.0');

DO $$	 
DECLARE 
    required_starting_database_version TEXT DEFAULT '3.1.0.0';
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
-- Remove indexes that aren't needed and could cause issues.
ALTER TABLE public."stg_Devices2" DROP CONSTRAINT IF EXISTS "PK_stg_Devices2";
ALTER TABLE public."stg_Users2" DROP CONSTRAINT IF EXISTS "PK_stg_Users2";
ALTER TABLE public."stg_Zones2" DROP CONSTRAINT IF EXISTS "PK_stg_Zones2";

 
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Rules2 table:
CREATE TABLE public."Rules2"
(
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone,
    "ActiveTo" timestamp without time zone,
    "BaseType" character varying(50),
    "Comment" character varying,
    "Groups" text,
    "Name" character varying(255),
    "Version" bigint NOT NULL,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL,	
    CONSTRAINT "PK_Rules2" PRIMARY KEY (id)
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."Rules2"
    OWNER to geotabadapter_client;

CREATE SEQUENCE public."Rules2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;
ALTER SEQUENCE public."Rules2_id_seq" OWNER TO geotabadapter_client;
ALTER SEQUENCE public."Rules2_id_seq" OWNED BY public."Rules2".id;

ALTER TABLE ONLY public."Rules2" ALTER COLUMN id SET DEFAULT nextval('public."Rules2_id_seq"'::regclass);

GRANT ALL ON SEQUENCE public."Rules2_id_seq" TO geotabadapter_client;

CREATE INDEX "IX_Rules2_RecordLastChangedUtc" ON public."Rules2" ("RecordLastChangedUtc");

ALTER TABLE ONLY public."Rules2" ADD CONSTRAINT "UK_Rules2_GeotabId" UNIQUE ("GeotabId");
	
REVOKE ALL ON TABLE public."Rules2" FROM geotabadapter_client;
GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."Rules2" TO geotabadapter_client;
GRANT ALL ON TABLE public."Rules2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create Trips2 table:
CREATE TABLE public."Trips2" (
    "id" bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "AfterHoursDistance" real,
    "AfterHoursDrivingDurationTicks" bigint,
    "AfterHoursEnd" boolean,
    "AfterHoursStart" boolean,
    "AfterHoursStopDurationTicks" bigint,
    "AverageSpeed" real,
    "DeletedDateTime" timestamp without time zone,
    "DeviceId" bigint NOT NULL,
    "Distance" real NOT NULL,
    "DriverId" bigint,
    "DrivingDurationTicks" bigint NOT NULL,
    "IdlingDurationTicks" bigint,
    "MaximumSpeed" real,
    "NextTripStart" timestamp without time zone NOT NULL,
    "SpeedRange1" integer,
    "SpeedRange1DurationTicks" bigint,
    "SpeedRange2" integer,
    "SpeedRange2DurationTicks" bigint,
    "SpeedRange3" integer,
    "SpeedRange3DurationTicks" bigint,
    "Start" timestamp without time zone NOT NULL,
    "Stop" timestamp without time zone NOT NULL,
    "StopDurationTicks" bigint NOT NULL,
    "StopPointX" double precision,
    "StopPointY" double precision,
    "WorkDistance" real,
    "WorkDrivingDurationTicks" bigint,
    "WorkStopDurationTicks" bigint,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_Trips2" PRIMARY KEY ("Start", "id"),
    CONSTRAINT "UK_Trips2_DeviceId_Start_EntityStatus" UNIQUE ("DeviceId", "Start", "EntityStatus")
) 
PARTITION BY RANGE ("Start");

ALTER TABLE IF EXISTS public."Trips2"
    OWNER TO geotabadapter_client;

CREATE SEQUENCE public."Trips2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;
ALTER SEQUENCE public."Trips2_id_seq" OWNER TO geotabadapter_client;
ALTER SEQUENCE public."Trips2_id_seq" OWNED BY public."Trips2"."id";

ALTER TABLE ONLY public."Trips2" ALTER COLUMN "id" SET DEFAULT nextval('public."Trips2_id_seq"'::regclass);

GRANT ALL ON SEQUENCE public."Trips2_id_seq" TO geotabadapter_client;

CREATE INDEX "CI_Trips2_Start_Id" ON public."Trips2" ("Start", "id");

CREATE INDEX "IX_Trips2_NextTripStart" ON public."Trips2" ("NextTripStart");

CREATE INDEX "IX_Trips2_RecordLastChangedUtc" ON public."Trips2" ("RecordLastChangedUtc");

ALTER TABLE public."Trips2"
    ADD CONSTRAINT "FK_Trips2_Devices2" FOREIGN KEY ("DeviceId")
    REFERENCES public."Devices2" ("id");

ALTER TABLE public."Trips2"
    ADD CONSTRAINT "FK_Trips2_Users2" FOREIGN KEY ("DriverId")
    REFERENCES public."Users2" ("id");

REVOKE ALL ON TABLE public."Trips2" FROM geotabadapter_client;
GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."Trips2" TO geotabadapter_client;
GRANT ALL ON TABLE public."Trips2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_Rules2 table:
CREATE TABLE public."stg_Rules2"
(
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone,
    "ActiveTo" timestamp without time zone,
    "BaseType" character varying(50),
    "Comment" character varying,
    "Groups" text,
    "Name" character varying(255),
    "Version" bigint NOT NULL,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL,	
    CONSTRAINT "PK_stg_Rules2" PRIMARY KEY (id)
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."stg_Rules2"
    OWNER to geotabadapter_client;

CREATE SEQUENCE public."stg_Rules2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;
ALTER SEQUENCE public."stg_Rules2_id_seq" OWNER TO geotabadapter_client;
ALTER SEQUENCE public."stg_Rules2_id_seq" OWNED BY public."stg_Rules2".id;

ALTER TABLE ONLY public."stg_Rules2" ALTER COLUMN id SET DEFAULT nextval('public."stg_Rules2_id_seq"'::regclass);

GRANT ALL ON SEQUENCE public."stg_Rules2_id_seq" TO geotabadapter_client;

CREATE INDEX "IX_stg_Rules2_GeotabId_RecordLastChangedUtc" ON public."stg_Rules2" USING btree ("GeotabId", "RecordLastChangedUtc" DESC);

REVOKE ALL ON TABLE public."stg_Rules2" FROM geotabadapter_client;
GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."stg_Rules2" TO geotabadapter_client;
GRANT ALL ON TABLE public."stg_Rules2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_Trips2 table:
CREATE TABLE public."stg_Trips2" (
    "id" bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "AfterHoursDistance" real,
    "AfterHoursDrivingDurationTicks" bigint,
    "AfterHoursEnd" boolean,
    "AfterHoursStart" boolean,
    "AfterHoursStopDurationTicks" bigint,
    "AverageSpeed" real,
    "DeletedDateTime" timestamp without time zone,
    "DeviceId" bigint NOT NULL,
    "Distance" real NOT NULL,
    "DriverId" bigint,
    "DrivingDurationTicks" bigint NOT NULL,
    "IdlingDurationTicks" bigint,
    "MaximumSpeed" real,
    "NextTripStart" timestamp without time zone NOT NULL,
    "SpeedRange1" integer,
    "SpeedRange1DurationTicks" bigint,
    "SpeedRange2" integer,
    "SpeedRange2DurationTicks" bigint,
    "SpeedRange3" integer,
    "SpeedRange3DurationTicks" bigint,
    "Start" timestamp without time zone NOT NULL,
    "Stop" timestamp without time zone NOT NULL,
    "StopDurationTicks" bigint NOT NULL,
    "StopPointX" double precision,
    "StopPointY" double precision,
    "WorkDistance" real,
    "WorkDrivingDurationTicks" bigint,
    "WorkStopDurationTicks" bigint,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_stg_Trips2" PRIMARY KEY ("id")
) TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."stg_Trips2"
    OWNER TO geotabadapter_client;

CREATE SEQUENCE public."stg_Trips2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;
ALTER SEQUENCE public."stg_Trips2_id_seq" OWNER TO geotabadapter_client;
ALTER SEQUENCE public."stg_Trips2_id_seq" OWNED BY public."stg_Trips2"."id";

ALTER TABLE ONLY public."stg_Trips2" 
    ALTER COLUMN "id" SET DEFAULT nextval('public."stg_Trips2_id_seq"'::regclass);

GRANT ALL ON SEQUENCE public."stg_Trips2_id_seq" TO geotabadapter_client;

CREATE INDEX "IX_stg_Trips2_DeviceId_Start_EntityStatus" ON public."stg_Trips2" ("DeviceId", "Start", "EntityStatus");

REVOKE ALL ON TABLE public."stg_Trips2" FROM geotabadapter_client;
GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."stg_Trips2" TO geotabadapter_client;
GRANT ALL ON TABLE public."stg_Trips2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_Rules2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_Rules2"(
	"SetEntityStatusDeletedForMissingRules" boolean DEFAULT false)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description: 
--   Upserts records from the stg_Rules2 staging table to the Rules2 table and then
--   truncates the staging table. If the SetEntityStatusDeletedForMissingRules 
--   parameter is set to true, the EntityStatus column will be set to 0 (Deleted) for 
--   any records in the Rules2 table for which there are no corresponding records 
--   with the same GeotabId in the stg_Rules2 table.
--
-- Notes:
--   - No transaction used as application should manage the transaction.
-- ==========================================================================================
BEGIN
    -- De-duplicate staging table by selecting the latest record per natural key (DeviceId, Start).
	-- Uses DISTINCT ON to keep only the latest record per DeviceId + Start.
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
	SELECT DISTINCT ON ("GeotabId") *
	FROM public."stg_Rules2"
	ORDER BY "GeotabId", "RecordLastChangedUtc" DESC;
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("GeotabId");

    -- Perform upsert.
    INSERT INTO public."Rules2" AS d (
        "GeotabId",
        "ActiveFrom",
        "ActiveTo",
        "BaseType",
        "Comment",
        "Groups",
        "Name",
        "Version",        
        "EntityStatus",
        "RecordLastChangedUtc"
    )
    SELECT 
        s."GeotabId",
        s."ActiveFrom",
        s."ActiveTo",
        s."BaseType",
        s."Comment",
        s."Groups",
        s."Name",
        s."Version",   
        s."EntityStatus",
        s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("GeotabId") 
    DO UPDATE SET
        "ActiveFrom" = EXCLUDED."ActiveFrom",
        "ActiveTo" = EXCLUDED."ActiveTo",
        "BaseType" = EXCLUDED."BaseType",
        "Comment" = EXCLUDED."Comment",
        "Groups" = EXCLUDED."Groups",
        "Name" = EXCLUDED."Name",
        "Version" = EXCLUDED."Version",
        "EntityStatus" = EXCLUDED."EntityStatus",        
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
    WHERE
        d."ActiveFrom" IS DISTINCT FROM EXCLUDED."ActiveFrom"
        OR d."ActiveTo" IS DISTINCT FROM EXCLUDED."ActiveTo"
        OR d."BaseType" IS DISTINCT FROM EXCLUDED."BaseType"
        OR d."Comment" IS DISTINCT FROM EXCLUDED."Comment"
        OR d."Groups" IS DISTINCT FROM EXCLUDED."Groups"
        OR d."Name" IS DISTINCT FROM EXCLUDED."Name"
        OR d."Version" IS DISTINCT FROM EXCLUDED."Version"
        OR d."EntityStatus" IS DISTINCT FROM EXCLUDED."EntityStatus";        
        -- OR d."RecordLastChangedUtc" IS DISTINCT FROM EXCLUDED."RecordLastChangedUtc";

    -- If SetEntityStatusDeletedForMissingRules is TRUE, mark missing Rules as deleted.
    IF "SetEntityStatusDeletedForMissingRules" THEN
        UPDATE public."Rules2" d
        SET "EntityStatus" = 0,
            "RecordLastChangedUtc" = clock_timestamp() AT TIME ZONE 'UTC'
        WHERE NOT EXISTS (
            SELECT 1 FROM public."stg_Rules2" s
            WHERE s."GeotabId" = d."GeotabId"
        );
    END IF;
	
    -- Clear staging table.
    TRUNCATE TABLE public."stg_Rules2";

    -- Drop temporary table.
    DROP TABLE "TMP_DeduplicatedStaging";
END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_Rules2"(boolean)
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_Rules2"(boolean) TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_Rules2"(boolean) FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_Trips2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_Trips2"()
	RETURNS void
	LANGUAGE plpgsql
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
        "GeotabId" = EXCLUDED."GeotabId",
        "AfterHoursDistance" = EXCLUDED."AfterHoursDistance",
        "AfterHoursDrivingDurationTicks" = EXCLUDED."AfterHoursDrivingDurationTicks",
        "AfterHoursEnd" = EXCLUDED."AfterHoursEnd",
        "AfterHoursStart" = EXCLUDED."AfterHoursStart",
        "AfterHoursStopDurationTicks" = EXCLUDED."AfterHoursStopDurationTicks",
        "AverageSpeed" = EXCLUDED."AverageSpeed",
        "DeletedDateTime" = EXCLUDED."DeletedDateTime",
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
        -- OR d."RecordLastChangedUtc" IS DISTINCT FROM EXCLUDED."RecordLastChangedUtc";

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


/*** [END] Part 2 of 3: Database Upgrades (tables, sequences, views) Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc")
SELECT "UpgradeDatabaseVersion", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "TMP_UpgradeDatabaseVersionTable";
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
/*** [END] Part 3 of 3: Database Version Update Above ***/
