-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION:
--   The purpose of this script is to upgrade the MyGeotab API Adapter database
--   from version 4.0.1.0 to version 4.1.0.0.
--
--   Changes:
--   1. spMerge_stg_Diagnostics2: Added self-heal logic to restore missing
--      DiagnosticIds2 entries for active Diagnostics2 rows, independent of
--      upsert output. Previously, DiagnosticIds2 rows could only be created
--      when the upsert produced an INSERT or UPDATE action; if a DiagnosticIds2
--      row was missing but the parent Diagnostics2 row was unchanged, the
--      upsert would not fire and the DiagnosticIds2 row would never be restored.
--   2. spMerge_stg_Diagnostics2: Removed duplicate soft-delete block that
--      incorrectly used GeotabId instead of GeotabGUIDString for the
--      missing-record comparison.
--   3. Users2 / stg_Users2: Added Designation column (varchar(50), nullable).
--   4. spMerge_stg_Users2: Updated to include Designation in upsert logic.
--   5. Devices2 / stg_Devices2: Added CustomProperties column (text, nullable).
--   6. spMerge_stg_Devices2: Updated to include CustomProperties in upsert logic.
--   7. Trips2 / stg_Trips2: Added EngineHours and Odometer columns (double precision, nullable).
--   8. spMerge_stg_Trips2: Updated to include EngineHours and Odometer in upsert logic.
--   9. AuditLogs2: Added new partitioned table for Audit feed data.
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
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('4.1.0.0');

