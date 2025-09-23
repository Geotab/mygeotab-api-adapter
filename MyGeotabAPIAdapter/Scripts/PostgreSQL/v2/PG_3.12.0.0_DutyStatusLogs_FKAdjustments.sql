-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.11.0.0 to version 3.12.0.0.
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
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('3.12.0.0');

DO $$	 
DECLARE 
    required_starting_database_version TEXT DEFAULT '3.11.0.0';
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
-- Remove the foreign keys associated with Users2.id and Rules2.id to fix the FK violation issue:
ALTER TABLE public."DeviceStatusInfo2" DROP CONSTRAINT "FK_DeviceStatusInfo2_Users2";
ALTER TABLE public."DriverChanges2" DROP CONSTRAINT "FK_DriverChanges2_Users2";
ALTER TABLE public."DutyStatusAvailabilities2" DROP CONSTRAINT "FK_DutyStatusAvailabilities2_Users2";
ALTER TABLE public."DVIRDefectRemarks2" DROP CONSTRAINT "FK_DVIRDefectRemarks2_Users2";
ALTER TABLE public."DVIRDefects2" DROP CONSTRAINT "FK_DVIRDefects2_Users2";
ALTER TABLE public."DVIRLogs2" DROP CONSTRAINT "FK_DVIRLogs2_Users2_Certified";
ALTER TABLE public."DVIRLogs2" DROP CONSTRAINT "FK_DVIRLogs2_Users2_Driver";
ALTER TABLE public."DVIRLogs2" DROP CONSTRAINT "FK_DVIRLogs2_Users2_Repaired";
ALTER TABLE public."ExceptionEvents2" DROP CONSTRAINT "FK_ExceptionEvents2_Rules2";
ALTER TABLE public."ExceptionEvents2" DROP CONSTRAINT "FK_ExceptionEvents2_Users2";
ALTER TABLE public."FaultData2" DROP CONSTRAINT "FK_FaultData2_Users2";
ALTER TABLE public."Trips2" DROP CONSTRAINT "FK_Trips2_Users2";

-- Create DutyStatusLogs2 table:
CREATE TABLE public."DutyStatusLogs2" (
    "id" uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "Annotations" text,
    "CoDrivers" text,
    "DateTime" timestamp without time zone NOT NULL,
    "DeferralMinutes" integer,
    "DeferralStatus" character varying(50),
    "DeviceId" bigint,
    "DistanceSinceValidCoordinates" real,
    "DriverId" bigint,
    "EditDateTime" timestamp without time zone,
    "EditRequestedByUserId" bigint,
    "EngineHours" double precision,
    "EventCheckSum" bigint,
    "EventCode" smallint,
    "EventRecordStatus" smallint,
    "EventType" smallint,
    "IsHidden" boolean,
    "IsIgnored" boolean,
    "IsTransitioning" boolean,
    "Location" text,
    "LocationX" double precision,
    "LocationY" double precision,
    "Malfunction" character varying(50),
    "Odometer" double precision,
    "Origin" character varying(50),
    "ParentId" character varying(50),
    "Sequence" bigint,
    "State" character varying(50),
    "Status" character varying(50),
    "UserHosRuleSet" text,
    "VerifyDateTime" timestamp without time zone,
    "Version" bigint,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_DutyStatusLogs2" PRIMARY KEY ("DateTime", "id")
) PARTITION BY RANGE ("DateTime");

ALTER TABLE IF EXISTS public."DutyStatusLogs2"
    OWNER TO geotabadapter_client;
	
CREATE INDEX "IX_DutyStatusLogs2_DeviceId" ON public."DutyStatusLogs2" ("DeviceId");

CREATE INDEX "IX_DutyStatusLogs2_DriverId" ON public."DutyStatusLogs2" ("DriverId");

CREATE INDEX "IX_DutyStatusLogs2_EditRequestedByUserId" ON public."DutyStatusLogs2" ("EditRequestedByUserId");

