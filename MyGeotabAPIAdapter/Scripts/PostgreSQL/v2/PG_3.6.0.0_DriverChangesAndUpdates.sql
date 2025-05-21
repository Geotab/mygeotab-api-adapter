-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.5.0.0 to version 3.6.0.0.
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
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('3.6.0.0');

DO $$	 
DECLARE 
    required_starting_database_version TEXT DEFAULT '3.5.0.0';
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
-- Create DriverChanges2 table:
CREATE TABLE public."DriverChanges2" (
    "id" uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DriverId" bigint,
    "Type" character varying(50) NOT NULL,
    "Version" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_DriverChanges2" PRIMARY KEY ("DateTime", "id")
)
PARTITION BY RANGE ("DateTime");

ALTER TABLE public."DriverChanges2" OWNER TO geotabadapter_client;

-- Foreign Key Constraints
ALTER TABLE public."DriverChanges2"
    ADD CONSTRAINT "FK_DriverChanges2_Devices2" FOREIGN KEY ("DeviceId")
        REFERENCES public."Devices2" ("id");

ALTER TABLE public."DriverChanges2"
    ADD CONSTRAINT "FK_DriverChanges2_Users2" FOREIGN KEY ("DriverId")
        REFERENCES public."Users2" ("id");

-- Indexes
CREATE INDEX "IX_DriverChanges2_DeviceId" ON public."DriverChanges2" USING btree ("DeviceId");

CREATE INDEX "IX_DriverChanges2_DriverId" ON public."DriverChanges2" USING btree ("DriverId");

CREATE INDEX "IX_DriverChanges2_RecordLastChangedUtc" ON public."DriverChanges2" USING btree ("RecordLastChangedUtc");

CREATE INDEX "IX_DriverChanges2_Type" ON public."DriverChanges2" USING btree ("Type");

CREATE INDEX "IX_DriverChanges2_DateTime_Device_Type" ON public."DriverChanges2" USING btree ("DateTime", "DeviceId", "Type");

