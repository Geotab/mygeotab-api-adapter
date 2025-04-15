-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- DESCRIPTION: 
--   The purpose of this script is to upgrade the MyGeotab API Adapter database 
--   from version 3.2.0.0 to version 3.3.0.0.
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
INSERT INTO "TMP_UpgradeDatabaseVersionTable" VALUES ('3.3.0.0');

DO $$	 
DECLARE 
    required_starting_database_version TEXT DEFAULT '3.2.0.0';
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
-- Create ExceptionEvents2 table:
--
-- Name: ExceptionEvents2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--
CREATE TABLE public."ExceptionEvents2" (
    "id" uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone NOT NULL,
    "ActiveTo" timestamp without time zone,
    "DeviceId" bigint NOT NULL,
    "Distance" real,
    "DriverId" bigint,
    "DurationTicks" bigint,
    "LastModifiedDateTime" timestamp without time zone,
    "RuleId" bigint NOT NULL,
    "State" integer,
    "Version" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
)
PARTITION BY RANGE ("ActiveFrom");

ALTER TABLE public."ExceptionEvents2" OWNER to geotabadapter_client;

--
-- Name: ExceptionEvents2 PK_ExceptionEvents2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--
ALTER TABLE public."ExceptionEvents2"
    ADD CONSTRAINT "PK_ExceptionEvents2" PRIMARY KEY ("ActiveFrom","id");

--
-- Name: ExceptionEvents2 FK_ExceptionEvents2_Devices2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--
ALTER TABLE public."ExceptionEvents2"
    ADD CONSTRAINT "FK_ExceptionEvents2_Devices2" FOREIGN KEY ("DeviceId")
        REFERENCES public."Devices2" ("id");

--
-- Name: ExceptionEvents2 FK_ExceptionEvents2_Rules2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--
ALTER TABLE public."ExceptionEvents2"
    ADD CONSTRAINT "FK_ExceptionEvents2_Rules2" FOREIGN KEY ("RuleId")
        REFERENCES public."Rules2" ("id");

--
-- Name: ExceptionEvents2 FK_ExceptionEvents2_Users2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--
ALTER TABLE public."ExceptionEvents2"
    ADD CONSTRAINT "FK_ExceptionEvents2_Users2" FOREIGN KEY ("DriverId")
        REFERENCES public."Users2" ("id");

--
-- Name: IX_ExceptionEvents2_DeviceId; Type: INDEX; Schema: public; Owner: geotabadapter_client
--
CREATE INDEX "IX_ExceptionEvents2_DeviceId" ON public."ExceptionEvents2" USING btree ("DeviceId");

--
-- Name: IX_ExceptionEvents2_DriverId; Type: INDEX; Schema: public; Owner: geotabadapter_client
--
CREATE INDEX "IX_ExceptionEvents2_DriverId" ON public."ExceptionEvents2" USING btree ("DriverId");

--
-- Name: IX_ExceptionEvents2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--
CREATE INDEX "IX_ExceptionEvents2_RecordLastChangedUtc" ON public."ExceptionEvents2" USING btree ("RecordLastChangedUtc");

--
-- Name: IX_ExceptionEvents2_RuleId; Type: INDEX; Schema: public; Owner: geotabadapter_client
--
CREATE INDEX "IX_ExceptionEvents2_RuleId" ON public."ExceptionEvents2" USING btree ("RuleId");

--
-- Name: IX_ExceptionEvents2_TimeRange; Type: INDEX; Schema: public; Owner: geotabadapter_client
--
CREATE INDEX "IX_ExceptionEvents2_TimeRange" ON public."ExceptionEvents2" USING btree ("ActiveFrom", "ActiveTo");

--
-- Name: IX_ExceptionEvents2_TimeRange_Driver_State; Type: INDEX; Schema: public; Owner: geotabadapter_client
--
CREATE INDEX "IX_ExceptionEvents2_TimeRange_Driver_State" ON public."ExceptionEvents2" USING btree ("ActiveFrom", "ActiveTo", "DriverId",  "State");