CREATE INDEX "IX_DutyStatusLogs2_RecordCreationTimeUtc" ON public."DutyStatusLogs2" ("RecordCreationTimeUtc");

CREATE INDEX "IX_DutyStatusLogs2_DateTime_Device" ON public."DutyStatusLogs2" ("DateTime", "DeviceId");

CREATE INDEX "IX_DutyStatusLogs2_DateTime_Driver" ON public."DutyStatusLogs2" ("DateTime" ASC, "DriverId" ASC);

CREATE INDEX "IX_DutyStatusLogs2_DateTime_EditRequestedByUser" ON public."DutyStatusLogs2" ("DateTime" ASC, "EditRequestedByUserId" ASC);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_DutyStatusLogs2 table:
CREATE TABLE public."stg_DutyStatusLogs2" (
    "id" uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "Annotations" text,
    "CoDrivers" text,
    "DateTime" timestamp without time zone NOT NULL,
    "DeferralMinutes" integer,
    "DeferralStatus" character varying(50),
    "DeviceId" bigint,
    "DistanceSinceValidCoordinates" real,
    "DriverId" bigint,
    "EditDateTime" timestamp without time zone,
    "EditRequestedByUserId" bigint,
    "EngineHours" double precision,
    "EventCheckSum" bigint,
    "EventCode" smallint,
    "EventRecordStatus" smallint,
    "EventType" smallint,
    "IsHidden" boolean,
    "IsIgnored" boolean,
    "IsTransitioning" boolean,
    "Location" text,
    "LocationX" double precision,
    "LocationY" double precision,
    "Malfunction" character varying(50),
	"Odometer" double precision,
    "Origin" character varying(50),
    "ParentId" character varying(50),
    "Sequence" bigint,
    "State" character varying(50),
    "Status" character varying(50),
    "UserHosRuleSet" text,
    "VerifyDateTime" timestamp without time zone,
    "Version" bigint,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);

ALTER TABLE IF EXISTS public."stg_DutyStatusLogs2"
    OWNER TO geotabadapter_client;								  
