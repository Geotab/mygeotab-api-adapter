-- ================================================================================
-- DATABASE TYPE: PostgreSQL
-- 
-- NOTES: 
--   1: This script applies to the MyGeotab API Adapter database starting with
--	    application version 3.0.0.0. It does not apply to earlier versions of the
--      application. 
--   2: This script will be updated with future schema changes such that someone
--      starting with a future version of the application can use this script as a
--      starting point without having to apply any earlier cumulative updates.
--   3: Be sure to connect to the "geotabadapterdb" before executing. 
--
-- DESCRIPTION: 
--   This script is intended for use in creating the database schema in an empty 
--   database starting from version 3.0.0.0 of the MyGeotab API Adapter and including
--   any cumulative schema changes up to the current application version at time of
--   download.
-- ================================================================================


/*** [START] Part 1: Install required extensions ***/ 
--
-- Name: pg_stat_statements; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pg_stat_statements WITH SCHEMA public;


--
-- Name: EXTENSION pg_stat_statements; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION pg_stat_statements IS 'track planning and execution statistics of all SQL statements executed';


--
-- Name: pgstattuple; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pgstattuple WITH SCHEMA public;


--
-- Name: EXTENSION pgstattuple; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION pgstattuple IS 'show tuple-level statistics';
/*** [END] Part 1: Install required extensions ***/ 



/*** [START] Part 2: pgAdmin-Generated Script (tables, sequences, views) ***/ 
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

--
-- Name: BinaryData2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."BinaryData2" (
    id uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "BinaryType" character varying(50),
    "ControllerId" character varying(50) NOT NULL,
    "Data" bytea NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" bigint NOT NULL,
    "Version" bigint,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
)
PARTITION BY RANGE ("DateTime");


ALTER TABLE public."BinaryData2" OWNER TO geotabadapter_client;

--
-- Name: ChargeEvents2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."ChargeEvents2" (
    id uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ChargeIsEstimated" boolean NOT NULL,
    "ChargeType" character varying(50) NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DurationTicks" bigint NOT NULL,
    "EndStateOfCharge" double precision,
    "EnergyConsumedKwh" double precision,
    "EnergyUsedSinceLastChargeKwh" double precision,
    "Latitude" double precision,
    "Longitude" double precision,
    "MaxACVoltage" double precision,
    "MeasuredBatteryEnergyInKwh" double precision,
    "MeasuredBatteryEnergyOutKwh" double precision,
    "MeasuredOnBoardChargerEnergyInKwh" double precision,
    "MeasuredOnBoardChargerEnergyOutKwh" double precision,
    "PeakPowerKw" double precision,
    "StartStateOfCharge" double precision,
    "StartTime" timestamp without time zone NOT NULL,
    "TripStop" timestamp without time zone,
    "Version" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
)
PARTITION BY RANGE ("StartTime");


ALTER TABLE public."ChargeEvents2" OWNER TO geotabadapter_client;

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
-- Name: DriverChanges2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."DriverChanges2" (
    id uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DriverId" bigint,
    "Type" character varying(50) NOT NULL,
    "Version" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
)
PARTITION BY RANGE ("DateTime");