DO $$
DECLARE
    required_starting_database_version TEXT DEFAULT '4.0.1.0';
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
-- Update spMerge_stg_Diagnostics2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_Diagnostics2"("SetEntityStatusDeletedForMissingDiagnostics" boolean DEFAULT false) RETURNS void
    LANGUAGE plpgsql
    AS $$
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_Diagnostics2 staging table to the Diagnostics2 table and
--   then truncates the staging table. If the SetEntityStatusDeletedForMissingDiagnostics
--   parameter is set to true, the EntityStatus column will be set to 0 (Deleted) for
--   any records in the Diagnostics2 table for which there are no corresponding records
--   with the same GeotabGUIDString in the stg_Diagnostics2 table. Additionally, inserts
--   into DiagnosticIds2 for inserts and updates to Diagnostics2 where there isn't already
--   a record for the subject GeotabGUIDString + GeotabId combination. Also performs
--   self-healing to restore any missing DiagnosticIds2 entries for active Diagnostics2
--   rows.
--
-- Notes:
--   - No transaction used as application should manage the transaction.
-- ==========================================================================================
BEGIN
    -- Create temporary table for storing merge output.
	DROP TABLE IF EXISTS "TMP_MergeOutput";
    CREATE TEMP TABLE "TMP_MergeOutput" (
        "Action" TEXT,
        "GeotabGUIDString" TEXT,
        "GeotabId" TEXT,
        "HasShimId" BOOLEAN,
        "FormerShimGeotabGUIDString" TEXT,
		"RecordLastChangedUtc" timestamp without time zone
    );
    CREATE INDEX ON "TMP_MergeOutput" ("Action");
    CREATE INDEX ON "TMP_MergeOutput" ("GeotabGUIDString", "GeotabId");

    -- De-duplicate staging table by selecting the latest record per GeotabGUIDString
    -- (GeotabGUIDString is used to uniquely identify MYG Diagnostics). Note that
    -- RecordLastChangedUtc is set in the order in which results are retrieved via GetFeed.
    WITH "DeduplicatedStaging" AS (
        SELECT DISTINCT ON ("GeotabGUIDString") *
        FROM public."stg_Diagnostics2"
        ORDER BY "GeotabGUIDString", "RecordLastChangedUtc" DESC
    ),
    -- Perform upsert and store output in temporary table.
	merge_results AS (
	    INSERT INTO public."Diagnostics2" AS d (
	        "GeotabGUIDString",
	        "GeotabId",
	        "HasShimId",
	        "FormerShimGeotabGUIDString",
	        "ControllerId",
	        "DiagnosticCode",
	        "DiagnosticName",
	        "DiagnosticSourceId",
	        "DiagnosticSourceName",
	        "DiagnosticUnitOfMeasureId",
	        "DiagnosticUnitOfMeasureName",
	        "OBD2DTC",
	        "EntityStatus",
	        "RecordLastChangedUtc"
	    )
	    SELECT
	        s."GeotabGUIDString",
	        s."GeotabId",
	        s."HasShimId",
	        s."FormerShimGeotabGUIDString",
	        s."ControllerId",
	        s."DiagnosticCode",
	        s."DiagnosticName",
	        s."DiagnosticSourceId",
	        s."DiagnosticSourceName",
	        s."DiagnosticUnitOfMeasureId",
	        s."DiagnosticUnitOfMeasureName",
	        s."OBD2DTC",
	        s."EntityStatus",
	        s."RecordLastChangedUtc"
	    FROM "DeduplicatedStaging" s
	    ON CONFLICT ("GeotabGUIDString")
	    DO UPDATE SET
	        "GeotabId" = EXCLUDED."GeotabId",
	        "HasShimId" = EXCLUDED."HasShimId",
	        "FormerShimGeotabGUIDString" = EXCLUDED."FormerShimGeotabGUIDString",
	        "ControllerId" = EXCLUDED."ControllerId",
	        "DiagnosticCode" = EXCLUDED."DiagnosticCode",
	        "DiagnosticName" = EXCLUDED."DiagnosticName",
	        "DiagnosticSourceId" = EXCLUDED."DiagnosticSourceId",
	        "DiagnosticSourceName" = EXCLUDED."DiagnosticSourceName",
	        "DiagnosticUnitOfMeasureId" = EXCLUDED."DiagnosticUnitOfMeasureId",
	        "DiagnosticUnitOfMeasureName" = EXCLUDED."DiagnosticUnitOfMeasureName",
	        "OBD2DTC" = EXCLUDED."OBD2DTC",
	        "EntityStatus" = EXCLUDED."EntityStatus",
	        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
	    WHERE
	        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId"
	        OR d."HasShimId" IS DISTINCT FROM EXCLUDED."HasShimId"
	        OR d."FormerShimGeotabGUIDString" IS DISTINCT FROM EXCLUDED."FormerShimGeotabGUIDString"
	        OR d."ControllerId" IS DISTINCT FROM EXCLUDED."ControllerId"
	        OR d."DiagnosticCode" IS DISTINCT FROM EXCLUDED."DiagnosticCode"
	        OR d."DiagnosticName" IS DISTINCT FROM EXCLUDED."DiagnosticName"
	        OR d."DiagnosticSourceId" IS DISTINCT FROM EXCLUDED."DiagnosticSourceId"
	        OR d."DiagnosticSourceName" IS DISTINCT FROM EXCLUDED."DiagnosticSourceName"
	        OR d."DiagnosticUnitOfMeasureId" IS DISTINCT FROM EXCLUDED."DiagnosticUnitOfMeasureId"
	        OR d."DiagnosticUnitOfMeasureName" IS DISTINCT FROM EXCLUDED."DiagnosticUnitOfMeasureName"
	        OR d."OBD2DTC" IS DISTINCT FROM EXCLUDED."OBD2DTC"
	        OR d."EntityStatus" IS DISTINCT FROM EXCLUDED."EntityStatus"
	    RETURNING
	        (CASE WHEN xmax = 0 THEN 'INSERT' ELSE 'UPDATE' END) AS "Action",
	        d."GeotabGUIDString",
	        d."GeotabId",
	        d."HasShimId",
	        d."FormerShimGeotabGUIDString",
	        d."RecordLastChangedUtc"
	)
	INSERT INTO "TMP_MergeOutput"
	SELECT * FROM merge_results;

    -- Insert into DiagnosticIds2 for inserts and updates to Diagnostics2 where there isn't
	-- already a record for the subject GeotabGUIDString + GeotabId combination.
    INSERT INTO public."DiagnosticIds2" ("GeotabGUIDString", "GeotabId", "HasShimId", "FormerShimGeotabGUIDString", "RecordLastChangedUtc")
    SELECT "GeotabGUIDString", "GeotabId", "HasShimId", "FormerShimGeotabGUIDString", "RecordLastChangedUtc"
    FROM "TMP_MergeOutput"
    WHERE "Action" IN ('INSERT', 'UPDATE')
    AND NOT EXISTS (
        SELECT 1 FROM public."DiagnosticIds2" di
        WHERE di."GeotabGUIDString" = "TMP_MergeOutput"."GeotabGUIDString"
        AND di."GeotabId" = "TMP_MergeOutput"."GeotabId"
    );

    -- Self-heal: Restore missing DiagnosticIds2 entries for any active Diagnostics2
    -- rows. This covers the case where DiagnosticIds2 rows were deleted but the
    -- parent Diagnostics2 rows remain unchanged and thus were not captured by the
    -- upsert output above.
    INSERT INTO public."DiagnosticIds2" ("GeotabGUIDString", "GeotabId", "HasShimId", "FormerShimGeotabGUIDString", "RecordLastChangedUtc")
    SELECT d."GeotabGUIDString", d."GeotabId", d."HasShimId", d."FormerShimGeotabGUIDString", clock_timestamp() AT TIME ZONE 'UTC'
    FROM public."Diagnostics2" d
    WHERE d."EntityStatus" = 1
    AND NOT EXISTS (
        SELECT 1 FROM public."DiagnosticIds2" di
        WHERE di."GeotabGUIDString" = d."GeotabGUIDString"
        AND di."GeotabId" = d."GeotabId"
    );

    -- If SetEntityStatusDeletedForMissingDiagnostics is TRUE, set EntityStatus to 0 (Deleted)
    -- for any records in Diagnostics2 where there is no corresponding record with the same
    -- GeotabGUIDString in stg_Diagnostics2.
    IF "SetEntityStatusDeletedForMissingDiagnostics" THEN
        UPDATE public."Diagnostics2" d
        SET "EntityStatus" = 0,
			"RecordLastChangedUtc" = clock_timestamp() AT TIME ZONE 'UTC'
        WHERE NOT EXISTS (
            SELECT 1 FROM public."stg_Diagnostics2" s
            WHERE s."GeotabGUIDString" = d."GeotabGUIDString"
        );
    END IF;

    -- Truncate staging table
    TRUNCATE TABLE public."stg_Diagnostics2";

    -- Drop temporary table
    DROP TABLE IF EXISTS "TMP_MergeOutput";