CREATE INDEX "IX_stg_DutyStatusLogs2_RecordCreationTimeUtc" ON public."stg_DutyStatusLogs2" ("id" ASC, "RecordCreationTimeUtc" ASC);
GRANT ALL ON TABLE public."stg_DutyStatusLogs2" TO geotabadapter_client;

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_DutyStatusLogs2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_DutyStatusLogs2"()
    RETURNS void
    LANGUAGE plpgsql
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_DutyStatusLogs2 staging table to the DutyStatusLogs2
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
	
    -- De-duplicate staging table by selecting the latest record per "id". Add a rownum
	-- column so that it is not necessary to list all columns when populating this table.
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("id") *
    FROM public."stg_DutyStatusLogs2"
    ORDER BY "id", "RecordCreationTimeUtc" DESC;
	
    -- Add an index to the temporary table on the column used for conflict resolution.
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id");

    -- Identify records where DateTime has changed.
    INSERT INTO "TMP_MovedRecordIds" ("id")
    SELECT s."id"
    FROM "TMP_DeduplicatedStaging" s
    INNER JOIN public."DutyStatusLogs2" d ON s."id" = d."id"
    WHERE s."DateTime" IS DISTINCT FROM d."DateTime";

    -- Delete the old versions of these "mover" records from the target table.
    DELETE FROM public."DutyStatusLogs2" AS d
    USING "TMP_MovedRecordIds" m
    WHERE d."id" = m."id";

    -- Perform upsert.
    -- Inserts new records AND records whose DateTime changed (deleted above).
    INSERT INTO public."DutyStatusLogs2" AS d (
        "id", 
		"GeotabId", 
		"Annotations", 
		"CoDrivers", 
		"DateTime", 
		"DeferralMinutes", 
		"DeferralStatus", 
		"DeviceId",
        "DistanceSinceValidCoordinates", 
		"DriverId", 
		"EditDateTime", 
		"EditRequestedByUserId", 
		"EngineHours",
        "EventCheckSum", 
		"EventCode", 
		"EventRecordStatus", 
		"EventType", 
		"IsHidden", 
		"IsIgnored", 
		"IsTransitioning",
        "Location", 
		"LocationX", 
		"LocationY", 
		"Malfunction", 
		"Odometer", 
		"Origin", 
		"ParentId", 
		"Sequence",
        "State", 
		"Status", 
		"UserHosRuleSet", 
		"VerifyDateTime", 
		"Version", 
		"RecordCreationTimeUtc"
    )
    SELECT
        s."id", 
		s."GeotabId", 
		s."Annotations", 
		s."CoDrivers", 
		s."DateTime", 
		s."DeferralMinutes", 
		s."DeferralStatus", 
		s."DeviceId",
        s."DistanceSinceValidCoordinates", 
		s."DriverId", 
		s."EditDateTime", 
		s."EditRequestedByUserId", 
		s."EngineHours",
        s."EventCheckSum", 
		s."EventCode", 
		s."EventRecordStatus", 
		s."EventType", 
		s."IsHidden", 
		s."IsIgnored", 
		s."IsTransitioning",
        s."Location", 
		s."LocationX", 
		s."LocationY", 
		s."Malfunction", 
		s."Odometer", 
		s."Origin", 
		s."ParentId", 
		s."Sequence",
        s."State", 
		s."Status", 
		s."UserHosRuleSet", 
		s."VerifyDateTime", 
		s."Version", 
		s."RecordCreationTimeUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("DateTime", "id")
	DO UPDATE SET
		-- "id" and "DateTime" excluded since they are subject of ON CONFLICT.
		-- If only "DateTime" changed, the original record will have been deleted
		-- and a new one will be inserted instead of updating the existing record.
        "GeotabId" = EXCLUDED."GeotabId",
        "Annotations" = EXCLUDED."Annotations",
        "CoDrivers" = EXCLUDED."CoDrivers",
        "DeferralMinutes" = EXCLUDED."DeferralMinutes",
        "DeferralStatus" = EXCLUDED."DeferralStatus",
        "DeviceId" = EXCLUDED."DeviceId",
        "DistanceSinceValidCoordinates" = EXCLUDED."DistanceSinceValidCoordinates",
        "DriverId" = EXCLUDED."DriverId",
        "EditDateTime" = EXCLUDED."EditDateTime",
        "EditRequestedByUserId" = EXCLUDED."EditRequestedByUserId",
        "EngineHours" = EXCLUDED."EngineHours",
        "EventCheckSum" = EXCLUDED."EventCheckSum",
        "EventCode" = EXCLUDED."EventCode",
        "EventRecordStatus" = EXCLUDED."EventRecordStatus",
        "EventType" = EXCLUDED."EventType",
        "IsHidden" = EXCLUDED."IsHidden",
        "IsIgnored" = EXCLUDED."IsIgnored",
        "IsTransitioning" = EXCLUDED."IsTransitioning",
        "Location" = EXCLUDED."Location",
        "LocationX" = EXCLUDED."LocationX",
        "LocationY" = EXCLUDED."LocationY",
        "Malfunction" = EXCLUDED."Malfunction",
        "Odometer" = EXCLUDED."Odometer",
        "Origin" = EXCLUDED."Origin",
        "ParentId" = EXCLUDED."ParentId",
        "Sequence" = EXCLUDED."Sequence",
        "State" = EXCLUDED."State",
        "Status" = EXCLUDED."Status",
        "UserHosRuleSet" = EXCLUDED."UserHosRuleSet",
        "VerifyDateTime" = EXCLUDED."VerifyDateTime",
        "Version" = EXCLUDED."Version",
        "RecordCreationTimeUtc" = EXCLUDED."RecordCreationTimeUtc"
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId" OR
		d."Annotations" IS DISTINCT FROM EXCLUDED."Annotations" OR
		d."CoDrivers" IS DISTINCT FROM EXCLUDED."CoDrivers" OR
		d."DeferralMinutes" IS DISTINCT FROM EXCLUDED."DeferralMinutes" OR
		d."DeferralStatus" IS DISTINCT FROM EXCLUDED."DeferralStatus" OR
		d."DeviceId" IS DISTINCT FROM EXCLUDED."DeviceId" OR
		d."DistanceSinceValidCoordinates" IS DISTINCT FROM EXCLUDED."DistanceSinceValidCoordinates" OR		
		d."DriverId" IS DISTINCT FROM EXCLUDED."DriverId" OR		
        d."EditDateTime" IS DISTINCT FROM EXCLUDED."EditDateTime" OR
		d."EditRequestedByUserId" IS DISTINCT FROM EXCLUDED."EditRequestedByUserId" OR
		d."EngineHours" IS DISTINCT FROM EXCLUDED."EngineHours" OR
		d."EventCheckSum" IS DISTINCT FROM EXCLUDED."EventCheckSum" OR      
		d."EventCode" IS DISTINCT FROM EXCLUDED."EventCode" OR
		d."EventRecordStatus" IS DISTINCT FROM EXCLUDED."EventRecordStatus" OR
		d."EventType" IS DISTINCT FROM EXCLUDED."EventType" OR
		d."IsHidden" IS DISTINCT FROM EXCLUDED."IsHidden" OR		
		d."IsIgnored" IS DISTINCT FROM EXCLUDED."IsIgnored" OR		
        d."IsTransitioning" IS DISTINCT FROM EXCLUDED."IsTransitioning" OR
        d."Location" IS DISTINCT FROM EXCLUDED."Location" OR
        d."LocationX" IS DISTINCT FROM EXCLUDED."LocationX" OR
        d."LocationY" IS DISTINCT FROM EXCLUDED."LocationY" OR
        d."Malfunction" IS DISTINCT FROM EXCLUDED."Malfunction" OR
        d."Odometer" IS DISTINCT FROM EXCLUDED."Odometer" OR
        d."Origin" IS DISTINCT FROM EXCLUDED."Origin" OR
        d."ParentId" IS DISTINCT FROM EXCLUDED."ParentId" OR
        d."Sequence" IS DISTINCT FROM EXCLUDED."Sequence" OR
        d."State" IS DISTINCT FROM EXCLUDED."State" OR
        d."Status" IS DISTINCT FROM EXCLUDED."Status" OR
        d."UserHosRuleSet" IS DISTINCT FROM EXCLUDED."UserHosRuleSet" OR
        d."VerifyDateTime" IS DISTINCT FROM EXCLUDED."VerifyDateTime" OR
        d."Version" IS DISTINCT FROM EXCLUDED."Version";
		-- RecordCreationTimeUtc not evaluated as it should never match.

    -- Clear staging table.
    TRUNCATE TABLE public."stg_DutyStatusLogs2";

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

