-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.10.0.0 to version 3.11.0.0.
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
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('3.11.0.0');

DO $$	 
DECLARE 
    required_starting_database_version TEXT DEFAULT '3.10.0.0';
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
-- Update spMerge_stg_DVIRDefects2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_DVIRDefects2"(
	)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_DVIRDefects2 staging table to the DVIRDefects2
--   table and then truncates the staging table. Handles changes to the DVIRLogDateTime
--   (partitioning key) by deleting the existing record and inserting the new version.
--
-- Notes:
--   - Uses a multi-step process (DELETE movers + INSERT ON CONFLICT) within a transaction block.
--   - Handles moving child (DVIRDefectRemarks2) records as well to prevent foreign key violations.
-- ==========================================================================================
BEGIN
    -- Create temporary table to store IDs of any records where DateTime has changed.
    DROP TABLE IF EXISTS "TMP_MovedRecordIds";
    CREATE TEMP TABLE "TMP_MovedRecordIds" (
        "id" uuid PRIMARY KEY
    );	

    -- De-duplicate staging table by selecting the latest record per "id".
    -- Uses DISTINCT ON to keep only the latest record per "id" based.
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("id") *
    FROM public."stg_DVIRDefects2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;

    -- Add an index to the temporary table on the column used for conflict resolution.
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id");

    -- Identify records where DVIRLogDateTime has changed.
    INSERT INTO "TMP_MovedRecordIds" ("id")
    SELECT s."id"
    FROM "TMP_DeduplicatedStaging" s
    INNER JOIN public."DVIRDefects2" d ON s."id" = d."id"
    WHERE s."DVIRLogDateTime" IS DISTINCT FROM d."DVIRLogDateTime";
	
	-- Save into a temporary table any DVIRDefectRemarks2 records that are children
	-- of DVIRDefects2 records that are moving.
	DROP TABLE IF EXISTS "TMP_DVIRDefectRemarks2ToReattach";
    CREATE TEMP TABLE "TMP_DVIRDefectRemarks2ToReattach" AS
    SELECT r.*
    FROM public."DVIRDefectRemarks2" r
    INNER JOIN "TMP_MovedRecordIds" m ON r."DVIRDefectId" = m."id";	

	-- Detach (delete) any DVIRDefectRemarks2 records that are children of DVIRDefects2 records that are moving.
    DELETE FROM public."DVIRDefectRemarks2" AS r
    USING "TMP_DVIRDefectRemarks2ToReattach" m
    WHERE r."id" = m."id";

    -- Delete the old versions of these records from the target table.
    DELETE FROM public."DVIRDefects2" AS d
    USING "TMP_MovedRecordIds" m
    WHERE d."id" = m."id";

    -- Perform upsert.
    -- Inserts new records AND records whose DVIRLogDateTime changed (deleted above).
    INSERT INTO public."DVIRDefects2" AS d (
        "id", 
		"GeotabId", 
		"DVIRLogId", 
		"DVIRLogDateTime", 
		"DefectListAssetType",
        "DefectListId", 
		"DefectListName", 
		"PartId", 
		"PartName", 
		"DefectId", 
		"DefectName",
        "DefectSeverityId", 
		"RepairDateTime", 
		"RepairStatusId", 
		"RepairUserId",
        "RecordLastChangedUtc"
    )
    SELECT
        s."id", 
		s."GeotabId", 
		s."DVIRLogId", 
		s."DVIRLogDateTime", 
		s."DefectListAssetType",
        s."DefectListId", 
		s."DefectListName", 
		s."PartId", 
		s."PartName", 
		s."DefectId", 
		s."DefectName",
        s."DefectSeverityId", 
		s."RepairDateTime", 
		s."RepairStatusId", 
		s."RepairUserId",
        s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("DVIRLogDateTime", "id")
    DO UPDATE SET
		-- "id" and "DVIRLogDateTime" excluded since they are subject of ON CONFLICT.
		-- If only "DVIRLogDateTime" changed, the original record will have been deleted
		-- and a new one will be inserted instead of updating the existing record.		
        "GeotabId" = EXCLUDED."GeotabId",
        "DVIRLogId" = EXCLUDED."DVIRLogId",
        "DefectListAssetType" = EXCLUDED."DefectListAssetType",
        "DefectListId" = EXCLUDED."DefectListId",
        "DefectListName" = EXCLUDED."DefectListName",
        "PartId" = EXCLUDED."PartId",
        "PartName" = EXCLUDED."PartName",
        "DefectId" = EXCLUDED."DefectId",
        "DefectName" = EXCLUDED."DefectName",
        "DefectSeverityId" = EXCLUDED."DefectSeverityId",
        "RepairDateTime" = EXCLUDED."RepairDateTime",
        "RepairStatusId" = EXCLUDED."RepairStatusId",
        "RepairUserId" = EXCLUDED."RepairUserId",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId" OR
        d."DVIRLogId" IS DISTINCT FROM EXCLUDED."DVIRLogId" OR
        d."DefectListAssetType" IS DISTINCT FROM EXCLUDED."DefectListAssetType" OR
        d."DefectListId" IS DISTINCT FROM EXCLUDED."DefectListId" OR
        d."DefectListName" IS DISTINCT FROM EXCLUDED."DefectListName" OR
        d."PartId" IS DISTINCT FROM EXCLUDED."PartId" OR
        d."PartName" IS DISTINCT FROM EXCLUDED."PartName" OR
        d."DefectId" IS DISTINCT FROM EXCLUDED."DefectId" OR
        d."DefectName" IS DISTINCT FROM EXCLUDED."DefectName" OR
        d."DefectSeverityId" IS DISTINCT FROM EXCLUDED."DefectSeverityId" OR
        d."RepairDateTime" IS DISTINCT FROM EXCLUDED."RepairDateTime" OR
        d."RepairStatusId" IS DISTINCT FROM EXCLUDED."RepairStatusId" OR
        d."RepairUserId" IS DISTINCT FROM EXCLUDED."RepairUserId";
        -- RecordLastChangedUtc not evaluated as it should never match.

	-- Re-attach (insert) any DVIRDefectRemarks2 records that are children of DVIRDefects2 records that moved.
    INSERT INTO public."DVIRDefectRemarks2" (
        "id", 
		"GeotabId", 
		"DVIRDefectId", 
		"DVIRLogDateTime", 
		"DateTime",
        "Remark", 
		"RemarkUserId", 
		"RecordLastChangedUtc"
    )
    SELECT
        r."id", 
		r."GeotabId", 
		r."DVIRDefectId", 
		s."DVIRLogDateTime", -- Use the NEW partition key from the staged parent
		r."DateTime",
        r."Remark", 
		r."RemarkUserId", 
		r."RecordLastChangedUtc"
    FROM "TMP_DVIRDefectRemarks2ToReattach" r
    INNER JOIN "TMP_DeduplicatedStaging" s ON r."DVIRDefectId" = s."id";

    -- Clear staging table.
    TRUNCATE TABLE public."stg_DVIRDefects2";

    -- Clean up temporary tables.
    DROP TABLE IF EXISTS "TMP_MovedRecordIds";
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
	DROP TABLE IF EXISTS "TMP_DVIRDefectRemarks2ToReattach";