END;
$$;
-- <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Add Designation column to Users2 and stg_Users2:
ALTER TABLE public."Users2" ADD COLUMN "Designation" character varying(50);
ALTER TABLE public."stg_Users2" ADD COLUMN "Designation" character varying(50);

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Users2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_Users2"("SetEntityStatusDeletedForMissingUsers" boolean DEFAULT false) RETURNS void
    LANGUAGE plpgsql
    AS $$
-- ==========================================================================================
-- Description:
--		Upserts records from the stg_Users2 staging table to the Users2 table and then
--		truncates the staging table. If the SetEntityStatusDeletedForMissingUsers
--		parameter is set to true, the EntityStatus column will be set to 0 (Deleted) for
--		any records in the Users2 table for which there are no corresponding records with
--		the same ids in the stg_Users2 table.
--
-- Notes:
--		- No transaction used as application should manage the transaction.
-- ==========================================================================================
BEGIN
    -- De-duplicate staging table by selecting the latest record per id.
    -- Uses DISTINCT ON to keep only the latest record per id.
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("id") *
    FROM public."stg_Users2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id");

    -- Perform upsert.
    INSERT INTO public."Users2" AS d (
        "id",
        "GeotabId",
        "ActiveFrom",
        "ActiveTo",
		"CompanyGroups",
        "Designation",
        "EmployeeNo",
        "FirstName",
        "HosRuleSet",
        "IsDriver",
        "LastAccessDate",
        "LastName",
        "Name",
        "EntityStatus",
        "RecordLastChangedUtc"
    )
    SELECT
        s."id",
        s."GeotabId",
        s."ActiveFrom",
        s."ActiveTo",
		s."CompanyGroups",
        s."Designation",
        s."EmployeeNo",
        s."FirstName",
        s."HosRuleSet",
        s."IsDriver",
        s."LastAccessDate",
        s."LastName",
        s."Name",
        s."EntityStatus",
        s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("id")
    DO UPDATE SET
		-- "id" is unique key and logical key for matching.
		-- "id" excluded since it is subject of ON CONFLICT.
        "GeotabId" = EXCLUDED."GeotabId",
        "ActiveFrom" = EXCLUDED."ActiveFrom",
        "ActiveTo" = EXCLUDED."ActiveTo",
		"CompanyGroups" = EXCLUDED."CompanyGroups",
        "Designation" = EXCLUDED."Designation",
        "EmployeeNo" = EXCLUDED."EmployeeNo",
        "FirstName" = EXCLUDED."FirstName",
        "HosRuleSet" = EXCLUDED."HosRuleSet",
        "IsDriver" = EXCLUDED."IsDriver",
        "LastAccessDate" = EXCLUDED."LastAccessDate",
        "LastName" = EXCLUDED."LastName",
        "Name" = EXCLUDED."Name",
        "EntityStatus" = EXCLUDED."EntityStatus",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId"
        OR d."ActiveFrom" IS DISTINCT FROM EXCLUDED."ActiveFrom"
        OR d."ActiveTo" IS DISTINCT FROM EXCLUDED."ActiveTo"
		OR d."CompanyGroups" IS DISTINCT FROM EXCLUDED."CompanyGroups"
        OR d."Designation" IS DISTINCT FROM EXCLUDED."Designation"
        OR d."EmployeeNo" IS DISTINCT FROM EXCLUDED."EmployeeNo"
        OR d."FirstName" IS DISTINCT FROM EXCLUDED."FirstName"
        OR d."HosRuleSet" IS DISTINCT FROM EXCLUDED."HosRuleSet"
        OR d."IsDriver" IS DISTINCT FROM EXCLUDED."IsDriver"
        OR d."LastAccessDate" IS DISTINCT FROM EXCLUDED."LastAccessDate"
        OR d."LastName" IS DISTINCT FROM EXCLUDED."LastName"
        OR d."Name" IS DISTINCT FROM EXCLUDED."Name"
        OR d."EntityStatus" IS DISTINCT FROM EXCLUDED."EntityStatus";
		-- RecordLastChangedUtc not evaluated as it should never match.

    -- If SetEntityStatusDeletedForMissingUsers is TRUE, mark missing users as deleted.
    IF "SetEntityStatusDeletedForMissingUsers" THEN
        UPDATE public."Users2" d
        SET "EntityStatus" = 0,
            "RecordLastChangedUtc" = clock_timestamp() AT TIME ZONE 'UTC'
        WHERE NOT EXISTS (
            SELECT 1 FROM public."stg_Users2" s
            WHERE s."id" = d."id"
        );
    END IF;

    -- Clear staging table.
    TRUNCATE TABLE public."stg_Users2";

    -- Drop temporary table.
    DROP TABLE "TMP_DeduplicatedStaging";