CREATE INDEX "IX_DriverChanges2_DateTime_Driver_Type" ON public."DriverChanges2" USING btree ("DateTime", "DriverId", "Type");


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_DriverChanges2 table:
CREATE TABLE public."stg_DriverChanges2" (
    "id" uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DriverId" bigint,
    "Type" character varying(50) NOT NULL,
    "Version" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

ALTER TABLE public."stg_DriverChanges2" OWNER TO geotabadapter_client;

CREATE INDEX "IX_stg_DriverChanges2_id_RecordLastChangedUtc" ON public."stg_DriverChanges2" USING btree ("id" ASC, "RecordLastChangedUtc" ASC);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_DriverChanges2 stored procedure:
CREATE OR REPLACE FUNCTION public."spMerge_stg_DriverChanges2"(
)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_DriverChanges2 staging table to the DriverChanges2
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
    -- Uses DISTINCT ON to keep only the latest record per "id" based.
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("id") *
    FROM public."stg_DriverChanges2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;

    -- Add an index to the temporary table on the column used for conflict resolution.
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id");

    -- Identify records where DateTime has changed.
    INSERT INTO "TMP_MovedRecordIds" ("id")
    SELECT s."id"
    FROM "TMP_DeduplicatedStaging" s
    INNER JOIN public."DriverChanges2" d ON s."id" = d."id"
    WHERE s."DateTime" IS DISTINCT FROM d."DateTime";

    -- Delete the old versions of these records from the target table.
    DELETE FROM public."DriverChanges2" AS d
    USING "TMP_MovedRecordIds" m
    WHERE d."id" = m."id";

    -- Perform upsert.
    -- Inserts new records AND records whose DateTime changed (deleted above).
    INSERT INTO public."DriverChanges2" AS d (
        "id",
        "GeotabId",
        "DateTime",
        "DeviceId",
        "DriverId",
        "Type",
        "Version",
        "RecordLastChangedUtc"
    )
    SELECT
        s."id",
        s."GeotabId",
        s."DateTime",
        s."DeviceId",
        s."DriverId",
        s."Type",
        s."Version",
        s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("id", "DateTime")
    DO UPDATE SET
		-- "id" and "DateTime" excluded since they are subject of ON CONFLICT.
		-- If only "DateTime" changed, the original record will have been deleted
		-- and a new one will be inserted instead of updating the existing record.	
        "GeotabId" = EXCLUDED."GeotabId",
        "DeviceId" = EXCLUDED."DeviceId",
        "DriverId" = EXCLUDED."DriverId",
        "Type" = EXCLUDED."Type",
        "Version" = EXCLUDED."Version",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc" -- Always update the timestamp
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId" OR
        d."DeviceId" IS DISTINCT FROM EXCLUDED."DeviceId" OR
        d."DriverId" IS DISTINCT FROM EXCLUDED."DriverId" OR
        d."Type" IS DISTINCT FROM EXCLUDED."Type" OR
        d."Version" IS DISTINCT FROM EXCLUDED."Version";
        -- RecordLastChangedUtc not evaluated as it should never match.

    -- Clear staging table.
    TRUNCATE TABLE public."stg_DriverChanges2";

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

ALTER FUNCTION public."spMerge_stg_DriverChanges2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_DriverChanges2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_DriverChanges2"() FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Devices2 stored procedure:
-- (Comment additions only)
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
        "id", 
        "GeotabId", 
        "ActiveFrom", 
        "ActiveTo", 
        "Comment", 
        "DeviceType", 
		"Groups", 
        "LicensePlate", 
        "LicenseState", 
        "Name", 
        "ProductId", 
        "SerialNumber", 
        "VIN", 
        "EntityStatus", 
        "RecordLastChangedUtc"
    )
    SELECT 
        s."id", 
        s."GeotabId", 
        s."ActiveFrom", 
        s."ActiveTo", 
        s."Comment", 
        s."DeviceType", 
		s."Groups", 
        s."LicensePlate", 
        s."LicenseState", 
        s."Name", 
        s."ProductId", 
        s."SerialNumber", 
        s."VIN", 
        s."EntityStatus", 
        s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("id") 
    DO UPDATE SET
		-- "id" excluded since it is subject of ON CONFLICT.
        "GeotabId" = EXCLUDED."GeotabId",
        "ActiveFrom" = EXCLUDED."ActiveFrom",
        "ActiveTo" = EXCLUDED."ActiveTo",
        "Comment" = EXCLUDED."Comment",
        "DeviceType" = EXCLUDED."DeviceType",
		"Groups" = EXCLUDED."Groups",
        "LicensePlate" = EXCLUDED."LicensePlate",
        "LicenseState" = EXCLUDED."LicenseState",
        "Name" = EXCLUDED."Name",
        "ProductId" = EXCLUDED."ProductId",
        "SerialNumber" = EXCLUDED."SerialNumber",
        "VIN" = EXCLUDED."VIN",
        "EntityStatus" = EXCLUDED."EntityStatus",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
	WHERE 
	    d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId"
	    OR d."ActiveFrom" IS DISTINCT FROM EXCLUDED."ActiveFrom"
	    OR d."ActiveTo" IS DISTINCT FROM EXCLUDED."ActiveTo"
	    OR d."Comment" IS DISTINCT FROM EXCLUDED."Comment"
	    OR d."DeviceType" IS DISTINCT FROM EXCLUDED."DeviceType"
		OR d."Groups" IS DISTINCT FROM EXCLUDED."Groups"
	    OR d."LicensePlate" IS DISTINCT FROM EXCLUDED."LicensePlate"
	    OR d."LicenseState" IS DISTINCT FROM EXCLUDED."LicenseState"
	    OR d."Name" IS DISTINCT FROM EXCLUDED."Name"
	    OR d."ProductId" IS DISTINCT FROM EXCLUDED."ProductId"
	    OR d."SerialNumber" IS DISTINCT FROM EXCLUDED."SerialNumber"
	    OR d."VIN" IS DISTINCT FROM EXCLUDED."VIN"
	    OR d."EntityStatus" IS DISTINCT FROM EXCLUDED."EntityStatus";
	    -- RecordLastChangedUtc not evaluated as it should never match.

    -- If SetEntityStatusDeletedForMissingDevices is TRUE, mark missing devices as deleted.
    IF "SetEntityStatusDeletedForMissingDevices" THEN
        UPDATE public."Devices2" d
        SET "EntityStatus" = 0,
            "RecordLastChangedUtc" = clock_timestamp() AT TIME ZONE 'UTC'
        WHERE NOT EXISTS (
            SELECT 1 FROM public."stg_Devices2" s
            WHERE s."id" = d."id"
        );
    END IF;

    -- Clear staging table.
    TRUNCATE TABLE public."stg_Devices2";

    -- Drop temporary table.
    DROP TABLE "TMP_DeduplicatedStaging";

END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_Devices2"(boolean)
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_Devices2"(boolean) TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_Devices2"(boolean) FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Groups2 stored procedure:
-- (Comment additions only)
CREATE OR REPLACE FUNCTION public."spMerge_stg_Groups2"(
	"SetEntityStatusDeletedForMissingGroups" boolean DEFAULT false)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description: 