--
-- Name: IX_ExceptionEvents2_TimeRange_Rule_Driver_State; Type: INDEX; Schema: public; Owner: geotabadapter_client
--
CREATE INDEX "IX_ExceptionEvents2_TimeRange_Rule_Driver_State" ON public."ExceptionEvents2" USING btree ("ActiveFrom", "ActiveTo", "RuleId", "DriverId", "State");

--
-- Name: IX_ExceptionEvents2_TimeRange_Rule_State; Type: INDEX; Schema: public; Owner: geotabadapter_client
--
CREATE INDEX "IX_ExceptionEvents2_TimeRange_Rule_State" ON public."ExceptionEvents2" USING btree ("ActiveFrom", "ActiveTo", "RuleId", "State");

--
-- Name: IX_ExceptionEvents2_State; Type: INDEX; Schema: public; Owner: geotabadapter_client
--
CREATE INDEX "IX_ExceptionEvents2_State" ON public."ExceptionEvents2" USING btree ("State");

GRANT DELETE, INSERT, SELECT, UPDATE ON TABLE public."ExceptionEvents2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_ExceptionEvents2 table:
--
-- Name: stg_ExceptionEvents2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--
CREATE TABLE public."stg_ExceptionEvents2" (
    "id" uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone NOT NULL,
    "ActiveTo" timestamp without time zone,
    "DeviceId" bigint NOT NULL,
    "Distance" real,
    "DriverId" bigint,
    "DurationTicks" bigint,
    "LastModifiedDateTime" timestamp without time zone,
    "RuleGeotabId" character varying(50) NOT NULL,
    "RuleId" bigint,
    "State" integer,
    "Version" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
) TABLESPACE pg_default;

ALTER TABLE public."stg_ExceptionEvents2" OWNER to geotabadapter_client;

--
-- Name: IX_stg_ExceptionEvents2_id_ActiveFrom; Type: INDEX; Schema: public; Owner: geotabadapter_client
--
CREATE INDEX "IX_stg_ExceptionEvents2_id_RecordLastChangedUtc" ON public."stg_ExceptionEvents2" USING btree ("id", "RecordLastChangedUtc");

GRANT ALL ON TABLE public."stg_ExceptionEvents2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_ExceptionEvents2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_ExceptionEvents2"(
	)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_ExceptionEvents2 staging table to the ExceptionEvents2
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
    FROM public."stg_ExceptionEvents2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id", "ActiveFrom");
	CREATE INDEX ON "TMP_DeduplicatedStaging" ("RuleGeotabId");

    -- Populate RuleId in the temp table by using the RuleGeotabId to find the corresponding 
	-- id in the Rules2 table.
    UPDATE "TMP_DeduplicatedStaging" s
    SET "RuleId" = r.id
    FROM public."Rules2" r
    WHERE s."RuleGeotabId" = r."GeotabId";
	
    -- Perform upsert.
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
        s."RuleId",
        s."State",
        s."Version",
        s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("id", "ActiveFrom")
    DO UPDATE SET
        "GeotabId" = EXCLUDED."GeotabId",
        "ActiveTo" = EXCLUDED."ActiveTo",
        "DeviceId" = EXCLUDED."DeviceId",
        "Distance" = EXCLUDED."Distance",
        "DriverId" = EXCLUDED."DriverId",
        "DurationTicks" = EXCLUDED."DurationTicks",
        "LastModifiedDateTime" = EXCLUDED."LastModifiedDateTime",
        "RuleId" = EXCLUDED."RuleId",
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
        OR d."RuleId" IS DISTINCT FROM EXCLUDED."RuleId"
        OR d."State" IS DISTINCT FROM EXCLUDED."State"
        OR d."Version" IS DISTINCT FROM EXCLUDED."Version";
        --OR d."RecordLastChangedUtc" IS DISTINCT FROM EXCLUDED."RecordLastChangedUtc";

    -- Clear staging table.
    TRUNCATE TABLE public."stg_ExceptionEvents2";

    -- Drop temporary table.
    DROP TABLE "TMP_DeduplicatedStaging";

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