ALTER FUNCTION public."spMerge_stg_DutyStatusLogs2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_DutyStatusLogs2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_DutyStatusLogs2"() FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Add a sentinel record to represent "NoUserId".
INSERT INTO public."Users2" (
    "id", "GeotabId", "ActiveFrom", "ActiveTo", "CompanyGroups",
    "EmployeeNo", "FirstName", "HosRuleSet", "IsDriver", "LastAccessDate",
    "LastName", "Name", "EntityStatus", "RecordLastChangedUtc"
)
VALUES (
    -1, 'NoUserId', '1912-06-23 00:00:00', '2099-12-31 00:00:00', NULL,
    NULL, 'No', NULL, false, NULL,
    'User', 'NoUser', 1, '1912-06-23 00:00:00'
);

-- Add a sentinel record to represent "NoDriverId".
INSERT INTO public."Users2" (
    "id", "GeotabId", "ActiveFrom", "ActiveTo", "CompanyGroups",
    "EmployeeNo", "FirstName", "HosRuleSet", "IsDriver", "LastAccessDate",
    "LastName", "Name", "EntityStatus", "RecordLastChangedUtc"
)
VALUES (
    -2, 'NoDriverId', '1912-06-23 00:00:00', '2099-12-31 00:00:00', NULL,
    NULL, 'No', NULL, false, NULL,
    'Driver', 'NoDriver', 1, '1912-06-23 00:00:00'
);