--   Upserts records from the stg_Groups2 staging table to the Groups2 table and then
--   truncates the staging table. If the SetEntityStatusDeletedForMissingGroups 
--   parameter is set to true, the EntityStatus column will be set to 0 (Deleted) for 
--   any records in the Groups2 table for which there are no corresponding records 
--   with the same GeotabId in the stg_Groups2 table.
--
-- Notes:
--   - No transaction used as application should manage the transaction.
-- ==========================================================================================
BEGIN
    -- De-duplicate staging table by selecting the latest record per GeotabId.
    -- Uses DISTINCT ON to keep only the latest record per GeotabId.
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("GeotabId") *
    FROM public."stg_Groups2"
    ORDER BY "GeotabId", "RecordLastChangedUtc" DESC;
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("GeotabId");

    -- Perform upsert.
    INSERT INTO public."Groups2" AS d (
        "GeotabId", 
		"Children", 
		"Color", 
        "Comments", 
        "Name", 
		"Reference", 
        "EntityStatus", 
        "RecordLastChangedUtc"
    )
    SELECT 
        s."GeotabId", 
		s."Children",
		s."Color",
        s."Comments", 
        s."Name", 
		s."Reference", 
        s."EntityStatus", 
        s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("GeotabId") 
    DO UPDATE SET
		-- "id" is unique key, but "GeotabId" is the logical key for matching.
		-- "id" excluded because it is database-generated on insert.
		-- "GeotabId" excluded since it is subject of ON CONFLICT.
		"Children" = EXCLUDED."Children",
        "Color" = EXCLUDED."Color",
		"Comments" = EXCLUDED."Comments",
        "Name" = EXCLUDED."Name",
		"Reference" = EXCLUDED."Reference",
        "EntityStatus" = EXCLUDED."EntityStatus",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
    WHERE 
		d."Children" IS DISTINCT FROM EXCLUDED."Children"
		OR d."Color" IS DISTINCT FROM EXCLUDED."Color"
        OR d."Comments" IS DISTINCT FROM EXCLUDED."Comments"
        OR d."Name" IS DISTINCT FROM EXCLUDED."Name"
		OR d."Reference" IS DISTINCT FROM EXCLUDED."Reference"
        OR d."EntityStatus" IS DISTINCT FROM EXCLUDED."EntityStatus";
        -- RecordLastChangedUtc not evaluated as it should never match.

    -- If SetEntityStatusDeletedForMissingGroups is TRUE, mark missing Groups as deleted.
    IF "SetEntityStatusDeletedForMissingGroups" THEN
        UPDATE public."Groups2" d
        SET "EntityStatus" = 0,
            "RecordLastChangedUtc" = clock_timestamp() AT TIME ZONE 'UTC'
        WHERE NOT EXISTS (
            SELECT 1 FROM public."stg_Groups2" s
            WHERE s."GeotabId" = d."GeotabId"
        );
    END IF;

    -- Clear staging table.
    TRUNCATE TABLE public."stg_Groups2";

    -- Drop temporary table.
    DROP TABLE "TMP_DeduplicatedStaging";

END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_Groups2"(boolean)
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_Groups2"(boolean) TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_Groups2"(boolean) FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Rules2 stored procedure:
-- (Fixed match on GeotabId vs. id and comment additions)
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
    -- De-duplicate staging table by selecting the latest record per GeotabId (id is 
	-- auto-generated on insert). Note that RecordLastChangedUtc is set in the order in which 
	-- results are retrieved via GetFeed.
	-- Uses DISTINCT ON to keep only the latest record per GeotabId.
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
		-- "id" is unique key, but "GeotabId" is the logical key for matching.
		-- "id" excluded because it is database-generated on insert.
		-- "GeotabId" excluded since it is subject of ON CONFLICT.		
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
        -- RecordLastChangedUtc not evaluated as it should never match.

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
-- Update spMerge_stg_Users2 stored procedure:
-- (Comment additions only)
CREATE OR REPLACE FUNCTION public."spMerge_stg_Users2"(
	"SetEntityStatusDeletedForMissingUsers" boolean DEFAULT false)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
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
$BODY$;

ALTER FUNCTION public."spMerge_stg_Users2"(boolean)
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_Users2"(boolean) TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_Users2"(boolean) FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Zones2 stored procedure:
-- (Comment additions only)
CREATE OR REPLACE FUNCTION public."spMerge_stg_Zones2"(
	"SetEntityStatusDeletedForMissingZones" boolean DEFAULT false)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description: 
