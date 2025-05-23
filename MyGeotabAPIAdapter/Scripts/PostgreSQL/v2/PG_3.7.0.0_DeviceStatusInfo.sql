-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.6.0.0 to version 3.7.0.0.
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
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('3.7.0.0');

DO $$	 
DECLARE 
    required_starting_database_version TEXT DEFAULT '3.6.0.0';
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
-- Create DeviceStatusInfo2 table:
CREATE TABLE public."DeviceStatusInfo2" (
    "id" bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "Bearing" double precision NOT NULL,
    "CurrentStateDuration" character varying(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DriverId" bigint,
    "IsDeviceCommunicating" boolean NOT NULL,
    "IsDriving" boolean NOT NULL,
    "IsHistoricLastDriver" boolean NOT NULL,
    "Latitude" double precision NOT NULL,
    "Longitude" double precision NOT NULL,
    "Speed" real NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_DeviceStatusInfo2" PRIMARY KEY ("id")
);

ALTER TABLE public."DeviceStatusInfo2" OWNER TO geotabadapter_client;

-- Indexes
CREATE INDEX "IX_DeviceStatusInfo2_DeviceId" ON public."DeviceStatusInfo2" USING btree ("DeviceId");

CREATE INDEX "IX_DeviceStatusInfo2_DriverId" ON public."DeviceStatusInfo2" USING btree ("DriverId");

-- Foreign Key Constraints
ALTER TABLE public."DeviceStatusInfo2"
    ADD CONSTRAINT "FK_DeviceStatusInfo2_Devices2" FOREIGN KEY ("DeviceId")
        REFERENCES public."Devices2" ("id");

ALTER TABLE public."DeviceStatusInfo2"
    ADD CONSTRAINT "FK_DeviceStatusInfo2_Users2" FOREIGN KEY ("DriverId")
        REFERENCES public."Users2" ("id");


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_DeviceStatusInfo2 table:
CREATE TABLE public."stg_DeviceStatusInfo2" (
    "id" bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "Bearing" double precision NOT NULL,
    "CurrentStateDuration" character varying(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DriverId" bigint,
    "IsDeviceCommunicating" boolean NOT NULL,
    "IsDriving" boolean NOT NULL,
    "IsHistoricLastDriver" boolean NOT NULL,
    "Latitude" double precision NOT NULL,
    "Longitude" double precision NOT NULL,
    "Speed" real NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

ALTER TABLE public."stg_DeviceStatusInfo2" OWNER TO geotabadapter_client;

-- Index
CREATE INDEX "IX_stg_DeviceStatusInfo2_id_RecordLastChangedUtc" ON public."stg_DeviceStatusInfo2" USING btree ("id" ASC, "RecordLastChangedUtc" ASC);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_DeviceStatusInfo2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_DeviceStatusInfo2"(
)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_DeviceStatusInfo2 staging table to the DeviceStatusInfo2
--   table and then truncates the staging table.
--
-- Notes:
--   - No transaction used as application should manage the transaction.
-- ==========================================================================================
BEGIN
    -- De-duplicate staging table by selecting the latest record per "id".
	-- Uses DISTINCT ON to keep only the latest record per "id" based on "RecordLastChangedUtc".
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("id") *
    FROM public."stg_DeviceStatusInfo2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;

    -- Add an index to the temporary table on the column used for conflict resolution.
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id");

    -- Perform upsert.
    INSERT INTO public."DeviceStatusInfo2" AS d (
        "id",
        "GeotabId",
        "Bearing",
        "CurrentStateDuration",
        "DateTime",
        "DeviceId",
        "DriverId",
        "IsDeviceCommunicating",
        "IsDriving",
        "IsHistoricLastDriver",
        "Latitude",
        "Longitude",
        "Speed",
        "RecordLastChangedUtc"
    )
    SELECT
        s."id",
        s."GeotabId",
        s."Bearing",
        s."CurrentStateDuration",
        s."DateTime",
        s."DeviceId",
        s."DriverId",
        s."IsDeviceCommunicating",
        s."IsDriving",
        s."IsHistoricLastDriver",
        s."Latitude",
        s."Longitude",
        s."Speed",
        s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("id")
    DO UPDATE SET
		-- "id" excluded since it is subject of ON CONFLICT.
        "GeotabId" = EXCLUDED."GeotabId",
        "Bearing" = EXCLUDED."Bearing",
        "CurrentStateDuration" = EXCLUDED."CurrentStateDuration",
        "DateTime" = EXCLUDED."DateTime",
        "DeviceId" = EXCLUDED."DeviceId",
        "DriverId" = EXCLUDED."DriverId",
        "IsDeviceCommunicating" = EXCLUDED."IsDeviceCommunicating",
        "IsDriving" = EXCLUDED."IsDriving",
        "IsHistoricLastDriver" = EXCLUDED."IsHistoricLastDriver",
        "Latitude" = EXCLUDED."Latitude",
        "Longitude" = EXCLUDED."Longitude",
        "Speed" = EXCLUDED."Speed",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId" OR
        d."Bearing" IS DISTINCT FROM EXCLUDED."Bearing" OR
        d."CurrentStateDuration" IS DISTINCT FROM EXCLUDED."CurrentStateDuration" OR
        d."DateTime" IS DISTINCT FROM EXCLUDED."DateTime" OR
        d."DeviceId" IS DISTINCT FROM EXCLUDED."DeviceId" OR
        d."DriverId" IS DISTINCT FROM EXCLUDED."DriverId" OR
        d."IsDeviceCommunicating" IS DISTINCT FROM EXCLUDED."IsDeviceCommunicating" OR
        d."IsDriving" IS DISTINCT FROM EXCLUDED."IsDriving" OR
        d."IsHistoricLastDriver" IS DISTINCT FROM EXCLUDED."IsHistoricLastDriver" OR
        d."Latitude" IS DISTINCT FROM EXCLUDED."Latitude" OR
        d."Longitude" IS DISTINCT FROM EXCLUDED."Longitude" OR
        d."Speed" IS DISTINCT FROM EXCLUDED."Speed";
        -- RecordLastChangedUtc not evaluated as it should never match.

    -- Clear staging table.
    TRUNCATE TABLE public."stg_DeviceStatusInfo2";

    -- Drop temporary table.
    DROP TABLE "TMP_DeduplicatedStaging";

END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_DeviceStatusInfo2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_DeviceStatusInfo2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_DeviceStatusInfo2"() FROM PUBLIC;
/*** [END] Part 2 of 3: Database Upgrades (tables, sequences, views) Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc")
SELECT "UpgradeDatabaseVersion", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "TMP_UpgradeDatabaseVersionTable";
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
/*** [END] Part 3 of 3: Database Version Update Above ***/
