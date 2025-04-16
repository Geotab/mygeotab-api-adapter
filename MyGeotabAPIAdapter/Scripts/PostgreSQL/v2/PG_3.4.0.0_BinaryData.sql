-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.3.0.0 to version 3.4.0.0.
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
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('3.4.0.0');

DO $$	 
DECLARE 
    required_starting_database_version TEXT DEFAULT '3.3.0.0';
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
-- Create BinaryData2 table:
CREATE TABLE public."BinaryData2" (
    "id" uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "BinaryType" character varying(50) NULL,
    "ControllerId" character varying(50) NOT NULL,
    "Data" bytea NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" bigint NOT NULL,
    "Version" bigint NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
)
PARTITION BY RANGE ("DateTime");

ALTER TABLE public."BinaryData2" OWNER to geotabadapter_client;

ALTER TABLE ONLY public."BinaryData2"
    ADD CONSTRAINT "PK_BinaryData2" PRIMARY KEY ("DateTime", "id");

ALTER TABLE public."BinaryData2"
    ADD CONSTRAINT "FK_BinaryData2_Devices2" FOREIGN KEY ("DeviceId")
        REFERENCES public."Devices2" ("id");
/*** [END] Part 2 of 3: Database Upgrades (tables, sequences, views) Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc")
SELECT "UpgradeDatabaseVersion", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "TMP_UpgradeDatabaseVersionTable";
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
/*** [END] Part 3 of 3: Database Version Update Above ***/