--		Upserts records from the stg_Zones2 staging table to the Zones2 table and then
--		truncates the staging table. If the SetEntityStatusDeletedForMissingZones 
--		parameter is set to true, the EntityStatus column will be set to 0 (Deleted) for 
--		any records in the Zones2 table for which there are no corresponding records with 
--		the same ids in the stg_Zones2 table.
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
    FROM public."stg_Zones2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id");

    -- Perform upsert.
    INSERT INTO public."Zones2" AS d (
        "id", 
        "GeotabId", 
        "ActiveFrom", 
        "ActiveTo", 
        "CentroidLatitude",
        "CentroidLongitude",		
        "Comment",
        "Displayed",
        "ExternalReference",
		"Groups",
        "MustIdentifyStops",
        "Name", 
        "Points",
        "ZoneTypeIds",
        "Version",
        "EntityStatus", 
        "RecordLastChangedUtc"
    )
    SELECT 
        s."id", 
        s."GeotabId", 
        s."ActiveFrom", 
        s."ActiveTo", 
        s."CentroidLatitude", 
        s."CentroidLongitude", 
        s."Comment", 
        s."Displayed", 
        s."ExternalReference", 
		s."Groups", 
        s."MustIdentifyStops", 
        s."Name", 
        s."Points", 
        s."ZoneTypeIds", 
        s."Version", 
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
        "CentroidLatitude" = EXCLUDED."CentroidLatitude",
        "CentroidLongitude" = EXCLUDED."CentroidLongitude",
        "Comment" = EXCLUDED."Comment",
        "Displayed" = EXCLUDED."Displayed",
        "ExternalReference" = EXCLUDED."ExternalReference",
		"Groups" = EXCLUDED."Groups",
        "MustIdentifyStops" = EXCLUDED."MustIdentifyStops",
        "Name" = EXCLUDED."Name",
        "Points" = EXCLUDED."Points",
        "ZoneTypeIds" = EXCLUDED."ZoneTypeIds",
        "Version" = EXCLUDED."Version",
        "EntityStatus" = EXCLUDED."EntityStatus",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
	WHERE 
	    d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId"
	    OR d."ActiveFrom" IS DISTINCT FROM EXCLUDED."ActiveFrom"
	    OR d."ActiveTo" IS DISTINCT FROM EXCLUDED."ActiveTo"
	    OR d."CentroidLatitude" IS DISTINCT FROM EXCLUDED."CentroidLatitude"
	    OR d."CentroidLongitude" IS DISTINCT FROM EXCLUDED."CentroidLongitude"
	    OR d."Comment" IS DISTINCT FROM EXCLUDED."Comment"
	    OR d."Displayed" IS DISTINCT FROM EXCLUDED."Displayed"
	    OR d."ExternalReference" IS DISTINCT FROM EXCLUDED."ExternalReference"
		OR d."Groups" IS DISTINCT FROM EXCLUDED."Groups"
	    OR d."MustIdentifyStops" IS DISTINCT FROM EXCLUDED."MustIdentifyStops"
	    OR d."Name" IS DISTINCT FROM EXCLUDED."Name"
	    OR d."Points" IS DISTINCT FROM EXCLUDED."Points"
	    OR d."ZoneTypeIds" IS DISTINCT FROM EXCLUDED."ZoneTypeIds"
	    OR d."Version" IS DISTINCT FROM EXCLUDED."Version"
	    OR d."EntityStatus" IS DISTINCT FROM EXCLUDED."EntityStatus";
	    -- RecordLastChangedUtc not evaluated as it should never match.

    -- If SetEntityStatusDeletedForMissingZones is TRUE, mark missing Zones as deleted.
    IF "SetEntityStatusDeletedForMissingZones" THEN
        UPDATE public."Zones2" d
        SET "EntityStatus" = 0,
            "RecordLastChangedUtc" = clock_timestamp() AT TIME ZONE 'UTC'
        WHERE NOT EXISTS (
            SELECT 1 FROM public."stg_Zones2" s
            WHERE s."id" = d."id"
        );
    END IF;

    -- Clear staging table.
    TRUNCATE TABLE public."stg_Zones2";

    -- Drop temporary table.
    DROP TABLE "TMP_DeduplicatedStaging";

END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_Zones2"(boolean)
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_Zones2"(boolean) TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_Zones2"(boolean) FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_ZoneTypes2 stored procedure:
-- (Comment additions only)
CREATE OR REPLACE FUNCTION public."spMerge_stg_ZoneTypes2"(
	"SetEntityStatusDeletedForMissingZoneTypes" boolean DEFAULT false)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description: 