EXCEPTION
    WHEN OTHERS THEN
		-- Ensure temporary table cleanup on error.
		DROP TABLE IF EXISTS "TMP_MovedRecordIds";
		DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
		DROP TABLE IF EXISTS "TMP_DVIRDefectRemarks2ToReattach";
		
		-- Re-raise the original error to be caught by the calling application, if necessary.
		RAISE; 
END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_DVIRDefects2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_DVIRDefects2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_DVIRDefects2"() FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_DVIRLogs2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_DVIRLogs2"(
	)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_DVIRLogs2 staging table to the DVIRLogs2 table and then 
--	 truncates the staging table. Handles changes to the DateTime (partitioning key) by 
--	 deleting the existing record and inserting the new version.
--
-- Notes:
--   - Uses a multi-step process (DELETE movers + INSERT ON CONFLICT) within a transaction block.
--   - Handles moving child (DVIRDefects2) and grandchild (DVIRDefectRemarks2) records as well
-- 	   to prevent foreign key violations.
-- ==========================================================================================
BEGIN
    -- Create temporary table to store IDs of any records where DateTime has changed.
    DROP TABLE IF EXISTS "TMP_MovedRecordIds";
    CREATE TEMP TABLE "TMP_MovedRecordIds" (
        "id" uuid PRIMARY KEY
    );	

    -- De-duplicate staging table by selecting the latest record per "id".
    -- Uses DISTINCT ON to keep only the latest record per "id" based.
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("id") *
    FROM public."stg_DVIRLogs2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;

    -- Add an index to the temporary table on the column used for conflict resolution.
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id");

    -- Identify records where DateTime has changed.
    INSERT INTO "TMP_MovedRecordIds" ("id")
    SELECT s."id"
    FROM "TMP_DeduplicatedStaging" s
    INNER JOIN public."DVIRLogs2" d ON s."id" = d."id"
    WHERE s."DateTime" IS DISTINCT FROM d."DateTime";

	-- Save into a temporary table any DVIRDefectRemarks2 records that are grandchildren 
	-- of DVIRLogs2 records that are moving.
	DROP TABLE IF EXISTS "TMP_DVIRDefectRemarks2ToReattach";
    CREATE TEMP TABLE "TMP_DVIRDefectRemarks2ToReattach" AS
    SELECT r.*
    FROM public."DVIRDefectRemarks2" r
    INNER JOIN public."DVIRDefects2" d ON r."DVIRDefectId" = d."id"
    INNER JOIN "TMP_MovedRecordIds" m ON d."DVIRLogId" = m."id";

	-- Save into a temporary table any DVIRDefects2 records that are children 
	-- of DVIRLogs2 records that are moving.
	DROP TABLE IF EXISTS "TMP_DVIRDefects2ToReattach";
    CREATE TEMP TABLE "TMP_DVIRDefects2ToReattach" AS
    SELECT d.*
    FROM public."DVIRDefects2" d
    INNER JOIN "TMP_MovedRecordIds" m ON d."DVIRLogId" = m."id";

	-- Detach (delete) any DVIRDefectRemarks2 records that are grandchildren
	-- of DVIRLogs2 records that are moving.
    DELETE FROM public."DVIRDefectRemarks2" AS r
    USING "TMP_DVIRDefectRemarks2ToReattach" m
    WHERE r."id" = m."id";

	-- Detach (delete) any DVIRDefects2 records that are children
	-- of DVIRLogs2 records that are moving.
    DELETE FROM public."DVIRDefects2" AS d
    USING "TMP_DVIRDefects2ToReattach" m
    WHERE d."id" = m."id";

    -- Delete the old versions of these records from the target table.
    DELETE FROM public."DVIRLogs2" AS d
    USING "TMP_MovedRecordIds" m
    WHERE d."id" = m."id";

    -- Perform upsert.
    -- Inserts new records AND records whose DateTime changed (deleted above).
    INSERT INTO public."DVIRLogs2" AS d (
        "id", 
		"GeotabId", 
		"AuthorityAddress", 
		"AuthorityName", 
		"CertifiedByUserId", 
		"CertifiedDate",
        "CertifyRemark", 
		"DateTime", 
		"DeviceId", 
		"DriverId", 
		"DriverRemark", 
		"DurationTicks",
        "EngineHours", 
		"IsSafeToOperate", 
		"LoadHeight", 
		"LoadWidth", 
		"LocationLatitude",
        "LocationLongitude", 
		"LogType", 
		"Odometer", 
		"RepairDate", 
		"RepairedByUserId",
        "RepairRemark", 
		"Version", 
		"RecordLastChangedUtc"
    )
    SELECT
        s."id", 
		s."GeotabId", 
		s."AuthorityAddress", 
		s."AuthorityName", 
		s."CertifiedByUserId", 
		s."CertifiedDate",
        s."CertifyRemark", 
		s."DateTime", 
		s."DeviceId", 
		s."DriverId", 
		s."DriverRemark", 
		s."DurationTicks",
        s."EngineHours", 
		s."IsSafeToOperate", 
		s."LoadHeight", 
		s."LoadWidth", 
		s."LocationLatitude",
        s."LocationLongitude", 
		s."LogType", 
		s."Odometer", 
		s."RepairDate", 
		s."RepairedByUserId",
        s."RepairRemark", 
		s."Version", 
		s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("DateTime", "id") 
	DO UPDATE SET
		-- "id" and "DateTime" excluded since they are subject of ON CONFLICT.
		-- If only "DateTime" changed, the original record will have been deleted
		-- and a new one will be inserted instead of updating the existing record.		
        "GeotabId" = EXCLUDED."GeotabId",
        "AuthorityAddress" = EXCLUDED."AuthorityAddress",
        "AuthorityName" = EXCLUDED."AuthorityName",
        "CertifiedByUserId" = EXCLUDED."CertifiedByUserId",
        "CertifiedDate" = EXCLUDED."CertifiedDate",
        "CertifyRemark" = EXCLUDED."CertifyRemark",
        "DeviceId" = EXCLUDED."DeviceId",
        "DriverId" = EXCLUDED."DriverId",
        "DriverRemark" = EXCLUDED."DriverRemark",
        "DurationTicks" = EXCLUDED."DurationTicks",
        "EngineHours" = EXCLUDED."EngineHours",
        "IsSafeToOperate" = EXCLUDED."IsSafeToOperate",
        "LoadHeight" = EXCLUDED."LoadHeight",
        "LoadWidth" = EXCLUDED."LoadWidth",
        "LocationLatitude" = EXCLUDED."LocationLatitude",
        "LocationLongitude" = EXCLUDED."LocationLongitude",
        "LogType" = EXCLUDED."LogType",
        "Odometer" = EXCLUDED."Odometer",
        "RepairDate" = EXCLUDED."RepairDate",
        "RepairedByUserId" = EXCLUDED."RepairedByUserId",
        "RepairRemark" = EXCLUDED."RepairRemark",
        "Version" = EXCLUDED."Version",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId" OR
        d."AuthorityAddress" IS DISTINCT FROM EXCLUDED."AuthorityAddress" OR
        d."AuthorityName" IS DISTINCT FROM EXCLUDED."AuthorityName" OR
        d."CertifiedByUserId" IS DISTINCT FROM EXCLUDED."CertifiedByUserId" OR
        d."CertifiedDate" IS DISTINCT FROM EXCLUDED."CertifiedDate" OR
        d."CertifyRemark" IS DISTINCT FROM EXCLUDED."CertifyRemark" OR
        d."DeviceId" IS DISTINCT FROM EXCLUDED."DeviceId" OR
        d."DriverId" IS DISTINCT FROM EXCLUDED."DriverId" OR
        d."DriverRemark" IS DISTINCT FROM EXCLUDED."DriverRemark" OR
        d."DurationTicks" IS DISTINCT FROM EXCLUDED."DurationTicks" OR
        d."EngineHours" IS DISTINCT FROM EXCLUDED."EngineHours" OR
        d."IsSafeToOperate" IS DISTINCT FROM EXCLUDED."IsSafeToOperate" OR
        d."LoadHeight" IS DISTINCT FROM EXCLUDED."LoadHeight" OR
        d."LoadWidth" IS DISTINCT FROM EXCLUDED."LoadWidth" OR
        d."LocationLatitude" IS DISTINCT FROM EXCLUDED."LocationLatitude" OR
        d."LocationLongitude" IS DISTINCT FROM EXCLUDED."LocationLongitude" OR
        d."LogType" IS DISTINCT FROM EXCLUDED."LogType" OR
        d."Odometer" IS DISTINCT FROM EXCLUDED."Odometer" OR
        d."RepairDate" IS DISTINCT FROM EXCLUDED."RepairDate" OR
        d."RepairedByUserId" IS DISTINCT FROM EXCLUDED."RepairedByUserId" OR
        d."RepairRemark" IS DISTINCT FROM EXCLUDED."RepairRemark" OR
        d."Version" IS DISTINCT FROM EXCLUDED."Version";
        -- RecordLastChangedUtc not evaluated as it should never match.

	-- Re-attach (insert) any DVIRDefects2 records that are children of DVIRLogs2 records that moved.
    INSERT INTO public."DVIRDefects2" (
        "id", 
		"GeotabId", 
		"DVIRLogId", 
		"DVIRLogDateTime", 
		"DefectListAssetType", 
		"DefectListId",
        "DefectListName", 
		"PartId", 
		"PartName", 
		"DefectId", 
		"DefectName", 
		"DefectSeverityId",
        "RepairDateTime", 
		"RepairStatusId", 
		"RepairUserId", 
		"RecordLastChangedUtc"
    )
    SELECT
        d."id", 
		d."GeotabId", 
		d."DVIRLogId", 
		s."DateTime", -- Use the NEW partition key from the staged parent
		d."DefectListAssetType", 
		d."DefectListId",
        d."DefectListName", 
		d."PartId", 
		d."PartName", 
		d."DefectId", 
		d."DefectName", 
		d."DefectSeverityId",
        d."RepairDateTime", 
		d."RepairStatusId", 
		d."RepairUserId", 
		d."RecordLastChangedUtc"
    FROM "TMP_DVIRDefects2ToReattach" d
    INNER JOIN "TMP_DeduplicatedStaging" s ON d."DVIRLogId" = s."id";

	-- Re-attach (insert) any DVIRDefectRemarks2 records that are grandchildren of DVIRLogs2 records that moved.
    INSERT INTO public."DVIRDefectRemarks2" (
        "id", 
		"GeotabId", 
		"DVIRDefectId", 
		"DVIRLogDateTime", 
		"DateTime",
        "Remark", 
		"RemarkUserId", 
		"RecordLastChangedUtc"
    )
    SELECT
        r."id", 
		r."GeotabId", 
		r."DVIRDefectId", 
		s."DateTime", -- Use the NEW partition key from the staged parent
		r."DateTime",
        r."Remark", 
		r."RemarkUserId", 
		r."RecordLastChangedUtc"
    FROM "TMP_DVIRDefectRemarks2ToReattach" r
    INNER JOIN "TMP_DVIRDefects2ToReattach" d ON r."DVIRDefectId" = d."id"
    INNER JOIN "TMP_DeduplicatedStaging" s ON d."DVIRLogId" = s."id";

    -- Clear staging table.
    TRUNCATE TABLE public."stg_DVIRLogs2";

    DROP TABLE IF EXISTS "TMP_MovedRecordIds";
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    DROP TABLE IF EXISTS "TMP_DVIRDefects2ToReattach";
    DROP TABLE IF EXISTS "TMP_DVIRDefectRemarks2ToReattach";	

EXCEPTION
	WHEN OTHERS THEN
		-- Ensure temporary table cleanup on error.
		DROP TABLE IF EXISTS "TMP_MovedRecordIds";
		DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
		DROP TABLE IF EXISTS "TMP_DVIRDefects2ToReattach";
		DROP TABLE IF EXISTS "TMP_DVIRDefectRemarks2ToReattach";			
		
		-- Re-raise the original error to be caught by the calling application, if necessary.
		RAISE; 
END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_DVIRLogs2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_DVIRLogs2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_DVIRLogs2"() FROM PUBLIC;
/*** [END] Part 2 of 3: Database Upgrades (tables, sequences, views) Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc")
SELECT "UpgradeDatabaseVersion", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "TMP_UpgradeDatabaseVersionTable";
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
/*** [END] Part 3 of 3: Database Version Update Above ***/
