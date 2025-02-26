-- ================================================================================
-- DATABASE TYPE: PostgreSQL
-- 
-- NOTES: 
--   1: This script applies to the MyGeotab API Adapter database starting with
--	    application version 3.0.0.0. It does not apply to earlier versions of the
--      application. 
--   2: This script will not be updated. Any future schema changes will be included
--      in new incremental scripts tagged with the relevant application versions.
--   3: Be sure to connect to the "geotabadapterdb" before executing. 
--
-- DESCRIPTION: 
--   This script is intended for use in creating the database schema for version
--   3.0.0.0 of the MyGeotab API Adapter in an empty database.
-- ================================================================================


/*** [START] Part 1 of 4: Install required extensions ***/ 
CREATE EXTENSION IF NOT EXISTS pgstattuple; 
CREATE EXTENSION IF NOT EXISTS pg_stat_statements; 
/*** [END] Part 1 of 4: Install required extensions ***/ 



/*** [START] Part 2 of 4: pgAdmin-Generated Script (tables, sequences, views) ***/ 
--
-- PostgreSQL database dump
--

-- Dumped from database version 16.0
-- Dumped by pg_dump version 16.0

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: DBMaintenanceLogs2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."DBMaintenanceLogs2" (
    id bigint NOT NULL,
    "MaintenanceTypeId" smallint NOT NULL,
    "StartTimeUtc" timestamp without time zone NOT NULL,
    "EndTimeUtc" timestamp without time zone,
    "Success" boolean,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."DBMaintenanceLogs2" OWNER TO geotabadapter_client;

--
-- Name: DBMaintenanceLogs2_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_client
--

CREATE SEQUENCE public."DBMaintenanceLogs2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."DBMaintenanceLogs2_id_seq" OWNER TO geotabadapter_client;

--
-- Name: DBMaintenanceLogs2_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_client
--

ALTER SEQUENCE public."DBMaintenanceLogs2_id_seq" OWNED BY public."DBMaintenanceLogs2".id;


--
-- Name: DBPartitionInfo2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."DBPartitionInfo2"
(
	id bigint NOT NULL,
    "InitialMinDateTimeUTC" timestamp without time zone NOT NULL,
	"InitialPartitionInterval" character varying(50) COLLATE pg_catalog."default" NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."DBPartitionInfo2" OWNER to geotabadapter_client;

--
-- Name: DBPartitionInfo2_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_client
--

CREATE SEQUENCE public."DBPartitionInfo2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."DBPartitionInfo2_id_seq" OWNER TO geotabadapter_client;

--
-- Name: DBPartitionInfo2_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_client
--

ALTER SEQUENCE public."DBPartitionInfo2_id_seq" OWNED BY public."DBPartitionInfo2".id;


--
-- Name: Devices2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."Devices2" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone,
    "ActiveTo" timestamp without time zone,
    "Comment" character varying(1024),
    "DeviceType" character varying(50) NOT NULL,
    "LicensePlate" character varying(50),
    "LicenseState" character varying(50),
    "Name" character varying(100) NOT NULL,
    "ProductId" integer,
    "SerialNumber" character varying(12),
    "VIN" character varying(50),
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."Devices2" OWNER TO geotabadapter_client;

--
-- Name: DiagnosticIds2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."DiagnosticIds2" (
    id bigint NOT NULL,
    "GeotabGUIDString" character varying(100) NOT NULL,
    "GeotabId" character varying(100) NOT NULL,
    "HasShimId" boolean NOT NULL,
    "FormerShimGeotabGUIDString" character varying(100),
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."DiagnosticIds2" OWNER TO geotabadapter_client;

--
-- Name: DiagnosticIds2_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_client
--

CREATE SEQUENCE public."DiagnosticIds2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."DiagnosticIds2_id_seq" OWNER TO geotabadapter_client;

--
-- Name: DiagnosticIds2_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_client
--

ALTER SEQUENCE public."DiagnosticIds2_id_seq" OWNED BY public."DiagnosticIds2".id;


--
-- Name: Diagnostics2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."Diagnostics2" (
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


ALTER TABLE public."Diagnostics2" OWNER TO geotabadapter_client;

--
-- Name: Diagnostics2_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_client
--

CREATE SEQUENCE public."Diagnostics2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Diagnostics2_id_seq" OWNER TO geotabadapter_client;

--
-- Name: Diagnostics2_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_client
--

ALTER SEQUENCE public."Diagnostics2_id_seq" OWNED BY public."Diagnostics2".id;


--
-- Name: EntityMetadata2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."EntityMetadata2" (
    id bigint NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "EntityType" smallint NOT NULL,
    "EntityId" bigint NOT NULL,
    "IsDeleted" boolean DEFAULT false,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
)
PARTITION BY RANGE ("DateTime");


ALTER TABLE public."EntityMetadata2" OWNER TO geotabadapter_client;

--
-- Name: EntityMetadata2_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_client
--

CREATE SEQUENCE public."EntityMetadata2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."EntityMetadata2_id_seq" OWNER TO geotabadapter_client;

--
-- Name: EntityMetadata2_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_client
--

ALTER SEQUENCE public."EntityMetadata2_id_seq" OWNED BY public."EntityMetadata2".id;


--
-- Name: FaultData2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."FaultData2" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "AmberWarningLamp" boolean,
    "ClassCode" character varying(50),
    "ControllerId" character varying(100) NOT NULL,
    "ControllerName" character varying(255),
    "Count" integer NOT NULL,
    "DateTime" timestamp(4) without time zone NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DiagnosticId" bigint NOT NULL,
    "DismissDateTime" timestamp without time zone,
    "DismissUserId" bigint,
    "FailureModeCode" integer,
    "FailureModeId" character varying(50) NOT NULL,
    "FailureModeName" character varying(255),
    "FaultLampState" character varying(50),
    "FaultState" character varying(50),
    "MalfunctionLamp" boolean,
    "ProtectWarningLamp" boolean,
    "RedStopLamp" boolean,
    "Severity" character varying(50),
    "SourceAddress" integer,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
)
PARTITION BY RANGE ("DateTime");


ALTER TABLE public."FaultData2" OWNER TO geotabadapter_client;

--
-- Name: FaultDataLocations2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."FaultDataLocations2" (
    id bigint NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DateTime" timestamp(4) without time zone NOT NULL,
    "Latitude" double precision,
    "Longitude" double precision,
    "Speed" real,
    "Bearing" real,
    "Direction" character varying(3),
    "LongLatProcessed" boolean NOT NULL,
    "LongLatReason" smallint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
)
PARTITION BY RANGE ("DateTime");


ALTER TABLE public."FaultDataLocations2" OWNER TO geotabadapter_client;

--
-- Name: LogRecords2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."LogRecords2" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" bigint NOT NULL,
    "Latitude" double precision DEFAULT 0 NOT NULL,
    "Longitude" double precision DEFAULT 0 NOT NULL,
    "Speed" real DEFAULT 0 NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
)
PARTITION BY RANGE ("DateTime");


ALTER TABLE public."LogRecords2" OWNER TO geotabadapter_client;

--
-- Name: MiddlewareVersionInfo2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."MiddlewareVersionInfo2" (
    id bigint NOT NULL,
    "DatabaseVersion" character varying(50) NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."MiddlewareVersionInfo2" OWNER TO geotabadapter_client;

--
-- Name: MiddlewareVersionInfo2_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_client
--

CREATE SEQUENCE public."MiddlewareVersionInfo2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."MiddlewareVersionInfo2_id_seq" OWNER TO geotabadapter_client;

--
-- Name: MiddlewareVersionInfo2_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_client
--

ALTER SEQUENCE public."MiddlewareVersionInfo2_id_seq" OWNED BY public."MiddlewareVersionInfo2".id;


--
-- Name: MyGeotabVersionInfo2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."MyGeotabVersionInfo2" (
    "DatabaseName" character varying(58) NOT NULL,
    "Server" character varying(50) NOT NULL,
    "DatabaseVersion" character varying(50) NOT NULL,
    "ApplicationBuild" character varying(50) NOT NULL,
    "ApplicationBranch" character varying(50) NOT NULL,
    "ApplicationCommit" character varying(50) NOT NULL,
    "GoTalkVersion" character varying(50) NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."MyGeotabVersionInfo2" OWNER TO geotabadapter_client;

--
-- Name: OServiceTracking2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."OServiceTracking2" (
    id bigint NOT NULL,
    "ServiceId" character varying(50) NOT NULL,
    "AdapterVersion" character varying(50),
    "AdapterMachineName" character varying(100),
    "EntitiesLastProcessedUtc" timestamp without time zone,
    "LastProcessedFeedVersion" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."OServiceTracking2" OWNER TO geotabadapter_client;

--
-- Name: OServiceTracking2_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_client
--

CREATE SEQUENCE public."OServiceTracking2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."OServiceTracking2_id_seq" OWNER TO geotabadapter_client;

--
-- Name: OServiceTracking2_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_client
--

ALTER SEQUENCE public."OServiceTracking2_id_seq" OWNED BY public."OServiceTracking2".id;


--
-- Name: StatusData2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."StatusData2" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "Data" double precision,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DiagnosticId" bigint NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
)
PARTITION BY RANGE ("DateTime");


ALTER TABLE public."StatusData2" OWNER TO geotabadapter_client;

--
-- Name: StatusDataLocations2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."StatusDataLocations2" (
    id bigint NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Latitude" double precision,
    "Longitude" double precision,
    "Speed" real,
    "Bearing" real,
    "Direction" character varying(3),
    "LongLatProcessed" boolean NOT NULL,
    "LongLatReason" smallint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
)
PARTITION BY RANGE ("DateTime");


ALTER TABLE public."StatusDataLocations2" OWNER TO geotabadapter_client;

--
-- Name: Users2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."Users2" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone NOT NULL,
    "ActiveTo" timestamp without time zone NOT NULL,
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


ALTER TABLE public."Users2" OWNER TO geotabadapter_client;

--
-- Name: ZoneTypes2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."ZoneTypes2" (
    id bigint NOT NULL,
    "GeotabId" character varying(100) NOT NULL,
    "Comment" character varying(255),
    "Name" character varying(255) NOT NULL,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."ZoneTypes2" OWNER TO geotabadapter_client;

--
-- Name: ZoneTypes2_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_client
--

CREATE SEQUENCE public."ZoneTypes2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."ZoneTypes2_id_seq" OWNER TO geotabadapter_client;

--
-- Name: ZoneTypes2_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_client
--

ALTER SEQUENCE public."ZoneTypes2_id_seq" OWNED BY public."ZoneTypes2".id;


--
-- Name: Zones2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."Zones2" (
    id bigint NOT NULL,
    "GeotabId" character varying(100) NOT NULL,
    "ActiveFrom" timestamp without time zone,
    "ActiveTo" timestamp without time zone,
    "CentroidLatitude" double precision,
    "CentroidLongitude" double precision,
    "Comment" character varying(500),
    "Displayed" boolean,
    "ExternalReference" character varying(255),
    "MustIdentifyStops" boolean,
    "Name" character varying(255) NOT NULL,
    "Points" text,
    "ZoneTypeIds" text,
    "Version" bigint,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."Zones2" OWNER TO geotabadapter_client;

--
-- Name: vwStatsForLevel1DBMaintenance; Type: VIEW; Schema: public; Owner: geotabadapter_client
--

CREATE VIEW public."vwStatsForLevel1DBMaintenance" AS
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

--
-- Name: vwStatsForLevel2DBMaintenance; Type: VIEW; Schema: public; Owner: geotabadapter_client
--

CREATE VIEW public."vwStatsForLevel2DBMaintenance" AS
 WITH index_info AS (
         SELECT i.schemaname,
            i.relname AS tablename,
            i.indexrelname AS indexname,
            c.oid AS index_oid
           FROM (pg_stat_user_indexes i
             JOIN pg_class c ON ((i.indexrelid = c.oid)))
          WHERE (i.schemaname = 'public'::name)
        ), pgstattuple_stats AS (
         SELECT idx.index_oid,
            (public.pgstattuple((idx.index_oid)::regclass)).free_space AS free_space,
            (public.pgstattuple((idx.index_oid)::regclass)).table_len AS table_len
           FROM index_info idx
        ), index_sizes AS (
         SELECT index_info.index_oid,
            pg_relation_size((index_info.index_oid)::regclass) AS total_size
           FROM index_info
        ), results AS (
         SELECT i.indexname,
            i.schemaname,
            i.tablename,
            pg_size_pretty(s.total_size) AS index_size,
            s.total_size AS index_size_bytes,
            ((ps.free_space)::double precision / (NULLIF(ps.table_len, 0))::double precision) AS bloat_ratio
           FROM ((index_info i
             JOIN pgstattuple_stats ps ON ((i.index_oid = ps.index_oid)))
             JOIN index_sizes s ON ((i.index_oid = s.index_oid)))
        ), orderedrows AS (
         SELECT results.schemaname AS "SchemaName",
            results.tablename AS "TableName",
            results.indexname AS "IndexName",
            results.index_size AS "IndexSizeText",
            results.index_size_bytes AS "IndexSize",
            COALESCE(results.bloat_ratio, (0.0)::double precision) AS "IndexBloatRatio"
           FROM results
          ORDER BY results.index_size_bytes DESC, results.bloat_ratio DESC
        )
 SELECT row_number() OVER () AS "RowId",
    "SchemaName",
    "TableName",
    "IndexName",
    "IndexSizeText",
    "IndexSize",
    "IndexBloatRatio"
   FROM orderedrows;


ALTER VIEW public."vwStatsForLevel2DBMaintenance" OWNER TO geotabadapter_client;

--
-- Name: vwStatsForLocationInterpolationProgress; Type: VIEW; Schema: public; Owner: geotabadapter_client
--

CREATE VIEW public."vwStatsForLocationInterpolationProgress" AS
SELECT 
    ROW_NUMBER() OVER (ORDER BY "Table") AS "RowId",
    "Table",
    "Total",
    "LongLatProcessedTotal",
    CASE 
        WHEN "Total" > 0 THEN ("LongLatProcessedTotal" * 100.0) / "Total" 
        ELSE 0 
    END AS "LongLatProcessedPercentage"
FROM (
    SELECT 
        'StatusDataLocations2' AS "Table",
        COUNT(*) AS "Total",
        SUM(CASE WHEN "LongLatProcessed" IS TRUE THEN 1 ELSE 0 END) AS "LongLatProcessedTotal"
    FROM public."StatusDataLocations2"
    UNION ALL
    SELECT 
        'FaultDataLocations2' AS "Table",
        COUNT(*) AS "Total",
        SUM(CASE WHEN "LongLatProcessed" IS TRUE THEN 1 ELSE 0 END) AS "LongLatProcessedTotal"
    FROM public."FaultDataLocations2"
) AS "InterpolationProgress";


ALTER VIEW public."vwStatsForLocationInterpolationProgress" OWNER TO geotabadapter_client;

--
-- Name: DBMaintenanceLogs2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."DBMaintenanceLogs2" ALTER COLUMN id SET DEFAULT nextval('public."DBMaintenanceLogs2_id_seq"'::regclass);


--
-- Name: DBPartitionInfo2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."DBPartitionInfo2" ALTER COLUMN id SET DEFAULT nextval('public."DBPartitionInfo2_id_seq"'::regclass);


--
-- Name: DiagnosticIds2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."DiagnosticIds2" ALTER COLUMN id SET DEFAULT nextval('public."DiagnosticIds2_id_seq"'::regclass);


--
-- Name: Diagnostics2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."Diagnostics2" ALTER COLUMN id SET DEFAULT nextval('public."Diagnostics2_id_seq"'::regclass);


--
-- Name: EntityMetadata2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."EntityMetadata2" ALTER COLUMN id SET DEFAULT nextval('public."EntityMetadata2_id_seq"'::regclass);


--
-- Name: MiddlewareVersionInfo2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."MiddlewareVersionInfo2" ALTER COLUMN id SET DEFAULT nextval('public."MiddlewareVersionInfo2_id_seq"'::regclass);


--
-- Name: OServiceTracking2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."OServiceTracking2" ALTER COLUMN id SET DEFAULT nextval('public."OServiceTracking2_id_seq"'::regclass);


--
-- Name: ZoneTypes2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."ZoneTypes2" ALTER COLUMN id SET DEFAULT nextval('public."ZoneTypes2_id_seq"'::regclass);


--
-- Name: DBMaintenanceLogs2 DBMaintenanceLogs2_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."DBMaintenanceLogs2"
    ADD CONSTRAINT "DBMaintenanceLogs2_pkey" PRIMARY KEY (id);


--
-- Name: DBPartitionInfo2 DBPartitionInfo2_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."DBPartitionInfo2"
    ADD CONSTRAINT "DBPartitionInfo2_pkey" PRIMARY KEY (id);


--
-- Name: OServiceTracking2 OServiceTracking2_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."OServiceTracking2"
    ADD CONSTRAINT "OServiceTracking2_pkey" PRIMARY KEY (id);


--
-- Name: Devices2 PK_Devices2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."Devices2"
    ADD CONSTRAINT "PK_Devices2" PRIMARY KEY (id);


--
-- Name: DiagnosticIds2 PK_DiagnosticIds2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."DiagnosticIds2"
    ADD CONSTRAINT "PK_DiagnosticIds2" PRIMARY KEY (id);


--
-- Name: Diagnostics2 PK_Diagnostics2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."Diagnostics2"
    ADD CONSTRAINT "PK_Diagnostics2" PRIMARY KEY (id);


--
-- Name: EntityMetadata2 PK_EntityMetadata2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."EntityMetadata2"
    ADD CONSTRAINT "PK_EntityMetadata2" PRIMARY KEY ("DateTime", id);


--
-- Name: FaultData2 PK_FaultData2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."FaultData2"
    ADD CONSTRAINT "PK_FaultData2" PRIMARY KEY ("DateTime", id);


--
-- Name: FaultDataLocations2 PK_FaultDataLocations2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."FaultDataLocations2"
    ADD CONSTRAINT "PK_FaultDataLocations2" PRIMARY KEY ("DateTime", id);


--
-- Name: LogRecords2 PK_LogRecords2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."LogRecords2"
    ADD CONSTRAINT "PK_LogRecords2" PRIMARY KEY ("DateTime", id);


--
-- Name: MiddlewareVersionInfo2 PK_MiddlewareVersionInfo2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."MiddlewareVersionInfo2"
    ADD CONSTRAINT "PK_MiddlewareVersionInfo2" PRIMARY KEY (id);


--
-- Name: StatusData2 PK_StatusData2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."StatusData2"
    ADD CONSTRAINT "PK_StatusData2" PRIMARY KEY ("DateTime", id);


--
-- Name: StatusDataLocations2 PK_StatusDataLocations2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."StatusDataLocations2"
    ADD CONSTRAINT "PK_StatusDataLocations2" PRIMARY KEY ("DateTime", id);


--
-- Name: Users2 PK_Users2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."Users2"
    ADD CONSTRAINT "PK_Users2" PRIMARY KEY (id);


--
-- Name: ZoneTypes2 PK_ZoneTypes2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."ZoneTypes2"
    ADD CONSTRAINT "PK_ZoneTypes2" PRIMARY KEY (id);


--
-- Name: Zones2 PK_Zones2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."Zones2"
    ADD CONSTRAINT "PK_Zones2" PRIMARY KEY (id);


--
-- Name: DiagnosticIds2 UK_DiagnosticIds2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."DiagnosticIds2"
    ADD CONSTRAINT "UK_DiagnosticIds2" UNIQUE ("GeotabGUIDString", "GeotabId");


--
-- Name: IX_DBMaintenanceLogs2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_DBMaintenanceLogs2_RecordLastChangedUtc" ON public."DBMaintenanceLogs2" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_DBPartitionInfo2_RecordCreationTimeUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_DBPartitionInfo2_RecordCreationTimeUtc" ON public."DBPartitionInfo2" USING btree ("RecordCreationTimeUtc");


--
-- Name: IX_Devices2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_Devices2_RecordLastChangedUtc" ON public."Devices2" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_Diagnostics2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_Diagnostics2_RecordLastChangedUtc" ON public."Diagnostics2" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_EntityMetadata2_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_EntityMetadata2_DateTime" ON ONLY public."EntityMetadata2" USING btree ("DateTime");


--
-- Name: IX_EntityMetadata2_DeviceId_DateTime_EntityType; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_EntityMetadata2_DeviceId_DateTime_EntityType" ON ONLY public."EntityMetadata2" USING btree ("DeviceId", "DateTime", "EntityType") WITH (deduplicate_items='true');


--
-- Name: IX_FaultData2_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_FaultData2_DateTime" ON ONLY public."FaultData2" USING btree ("DateTime");


--
-- Name: IX_FaultData2_DeviceId_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_FaultData2_DeviceId_DateTime" ON ONLY public."FaultData2" USING btree ("DeviceId", "DateTime");


--
-- Name: IX_FaultData2_Id; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_FaultData2_Id" ON ONLY public."FaultData2" USING btree (id) WITH (deduplicate_items='true');


--
-- Name: IX_FaultDataLocations2_DateTime_DeviceId_Filtered; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_FaultDataLocations2_DateTime_DeviceId_Filtered" ON ONLY public."FaultDataLocations2" USING btree ("DateTime", "DeviceId") WHERE ("LongLatProcessed" = false);


--
-- Name: IX_FaultDataLocations2_Id; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_FaultDataLocations2_Id" ON ONLY public."FaultDataLocations2" USING btree (id) WITH (deduplicate_items='true');


--
-- Name: IX_FaultDataLocations2_LongLatProcessed_DateTime_id; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_FaultDataLocations2_LongLatProcessed_DateTime_id" ON ONLY public."FaultDataLocations2" USING btree ("LongLatProcessed", "DateTime", id) WITH (deduplicate_items='true');


--
-- Name: IX_FaultDataLocations2_id_LongLatProcessed; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_FaultDataLocations2_id_LongLatProcessed" ON ONLY public."FaultDataLocations2" USING btree (id, "LongLatProcessed") WITH (deduplicate_items='true');


--
-- Name: IX_LogRecords2_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_LogRecords2_DateTime" ON ONLY public."LogRecords2" USING btree ("DateTime");


--
-- Name: IX_LogRecords2_DeviceId_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_LogRecords2_DeviceId_DateTime" ON ONLY public."LogRecords2" USING btree ("DeviceId", "DateTime") WITH (deduplicate_items='true');


--
-- Name: IX_LogRecords2_Id; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_LogRecords2_Id" ON ONLY public."LogRecords2" USING btree (id) WITH (deduplicate_items='true');


--
-- Name: IX_MyGeotabVersionInfo2_RecordCreationTimeUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_MyGeotabVersionInfo2_RecordCreationTimeUtc" ON public."MyGeotabVersionInfo2" USING btree ("RecordCreationTimeUtc");


--
-- Name: IX_OServiceTracking2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_OServiceTracking2_RecordLastChangedUtc" ON public."OServiceTracking2" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_StatusData2_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_StatusData2_DateTime" ON ONLY public."StatusData2" USING btree ("DateTime");


--
-- Name: IX_StatusData2_DeviceId_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_StatusData2_DeviceId_DateTime" ON ONLY public."StatusData2" USING btree ("DeviceId", "DateTime");


--
-- Name: IX_StatusData2_Id; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_StatusData2_Id" ON ONLY public."StatusData2" USING btree (id) WITH (deduplicate_items='true');


--
-- Name: IX_StatusDataLocations2_DateTime_DeviceId_Filtered; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_StatusDataLocations2_DateTime_DeviceId_Filtered" ON ONLY public."StatusDataLocations2" USING btree ("DateTime", "DeviceId") WHERE ("LongLatProcessed" = false);


--
-- Name: IX_StatusDataLocations2_Id; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_StatusDataLocations2_Id" ON ONLY public."StatusDataLocations2" USING btree (id) WITH (deduplicate_items='true');


--
-- Name: IX_StatusDataLocations2_LongLatProcessed_DateTime_id; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_StatusDataLocations2_LongLatProcessed_DateTime_id" ON ONLY public."StatusDataLocations2" USING btree ("LongLatProcessed", "DateTime", id) WITH (deduplicate_items='true');


--
-- Name: IX_StatusDataLocations2_id_LongLatProcessed; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_StatusDataLocations2_id_LongLatProcessed" ON ONLY public."StatusDataLocations2" USING btree (id, "LongLatProcessed") WITH (deduplicate_items='true');


--
-- Name: IX_Users2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_Users2_RecordLastChangedUtc" ON public."Users2" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_ZoneTypes2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_ZoneTypes2_RecordLastChangedUtc" ON public."ZoneTypes2" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_Zones2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_Zones2_RecordLastChangedUtc" ON public."Zones2" USING btree ("RecordLastChangedUtc");


--
-- Name: UI_Diagnostics2_GeotabGUIDString; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE UNIQUE INDEX "UI_Diagnostics2_GeotabGUIDString" ON public."Diagnostics2" USING btree ("GeotabGUIDString");


--
-- Name: fki_FK_FaultDataLocations2_FaultData2; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "fki_FK_FaultDataLocations2_FaultData2" ON ONLY public."FaultDataLocations2" USING btree (id);


--
-- Name: fki_FK_StatusDataLocations2_StatusData2; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "fki_FK_StatusDataLocations2_StatusData2" ON ONLY public."StatusDataLocations2" USING btree (id);


--
-- Name: DiagnosticIds2 FK_DiagnosticIds2_Diagnostics2; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."DiagnosticIds2"
    ADD CONSTRAINT "FK_DiagnosticIds2_Diagnostics2" FOREIGN KEY ("GeotabGUIDString") REFERENCES public."Diagnostics2"("GeotabGUIDString");


--
-- Name: DiagnosticIds2 FK_DiagnosticIds2_Diagnostics21; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."DiagnosticIds2"
    ADD CONSTRAINT "FK_DiagnosticIds2_Diagnostics21" FOREIGN KEY ("FormerShimGeotabGUIDString") REFERENCES public."Diagnostics2"("GeotabGUIDString");


--
-- Name: FaultData2 FK_FaultData2_Devices2; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE public."FaultData2"
    ADD CONSTRAINT "FK_FaultData2_Devices2" FOREIGN KEY ("DeviceId") REFERENCES public."Devices2"(id);


--
-- Name: FaultData2 FK_FaultData2_DiagnosticIds2; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE public."FaultData2"
    ADD CONSTRAINT "FK_FaultData2_DiagnosticIds2" FOREIGN KEY ("DiagnosticId") REFERENCES public."DiagnosticIds2"(id);


--
-- Name: FaultData2 FK_FaultData2_Users2; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE public."FaultData2"
    ADD CONSTRAINT "FK_FaultData2_Users2" FOREIGN KEY ("DismissUserId") REFERENCES public."Users2"(id);


--
-- Name: LogRecords2 FK_LogRecords2_Devices2; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE public."LogRecords2"
    ADD CONSTRAINT "FK_LogRecords2_Devices2" FOREIGN KEY ("DeviceId") REFERENCES public."Devices2"(id) ON UPDATE CASCADE ON DELETE CASCADE;


--
-- Name: StatusData2 FK_StatusData2_Devices2; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE public."StatusData2"
    ADD CONSTRAINT "FK_StatusData2_Devices2" FOREIGN KEY ("DeviceId") REFERENCES public."Devices2"(id);


--
-- Name: StatusData2 FK_StatusData2_DiagnosticIds2; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE public."StatusData2"
    ADD CONSTRAINT "FK_StatusData2_DiagnosticIds2" FOREIGN KEY ("DiagnosticId") REFERENCES public."DiagnosticIds2"(id);


--
-- PostgreSQL database dump complete
--
/*** [END] Part 2 of 4: pgAdmin-Generated Script (tables, sequences, views) ***/ 



/*** [START] Part 3 of 4: pgAdmin-Generated Script (functions) ***/
-- FUNCTION: public.spFaultData2WithLagLeadLongLatBatch(integer, integer, integer)

-- DROP FUNCTION IF EXISTS public."spFaultData2WithLagLeadLongLatBatch"(integer, integer, integer);

CREATE OR REPLACE FUNCTION public."spFaultData2WithLagLeadLongLatBatch"(
	"MaxDaysPerBatch" integer,
	"MaxBatchSize" integer,
	"BufferMinutes" integer)
    RETURNS TABLE(id bigint, "GeotabId" character varying, "FaultDataDateTime" timestamp without time zone, "DeviceId" bigint, "LagDateTime" timestamp without time zone, "LagLatitude" double precision, "LagLongitude" double precision, "LagSpeed" real, "LeadDateTime" timestamp without time zone, "LeadLatitude" double precision, "LeadLongitude" double precision, "LogRecords2MinDateTime" timestamp without time zone, "LogRecords2MaxDateTime" timestamp without time zone, "DeviceLogRecords2MinDateTime" timestamp without time zone, "DeviceLogRecords2MaxDateTime" timestamp without time zone) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
-- ==========================================================================================
-- Description: Returns a batch of FaultData2 records with additional
--              metadata about the LogRecords2 table. Each returned record
--              also contains the DateTime, Latitude and Longitude values of the LogRecord2
--              records with DateTimes immediately before (or equal to) and after the 
--              DateTime of the FaultData2 record. This result set is intended to be used
--              for interpolation of location coordinates, speed, bearing and compass
--              direction for the subject FaultData2 records.
--
-- Parameters:
--		MaxDaysPerBatch: The maximum number of days over which unprocessed FaultData records 
--			in a batch can span.
--		MaxBatchSize: The maximum number of unprocessed FaultData records to retrieve for 
--			interpolation per batch.
--		BufferMinutes: When getting the DateTime range of a batch of unprocessed FaultData 
--			records, this buffer is applied to either end of the DateTime range when 
--			selecting LogRecords to use for interpolation such that lag LogRecords can be 
--			obtained for records that are “early” in the batch and lead LogRecords can be 
--			obtained for records that are “late” in the batch.
-- ==========================================================================================
DECLARE
	-- Constants:
	min_allowed_max_days_per_batch INTEGER := 1;
	max_allowed_max_days_per_batch INTEGER := 10;
	min_allowed_max_batch_size INTEGER := 10000;
	max_allowed_max_batch_size INTEGER := 500000;
	min_allowed_buffer_minutes INTEGER := 10;
	max_allowed_buffer_minutes INTEGER := 1440;
	
    -- The maximum number of days that can be spanned in a batch.
    max_days_per_batch INTEGER := "MaxDaysPerBatch";
    -- The maximum number of records to return.
    max_batch_size INTEGER := "MaxBatchSize";
    -- Buffer period, in minutes, for fetching encompassing values.
    buffer_minutes INTEGER := "BufferMinutes";

    -- Variables
	default_datetime TIMESTAMP WITHOUT TIME ZONE := '1900-01-01'::TIMESTAMP WITHOUT TIME ZONE;
    faultdata2_primary_partition_max_datetime TIMESTAMP WITHOUT TIME ZONE;
    logrecords2_min_datetime TIMESTAMP WITHOUT TIME ZONE;
    logrecords2_max_datetime TIMESTAMP WITHOUT TIME ZONE;
    function_name TEXT := 'spFaultData2WithLagLeadLongLatBatch';
    unprocessed_faultdata_min_datetime TIMESTAMP WITHOUT TIME ZONE;
    unprocessed_faultdata_max_allowed_datetime TIMESTAMP WITHOUT TIME ZONE;
    unprocessed_faultdata2_batch_min_datetime TIMESTAMP WITHOUT TIME ZONE;
    unprocessed_faultdata2_batch_max_datetime TIMESTAMP WITHOUT TIME ZONE;		
    buffered_unprocessed_faultdata2_batch_min_datetime TIMESTAMP WITHOUT TIME ZONE;
    buffered_unprocessed_faultdata2_batch_max_datetime TIMESTAMP WITHOUT TIME ZONE;			
    function_start_time TIMESTAMP;
    start_time TIMESTAMP;
    end_time TIMESTAMP;
    start_time_string TEXT;
    duration_string TEXT;
    record_count INTEGER;
BEGIN
	-- ======================================================================================
    -- Log start of stored procedure execution.
    function_start_time := CLOCK_TIMESTAMP();
    start_time := CLOCK_TIMESTAMP();
    start_time_string := TO_CHAR(start_time, 'YYYY-MM-DD HH24:MI:SS');
    RAISE NOTICE 'Executing function ''%'' Start: %', function_name, start_time_string;
    RAISE NOTICE '> max_days_per_batch: %', max_days_per_batch;
    RAISE NOTICE '> max_batch_size: %', max_batch_size;
    RAISE NOTICE '> buffer_minutes: %', buffer_minutes;
	
	-- ======================================================================================
    -- STEP 1: Validate input parameter values.
	RAISE NOTICE 'Step 1 [Validating input parameter values]...';
	
	-- MaxDaysPerBatch
	IF max_days_per_batch < min_allowed_max_days_per_batch OR max_days_per_batch > max_allowed_max_days_per_batch THEN
		RAISE EXCEPTION 'ERROR: MaxDaysPerBatch (%) is out of the allowed range [%, %].', 
			max_days_per_batch, min_allowed_max_days_per_batch, max_allowed_max_days_per_batch;
	END IF;

	-- MaxBatchSize
	IF max_batch_size < min_allowed_max_batch_size OR max_batch_size > max_allowed_max_batch_size THEN
		RAISE EXCEPTION 'ERROR: MaxBatchSize (%) is out of the allowed range [%, %].', 
			max_batch_size, min_allowed_max_batch_size, max_allowed_max_batch_size;
	END IF;

	-- BufferMinutes
	IF buffer_minutes < min_allowed_buffer_minutes OR buffer_minutes > max_allowed_buffer_minutes THEN
		RAISE EXCEPTION 'ERROR: BufferMinutes (%) is out of the allowed range [%, %].', 
			buffer_minutes, min_allowed_buffer_minutes, max_allowed_buffer_minutes;
	END IF;	
	
	-- Log
    end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds'; 
    RAISE NOTICE 'STEP 1 completed. Duration: %', duration_string;
    start_time := end_time;

	-- ======================================================================================
    -- STEP 2: Get the max DateTime value from the EARLIEST partition in the FaultData2 table.
	SELECT MAX(fd."DateTime") 
	INTO faultdata2_primary_partition_max_datetime
	FROM public."FaultDataLocations2_default" fd;
	
	-- If no data is found, set a default value
	IF faultdata2_primary_partition_max_datetime IS NULL THEN
	    faultdata2_primary_partition_max_datetime := default_datetime;
	END IF;
	
	-- Log the duration and the retrieved value (using RAISE NOTICE for logging)
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000;
	RAISE NOTICE 'STEP 2 Duration: % milliseconds', duration_string;
	RAISE NOTICE '> faultdata2_primary_partition_max_datetime: %', faultdata2_primary_partition_max_datetime;
	start_time := end_time;

	-- ======================================================================================
	-- STEP 3: 
	-- Get min and max DateTime values by Device for unprocessed FaultDataLocations2 
	-- records. Also get associated min and max DateTime values for LogRecords2 records where
	-- the DateTimes of the LogRecords are greater than or equal to the min DateTimes of the 
	-- unprocessed FaultDataLocations2 records. Do this ONLY for Devices that have both 
	-- FaultData and LogRecords. Exclude data from the EARLIEST partition.
	DROP TABLE IF EXISTS "TMP_DeviceDataMinMaxDateTimes";
	
	-- Create and populate the temporary table
	CREATE TEMP TABLE "TMP_DeviceDataMinMaxDateTimes" AS
	WITH FaultDataMinMax AS (
	    SELECT fdl."DeviceId",
	           MIN(fdl."DateTime") AS "DeviceFaultData2MinDateTime",
	           MAX(fdl."DateTime") AS "DeviceFaultData2MaxDateTime"
	    FROM public."FaultDataLocations2" fdl
	    WHERE fdl."DateTime" > faultdata2_primary_partition_max_datetime
	      AND fdl."LongLatProcessed" = false
	    GROUP BY fdl."DeviceId"
	),
	FilteredLogRecords AS (
	    SELECT lr."DeviceId",
	           MIN(lr."DateTime") AS "DeviceLogRecords2MinDateTime",
	           MAX(lr."DateTime") AS "DeviceLogRecords2MaxDateTime"
	    FROM public."LogRecords2" lr
		WHERE lr."DateTime" >= (SELECT MIN("DeviceFaultData2MinDateTime") FROM FaultDataMinMax)
	    GROUP BY lr."DeviceId"
	)
	SELECT fd."DeviceId",
	       fd."DeviceFaultData2MinDateTime",
	       fd."DeviceFaultData2MaxDateTime",
	       flr."DeviceLogRecords2MinDateTime",
	       flr."DeviceLogRecords2MaxDateTime"
	FROM FaultDataMinMax fd
	INNER JOIN FilteredLogRecords flr
	    ON fd."DeviceId" = flr."DeviceId";	

    -- -- Log
	GET DIAGNOSTICS record_count = ROW_COUNT;
    end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds'; 
	RAISE NOTICE 'STEP 3 (Create tmp_device_data_min_max_date_times) Duration: %', duration_string;
    RAISE NOTICE '> Record Count: %', record_count;
    start_time := end_time;

	-- ======================================================================================
	-- STEP 3A: 
	-- Add indexes to temporary table.
	CREATE INDEX IF NOT EXISTS "IX_TMP_DeviceDataMinMaxDateTimes_DeviceId" 
	ON "TMP_DeviceDataMinMaxDateTimes" ("DeviceId");
	
	CREATE INDEX IF NOT EXISTS "IX_TMP_DeviceDataMinMaxDateTimes_DeviceLogRecords2MinDateTime" 
	ON "TMP_DeviceDataMinMaxDateTimes" ("DeviceLogRecords2MinDateTime");
	
	CREATE INDEX IF NOT EXISTS "IX_TMP_DeviceDataMinMaxDateTimes_DeviceLogRecords2MaxDateTime" 
	ON "TMP_DeviceDataMinMaxDateTimes" ("DeviceLogRecords2MaxDateTime");

    -- Log
    end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds'; 
    RAISE NOTICE 'STEP 3A (Index TMP_DeviceDataMinMaxDateTimes) Duration: %', duration_string;
    start_time := end_time;

	-- ======================================================================================
	-- STEP 4:
	-- Get the DateTime of the unprocessed FaultDataLocations2 record with the lowest 
	-- DateTime value that is greater than the max DateTime value from the EARLIEST partition
	-- in the FaultDataLocations2 table.
	
	SELECT MIN(ddmm."DeviceFaultData2MinDateTime") INTO unprocessed_faultdata_min_datetime 
	FROM "TMP_DeviceDataMinMaxDateTimes" ddmm;
	
	-- Determine the maximum allowed DateTime for the current batch based on adding max_days_per_batch
	-- to the unprocessed_faultdata_min_datetime. The purpose of this is to limit the number of 
	-- partitions that must be scanned as well as the potential number of LogRecords that
	-- may be returned in subsequent queries - which may be insignificant with smaller fleets,
	-- but can have a huge impact with larger fleets and their associated data volumes.
	unprocessed_faultdata_max_allowed_datetime := 
	    (unprocessed_faultdata_min_datetime + (max_days_per_batch || ' days')::interval) - interval '1 second'; 
	
	-- Get the minimum DateTime value of any LogRecord.
	SELECT MIN(ddmm."DeviceLogRecords2MinDateTime") INTO logrecords2_min_datetime
	FROM "TMP_DeviceDataMinMaxDateTimes" ddmm;
	
	-- Get the maximum DateTime value of any LogRecord.
	SELECT MAX(ddmm."DeviceLogRecords2MaxDateTime") INTO logrecords2_max_datetime 
	FROM "TMP_DeviceDataMinMaxDateTimes" ddmm;
	
	-- Log.
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 4 Duration: %', duration_string;
	RAISE NOTICE '> unprocessed_faultdata_min_datetime: %', unprocessed_faultdata_min_datetime;
	RAISE NOTICE '> unprocessed_faultdata_max_allowed_datetime: %', unprocessed_faultdata_max_allowed_datetime;
	RAISE NOTICE '> logrecords2_min_datetime: %', logrecords2_min_datetime;
	RAISE NOTICE '> logrecords2_max_datetime: %', logrecords2_max_datetime;
	start_time := end_time;

	-- ======================================================================================
	-- STEP 5:
	-- Get up to the first max_batch_size of FaultDataLocations2 records within the 
	-- unprocessed_faultdata_min_datetime to unprocessed_faultdata_max_allowed_datetime DateTime 
	-- range. Also make sure not to exceed the logrecords2_max_datetime.
	DROP TABLE IF EXISTS "TMP_BatchFaultDataLocationIds";
	
	CREATE TEMP TABLE "TMP_BatchFaultDataLocationIds" AS
	SELECT fdl.id
	FROM public."FaultDataLocations2" fdl
	INNER JOIN "TMP_DeviceDataMinMaxDateTimes" ddmmdt ON fdl."DeviceId" = ddmmdt."DeviceId"
	WHERE fdl."LongLatProcessed" = false
	  AND fdl."DateTime" BETWEEN unprocessed_faultdata_min_datetime AND unprocessed_faultdata_max_allowed_datetime
	  AND fdl."DateTime" <= logrecords2_max_datetime
	LIMIT max_batch_size;
	
	-- Log.
	GET DIAGNOSTICS record_count = ROW_COUNT;
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 5 (Create TMP_BatchFaultDataLocationIds) Duration: %', duration_string;
	RAISE NOTICE '> Record Count: %', record_count;
	start_time := end_time;

	-- ======================================================================================
	-- STEP 5A:
	-- Add index to temporary table.
	CREATE INDEX "IX_TMP_BatchFaultDataLocationIds_id" 
	ON "TMP_BatchFaultDataLocationIds" (id);
	
	-- Log.
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 5A (Index TMP_BatchFaultDataLocationIds) Duration: %', duration_string;
	start_time := end_time;

	-- ======================================================================================
	-- STEP 6:
	-- Get the batch of unprocessed FaultData2 records.
	DROP TABLE IF EXISTS "TMP_UnprocessedFaultData2Batch";

	CREATE TEMP TABLE "TMP_UnprocessedFaultData2Batch" AS
	SELECT bfdl.id,
		fd."GeotabId",
		fdl."DateTime",
		fdl."DeviceId",
		logrecords2_min_datetime AS "LogRecords2MinDateTime",
		logrecords2_max_datetime AS "LogRecords2MaxDateTime",
		dlrmm."DeviceLogRecords2MinDateTime",
		dlrmm."DeviceLogRecords2MaxDateTime"
	FROM  "TMP_BatchFaultDataLocationIds" bfdl
	LEFT JOIN public."FaultDataLocations2" fdl 
		ON bfdl.id = fdl.id
	LEFT JOIN "TMP_DeviceDataMinMaxDateTimes" dlrmm 
		ON fdl."DeviceId" = dlrmm."DeviceId"	
	LEFT JOIN public."FaultData2" fd
		ON bfdl.id = fd.id
	WHERE fd."DateTime" BETWEEN unprocessed_faultdata_min_datetime AND unprocessed_faultdata_max_allowed_datetime;

	-- Log.
	GET DIAGNOSTICS record_count = ROW_COUNT;
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 6 (Create TMP_UnprocessedFaultData2Batch) Duration: %', duration_string;
	RAISE NOTICE '> Record Count: %', record_count;
	start_time := end_time;

	-- ======================================================================================
	-- STEP 6A: 
	-- Add indexes to temporary table.
	CREATE INDEX "IX_TMP_UnprocessedFaultData2Batch_id" 
	ON "TMP_UnprocessedFaultData2Batch" ("id");
	
	CREATE INDEX "IX_TMP_UnprocessedFaultData2Batch_DateTime" 
	ON "TMP_UnprocessedFaultData2Batch" ("DateTime");
	
	CREATE INDEX "IX_TMP_UnprocessedFaultData2Batch_DeviceId_DateTime" 
	ON "TMP_UnprocessedFaultData2Batch" ("DeviceId", "DateTime");
	
	-- Log.
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 6A (Index TMP_UnprocessedFaultData2Batch) Duration: %', duration_string;
	start_time := end_time;	

	-- ======================================================================================
	-- STEP 7:
	-- Get the min and max DateTime values from TMP_UnprocessedFaultData2Batch.
	SELECT MIN("DateTime") INTO unprocessed_faultdata2_batch_min_datetime
	FROM "TMP_UnprocessedFaultData2Batch";	
	
	SELECT MAX("DateTime") INTO unprocessed_faultdata2_batch_max_datetime
	FROM "TMP_UnprocessedFaultData2Batch";
	
	buffered_unprocessed_faultdata2_batch_min_datetime := unprocessed_faultdata2_batch_min_datetime - (buffer_minutes || ' minutes')::interval;
	buffered_unprocessed_faultdata2_batch_max_datetime := unprocessed_faultdata2_batch_max_datetime + (buffer_minutes || ' minutes')::interval;	

	-- Log.
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 7 Duration: %', duration_string;
	RAISE NOTICE '> unprocessed_faultdata2_batch_min_datetime: %', unprocessed_faultdata2_batch_min_datetime;
	RAISE NOTICE '> unprocessed_faultdata2_batch_max_datetime: %', unprocessed_faultdata2_batch_max_datetime;
	RAISE NOTICE '> buffered_unprocessed_faultdata2_batch_min_datetime: %', buffered_unprocessed_faultdata2_batch_min_datetime;
	RAISE NOTICE '> buffered_unprocessed_faultdata2_batch_max_datetime: %', buffered_unprocessed_faultdata2_batch_max_datetime;	
	start_time := end_time;

	-- ======================================================================================
	-- STEP 8:
	-- Get the Ids of all records from the LogRecords2 table that serve as lag, equal or lead
	-- to records in the TMP_UnprocessedFaultData2Batch based on DateTime.	
	-- Use the EntityMetadata2 table for speed in this step. Note: EntityType 1 = LogRecord.
	DROP TABLE IF EXISTS "TMP_LogRecordIdsForUnprocessedFaultData2Batch";
	
	CREATE TEMP TABLE "TMP_LogRecordIdsForUnprocessedFaultData2Batch" AS
	SELECT 
	    ufdb.id AS "UnprocessedFaultDataEntityId",
	    emlaglr."EntityId" AS "LagLogRecordEntityId",
	    emleadlr."EntityId" AS "LeadLogRecordEntityId"
	FROM 
	    "TMP_UnprocessedFaultData2Batch" ufdb
	LEFT JOIN LATERAL (
	    SELECT em."EntityId"
	    FROM public."EntityMetadata2" em
	    WHERE em."EntityType" = 1 
	      AND em."DeviceId" = ufdb."DeviceId" 
		  AND em."DateTime" >= buffered_unprocessed_faultdata2_batch_min_datetime
	      AND em."DateTime" <= ufdb."DateTime"
	    ORDER BY em."DateTime" DESC
	    LIMIT 1
	) emlaglr ON true
	LEFT JOIN LATERAL (
	    SELECT em."EntityId"
	    FROM public."EntityMetadata2" em
	    WHERE em."EntityType" = 1 
	      AND em."DeviceId" = ufdb."DeviceId"
		  AND em."DateTime" <= buffered_unprocessed_faultdata2_batch_max_datetime
	      AND em."DateTime" > ufdb."DateTime"
	    ORDER BY em."DateTime" ASC
	    LIMIT 1
	) emleadlr ON true;

	-- Log.
	GET DIAGNOSTICS record_count = ROW_COUNT;
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 8 (Create TMP_LogRecordIdsForUnprocessedFaultData2Batch) Duration: %', duration_string;
	RAISE NOTICE '> Record Count: %', record_count;
	start_time := end_time;

	-- ======================================================================================
	-- STEP 8A:
	-- Add indexes to temporary table.
	CREATE INDEX "IX_TMP_LRIdsForUnprocessedFD2Batch_UnprocessedFDEntityId"
	ON "TMP_LogRecordIdsForUnprocessedFaultData2Batch" ("UnprocessedFaultDataEntityId");
	
	CREATE INDEX "IX_TMP_LRIdsForUnprocessedFD2Batch_LagLREntityId"
	ON "TMP_LogRecordIdsForUnprocessedFaultData2Batch" ("LagLogRecordEntityId");

	CREATE INDEX "IX_TMP_LRIdsForUnprocessedFD2Batch_LeadLREntityId"
	ON "TMP_LogRecordIdsForUnprocessedFaultData2Batch" ("LeadLogRecordEntityId");

	-- Log.
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 8A (Index TMP_LogRecordIdsForUnprocessedFaultData2Batch) Duration: %', duration_string;
	start_time := end_time;	

	-- ======================================================================================
	-- STEP 9:
	-- Produce and return the final output, sorted by FaultDataDateTime. Achieve this by 
	-- joining the TMP_LogRecordIdsForUnprocessedFaultData2Batch records to the FaultData2
	-- and LogRecords2 tables via multiple CTEs and combining the results. The DateTime 
	-- filters on the CTE queries are critical to ensuring partition pruning to achieve solid
	-- performance.
	RETURN QUERY
	WITH "FaultDataCols" AS (
		SELECT ufdllbatch."UnprocessedFaultDataEntityId" AS "id",
			   fd."GeotabId",
			   fd."DateTime" AS "FaultDataDateTime",
			   fd."DeviceId"
		FROM "TMP_LogRecordIdsForUnprocessedFaultData2Batch" ufdllbatch
		LEFT JOIN public."FaultData2" fd
			ON ufdllbatch."UnprocessedFaultDataEntityId" = fd.id
		   AND fd."DateTime" BETWEEN buffered_unprocessed_faultdata2_batch_min_datetime
								AND buffered_unprocessed_faultdata2_batch_max_datetime
	),
	"LagLogRecordCols" AS (
		SELECT ufdllbatch."UnprocessedFaultDataEntityId",
			   laglr."DateTime" AS "LagDateTime",
			   laglr."Latitude" AS "LagLatitude",
			   laglr."Longitude" AS "LagLongitude",
			   laglr."Speed" AS "LagSpeed"
		FROM "TMP_LogRecordIdsForUnprocessedFaultData2Batch" ufdllbatch
		LEFT JOIN public."LogRecords2" laglr
			ON ufdllbatch."LagLogRecordEntityId" = laglr.id
		   AND laglr."DateTime" BETWEEN buffered_unprocessed_faultdata2_batch_min_datetime
								AND buffered_unprocessed_faultdata2_batch_max_datetime
	),
	"LeadLogRecordCols" AS (
		SELECT ufdllbatch."UnprocessedFaultDataEntityId",
			   leadlr."DateTime" AS "LeadDateTime",
			   leadlr."Latitude" AS "LeadLatitude",
			   leadlr."Longitude" AS "LeadLongitude"
		FROM "TMP_LogRecordIdsForUnprocessedFaultData2Batch" ufdllbatch
		LEFT JOIN public."LogRecords2" leadlr
			ON ufdllbatch."LeadLogRecordEntityId" = leadlr.id
		   AND leadlr."DateTime" BETWEEN buffered_unprocessed_faultdata2_batch_min_datetime
								AND buffered_unprocessed_faultdata2_batch_max_datetime
	)
	SELECT fdcols."id", 
		fdcols."GeotabId", 
		fdcols."FaultDataDateTime", 
		fdcols."DeviceId", 
		laglrcols."LagDateTime", 
		laglrcols."LagLatitude", 
		laglrcols."LagLongitude", 
		laglrcols."LagSpeed",
		leadlrcols."LeadDateTime", 
		leadlrcols."LeadLatitude", 
		leadlrcols."LeadLongitude", 
		logrecords2_min_datetime AS "LogRecords2MinDateTime",
		logrecords2_max_datetime AS "LogRecords2MaxDateTime",
		ddmm."DeviceLogRecords2MinDateTime",
		ddmm."DeviceLogRecords2MaxDateTime"
	FROM "FaultDataCols" fdcols
	LEFT JOIN "LagLogRecordCols" laglrcols 
		ON fdcols."id" = laglrcols."UnprocessedFaultDataEntityId"
	LEFT JOIN "LeadLogRecordCols" leadlrcols 
		ON fdcols."id" = leadlrcols."UnprocessedFaultDataEntityId"
	LEFT JOIN "TMP_DeviceDataMinMaxDateTimes" ddmm
		ON fdcols."DeviceId" = ddmm."DeviceId"
	ORDER BY fdcols."FaultDataDateTime";

	-- Log.
	GET DIAGNOSTICS record_count = ROW_COUNT;
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 9 (Return final output) Duration: %', duration_string;
	RAISE NOTICE '> Record Count: %', record_count;
	start_time := end_time;

	-- ======================================================================================
	-- STEP 10:
	-- Drop temporary tables.
	DROP TABLE IF EXISTS "TMP_DeviceDataMinMaxDateTimes";
	DROP TABLE IF EXISTS "TMP_BatchFaultDataLocationIds";
	DROP TABLE IF EXISTS "TMP_UnprocessedFaultData2Batch";
	DROP TABLE IF EXISTS "TMP_LogRecordIdsForUnprocessedFaultData2Batch";
	
	-- Log.
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 10 (Drop temporary tables) Duration: %', duration_string;
	start_time := end_time;
	
	-- ======================================================================================
	-- Log end of function execution.
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - function_start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'Function ''%'' executed successfully. Total Duration: %', function_name, duration_string;
	
EXCEPTION
    WHEN others THEN
		-- Ensure temporary table cleanup on error.
		DROP TABLE IF EXISTS "TMP_DeviceDataMinMaxDateTimes";
		DROP TABLE IF EXISTS "TMP_BatchFaultDataLocationIds";
		DROP TABLE IF EXISTS "TMP_UnprocessedFaultData2Batch";
		DROP TABLE IF EXISTS "TMP_LogRecordIdsForUnprocessedFaultData2Batch";

        -- Handle any errors.
        RAISE NOTICE 'An unexpected error occurred: %', SQLERRM;
END;
$BODY$;

ALTER FUNCTION public."spFaultData2WithLagLeadLongLatBatch"(integer, integer, integer)
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spFaultData2WithLagLeadLongLatBatch"(integer, integer, integer) TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spFaultData2WithLagLeadLongLatBatch"(integer, integer, integer) FROM PUBLIC;


-- FUNCTION: public.spStatusData2WithLagLeadLongLatBatch(integer, integer, integer)

-- DROP FUNCTION IF EXISTS public."spStatusData2WithLagLeadLongLatBatch"(integer, integer, integer);

CREATE OR REPLACE FUNCTION public."spStatusData2WithLagLeadLongLatBatch"(
	"MaxDaysPerBatch" integer,
	"MaxBatchSize" integer,
	"BufferMinutes" integer)
    RETURNS TABLE(id bigint, "GeotabId" character varying, "StatusDataDateTime" timestamp without time zone, "DeviceId" bigint, "LagDateTime" timestamp without time zone, "LagLatitude" double precision, "LagLongitude" double precision, "LagSpeed" real, "LeadDateTime" timestamp without time zone, "LeadLatitude" double precision, "LeadLongitude" double precision, "LogRecords2MinDateTime" timestamp without time zone, "LogRecords2MaxDateTime" timestamp without time zone, "DeviceLogRecords2MinDateTime" timestamp without time zone, "DeviceLogRecords2MaxDateTime" timestamp without time zone) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 100000

AS $BODY$
-- ==========================================================================================
-- Description: Returns a batch of StatusData2 records with additional
--              metadata about the LogRecords2 table. Each returned record
--              also contains the DateTime, Latitude and Longitude values of the LogRecord2
--              records with DateTimes immediately before (or equal to) and after the 
--              DateTime of the StatusData2 record. This result set is intended to be used
--              for interpolation of location coordinates, speed, bearing and compass
--              direction for the subject StatusData2 records.
--
-- Parameters:
--		MaxDaysPerBatch: The maximum number of days over which unprocessed StatusData records 
--			in a batch can span.
--		MaxBatchSize: The maximum number of unprocessed StatusData records to retrieve for 
--			interpolation per batch.
--		BufferMinutes: When getting the DateTime range of a batch of unprocessed StatusData 
--			records, this buffer is applied to either end of the DateTime range when 
--			selecting LogRecords to use for interpolation such that lag LogRecords can be 
--			obtained for records that are “early” in the batch and lead LogRecords can be 
--			obtained for records that are “late” in the batch.
-- ==========================================================================================
DECLARE
	-- Constants:
	min_allowed_max_days_per_batch INTEGER := 1;
	max_allowed_max_days_per_batch INTEGER := 10;
	min_allowed_max_batch_size INTEGER := 10000;
	max_allowed_max_batch_size INTEGER := 500000;
	min_allowed_buffer_minutes INTEGER := 10;
	max_allowed_buffer_minutes INTEGER := 1440;

    -- The maximum number of days that can be spanned in a batch.
    max_days_per_batch INTEGER := "MaxDaysPerBatch";
    -- The maximum number of records to return.
    max_batch_size INTEGER := "MaxBatchSize";
    -- Buffer period, in minutes, for fetching encompassing values.
    buffer_minutes INTEGER := "BufferMinutes";

    -- Variables
	default_datetime TIMESTAMP WITHOUT TIME ZONE := '1900-01-01'::TIMESTAMP WITHOUT TIME ZONE;
    statusdata2_primary_partition_max_datetime TIMESTAMP WITHOUT TIME ZONE;
    logrecords2_min_datetime TIMESTAMP WITHOUT TIME ZONE;
    logrecords2_max_datetime TIMESTAMP WITHOUT TIME ZONE;
    function_name TEXT := 'spStatusData2WithLagLeadLongLatBatch';
    unprocessed_statusdata_min_datetime TIMESTAMP WITHOUT TIME ZONE;
    unprocessed_statusdata_max_allowed_datetime TIMESTAMP WITHOUT TIME ZONE;
    unprocessed_statusdata2_batch_min_datetime TIMESTAMP WITHOUT TIME ZONE;
    unprocessed_statusdata2_batch_max_datetime TIMESTAMP WITHOUT TIME ZONE;	
    buffered_unprocessed_statusdata2_batch_min_datetime TIMESTAMP WITHOUT TIME ZONE;
    buffered_unprocessed_statusdata2_batch_max_datetime TIMESTAMP WITHOUT TIME ZONE;		
    function_start_time TIMESTAMP;
    start_time TIMESTAMP;
    end_time TIMESTAMP;
    start_time_string TEXT;
    duration_string TEXT;
    record_count INTEGER;
BEGIN
	-- ======================================================================================
    -- Log start of stored procedure execution.
    function_start_time := CLOCK_TIMESTAMP();
    start_time := CLOCK_TIMESTAMP();
    start_time_string := TO_CHAR(start_time, 'YYYY-MM-DD HH24:MI:SS');
    RAISE NOTICE 'Executing function ''%'' Start: %', function_name, start_time_string;
    RAISE NOTICE '> max_days_per_batch: %', max_days_per_batch;
    RAISE NOTICE '> max_batch_size: %', max_batch_size;
    RAISE NOTICE '> buffer_minutes: %', buffer_minutes;

	-- ======================================================================================
    -- STEP 1: Validate input parameter values.
	RAISE NOTICE 'Step 1 [Validating input parameter values]...';
	
	-- MaxDaysPerBatch
	IF max_days_per_batch < min_allowed_max_days_per_batch OR max_days_per_batch > max_allowed_max_days_per_batch THEN
		RAISE EXCEPTION 'ERROR: MaxDaysPerBatch (%) is out of the allowed range [%, %].', 
			max_days_per_batch, min_allowed_max_days_per_batch, max_allowed_max_days_per_batch;
	END IF;

	-- MaxBatchSize
	IF max_batch_size < min_allowed_max_batch_size OR max_batch_size > max_allowed_max_batch_size THEN
		RAISE EXCEPTION 'ERROR: MaxBatchSize (%) is out of the allowed range [%, %].', 
			max_batch_size, min_allowed_max_batch_size, max_allowed_max_batch_size;
	END IF;

	-- BufferMinutes
	IF buffer_minutes < min_allowed_buffer_minutes OR buffer_minutes > max_allowed_buffer_minutes THEN
		RAISE EXCEPTION 'ERROR: BufferMinutes (%) is out of the allowed range [%, %].', 
			buffer_minutes, min_allowed_buffer_minutes, max_allowed_buffer_minutes;
	END IF;	
	
	-- Log
    end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds'; 
    RAISE NOTICE 'STEP 1 completed. Duration: %', duration_string;
    start_time := end_time;

	-- ======================================================================================
    -- STEP 2: Get the max DateTime value from the EARLIEST partition in the StatusData2 table.
	SELECT MAX(sd."DateTime") 
	INTO statusdata2_primary_partition_max_datetime
	FROM public."StatusDataLocations2_default" sd;
	
	-- If no data is found, set a default value
	IF statusdata2_primary_partition_max_datetime IS NULL THEN
	    statusdata2_primary_partition_max_datetime := default_datetime;
	END IF;
	
	-- Log the duration and the retrieved value (using RAISE NOTICE for logging)
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000;
	RAISE NOTICE 'STEP 2 Duration: % milliseconds', duration_string;
	RAISE NOTICE '> statusdata2_primary_partition_max_datetime: %', statusdata2_primary_partition_max_datetime;
	start_time := end_time;

	-- ======================================================================================
	-- STEP 3: 
	-- Get min and max DateTime values by Device for unprocessed StatusDataLocations2 
	-- records. Also get associated min and max DateTime values for LogRecords2 records where
	-- the DateTimes of the LogRecords are greater than or equal to the min DateTimes of the 
	-- unprocessed StatusDataLocations2 records. Do this ONLY for Devices that have both 
	-- StatusData and LogRecords. Exclude data from the EARLIEST partition.
	DROP TABLE IF EXISTS "TMP_DeviceDataMinMaxDateTimes";
	
	-- Create and populate the temporary table
	CREATE TEMP TABLE "TMP_DeviceDataMinMaxDateTimes" AS
	WITH StatusDataMinMax AS (
	    SELECT sdl."DeviceId",
	           MIN(sdl."DateTime") AS "DeviceStatusData2MinDateTime",
	           MAX(sdl."DateTime") AS "DeviceStatusData2MaxDateTime"
	    FROM public."StatusDataLocations2" sdl
	    WHERE sdl."DateTime" > statusdata2_primary_partition_max_datetime
	      AND sdl."LongLatProcessed" = false
	    GROUP BY sdl."DeviceId"
	),
	FilteredLogRecords AS (
	    SELECT lr."DeviceId",
	           MIN(lr."DateTime") AS "DeviceLogRecords2MinDateTime",
	           MAX(lr."DateTime") AS "DeviceLogRecords2MaxDateTime"
	    FROM public."LogRecords2" lr
	    WHERE lr."DateTime" >= (SELECT MIN("DeviceStatusData2MinDateTime") FROM StatusDataMinMax)
	    GROUP BY lr."DeviceId"
	)
	SELECT sd."DeviceId",
	       sd."DeviceStatusData2MinDateTime",
	       sd."DeviceStatusData2MaxDateTime",
	       flr."DeviceLogRecords2MinDateTime",
	       flr."DeviceLogRecords2MaxDateTime"
	FROM StatusDataMinMax sd
	INNER JOIN FilteredLogRecords flr
	    ON sd."DeviceId" = flr."DeviceId";

    -- Log
	GET DIAGNOSTICS record_count = ROW_COUNT;
    end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds'; 
	RAISE NOTICE 'STEP 3 (Create tmp_device_data_min_max_date_times) Duration: %', duration_string;
    RAISE NOTICE '> Record Count: %', record_count;
    start_time := end_time;

	-- ======================================================================================
	-- STEP 3A: 
	-- Add indexes to temporary table.
	CREATE INDEX "IX_TMP_DeviceDataMinMaxDateTimes_DeviceId" 
	ON "TMP_DeviceDataMinMaxDateTimes" ("DeviceId");
	
	CREATE INDEX "IX_TMP_DeviceDataMinMaxDateTimes_DeviceLogRecords2MinDateTime" 
	ON "TMP_DeviceDataMinMaxDateTimes" ("DeviceLogRecords2MinDateTime");
	
	CREATE INDEX "IX_TMP_DeviceDataMinMaxDateTimes_DeviceLogRecords2MaxDateTime" 
	ON "TMP_DeviceDataMinMaxDateTimes" ("DeviceLogRecords2MaxDateTime");

    -- Log
    end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds'; 
    RAISE NOTICE 'STEP 3A (Index TMP_DeviceDataMinMaxDateTimes) Duration: %', duration_string;
    start_time := end_time;

	-- ======================================================================================
	-- STEP 4:
	-- Get the DateTime of the unprocessed StatusDataLocations2 record with the lowest 
	-- DateTime value that is greater than the max DateTime value from the EARLIEST partition
	-- in the StatusDataLocations2 table.
	
	SELECT MIN(ddmm."DeviceStatusData2MinDateTime") INTO unprocessed_statusdata_min_datetime 
	FROM "TMP_DeviceDataMinMaxDateTimes" ddmm;
	
	-- Determine the maximum allowed DateTime for the current batch based on adding max_days_per_batch
	-- to the unprocessed_statusdata_min_datetime. The purpose of this is to limit the number of 
	-- partitions that must be scanned as well as the potential number of LogRecords that
	-- may be returned in subsequent queries - which may be insignificant with smaller fleets,
	-- but can have a huge impact with larger fleets and their associated data volumes.
	unprocessed_statusdata_max_allowed_datetime := 
	    (unprocessed_statusdata_min_datetime + (max_days_per_batch || ' days')::interval) - interval '1 second'; 
	
	-- Get the minimum DateTime value of any LogRecord.
	SELECT MIN(ddmm."DeviceLogRecords2MinDateTime") INTO logrecords2_min_datetime
	FROM "TMP_DeviceDataMinMaxDateTimes" ddmm;
	
	-- Get the maximum DateTime value of any LogRecord.
	SELECT MAX(ddmm."DeviceLogRecords2MaxDateTime") INTO logrecords2_max_datetime 
	FROM "TMP_DeviceDataMinMaxDateTimes" ddmm;
	
	-- Log.
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 4 Duration: %', duration_string;
	RAISE NOTICE '> unprocessed_statusdata_min_datetime: %', unprocessed_statusdata_min_datetime;
	RAISE NOTICE '> unprocessed_statusdata_max_allowed_datetime: %', unprocessed_statusdata_max_allowed_datetime;
	RAISE NOTICE '> logrecords2_min_datetime: %', logrecords2_min_datetime;
	RAISE NOTICE '> logrecords2_max_datetime: %', logrecords2_max_datetime;
	start_time := end_time;

	-- ======================================================================================
	-- STEP 5:
	-- Get up to the first max_batch_size of StatusDataLocations2 records within the 
	-- unprocessed_statusdata_min_datetime to unprocessed_statusdata_max_allowed_datetime DateTime 
	-- range. Also make sure not to exceed the logrecords2_max_datetime.
	DROP TABLE IF EXISTS "TMP_BatchStatusDataLocationIds";

	CREATE TEMP TABLE "TMP_BatchStatusDataLocationIds" AS
	SELECT sdl.id
	FROM public."StatusDataLocations2" sdl
	INNER JOIN "TMP_DeviceDataMinMaxDateTimes" ddmmdt ON sdl."DeviceId" = ddmmdt."DeviceId"
	WHERE sdl."LongLatProcessed" = false
	  AND sdl."DateTime" BETWEEN unprocessed_statusdata_min_datetime AND unprocessed_statusdata_max_allowed_datetime
	  AND sdl."DateTime" <= logrecords2_max_datetime
	LIMIT max_batch_size;	
	
	-- Log.
	GET DIAGNOSTICS record_count = ROW_COUNT;
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 5 (Create TMP_BatchStatusDataLocationIds) Duration: %', duration_string;
	RAISE NOTICE '> Record Count: %', record_count;
	start_time := end_time;

	-- ======================================================================================
	-- STEP 5A:
	-- Add index to temporary table.
	CREATE INDEX "IX_TMP_BatchStatusDataLocationIds_id" 
	ON "TMP_BatchStatusDataLocationIds" (id);
	
	-- Log.
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 5A (Index TMP_BatchStatusDataLocationIds) Duration: %', duration_string;
	start_time := end_time;

	-- ======================================================================================
	-- STEP 6:
	-- Get the batch of unprocessed StatusData2 records.
	DROP TABLE IF EXISTS "TMP_UnprocessedStatusData2Batch";

	CREATE TEMP TABLE "TMP_UnprocessedStatusData2Batch" AS
	SELECT bsdl.id,
		sd."GeotabId",
		sdl."DateTime",
		sdl."DeviceId",
		logrecords2_min_datetime AS "LogRecords2MinDateTime",
		logrecords2_max_datetime AS "LogRecords2MaxDateTime",
		dlrmm."DeviceLogRecords2MinDateTime",
		dlrmm."DeviceLogRecords2MaxDateTime"
	FROM  "TMP_BatchStatusDataLocationIds" bsdl
	LEFT JOIN public."StatusDataLocations2" sdl 
		ON bsdl.id = sdl.id
	LEFT JOIN "TMP_DeviceDataMinMaxDateTimes" dlrmm 
		ON sdl."DeviceId" = dlrmm."DeviceId"	
	LEFT JOIN public."StatusData2" sd
		ON bsdl.id = sd.id
	WHERE sd."DateTime" BETWEEN unprocessed_statusdata_min_datetime AND unprocessed_statusdata_max_allowed_datetime;

	-- Log.
	GET DIAGNOSTICS record_count = ROW_COUNT;
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 6 (Create TMP_UnprocessedStatusData2Batch) Duration: %', duration_string;
	RAISE NOTICE '> Record Count: %', record_count;
	start_time := end_time;

	-- ======================================================================================
	-- STEP 6A: 
	-- Add indexes to temporary table.
	CREATE INDEX "IX_TMP_UnprocessedStatusData2Batch_id" 
	ON "TMP_UnprocessedStatusData2Batch" ("id");
	
	CREATE INDEX "IX_TMP_UnprocessedStatusData2Batch_DateTime" 
	ON "TMP_UnprocessedStatusData2Batch" ("DateTime");
	
	CREATE INDEX "IX_TMP_UnprocessedStatusData2Batch_DeviceId_DateTime" 
	ON "TMP_UnprocessedStatusData2Batch" ("DeviceId", "DateTime");
	
	-- Log.
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 6A (Index TMP_UnprocessedStatusData2Batch) Duration: %', duration_string;
	start_time := end_time;	

	-- ======================================================================================
	-- STEP 7:
	-- Get the min and max DateTime values from TMP_UnprocessedStatusData2Batch.
	SELECT MIN("DateTime") INTO unprocessed_statusdata2_batch_min_datetime
	FROM "TMP_UnprocessedStatusData2Batch";	
	
	SELECT MAX("DateTime") INTO unprocessed_statusdata2_batch_max_datetime
	FROM "TMP_UnprocessedStatusData2Batch";

	buffered_unprocessed_statusdata2_batch_min_datetime := unprocessed_statusdata2_batch_min_datetime - (buffer_minutes || ' minutes')::interval;
	buffered_unprocessed_statusdata2_batch_max_datetime := unprocessed_statusdata2_batch_max_datetime + (buffer_minutes || ' minutes')::interval;

	-- Log.
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 7 Duration: %', duration_string;
	RAISE NOTICE '> unprocessed_statusdata2_batch_min_datetime: %', unprocessed_statusdata2_batch_min_datetime;
	RAISE NOTICE '> unprocessed_statusdata2_batch_max_datetime: %', unprocessed_statusdata2_batch_max_datetime;
	RAISE NOTICE '> buffered_unprocessed_statusdata2_batch_min_datetime: %', buffered_unprocessed_statusdata2_batch_min_datetime;
	RAISE NOTICE '> buffered_unprocessed_statusdata2_batch_max_datetime: %', buffered_unprocessed_statusdata2_batch_max_datetime;	
	start_time := end_time;

	-- ======================================================================================
	-- STEP 8:
	-- Get the Ids of all records from the LogRecords2 table that serve as lag, equal or lead
	-- to records in the TMP_UnprocessedStatusData2Batch based on DateTime.	
	-- Use the EntityMetadata2 table for speed in this step. Note: EntityType 1 = LogRecord.
	DROP TABLE IF EXISTS "TMP_LogRecordIdsForUnprocessedStatusData2Batch";
	
	CREATE TEMP TABLE "TMP_LogRecordIdsForUnprocessedStatusData2Batch" AS
	SELECT 
	    usdb.id AS "UnprocessedStatusDataEntityId",
	    emlaglr."EntityId" AS "LagLogRecordEntityId",
	    emleadlr."EntityId" AS "LeadLogRecordEntityId"
	FROM 
	    "TMP_UnprocessedStatusData2Batch" usdb
	LEFT JOIN LATERAL (
	    SELECT em."EntityId"
	    FROM public."EntityMetadata2" em
	    WHERE em."EntityType" = 1 
	      AND em."DeviceId" = usdb."DeviceId"
		  AND em."DateTime" >= buffered_unprocessed_statusdata2_batch_min_datetime
	      AND em."DateTime" <= usdb."DateTime"
	    ORDER BY em."DateTime" DESC
	    LIMIT 1
	) emlaglr ON true
	LEFT JOIN LATERAL (
	    SELECT em."EntityId"
	    FROM public."EntityMetadata2" em
	    WHERE em."EntityType" = 1 
	      AND em."DeviceId" = usdb."DeviceId"
		  AND em."DateTime" <= buffered_unprocessed_statusdata2_batch_max_datetime
	      AND em."DateTime" > usdb."DateTime"
	    ORDER BY em."DateTime" ASC
	    LIMIT 1
	) emleadlr ON true;

	-- Log.
	GET DIAGNOSTICS record_count = ROW_COUNT;
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 8 (Create TMP_LogRecordIdsForUnprocessedStatusData2Batch) Duration: %', duration_string;
	RAISE NOTICE '> Record Count: %', record_count;
	start_time := end_time;

	-- ======================================================================================
	-- STEP 8A:
	-- Add indexes to temporary table.
	CREATE INDEX "IX_TMP_LRIdsForUnprocessedSD2Batch_UnprocessedSDEntityId"
	ON "TMP_LogRecordIdsForUnprocessedStatusData2Batch" ("UnprocessedStatusDataEntityId");
	
	CREATE INDEX "IX_TMP_LRIdsForUnprocessedSD2Batch_LagLREntityId"
	ON "TMP_LogRecordIdsForUnprocessedStatusData2Batch" ("LagLogRecordEntityId");

	CREATE INDEX "IX_TMP_LRIdsForUnprocessedSD2Batch_LeadLREntityId"
	ON "TMP_LogRecordIdsForUnprocessedStatusData2Batch" ("LeadLogRecordEntityId");

	-- Log.
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 8A (Index TMP_LogRecordIdsForUnprocessedStatusData2Batch) Duration: %', duration_string;
	start_time := end_time;	

	-- ======================================================================================
	-- STEP 9:
	-- Produce and return the final output, sorted by StatusDataDateTime. Achieve this by 
	-- joining the TMP_LogRecordIdsForUnprocessedStatusData2Batch records to the StatusData2
	-- and LogRecords2 tables via multiple CTEs and combining the results. The DateTime 
	-- filters on the CTE queries are critical to ensuring partition pruning to achieve solid
	-- performance.
	RETURN QUERY
	WITH "StatusDataCols" AS (
		SELECT usdllbatch."UnprocessedStatusDataEntityId" AS "id",
			   sd."GeotabId",
			   sd."DateTime" AS "StatusDataDateTime",
			   sd."DeviceId"
		FROM "TMP_LogRecordIdsForUnprocessedStatusData2Batch" usdllbatch
		LEFT JOIN public."StatusData2" sd
			ON usdllbatch."UnprocessedStatusDataEntityId" = sd.id
		   AND sd."DateTime" BETWEEN buffered_unprocessed_statusdata2_batch_min_datetime
								AND buffered_unprocessed_statusdata2_batch_max_datetime
	),
	"LagLogRecordCols" AS (
		SELECT usdllbatch."UnprocessedStatusDataEntityId",
			   laglr."DateTime" AS "LagDateTime",
			   laglr."Latitude" AS "LagLatitude",
			   laglr."Longitude" AS "LagLongitude",
			   laglr."Speed" AS "LagSpeed"
		FROM "TMP_LogRecordIdsForUnprocessedStatusData2Batch" usdllbatch
		LEFT JOIN public."LogRecords2" laglr
			ON usdllbatch."LagLogRecordEntityId" = laglr.id
		   AND laglr."DateTime" BETWEEN buffered_unprocessed_statusdata2_batch_min_datetime
								AND buffered_unprocessed_statusdata2_batch_max_datetime
	),
	"LeadLogRecordCols" AS (
		SELECT usdllbatch."UnprocessedStatusDataEntityId",
			   leadlr."DateTime" AS "LeadDateTime",
			   leadlr."Latitude" AS "LeadLatitude",
			   leadlr."Longitude" AS "LeadLongitude"
		FROM "TMP_LogRecordIdsForUnprocessedStatusData2Batch" usdllbatch
		LEFT JOIN public."LogRecords2" leadlr
			ON usdllbatch."LeadLogRecordEntityId" = leadlr.id
		   AND leadlr."DateTime" BETWEEN buffered_unprocessed_statusdata2_batch_min_datetime
								AND buffered_unprocessed_statusdata2_batch_max_datetime
	)
	SELECT sdcols."id", 
		sdcols."GeotabId", 
		sdcols."StatusDataDateTime", 
		sdcols."DeviceId", 
		laglrcols."LagDateTime", 
		laglrcols."LagLatitude", 
		laglrcols."LagLongitude", 
		laglrcols."LagSpeed",
		leadlrcols."LeadDateTime", 
		leadlrcols."LeadLatitude", 
		leadlrcols."LeadLongitude", 
		logrecords2_min_datetime AS "LogRecords2MinDateTime",
		logrecords2_max_datetime AS "LogRecords2MaxDateTime",
		ddmm."DeviceLogRecords2MinDateTime",
		ddmm."DeviceLogRecords2MaxDateTime"
	FROM "StatusDataCols" sdcols
	LEFT JOIN "LagLogRecordCols" laglrcols 
		ON sdcols."id" = laglrcols."UnprocessedStatusDataEntityId"
	LEFT JOIN "LeadLogRecordCols" leadlrcols 
		ON sdcols."id" = leadlrcols."UnprocessedStatusDataEntityId"
	LEFT JOIN "TMP_DeviceDataMinMaxDateTimes" ddmm
		ON sdcols."DeviceId" = ddmm."DeviceId"
	ORDER BY sdcols."StatusDataDateTime";

	-- Log.
	GET DIAGNOSTICS record_count = ROW_COUNT;
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 9 (Return final output) Duration: %', duration_string;
	RAISE NOTICE '> Record Count: %', record_count;
	start_time := end_time;

	-- ======================================================================================
	-- STEP 10:
	-- Drop temporary tables.
	DROP TABLE IF EXISTS "TMP_DeviceDataMinMaxDateTimes";
	DROP TABLE IF EXISTS "TMP_BatchStatusDataLocationIds";
	DROP TABLE IF EXISTS "TMP_UnprocessedStatusData2Batch";
	DROP TABLE IF EXISTS "TMP_LogRecordIdsForUnprocessedStatusData2Batch";
	
	-- Log.
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'STEP 10 (Drop temporary tables) Duration: %', duration_string;
	start_time := end_time;

	-- ======================================================================================
	-- Log end of function execution.
	end_time := CLOCK_TIMESTAMP();
	duration_string := EXTRACT(EPOCH FROM (end_time - function_start_time)) * 1000 || ' milliseconds';
	RAISE NOTICE 'Function ''%'' executed successfully. Total Duration: %', function_name, duration_string;
	
EXCEPTION
    WHEN others THEN
		-- Ensure temporary table cleanup on error.
		DROP TABLE IF EXISTS "TMP_DeviceDataMinMaxDateTimes";
		DROP TABLE IF EXISTS "TMP_BatchStatusDataLocationIds";
		DROP TABLE IF EXISTS "TMP_UnprocessedStatusData2Batch";
		DROP TABLE IF EXISTS "TMP_LogRecordIdsForUnprocessedStatusData2Batch";

        -- Handle any errors.
        RAISE NOTICE 'An unexpected error occurred: %', SQLERRM;
END;
$BODY$;

ALTER FUNCTION public."spStatusData2WithLagLeadLongLatBatch"(integer, integer, integer)
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spStatusData2WithLagLeadLongLatBatch"(integer, integer, integer) TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spStatusData2WithLagLeadLongLatBatch"(integer, integer, integer) FROM PUBLIC;
/*** [END] Part 3 of 4: pgAdmin-Generated Script (functions) ***/ 



/*** [START] Part 4 of 4: Database Version Update ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc") 
VALUES ('3.0.0.0', timezone('UTC', NOW())); 
/*** [END] Part 4 of 4: Database Version Update ***/
