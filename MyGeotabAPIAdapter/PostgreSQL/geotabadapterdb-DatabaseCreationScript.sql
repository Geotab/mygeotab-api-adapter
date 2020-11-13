--
-- PostgreSQL database dump
--

-- Dumped from database version 11.7
-- Dumped by pg_dump version 11.7

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

--
-- Name: geotabadapterdb; Type: DATABASE; Schema: -; Owner: geotabadapter_owner
--

CREATE DATABASE geotabadapterdb WITH TEMPLATE = template0 ENCODING = 'UTF8' LC_COLLATE = 'English_United States.1252' LC_CTYPE = 'English_United States.1252';


ALTER DATABASE geotabadapterdb OWNER TO geotabadapter_owner;

\connect geotabadapterdb

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

SET default_with_oids = false;

--
-- Name: Conditions; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."Conditions" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ParentId" character varying(50),
    "RuleId" character varying(50),
    "ConditionType" character varying(50) NOT NULL,
    "DeviceId" character varying(50),
    "DiagnosticId" character varying(100),
    "DriverId" character varying(50),
    "Value" double precision,
    "WorkTimeId" character varying(50),
    "ZoneId" character varying(50),
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."Conditions" OWNER TO geotabadapter_owner;

--
-- Name: Conditions_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."Conditions_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."Conditions_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: Conditions_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."Conditions_id_seq" OWNED BY public."Conditions".id;


--
-- Name: ConfigFeedVersions; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."ConfigFeedVersions" (
    id bigint NOT NULL,
    "FeedTypeId" character varying(50) NOT NULL,
    "LastProcessedFeedVersion" bigint NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."ConfigFeedVersions" OWNER TO geotabadapter_owner;

--
-- Name: ConfigFeedVersions_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."ConfigFeedVersions_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."ConfigFeedVersions_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: ConfigFeedVersions_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."ConfigFeedVersions_id_seq" OWNED BY public."ConfigFeedVersions".id;


--
-- Name: DVIRDefectRemarks; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."DVIRDefectRemarks" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DVIRDefectId" character varying(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Remark" text NOT NULL,
    "RemarkUserId" character varying(50),
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."DVIRDefectRemarks" OWNER TO geotabadapter_owner;

--
-- Name: DVIRDefectRemarks_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."DVIRDefectRemarks_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."DVIRDefectRemarks_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: DVIRDefectRemarks_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."DVIRDefectRemarks_id_seq" OWNED BY public."DVIRDefectRemarks".id;


--
-- Name: DVIRDefects; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."DVIRDefects" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DVIRLogId" character varying(50) NOT NULL,
    "DefectListAssetType" character varying(50),
    "DefectListId" character varying(50),
    "DefectListName" character varying(255),
    "PartId" character varying(50),
    "PartName" character varying(255),
    "DefectId" character varying(50),
    "DefectName" character varying(255),
    "DefectSeverity" character varying(50),
    "RepairDateTime" timestamp without time zone,
    "RepairStatus" character varying(50),
    "RepairUserId" character varying(50),
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."DVIRDefects" OWNER TO geotabadapter_owner;

--
-- Name: DVIRDefects_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."DVIRDefects_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."DVIRDefects_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: DVIRDefects_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."DVIRDefects_id_seq" OWNED BY public."DVIRDefects".id;


--
-- Name: DVIRLogs; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."DVIRLogs" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "CertifiedByUserId" character varying(50),
    "CertifiedDate" timestamp without time zone,
    "CertifyRemark" text,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" character varying(50),
    "DriverId" character varying(50),
    "DriverRemark" text,
    "IsSafeToOperate" boolean,
    "LocationLatitude" double precision,
    "LocationLongitude" double precision,
    "LogType" character varying(50),
    "RepairDate" timestamp without time zone,
    "RepairRemark" text,
    "RepairedByUserId" character varying(50),
    "TrailerId" character varying(50),
    "TrailerName" character varying(255),
    "Version" bigint NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."DVIRLogs" OWNER TO geotabadapter_owner;

--
-- Name: DVIRLogs_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."DVIRLogs_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."DVIRLogs_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: DVIRLogs_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."DVIRLogs_id_seq" OWNED BY public."DVIRLogs".id;


--
-- Name: Devices; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."Devices" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp(4) without time zone,
    "ActiveTo" timestamp(4) without time zone,
    "DeviceType" character varying(50) NOT NULL,
    "LicensePlate" character varying(50),
    "LicenseState" character varying(50),
    "Name" character varying(50) NOT NULL,
    "ProductId" integer,
    "SerialNumber" character varying(12) NOT NULL,
    "VIN" character varying(50),
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp(4) without time zone NOT NULL
);


