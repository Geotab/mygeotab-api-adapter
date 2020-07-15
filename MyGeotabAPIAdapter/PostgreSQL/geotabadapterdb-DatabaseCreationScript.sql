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
    "Id" character varying(50) NOT NULL,
    "ParentId" character varying(50),
    "RuleId" character varying(50),
    "ConditionType" character varying(50) NOT NULL,
    "DeviceId" character varying(50),
    "DiagnosticId" character varying(50),
    "DriverId" character varying(50),
    "Value" double precision,
    "WorkTimeId" character varying(50),
    "ZoneId" character varying(50),
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."Conditions" OWNER TO geotabadapter_owner;

--
-- Name: ConfigFeedVersions; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."ConfigFeedVersions" (
    "FeedTypeId" character varying(50) NOT NULL,
    "LastProcessedFeedVersion" bigint NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."ConfigFeedVersions" OWNER TO geotabadapter_owner;

--
-- Name: DVIRDefectRemarks; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."DVIRDefectRemarks" (
    "Id" character varying(50) NOT NULL,
    "DVIRDefectId" character varying(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "Remark" text NOT NULL,
    "RemarkUserId" character varying(50),
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."DVIRDefectRemarks" OWNER TO geotabadapter_owner;

--
-- Name: DVIRDefects; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."DVIRDefects" (
    "Id" character varying(50) NOT NULL,
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
-- Name: DVIRLogs; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."DVIRLogs" (
    "Id" character varying(50) NOT NULL,
    "CertifiedByUserId" character varying(50),
    "CertifiedDate" timestamp without time zone,
    "CertifyRemark" text,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" character varying(50),
    "DriverId" character varying(50),
    "DriverRemark" text,
    "IsSafeToOperate" boolean,
    "LocationLatitude" real,
    "LocationLongitude" real,
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
-- Name: Devices; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."Devices" (
    "Id" character varying(50) NOT NULL,
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
-- Name: Diagnostics; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."Diagnostics" (
    "Id" character varying(100) NOT NULL,
    "DiagnosticCode" integer,
    "DiagnosticName" character varying(255) NOT NULL,
    "DiagnosticSourceId" character varying(50) NOT NULL,
    "DiagnosticSourceName" character varying(255) NOT NULL,
    "DiagnosticUnitOfMeasureId" character varying(50) NOT NULL,
    "DiagnosticUnitOfMeasureName" character varying(255) NOT NULL,
    "EntityStatus" integer NOT NULL,
    "RecordLastChangedUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."Diagnostics" OWNER TO geotabadapter_owner;

--
-- Name: ExceptionEvents; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."ExceptionEvents" (
    "Id" character varying(50) NOT NULL,
    "ActiveFrom" timestamp without time zone,
    "ActiveTo" timestamp without time zone,
    "DeviceId" character varying(50),
    "Distance" real,
    "DriverId" character varying(50),
    "Duration" interval,
    "RuleId" character varying(50),
    "Version" bigint,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."ExceptionEvents" OWNER TO geotabadapter_owner;

--
-- Name: FaultData; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."FaultData" (
    "Id" character varying(50) NOT NULL,
    "AmberWarningLamp" boolean,
    "ClassCode" character varying(50),
    "ControllerId" character varying(50) NOT NULL,
    "ControllerName" character varying(255),
    "Count" integer NOT NULL,
    "DateTime" timestamp(4) without time zone,
    "DeviceId" character varying(50) NOT NULL,
    "DiagnosticId" character varying(100) NOT NULL,
    "DismissDateTime" timestamp without time zone,
    "DismissUserId" character varying(50),
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
-- Name: LogRecords; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."LogRecords" (
    "Id" character varying(50) NOT NULL,
    "DateTime" timestamp without time zone NOT NULL,
    "DeviceId" character varying(50) NOT NULL,
    "Latitude" double precision DEFAULT 0 NOT NULL,
    "Longitude" double precision DEFAULT 0 NOT NULL,
    "Speed" real DEFAULT 0 NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."LogRecords" OWNER TO geotabadapter_owner;

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
    "Id" character varying(50) NOT NULL,
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
-- Name: StatusData; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."StatusData" (
    "Id" character varying(50) NOT NULL,
    "Data" double precision,
    "DateTime" timestamp without time zone,
    "DeviceId" character varying(50) NOT NULL,
    "DiagnosticId" character varying(100) NOT NULL,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."StatusData" OWNER TO geotabadapter_owner;

--
-- Name: Trips; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."Trips" (
    "Id" character varying(50) NOT NULL,
    "DeviceId" character varying(50) NOT NULL,
    "DriverId" character varying(50) NOT NULL,
    "Distance" real NOT NULL,
    "DrivingDuration" interval NOT NULL,
    "NextTripStart" timestamp without time zone NOT NULL,
    "Start" timestamp without time zone NOT NULL,
    "Stop" timestamp without time zone NOT NULL,
    "StopDuration" interval NOT NULL,
    "StopPointX" double precision,
    "StopPointY" double precision,
    "RecordCreationTimeUtc" timestamp without time zone NOT NULL
);


ALTER TABLE public."Trips" OWNER TO geotabadapter_owner;

--
-- Name: Users; Type: TABLE; Schema: public; Owner: geotabadapter_owner
--

CREATE TABLE public."Users" (
    "Id" character varying(50) NOT NULL,
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
-- Name: vwRuleObject; Type: VIEW; Schema: public; Owner: geotabadapter_owner
--

CREATE VIEW public."vwRuleObject" AS
 SELECT r."Id",
    r."ActiveFrom",
    r."ActiveTo",
    r."BaseType",
    r."Comment",
    r."Name",
    r."Version",
    r."EntityStatus",
    r."RecordLastChangedUtc",
    c."Id" AS "Cond_Id",
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
     JOIN public."Conditions" c ON (((r."Id")::text = (c."RuleId")::text)))
  ORDER BY r."Id", c."Id";


ALTER TABLE public."vwRuleObject" OWNER TO geotabadapter_owner;

--
-- Name: TABLE "Conditions"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Conditions" TO geotabadapter_client;


--
-- Name: TABLE "ConfigFeedVersions"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."ConfigFeedVersions" TO geotabadapter_client;


--
-- Name: TABLE "DVIRDefectRemarks"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DVIRDefectRemarks" TO geotabadapter_client;


--
-- Name: TABLE "DVIRDefects"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DVIRDefects" TO geotabadapter_client;


--
-- Name: TABLE "DVIRLogs"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."DVIRLogs" TO geotabadapter_client;


--
-- Name: TABLE "Devices"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Devices" TO geotabadapter_client;


--
-- Name: TABLE "Diagnostics"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Diagnostics" TO geotabadapter_client;


--
-- Name: TABLE "ExceptionEvents"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."ExceptionEvents" TO geotabadapter_client;


--
-- Name: TABLE "FaultData"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."FaultData" TO geotabadapter_client;


--
-- Name: TABLE "LogRecords"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."LogRecords" TO geotabadapter_client;


--
-- Name: TABLE "MyGeotabVersionInfo"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."MyGeotabVersionInfo" TO geotabadapter_client;


--
-- Name: TABLE "Rules"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Rules" TO geotabadapter_client;


--
-- Name: TABLE "StatusData"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,UPDATE ON TABLE public."StatusData" TO geotabadapter_client;


--
-- Name: TABLE "Trips"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Trips" TO geotabadapter_client;


--
-- Name: TABLE "Users"; Type: ACL; Schema: public; Owner: geotabadapter_owner
--

GRANT SELECT,INSERT,DELETE,UPDATE ON TABLE public."Users" TO geotabadapter_client;


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