ALTER TABLE public."DriverChanges2" OWNER TO geotabadapter_client;

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
-- Name: ExceptionEvents2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."ExceptionEvents2" (
    id uuid NOT NULL,
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


ALTER TABLE public."ExceptionEvents2" OWNER TO geotabadapter_client;

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
-- Name: Groups2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."Groups2" (
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


ALTER TABLE public."Groups2" OWNER TO geotabadapter_client;

--
-- Name: Groups2_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_client
--

CREATE SEQUENCE public."Groups2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Groups2_id_seq" OWNER TO geotabadapter_client;

--
-- Name: Groups2_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_client
--

ALTER SEQUENCE public."Groups2_id_seq" OWNED BY public."Groups2".id;


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
-- Name: Rules2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."Rules2" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone,
    "ActiveTo" timestamp without time zone,
    "BaseType" character varying(50),
    "Comment" character varying,
    "Condition" text,
    "Groups" text,
    "Name" character varying(255),
    "Version" bigint NOT NULL,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."Rules2" OWNER TO geotabadapter_client;

--
-- Name: Rules2_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_client
--

CREATE SEQUENCE public."Rules2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Rules2_id_seq" OWNER TO geotabadapter_client;

--
-- Name: Rules2_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_client
--

ALTER SEQUENCE public."Rules2_id_seq" OWNED BY public."Rules2".id;


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
-- Name: Trips2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."Trips2" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "AfterHoursDistance" real,
    "AfterHoursDrivingDurationTicks" bigint,
    "AfterHoursEnd" boolean,
    "AfterHoursStart" boolean,
    "AfterHoursStopDurationTicks" bigint,
    "AverageSpeed" real,
    "DeletedDateTime" timestamp without time zone,
    "DeviceId" bigint NOT NULL,
    "Distance" real NOT NULL,
    "DriverId" bigint,
    "DrivingDurationTicks" bigint NOT NULL,
    "IdlingDurationTicks" bigint,
    "MaximumSpeed" real,
    "NextTripStart" timestamp without time zone NOT NULL,
    "SpeedRange1" integer,
    "SpeedRange1DurationTicks" bigint,
    "SpeedRange2" integer,
    "SpeedRange2DurationTicks" bigint,
    "SpeedRange3" integer,
    "SpeedRange3DurationTicks" bigint,
    "Start" timestamp without time zone NOT NULL,
    "Stop" timestamp without time zone NOT NULL,
    "StopDurationTicks" bigint NOT NULL,
    "StopPointX" double precision,
    "StopPointY" double precision,
    "WorkDistance" real,
    "WorkDrivingDurationTicks" bigint,
    "WorkStopDurationTicks" bigint,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
)
PARTITION BY RANGE ("Start");


ALTER TABLE public."Trips2" OWNER TO geotabadapter_client;

--
-- Name: Trips2_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_client
--

CREATE SEQUENCE public."Trips2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Trips2_id_seq" OWNER TO geotabadapter_client;

--
-- Name: Trips2_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_client
--

ALTER SEQUENCE public."Trips2_id_seq" OWNED BY public."Trips2".id;


--
-- Name: Users2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."Users2" (
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
    "Groups" text,
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
-- Name: stg_ChargeEvents2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."stg_ChargeEvents2" (
    id uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ChargeIsEstimated" boolean NOT NULL,
    "ChargeType" character varying(50) NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DurationTicks" bigint NOT NULL,
    "EndStateOfCharge" double precision,
    "EnergyConsumedKwh" double precision,
    "EnergyUsedSinceLastChargeKwh" double precision,
    "Latitude" double precision,
    "Longitude" double precision,
    "MaxACVoltage" double precision,
    "MeasuredBatteryEnergyInKwh" double precision,
    "MeasuredBatteryEnergyOutKwh" double precision,
    "MeasuredOnBoardChargerEnergyInKwh" double precision,
    "MeasuredOnBoardChargerEnergyOutKwh" double precision,
    "PeakPowerKw" double precision,
    "StartStateOfCharge" double precision,
    "StartTime" timestamp without time zone NOT NULL,
    "TripStop" timestamp without time zone,
    "Version" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."stg_ChargeEvents2" OWNER TO geotabadapter_client;

--
-- Name: stg_Devices2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

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

--
-- Name: stg_Diagnostics2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

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

--
-- Name: stg_Diagnostics2_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_client
--

CREATE SEQUENCE public."stg_Diagnostics2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."stg_Diagnostics2_id_seq" OWNER TO geotabadapter_client;

--
-- Name: stg_Diagnostics2_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_client
--

ALTER SEQUENCE public."stg_Diagnostics2_id_seq" OWNED BY public."stg_Diagnostics2".id;


--
-- Name: stg_DriverChanges2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."stg_DriverChanges2" (
    id uuid NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DriverId" bigint,
    "Type" character varying(50) NOT NULL,
    "Version" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."stg_DriverChanges2" OWNER TO geotabadapter_client;

--
-- Name: stg_ExceptionEvents2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."stg_ExceptionEvents2" (
    id uuid NOT NULL,
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
);


ALTER TABLE public."stg_ExceptionEvents2" OWNER TO geotabadapter_client;

--
-- Name: stg_Groups2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

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

--
-- Name: stg_Groups2_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_client
--

CREATE SEQUENCE public."stg_Groups2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."stg_Groups2_id_seq" OWNER TO geotabadapter_client;

--
-- Name: stg_Groups2_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_client
--

ALTER SEQUENCE public."stg_Groups2_id_seq" OWNED BY public."stg_Groups2".id;


--
-- Name: stg_Rules2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."stg_Rules2" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone,
    "ActiveTo" timestamp without time zone,
    "BaseType" character varying(50),
    "Comment" character varying,
    "Condition" text,
    "Groups" text,
    "Name" character varying(255),
    "Version" bigint NOT NULL,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."stg_Rules2" OWNER TO geotabadapter_client;

--
-- Name: stg_Rules2_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_client
--

CREATE SEQUENCE public."stg_Rules2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."stg_Rules2_id_seq" OWNER TO geotabadapter_client;

--
-- Name: stg_Rules2_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_client
--

ALTER SEQUENCE public."stg_Rules2_id_seq" OWNED BY public."stg_Rules2".id;


--
-- Name: stg_Trips2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."stg_Trips2" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "AfterHoursDistance" real,
    "AfterHoursDrivingDurationTicks" bigint,
    "AfterHoursEnd" boolean,
    "AfterHoursStart" boolean,
    "AfterHoursStopDurationTicks" bigint,
    "AverageSpeed" real,
    "DeletedDateTime" timestamp without time zone,
    "DeviceId" bigint NOT NULL,
    "Distance" real NOT NULL,
    "DriverId" bigint,
    "DrivingDurationTicks" bigint NOT NULL,
    "IdlingDurationTicks" bigint,
    "MaximumSpeed" real,
    "NextTripStart" timestamp without time zone NOT NULL,
    "SpeedRange1" integer,
    "SpeedRange1DurationTicks" bigint,
    "SpeedRange2" integer,
    "SpeedRange2DurationTicks" bigint,
    "SpeedRange3" integer,
    "SpeedRange3DurationTicks" bigint,
    "Start" timestamp without time zone NOT NULL,
    "Stop" timestamp without time zone NOT NULL,
    "StopDurationTicks" bigint NOT NULL,
    "StopPointX" double precision,
    "StopPointY" double precision,
    "WorkDistance" real,
    "WorkDrivingDurationTicks" bigint,
    "WorkStopDurationTicks" bigint,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."stg_Trips2" OWNER TO geotabadapter_client;

--
-- Name: stg_Trips2_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_client
--

CREATE SEQUENCE public."stg_Trips2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."stg_Trips2_id_seq" OWNER TO geotabadapter_client;

--
-- Name: stg_Trips2_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_client
--

ALTER SEQUENCE public."stg_Trips2_id_seq" OWNED BY public."stg_Trips2".id;


--
-- Name: stg_Users2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

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

--
-- Name: stg_ZoneTypes2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

CREATE TABLE public."stg_ZoneTypes2" (
    id bigint NOT NULL,
    "GeotabId" character varying(100) NOT NULL,
    "Comment" character varying(255),
    "Name" character varying(255) NOT NULL,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."stg_ZoneTypes2" OWNER TO geotabadapter_client;

--
-- Name: stg_ZoneTypes2_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_client
--

CREATE SEQUENCE public."stg_ZoneTypes2_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."stg_ZoneTypes2_id_seq" OWNER TO geotabadapter_client;

--
-- Name: stg_ZoneTypes2_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_client
--

ALTER SEQUENCE public."stg_ZoneTypes2_id_seq" OWNED BY public."stg_ZoneTypes2".id;


--
-- Name: stg_Zones2; Type: TABLE; Schema: public; Owner: geotabadapter_client
--

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
          WHERE ((((pg_stat_user_tables.n_dead_tup)::numeric > (0.2 * (pg_stat_user_tables.n_live_tup)::numeric)) OR ((pg_stat_user_tables.n_mod_since_analyze)::numeric > (0.1 * (pg_stat_user_tables.n_live_tup)::numeric)) OR (pg_stat_user_tables.n_dead_tup > 1000)) AND (pg_stat_user_tables.schemaname = 'public'::name))
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
 SELECT row_number() OVER (ORDER BY "Table") AS "RowId",
    "Table",
    "Total",
    "LongLatProcessedTotal",
        CASE
            WHEN ("Total" > 0) THEN ((("LongLatProcessedTotal")::numeric * 100.0) / ("Total")::numeric)
            ELSE (0)::numeric
        END AS "LongLatProcessedPercentage"
   FROM ( SELECT 'StatusDataLocations2'::text AS "Table",
            count(*) AS "Total",
            sum(
                CASE
                    WHEN ("StatusDataLocations2"."LongLatProcessed" IS TRUE) THEN 1
                    ELSE 0
                END) AS "LongLatProcessedTotal"
           FROM public."StatusDataLocations2"
        UNION ALL
         SELECT 'FaultDataLocations2'::text AS "Table",
            count(*) AS "Total",
            sum(
                CASE
                    WHEN ("FaultDataLocations2"."LongLatProcessed" IS TRUE) THEN 1
                    ELSE 0
                END) AS "LongLatProcessedTotal"
           FROM public."FaultDataLocations2") "InterpolationProgress";


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
-- Name: Groups2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."Groups2" ALTER COLUMN id SET DEFAULT nextval('public."Groups2_id_seq"'::regclass);


--
-- Name: MiddlewareVersionInfo2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."MiddlewareVersionInfo2" ALTER COLUMN id SET DEFAULT nextval('public."MiddlewareVersionInfo2_id_seq"'::regclass);


--
-- Name: OServiceTracking2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."OServiceTracking2" ALTER COLUMN id SET DEFAULT nextval('public."OServiceTracking2_id_seq"'::regclass);


--
-- Name: Rules2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."Rules2" ALTER COLUMN id SET DEFAULT nextval('public."Rules2_id_seq"'::regclass);


--
-- Name: Trips2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."Trips2" ALTER COLUMN id SET DEFAULT nextval('public."Trips2_id_seq"'::regclass);


--
-- Name: ZoneTypes2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."ZoneTypes2" ALTER COLUMN id SET DEFAULT nextval('public."ZoneTypes2_id_seq"'::regclass);


--
-- Name: stg_Diagnostics2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."stg_Diagnostics2" ALTER COLUMN id SET DEFAULT nextval('public."stg_Diagnostics2_id_seq"'::regclass);


--
-- Name: stg_Groups2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."stg_Groups2" ALTER COLUMN id SET DEFAULT nextval('public."stg_Groups2_id_seq"'::regclass);


--
-- Name: stg_Rules2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."stg_Rules2" ALTER COLUMN id SET DEFAULT nextval('public."stg_Rules2_id_seq"'::regclass);


--
-- Name: stg_Trips2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."stg_Trips2" ALTER COLUMN id SET DEFAULT nextval('public."stg_Trips2_id_seq"'::regclass);


--
-- Name: stg_ZoneTypes2 id; Type: DEFAULT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."stg_ZoneTypes2" ALTER COLUMN id SET DEFAULT nextval('public."stg_ZoneTypes2_id_seq"'::regclass);


--
-- Name: DBMaintenanceLogs2 DBMaintenanceLogs2_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."DBMaintenanceLogs2"
    ADD CONSTRAINT "DBMaintenanceLogs2_pkey" PRIMARY KEY (id);


--
-- Name: OServiceTracking2 OServiceTracking2_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."OServiceTracking2"
    ADD CONSTRAINT "OServiceTracking2_pkey" PRIMARY KEY (id);


--
-- Name: BinaryData2 PK_BinaryData2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."BinaryData2"
    ADD CONSTRAINT "PK_BinaryData2" PRIMARY KEY ("DateTime", id);


--
-- Name: ChargeEvents2 PK_ChargeEvents2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."ChargeEvents2"
    ADD CONSTRAINT "PK_ChargeEvents2" PRIMARY KEY ("StartTime", id);


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
-- Name: DriverChanges2 PK_DriverChanges2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."DriverChanges2"
    ADD CONSTRAINT "PK_DriverChanges2" PRIMARY KEY ("DateTime", id);


--
-- Name: EntityMetadata2 PK_EntityMetadata2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."EntityMetadata2"
    ADD CONSTRAINT "PK_EntityMetadata2" PRIMARY KEY ("DateTime", id);


--
-- Name: ExceptionEvents2 PK_ExceptionEvents2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."ExceptionEvents2"
    ADD CONSTRAINT "PK_ExceptionEvents2" PRIMARY KEY ("ActiveFrom", id);


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
-- Name: Groups2 PK_Groups2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."Groups2"
    ADD CONSTRAINT "PK_Groups2" PRIMARY KEY (id);


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
-- Name: Rules2 PK_Rules2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."Rules2"
    ADD CONSTRAINT "PK_Rules2" PRIMARY KEY (id);


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
-- Name: Trips2 PK_Trips2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."Trips2"
    ADD CONSTRAINT "PK_Trips2" PRIMARY KEY ("Start", id);


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
-- Name: stg_Diagnostics2 PK_stg_Diagnostics2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."stg_Diagnostics2"
    ADD CONSTRAINT "PK_stg_Diagnostics2" PRIMARY KEY (id);


--
-- Name: stg_Groups2 PK_stg_Groups2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."stg_Groups2"
    ADD CONSTRAINT "PK_stg_Groups2" PRIMARY KEY (id);


--
-- Name: stg_Rules2 PK_stg_Rules2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."stg_Rules2"
    ADD CONSTRAINT "PK_stg_Rules2" PRIMARY KEY (id);


--
-- Name: stg_Trips2 PK_stg_Trips2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."stg_Trips2"
    ADD CONSTRAINT "PK_stg_Trips2" PRIMARY KEY (id);


--
-- Name: stg_ZoneTypes2 PK_stg_ZoneTypes2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."stg_ZoneTypes2"
    ADD CONSTRAINT "PK_stg_ZoneTypes2" PRIMARY KEY (id);


--
-- Name: DiagnosticIds2 UK_DiagnosticIds2; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."DiagnosticIds2"
    ADD CONSTRAINT "UK_DiagnosticIds2" UNIQUE ("GeotabGUIDString", "GeotabId");


--
-- Name: Groups2 UK_Groups2_GeotabId; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."Groups2"
    ADD CONSTRAINT "UK_Groups2_GeotabId" UNIQUE ("GeotabId");


--
-- Name: Rules2 UK_Rules2_GeotabId; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."Rules2"
    ADD CONSTRAINT "UK_Rules2_GeotabId" UNIQUE ("GeotabId");


--
-- Name: Trips2 UK_Trips2_DeviceId_Start_EntityStatus; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."Trips2"
    ADD CONSTRAINT "UK_Trips2_DeviceId_Start_EntityStatus" UNIQUE ("DeviceId", "Start", "EntityStatus");


--
-- Name: ZoneTypes2 UK_ZoneTypes2_GeotabId; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE ONLY public."ZoneTypes2"
    ADD CONSTRAINT "UK_ZoneTypes2_GeotabId" UNIQUE ("GeotabId");


--
-- Name: CI_Trips2_Start_Id; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "CI_Trips2_Start_Id" ON ONLY public."Trips2" USING btree ("Start", id);


--
-- Name: IX_ChargeEvents2_DeviceId; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_ChargeEvents2_DeviceId" ON ONLY public."ChargeEvents2" USING btree ("DeviceId");


--
-- Name: IX_ChargeEvents2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_ChargeEvents2_RecordLastChangedUtc" ON ONLY public."ChargeEvents2" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_ChargeEvents2_StartTime_DeviceId; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_ChargeEvents2_StartTime_DeviceId" ON ONLY public."ChargeEvents2" USING btree ("StartTime", "DeviceId");


--
-- Name: IX_DBMaintenanceLogs2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_DBMaintenanceLogs2_RecordLastChangedUtc" ON public."DBMaintenanceLogs2" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_Devices2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_Devices2_RecordLastChangedUtc" ON public."Devices2" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_Diagnostics2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_Diagnostics2_RecordLastChangedUtc" ON public."Diagnostics2" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_DriverChanges2_DateTime_Device_Type; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_DriverChanges2_DateTime_Device_Type" ON ONLY public."DriverChanges2" USING btree ("DateTime", "DeviceId", "Type");


--
-- Name: IX_DriverChanges2_DateTime_Driver_Type; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_DriverChanges2_DateTime_Driver_Type" ON ONLY public."DriverChanges2" USING btree ("DateTime", "DriverId", "Type");


--
-- Name: IX_DriverChanges2_DeviceId; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_DriverChanges2_DeviceId" ON ONLY public."DriverChanges2" USING btree ("DeviceId");


--
-- Name: IX_DriverChanges2_DriverId; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_DriverChanges2_DriverId" ON ONLY public."DriverChanges2" USING btree ("DriverId");


--
-- Name: IX_DriverChanges2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_DriverChanges2_RecordLastChangedUtc" ON ONLY public."DriverChanges2" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_DriverChanges2_Type; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_DriverChanges2_Type" ON ONLY public."DriverChanges2" USING btree ("Type");


--
-- Name: IX_EntityMetadata2_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_EntityMetadata2_DateTime" ON ONLY public."EntityMetadata2" USING btree ("DateTime");


--
-- Name: IX_EntityMetadata2_DeviceId_DateTime_EntityType; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_EntityMetadata2_DeviceId_DateTime_EntityType" ON ONLY public."EntityMetadata2" USING btree ("DeviceId", "DateTime", "EntityType") WITH (deduplicate_items='true');


--
-- Name: IX_ExceptionEvents2_DeviceId; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_ExceptionEvents2_DeviceId" ON ONLY public."ExceptionEvents2" USING btree ("DeviceId");


--
-- Name: IX_ExceptionEvents2_DriverId; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_ExceptionEvents2_DriverId" ON ONLY public."ExceptionEvents2" USING btree ("DriverId");


--
-- Name: IX_ExceptionEvents2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_ExceptionEvents2_RecordLastChangedUtc" ON ONLY public."ExceptionEvents2" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_ExceptionEvents2_RuleId; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_ExceptionEvents2_RuleId" ON ONLY public."ExceptionEvents2" USING btree ("RuleId");


--
-- Name: IX_ExceptionEvents2_State; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_ExceptionEvents2_State" ON ONLY public."ExceptionEvents2" USING btree ("State");


--
-- Name: IX_ExceptionEvents2_TimeRange; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_ExceptionEvents2_TimeRange" ON ONLY public."ExceptionEvents2" USING btree ("ActiveFrom", "ActiveTo");


--
-- Name: IX_ExceptionEvents2_TimeRange_Driver_State; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_ExceptionEvents2_TimeRange_Driver_State" ON ONLY public."ExceptionEvents2" USING btree ("ActiveFrom", "ActiveTo", "DriverId", "State");


--
-- Name: IX_ExceptionEvents2_TimeRange_Rule_Driver_State; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_ExceptionEvents2_TimeRange_Rule_Driver_State" ON ONLY public."ExceptionEvents2" USING btree ("ActiveFrom", "ActiveTo", "RuleId", "DriverId", "State");


--
-- Name: IX_ExceptionEvents2_TimeRange_Rule_State; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_ExceptionEvents2_TimeRange_Rule_State" ON ONLY public."ExceptionEvents2" USING btree ("ActiveFrom", "ActiveTo", "RuleId", "State");


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
-- Name: IX_Groups2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_Groups2_RecordLastChangedUtc" ON public."Groups2" USING btree ("RecordLastChangedUtc");


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
-- Name: IX_Rules2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_Rules2_RecordLastChangedUtc" ON public."Rules2" USING btree ("RecordLastChangedUtc");


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
-- Name: IX_Trips2_NextTripStart; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_Trips2_NextTripStart" ON ONLY public."Trips2" USING btree ("NextTripStart");


--
-- Name: IX_Trips2_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_Trips2_RecordLastChangedUtc" ON ONLY public."Trips2" USING btree ("RecordLastChangedUtc");


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
-- Name: IX_stg_ChargeEvents2_id_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_stg_ChargeEvents2_id_RecordLastChangedUtc" ON public."stg_ChargeEvents2" USING btree (id, "RecordLastChangedUtc");


--
-- Name: IX_stg_Devices2_id_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_stg_Devices2_id_RecordLastChangedUtc" ON public."stg_Devices2" USING btree (id, "RecordLastChangedUtc" DESC);


--
-- Name: IX_stg_Diagnostics2_GeotabGUIDString_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_stg_Diagnostics2_GeotabGUIDString_RecordLastChangedUtc" ON public."stg_Diagnostics2" USING btree ("GeotabGUIDString", "RecordLastChangedUtc" DESC);


--
-- Name: IX_stg_DriverChanges2_id_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_stg_DriverChanges2_id_RecordLastChangedUtc" ON public."stg_DriverChanges2" USING btree (id, "RecordLastChangedUtc");


--
-- Name: IX_stg_ExceptionEvents2_id_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_stg_ExceptionEvents2_id_RecordLastChangedUtc" ON public."stg_ExceptionEvents2" USING btree (id, "RecordLastChangedUtc");


--
-- Name: IX_stg_Groups2_GeotabId_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_stg_Groups2_GeotabId_RecordLastChangedUtc" ON public."stg_Groups2" USING btree ("GeotabId", "RecordLastChangedUtc" DESC);


--
-- Name: IX_stg_Rules2_GeotabId_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_stg_Rules2_GeotabId_RecordLastChangedUtc" ON public."stg_Rules2" USING btree ("GeotabId", "RecordLastChangedUtc" DESC);


--
-- Name: IX_stg_Trips2_DeviceId_Start_EntityStatus; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_stg_Trips2_DeviceId_Start_EntityStatus" ON public."stg_Trips2" USING btree ("DeviceId", "Start", "EntityStatus");


--
-- Name: IX_stg_Users2_id_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_stg_Users2_id_RecordLastChangedUtc" ON public."stg_Users2" USING btree (id, "RecordLastChangedUtc" DESC);


--
-- Name: IX_stg_ZoneTypes2_GeotabId_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_stg_ZoneTypes2_GeotabId_RecordLastChangedUtc" ON public."stg_ZoneTypes2" USING btree ("GeotabId", "RecordLastChangedUtc" DESC);


--
-- Name: IX_stg_Zones2_id_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_client
--

CREATE INDEX "IX_stg_Zones2_id_RecordLastChangedUtc" ON public."stg_Zones2" USING btree (id, "RecordLastChangedUtc" DESC);


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
-- Name: BinaryData2 FK_BinaryData2_Devices2; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE public."BinaryData2"
    ADD CONSTRAINT "FK_BinaryData2_Devices2" FOREIGN KEY ("DeviceId") REFERENCES public."Devices2"(id);


--
-- Name: ChargeEvents2 FK_ChargeEvents2_Devices2; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE public."ChargeEvents2"
    ADD CONSTRAINT "FK_ChargeEvents2_Devices2" FOREIGN KEY ("DeviceId") REFERENCES public."Devices2"(id);


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
-- Name: DriverChanges2 FK_DriverChanges2_Devices2; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE public."DriverChanges2"
    ADD CONSTRAINT "FK_DriverChanges2_Devices2" FOREIGN KEY ("DeviceId") REFERENCES public."Devices2"(id);


--
-- Name: DriverChanges2 FK_DriverChanges2_Users2; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE public."DriverChanges2"
    ADD CONSTRAINT "FK_DriverChanges2_Users2" FOREIGN KEY ("DriverId") REFERENCES public."Users2"(id);


--
-- Name: ExceptionEvents2 FK_ExceptionEvents2_Devices2; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE public."ExceptionEvents2"
    ADD CONSTRAINT "FK_ExceptionEvents2_Devices2" FOREIGN KEY ("DeviceId") REFERENCES public."Devices2"(id);


--
-- Name: ExceptionEvents2 FK_ExceptionEvents2_Rules2; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE public."ExceptionEvents2"
    ADD CONSTRAINT "FK_ExceptionEvents2_Rules2" FOREIGN KEY ("RuleId") REFERENCES public."Rules2"(id);


--
-- Name: ExceptionEvents2 FK_ExceptionEvents2_Users2; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE public."ExceptionEvents2"
    ADD CONSTRAINT "FK_ExceptionEvents2_Users2" FOREIGN KEY ("DriverId") REFERENCES public."Users2"(id);


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
-- Name: Trips2 FK_Trips2_Devices2; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE public."Trips2"
    ADD CONSTRAINT "FK_Trips2_Devices2" FOREIGN KEY ("DeviceId") REFERENCES public."Devices2"(id);


--
-- Name: Trips2 FK_Trips2_Users2; Type: FK CONSTRAINT; Schema: public; Owner: geotabadapter_client
--

ALTER TABLE public."Trips2"
    ADD CONSTRAINT "FK_Trips2_Users2" FOREIGN KEY ("DriverId") REFERENCES public."Users2"(id);


--
-- PostgreSQL database dump complete
--
/*** [END] Part 2: pgAdmin-Generated Script (tables, sequences, views) ***/ 



/*** [START] Part 3: pgAdmin-Generated Script (functions) ***/
--
-- Name: spFaultData2WithLagLeadLongLatBatch(integer, integer, integer); Type: FUNCTION; Schema: public; Owner: geotabadapter_client
--

CREATE FUNCTION public."spFaultData2WithLagLeadLongLatBatch"("MaxDaysPerBatch" integer, "MaxBatchSize" integer, "BufferMinutes" integer) RETURNS TABLE(id bigint, "GeotabId" character varying, "FaultDataDateTime" timestamp without time zone, "DeviceId" bigint, "LagDateTime" timestamp without time zone, "LagLatitude" double precision, "LagLongitude" double precision, "LagSpeed" real, "LeadDateTime" timestamp without time zone, "LeadLatitude" double precision, "LeadLongitude" double precision, "LogRecords2MinDateTime" timestamp without time zone, "LogRecords2MaxDateTime" timestamp without time zone, "DeviceLogRecords2MinDateTime" timestamp without time zone, "DeviceLogRecords2MaxDateTime" timestamp without time zone)
    LANGUAGE plpgsql
    AS $$
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
$$;


ALTER FUNCTION public."spFaultData2WithLagLeadLongLatBatch"("MaxDaysPerBatch" integer, "MaxBatchSize" integer, "BufferMinutes" integer) OWNER TO geotabadapter_client;

--
-- Name: spMerge_stg_ChargeEvents2(); Type: FUNCTION; Schema: public; Owner: geotabadapter_client
--

CREATE FUNCTION public."spMerge_stg_ChargeEvents2"() RETURNS void
    LANGUAGE plpgsql
    AS $$
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
$$;


ALTER FUNCTION public."spMerge_stg_ChargeEvents2"() OWNER TO geotabadapter_client;

--
-- Name: spMerge_stg_Devices2(boolean); Type: FUNCTION; Schema: public; Owner: geotabadapter_client
--

CREATE FUNCTION public."spMerge_stg_Devices2"("SetEntityStatusDeletedForMissingDevices" boolean DEFAULT false) RETURNS void
    LANGUAGE plpgsql
    AS $$
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
$$;


ALTER FUNCTION public."spMerge_stg_Devices2"("SetEntityStatusDeletedForMissingDevices" boolean) OWNER TO geotabadapter_client;

--
-- Name: spMerge_stg_Diagnostics2(boolean); Type: FUNCTION; Schema: public; Owner: geotabadapter_client
--

CREATE FUNCTION public."spMerge_stg_Diagnostics2"("SetEntityStatusDeletedForMissingDiagnostics" boolean DEFAULT false) RETURNS void
    LANGUAGE plpgsql
    AS $$
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
$$;


ALTER FUNCTION public."spMerge_stg_Diagnostics2"("SetEntityStatusDeletedForMissingDiagnostics" boolean) OWNER TO geotabadapter_client;

--
-- Name: spMerge_stg_DriverChanges2(); Type: FUNCTION; Schema: public; Owner: geotabadapter_client
--

CREATE FUNCTION public."spMerge_stg_DriverChanges2"() RETURNS void
    LANGUAGE plpgsql
    AS $$
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
$$;


ALTER FUNCTION public."spMerge_stg_DriverChanges2"() OWNER TO geotabadapter_client;

--
-- Name: spMerge_stg_ExceptionEvents2(); Type: FUNCTION; Schema: public; Owner: geotabadapter_client
--

CREATE FUNCTION public."spMerge_stg_ExceptionEvents2"() RETURNS void
    LANGUAGE plpgsql
    AS $$
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
$$;


ALTER FUNCTION public."spMerge_stg_ExceptionEvents2"() OWNER TO geotabadapter_client;

--
-- Name: spMerge_stg_Groups2(boolean); Type: FUNCTION; Schema: public; Owner: geotabadapter_client
--

CREATE FUNCTION public."spMerge_stg_Groups2"("SetEntityStatusDeletedForMissingGroups" boolean DEFAULT false) RETURNS void
    LANGUAGE plpgsql
    AS $$
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
$$;


ALTER FUNCTION public."spMerge_stg_Groups2"("SetEntityStatusDeletedForMissingGroups" boolean) OWNER TO geotabadapter_client;

--
-- Name: spMerge_stg_Rules2(boolean); Type: FUNCTION; Schema: public; Owner: geotabadapter_client
--

CREATE FUNCTION public."spMerge_stg_Rules2"("SetEntityStatusDeletedForMissingRules" boolean DEFAULT false) RETURNS void
    LANGUAGE plpgsql
    AS $$
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
$$;


ALTER FUNCTION public."spMerge_stg_Rules2"("SetEntityStatusDeletedForMissingRules" boolean) OWNER TO geotabadapter_client;

--
-- Name: spMerge_stg_Trips2(); Type: FUNCTION; Schema: public; Owner: geotabadapter_client
--

CREATE FUNCTION public."spMerge_stg_Trips2"() RETURNS void
    LANGUAGE plpgsql
    AS $$
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
$$;


ALTER FUNCTION public."spMerge_stg_Trips2"() OWNER TO geotabadapter_client;

--
-- Name: spMerge_stg_Users2(boolean); Type: FUNCTION; Schema: public; Owner: geotabadapter_client
--

CREATE FUNCTION public."spMerge_stg_Users2"("SetEntityStatusDeletedForMissingUsers" boolean DEFAULT false) RETURNS void
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
$$;


ALTER FUNCTION public."spMerge_stg_Users2"("SetEntityStatusDeletedForMissingUsers" boolean) OWNER TO geotabadapter_client;

--
-- Name: spMerge_stg_ZoneTypes2(boolean); Type: FUNCTION; Schema: public; Owner: geotabadapter_client
--

CREATE FUNCTION public."spMerge_stg_ZoneTypes2"("SetEntityStatusDeletedForMissingZoneTypes" boolean DEFAULT false) RETURNS void
    LANGUAGE plpgsql
    AS $$
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
$$;


ALTER FUNCTION public."spMerge_stg_ZoneTypes2"("SetEntityStatusDeletedForMissingZoneTypes" boolean) OWNER TO geotabadapter_client;

--
-- Name: spMerge_stg_Zones2(boolean); Type: FUNCTION; Schema: public; Owner: geotabadapter_client
--

CREATE FUNCTION public."spMerge_stg_Zones2"("SetEntityStatusDeletedForMissingZones" boolean DEFAULT false) RETURNS void
    LANGUAGE plpgsql
    AS $$
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
$$;


ALTER FUNCTION public."spMerge_stg_Zones2"("SetEntityStatusDeletedForMissingZones" boolean) OWNER TO geotabadapter_client;

--
-- Name: spStatusData2WithLagLeadLongLatBatch(integer, integer, integer); Type: FUNCTION; Schema: public; Owner: geotabadapter_client
--

CREATE FUNCTION public."spStatusData2WithLagLeadLongLatBatch"("MaxDaysPerBatch" integer, "MaxBatchSize" integer, "BufferMinutes" integer) RETURNS TABLE(id bigint, "GeotabId" character varying, "StatusDataDateTime" timestamp without time zone, "DeviceId" bigint, "LagDateTime" timestamp without time zone, "LagLatitude" double precision, "LagLongitude" double precision, "LagSpeed" real, "LeadDateTime" timestamp without time zone, "LeadLatitude" double precision, "LeadLongitude" double precision, "LogRecords2MinDateTime" timestamp without time zone, "LogRecords2MaxDateTime" timestamp without time zone, "DeviceLogRecords2MinDateTime" timestamp without time zone, "DeviceLogRecords2MaxDateTime" timestamp without time zone)
    LANGUAGE plpgsql ROWS 100000
    AS $$
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
$$;


ALTER FUNCTION public."spStatusData2WithLagLeadLongLatBatch"("MaxDaysPerBatch" integer, "MaxBatchSize" integer, "BufferMinutes" integer) OWNER TO geotabadapter_client;

--
-- Name: FUNCTION pg_stat_statements(showtext boolean, OUT userid oid, OUT dbid oid, OUT toplevel boolean, OUT queryid bigint, OUT query text, OUT plans bigint, OUT total_plan_time double precision, OUT min_plan_time double precision, OUT max_plan_time double precision, OUT mean_plan_time double precision, OUT stddev_plan_time double precision, OUT calls bigint, OUT total_exec_time double precision, OUT min_exec_time double precision, OUT max_exec_time double precision, OUT mean_exec_time double precision, OUT stddev_exec_time double precision, OUT rows bigint, OUT shared_blks_hit bigint, OUT shared_blks_read bigint, OUT shared_blks_dirtied bigint, OUT shared_blks_written bigint, OUT local_blks_hit bigint, OUT local_blks_read bigint, OUT local_blks_dirtied bigint, OUT local_blks_written bigint, OUT temp_blks_read bigint, OUT temp_blks_written bigint, OUT blk_read_time double precision, OUT blk_write_time double precision, OUT temp_blk_read_time double precision, OUT temp_blk_write_time double precision, OUT wal_records bigint, OUT wal_fpi bigint, OUT wal_bytes numeric, OUT jit_functions bigint, OUT jit_generation_time double precision, OUT jit_inlining_count bigint, OUT jit_inlining_time double precision, OUT jit_optimization_count bigint, OUT jit_optimization_time double precision, OUT jit_emission_count bigint, OUT jit_emission_time double precision); Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON FUNCTION public.pg_stat_statements(showtext boolean, OUT userid oid, OUT dbid oid, OUT toplevel boolean, OUT queryid bigint, OUT query text, OUT plans bigint, OUT total_plan_time double precision, OUT min_plan_time double precision, OUT max_plan_time double precision, OUT mean_plan_time double precision, OUT stddev_plan_time double precision, OUT calls bigint, OUT total_exec_time double precision, OUT min_exec_time double precision, OUT max_exec_time double precision, OUT mean_exec_time double precision, OUT stddev_exec_time double precision, OUT rows bigint, OUT shared_blks_hit bigint, OUT shared_blks_read bigint, OUT shared_blks_dirtied bigint, OUT shared_blks_written bigint, OUT local_blks_hit bigint, OUT local_blks_read bigint, OUT local_blks_dirtied bigint, OUT local_blks_written bigint, OUT temp_blks_read bigint, OUT temp_blks_written bigint, OUT blk_read_time double precision, OUT blk_write_time double precision, OUT temp_blk_read_time double precision, OUT temp_blk_write_time double precision, OUT wal_records bigint, OUT wal_fpi bigint, OUT wal_bytes numeric, OUT jit_functions bigint, OUT jit_generation_time double precision, OUT jit_inlining_count bigint, OUT jit_inlining_time double precision, OUT jit_optimization_count bigint, OUT jit_optimization_time double precision, OUT jit_emission_count bigint, OUT jit_emission_time double precision) TO geotabadapter_client;


--
-- Name: FUNCTION pg_stat_statements_info(OUT dealloc bigint, OUT stats_reset timestamp with time zone); Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON FUNCTION public.pg_stat_statements_info(OUT dealloc bigint, OUT stats_reset timestamp with time zone) TO geotabadapter_client;


--
-- Name: FUNCTION "spFaultData2WithLagLeadLongLatBatch"("MaxDaysPerBatch" integer, "MaxBatchSize" integer, "BufferMinutes" integer); Type: ACL; Schema: public; Owner: geotabadapter_client
--

REVOKE ALL ON FUNCTION public."spFaultData2WithLagLeadLongLatBatch"("MaxDaysPerBatch" integer, "MaxBatchSize" integer, "BufferMinutes" integer) FROM PUBLIC;


--
-- Name: FUNCTION "spMerge_stg_ChargeEvents2"(); Type: ACL; Schema: public; Owner: geotabadapter_client
--

REVOKE ALL ON FUNCTION public."spMerge_stg_ChargeEvents2"() FROM PUBLIC;


--
-- Name: FUNCTION "spMerge_stg_Devices2"("SetEntityStatusDeletedForMissingDevices" boolean); Type: ACL; Schema: public; Owner: geotabadapter_client
--

REVOKE ALL ON FUNCTION public."spMerge_stg_Devices2"("SetEntityStatusDeletedForMissingDevices" boolean) FROM PUBLIC;


--
-- Name: FUNCTION "spMerge_stg_Diagnostics2"("SetEntityStatusDeletedForMissingDiagnostics" boolean); Type: ACL; Schema: public; Owner: geotabadapter_client
--

REVOKE ALL ON FUNCTION public."spMerge_stg_Diagnostics2"("SetEntityStatusDeletedForMissingDiagnostics" boolean) FROM PUBLIC;


--
-- Name: FUNCTION "spMerge_stg_DriverChanges2"(); Type: ACL; Schema: public; Owner: geotabadapter_client
--

REVOKE ALL ON FUNCTION public."spMerge_stg_DriverChanges2"() FROM PUBLIC;


--
-- Name: FUNCTION "spMerge_stg_ExceptionEvents2"(); Type: ACL; Schema: public; Owner: geotabadapter_client
--

REVOKE ALL ON FUNCTION public."spMerge_stg_ExceptionEvents2"() FROM PUBLIC;


--
-- Name: FUNCTION "spMerge_stg_Groups2"("SetEntityStatusDeletedForMissingGroups" boolean); Type: ACL; Schema: public; Owner: geotabadapter_client
--

REVOKE ALL ON FUNCTION public."spMerge_stg_Groups2"("SetEntityStatusDeletedForMissingGroups" boolean) FROM PUBLIC;


--
-- Name: FUNCTION "spMerge_stg_Rules2"("SetEntityStatusDeletedForMissingRules" boolean); Type: ACL; Schema: public; Owner: geotabadapter_client
--

REVOKE ALL ON FUNCTION public."spMerge_stg_Rules2"("SetEntityStatusDeletedForMissingRules" boolean) FROM PUBLIC;


--
-- Name: FUNCTION "spMerge_stg_Trips2"(); Type: ACL; Schema: public; Owner: geotabadapter_client
--

REVOKE ALL ON FUNCTION public."spMerge_stg_Trips2"() FROM PUBLIC;


--
-- Name: FUNCTION "spMerge_stg_Users2"("SetEntityStatusDeletedForMissingUsers" boolean); Type: ACL; Schema: public; Owner: geotabadapter_client
--

REVOKE ALL ON FUNCTION public."spMerge_stg_Users2"("SetEntityStatusDeletedForMissingUsers" boolean) FROM PUBLIC;


--
-- Name: FUNCTION "spMerge_stg_ZoneTypes2"("SetEntityStatusDeletedForMissingZoneTypes" boolean); Type: ACL; Schema: public; Owner: geotabadapter_client
--

REVOKE ALL ON FUNCTION public."spMerge_stg_ZoneTypes2"("SetEntityStatusDeletedForMissingZoneTypes" boolean) FROM PUBLIC;


--
-- Name: FUNCTION "spMerge_stg_Zones2"("SetEntityStatusDeletedForMissingZones" boolean); Type: ACL; Schema: public; Owner: geotabadapter_client
--

REVOKE ALL ON FUNCTION public."spMerge_stg_Zones2"("SetEntityStatusDeletedForMissingZones" boolean) FROM PUBLIC;


--
-- Name: FUNCTION "spStatusData2WithLagLeadLongLatBatch"("MaxDaysPerBatch" integer, "MaxBatchSize" integer, "BufferMinutes" integer); Type: ACL; Schema: public; Owner: geotabadapter_client
--

REVOKE ALL ON FUNCTION public."spStatusData2WithLagLeadLongLatBatch"("MaxDaysPerBatch" integer, "MaxBatchSize" integer, "BufferMinutes" integer) FROM PUBLIC;


--
-- Name: DEFAULT PRIVILEGES FOR SEQUENCES; Type: DEFAULT ACL; Schema: public; Owner: postgres
--

ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public GRANT ALL ON SEQUENCES TO geotabadapter_client;


--
-- Name: DEFAULT PRIVILEGES FOR FUNCTIONS; Type: DEFAULT ACL; Schema: public; Owner: postgres
--

ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public GRANT ALL ON FUNCTIONS TO geotabadapter_client;
/*** [END] Part 3: pgAdmin-Generated Script (functions) ***/ 



/*** [START] Version 3.7.0.0 Updates ***/
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create DeviceStatusInfo2 table:
CREATE TABLE public."DeviceStatusInfo2" (
    "id" bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "Bearing" double precision NOT NULL,
    "CurrentStateDuration" character varying(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DriverId" bigint,
    "IsDeviceCommunicating" boolean NOT NULL,
    "IsDriving" boolean NOT NULL,
    "IsHistoricLastDriver" boolean NOT NULL,
    "Latitude" double precision NOT NULL,
    "Longitude" double precision NOT NULL,
    "Speed" real NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_DeviceStatusInfo2" PRIMARY KEY ("id")
);

ALTER TABLE public."DeviceStatusInfo2" OWNER TO geotabadapter_client;

-- Indexes
CREATE INDEX "IX_DeviceStatusInfo2_DeviceId" ON public."DeviceStatusInfo2" USING btree ("DeviceId");

CREATE INDEX "IX_DeviceStatusInfo2_DriverId" ON public."DeviceStatusInfo2" USING btree ("DriverId");

-- Foreign Key Constraints
ALTER TABLE public."DeviceStatusInfo2"
    ADD CONSTRAINT "FK_DeviceStatusInfo2_Devices2" FOREIGN KEY ("DeviceId")
        REFERENCES public."Devices2" ("id");

ALTER TABLE public."DeviceStatusInfo2"
    ADD CONSTRAINT "FK_DeviceStatusInfo2_Users2" FOREIGN KEY ("DriverId")
        REFERENCES public."Users2" ("id");


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_DeviceStatusInfo2 table:
CREATE TABLE public."stg_DeviceStatusInfo2" (
    "id" bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "Bearing" double precision NOT NULL,
    "CurrentStateDuration" character varying(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" bigint NOT NULL,
    "DriverId" bigint,
    "IsDeviceCommunicating" boolean NOT NULL,
    "IsDriving" boolean NOT NULL,
    "IsHistoricLastDriver" boolean NOT NULL,
    "Latitude" double precision NOT NULL,
    "Longitude" double precision NOT NULL,
    "Speed" real NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

ALTER TABLE public."stg_DeviceStatusInfo2" OWNER TO geotabadapter_client;

-- Index
CREATE INDEX "IX_stg_DeviceStatusInfo2_id_RecordLastChangedUtc" ON public."stg_DeviceStatusInfo2" USING btree ("id" ASC, "RecordLastChangedUtc" ASC);


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_DeviceStatusInfo2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_DeviceStatusInfo2"(
)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_DeviceStatusInfo2 staging table to the DeviceStatusInfo2
--   table and then truncates the staging table.
--
-- Notes:
--   - No transaction used as application should manage the transaction.
-- ==========================================================================================
BEGIN
    -- De-duplicate staging table by selecting the latest record per "id".
	-- Uses DISTINCT ON to keep only the latest record per "id" based on "RecordLastChangedUtc".
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("id") *
    FROM public."stg_DeviceStatusInfo2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;

    -- Add an index to the temporary table on the column used for conflict resolution.
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id");

    -- Perform upsert.
    INSERT INTO public."DeviceStatusInfo2" AS d (
        "id",
        "GeotabId",
        "Bearing",
        "CurrentStateDuration",
        "DateTime",
        "DeviceId",
        "DriverId",
        "IsDeviceCommunicating",
        "IsDriving",
        "IsHistoricLastDriver",
        "Latitude",
        "Longitude",
        "Speed",
        "RecordLastChangedUtc"
    )
    SELECT
        s."id",
        s."GeotabId",
        s."Bearing",
        s."CurrentStateDuration",
        s."DateTime",
        s."DeviceId",
        s."DriverId",
        s."IsDeviceCommunicating",
        s."IsDriving",
        s."IsHistoricLastDriver",
        s."Latitude",
        s."Longitude",
        s."Speed",
        s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("id")
    DO UPDATE SET
		-- "id" excluded since it is subject of ON CONFLICT.
        "GeotabId" = EXCLUDED."GeotabId",
        "Bearing" = EXCLUDED."Bearing",
        "CurrentStateDuration" = EXCLUDED."CurrentStateDuration",
        "DateTime" = EXCLUDED."DateTime",
        "DeviceId" = EXCLUDED."DeviceId",
        "DriverId" = EXCLUDED."DriverId",
        "IsDeviceCommunicating" = EXCLUDED."IsDeviceCommunicating",
        "IsDriving" = EXCLUDED."IsDriving",
        "IsHistoricLastDriver" = EXCLUDED."IsHistoricLastDriver",
        "Latitude" = EXCLUDED."Latitude",
        "Longitude" = EXCLUDED."Longitude",
        "Speed" = EXCLUDED."Speed",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId" OR
        d."Bearing" IS DISTINCT FROM EXCLUDED."Bearing" OR
        d."CurrentStateDuration" IS DISTINCT FROM EXCLUDED."CurrentStateDuration" OR
        d."DateTime" IS DISTINCT FROM EXCLUDED."DateTime" OR
        d."DeviceId" IS DISTINCT FROM EXCLUDED."DeviceId" OR
        d."DriverId" IS DISTINCT FROM EXCLUDED."DriverId" OR
        d."IsDeviceCommunicating" IS DISTINCT FROM EXCLUDED."IsDeviceCommunicating" OR
        d."IsDriving" IS DISTINCT FROM EXCLUDED."IsDriving" OR
        d."IsHistoricLastDriver" IS DISTINCT FROM EXCLUDED."IsHistoricLastDriver" OR
        d."Latitude" IS DISTINCT FROM EXCLUDED."Latitude" OR
        d."Longitude" IS DISTINCT FROM EXCLUDED."Longitude" OR
        d."Speed" IS DISTINCT FROM EXCLUDED."Speed";
        -- RecordLastChangedUtc not evaluated as it should never match.

    -- Clear staging table.
    TRUNCATE TABLE public."stg_DeviceStatusInfo2";

    -- Drop temporary table.
    DROP TABLE "TMP_DeduplicatedStaging";

END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_DeviceStatusInfo2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_DeviceStatusInfo2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_DeviceStatusInfo2"() FROM PUBLIC;
/*** [END] Version 3.7.0.0 Updates ***/



/*** [START] Version 3.8.0.0 Updates ***/
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
/*** [END] Version 3.8.0.0 Updates ***/



/*** [START] Version 3.9.0.0 Updates ***/
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create DutyStatusAvailabilities2 table:
CREATE TABLE public."DutyStatusAvailabilities2" (
    "id" bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DriverId" bigint NOT NULL,
    "CycleAvailabilities" text,
    "CycleDrivingTicks" bigint,
    "CycleTicks" bigint,
    "CycleRestTicks" bigint,
    "DrivingBreakDurationTicks" bigint,
    "DrivingTicks" bigint,
    "DutyTicks" bigint,
    "DutySinceCycleRestTicks" bigint,
    "Is16HourExemptionAvailable" boolean,
    "IsAdverseDrivingApplied" boolean,
    "IsAdverseDrivingExemptionAvailable" boolean,
    "IsOffDutyDeferralExemptionAvailable" boolean,
    "IsRailroadExemptionAvailable" boolean,
    "Recap" text,
    "RestTicks" bigint,
    "WorkdayTicks" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_DutyStatusAvailabilities2" PRIMARY KEY ("id")
);

ALTER TABLE IF EXISTS public."DutyStatusAvailabilities2"
    OWNER to geotabadapter_client;

CREATE INDEX "IX_DutyStatusAvailabilities2_DriverId" ON public."DutyStatusAvailabilities2" ("DriverId");

ALTER TABLE public."DutyStatusAvailabilities2"
	ADD CONSTRAINT "FK_DutyStatusAvailabilities2_Users2" FOREIGN KEY ("DriverId")
	REFERENCES public."Users2" (id);

GRANT ALL ON TABLE public."DutyStatusAvailabilities2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_DutyStatusAvailabilities2 table:
CREATE TABLE public."stg_DutyStatusAvailabilities2" (
    "id" bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DriverId" bigint NOT NULL,
    "CycleAvailabilities" text,
    "CycleDrivingTicks" bigint,
    "CycleTicks" bigint,
    "CycleRestTicks" bigint,
    "DrivingBreakDurationTicks" bigint,
    "DrivingTicks" bigint,
    "DutyTicks" bigint,
    "DutySinceCycleRestTicks" bigint,
    "Is16HourExemptionAvailable" boolean,
    "IsAdverseDrivingApplied" boolean,
    "IsAdverseDrivingExemptionAvailable" boolean,
    "IsOffDutyDeferralExemptionAvailable" boolean,
    "IsRailroadExemptionAvailable" boolean,
    "Recap" text,
    "RestTicks" bigint,
    "WorkdayTicks" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);

ALTER TABLE IF EXISTS public."stg_DutyStatusAvailabilities2"
    OWNER to geotabadapter_client;

CREATE INDEX "IX_stg_DutyStatusAvailabilities2_id_RecordLastChangedUtc" ON public."stg_DutyStatusAvailabilities2" ("id" ASC, "RecordLastChangedUtc" ASC);

GRANT ALL ON TABLE public."stg_DutyStatusAvailabilities2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_DutyStatusAvailabilities2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_DutyStatusAvailabilities2"()
    RETURNS void
    LANGUAGE plpgsql
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_DutyStatusAvailabilities2 staging table to the
--   DutyStatusAvailabilities2 table and then truncates the staging table.
--
-- Notes:
--   - No transaction used as application should manage the transaction.
-- ==========================================================================================
BEGIN
    -- De-duplicate staging table by selecting the latest record per "id".
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("id") *
    FROM public."stg_DutyStatusAvailabilities2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id");

    -- Perform upsert.
    INSERT INTO public."DutyStatusAvailabilities2" AS d (
        "id", "GeotabId", "DriverId", "CycleAvailabilities", "CycleDrivingTicks",
        "CycleTicks", "CycleRestTicks", "DrivingBreakDurationTicks", "DrivingTicks",
        "DutyTicks", "DutySinceCycleRestTicks", "Is16HourExemptionAvailable",
        "IsAdverseDrivingApplied", "IsAdverseDrivingExemptionAvailable",
        "IsOffDutyDeferralExemptionAvailable", "IsRailroadExemptionAvailable",
        "Recap", "RestTicks", "WorkdayTicks", "RecordLastChangedUtc"
    )
    SELECT
        s."id", s."GeotabId", s."DriverId", s."CycleAvailabilities", s."CycleDrivingTicks",
        s."CycleTicks", s."CycleRestTicks", s."DrivingBreakDurationTicks", s."DrivingTicks",
        s."DutyTicks", s."DutySinceCycleRestTicks", s."Is16HourExemptionAvailable",
        s."IsAdverseDrivingApplied", s."IsAdverseDrivingExemptionAvailable",
        s."IsOffDutyDeferralExemptionAvailable", s."IsRailroadExemptionAvailable",
        s."Recap", s."RestTicks", s."WorkdayTicks", s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("id")
    DO UPDATE SET
		-- "id" excluded since it is subject of ON CONFLICT.
        "GeotabId" = EXCLUDED."GeotabId",
        "DriverId" = EXCLUDED."DriverId",
        "CycleAvailabilities" = EXCLUDED."CycleAvailabilities",
        "CycleDrivingTicks" = EXCLUDED."CycleDrivingTicks",
        "CycleTicks" = EXCLUDED."CycleTicks",
        "CycleRestTicks" = EXCLUDED."CycleRestTicks",
        "DrivingBreakDurationTicks" = EXCLUDED."DrivingBreakDurationTicks",
        "DrivingTicks" = EXCLUDED."DrivingTicks",
        "DutyTicks" = EXCLUDED."DutyTicks",
        "DutySinceCycleRestTicks" = EXCLUDED."DutySinceCycleRestTicks",
        "Is16HourExemptionAvailable" = EXCLUDED."Is16HourExemptionAvailable",
        "IsAdverseDrivingApplied" = EXCLUDED."IsAdverseDrivingApplied",
        "IsAdverseDrivingExemptionAvailable" = EXCLUDED."IsAdverseDrivingExemptionAvailable",
        "IsOffDutyDeferralExemptionAvailable" = EXCLUDED."IsOffDutyDeferralExemptionAvailable",
        "IsRailroadExemptionAvailable" = EXCLUDED."IsRailroadExemptionAvailable",
        "Recap" = EXCLUDED."Recap",
        "RestTicks" = EXCLUDED."RestTicks",
        "WorkdayTicks" = EXCLUDED."WorkdayTicks",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc"
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId" OR
        d."DriverId" IS DISTINCT FROM EXCLUDED."DriverId" OR
        d."CycleAvailabilities" IS DISTINCT FROM EXCLUDED."CycleAvailabilities" OR
        d."CycleDrivingTicks" IS DISTINCT FROM EXCLUDED."CycleDrivingTicks" OR
        d."CycleTicks" IS DISTINCT FROM EXCLUDED."CycleTicks" OR
        d."CycleRestTicks" IS DISTINCT FROM EXCLUDED."CycleRestTicks" OR
        d."DrivingBreakDurationTicks" IS DISTINCT FROM EXCLUDED."DrivingBreakDurationTicks" OR
        d."DrivingTicks" IS DISTINCT FROM EXCLUDED."DrivingTicks" OR
        d."DutyTicks" IS DISTINCT FROM EXCLUDED."DutyTicks" OR
        d."DutySinceCycleRestTicks" IS DISTINCT FROM EXCLUDED."DutySinceCycleRestTicks" OR
        d."Is16HourExemptionAvailable" IS DISTINCT FROM EXCLUDED."Is16HourExemptionAvailable" OR
        d."IsAdverseDrivingApplied" IS DISTINCT FROM EXCLUDED."IsAdverseDrivingApplied" OR
        d."IsAdverseDrivingExemptionAvailable" IS DISTINCT FROM EXCLUDED."IsAdverseDrivingExemptionAvailable" OR
        d."IsOffDutyDeferralExemptionAvailable" IS DISTINCT FROM EXCLUDED."IsOffDutyDeferralExemptionAvailable" OR
        d."IsRailroadExemptionAvailable" IS DISTINCT FROM EXCLUDED."IsRailroadExemptionAvailable" OR
        d."Recap" IS DISTINCT FROM EXCLUDED."Recap" OR
        d."RestTicks" IS DISTINCT FROM EXCLUDED."RestTicks" OR
        d."WorkdayTicks" IS DISTINCT FROM EXCLUDED."WorkdayTicks";
        -- RecordLastChangedUtc not evaluated as it should never match.

    -- Clear staging table.
    TRUNCATE TABLE public."stg_DutyStatusAvailabilities2";

    -- Clean up temporary table.
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";

EXCEPTION
    WHEN OTHERS THEN
        -- Ensure temporary table cleanup on error.
        DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
        
        -- Re-raise the original error to be caught by the calling application, if necessary.
        RAISE;
END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_DutyStatusAvailabilities2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_DutyStatusAvailabilities2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_DutyStatusAvailabilities2"() FROM PUBLIC;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Adjust length of DiagnosticName column in stg_Diagnostics2 and Diagnostics2 tables:
ALTER TABLE public."stg_Diagnostics2"
ALTER COLUMN "DiagnosticName" TYPE TEXT,
ALTER COLUMN "DiagnosticName" SET NOT NULL;
ALTER TABLE public."Diagnostics2"
ALTER COLUMN "DiagnosticName" TYPE TEXT,
ALTER COLUMN "DiagnosticName" SET NOT NULL;
/*** [END] Version 3.9.0.0 Updates ***/



/*** [START] Version 3.10.0.0 Updates ***/
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Add columns for new properties to FaultData2 table:
ALTER TABLE public."FaultData2"
ADD COLUMN "EffectOnComponent" TEXT;

ALTER TABLE public."FaultData2"
ADD COLUMN "FaultDescription" TEXT;

ALTER TABLE public."FaultData2"
ADD COLUMN "FlashCodeId" character varying(255);

ALTER TABLE public."FaultData2"
ADD COLUMN "FlashCodeName" character varying(255);

ALTER TABLE public."FaultData2"
ADD COLUMN "Recommendation" TEXT;

ALTER TABLE public."FaultData2"
ADD COLUMN "RiskOfBreakdown" double precision;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create FuelAndEnergyUsed2 table:
CREATE TABLE public."FuelAndEnergyUsed2"(
	"id" uuid NOT NULL,
	"GeotabId" character varying(50) NOT NULL,
	"DateTime" timestamp without time zone NOT NULL,
	"DeviceId" bigint NOT NULL,
	"TotalEnergyUsedKwh" double precision NULL,
	"TotalFuelUsed" double precision NULL,
	"TotalIdlingEnergyUsedKwh" double precision NULL,
	"TotalIdlingFuelUsedL" double precision NULL,
	"Version" bigint NULL,
	"RecordLastChangedUtc" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_FuelAndEnergyUsed2" PRIMARY KEY ("DateTime", "id")
) PARTITION BY RANGE ("DateTime");

ALTER TABLE IF EXISTS public."FuelAndEnergyUsed2"
    OWNER to geotabadapter_client;

CREATE INDEX "IX_FuelAndEnergyUsed2_DeviceId" ON public."FuelAndEnergyUsed2" ("DeviceId");

CREATE INDEX "IX_FuelAndEnergyUsed2_RecordLastChangedUtc" ON public."FuelAndEnergyUsed2" ("RecordLastChangedUtc");

CREATE INDEX "IX_FuelAndEnergyUsed2_DateTime_Device" ON public."FuelAndEnergyUsed2" ("DateTime" ASC, "DeviceId" ASC);

ALTER TABLE public."FuelAndEnergyUsed2"
    ADD CONSTRAINT "FK_FuelAndEnergyUsed2_Devices2" FOREIGN KEY ("DeviceId")
    REFERENCES public."Devices2" ("id");
	
GRANT ALL ON TABLE public."FuelAndEnergyUsed2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create stg_FuelAndEnergyUsed2 table:
CREATE TABLE public."stg_FuelAndEnergyUsed2"(
	"id" uuid NOT NULL,
	"GeotabId" character varying(50) NOT NULL,
	"DateTime" timestamp without time zone NOT NULL,
	"DeviceId" bigint NOT NULL,
	"TotalEnergyUsedKwh" double precision NULL,
	"TotalFuelUsed" double precision NULL,
	"TotalIdlingEnergyUsedKwh" double precision NULL,
	"TotalIdlingFuelUsedL" double precision NULL,
	"Version" bigint NULL,
	"RecordLastChangedUtc" timestamp without time zone NOT NULL
);

ALTER TABLE IF EXISTS public."stg_FuelAndEnergyUsed2"
    OWNER to geotabadapter_client;

CREATE INDEX "IX_stg_FuelAndEnergyUsed2_id_RecordLastChangedUtc" ON public."stg_FuelAndEnergyUsed2" ("id" ASC, "RecordLastChangedUtc" ASC);

GRANT ALL ON TABLE public."stg_FuelAndEnergyUsed2" TO geotabadapter_client;


-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- Create spMerge_stg_FuelAndEnergyUsed2 function:
CREATE OR REPLACE FUNCTION public."spMerge_stg_FuelAndEnergyUsed2"()
    RETURNS void
    LANGUAGE plpgsql
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
-- ==========================================================================================
-- Description:
--   Upserts records from the stg_FuelAndEnergyUsed2 staging table to the FuelAndEnergyUsed2
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
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
    CREATE TEMP TABLE "TMP_DeduplicatedStaging" AS
    SELECT DISTINCT ON ("id") *
    FROM public."stg_FuelAndEnergyUsed2"
    ORDER BY "id", "RecordLastChangedUtc" DESC;

    -- Add an index to the temporary table on the column used for conflict resolution.
    CREATE INDEX ON "TMP_DeduplicatedStaging" ("id");

    -- Identify records where DateTime has changed.
    INSERT INTO "TMP_MovedRecordIds" ("id")
    SELECT s."id"
    FROM "TMP_DeduplicatedStaging" s
    INNER JOIN public."FuelAndEnergyUsed2" d ON s."id" = d."id"
    WHERE s."DateTime" IS DISTINCT FROM d."DateTime";

    -- Delete the old versions of these records from the target table.
    DELETE FROM public."FuelAndEnergyUsed2" AS d
    USING "TMP_MovedRecordIds" m
    WHERE d."id" = m."id";

    -- Perform upsert.
    -- Inserts new records AND records whose DateTime changed (deleted above).
    INSERT INTO public."FuelAndEnergyUsed2" AS d 
	(
        "id", 
		"GeotabId", 
		"DateTime", 
		"DeviceId", 
		"TotalEnergyUsedKwh", 
		"TotalFuelUsed",
        "TotalIdlingEnergyUsedKwh", 
		"TotalIdlingFuelUsedL", 
		"Version", 
		"RecordLastChangedUtc"
    )
    SELECT
        s."id", 
		s."GeotabId", 
		s."DateTime", 
		s."DeviceId", 
		s."TotalEnergyUsedKwh", 
		s."TotalFuelUsed",
        s."TotalIdlingEnergyUsedKwh", 
		s."TotalIdlingFuelUsedL", 
		s."Version", 
		s."RecordLastChangedUtc"
    FROM "TMP_DeduplicatedStaging" s
    ON CONFLICT ("DateTime", "id")
    DO UPDATE SET
		-- "id" and "DateTime" excluded since they are subject of ON CONFLICT.
		-- If only "DateTime" changed, the original record will have been deleted
		-- and a new one will be inserted instead of updating the existing record.		
        "GeotabId" = EXCLUDED."GeotabId",
        "DeviceId" = EXCLUDED."DeviceId",
        "TotalEnergyUsedKwh" = EXCLUDED."TotalEnergyUsedKwh",
        "TotalFuelUsed" = EXCLUDED."TotalFuelUsed",
        "TotalIdlingEnergyUsedKwh" = EXCLUDED."TotalIdlingEnergyUsedKwh",
        "TotalIdlingFuelUsedL" = EXCLUDED."TotalIdlingFuelUsedL",
        "Version" = EXCLUDED."Version",
        "RecordLastChangedUtc" = EXCLUDED."RecordLastChangedUtc" -- Always update the timestamp
    WHERE
        d."GeotabId" IS DISTINCT FROM EXCLUDED."GeotabId" OR
        d."DeviceId" IS DISTINCT FROM EXCLUDED."DeviceId" OR
        d."TotalEnergyUsedKwh" IS DISTINCT FROM EXCLUDED."TotalEnergyUsedKwh" OR
        d."TotalFuelUsed" IS DISTINCT FROM EXCLUDED."TotalFuelUsed" OR
        d."TotalIdlingEnergyUsedKwh" IS DISTINCT FROM EXCLUDED."TotalIdlingEnergyUsedKwh" OR
        d."TotalIdlingFuelUsedL" IS DISTINCT FROM EXCLUDED."TotalIdlingFuelUsedL" OR
        d."Version" IS DISTINCT FROM EXCLUDED."Version";
        -- RecordLastChangedUtc not evaluated as it should never match.

    -- Clear staging table.
    TRUNCATE TABLE public."stg_FuelAndEnergyUsed2";

    -- Clean up temporary tables.
	DROP TABLE IF EXISTS "TMP_MovedRecordIds";
    DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";

EXCEPTION
    WHEN OTHERS THEN
        -- Ensure temporary tables are cleaned up on error.
		DROP TABLE IF EXISTS "TMP_MovedRecordIds";
        DROP TABLE IF EXISTS "TMP_DeduplicatedStaging";
        
        -- Re-raise the original error to be caught by the calling application.
        RAISE;
END;
$BODY$;

ALTER FUNCTION public."spMerge_stg_FuelAndEnergyUsed2"()
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."spMerge_stg_FuelAndEnergyUsed2"() TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."spMerge_stg_FuelAndEnergyUsed2"() FROM PUBLIC;
/*** [END] Version 3.10.0.0 Updates ***/



/*** [START] Database Version Update ***/  
-- Insert a record into the MiddlewareVersionInfo2 table to reflect the current
-- database version.
INSERT INTO public."MiddlewareVersionInfo2" ("DatabaseVersion", "RecordCreationTimeUtc") 
VALUES ('3.10.0.0', timezone('UTC', NOW())); 
/*** [END] Database Version Update ***/
