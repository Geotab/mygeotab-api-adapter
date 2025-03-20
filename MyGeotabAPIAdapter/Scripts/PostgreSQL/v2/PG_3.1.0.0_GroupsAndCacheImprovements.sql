-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.0.0.0 to version 3.1.0.0.
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
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('3.1.0.0');

DO $$	 
DECLARE 
    required_starting_database_version TEXT DEFAULT '3.0.0.0';
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
-- Create Groups2 table:
CREATE TABLE public."Groups2" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "Children" text,
    "Color" character varying(50),
    "Comments" character varying(1024),
    "Name" character varying(255),
    "Reference" character varying(255),
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL,
	CONSTRAINT "PK_Groups2" PRIMARY KEY ("id")
);

ALTER TABLE IF EXISTS public."Groups2"
    OWNER to geotabadapter_client;
	
ALTER TABLE public."Groups2"
ADD CONSTRAINT "UK_Groups2_GeotabId" UNIQUE ("GeotabId");

CREATE SEQUENCE public."Groups2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER SEQUENCE public."Groups2_id_seq" OWNER TO geotabadapter_client;

ALTER SEQUENCE public."Groups2_id_seq" OWNED BY public."Groups2".id;

REVOKE ALL ON TABLE public."Groups2" FROM geotabadapter_client;

GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."Groups2" TO geotabadapter_client;

GRANT ALL ON TABLE public."Groups2" TO geotabadapter_client;

CREATE INDEX "IX_Groups2_RecordLastChangedUtc" ON public."Groups2" ("RecordLastChangedUtc");

ALTER TABLE ONLY public."Groups2" ALTER COLUMN id SET DEFAULT nextval('public."Groups2_id_seq"'::regclass);

GRANT ALL ON SEQUENCE public."Groups2_id_seq" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Add Group-related columns to other tables:
ALTER TABLE public."Devices2"
ADD COLUMN "Groups" TEXT;

ALTER TABLE public."Users2"
ADD COLUMN "CompanyGroups" TEXT;

ALTER TABLE public."Zones2"
ADD COLUMN "Groups" TEXT;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_Devices2 table:
CREATE TABLE public."stg_Devices2" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone,
    "ActiveTo" timestamp without time zone,
    "Comment" character varying(1024),
    "DeviceType" character varying(50) NOT NULL,
	"Groups" text,
    "LicensePlate" character varying(50),
    "LicenseState" character varying(50),
    "Name" character varying(100) NOT NULL,
    "ProductId" integer,
    "SerialNumber" character varying(12),
    "VIN" character varying(50),
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

ALTER TABLE public."stg_Devices2" OWNER TO geotabadapter_client;

ALTER TABLE ONLY public."stg_Devices2"
    ADD CONSTRAINT "PK_stg_Devices2" PRIMARY KEY (id);

CREATE INDEX IF NOT EXISTS "IX_stg_Devices2_id_RecordLastChangedUtc"
    ON public."stg_Devices2" ("id", "RecordLastChangedUtc" DESC);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_Devices2 function:
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
	    -- OR d."RecordLastChangedUtc" IS DISTINCT FROM EXCLUDED."RecordLastChangedUtc";

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
-- Create stg_Diagnostics2 table:
CREATE TABLE public."stg_Diagnostics2" (
    id bigint NOT NULL,
    "GeotabId" character varying(100) NOT NULL,
    "GeotabGUIDString" character varying(100) NOT NULL,
    "HasShimId" boolean NOT NULL,
    "FormerShimGeotabGUIDString" character varying(100),
    "ControllerId" character varying(100),
    "DiagnosticCode" integer,
    "DiagnosticName" character varying(255) NOT NULL,
    "DiagnosticSourceId" character varying(50) NOT NULL,
    "DiagnosticSourceName" character varying(255) NOT NULL,
    "DiagnosticUnitOfMeasureId" character varying(50) NOT NULL,
    "DiagnosticUnitOfMeasureName" character varying(255) NOT NULL,
    "OBD2DTC" character varying(50),
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

ALTER TABLE public."stg_Diagnostics2" OWNER TO geotabadapter_client;

CREATE SEQUENCE public."stg_Diagnostics2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER SEQUENCE public."stg_Diagnostics2_id_seq" OWNER TO geotabadapter_client;

ALTER SEQUENCE public."stg_Diagnostics2_id_seq" OWNED BY public."stg_Diagnostics2".id;

ALTER TABLE ONLY public."stg_Diagnostics2" ALTER COLUMN id SET DEFAULT nextval('public."stg_Diagnostics2_id_seq"'::regclass);

ALTER TABLE ONLY public."stg_Diagnostics2"
    ADD CONSTRAINT "PK_stg_Diagnostics2" PRIMARY KEY (id);

CREATE INDEX IF NOT EXISTS "IX_stg_Diagnostics2_GeotabGUIDString_RecordLastChangedUtc"
ON public."stg_Diagnostics2" ("GeotabGUIDString", "RecordLastChangedUtc" DESC);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_Diagnostics2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_Diagnostics2"(
	"SetEntityStatusDeletedForMissingDiagnostics" boolean DEFAULT false)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description: 