--   Upserts records from the stg_ZoneTypes2 staging table to the ZoneTypes2 table and then
--   truncates the staging table. If the SetEntityStatusDeletedForMissingZoneTypes 
--   parameter is set to true, the EntityStatus column will be set to 0 (Deleted) for 
--   any records in the ZoneTypes2 table for which there are no corresponding records 
--   with the same GeotabId in the stg_ZoneTypes2 table.
--
-- Notes:
--   - No transaction used as application should manage the transaction.
-- ==========================================================================================
BEGIN
    -- De-duplicate staging table by selecting the latest record per GeotabId.
    -- Uses DISTINCT ON to keep only the latest record per GeotabId.
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("GeotabId") *
    FROM public."stg_ZoneTypes2"
    ORDER BY "GeotabId", "RecordLastChangedUtc" DESC;
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("GeotabId");

    -- Perform upsert.
    INSERT INTO public."ZoneTypes2" AS d (
        "GeotabId", 
        "Comment", 
        "Name", 
        "EntityStatus", 
        "RecordLastChangedUtc"
    )
    SELECT 
        s."GeotabId", 
        s."Comment", 
        s."Name", 
        s."EntityStatus", 
        s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("GeotabId") 
    DO UPDATE SET
		-- "id" is unique key, but "GeotabId" is the logical key for matching.
		-- "id" excluded because it is database-generated on insert.
		-- "GeotabId" excluded since it is subject of ON CONFLICT.			
        "Comment" = EXCLUDED."Comment",
        "Name" = EXCLUDED."Name",
        "EntityStatus" = EXCLUDED."EntityStatus",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
    WHERE 
        d."Comment" IS DISTINCT FROM EXCLUDED."Comment"
        OR d."Name" IS DISTINCT FROM EXCLUDED."Name"
        OR d."EntityStatus" IS DISTINCT FROM EXCLUDED."EntityStatus";
        -- RecordLastChangedUtc not evaluated as it should never match.

    -- If SetEntityStatusDeletedForMissingZoneTypes is TRUE, mark missing ZoneTypes as deleted.
    IF "SetEntityStatusDeletedForMissingZoneTypes" THEN
        UPDATE public."ZoneTypes2" d
        SET "EntityStatus" = 0,
            "RecordLastChangedUtc" = clock_timestamp() AT TIME ZONE 'UTC'
        WHERE NOT EXISTS (
            SELECT 1 FROM public."stg_ZoneTypes2" s
            WHERE s."GeotabId" = d."GeotabId"
        );
    END IF;

    -- Clear staging table.
    TRUNCATE TABLE public."stg_ZoneTypes2";

    -- Drop temporary table.
    DROP TABLE "TMP_DeduplicatedStaging";

END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_ZoneTypes2"(boolean)
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_ZoneTypes2"(boolean) TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_ZoneTypes2"(boolean) FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_Trips2 stored procedure:
-- (Comment additions only)
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
-- Re-order columns in PK_ChargeEvents2 to leverage partition pruning:
-- ======================================================================
-- Script to change the Primary Key for public."ChargeEvents2"
-- From: (id ASC, StartTime ASC)
-- To:   (StartTime ASC, id ASC)
-- To align with partitioning on StartTime for better partition pruning.
-- ======================================================================

-- **********************************************************************
-- ** WARNING: HIGH IMPACT OPERATION! **
-- **********************************************************************
-- 1. BACKUP YOUR DATABASE before running this script.
-- 2. This script rebuilds the primary key for public."ChargeEvents2".
-- 3. Expect SIGNIFICANT DOWNTIME AND BLOCKING. Operations require
--    ACCESS EXCLUSIVE locks.
-- 4. Resource usage (CPU, IO, WAL generation) will be high,
--    proportional to table size.
-- 5. Plan and execute during a MAINTENANCE WINDOW.
-- 6. TEST THOROUGHLY in a non-production environment first.
-- 7. Identify and script any INBOUND Foreign Keys referencing
--    public."ChargeEvents2"."PK_ChargeEvents2" FROM OTHER tables
--    (see Step 1 and Step 6 below).
-- **********************************************************************

BEGIN;

-- Step 1: Drop Foreign Key constraints FROM OTHER tables referencing public."ChargeEvents2" (if any).
-- Example:
-- ALTER TABLE public."SomeOtherTable" DROP CONSTRAINT IF EXISTS "FK_SomeOtherTable_ChargeEvents2";
-- PRINT 'Dropped FK constraint FK_SomeOtherTable_ChargeEvents2 (if it existed)';
DO $$
BEGIN
    RAISE NOTICE 'Step 1: Placeholder for dropping INBOUND Foreign Keys. Manually identify and script these if they exist.';
END $$;

-- Step 2: Drop ALL existing Non-Primary Key Indexes on public."ChargeEvents2".
-- They will be rebuilt after the primary key changes.
DO $$
BEGIN
    RAISE NOTICE 'Step 2: Dropping Non-Primary Key Indexes on public."ChargeEvents2"...';
END $$;

DROP INDEX IF EXISTS public."IX_ChargeEvents2_DeviceId";
DO $$ BEGIN RAISE NOTICE 'Dropped index public."IX_ChargeEvents2_DeviceId" (if it existed).'; END $$;