END;
$$;
-- <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Add CustomProperties column to Devices2 and stg_Devices2:
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'Devices2' AND column_name = 'CustomProperties') THEN
        ALTER TABLE public."Devices2" ADD COLUMN "CustomProperties" TEXT NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stg_Devices2' AND column_name = 'CustomProperties') THEN
        ALTER TABLE public."stg_Devices2" ADD COLUMN "CustomProperties" TEXT NULL;
    END IF;
END $$;

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Devices2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_Devices2"(
    "SetEntityStatusDeletedForMissingDevices" boolean DEFAULT false)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--		Upserts records from the stg_Devices2 staging table to the Devices2 table and then
--		truncates the staging table. If the SetEntityStatusDeletedForMissingDevices
--		parameter is set to true, the EntityStatus column will be set to 0 (Deleted) for
--		any records in the Devices2 table for which there are no corresponding records with
--		the same ids in the stg_Devices2 table.
--
-- Notes:
--		- No transaction used as application should manage the transaction.
-- ==========================================================================================
BEGIN
    -- De-duplicate staging table by selecting the latest record per id.
    -- Uses DISTINCT ON to keep only the latest record per id.
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("id") *
    FROM public."stg_Devices2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id");

    -- Perform upsert.
    INSERT INTO public."Devices2" AS d (
        "id", "GeotabId", "ActiveFrom", "ActiveTo", "Comment", "CustomProperties", "DeviceType", "Groups",
        "LicensePlate", "LicenseState", "Name", "ProductId", "SerialNumber", "VIN",
        "EntityStatus", "TmpTrailerGeotabId", "TmpTrailerId", "RecordLastChangedUtc"
    )
    SELECT s."id", s."GeotabId", s."ActiveFrom", s."ActiveTo", s."Comment", s."CustomProperties", s."DeviceType", s."Groups",
           s."LicensePlate", s."LicenseState", s."Name", s."ProductId", s."SerialNumber", s."VIN",
           s."EntityStatus", s."TmpTrailerGeotabId", s."TmpTrailerId", s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("id")
    DO UPDATE SET
        "GeotabId" = EXCLUDED."GeotabId",
        "ActiveFrom" = EXCLUDED."ActiveFrom",
        "ActiveTo" = EXCLUDED."ActiveTo",
        "Comment" = EXCLUDED."Comment",
        "CustomProperties" = EXCLUDED."CustomProperties",
        "DeviceType" = EXCLUDED."DeviceType",
        "Groups" = EXCLUDED."Groups",
        "LicensePlate" = EXCLUDED."LicensePlate",
        "LicenseState" = EXCLUDED."LicenseState",
        "Name" = EXCLUDED."Name",
        "ProductId" = EXCLUDED."ProductId",
        "SerialNumber" = EXCLUDED."SerialNumber",
        "VIN" = EXCLUDED."VIN",
        "EntityStatus" = EXCLUDED."EntityStatus",
        "TmpTrailerGeotabId" = EXCLUDED."TmpTrailerGeotabId",
        "TmpTrailerId" = EXCLUDED."TmpTrailerId",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId"
        OR d."ActiveFrom" IS DISTINCT FROM EXCLUDED."ActiveFrom"
        OR d."ActiveTo" IS DISTINCT FROM EXCLUDED."ActiveTo"
        OR d."Comment" IS DISTINCT FROM EXCLUDED."Comment"
        OR d."CustomProperties" IS DISTINCT FROM EXCLUDED."CustomProperties"
        OR d."DeviceType" IS DISTINCT FROM EXCLUDED."DeviceType"
        OR d."Groups" IS DISTINCT FROM EXCLUDED."Groups"
        OR d."LicensePlate" IS DISTINCT FROM EXCLUDED."LicensePlate"
        OR d."LicenseState" IS DISTINCT FROM EXCLUDED."LicenseState"
        OR d."Name" IS DISTINCT FROM EXCLUDED."Name"
        OR d."ProductId" IS DISTINCT FROM EXCLUDED."ProductId"
        OR d."SerialNumber" IS DISTINCT FROM EXCLUDED."SerialNumber"
        OR d."VIN" IS DISTINCT FROM EXCLUDED."VIN"
        OR d."EntityStatus" IS DISTINCT FROM EXCLUDED."EntityStatus"
        OR d."TmpTrailerGeotabId" IS DISTINCT FROM EXCLUDED."TmpTrailerGeotabId"
        OR d."TmpTrailerId" IS DISTINCT FROM EXCLUDED."TmpTrailerId";
        -- RecordLastChangedUtc not evaluated as it should never match.

    -- If SetEntityStatusDeletedForMissingDevices is TRUE, mark missing devices as deleted.
    IF "SetEntityStatusDeletedForMissingDevices" THEN
        UPDATE public."Devices2" d
        SET "EntityStatus" = 0,
            "RecordLastChangedUtc" = clock_timestamp() AT TIME ZONE 'UTC'
        WHERE NOT EXISTS (
            SELECT 1 FROM public."stg_Devices2" s WHERE s."id" = d."id"
        );
    END IF;

    -- Clear staging table.
    TRUNCATE TABLE public."stg_Devices2";

    -- Drop temporary table.
    DROP TABLE "TMP_DeduplicatedStaging";
