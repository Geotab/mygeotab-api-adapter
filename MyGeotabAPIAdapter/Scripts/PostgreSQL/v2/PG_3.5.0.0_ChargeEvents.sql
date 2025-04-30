-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.4.0.0 to version 3.5.0.0.
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
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('3.5.0.0');

DO $$	 
DECLARE 
    required_starting_database_version TEXT DEFAULT '3.4.0.0';
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
-- Create ChargeEvents2 table:
CREATE TABLE public."ChargeEvents2" (
    "id" uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ChargeIsEstimated" boolean NOT NULL,
    "ChargeType" character varying(50) NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DurationTicks" bigint NOT NULL,
    "EndStateOfCharge" double precision,
    "EnergyConsumedKwh" double precision,
    "EnergyUsedSinceLastChargeKwh" double precision,
    "Latitude" double precision,
    "Longitude" double precision,
    "MaxACVoltage" double precision,
    "MeasuredBatteryEnergyInKwh" double precision,
    "MeasuredBatteryEnergyOutKwh" double precision,
    "MeasuredOnBoardChargerEnergyInKwh" double precision,
    "MeasuredOnBoardChargerEnergyOutKwh" double precision,
    "PeakPowerKw" double precision,
    "StartStateOfCharge" double precision,
    "StartTime" timestamp without time zone NOT NULL,
    "TripStop" timestamp without time zone,
    "Version" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_ChargeEvents2" PRIMARY KEY ("id", "StartTime")
)
PARTITION BY RANGE ("StartTime");

ALTER TABLE public."ChargeEvents2" OWNER TO geotabadapter_client;

ALTER TABLE public."ChargeEvents2"
    ADD CONSTRAINT "FK_ChargeEvents2_Devices2" FOREIGN KEY ("DeviceId")
        REFERENCES public."Devices2" ("id");

CREATE INDEX "IX_ChargeEvents2_DeviceId" ON public."ChargeEvents2" USING btree ("DeviceId");

CREATE INDEX "IX_ChargeEvents2_RecordLastChangedUtc" ON public."ChargeEvents2" USING btree ("RecordLastChangedUtc");

CREATE INDEX "IX_ChargeEvents2_StartTime_DeviceId" ON public."ChargeEvents2" USING btree ("StartTime", "DeviceId");


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_ChargeEvents2 table:
CREATE TABLE public."stg_ChargeEvents2" (
    "id" uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ChargeIsEstimated" boolean NOT NULL,
    "ChargeType" character varying(50) NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DurationTicks" bigint NOT NULL,
    "EndStateOfCharge" double precision,
    "EnergyConsumedKwh" double precision,
    "EnergyUsedSinceLastChargeKwh" double precision,
    "Latitude" double precision,
    "Longitude" double precision,
    "MaxACVoltage" double precision,
    "MeasuredBatteryEnergyInKwh" double precision,
    "MeasuredBatteryEnergyOutKwh" double precision,
    "MeasuredOnBoardChargerEnergyInKwh" double precision,
    "MeasuredOnBoardChargerEnergyOutKwh" double precision,
    "PeakPowerKw" double precision,
    "StartStateOfCharge" double precision,
    "StartTime" timestamp without time zone NOT NULL,
    "TripStop" timestamp without time zone,
    "Version" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

ALTER TABLE public."stg_ChargeEvents2" OWNER TO geotabadapter_client;

CREATE INDEX "IX_stg_ChargeEvents2_id_RecordLastChangedUtc" ON public."stg_ChargeEvents2" USING btree ("id", "RecordLastChangedUtc");


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_ChargeEvents2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_ChargeEvents2"(
	)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_ChargeEvents2 staging table to the ChargeEvents2