DROP INDEX IF EXISTS public."IX_ChargeEvents2_RecordLastChangedUtc";
DO $$ BEGIN RAISE NOTICE 'Dropped index public."IX_ChargeEvents2_RecordLastChangedUtc" (if it existed).'; END $$;

DROP INDEX IF EXISTS public."IX_ChargeEvents2_StartTime_DeviceId";
DO $$ BEGIN RAISE NOTICE 'Dropped index public."IX_ChargeEvents2_StartTime_DeviceId" (if it existed).'; END $$;

DO $$ BEGIN RAISE NOTICE 'Finished dropping Non-Primary Key Indexes.'; END $$;

-- Step 3: Drop the existing Primary Key constraint.
DO $$
BEGIN
    RAISE NOTICE 'Step 3: Dropping existing Primary Key public."PK_ChargeEvents2"...';
END $$;

ALTER TABLE public."ChargeEvents2" DROP CONSTRAINT IF EXISTS "PK_ChargeEvents2";
DO $$ BEGIN RAISE NOTICE 'Dropped constraint public."PK_ChargeEvents2" (if it existed).'; END $$;
DO $$ BEGIN RAISE NOTICE 'Finished dropping Primary Key.'; END $$;

-- Step 4: Add the new Primary Key constraint with the correct column order
-- ("StartTime" ASC, "id" ASC).
DO $$
BEGIN
    RAISE NOTICE 'Step 4: Creating new Primary Key public."PK_ChargeEvents2" (StartTime, id)...';
END $$;

ALTER TABLE public."ChargeEvents2"
    ADD CONSTRAINT "PK_ChargeEvents2" PRIMARY KEY ("StartTime", "id");
DO $$ BEGIN RAISE NOTICE 'Finished creating new Primary Key.'; END $$;

-- Step 5: Recreate ALL Non-Primary Key Indexes exactly as they were before.
DO $$
BEGIN
    RAISE NOTICE 'Step 5: Recreating Non-Primary Key Indexes...';
END $$;

CREATE INDEX "IX_ChargeEvents2_DeviceId" ON public."ChargeEvents2" USING btree ("DeviceId");
DO $$ BEGIN RAISE NOTICE 'Recreated index public."IX_ChargeEvents2_DeviceId".'; END $$;

CREATE INDEX "IX_ChargeEvents2_RecordLastChangedUtc" ON public."ChargeEvents2" USING btree ("RecordLastChangedUtc");
DO $$ BEGIN RAISE NOTICE 'Recreated index public."IX_ChargeEvents2_RecordLastChangedUtc".'; END $$;

CREATE INDEX "IX_ChargeEvents2_StartTime_DeviceId" ON public."ChargeEvents2" USING btree ("StartTime", "DeviceId");
DO $$ BEGIN RAISE NOTICE 'Recreated index public."IX_ChargeEvents2_StartTime_DeviceId".'; END $$;

DO $$ BEGIN RAISE NOTICE 'Finished recreating Non-Primary Key Indexes.'; END $$;

-- Step 6: Recreate Foreign Key constraints FROM OTHER tables referencing public."ChargeEvents2" (if any).
-- Use the same constraint names as before.
-- Example:
-- ALTER TABLE public."SomeOtherTable" ADD CONSTRAINT "FK_SomeOtherTable_ChargeEvents2" FOREIGN KEY ("ReferencingColumn1InOtherTable", "ReferencingColumn2InOtherTable") REFERENCES public."ChargeEvents2" ("StartTime", "id");
-- RAISE NOTICE 'Recreated FK constraint FK_SomeOtherTable_ChargeEvents2';
DO $$
BEGIN
    RAISE NOTICE 'Step 6: Placeholder for recreating INBOUND Foreign Keys. Manually identify and script these if they were dropped in Step 1.';
END $$;

-- Step 7: Final commit or rollback
DO $$
BEGIN
    RAISE NOTICE 'All steps completed successfully. Review changes and then COMMIT.';
    RAISE NOTICE 'If any errors occurred or verification fails, issue a ROLLBACK.';
END $$;

COMMIT;
-- ROLLBACK; -- Uncomment to rollback if testing or errors occur