END;
$BODY$;
-- <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Add EngineHours and Odometer columns to Trips2 and stg_Trips2:
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'Trips2' AND column_name = 'EngineHours') THEN
        ALTER TABLE public."Trips2" ADD COLUMN "EngineHours" double precision NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'Trips2' AND column_name = 'Odometer') THEN
        ALTER TABLE public."Trips2" ADD COLUMN "Odometer" double precision NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stg_Trips2' AND column_name = 'EngineHours') THEN
        ALTER TABLE public."stg_Trips2" ADD COLUMN "EngineHours" double precision NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stg_Trips2' AND column_name = 'Odometer') THEN
        ALTER TABLE public."stg_Trips2" ADD COLUMN "Odometer" double precision NULL;
    END IF;
END $$;

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Trips2 function:
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
        "EngineHours",
        "IdlingDurationTicks",
        "MaximumSpeed",
        "NextTripStart",
        "Odometer",
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
        s."EngineHours",
        s."IdlingDurationTicks",
        s."MaximumSpeed",
        s."NextTripStart",
        s."Odometer",
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
        "EngineHours" = EXCLUDED."EngineHours",
        "IdlingDurationTicks" = EXCLUDED."IdlingDurationTicks",
        "MaximumSpeed" = EXCLUDED."MaximumSpeed",
        "NextTripStart" = EXCLUDED."NextTripStart",
        "Odometer" = EXCLUDED."Odometer",
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
        OR d."EngineHours" IS DISTINCT FROM EXCLUDED."EngineHours"
        OR d."IdlingDurationTicks" IS DISTINCT FROM EXCLUDED."IdlingDurationTicks"
        OR d."MaximumSpeed" IS DISTINCT FROM EXCLUDED."MaximumSpeed"
        OR d."NextTripStart" IS DISTINCT FROM EXCLUDED."NextTripStart"
        OR d."Odometer" IS DISTINCT FROM EXCLUDED."Odometer"
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