--   table and then truncates the staging table.
--
-- Notes:
--   - No transaction used as application should manage the transaction.
-- ==========================================================================================
BEGIN
    -- De-duplicate staging table by selecting the latest record per "id".
    -- Uses DISTINCT ON to keep only the latest record per "id".
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("id") *
    FROM public."stg_ChargeEvents2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id", "StartTime");

    -- Perform upsert.
    INSERT INTO public."ChargeEvents2" AS d (
        "id",
        "GeotabId",
        "ChargeIsEstimated",
        "ChargeType",
        "DeviceId",
        "DurationTicks",
        "EndStateOfCharge",
        "EnergyConsumedKwh",
        "EnergyUsedSinceLastChargeKwh",
        "Latitude",
        "Longitude",
        "MaxACVoltage",
        "MeasuredBatteryEnergyInKwh",
        "MeasuredBatteryEnergyOutKwh",
        "MeasuredOnBoardChargerEnergyInKwh",
        "MeasuredOnBoardChargerEnergyOutKwh",
        "PeakPowerKw",
        "StartStateOfCharge",
        "StartTime",
        "TripStop",
        "Version",
        "RecordLastChangedUtc"
    )
    SELECT
        s."id",
        s."GeotabId",
        s."ChargeIsEstimated",
        s."ChargeType",
        s."DeviceId",
        s."DurationTicks",
        s."EndStateOfCharge",
        s."EnergyConsumedKwh",
        s."EnergyUsedSinceLastChargeKwh",
        s."Latitude",
        s."Longitude",
        s."MaxACVoltage",
        s."MeasuredBatteryEnergyInKwh",
        s."MeasuredBatteryEnergyOutKwh",
        s."MeasuredOnBoardChargerEnergyInKwh",
        s."MeasuredOnBoardChargerEnergyOutKwh",
        s."PeakPowerKw",
        s."StartStateOfCharge",
        s."StartTime",
        s."TripStop",
        s."Version",
        s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("id", "StartTime")
    DO UPDATE SET
        "GeotabId" = EXCLUDED."GeotabId",
        "ChargeIsEstimated" = EXCLUDED."ChargeIsEstimated",
        "ChargeType" = EXCLUDED."ChargeType",
        "DeviceId" = EXCLUDED."DeviceId",
        "DurationTicks" = EXCLUDED."DurationTicks",
        "EndStateOfCharge" = EXCLUDED."EndStateOfCharge",
        "EnergyConsumedKwh" = EXCLUDED."EnergyConsumedKwh",
        "EnergyUsedSinceLastChargeKwh" = EXCLUDED."EnergyUsedSinceLastChargeKwh",
        "Latitude" = EXCLUDED."Latitude",
        "Longitude" = EXCLUDED."Longitude",
        "MaxACVoltage" = EXCLUDED."MaxACVoltage",
        "MeasuredBatteryEnergyInKwh" = EXCLUDED."MeasuredBatteryEnergyInKwh",
        "MeasuredBatteryEnergyOutKwh" = EXCLUDED."MeasuredBatteryEnergyOutKwh",
        "MeasuredOnBoardChargerEnergyInKwh" = EXCLUDED."MeasuredOnBoardChargerEnergyInKwh",
        "MeasuredOnBoardChargerEnergyOutKwh" = EXCLUDED."MeasuredOnBoardChargerEnergyOutKwh",
        "PeakPowerKw" = EXCLUDED."PeakPowerKw",
        "StartStateOfCharge" = EXCLUDED."StartStateOfCharge",
        "TripStop" = EXCLUDED."TripStop",
        "Version" = EXCLUDED."Version",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc" -- Always update the timestamp
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId"
        OR d."ChargeIsEstimated" IS DISTINCT FROM EXCLUDED."ChargeIsEstimated"
        OR d."ChargeType" IS DISTINCT FROM EXCLUDED."ChargeType"
        OR d."DeviceId" IS DISTINCT FROM EXCLUDED."DeviceId"
        OR d."DurationTicks" IS DISTINCT FROM EXCLUDED."DurationTicks"
        OR d."EndStateOfCharge" IS DISTINCT FROM EXCLUDED."EndStateOfCharge"
        OR d."EnergyConsumedKwh" IS DISTINCT FROM EXCLUDED."EnergyConsumedKwh"
        OR d."EnergyUsedSinceLastChargeKwh" IS DISTINCT FROM EXCLUDED."EnergyUsedSinceLastChargeKwh"
        OR d."Latitude" IS DISTINCT FROM EXCLUDED."Latitude"
        OR d."Longitude" IS DISTINCT FROM EXCLUDED."Longitude"
        OR d."MaxACVoltage" IS DISTINCT FROM EXCLUDED."MaxACVoltage"
        OR d."MeasuredBatteryEnergyInKwh" IS DISTINCT FROM EXCLUDED."MeasuredBatteryEnergyInKwh"
        OR d."MeasuredBatteryEnergyOutKwh" IS DISTINCT FROM EXCLUDED."MeasuredBatteryEnergyOutKwh"
        OR d."MeasuredOnBoardChargerEnergyInKwh" IS DISTINCT FROM EXCLUDED."MeasuredOnBoardChargerEnergyInKwh"
        OR d."MeasuredOnBoardChargerEnergyOutKwh" IS DISTINCT FROM EXCLUDED."MeasuredOnBoardChargerEnergyOutKwh"
        OR d."PeakPowerKw" IS DISTINCT FROM EXCLUDED."PeakPowerKw"
        OR d."StartStateOfCharge" IS DISTINCT FROM EXCLUDED."StartStateOfCharge"
        OR d."TripStop" IS DISTINCT FROM EXCLUDED."TripStop"
        OR d."Version" IS DISTINCT FROM EXCLUDED."Version";
        -- OR d."RecordLastChangedUtc" IS DISTINCT FROM EXCLUDED."RecordLastChangedUtc";

    -- Clear staging table.
    TRUNCATE TABLE public."stg_ChargeEvents2";

    -- Drop temporary table.
    DROP TABLE "TMP_DeduplicatedStaging";

END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_ChargeEvents2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_ChargeEvents2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_ChargeEvents2"() FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Modify Rules2 and stg_Rules2 tables:
-- Add a Condition column to the Rules2 table.
ALTER TABLE public."Rules2"
ADD COLUMN "Condition" text;

-- Add a Condition column to the stg_Rules2 table.
ALTER TABLE public."stg_Rules2"
ADD COLUMN "Condition" text;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Modify spMerge_stg_Rules2 function:
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
		"Condition",
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
		s."Condition",
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
		"Condition" = EXCLUDED."Condition",
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
		OR d."Condition" IS DISTINCT FROM EXCLUDED."Condition"
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
/*** [END] Part 2 of 3: Database Upgrades (tables, sequences, views) Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc")
SELECT "UpgradeDatabaseVersion", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "TMP_UpgradeDatabaseVersionTable";
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
/*** [END] Part 3 of 3: Database Version Update Above ***/