ALTER TABLE public."Devices" OWNER TO geotabadapter_owner;

--
-- Name: Devices_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."Devices_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."Devices_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: Devices_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."Devices_id_seq" OWNED BY public."Devices".id;


--
-- Name: Diagnostics; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."Diagnostics" (
    id bigint NOT NULL,
    "GeotabId" character varying(100) NOT NULL,
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


ALTER TABLE public."Diagnostics" OWNER TO geotabadapter_owner;

--
-- Name: Diagnostics_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."Diagnostics_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."Diagnostics_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: Diagnostics_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."Diagnostics_id_seq" OWNED BY public."Diagnostics".id;


--
-- Name: ExceptionEvents; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."ExceptionEvents" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone,
    "ActiveTo" timestamp without time zone,
    "DeviceId" character varying(50),
    "Distance" real,
    "DriverId" character varying(50),
    "DurationTicks" bigint,
    "RuleId" character varying(50),
    "Version" bigint,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."ExceptionEvents" OWNER TO geotabadapter_owner;

--
-- Name: ExceptionEvents_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."ExceptionEvents_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."ExceptionEvents_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: ExceptionEvents_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."ExceptionEvents_id_seq" OWNED BY public."ExceptionEvents".id;


--
-- Name: FaultData; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."FaultData" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "AmberWarningLamp" boolean,
    "ClassCode" character varying(50),
    "ControllerId" character varying(100) NOT NULL,
    "ControllerName" character varying(255),
    "Count" integer NOT NULL,
    "DateTime" timestamp(4) without time zone,
    "DeviceId" character varying(50) NOT NULL,
    "DiagnosticId" character varying(100) NOT NULL,
    "DismissDateTime" timestamp without time zone,
    "DismissUserId" character varying(50),
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
    "RecordCreationTimeUtc" timestamp(4) without time zone NOT NULL
);


ALTER TABLE public."FaultData" OWNER TO geotabadapter_owner;

--
-- Name: FaultData_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."FaultData_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."FaultData_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: FaultData_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."FaultData_id_seq" OWNED BY public."FaultData".id;


--
-- Name: LogRecords; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."LogRecords" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" character varying(50) NOT NULL,
    "Latitude" double precision DEFAULT 0 NOT NULL,
    "Longitude" double precision DEFAULT 0 NOT NULL,
    "Speed" real DEFAULT 0 NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."LogRecords" OWNER TO geotabadapter_owner;

--
-- Name: LogRecords_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."LogRecords_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."LogRecords_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: LogRecords_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."LogRecords_id_seq" OWNED BY public."LogRecords".id;