--   Upserts records from the stg_Diagnostics2 staging table to the Diagnostics2 table and 
--   then truncates the staging table. If the SetEntityStatusDeletedForMissingDiagnostics 
--   parameter is set to true, the EntityStatus column will be set to 0 (Deleted) for 
--   any records in the Diagnostics2 table for which there are no corresponding records 
--   with the same GeotabId in the stg_Diagnostics2 table.
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

    -- If SetEntityStatusDeletedForMissingDiagnostics is TRUE, set EntityStatus to 0 (Deleted)
    -- for any records in Diagnostics2 where there is no corresponding record with the same GeotabGUIDString
    -- in stg_Diagnostics2.
    IF "SetEntityStatusDeletedForMissingDiagnostics" THEN
        UPDATE public."Diagnostics2" d
        SET "EntityStatus" = 0,
            "RecordLastChangedUtc" = clock_timestamp() AT TIME ZONE 'UTC'
        WHERE NOT EXISTS (
            SELECT 1 FROM public."stg_Diagnostics2" s
            WHERE s."GeotabId" = d."GeotabId"
        );
    END IF;

    -- Update entity status to 0 (deleted) for missing records
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
$BODY$;

ALTER FUNCTION public."spMerge_stg_Diagnostics2"(boolean)
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_Diagnostics2"(boolean) TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_Diagnostics2"(boolean) FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_Groups2 table:
CREATE TABLE public."stg_Groups2" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "Children" text,
    "Color" character varying(50),
    "Comments" character varying(1024),
    "Name" character varying(255),
    "Reference" character varying(255),
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

ALTER TABLE public."stg_Groups2" OWNER TO geotabadapter_client;

CREATE SEQUENCE public."stg_Groups2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER SEQUENCE public."stg_Groups2_id_seq" OWNER TO geotabadapter_client;

ALTER SEQUENCE public."stg_Groups2_id_seq" OWNED BY public."stg_Groups2".id;

ALTER TABLE ONLY public."stg_Groups2" ALTER COLUMN id SET DEFAULT nextval('public."stg_Groups2_id_seq"'::regclass);

ALTER TABLE ONLY public."stg_Groups2"
    ADD CONSTRAINT "PK_stg_Groups2" PRIMARY KEY (id);

CREATE INDEX "IX_stg_Groups2_RecordLastChangedUtc" ON public."stg_Groups2" USING btree ("RecordLastChangedUtc");


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_Groups2 function:
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
        -- OR d."RecordLastChangedUtc" IS DISTINCT FROM EXCLUDED."RecordLastChangedUtc";

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
-- Create stg_Users2 table:
CREATE TABLE public."stg_Users2" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone NOT NULL,
    "ActiveTo" timestamp without time zone NOT NULL,
	"CompanyGroups" text,
    "EmployeeNo" character varying(50),
    "FirstName" character varying(255),
    "HosRuleSet" character varying,
    "IsDriver" boolean NOT NULL,
    "LastAccessDate" timestamp without time zone,
    "LastName" character varying(255),
    "Name" character varying(255) NOT NULL,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

ALTER TABLE public."stg_Users2" OWNER TO geotabadapter_client;

ALTER TABLE ONLY public."stg_Users2"
    ADD CONSTRAINT "PK_stg_Users2" PRIMARY KEY (id);

