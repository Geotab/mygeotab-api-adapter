-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.7.0.0 to version 3.8.0.0.
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
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('3.8.0.0');

DO $$	 
DECLARE 
    required_starting_database_version TEXT DEFAULT '3.7.0.0';
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
-- Modify Devices2 table (add TmpTrailerId and TmpTrailerGeotabId columns and UI_Devices2_TmpTrailerId index):
-- Add TmpTrailerGeotabId column to the Devices2 table.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Devices2'
          AND column_name = 'TmpTrailerGeotabId'
    ) THEN
        ALTER TABLE public."Devices2"
        ADD COLUMN "TmpTrailerGeotabId" VARCHAR(50) NULL;
        RAISE NOTICE 'Column "TmpTrailerGeotabId" added to public."Devices2".';
    ELSE
        RAISE NOTICE 'Column "TmpTrailerGeotabId" already exists in public."Devices2".';
    END IF;
END;
$$;

-- Add TmpTrailerId column to the Devices2 table.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Devices2'
          AND column_name = 'TmpTrailerId'
    ) THEN
        ALTER TABLE public."Devices2"
        ADD COLUMN "TmpTrailerId" UUID NULL;
        RAISE NOTICE 'Column "TmpTrailerId" added to public."Devices2".';
    ELSE
        RAISE NOTICE 'Column "TmpTrailerId" already exists in public."Devices2".';
    END IF;
END;
$$;

-- Ensure the column TmpTrailerId exists and, if so, add a unique index on it.
DO $$
BEGIN
    -- Check if the column TmpTrailerId exists
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Devices2'
          AND column_name = 'TmpTrailerId'
    ) THEN
        RAISE NOTICE 'Column "TmpTrailerId" does not exist in public."Devices2". Cannot create index. Please add the column first.';
    ELSE
        -- Column exists, now check if the unique index already exists before trying to create it.
        IF NOT EXISTS (
            SELECT 1
            FROM   pg_class c
            JOIN   pg_namespace n ON n.oid = c.relnamespace
            WHERE  c.relkind = 'i' -- Specific to indexes
              AND  c.relname = 'UI_Devices2_TmpTrailerId' -- Index name (case-sensitive)
              AND  n.nspname = 'public' -- Schema name
        ) THEN
            -- Create the unique nonclustered index for non-null values.
            CREATE UNIQUE INDEX "UI_Devices2_TmpTrailerId"
            ON public."Devices2" USING btree ("TmpTrailerId" ASC NULLS LAST)
            TABLESPACE pg_default
            WHERE "TmpTrailerId" IS NOT NULL;
            RAISE NOTICE 'Unique index "UI_Devices2_TmpTrailerId" created on public."Devices2"("TmpTrailerId") for non-null values.';
        ELSE
            RAISE NOTICE 'Index "UI_Devices2_TmpTrailerId" already exists on public."Devices2".';
        END IF;
    END IF;
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Modify stg_Devices2 table (add TmpTrailerId and TmpTrailerGeotabId columns):
-- Add TmpTrailerGeotabId column to the stg_Devices2 table.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'stg_Devices2'
          AND column_name = 'TmpTrailerGeotabId'
    ) THEN
        ALTER TABLE public."stg_Devices2"
        ADD COLUMN "TmpTrailerGeotabId" VARCHAR(50) NULL;
        RAISE NOTICE 'Column "TmpTrailerGeotabId" added to public."stg_Devices2".';
    ELSE
        RAISE NOTICE 'Column "TmpTrailerGeotabId" already exists in public."stg_Devices2".';
    END IF;
END;
$$;

-- Add TmpTrailerId column to the stg_Devices2 table.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'stg_Devices2'
          AND column_name = 'TmpTrailerId'
    ) THEN
        ALTER TABLE public."stg_Devices2"
        ADD COLUMN "TmpTrailerId" UUID NULL;
        RAISE NOTICE 'Column "TmpTrailerId" added to public."stg_Devices2".';
    ELSE
        RAISE NOTICE 'Column "TmpTrailerId" already exists in public."stg_Devices2".';
    END IF;