-- Optional: Verify index structure and PK afterwards
/*
SELECT
    i.relname AS index_name,
    am.amname AS index_type,
    c.relname AS table_name,
    ix.indisprimary AS is_primary_key,
    ix.indisunique AS is_unique,
    pg_get_indexdef(i.oid) AS index_definition
FROM
    pg_class c
JOIN
    pg_index ix ON c.oid = ix.indrelid
JOIN
    pg_class i ON i.oid = ix.indexrelid
JOIN
    pg_am am ON i.relam = am.oid
WHERE
    c.relname = 'ChargeEvents2' AND c.relnamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'public')
ORDER BY
    i.relname;

SELECT conname, pg_get_constraintdef(oid)
FROM pg_constraint
WHERE conrelid = 'public."ChargeEvents2"'::regclass
ORDER BY conname;
*/


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_ChargeEvents2 stored procedure:
-- Modified to handle potential StartTime (partitioning key) changes by deleting the
-- existing record and inserting the new version.
CREATE OR REPLACE FUNCTION public."spMerge_stg_ChargeEvents2"(
)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--  Upserts records from the stg_ChargeEvents2 staging table to the ChargeEvents2
--  table and then truncates the staging table. Handles changes to the StartTime
--  (partitioning key) by deleting the existing record and inserting the new version.
--
-- Notes:
--  - Uses a multi-step process (DELETE movers + MERGE) within a transaction.
-- ==========================================================================================
BEGIN

	-- Create temporary table to store IDs of any records where "StartTime" has changed.
    DROP TABLE IF EXISTS "TMP_MovedRecordIds";
    CREATE TEMP TABLE "TMP_MovedRecordIds" (
        "id" uuid PRIMARY KEY
    );
	
    -- De-duplicate staging table by selecting the latest record per "id".
    -- Uses DISTINCT ON to keep only the latest record per "id", ordering by RecordLastChangedUtc descending.
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("id") *
    FROM public."stg_ChargeEvents2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;

    -- Index the temp table for efficient joins and conflict detection.
    -- Index on "id" for identifying movers.
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id");
    -- Index on ("StartTime", "id") for the INSERT ... ON CONFLICT.
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("StartTime", "id");

	-- Identify records where StartTime has changed.
    INSERT INTO "TMP_MovedRecordIds" ("id")
    SELECT s."id"
    FROM "TMP_DeduplicatedStaging" s
    JOIN public."ChargeEvents2" d ON s."id" = d."id"
    WHERE s."StartTime" <> d."StartTime";

	-- Delete the old versions of these records from the target table.
    IF EXISTS (SELECT 1 FROM "TMP_MovedRecordIds") THEN
        DELETE FROM public."ChargeEvents2" AS d
        WHERE d."id" IN (SELECT m."id" FROM "TMP_MovedRecordIds" m);
    END IF;

    -- Perform upsert.
    -- "Movers" will be inserted because their old versions were deleted.
    -- Existing records (non-movers) with matching ("StartTime", "id") will be updated if data changed.
    -- Entirely new records will be inserted.
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
    ON CONFLICT ("StartTime", "id") -- Conflict on the full Primary Key of ChargeEvents2
    DO UPDATE SET
		-- "StartTime" and "id" excluded since they are subject of ON CONFLICT.	
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
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
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
        -- RecordLastChangedUtc not evaluated as it should never match.

    -- Clear staging table.
    TRUNCATE TABLE public."stg_ChargeEvents2";

    -- Drop temporary tables.
	DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
	DROP TABLE IF EXISTS "TMP_MovedRecordIds";

EXCEPTION
    WHEN OTHERS THEN
        -- Ensure temporary table cleanup on error.
        DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
        DROP TABLE IF EXISTS "TMP_MovedRecordIds";
        -- Rethrow the error.
        RAISE;
