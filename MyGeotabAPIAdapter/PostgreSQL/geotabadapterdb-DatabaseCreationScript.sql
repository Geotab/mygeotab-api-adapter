--
-- PostgreSQL database dump
--

-- Dumped from database version 16.0
-- Dumped by pg_dump version 16.0

-- Started on 2024-01-19 15:36:25

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
-- TOC entry 5042 (class 1262 OID 16398)
-- Name: geotabadapterdb; Type: DATABASE; Schema: -; Owner: geotabadapter_owner
--

CREATE DATABASE geotabadapterdb WITH TEMPLATE = template0 ENCODING = 'UTF8' LOCALE_PROVIDER = libc LOCALE = 'English_United States.1252';


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
-- TOC entry 237 (class 1259 OID 16401)
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
-- TOC entry 238 (class 1259 OID 16406)
-- Name: BinaryData_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."BinaryData_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."BinaryData_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5044 (class 0 OID 0)
-- Dependencies: 238
-- Name: BinaryData_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."BinaryData_id_seq" OWNED BY public."BinaryData".id;


--
-- TOC entry 288 (class 1259 OID 17926)
-- Name: ChargeEvents; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."ChargeEvents" (
    id bigint NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "ChargeIsEstimated" boolean NOT NULL,
    "ChargeType" character varying(50) NOT NULL,
    "StartTime" timestamp without time zone NOT NULL,
    "DeviceId" character varying(50) NOT NULL,
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
    "TripStop" timestamp without time zone,
    "Version" bigint NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."ChargeEvents" OWNER TO geotabadapter_owner;

--
-- TOC entry 289 (class 1259 OID 17929)
-- Name: ChargeEvents_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."ChargeEvents_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."ChargeEvents_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5047 (class 0 OID 0)
-- Dependencies: 289
-- Name: ChargeEvents_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."ChargeEvents_id_seq" OWNED BY public."ChargeEvents".id;


--
-- TOC entry 239 (class 1259 OID 16407)
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
-- TOC entry 240 (class 1259 OID 16412)
-- Name: Conditions_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."Conditions_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Conditions_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5050 (class 0 OID 0)
-- Dependencies: 240
-- Name: Conditions_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."Conditions_id_seq" OWNED BY public."Conditions".id;


--
-- TOC entry 241 (class 1259 OID 16413)
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
-- TOC entry 242 (class 1259 OID 16418)
-- Name: DVIRDefectRemarks_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."DVIRDefectRemarks_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."DVIRDefectRemarks_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5053 (class 0 OID 0)
-- Dependencies: 242
-- Name: DVIRDefectRemarks_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."DVIRDefectRemarks_id_seq" OWNED BY public."DVIRDefectRemarks".id;


--
-- TOC entry 243 (class 1259 OID 16419)
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
-- TOC entry 244 (class 1259 OID 16424)
-- Name: DVIRDefectUpdates_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."DVIRDefectUpdates_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."DVIRDefectUpdates_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5056 (class 0 OID 0)
-- Dependencies: 244
-- Name: DVIRDefectUpdates_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."DVIRDefectUpdates_id_seq" OWNED BY public."DVIRDefectUpdates".id;


--
-- TOC entry 245 (class 1259 OID 16425)
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
-- TOC entry 246 (class 1259 OID 16430)
-- Name: DVIRDefects_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."DVIRDefects_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."DVIRDefects_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5058 (class 0 OID 0)
-- Dependencies: 246
-- Name: DVIRDefects_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."DVIRDefects_id_seq" OWNED BY public."DVIRDefects".id;


--
-- TOC entry 247 (class 1259 OID 16431)
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
-- TOC entry 248 (class 1259 OID 16436)
-- Name: DVIRLogs_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."DVIRLogs_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."DVIRLogs_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5061 (class 0 OID 0)
-- Dependencies: 248
-- Name: DVIRLogs_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."DVIRLogs_id_seq" OWNED BY public."DVIRLogs".id;


--
-- TOC entry 249 (class 1259 OID 16437)
-- Name: DebugData_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."DebugData_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."DebugData_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 250 (class 1259 OID 16438)
-- Name: DebugData; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."DebugData" (
    id bigint DEFAULT nextval('public."DebugData_id_seq"'::regclass) NOT NULL,
    "GeotabId" character varying(50) NOT NULL,
    "Data" text NOT NULL,
    "DateTime" timestamp without time zone,
    "DebugReasonId" bigint,
    "DebugReasonName" character varying(255),
    "DeviceId" character varying(50),
    "DriverId" character varying(50),
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."DebugData" OWNER TO geotabadapter_owner;

--
-- TOC entry 251 (class 1259 OID 16444)
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
-- TOC entry 252 (class 1259 OID 16451)
-- Name: DeviceStatusInfo_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."DeviceStatusInfo_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."DeviceStatusInfo_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5066 (class 0 OID 0)
-- Dependencies: 252
-- Name: DeviceStatusInfo_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."DeviceStatusInfo_id_seq" OWNED BY public."DeviceStatusInfo".id;


--
-- TOC entry 253 (class 1259 OID 16452)
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
-- TOC entry 254 (class 1259 OID 16457)
-- Name: Devices_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."Devices_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Devices_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5069 (class 0 OID 0)
-- Dependencies: 254
-- Name: Devices_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."Devices_id_seq" OWNED BY public."Devices".id;


--
-- TOC entry 255 (class 1259 OID 16458)
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
-- TOC entry 256 (class 1259 OID 16463)
-- Name: Diagnostics_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."Diagnostics_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Diagnostics_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5072 (class 0 OID 0)
-- Dependencies: 256
-- Name: Diagnostics_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."Diagnostics_id_seq" OWNED BY public."Diagnostics".id;


--
-- TOC entry 257 (class 1259 OID 16464)
-- Name: DriverChanges_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."DriverChanges_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."DriverChanges_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 258 (class 1259 OID 16465)
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
-- TOC entry 259 (class 1259 OID 16469)
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
-- TOC entry 260 (class 1259 OID 16474)
-- Name: DutyStatusAvailabilities_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."DutyStatusAvailabilities_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."DutyStatusAvailabilities_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5077 (class 0 OID 0)
-- Dependencies: 260
-- Name: DutyStatusAvailabilities_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."DutyStatusAvailabilities_id_seq" OWNED BY public."DutyStatusAvailabilities".id;


--
-- TOC entry 261 (class 1259 OID 16475)
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
-- TOC entry 262 (class 1259 OID 16478)
-- Name: ExceptionEvents_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."ExceptionEvents_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."ExceptionEvents_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5080 (class 0 OID 0)
-- Dependencies: 262
-- Name: ExceptionEvents_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."ExceptionEvents_id_seq" OWNED BY public."ExceptionEvents".id;


--
-- TOC entry 263 (class 1259 OID 16479)
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
-- TOC entry 264 (class 1259 OID 16484)
-- Name: FailedDVIRDefectUpdates_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."FailedDVIRDefectUpdates_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."FailedDVIRDefectUpdates_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5083 (class 0 OID 0)
-- Dependencies: 264
-- Name: FailedDVIRDefectUpdates_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."FailedDVIRDefectUpdates_id_seq" OWNED BY public."FailedDVIRDefectUpdates".id;


--
-- TOC entry 265 (class 1259 OID 16485)
-- Name: FailedOVDSServerCommands_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."FailedOVDSServerCommands_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."FailedOVDSServerCommands_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 266 (class 1259 OID 16486)
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
-- TOC entry 267 (class 1259 OID 16492)
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
-- TOC entry 268 (class 1259 OID 16497)
-- Name: FaultData_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."FaultData_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."FaultData_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5088 (class 0 OID 0)
-- Dependencies: 268
-- Name: FaultData_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."FaultData_id_seq" OWNED BY public."FaultData".id;


--
-- TOC entry 269 (class 1259 OID 16498)
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
-- TOC entry 270 (class 1259 OID 16504)
-- Name: LogRecords_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."LogRecords_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."LogRecords_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5091 (class 0 OID 0)
-- Dependencies: 270
-- Name: LogRecords_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."LogRecords_id_seq" OWNED BY public."LogRecords".id;


--
-- TOC entry 271 (class 1259 OID 16505)
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
-- TOC entry 272 (class 1259 OID 16508)
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
-- TOC entry 273 (class 1259 OID 16511)
-- Name: OServiceTracking_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."OServiceTracking_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."OServiceTracking_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5095 (class 0 OID 0)
-- Dependencies: 273
-- Name: OServiceTracking_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."OServiceTracking_id_seq" OWNED BY public."OServiceTracking".id;


--
-- TOC entry 274 (class 1259 OID 16512)
-- Name: OVDSServerCommands; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."OVDSServerCommands" (
    id bigint NOT NULL,
    "Command" character varying NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."OVDSServerCommands" OWNER TO geotabadapter_owner;

--
-- TOC entry 275 (class 1259 OID 16517)
-- Name: OVDSServerCommands_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."OVDSServerCommands_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."OVDSServerCommands_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5098 (class 0 OID 0)
-- Dependencies: 275
-- Name: OVDSServerCommands_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."OVDSServerCommands_id_seq" OWNED BY public."OVDSServerCommands".id;


--
-- TOC entry 276 (class 1259 OID 16518)
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
-- TOC entry 277 (class 1259 OID 16523)
-- Name: Rules_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."Rules_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Rules_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5101 (class 0 OID 0)
-- Dependencies: 277
-- Name: Rules_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."Rules_id_seq" OWNED BY public."Rules".id;


--
-- TOC entry 278 (class 1259 OID 16524)
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
-- TOC entry 279 (class 1259 OID 16527)
-- Name: StatusData_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."StatusData_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."StatusData_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5104 (class 0 OID 0)
-- Dependencies: 279
-- Name: StatusData_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."StatusData_id_seq" OWNED BY public."StatusData".id;


--
-- TOC entry 280 (class 1259 OID 16528)
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
-- TOC entry 281 (class 1259 OID 16531)
-- Name: Trips_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."Trips_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Trips_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5107 (class 0 OID 0)
-- Dependencies: 281
-- Name: Trips_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."Trips_id_seq" OWNED BY public."Trips".id;


--
-- TOC entry 282 (class 1259 OID 16532)
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
-- TOC entry 283 (class 1259 OID 16537)
-- Name: Users_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."Users_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Users_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5110 (class 0 OID 0)
-- Dependencies: 283
-- Name: Users_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."Users_id_seq" OWNED BY public."Users".id;


--
-- TOC entry 284 (class 1259 OID 16538)
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
-- TOC entry 285 (class 1259 OID 16543)
-- Name: ZoneTypes_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."ZoneTypes_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."ZoneTypes_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5113 (class 0 OID 0)
-- Dependencies: 285
-- Name: ZoneTypes_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."ZoneTypes_id_seq" OWNED BY public."ZoneTypes".id;


--
-- TOC entry 286 (class 1259 OID 16544)
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
-- TOC entry 287 (class 1259 OID 16549)
-- Name: Zones_id_seq; Type: SEQUENCE; Schema: public; Owner: geotabadapter_owner
--

CREATE SEQUENCE public."Zones_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Zones_id_seq" OWNER TO geotabadapter_owner;

--
-- TOC entry 5116 (class 0 OID 0)
-- Dependencies: 287
-- Name: Zones_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: geotabadapter_owner
--

ALTER SEQUENCE public."Zones_id_seq" OWNED BY public."Zones".id;


--
-- TOC entry 4787 (class 2604 OID 16550)
-- Name: BinaryData id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."BinaryData" ALTER COLUMN id SET DEFAULT nextval('public."BinaryData_id_seq"'::regclass);


--
-- TOC entry 4819 (class 2604 OID 17930)
-- Name: ChargeEvents id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."ChargeEvents" ALTER COLUMN id SET DEFAULT nextval('public."ChargeEvents_id_seq"'::regclass);


--
-- TOC entry 4788 (class 2604 OID 16551)
-- Name: Conditions id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Conditions" ALTER COLUMN id SET DEFAULT nextval('public."Conditions_id_seq"'::regclass);


--
-- TOC entry 4789 (class 2604 OID 16552)
-- Name: DVIRDefectRemarks id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRDefectRemarks" ALTER COLUMN id SET DEFAULT nextval('public."DVIRDefectRemarks_id_seq"'::regclass);


--
-- TOC entry 4790 (class 2604 OID 16553)
-- Name: DVIRDefectUpdates id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRDefectUpdates" ALTER COLUMN id SET DEFAULT nextval('public."DVIRDefectUpdates_id_seq"'::regclass);


--
-- TOC entry 4791 (class 2604 OID 16554)
-- Name: DVIRDefects id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRDefects" ALTER COLUMN id SET DEFAULT nextval('public."DVIRDefects_id_seq"'::regclass);


--
-- TOC entry 4792 (class 2604 OID 16555)
-- Name: DVIRLogs id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRLogs" ALTER COLUMN id SET DEFAULT nextval('public."DVIRLogs_id_seq"'::regclass);


--
-- TOC entry 4794 (class 2604 OID 16556)
-- Name: DeviceStatusInfo id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DeviceStatusInfo" ALTER COLUMN id SET DEFAULT nextval('public."DeviceStatusInfo_id_seq"'::regclass);


--
-- TOC entry 4799 (class 2604 OID 16557)
-- Name: Devices id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Devices" ALTER COLUMN id SET DEFAULT nextval('public."Devices_id_seq"'::regclass);


--
-- TOC entry 4800 (class 2604 OID 16558)
-- Name: Diagnostics id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Diagnostics" ALTER COLUMN id SET DEFAULT nextval('public."Diagnostics_id_seq"'::regclass);


--
-- TOC entry 4802 (class 2604 OID 16559)
-- Name: DutyStatusAvailabilities id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DutyStatusAvailabilities" ALTER COLUMN id SET DEFAULT nextval('public."DutyStatusAvailabilities_id_seq"'::regclass);


--
-- TOC entry 4803 (class 2604 OID 16560)
-- Name: ExceptionEvents id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."ExceptionEvents" ALTER COLUMN id SET DEFAULT nextval('public."ExceptionEvents_id_seq"'::regclass);


--
-- TOC entry 4804 (class 2604 OID 16561)
-- Name: FailedDVIRDefectUpdates id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."FailedDVIRDefectUpdates" ALTER COLUMN id SET DEFAULT nextval('public."FailedDVIRDefectUpdates_id_seq"'::regclass);


--
-- TOC entry 4806 (class 2604 OID 16562)
-- Name: FaultData id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."FaultData" ALTER COLUMN id SET DEFAULT nextval('public."FaultData_id_seq"'::regclass);


--
-- TOC entry 4807 (class 2604 OID 16563)
-- Name: LogRecords id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."LogRecords" ALTER COLUMN id SET DEFAULT nextval('public."LogRecords_id_seq"'::regclass);


--
-- TOC entry 4811 (class 2604 OID 16564)
-- Name: OServiceTracking id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."OServiceTracking" ALTER COLUMN id SET DEFAULT nextval('public."OServiceTracking_id_seq"'::regclass);


--
-- TOC entry 4812 (class 2604 OID 16565)
-- Name: OVDSServerCommands id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."OVDSServerCommands" ALTER COLUMN id SET DEFAULT nextval('public."OVDSServerCommands_id_seq"'::regclass);


--
-- TOC entry 4813 (class 2604 OID 16566)
-- Name: Rules id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Rules" ALTER COLUMN id SET DEFAULT nextval('public."Rules_id_seq"'::regclass);


--
-- TOC entry 4814 (class 2604 OID 16567)
-- Name: StatusData id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."StatusData" ALTER COLUMN id SET DEFAULT nextval('public."StatusData_id_seq"'::regclass);


--
-- TOC entry 4815 (class 2604 OID 16568)
-- Name: Trips id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Trips" ALTER COLUMN id SET DEFAULT nextval('public."Trips_id_seq"'::regclass);


--
-- TOC entry 4816 (class 2604 OID 16569)
-- Name: Users id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Users" ALTER COLUMN id SET DEFAULT nextval('public."Users_id_seq"'::regclass);


--
-- TOC entry 4817 (class 2604 OID 16570)
-- Name: ZoneTypes id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."ZoneTypes" ALTER COLUMN id SET DEFAULT nextval('public."ZoneTypes_id_seq"'::regclass);


--
-- TOC entry 4818 (class 2604 OID 16571)
-- Name: Zones id; Type: DEFAULT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Zones" ALTER COLUMN id SET DEFAULT nextval('public."Zones_id_seq"'::regclass);


--
-- TOC entry 4821 (class 2606 OID 16573)
-- Name: BinaryData BinaryData_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."BinaryData"
    ADD CONSTRAINT "BinaryData_pkey" PRIMARY KEY (id);


--
-- TOC entry 4892 (class 2606 OID 17932)
-- Name: ChargeEvents ChargeEvents_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."ChargeEvents"
    ADD CONSTRAINT "ChargeEvents_pkey" PRIMARY KEY (id);


--
-- TOC entry 4824 (class 2606 OID 16575)
-- Name: Conditions Conditions_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Conditions"
    ADD CONSTRAINT "Conditions_pkey" PRIMARY KEY (id);


--
-- TOC entry 4827 (class 2606 OID 16577)
-- Name: DVIRDefectRemarks DVIRDefectRemarks_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRDefectRemarks"
    ADD CONSTRAINT "DVIRDefectRemarks_pkey" PRIMARY KEY (id);


--
-- TOC entry 4830 (class 2606 OID 16579)
-- Name: DVIRDefectUpdates DVIRDefectUpdates_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRDefectUpdates"
    ADD CONSTRAINT "DVIRDefectUpdates_pkey" PRIMARY KEY (id);


--
-- TOC entry 4833 (class 2606 OID 16581)
-- Name: DVIRDefects DVIRDefects_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRDefects"
    ADD CONSTRAINT "DVIRDefects_pkey" PRIMARY KEY (id);


--
-- TOC entry 4836 (class 2606 OID 16583)
-- Name: DVIRLogs DVIRLogs_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DVIRLogs"
    ADD CONSTRAINT "DVIRLogs_pkey" PRIMARY KEY (id);


--
-- TOC entry 4839 (class 2606 OID 16585)
-- Name: DebugData DebugData_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DebugData"
    ADD CONSTRAINT "DebugData_pkey" PRIMARY KEY (id);


--
-- TOC entry 4842 (class 2606 OID 16587)
-- Name: DeviceStatusInfo DeviceStatusInfo_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DeviceStatusInfo"
    ADD CONSTRAINT "DeviceStatusInfo_pkey" PRIMARY KEY (id);


--
-- TOC entry 4845 (class 2606 OID 16589)
-- Name: Devices Devices_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Devices"
    ADD CONSTRAINT "Devices_pkey" PRIMARY KEY (id);


--
-- TOC entry 4848 (class 2606 OID 16591)
-- Name: Diagnostics Diagnostics_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Diagnostics"
    ADD CONSTRAINT "Diagnostics_pkey" PRIMARY KEY (id);


--
-- TOC entry 4851 (class 2606 OID 16593)
-- Name: DriverChanges DriverChange_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DriverChanges"
    ADD CONSTRAINT "DriverChange_pkey" PRIMARY KEY (id);


--
-- TOC entry 4854 (class 2606 OID 16595)
-- Name: DutyStatusAvailabilities DutyStatusAvailabilities_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."DutyStatusAvailabilities"
    ADD CONSTRAINT "DutyStatusAvailabilities_pkey" PRIMARY KEY (id);


--
-- TOC entry 4857 (class 2606 OID 16597)
-- Name: ExceptionEvents ExceptionEvents_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."ExceptionEvents"
    ADD CONSTRAINT "ExceptionEvents_pkey" PRIMARY KEY (id);


--
-- TOC entry 4860 (class 2606 OID 16599)
-- Name: FailedOVDSServerCommands FailedOVDSServerCommands_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."FailedOVDSServerCommands"
    ADD CONSTRAINT "FailedOVDSServerCommands_pkey" PRIMARY KEY (id);


--
-- TOC entry 4862 (class 2606 OID 16601)
-- Name: FaultData FaultData_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."FaultData"
    ADD CONSTRAINT "FaultData_pkey" PRIMARY KEY (id);


--
-- TOC entry 4866 (class 2606 OID 16603)
-- Name: LogRecords LogRecords_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."LogRecords"
    ADD CONSTRAINT "LogRecords_pkey" PRIMARY KEY (id);


--
-- TOC entry 4870 (class 2606 OID 16605)
-- Name: OServiceTracking OServiceTracking_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."OServiceTracking"
    ADD CONSTRAINT "OServiceTracking_pkey" PRIMARY KEY (id);


--
-- TOC entry 4872 (class 2606 OID 16607)
-- Name: OVDSServerCommands OVDSServerCommands_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."OVDSServerCommands"
    ADD CONSTRAINT "OVDSServerCommands_pkey" PRIMARY KEY (id);


--
-- TOC entry 4875 (class 2606 OID 16609)
-- Name: Rules Rules_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Rules"
    ADD CONSTRAINT "Rules_pkey" PRIMARY KEY (id);


--
-- TOC entry 4878 (class 2606 OID 16611)
-- Name: StatusData StatusData_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."StatusData"
    ADD CONSTRAINT "StatusData_pkey" PRIMARY KEY (id);


--
-- TOC entry 4881 (class 2606 OID 16613)
-- Name: Trips Trips_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Trips"
    ADD CONSTRAINT "Trips_pkey" PRIMARY KEY (id);


--
-- TOC entry 4884 (class 2606 OID 16615)
-- Name: Users Users_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "Users_pkey" PRIMARY KEY (id);


--
-- TOC entry 4887 (class 2606 OID 16617)
-- Name: ZoneTypes ZoneTypes_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."ZoneTypes"
    ADD CONSTRAINT "ZoneTypes_pkey" PRIMARY KEY (id);


--
-- TOC entry 4890 (class 2606 OID 16619)
-- Name: Zones Zones_pkey; Type: CONSTRAINT; Schema: public; Owner: geotabadapter_owner
--

ALTER TABLE ONLY public."Zones"
    ADD CONSTRAINT "Zones_pkey" PRIMARY KEY (id);


--
-- TOC entry 4822 (class 1259 OID 16620)
-- Name: IX_BinaryData_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_BinaryData_DateTime" ON public."BinaryData" USING btree ("DateTime");


--
-- TOC entry 4893 (class 1259 OID 17933)
-- Name: IX_ChargeEvents_TripStop; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_ChargeEvents_TripStop" ON public."ChargeEvents" USING btree ("TripStop");


--
-- TOC entry 4825 (class 1259 OID 16621)
-- Name: IX_Conditions_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_Conditions_RecordLastChangedUtc" ON public."Conditions" USING btree ("RecordLastChangedUtc");


--
-- TOC entry 4828 (class 1259 OID 16622)
-- Name: IX_DVIRDefectRemarks_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_DVIRDefectRemarks_RecordLastChangedUtc" ON public."DVIRDefectRemarks" USING btree ("RecordLastChangedUtc");


--
-- TOC entry 4831 (class 1259 OID 16623)
-- Name: IX_DVIRDefectUpdates_RecordCreationTimeUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_DVIRDefectUpdates_RecordCreationTimeUtc" ON public."DVIRDefectUpdates" USING btree ("RecordCreationTimeUtc");


--
-- TOC entry 4834 (class 1259 OID 16624)
-- Name: IX_DVIRDefects_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_DVIRDefects_RecordLastChangedUtc" ON public."DVIRDefects" USING btree ("RecordLastChangedUtc");


--
-- TOC entry 4837 (class 1259 OID 16625)
-- Name: IX_DVIRLogs_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_DVIRLogs_DateTime" ON public."DVIRLogs" USING btree ("DateTime");


--
-- TOC entry 4840 (class 1259 OID 16626)
-- Name: IX_DebugData_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_DebugData_DateTime" ON public."DebugData" USING btree ("DateTime");


--
-- TOC entry 4843 (class 1259 OID 16627)
-- Name: IX_DeviceStatusInfo_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_DeviceStatusInfo_RecordLastChangedUtc" ON public."DeviceStatusInfo" USING btree ("RecordLastChangedUtc");


--
-- TOC entry 4846 (class 1259 OID 16628)
-- Name: IX_Devices_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_Devices_RecordLastChangedUtc" ON public."Devices" USING btree ("RecordLastChangedUtc");


--
-- TOC entry 4849 (class 1259 OID 16629)
-- Name: IX_Diagnostics_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_Diagnostics_RecordLastChangedUtc" ON public."Diagnostics" USING btree ("RecordLastChangedUtc");


--
-- TOC entry 4852 (class 1259 OID 16630)
-- Name: IX_DriverChanges_RecordCreationTimeUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_DriverChanges_RecordCreationTimeUtc" ON public."DriverChanges" USING btree ("RecordCreationTimeUtc");


--
-- TOC entry 4855 (class 1259 OID 16631)
-- Name: IX_DutyStatusAvailabilities_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_DutyStatusAvailabilities_RecordLastChangedUtc" ON public."DutyStatusAvailabilities" USING btree ("RecordLastChangedUtc");


--
-- TOC entry 4858 (class 1259 OID 16632)
-- Name: IX_ExceptionEvents_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_ExceptionEvents_RecordLastChangedUtc" ON public."ExceptionEvents" USING btree ("RecordLastChangedUtc");


--
-- TOC entry 4863 (class 1259 OID 16633)
-- Name: IX_FaultData_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_FaultData_DateTime" ON public."FaultData" USING btree ("DateTime");


--
-- TOC entry 4864 (class 1259 OID 16634)
-- Name: IX_LogRecords_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_LogRecords_DateTime" ON public."LogRecords" USING btree ("DateTime");


--
-- TOC entry 4867 (class 1259 OID 16635)
-- Name: IX_MyGeotabVersionInfo_RecordCreationTimeUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_MyGeotabVersionInfo_RecordCreationTimeUtc" ON public."MyGeotabVersionInfo" USING btree ("RecordCreationTimeUtc");


--
-- TOC entry 4868 (class 1259 OID 16636)
-- Name: IX_OServiceTracking_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_OServiceTracking_RecordLastChangedUtc" ON public."OServiceTracking" USING btree ("RecordLastChangedUtc");


--
-- TOC entry 4873 (class 1259 OID 16637)
-- Name: IX_Rules_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_Rules_RecordLastChangedUtc" ON public."Rules" USING btree ("RecordLastChangedUtc");


--
-- TOC entry 4876 (class 1259 OID 16638)
-- Name: IX_StatusData_DateTime; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_StatusData_DateTime" ON public."StatusData" USING btree ("DateTime");


--
-- TOC entry 4879 (class 1259 OID 16639)
-- Name: IX_Trips_RecordCreationTimeUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_Trips_RecordCreationTimeUtc" ON public."Trips" USING btree ("RecordCreationTimeUtc");


--
-- TOC entry 4882 (class 1259 OID 16640)
-- Name: IX_Users_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_Users_RecordLastChangedUtc" ON public."Users" USING btree ("RecordLastChangedUtc");


--
-- TOC entry 4885 (class 1259 OID 16641)
-- Name: IX_ZoneTypes_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_ZoneTypes_RecordLastChangedUtc" ON public."ZoneTypes" USING btree ("RecordLastChangedUtc");


--
-- TOC entry 4888 (class 1259 OID 16642)
-- Name: IX_Zones_RecordLastChangedUtc; Type: INDEX; Schema: public; Owner: geotabadapter_owner
--

CREATE INDEX "IX_Zones_RecordLastChangedUtc" ON public."Zones" USING btree ("RecordLastChangedUtc");


--
-- TOC entry 5043 (class 0 OID 0)
-- Dependencies: 237
-- Name: TABLE "BinaryData"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."BinaryData" TO geotabadapter_client;


--
-- TOC entry 5045 (class 0 OID 0)
-- Dependencies: 238
-- Name: SEQUENCE "BinaryData_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."BinaryData_id_seq" TO geotabadapter_client;


--
-- TOC entry 5046 (class 0 OID 0)
-- Dependencies: 288
-- Name: TABLE "ChargeEvents"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."ChargeEvents" TO geotabadapter_client;


--
-- TOC entry 5048 (class 0 OID 0)
-- Dependencies: 289
-- Name: SEQUENCE "ChargeEvents_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."ChargeEvents_id_seq" TO geotabadapter_client;


--
-- TOC entry 5049 (class 0 OID 0)
-- Dependencies: 239
-- Name: TABLE "Conditions"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Conditions" TO geotabadapter_client;


--
-- TOC entry 5051 (class 0 OID 0)
-- Dependencies: 240
-- Name: SEQUENCE "Conditions_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

REVOKE ALL ON SEQUENCE public."Conditions_id_seq" FROM geotabadapter_owner;
GRANT ALL ON SEQUENCE public."Conditions_id_seq" TO geotabadapter_client;


--
-- TOC entry 5052 (class 0 OID 0)
-- Dependencies: 241
-- Name: TABLE "DVIRDefectRemarks"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DVIRDefectRemarks" TO geotabadapter_client;


--
-- TOC entry 5054 (class 0 OID 0)
-- Dependencies: 242
-- Name: SEQUENCE "DVIRDefectRemarks_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."DVIRDefectRemarks_id_seq" TO geotabadapter_client;


--
-- TOC entry 5055 (class 0 OID 0)
-- Dependencies: 243
-- Name: TABLE "DVIRDefectUpdates"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DVIRDefectUpdates" TO geotabadapter_client;


--
-- TOC entry 5057 (class 0 OID 0)
-- Dependencies: 245
-- Name: TABLE "DVIRDefects"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DVIRDefects" TO geotabadapter_client;


--
-- TOC entry 5059 (class 0 OID 0)
-- Dependencies: 246
-- Name: SEQUENCE "DVIRDefects_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."DVIRDefects_id_seq" TO geotabadapter_client;


--
-- TOC entry 5060 (class 0 OID 0)
-- Dependencies: 247
-- Name: TABLE "DVIRLogs"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DVIRLogs" TO geotabadapter_client;


--
-- TOC entry 5062 (class 0 OID 0)
-- Dependencies: 248
-- Name: SEQUENCE "DVIRLogs_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."DVIRLogs_id_seq" TO geotabadapter_client;


--
-- TOC entry 5063 (class 0 OID 0)
-- Dependencies: 249
-- Name: SEQUENCE "DebugData_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."DebugData_id_seq" TO geotabadapter_client;


--
-- TOC entry 5064 (class 0 OID 0)
-- Dependencies: 250
-- Name: TABLE "DebugData"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DebugData" TO geotabadapter_client;


--
-- TOC entry 5065 (class 0 OID 0)
-- Dependencies: 251
-- Name: TABLE "DeviceStatusInfo"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DeviceStatusInfo" TO geotabadapter_client;


--
-- TOC entry 5067 (class 0 OID 0)
-- Dependencies: 252
-- Name: SEQUENCE "DeviceStatusInfo_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."DeviceStatusInfo_id_seq" TO geotabadapter_client;


--
-- TOC entry 5068 (class 0 OID 0)
-- Dependencies: 253
-- Name: TABLE "Devices"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Devices" TO geotabadapter_client;


--
-- TOC entry 5070 (class 0 OID 0)
-- Dependencies: 254
-- Name: SEQUENCE "Devices_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."Devices_id_seq" TO geotabadapter_client;


--
-- TOC entry 5071 (class 0 OID 0)
-- Dependencies: 255
-- Name: TABLE "Diagnostics"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Diagnostics" TO geotabadapter_client;


--
-- TOC entry 5073 (class 0 OID 0)
-- Dependencies: 256
-- Name: SEQUENCE "Diagnostics_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."Diagnostics_id_seq" TO geotabadapter_client;


--
-- TOC entry 5074 (class 0 OID 0)
-- Dependencies: 257
-- Name: SEQUENCE "DriverChanges_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."DriverChanges_id_seq" TO geotabadapter_client;


--
-- TOC entry 5075 (class 0 OID 0)
-- Dependencies: 258
-- Name: TABLE "DriverChanges"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DriverChanges" TO geotabadapter_client;


--
-- TOC entry 5076 (class 0 OID 0)
-- Dependencies: 259
-- Name: TABLE "DutyStatusAvailabilities"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DutyStatusAvailabilities" TO geotabadapter_client;


--
-- TOC entry 5078 (class 0 OID 0)
-- Dependencies: 260
-- Name: SEQUENCE "DutyStatusAvailabilities_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."DutyStatusAvailabilities_id_seq" TO geotabadapter_client;


--
-- TOC entry 5079 (class 0 OID 0)
-- Dependencies: 261
-- Name: TABLE "ExceptionEvents"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."ExceptionEvents" TO geotabadapter_client;


--
-- TOC entry 5081 (class 0 OID 0)
-- Dependencies: 262
-- Name: SEQUENCE "ExceptionEvents_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."ExceptionEvents_id_seq" TO geotabadapter_client;


--
-- TOC entry 5082 (class 0 OID 0)
-- Dependencies: 263
-- Name: TABLE "FailedDVIRDefectUpdates"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."FailedDVIRDefectUpdates" TO geotabadapter_client;


--
-- TOC entry 5084 (class 0 OID 0)
-- Dependencies: 264
-- Name: SEQUENCE "FailedDVIRDefectUpdates_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."FailedDVIRDefectUpdates_id_seq" TO geotabadapter_client;


--
-- TOC entry 5085 (class 0 OID 0)
-- Dependencies: 265
-- Name: SEQUENCE "FailedOVDSServerCommands_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."FailedOVDSServerCommands_id_seq" TO geotabadapter_client;


--
-- TOC entry 5086 (class 0 OID 0)
-- Dependencies: 266
-- Name: TABLE "FailedOVDSServerCommands"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."FailedOVDSServerCommands" TO geotabadapter_client;


--
-- TOC entry 5087 (class 0 OID 0)
-- Dependencies: 267
-- Name: TABLE "FaultData"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."FaultData" TO geotabadapter_client;


--
-- TOC entry 5089 (class 0 OID 0)
-- Dependencies: 268
-- Name: SEQUENCE "FaultData_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."FaultData_id_seq" TO geotabadapter_client;


--
-- TOC entry 5090 (class 0 OID 0)
-- Dependencies: 269
-- Name: TABLE "LogRecords"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."LogRecords" TO geotabadapter_client;


--
-- TOC entry 5092 (class 0 OID 0)
-- Dependencies: 270
-- Name: SEQUENCE "LogRecords_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."LogRecords_id_seq" TO geotabadapter_client;


--
-- TOC entry 5093 (class 0 OID 0)
-- Dependencies: 271
-- Name: TABLE "MyGeotabVersionInfo"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."MyGeotabVersionInfo" TO geotabadapter_client;


--
-- TOC entry 5094 (class 0 OID 0)
-- Dependencies: 272
-- Name: TABLE "OServiceTracking"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."OServiceTracking" TO geotabadapter_client;


--
-- TOC entry 5096 (class 0 OID 0)
-- Dependencies: 273
-- Name: SEQUENCE "OServiceTracking_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."OServiceTracking_id_seq" TO geotabadapter_client;


--
-- TOC entry 5097 (class 0 OID 0)
-- Dependencies: 274
-- Name: TABLE "OVDSServerCommands"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."OVDSServerCommands" TO geotabadapter_client;


--
-- TOC entry 5099 (class 0 OID 0)
-- Dependencies: 275
-- Name: SEQUENCE "OVDSServerCommands_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

REVOKE ALL ON SEQUENCE public."OVDSServerCommands_id_seq" FROM geotabadapter_owner;
GRANT ALL ON SEQUENCE public."OVDSServerCommands_id_seq" TO geotabadapter_client;


--
-- TOC entry 5100 (class 0 OID 0)
-- Dependencies: 276
-- Name: TABLE "Rules"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Rules" TO geotabadapter_client;


--
-- TOC entry 5102 (class 0 OID 0)
-- Dependencies: 277
-- Name: SEQUENCE "Rules_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."Rules_id_seq" TO geotabadapter_client;


--
-- TOC entry 5103 (class 0 OID 0)
-- Dependencies: 278
-- Name: TABLE "StatusData"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."StatusData" TO geotabadapter_client;


--
-- TOC entry 5105 (class 0 OID 0)
-- Dependencies: 279
-- Name: SEQUENCE "StatusData_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."StatusData_id_seq" TO geotabadapter_client;


--
-- TOC entry 5106 (class 0 OID 0)
-- Dependencies: 280
-- Name: TABLE "Trips"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Trips" TO geotabadapter_client;


--
-- TOC entry 5108 (class 0 OID 0)
-- Dependencies: 281
-- Name: SEQUENCE "Trips_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."Trips_id_seq" TO geotabadapter_client;


--
-- TOC entry 5109 (class 0 OID 0)
-- Dependencies: 282
-- Name: TABLE "Users"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Users" TO geotabadapter_client;


--
-- TOC entry 5111 (class 0 OID 0)
-- Dependencies: 283
-- Name: SEQUENCE "Users_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."Users_id_seq" TO geotabadapter_client;


--
-- TOC entry 5112 (class 0 OID 0)
-- Dependencies: 284
-- Name: TABLE "ZoneTypes"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."ZoneTypes" TO geotabadapter_client;


--
-- TOC entry 5114 (class 0 OID 0)
-- Dependencies: 285
-- Name: SEQUENCE "ZoneTypes_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."ZoneTypes_id_seq" TO geotabadapter_client;


--
-- TOC entry 5115 (class 0 OID 0)
-- Dependencies: 286
-- Name: TABLE "Zones"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Zones" TO geotabadapter_client;


--
-- TOC entry 5117 (class 0 OID 0)
-- Dependencies: 287
-- Name: SEQUENCE "Zones_id_seq"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT ALL ON SEQUENCE public."Zones_id_seq" TO geotabadapter_client;


--
-- TOC entry 2189 (class 826 OID 16643)
-- Name: DEFAULT PRIVILEGES FOR TABLES; Type: DEFAULT ACL; Schema: public; Owner: postgres
--

ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public GRANT SELECT,INSERT,DELETE,UPDATE ON TABLES TO geotabadapter_client;


--
-- TOC entry 2190 (class 826 OID 16644)
-- Name: DEFAULT PRIVILEGES FOR TABLES; Type: DEFAULT ACL; Schema: -; Owner: postgres
--

ALTER DEFAULT PRIVILEGES FOR ROLE postgres GRANT SELECT,INSERT,DELETE,UPDATE ON TABLES TO geotabadapter_client;


-- Completed on 2024-01-19 15:36:25

--
-- PostgreSQL database dump complete
--