END;
$$;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Modify spMerge_stg_Devices2 function (to include new columns):
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
        "TmpTrailerGeotabId",
        "TmpTrailerId",
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
        s."TmpTrailerGeotabId",
        s."TmpTrailerId",
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
        "TmpTrailerGeotabId" = EXCLUDED."TmpTrailerGeotabId",
        "TmpTrailerId" = EXCLUDED."TmpTrailerId",
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
-- Create DVIRLogs2 table:
CREATE TABLE public."DVIRLogs2" (
    "id" uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "AuthorityAddress" character varying(255),
    "AuthorityName" character varying(255),
    "CertifiedByUserId" bigint,
    "CertifiedDate" timestamp without time zone,
    "CertifyRemark" text,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DriverId" bigint,
    "DriverRemark" text,
    "DurationTicks" bigint,
    "EngineHours" real,
    "IsSafeToOperate" boolean,
    "LoadHeight" real,
    "LoadWidth" real,
    "LocationLatitude" double precision,
    "LocationLongitude" double precision,
    "LogType" character varying(50),
    "Odometer" double precision,
    "RepairDate" timestamp without time zone,
    "RepairedByUserId" bigint,
    "RepairRemark" text,
    "Version" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_DVIRLogs2" PRIMARY KEY ("DateTime", "id")
) PARTITION BY RANGE ("DateTime");

ALTER TABLE IF EXISTS public."DVIRLogs2"
    OWNER TO geotabadapter_client;

CREATE INDEX "IX_DVIRLogs2_CertifiedByUserId" ON public."DVIRLogs2" ("CertifiedByUserId");

CREATE INDEX "IX_DVIRLogs2_DeviceId" ON public."DVIRLogs2" ("DeviceId");

CREATE INDEX "IX_DVIRLogs2_DriverId" ON public."DVIRLogs2" ("DriverId");

CREATE INDEX "IX_DVIRLogs2_RepairedByUserId" ON public."DVIRLogs2" ("RepairedByUserId");

CREATE INDEX "IX_DVIRLogs2_RecordLastChangedUtc" ON public."DVIRLogs2" ("RecordLastChangedUtc");

CREATE INDEX "IX_DVIRLogs2_DateTime_CertifiedByUser" ON public."DVIRLogs2" ("DateTime" ASC, "CertifiedByUserId" ASC);

CREATE INDEX "IX_DVIRLogs2_DateTime_Device" ON public."DVIRLogs2" ("DateTime" ASC, "DeviceId" ASC);

CREATE INDEX "IX_DVIRLogs2_DateTime_Driver" ON public."DVIRLogs2" ("DateTime" ASC, "DriverId" ASC);

CREATE INDEX "IX_DVIRLogs2_DateTime_RepairedByUser" ON public."DVIRLogs2" ("DateTime" ASC, "RepairedByUserId" ASC);

ALTER TABLE public."DVIRLogs2"
    ADD CONSTRAINT "FK_DVIRLogs2_Devices2" FOREIGN KEY ("DeviceId")
    REFERENCES public."Devices2" ("id");

ALTER TABLE public."DVIRLogs2"
    ADD CONSTRAINT "FK_DVIRLogs2_Users2_Driver" FOREIGN KEY ("DriverId")
    REFERENCES public."Users2" ("id");

ALTER TABLE public."DVIRLogs2"
    ADD CONSTRAINT "FK_DVIRLogs2_Users2_Certified" FOREIGN KEY ("CertifiedByUserId")
    REFERENCES public."Users2" ("id");

ALTER TABLE public."DVIRLogs2"
    ADD CONSTRAINT "FK_DVIRLogs2_Users2_Repaired" FOREIGN KEY ("RepairedByUserId")
    REFERENCES public."Users2" ("id");