--
-- Name: MyGeotabVersionInfo; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."MyGeotabVersionInfo" (
    "DatabaseName" character varying(58) NOT NULL,
    "Server" character varying(50) NOT NULL,
    "DatabaseVersion" character varying(50) NOT NULL,
    "ApplicationBuild" character varying(50) NOT NULL,
    "ApplicationBranch" character varying(50) NOT NULL,
    "ApplicationCommit" character varying(50) NOT NULL,
    "GoTalkVersion" character varying(50) NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."MyGeotabVersionInfo" OWNER TO geotabadapter_owner;


--
-- Name: Rules; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."Rules" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone,
    "ActiveTo" timestamp without time zone,
    "BaseType" character varying(50),
    "Comment" character varying(255),
    "Name" character varying(255),
    "Version" bigint NOT NULL,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."Rules" OWNER TO geotabadapter_owner;

--
-- Name: Rules_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."Rules_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."Rules_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: Rules_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."Rules_id_seq" OWNED BY public."Rules".id;


--
-- Name: StatusData; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."StatusData" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "Data" double precision,
    "DateTime" timestamp without time zone,
    "DeviceId" character varying(50) NOT NULL,
    "DiagnosticId" character varying(100) NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."StatusData" OWNER TO geotabadapter_owner;

--
-- Name: StatusData_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."StatusData_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."StatusData_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: StatusData_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."StatusData_id_seq" OWNED BY public."StatusData".id;


--
-- Name: Trips; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."Trips" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DeviceId" character varying(50) NOT NULL,
    "DriverId" character varying(50) NOT NULL,
    "Distance" real NOT NULL,
    "DrivingDurationTicks" bigint NOT NULL,
    "NextTripStart" timestamp without time zone NOT NULL,
    "Start" timestamp without time zone NOT NULL,
    "Stop" timestamp without time zone NOT NULL,
    "StopDurationTicks" bigint NOT NULL,
    "StopPointX" double precision,
    "StopPointY" double precision,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."Trips" OWNER TO geotabadapter_owner;

--
-- Name: Trips_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."Trips_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."Trips_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: Trips_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."Trips_id_seq" OWNED BY public."Trips".id;


--
-- Name: Users; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."Users" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone NOT NULL,
    "ActiveTo" timestamp without time zone NOT NULL,
    "EmployeeNo" character varying(50),
    "FirstName" character varying(255) NOT NULL,
    "IsDriver" boolean NOT NULL,
    "LastName" character varying(255) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."Users" OWNER TO geotabadapter_owner;

--
-- Name: Users_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."Users_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."Users_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: Users_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."Users_id_seq" OWNED BY public."Users".id;


--
-- Name: Zones; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."Zones" (
    id bigint NOT NULL,
    "GeotabId" character varying(100) NOT NULL,
    "ActiveFrom" timestamp without time zone,
    "ActiveTo" timestamp without time zone,
    "CentroidLatitude" double precision,
    "CentroidLongitude" double precision,
    "Comment" character varying(255),
    "Displayed" boolean,
    "ExternalReference" character varying(255),
    "MustIdentifyStops" boolean,
    "Name" character varying(255) NOT NULL,
    "Points" text,
    "Version" bigint,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."Zones" OWNER TO geotabadapter_owner;

--
-- Name: Zones_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."Zones_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."Zones_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: Zones_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."Zones_id_seq" OWNED BY public."Zones".id;


--
-- Name: vwRuleObject; Type: VIEW; Schema: public; Owner: geotabadapter_owner
--

CREATE VIEW public."vwRuleObject" AS
 SELECT r."GeotabId",
    r."ActiveFrom",
    r."ActiveTo",
    r."BaseType",
    r."Comment",
    r."Name",
    r."Version",
    r."EntityStatus",
    r."RecordLastChangedUtc",
    c."GeotabId" AS "Cond_Id",
    c."ParentId" AS "Cond_ParentId",
    c."RuleId" AS "Cond_RuleId",
    c."ConditionType" AS "Cond_ConditionType",
    c."DeviceId" AS "Cond_DeviceId",
    c."DiagnosticId" AS "Cond_DiagnosticId",
    c."DriverId" AS "Cond_DriverId",
    c."Value" AS "Cond_Value",
    c."WorkTimeId" AS "Cond_WorkTimeId",
    c."ZoneId" AS "Cond_ZoneId",
    c."EntityStatus" AS "Cond_EntityStatus",
    c."RecordLastChangedUtc" AS "Cond_RecordLastChangedUtc"
   FROM (public."Rules" r
     JOIN public."Conditions" c ON (((r."GeotabId")::text = (c."RuleId")::text)))
  ORDER BY r."GeotabId", c."RuleId";


ALTER TABLE public."vwRuleObject" OWNER TO geotabadapter_owner;

--
-- Name: Conditions id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Conditions" ALTER COLUMN id SET DEFAULT nextval('public."Conditions_id_seq"'::regclass);


--
-- Name: ConfigFeedVersions id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."ConfigFeedVersions" ALTER COLUMN id SET DEFAULT nextval('public."ConfigFeedVersions_id_seq"'::regclass);


--
-- Name: DVIRDefectRemarks id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRDefectRemarks" ALTER COLUMN id SET DEFAULT nextval('public."DVIRDefectRemarks_id_seq"'::regclass);


--
-- Name: DVIRDefects id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRDefects" ALTER COLUMN id SET DEFAULT nextval('public."DVIRDefects_id_seq"'::regclass);


--
-- Name: DVIRLogs id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRLogs" ALTER COLUMN id SET DEFAULT nextval('public."DVIRLogs_id_seq"'::regclass);


--
-- Name: Devices id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Devices" ALTER COLUMN id SET DEFAULT nextval('public."Devices_id_seq"'::regclass);


--
-- Name: Diagnostics id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Diagnostics" ALTER COLUMN id SET DEFAULT nextval('public."Diagnostics_id_seq"'::regclass);


--
-- Name: ExceptionEvents id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."ExceptionEvents" ALTER COLUMN id SET DEFAULT nextval('public."ExceptionEvents_id_seq"'::regclass);


--
-- Name: FaultData id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."FaultData" ALTER COLUMN id SET DEFAULT nextval('public."FaultData_id_seq"'::regclass);


--
-- Name: LogRecords id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."LogRecords" ALTER COLUMN id SET DEFAULT nextval('public."LogRecords_id_seq"'::regclass);


--
-- Name: Rules id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Rules" ALTER COLUMN id SET DEFAULT nextval('public."Rules_id_seq"'::regclass);


--
-- Name: StatusData id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."StatusData" ALTER COLUMN id SET DEFAULT nextval('public."StatusData_id_seq"'::regclass);


--
-- Name: Trips id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Trips" ALTER COLUMN id SET DEFAULT nextval('public."Trips_id_seq"'::regclass);


--
-- Name: Users id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Users" ALTER COLUMN id SET DEFAULT nextval('public."Users_id_seq"'::regclass);


--
-- Name: Zones id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Zones" ALTER COLUMN id SET DEFAULT nextval('public."Zones_id_seq"'::regclass);


--
-- Name: Conditions Conditions_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Conditions"
    ADD CONSTRAINT "Conditions_pkey" PRIMARY KEY (id);


--
-- Name: ConfigFeedVersions ConfigFeedVersions_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."ConfigFeedVersions"
    ADD CONSTRAINT "ConfigFeedVersions_pkey" PRIMARY KEY (id);


--
-- Name: DVIRDefectRemarks DVIRDefectRemarks_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRDefectRemarks"
    ADD CONSTRAINT "DVIRDefectRemarks_pkey" PRIMARY KEY (id);


--
-- Name: DVIRDefects DVIRDefects_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRDefects"
    ADD CONSTRAINT "DVIRDefects_pkey" PRIMARY KEY (id);


--
-- Name: DVIRLogs DVIRLogs_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRLogs"
    ADD CONSTRAINT "DVIRLogs_pkey" PRIMARY KEY (id);


--
-- Name: Devices Devices_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Devices"
    ADD CONSTRAINT "Devices_pkey" PRIMARY KEY (id);


--
-- Name: Diagnostics Diagnostics_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Diagnostics"
    ADD CONSTRAINT "Diagnostics_pkey" PRIMARY KEY (id);


--
-- Name: ExceptionEvents ExceptionEvents_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."ExceptionEvents"
    ADD CONSTRAINT "ExceptionEvents_pkey" PRIMARY KEY (id);


--
-- Name: FaultData FaultData_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."FaultData"
    ADD CONSTRAINT "FaultData_pkey" PRIMARY KEY (id);


--
-- Name: LogRecords LogRecords_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."LogRecords"
    ADD CONSTRAINT "LogRecords_pkey" PRIMARY KEY (id);


--
-- Name: Rules Rules_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Rules"
    ADD CONSTRAINT "Rules_pkey" PRIMARY KEY (id);


--
-- Name: StatusData StatusData_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."StatusData"
    ADD CONSTRAINT "StatusData_pkey" PRIMARY KEY (id);


--
-- Name: Trips Trips_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Trips"
    ADD CONSTRAINT "Trips_pkey" PRIMARY KEY (id);


--
-- Name: Users Users_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "Users_pkey" PRIMARY KEY (id);


--
-- Name: Zones Zones_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Zones"
    ADD CONSTRAINT "Zones_pkey" PRIMARY KEY (id);


--
-- Name: TABLE "Conditions"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Conditions" TO geotabadapter_client;


--
-- Name: SEQUENCE "Conditions_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

REVOKE ALL ON SEQUENCE public."Conditions_id_seq" FROM geotabadapter_owner;
GRANT ALL ON SEQUENCE public."Conditions_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "ConfigFeedVersions"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."ConfigFeedVersions" TO geotabadapter_client;


--
-- Name: SEQUENCE "ConfigFeedVersions_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."ConfigFeedVersions_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "DVIRDefectRemarks"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DVIRDefectRemarks" TO geotabadapter_client;


--
-- Name: SEQUENCE "DVIRDefectRemarks_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."DVIRDefectRemarks_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "DVIRDefects"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DVIRDefects" TO geotabadapter_client;


--
-- Name: SEQUENCE "DVIRDefects_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."DVIRDefects_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "DVIRLogs"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DVIRLogs" TO geotabadapter_client;


--
-- Name: SEQUENCE "DVIRLogs_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."DVIRLogs_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "Devices"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Devices" TO geotabadapter_client;


--
-- Name: SEQUENCE "Devices_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."Devices_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "Diagnostics"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Diagnostics" TO geotabadapter_client;


--
-- Name: SEQUENCE "Diagnostics_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."Diagnostics_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "ExceptionEvents"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."ExceptionEvents" TO geotabadapter_client;


--
-- Name: SEQUENCE "ExceptionEvents_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."ExceptionEvents_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "FaultData"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."FaultData" TO geotabadapter_client;


--
-- Name: SEQUENCE "FaultData_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."FaultData_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "LogRecords"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."LogRecords" TO geotabadapter_client;


--
-- Name: SEQUENCE "LogRecords_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."LogRecords_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "MyGeotabVersionInfo"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."MyGeotabVersionInfo" TO geotabadapter_client;


--
-- Name: TABLE "Rules"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Rules" TO geotabadapter_client;


--
-- Name: SEQUENCE "Rules_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."Rules_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "StatusData"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,UPDATE ON TABLE public."StatusData" TO geotabadapter_client;


--
-- Name: SEQUENCE "StatusData_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."StatusData_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "Trips"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Trips" TO geotabadapter_client;


--
-- Name: SEQUENCE "Trips_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."Trips_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "Users"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Users" TO geotabadapter_client;


--
-- Name: SEQUENCE "Users_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."Users_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "Zones"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Zones" TO geotabadapter_client;


--
-- Name: SEQUENCE "Zones_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."Zones_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "vwRuleObject"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."vwRuleObject" TO geotabadapter_client;


--
-- Name: DEFAULT PRIVILEGES FOR TABLES; Type: DEFAULT ACL; Schema: public; Owner: postgres
--

ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public REVOKE ALL ON TABLES  FROM postgres;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public GRANT SELECT,INSERT,DELETE,UPDATE ON TABLES  TO geotabadapter_client;


--
-- Name: DEFAULT PRIVILEGES FOR TABLES; Type: DEFAULT ACL; Schema: -; Owner: postgres
--

ALTER DEFAULT PRIVILEGES FOR ROLE postgres GRANT SELECT,INSERT,DELETE,UPDATE ON TABLES  TO geotabadapter_client;


--
-- PostgreSQL database dump complete
--

