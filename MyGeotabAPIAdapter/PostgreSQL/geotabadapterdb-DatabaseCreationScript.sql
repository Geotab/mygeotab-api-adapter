--
-- PostgreSQL database dump
--

-- Dumped from database version 14.0
-- Dumped by pg_dump version 14.0

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

CREATE DATABASE geotabadapterdb WITH TEMPLATE = template0 ENCODING = 'UTF8' LOCALE = 'English_United States.1252';


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

SET default_table_access_method = heap;

--
-- Name: BinaryData; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."BinaryData" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "BinaryType" character varying(50),
    "ControllerId" character varying(50) NOT NULL,
    "Data" character varying(1024) NOT NULL,
    "DateTime" timestamp without time zone,
    "DeviceId" character varying(50),
    "Version" character varying(50),
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."BinaryData" OWNER TO geotabadapter_owner;

--
-- Name: BinaryData_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."BinaryData_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."BinaryData_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: BinaryData_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."BinaryData_id_seq" OWNED BY public."BinaryData".id;


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
-- Name: DVIRDefectRemarks; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."DVIRDefectRemarks" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DVIRDefectId" character varying(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Remark" text,
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
-- Name: DVIRDefectUpdates; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."DVIRDefectUpdates" (
    id bigint NOT NULL,
    "DVIRLogId" character varying(50) NOT NULL,
    "DVIRDefectId" character varying(50) NOT NULL,
    "RepairDateTime" timestamp without time zone,
    "RepairStatus" character varying(50),
    "RepairUserId" character varying(50),
    "Remark" text,
    "RemarkDateTime" timestamp without time zone,
    "RemarkUserId" character varying(50),
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."DVIRDefectUpdates" OWNER TO geotabadapter_owner;

--
-- Name: DVIRDefectUpdates_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."DVIRDefectUpdates_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."DVIRDefectUpdates_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: DVIRDefectUpdates_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."DVIRDefectUpdates_id_seq" OWNED BY public."DVIRDefectUpdates".id;


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
-- Name: DeviceStatusInfo; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."DeviceStatusInfo" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "Bearing" double precision DEFAULT 0 NOT NULL,
    "CurrentStateDuration" character varying(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" character varying(50) NOT NULL,
    "DriverId" character varying(50) NOT NULL,
    "IsDeviceCommunicating" boolean NOT NULL,
    "IsDriving" boolean NOT NULL,
    "IsHistoricLastDriver" boolean NOT NULL,
    "Latitude" double precision DEFAULT 0 NOT NULL,
    "Longitude" double precision DEFAULT 0 NOT NULL,
    "Speed" real DEFAULT 0 NOT NULL,
    "RecordLastChangedUtc" timestamp(4) without time zone NOT NULL
);


ALTER TABLE public."DeviceStatusInfo" OWNER TO geotabadapter_owner;

--
-- Name: DeviceStatusInfo_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."DeviceStatusInfo_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."DeviceStatusInfo_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: DeviceStatusInfo_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."DeviceStatusInfo_id_seq" OWNED BY public."DeviceStatusInfo".id;


--
-- Name: Devices; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."Devices" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp(4) without time zone,
    "ActiveTo" timestamp(4) without time zone,
    "Comment" character varying(1024),
    "DeviceType" character varying(50) NOT NULL,
    "LicensePlate" character varying(50),
    "LicenseState" character varying(50),
    "Name" character varying(100) NOT NULL,
    "ProductId" integer,
    "SerialNumber" character varying(12),
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
    "GeotabGUID" character varying(100) NOT NULL,
    "HasShimId" boolean,
    "FormerShimGeotabGUID" character varying(100),
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
-- Name: DriverChanges_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."DriverChanges_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."DriverChanges_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: DriverChanges; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."DriverChanges" (
    id bigint DEFAULT nextval('public."DriverChanges_id_seq"'::regclass) NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "DateTime" timestamp without time zone,
    "DeviceId" character varying(50) NOT NULL,
    "DriverId" character varying(50) NOT NULL,
    "Type" character varying(50) NOT NULL,
    "Version" bigint NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."DriverChanges" OWNER TO geotabadapter_owner;

--
-- Name: DutyStatusAvailabilities; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."DutyStatusAvailabilities" (
    id bigint NOT NULL,
    "DriverId" character varying(50) NOT NULL,
    "CycleAvailabilities" text,
    "CycleTicks" bigint,
    "CycleRestTicks" bigint,
    "DrivingTicks" bigint,
    "DutyTicks" bigint,
    "DutySinceCycleRestTicks" bigint,
    "Is16HourExemptionAvailable" boolean,
    "IsAdverseDrivingExemptionAvailable" boolean,
    "IsOffDutyDeferralExemptionAvailable" boolean,
    "Recap" text,
    "RestTicks" bigint,
    "WorkdayTicks" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."DutyStatusAvailabilities" OWNER TO geotabadapter_owner;

--
-- Name: DutyStatusAvailabilities_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."DutyStatusAvailabilities_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."DutyStatusAvailabilities_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: DutyStatusAvailabilities_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."DutyStatusAvailabilities_id_seq" OWNED BY public."DutyStatusAvailabilities".id;


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
    "LastModifiedDateTime" timestamp without time zone,
    "RuleId" character varying(50),
    "State" integer,
    "Version" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
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
-- Name: FailedDVIRDefectUpdates; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."FailedDVIRDefectUpdates" (
    id bigint NOT NULL,
    "DVIRDefectUpdateId" bigint NOT NULL,
    "DVIRLogId" character varying(50) NOT NULL,
    "DVIRDefectId" character varying(50) NOT NULL,
    "RepairDateTime" timestamp without time zone,
    "RepairStatus" character varying(50),
    "RepairUserId" character varying(50),
    "Remark" text,
    "RemarkDateTime" timestamp without time zone,
    "RemarkUserId" character varying(50),
    "FailureMessage" text,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."FailedDVIRDefectUpdates" OWNER TO geotabadapter_owner;

--
-- Name: FailedDVIRDefectUpdates_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."FailedDVIRDefectUpdates_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."FailedDVIRDefectUpdates_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: FailedDVIRDefectUpdates_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."FailedDVIRDefectUpdates_id_seq" OWNED BY public."FailedDVIRDefectUpdates".id;


--
-- Name: FailedOVDSServerCommands_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."FailedOVDSServerCommands_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."FailedOVDSServerCommands_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: FailedOVDSServerCommands; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."FailedOVDSServerCommands" (
    id bigint DEFAULT nextval('public."FailedOVDSServerCommands_id_seq"'::regclass) NOT NULL,
    "OVDSServerCommandId" bigint NOT NULL,
    "Command" character varying NOT NULL,
    "FailureMessage" text,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."FailedOVDSServerCommands" OWNER TO geotabadapter_owner;

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
-- Name: OServiceTracking; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."OServiceTracking" (
    id bigint NOT NULL,
    "ServiceId" character varying(50) NOT NULL,
    "AdapterVersion" character varying(50),
    "AdapterMachineName" character varying(100),
    "EntitiesLastProcessedUtc" timestamp without time zone,
    "LastProcessedFeedVersion" bigint,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."OServiceTracking" OWNER TO geotabadapter_owner;

--
-- Name: OServiceTracking_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."OServiceTracking_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."OServiceTracking_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: OServiceTracking_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."OServiceTracking_id_seq" OWNED BY public."OServiceTracking".id;


--
-- Name: OVDSServerCommands; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."OVDSServerCommands" (
    id bigint NOT NULL,
    "Command" character varying NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."OVDSServerCommands" OWNER TO geotabadapter_owner;

--
-- Name: OVDSServerCommands_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."OVDSServerCommands_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."OVDSServerCommands_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: OVDSServerCommands_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."OVDSServerCommands_id_seq" OWNED BY public."OVDSServerCommands".id;


--
-- Name: Rules; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."Rules" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone,
    "ActiveTo" timestamp without time zone,
    "BaseType" character varying(50),
    "Comment" character varying,
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
    "AfterHoursDistance" real,
    "AfterHoursDrivingDurationTicks" bigint,
    "AfterHoursEnd" boolean,
    "AfterHoursStart" boolean,
    "AfterHoursStopDurationTicks" bigint,
    "AverageSpeed" real,
    "DeviceId" character varying(50) NOT NULL,
    "Distance" real NOT NULL,
    "DriverId" character varying(50) NOT NULL,
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
    "FirstName" character varying(255),
    "HosRuleSet" character varying,
    "IsDriver" boolean NOT NULL,
    "LastAccessDate" timestamp without time zone,
    "LastName" character varying(255),
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
-- Name: ZoneTypes; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."ZoneTypes" (
    id bigint NOT NULL,
    "GeotabId" character varying(100) NOT NULL,
    "Comment" character varying(255),
    "Name" character varying(255) NOT NULL,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."ZoneTypes" OWNER TO geotabadapter_owner;

--
-- Name: ZoneTypes_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."ZoneTypes_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public."ZoneTypes_id_seq" OWNER TO geotabadapter_owner;

--
-- Name: ZoneTypes_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."ZoneTypes_id_seq" OWNED BY public."ZoneTypes".id;


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
-- Name: BinaryData id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."BinaryData" ALTER COLUMN id SET DEFAULT nextval('public."BinaryData_id_seq"'::regclass);


--
-- Name: Conditions id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Conditions" ALTER COLUMN id SET DEFAULT nextval('public."Conditions_id_seq"'::regclass);


--
-- Name: DVIRDefectRemarks id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRDefectRemarks" ALTER COLUMN id SET DEFAULT nextval('public."DVIRDefectRemarks_id_seq"'::regclass);


--
-- Name: DVIRDefectUpdates id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRDefectUpdates" ALTER COLUMN id SET DEFAULT nextval('public."DVIRDefectUpdates_id_seq"'::regclass);


--
-- Name: DVIRDefects id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRDefects" ALTER COLUMN id SET DEFAULT nextval('public."DVIRDefects_id_seq"'::regclass);


--
-- Name: DVIRLogs id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRLogs" ALTER COLUMN id SET DEFAULT nextval('public."DVIRLogs_id_seq"'::regclass);


--
-- Name: DeviceStatusInfo id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DeviceStatusInfo" ALTER COLUMN id SET DEFAULT nextval('public."DeviceStatusInfo_id_seq"'::regclass);


--
-- Name: Devices id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Devices" ALTER COLUMN id SET DEFAULT nextval('public."Devices_id_seq"'::regclass);


--
-- Name: Diagnostics id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Diagnostics" ALTER COLUMN id SET DEFAULT nextval('public."Diagnostics_id_seq"'::regclass);


--
-- Name: DutyStatusAvailabilities id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DutyStatusAvailabilities" ALTER COLUMN id SET DEFAULT nextval('public."DutyStatusAvailabilities_id_seq"'::regclass);


--
-- Name: ExceptionEvents id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."ExceptionEvents" ALTER COLUMN id SET DEFAULT nextval('public."ExceptionEvents_id_seq"'::regclass);


--
-- Name: FailedDVIRDefectUpdates id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."FailedDVIRDefectUpdates" ALTER COLUMN id SET DEFAULT nextval('public."FailedDVIRDefectUpdates_id_seq"'::regclass);


--
-- Name: FaultData id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."FaultData" ALTER COLUMN id SET DEFAULT nextval('public."FaultData_id_seq"'::regclass);


--
-- Name: LogRecords id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."LogRecords" ALTER COLUMN id SET DEFAULT nextval('public."LogRecords_id_seq"'::regclass);


--
-- Name: OServiceTracking id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."OServiceTracking" ALTER COLUMN id SET DEFAULT nextval('public."OServiceTracking_id_seq"'::regclass);


--
-- Name: OVDSServerCommands id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."OVDSServerCommands" ALTER COLUMN id SET DEFAULT nextval('public."OVDSServerCommands_id_seq"'::regclass);


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
-- Name: ZoneTypes id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."ZoneTypes" ALTER COLUMN id SET DEFAULT nextval('public."ZoneTypes_id_seq"'::regclass);


--
-- Name: Zones id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Zones" ALTER COLUMN id SET DEFAULT nextval('public."Zones_id_seq"'::regclass);


--
-- Name: BinaryData BinaryData_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."BinaryData"
    ADD CONSTRAINT "BinaryData_pkey" PRIMARY KEY (id);


--
-- Name: Conditions Conditions_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Conditions"
    ADD CONSTRAINT "Conditions_pkey" PRIMARY KEY (id);


--
-- Name: DVIRDefectRemarks DVIRDefectRemarks_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRDefectRemarks"
    ADD CONSTRAINT "DVIRDefectRemarks_pkey" PRIMARY KEY (id);


--
-- Name: DVIRDefectUpdates DVIRDefectUpdates_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRDefectUpdates"
    ADD CONSTRAINT "DVIRDefectUpdates_pkey" PRIMARY KEY (id);


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
-- Name: DeviceStatusInfo DeviceStatusInfo_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DeviceStatusInfo"
    ADD CONSTRAINT "DeviceStatusInfo_pkey" PRIMARY KEY (id);


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
-- Name: DriverChanges DriverChange_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DriverChanges"
    ADD CONSTRAINT "DriverChange_pkey" PRIMARY KEY (id);


--
-- Name: DutyStatusAvailabilities DutyStatusAvailabilities_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DutyStatusAvailabilities"
    ADD CONSTRAINT "DutyStatusAvailabilities_pkey" PRIMARY KEY (id);


--
-- Name: ExceptionEvents ExceptionEvents_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."ExceptionEvents"
    ADD CONSTRAINT "ExceptionEvents_pkey" PRIMARY KEY (id);


--
-- Name: FailedOVDSServerCommands FailedOVDSServerCommands_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."FailedOVDSServerCommands"
    ADD CONSTRAINT "FailedOVDSServerCommands_pkey" PRIMARY KEY (id);


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
-- Name: OServiceTracking OServiceTracking_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."OServiceTracking"
    ADD CONSTRAINT "OServiceTracking_pkey" PRIMARY KEY (id);


--
-- Name: OVDSServerCommands OVDSServerCommands_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."OVDSServerCommands"
    ADD CONSTRAINT "OVDSServerCommands_pkey" PRIMARY KEY (id);


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
-- Name: ZoneTypes ZoneTypes_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."ZoneTypes"
    ADD CONSTRAINT "ZoneTypes_pkey" PRIMARY KEY (id);


--
-- Name: Zones Zones_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Zones"
    ADD CONSTRAINT "Zones_pkey" PRIMARY KEY (id);


--
-- Name: IX_BinaryData_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_BinaryData_DateTime" ON public."BinaryData" USING btree ("DateTime");


--
-- Name: IX_Conditions_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_Conditions_RecordLastChangedUtc" ON public."Conditions" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_DVIRDefectRemarks_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_DVIRDefectRemarks_RecordLastChangedUtc" ON public."DVIRDefectRemarks" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_DVIRDefectUpdates_RecordCreationTimeUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_DVIRDefectUpdates_RecordCreationTimeUtc" ON public."DVIRDefectUpdates" USING btree ("RecordCreationTimeUtc");


--
-- Name: IX_DVIRDefects_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_DVIRDefects_RecordLastChangedUtc" ON public."DVIRDefects" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_DVIRLogs_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_DVIRLogs_DateTime" ON public."DVIRLogs" USING btree ("DateTime");


--
-- Name: IX_DeviceStatusInfo_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_DeviceStatusInfo_RecordLastChangedUtc" ON public."DeviceStatusInfo" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_Devices_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_Devices_RecordLastChangedUtc" ON public."Devices" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_Diagnostics_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_Diagnostics_RecordLastChangedUtc" ON public."Diagnostics" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_DriverChanges_RecordCreationTimeUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_DriverChanges_RecordCreationTimeUtc" ON public."DriverChanges" USING btree ("RecordCreationTimeUtc");


--
-- Name: IX_DutyStatusAvailabilities_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_DutyStatusAvailabilities_RecordLastChangedUtc" ON public."DutyStatusAvailabilities" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_ExceptionEvents_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_ExceptionEvents_RecordLastChangedUtc" ON public."ExceptionEvents" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_FaultData_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_FaultData_DateTime" ON public."FaultData" USING btree ("DateTime");


--
-- Name: IX_LogRecords_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_LogRecords_DateTime" ON public."LogRecords" USING btree ("DateTime");


--
-- Name: IX_MyGeotabVersionInfo_RecordCreationTimeUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_MyGeotabVersionInfo_RecordCreationTimeUtc" ON public."MyGeotabVersionInfo" USING btree ("RecordCreationTimeUtc");


--
-- Name: IX_OServiceTracking_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_OServiceTracking_RecordLastChangedUtc" ON public."OServiceTracking" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_Rules_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_Rules_RecordLastChangedUtc" ON public."Rules" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_StatusData_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_StatusData_DateTime" ON public."StatusData" USING btree ("DateTime");


--
-- Name: IX_Trips_RecordCreationTimeUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_Trips_RecordCreationTimeUtc" ON public."Trips" USING btree ("RecordCreationTimeUtc");


--
-- Name: IX_Users_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_Users_RecordLastChangedUtc" ON public."Users" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_ZoneTypes_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_ZoneTypes_RecordLastChangedUtc" ON public."ZoneTypes" USING btree ("RecordLastChangedUtc");


--
-- Name: IX_Zones_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_Zones_RecordLastChangedUtc" ON public."Zones" USING btree ("RecordLastChangedUtc");


--
-- Name: TABLE "BinaryData"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."BinaryData" TO geotabadapter_client;


--
-- Name: SEQUENCE "BinaryData_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."BinaryData_id_seq" TO geotabadapter_client;


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
-- Name: TABLE "DVIRDefectRemarks"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DVIRDefectRemarks" TO geotabadapter_client;


--
-- Name: SEQUENCE "DVIRDefectRemarks_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."DVIRDefectRemarks_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "DVIRDefectUpdates"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DVIRDefectUpdates" TO geotabadapter_client;


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
-- Name: TABLE "DeviceStatusInfo"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DeviceStatusInfo" TO geotabadapter_client;


--
-- Name: SEQUENCE "DeviceStatusInfo_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."DeviceStatusInfo_id_seq" TO geotabadapter_client;


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
-- Name: SEQUENCE "DriverChanges_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."DriverChanges_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "DriverChanges"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DriverChanges" TO geotabadapter_client;


--
-- Name: TABLE "DutyStatusAvailabilities"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DutyStatusAvailabilities" TO geotabadapter_client;


--
-- Name: SEQUENCE "DutyStatusAvailabilities_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."DutyStatusAvailabilities_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "ExceptionEvents"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."ExceptionEvents" TO geotabadapter_client;


--
-- Name: SEQUENCE "ExceptionEvents_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."ExceptionEvents_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "FailedDVIRDefectUpdates"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."FailedDVIRDefectUpdates" TO geotabadapter_client;


--
-- Name: SEQUENCE "FailedDVIRDefectUpdates_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."FailedDVIRDefectUpdates_id_seq" TO geotabadapter_client;


--
-- Name: SEQUENCE "FailedOVDSServerCommands_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."FailedOVDSServerCommands_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "FailedOVDSServerCommands"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."FailedOVDSServerCommands" TO geotabadapter_client;


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
-- Name: TABLE "OServiceTracking"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."OServiceTracking" TO geotabadapter_client;


--
-- Name: SEQUENCE "OServiceTracking_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."OServiceTracking_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "OVDSServerCommands"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."OVDSServerCommands" TO geotabadapter_client;


--
-- Name: SEQUENCE "OVDSServerCommands_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

REVOKE ALL ON SEQUENCE public."OVDSServerCommands_id_seq" FROM geotabadapter_owner;
GRANT ALL ON SEQUENCE public."OVDSServerCommands_id_seq" TO geotabadapter_client;


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

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."StatusData" TO geotabadapter_client;


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
-- Name: TABLE "ZoneTypes"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."ZoneTypes" TO geotabadapter_client;


--
-- Name: SEQUENCE "ZoneTypes_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."ZoneTypes_id_seq" TO geotabadapter_client;


--
-- Name: TABLE "Zones"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Zones" TO geotabadapter_client;


--
-- Name: SEQUENCE "Zones_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."Zones_id_seq" TO geotabadapter_client;


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