GRANT ALL ON TABLE public."DVIRLogs2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_DVIRLogs2 table:
CREATE TABLE public."stg_DVIRLogs2" (
    "id" uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "AuthorityAddress" character varying(255),
    "AuthorityName" character varying(255),
    "CertifiedByUserId" bigint,
    "CertifiedDate" timestamp without time zone,
    "CertifyRemark" text,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DriverId" bigint,
    "DriverRemark" text,
    "DurationTicks" bigint,
    "EngineHours" real,
    "IsSafeToOperate" boolean,
    "LoadHeight" real,
    "LoadWidth" real,
    "LocationLatitude" double precision,
    "LocationLongitude" double precision,
    "LogType" character varying(50),
    "Odometer" double precision,
    "RepairDate" timestamp without time zone,
    "RepairedByUserId" bigint,
    "RepairRemark" text,
    "Version" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

ALTER TABLE IF EXISTS public."stg_DVIRLogs2"
    OWNER TO geotabadapter_client;

CREATE INDEX "IX_stg_DVIRLogs2_id_RecordLastChangedUtc" ON public."stg_DVIRLogs2" ("id" ASC, "RecordLastChangedUtc" ASC);

GRANT ALL ON TABLE public."stg_DVIRLogs2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_DVIRLogs2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_DVIRLogs2"()
    RETURNS void
    LANGUAGE plpgsql
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

    -- Clear staging table.
    TRUNCATE TABLE public."stg_DVIRLogs2";

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

ALTER FUNCTION public."spMerge_stg_DVIRLogs2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_DVIRLogs2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_DVIRLogs2"() FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create DefectSeverities2 table:
CREATE TABLE public."DefectSeverities2" (
    "id" smallint NOT NULL,
    "Name" character varying(50) NOT NULL,
    CONSTRAINT "PK_DefectSeverities2" PRIMARY KEY ("id"),
    CONSTRAINT "UK_DefectSeverities2_Name" UNIQUE ("Name")
);

INSERT INTO public."DefectSeverities2" ("id", "Name") VALUES
(-1, 'Unregulated'),
(0, 'Normal'),
(1, 'Critical');


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create RepairStatuses2 table:
CREATE TABLE public."RepairStatuses2" (
    "id" smallint NOT NULL,
    "Name" character varying(50) NOT NULL,
    CONSTRAINT "PK_RepairStatuses2" PRIMARY KEY ("id"),
    CONSTRAINT "UK_RepairStatuses2_Name" UNIQUE ("Name")
);

INSERT INTO public."RepairStatuses2" ("id", "Name") VALUES
(0, 'NotRepaired'),
(1, 'Repaired'),
(2, 'NotNecessary');


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create DVIRDefects2 table:
CREATE TABLE public."DVIRDefects2" (
    "id" uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DVIRLogId" uuid NOT NULL,
    "DVIRLogDateTime" timestamp without time zone NOT NULL,
    "DefectListAssetType" character varying(50),
    "DefectListId" character varying(50),
    "DefectListName" character varying(255),
    "PartId" character varying(50),
    "PartName" character varying(255),
    "DefectId" character varying(50),
    "DefectName" character varying(255),
    "DefectSeverityId" smallint,
    "RepairDateTime" timestamp without time zone,
    "RepairStatusId" smallint,
    "RepairUserId" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_DVIRDefects2" PRIMARY KEY ("DVIRLogDateTime", "id")
) PARTITION BY RANGE ("DVIRLogDateTime");

CREATE INDEX "IX_DVIRDefects2_DVIRLogId" ON public."DVIRDefects2" ("DVIRLogId");

CREATE INDEX "IX_DVIRDefects2_DefectSeverityId" ON public."DVIRDefects2" ("DefectSeverityId");

CREATE INDEX "IX_DVIRDefects2_RepairStatusId" ON public."DVIRDefects2" ("RepairStatusId");

CREATE INDEX "IX_DVIRDefects2_RepairUserId" ON public."DVIRDefects2" ("RepairUserId");

CREATE INDEX "IX_DVIRDefects2_RecordLastChangedUtc" ON public."DVIRDefects2" ("RecordLastChangedUtc");

ALTER TABLE public."DVIRDefects2"
    ADD CONSTRAINT "FK_DVIRDefects2_DVIRLogs2" FOREIGN KEY ("DVIRLogDateTime", "DVIRLogId")
    REFERENCES public."DVIRLogs2" ("DateTime", "id");

ALTER TABLE public."DVIRDefects2"
    ADD CONSTRAINT "FK_DVIRDefects2_DefectSeverities2" FOREIGN KEY ("DefectSeverityId")
    REFERENCES public."DefectSeverities2" ("id");

ALTER TABLE public."DVIRDefects2"
    ADD CONSTRAINT "FK_DVIRDefects2_RepairStatuses2" FOREIGN KEY ("RepairStatusId")
    REFERENCES public."RepairStatuses2" ("id");

ALTER TABLE public."DVIRDefects2"
    ADD CONSTRAINT "FK_DVIRDefects2_Users2" FOREIGN KEY ("RepairUserId")
    REFERENCES public."Users2" ("id");

GRANT ALL ON TABLE public."DVIRDefects2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_DVIRDefects2 table:
CREATE TABLE public."stg_DVIRDefects2" (
    "id" uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DVIRLogId" uuid NOT NULL,
    "DVIRLogDateTime" timestamp without time zone NOT NULL,
    "DefectListAssetType" character varying(50),
    "DefectListId" character varying(50),
    "DefectListName" character varying(255),
    "PartId" character varying(50),
    "PartName" character varying(255),
    "DefectId" character varying(50),
    "DefectName" character varying(255),
    "DefectSeverityId" smallint,
    "RepairDateTime" timestamp without time zone,
    "RepairStatusId" smallint,
    "RepairUserId" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

CREATE INDEX "IX_stg_DVIRDefects2_id_RecordLastChangedUtc" ON public."stg_DVIRDefects2" ("id" ASC, "RecordLastChangedUtc" ASC);

GRANT ALL ON TABLE public."stg_DVIRDefects2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_DVIRDefects2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_DVIRDefects2"()
    RETURNS void
    LANGUAGE plpgsql
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

    -- Clear staging table.
    TRUNCATE TABLE public."stg_DVIRDefects2";

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

ALTER FUNCTION public."spMerge_stg_DVIRDefects2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_DVIRDefects2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_DVIRDefects2"() FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create DVIRDefectRemarks2 table:
CREATE TABLE public."DVIRDefectRemarks2"(
	"id" uuid NOT NULL,
	"GeotabId" character varying(50) NOT NULL,
	"DVIRDefectId" uuid NOT NULL,
	"DVIRLogDateTime" timestamp without time zone NOT NULL,
	"DateTime" timestamp without time zone NOT NULL,
	"Remark" text,
	"RemarkUserId" bigint,
	"RecordLastChangedUtc" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_DVIRDefectRemarks2" PRIMARY KEY ("DVIRLogDateTime", "id")
) PARTITION BY RANGE ("DVIRLogDateTime");

CREATE INDEX "IX_DVIRDefectRemarks2_DVIRDefectId" ON public."DVIRDefectRemarks2" ("DVIRDefectId");

CREATE INDEX "IX_DVIRDefectRemarks2_RemarkUserId" ON public."DVIRDefectRemarks2" ("RemarkUserId");

CREATE INDEX "IX_DVIRDefectRemarks2_RecordLastChangedUtc" ON public."DVIRDefectRemarks2" ("RecordLastChangedUtc");

ALTER TABLE public."DVIRDefectRemarks2"
    ADD CONSTRAINT "FK_DVIRDefectRemarks2_DVIRDefects2" FOREIGN KEY ("DVIRLogDateTime", "DVIRDefectId")
    REFERENCES public."DVIRDefects2" ("DVIRLogDateTime", "id");

ALTER TABLE public."DVIRDefectRemarks2"
    ADD CONSTRAINT "FK_DVIRDefectRemarks2_Users2" FOREIGN KEY ("RemarkUserId")
    REFERENCES public."Users2" ("id");

GRANT ALL ON TABLE public."DVIRDefectRemarks2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_DVIRDefectRemarks2 table:
CREATE TABLE public."stg_DVIRDefectRemarks2"(
	"id" uuid NOT NULL,
	"GeotabId" character varying(50) NOT NULL,
	"DVIRDefectId" uuid NOT NULL,
	"DVIRLogDateTime" timestamp without time zone NOT NULL,
	"DateTime" timestamp without time zone NOT NULL,
	"Remark" text,
	"RemarkUserId" bigint,
	"RecordLastChangedUtc" timestamp without time zone NOT NULL
);

CREATE INDEX "IX_stg_DVIRDefectRemarks2_id_RecordLastChangedUtc" ON public."stg_DVIRDefectRemarks2" ("id" ASC, "RecordLastChangedUtc" ASC);

GRANT ALL ON TABLE public."stg_DVIRDefectRemarks2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_DVIRDefectRemarks2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_DVIRDefectRemarks2"()
    RETURNS void
    LANGUAGE plpgsql
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_DVIRDefectRemarks2 staging table to the DVIRDefectRemarks2 
--   table and then truncates the staging table. Handles changes to the DVIRLogDateTime 
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
    FROM public."stg_DVIRDefectRemarks2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;

    -- Add an index to the temporary table on the column used for conflict resolution.
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id");

    -- Identify records where DVIRLogDateTime has changed.
    INSERT INTO "TMP_MovedRecordIds" ("id")
    SELECT s."id"
    FROM "TMP_DeduplicatedStaging" s
    INNER JOIN public."DVIRDefectRemarks2" d ON s."id" = d."id"
    WHERE s."DVIRLogDateTime" IS DISTINCT FROM d."DVIRLogDateTime";

    -- Delete the old versions of these records from the target table.
    DELETE FROM public."DVIRDefectRemarks2" AS d
    USING "TMP_MovedRecordIds" m
    WHERE d."id" = m."id";

    -- Perform upsert.
    -- Inserts new records AND records whose DVIRLogDateTime changed (deleted above).
    INSERT INTO public."DVIRDefectRemarks2" AS d (
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
        s."id", 
		s."GeotabId", 
		s."DVIRDefectId", 
		s."DVIRLogDateTime", 
		s."DateTime",
        s."Remark", 
		s."RemarkUserId", 
		s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("DVIRLogDateTime", "id")
    DO UPDATE SET
		-- "id" and "DVIRLogDateTime" excluded since they are subject of ON CONFLICT.
		-- If only "DVIRLogDateTime" changed, the original record will have been deleted
		-- and a new one will be inserted instead of updating the existing record.		
        "GeotabId" = EXCLUDED."GeotabId",
        "DVIRDefectId" = EXCLUDED."DVIRDefectId",
        "DateTime" = EXCLUDED."DateTime",
        "Remark" = EXCLUDED."Remark",
        "RemarkUserId" = EXCLUDED."RemarkUserId",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId" OR
        d."DVIRDefectId" IS DISTINCT FROM EXCLUDED."DVIRDefectId" OR
        d."DateTime" IS DISTINCT FROM EXCLUDED."DateTime" OR
        d."Remark" IS DISTINCT FROM EXCLUDED."Remark" OR
        d."RemarkUserId" IS DISTINCT FROM EXCLUDED."RemarkUserId";
        -- RecordLastChangedUtc not evaluated as it should never match.

    -- Clear staging table.
    TRUNCATE TABLE public."stg_DVIRDefectRemarks2";

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

ALTER FUNCTION public."spMerge_stg_DVIRDefectRemarks2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_DVIRDefectRemarks2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_DVIRDefectRemarks2"() FROM PUBLIC;
/*** [END] Part 2 of 3: Database Upgrades (tables, sequences, views) Above ***/ 



/*** [START] Part 3 of 3: Database Version Update Below ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc")
SELECT "UpgradeDatabaseVersion", CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
FROM "TMP_UpgradeDatabaseVersionTable";
DROP TABLE IF EXISTS "TMP_UpgradeDatabaseVersionTable";
/*** [END] Part 3 of 3: Database Version Update Above ***/