END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_ChargeEvents2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_ChargeEvents2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_ChargeEvents2"() FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Update spMerge_stg_ExceptionEvents2 stored procedure:
-- Modified to handle potential ActiveFrom (partitioning key) changes by deleting the
-- existing record and inserting the new version.
CREATE OR REPLACE FUNCTION public."spMerge_stg_ExceptionEvents2"(
)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--  Upserts records from the stg_ExceptionEvents2 staging table to the ExceptionEvents2
--  table and then truncates the staging table. Handles changes to ActiveFrom
--  (partitioning key) by deleting the existing record and inserting the new version.
--
-- Notes:
--  - Uses a multi-step process (DELETE movers + MERGE) within a transaction.
-- ==========================================================================================
BEGIN
	-- Create temporary table to store IDs of any records where ActiveFrom has changed.
    DROP TABLE IF EXISTS "TMP_MovedRecordIds";
    CREATE TEMP TABLE "TMP_MovedRecordIds" (
        "id" uuid PRIMARY KEY
    );

    -- De-duplicate staging table by selecting the latest record per "id".
    -- Uses DISTINCT ON to keep only the latest record per "id", ordering by "RecordLastChangedUtc" descending. 
	-- Also retrieve "RuleId" by using the "RuleGeotabId" to find the corresponding "id" in the Rules2 table.
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON (s."id") *
    FROM (
        SELECT
            stg.*,
            r.id AS "LookedUpRuleId"
        FROM public."stg_ExceptionEvents2" stg
        LEFT JOIN public."Rules2" r 
			ON stg."RuleGeotabId" = r."GeotabId"
    ) s
    ORDER BY s.id, s."RecordLastChangedUtc" DESC;

    -- Add necessary indexes to the temporary table
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id"); -- For identifying movers
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("ActiveFrom", "id"); -- For INSERT ON CONFLICT

	-- Identify records where ActiveFrom has changed.
    INSERT INTO "TMP_MovedRecordIds" ("id")
    SELECT s."id"
    FROM "TMP_DeduplicatedStaging" s
    JOIN public."ExceptionEvents2" d ON s."id" = d."id"
    WHERE s."ActiveFrom" <> d."ActiveFrom";

    -- Delete the old versions of these "mover" records from the target table.
    IF EXISTS (SELECT 1 FROM "TMP_MovedRecordIds") THEN
        DELETE FROM public."ExceptionEvents2" AS d
        WHERE d."id" IN (SELECT m."id" FROM "TMP_MovedRecordIds" m);
    END IF;

    -- Perform upsert.
    -- "Movers" will be inserted because their old versions were deleted.
    -- Existing records (non-movers) with matching ("ActiveFrom", "id") will be updated if data changed.
    -- Entirely new records will be inserted.	
    INSERT INTO public."ExceptionEvents2" AS d (
        "id", 
		"GeotabId", 
		"ActiveFrom", 
		"ActiveTo", 
		"DeviceId", 
		"Distance",
        "DriverId", 
		"DurationTicks", 
		"LastModifiedDateTime", 
		"RuleId", 
		"State",
        "Version", 
		"RecordLastChangedUtc"
    )
    SELECT
        s."id", 
		s."GeotabId", 
		s."ActiveFrom", 
		s."ActiveTo", 
		s."DeviceId", 
		s."Distance",
        s."DriverId", 
		s."DurationTicks", 
		s."LastModifiedDateTime", 
		s."LookedUpRuleId", -- s."LookedUpRuleId" from the temp table is used for the target "RuleId".
        s."State", 
		s."Version", 
		s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("ActiveFrom", "id") -- Conflict on the full Primary Key of ExceptionEvents2
    DO UPDATE SET
		-- "ActiveFrom" and "id" excluded since they are subject of ON CONFLICT.	
        "GeotabId" = EXCLUDED."GeotabId",
        "ActiveTo" = EXCLUDED."ActiveTo",
        "DeviceId" = EXCLUDED."DeviceId",
        "Distance" = EXCLUDED."Distance",
        "DriverId" = EXCLUDED."DriverId",
        "DurationTicks" = EXCLUDED."DurationTicks",
        "LastModifiedDateTime" = EXCLUDED."LastModifiedDateTime",
        "RuleId" = EXCLUDED."RuleId", -- EXCLUDED.RuleId refers to the value attempted to be inserted, which is LookedUpRuleId.
        "State" = EXCLUDED."State",
        "Version" = EXCLUDED."Version",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId"
        OR d."ActiveTo" IS DISTINCT FROM EXCLUDED."ActiveTo"
        OR d."DeviceId" IS DISTINCT FROM EXCLUDED."DeviceId"
        OR d."Distance" IS DISTINCT FROM EXCLUDED."Distance"
        OR d."DriverId" IS DISTINCT FROM EXCLUDED."DriverId"
        OR d."DurationTicks" IS DISTINCT FROM EXCLUDED."DurationTicks"
        OR d."LastModifiedDateTime" IS DISTINCT FROM EXCLUDED."LastModifiedDateTime"
        OR d."RuleId" IS DISTINCT FROM EXCLUDED."RuleId" -- Compares target's RuleId with the new LookedUpRuleId.
        OR d."State" IS DISTINCT FROM EXCLUDED."State"
        OR d."Version" IS DISTINCT FROM EXCLUDED."Version";
        -- RecordLastChangedUtc not evaluated as it should never match.

    -- Clear staging table.
    TRUNCATE TABLE public."stg_ExceptionEvents2";

    -- Drop temporary tables.
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    DROP TABLE IF EXISTS "TMP_MovedRecordIds";

EXCEPTION
    WHEN OTHERS THEN
        -- Ensure temporary table cleanup on error.
        DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
        DROP TABLE IF EXISTS "TMP_MovedRecordIds";
        -- Rethrow the error.
        RAISE;
END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_ExceptionEvents2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_ExceptionEvents2"() TO geotabadapter_client;
REVOKE ALL ON FUNCTION public."spMerge_stg_ExceptionEvents2"() FROM PUBLIC;
/*** [END] Part 2 of 3: Database Upgrades (tables, sequences, views) Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc")
SELECT "UpgradeDatabaseVersion", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "TMP_UpgradeDatabaseVersionTable";
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
/*** [END] Part 3 of 3: Database Version Update Above ***/