CREATE INDEX IF NOT EXISTS "IX_stg_Users2_id_RecordLastChangedUtc"
ON public."stg_Users2" ("id", "RecordLastChangedUtc" DESC);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_Users2 function:
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
		-- OR d."RecordLastChangedUtc" IS DISTINCT FROM EXCLUDED."RecordLastChangedUtc";

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
-- Add Unique Constraint on GeotabId to ZoneTypes2 table:
ALTER TABLE public."ZoneTypes2"
ADD CONSTRAINT "UK_ZoneTypes2_GeotabId" UNIQUE ("GeotabId");


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_ZoneTypes2 table:
CREATE TABLE public."stg_ZoneTypes2" (
    id bigint NOT NULL,
    "GeotabId" character varying(100) NOT NULL,
    "Comment" character varying(255),
    "Name" character varying(255) NOT NULL,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

ALTER TABLE public."stg_ZoneTypes2" OWNER TO geotabadapter_client;

CREATE SEQUENCE public."stg_ZoneTypes2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER SEQUENCE public."stg_ZoneTypes2_id_seq" OWNER TO geotabadapter_client;

ALTER SEQUENCE public."stg_ZoneTypes2_id_seq" OWNED BY public."stg_ZoneTypes2".id;

ALTER TABLE ONLY public."stg_ZoneTypes2" ALTER COLUMN id SET DEFAULT nextval('public."stg_ZoneTypes2_id_seq"'::regclass);

ALTER TABLE ONLY public."stg_ZoneTypes2"
    ADD CONSTRAINT "PK_stg_ZoneTypes2" PRIMARY KEY (id);

CREATE INDEX IF NOT EXISTS "IX_stg_ZoneTypes2_GeotabId_RecordLastChangedUtc"
ON public."stg_ZoneTypes2" ("GeotabId", "RecordLastChangedUtc" DESC);



-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_ZoneTypes2 function:
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
        "Comment" = EXCLUDED."Comment",
        "Name" = EXCLUDED."Name",
        "EntityStatus" = EXCLUDED."EntityStatus",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
    WHERE 
        d."Comment" IS DISTINCT FROM EXCLUDED."Comment"
        OR d."Name" IS DISTINCT FROM EXCLUDED."Name"
        OR d."EntityStatus" IS DISTINCT FROM EXCLUDED."EntityStatus";
        -- OR d."RecordLastChangedUtc" IS DISTINCT FROM EXCLUDED."RecordLastChangedUtc";

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
-- Create stg_Zones2 table:
CREATE TABLE public."stg_Zones2" (
    id bigint NOT NULL,
    "GeotabId" character varying(100) NOT NULL,
    "ActiveFrom" timestamp without time zone,
    "ActiveTo" timestamp without time zone,
    "CentroidLatitude" double precision,
    "CentroidLongitude" double precision,
    "Comment" character varying(500),
    "Displayed" boolean,
    "ExternalReference" character varying(255),
	"Groups" text,
    "MustIdentifyStops" boolean,
    "Name" character varying(255) NOT NULL,
    "Points" text,
    "ZoneTypeIds" text,
    "Version" bigint,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

ALTER TABLE public."stg_Zones2" OWNER TO geotabadapter_client;

ALTER TABLE ONLY public."stg_Zones2"
    ADD CONSTRAINT "PK_stg_Zones2" PRIMARY KEY (id);

CREATE INDEX IF NOT EXISTS "IX_stg_Zones2_id_RecordLastChangedUtc"
ON public."stg_Zones2" ("id", "RecordLastChangedUtc" DESC);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_Zones2 function:
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
	    -- OR d."RecordLastChangedUtc" IS DISTINCT FROM EXCLUDED."RecordLastChangedUtc";

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
-- Update vwStatsForLevel1DBMaintenance view to only return tables in the public schema:
CREATE OR REPLACE VIEW public."vwStatsForLevel1DBMaintenance" AS
 WITH orderedrows AS (
         SELECT pg_stat_user_tables.schemaname AS "SchemaName",
            pg_stat_user_tables.relname AS "TableName",
            pg_stat_user_tables.n_live_tup AS "LiveTuples",
            pg_stat_user_tables.n_dead_tup AS "DeadTuples",
            ((pg_stat_user_tables.n_dead_tup)::numeric / (NULLIF(pg_stat_user_tables.n_live_tup, 0))::numeric) AS "PctDeadTuples",
            pg_stat_user_tables.n_mod_since_analyze AS "ModsSinceLastAnalyze",
            ((pg_stat_user_tables.n_mod_since_analyze)::numeric / (NULLIF(pg_stat_user_tables.n_live_tup, 0))::numeric) AS "PctModsSinceLastAnalyze"
           FROM pg_stat_user_tables
          WHERE (((pg_stat_user_tables.n_dead_tup)::numeric > (0.2 * (pg_stat_user_tables.n_live_tup)::numeric)) OR ((pg_stat_user_tables.n_mod_since_analyze)::numeric > (0.1 * (pg_stat_user_tables.n_live_tup)::numeric)) OR (pg_stat_user_tables.n_dead_tup > 1000)) 
			AND pg_stat_user_tables.schemaname = 'public'
          ORDER BY ((pg_stat_user_tables.n_dead_tup)::numeric / (NULLIF(pg_stat_user_tables.n_live_tup, 0))::numeric) DESC, ((pg_stat_user_tables.n_mod_since_analyze)::numeric / (NULLIF(pg_stat_user_tables.n_live_tup, 0))::numeric) DESC
        )
 SELECT row_number() OVER () AS "RowId",
    "SchemaName",
    "TableName",
    "LiveTuples",
    "DeadTuples",
    "PctDeadTuples",
    "ModsSinceLastAnalyze",
    "PctModsSinceLastAnalyze"
   FROM orderedrows;

ALTER VIEW public."vwStatsForLevel1DBMaintenance" OWNER TO geotabadapter_client;
/*** [END] Part 2 of 3: Database Upgrades (tables, sequences, views) Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc")
SELECT "UpgradeDatabaseVersion", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "TMP_UpgradeDatabaseVersionTable";
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
/*** [END] Part 3 of 3: Database Version Update Above ***/