ALTER FUNCTION public."spMerge_stg_Trips2"() OWNER TO geotabadapter_client;
GRANT EXECUTE ON FUNCTION public."spMerge_stg_Trips2"() TO geotabadapter_client;
REVOKE ALL ON FUNCTION public."spMerge_stg_Trips2"() FROM PUBLIC;
-- <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create AuditLogs2 table:
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'AuditLogs2') THEN
        CREATE TABLE public."AuditLogs2" (
            "id" uuid NOT NULL,
            "GeotabId" character varying(50) NOT NULL,
            "Comment" text NULL,
            "DateTime" timestamp without time zone NOT NULL,
            "Name" character varying(255) NULL,
            "UserName" character varying(255) NULL,
            "Version" bigint NULL,
            "RecordCreationTimeUtc" timestamp without time zone NOT NULL
        ) PARTITION BY RANGE ("DateTime");

        ALTER TABLE public."AuditLogs2" OWNER TO geotabadapter_client;

        ALTER TABLE ONLY public."AuditLogs2"
            ADD CONSTRAINT "PK_AuditLogs2" PRIMARY KEY ("id", "DateTime");
    END IF;
END $$;
-- <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
/*** [END] Part 2 of 3: Database Upgrades Above ***/



/*** [START] Part 3 of 3: Database Version Update Below ***/
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc")
SELECT "UpgradeDatabaseVersion", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "TMP_UpgradeDatabaseVersionTable";
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
/*** [END] Part 3 of 3: Database Version Update Above ***/