-- Add a sentinel record to represent "UnknownDriverId".
INSERT INTO public."Users2" (
    "id", "GeotabId", "ActiveFrom", "ActiveTo", "CompanyGroups",
    "EmployeeNo", "FirstName", "HosRuleSet", "IsDriver", "LastAccessDate",
    "LastName", "Name", "EntityStatus", "RecordLastChangedUtc"
)
VALUES (
    -3, 'UnknownDriverId', '1912-06-23 00:00:00', '2099-12-31 00:00:00', NULL,
    NULL, 'No', NULL, true, NULL,
    'Driver', 'UnknownDriver', 1, '1912-06-23 00:00:00'
);

-- Add a sentinel record to represent "NoDeviceId".
INSERT INTO public."Devices2" (
    "id", "GeotabId", "ActiveFrom", "ActiveTo", "Comment",
    "DeviceType", "Groups", "LicensePlate", "LicenseState", "Name",
    "ProductId", "SerialNumber", "VIN", "EntityStatus", "RecordLastChangedUtc",
    "TmpTrailerGeotabId", "TmpTrailerId"
)
VALUES (
    -1, 'NoDeviceId', '1912-06-23 00:00:00', '2099-12-31 00:00:00', NULL,
    'None', NULL, NULL, NULL, 'NoDevice',
    NULL, NULL, NULL, 1, '1912-06-23 00:00:00',
    NULL, NULL
);

-- Add a sentinel record to represent "NoRuleId".
INSERT INTO public."Rules2" (
    "id", "GeotabId", "ActiveFrom", "ActiveTo", "BaseType",
    "Comment", "Condition", "Groups", "Name", "Version",
    "EntityStatus", "RecordLastChangedUtc"
)
OVERRIDING SYSTEM VALUE
VALUES (
    -1, 'NoRuleId', '1912-06-23 00:00:00', '2099-12-31 00:00:00', NULL,
    NULL, NULL, NULL, 'NoRule', 0,
    1, '1912-06-23 00:00:00'
);

-- Add a sentinel record to represent "NoZoneId".
INSERT INTO public."Zones2" (
    "id", "GeotabId", "ActiveFrom", "ActiveTo", "CentroidLatitude",
    "CentroidLongitude", "Comment", "Displayed", "ExternalReference", "Groups",
    "MustIdentifyStops", "Name", "Points", "ZoneTypeIds", "Version",
    "EntityStatus", "RecordLastChangedUtc"
)
VALUES (
    -1, 'NoZoneId', '1912-06-23 00:00:00', '2099-12-31 00:00:00', NULL,
    NULL, NULL, false, NULL, NULL,
    false, 'NoZone', NULL, 'None', 0,
    1, '1912-06-23 00:00:00'
);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Make ExceptionEvents2.RuleId nullable:
ALTER TABLE public."ExceptionEvents2"
ALTER COLUMN "RuleId" DROP NOT NULL;
/*** [END] Part 2 of 3: Database Upgrades (tables, sequences, views) Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc")
SELECT "UpgradeDatabaseVersion", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "TMP_UpgradeDatabaseVersionTable";
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
/*** [END] Part 3 of 3: Database Version Update Above ***/
