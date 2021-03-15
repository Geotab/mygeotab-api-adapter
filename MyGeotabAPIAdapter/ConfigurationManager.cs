using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Geotab.Checkmate.ObjectModel.Exceptions;
using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Reflection;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Reads (from appsettings.json) and stores application configuration settings.
    /// </summary>
    public class ConfigurationManager : IDisposable
    {
        static IConfiguration _configuration;

        // Argument Names for appsettings:
        // > DatabaseSettings
        const string ArgNameDatabaseProviderType = "DatabaseSettings:DatabaseProviderType";
        const string ArgNameDatabaseConnectionString = "DatabaseSettings:DatabaseConnectionString";
        // > LoginSettings
        const string ArgNameMyGeotabDatabase = "LoginSettings:MyGeotabDatabase";
        const string ArgNameMyGeotabPassword = "LoginSettings:MyGeotabPassword";
        const string ArgNameMyGeotabServer = "LoginSettings:MyGeotabServer";
        const string ArgNameMyGeotabUser = "LoginSettings:MyGeotabUser";
        // > AppSettings:GeneralSettings
        const string ArgNameTimeoutSecondsForDatabaseTasks = "AppSettings:GeneralSettings:TimeoutSecondsForDatabaseTasks";
        const string ArgNameTimeoutSecondsForMyGeotabTasks = "AppSettings:GeneralSettings:TimeoutSecondsForMyGeotabTasks";
        // > AppSettings:Caches:Controller
        const string ArgNameControllerCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:Controller:ControllerCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameControllerCacheUpdateIntervalMinutes = "AppSettings:Caches:Controller:ControllerCacheUpdateIntervalMinutes";
        const string ArgNameControllerCacheRefreshIntervalMinutes = "AppSettings:Caches:Controller:ControllerCacheRefreshIntervalMinutes";
        // > AppSettings:Caches:Device
        const string ArgNameDeviceCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:Device:DeviceCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameDeviceCacheUpdateIntervalMinutes = "AppSettings:Caches:Device:DeviceCacheUpdateIntervalMinutes";
        const string ArgNameDeviceCacheRefreshIntervalMinutes = "AppSettings:Caches:Device:DeviceCacheRefreshIntervalMinutes";
        // > AppSettings:Caches:Diagnostic
        const string ArgNameDiagnosticCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:Diagnostic:DiagnosticCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameDiagnosticCacheUpdateIntervalMinutes = "AppSettings:Caches:Diagnostic:DiagnosticCacheUpdateIntervalMinutes";
        const string ArgNameDiagnosticCacheRefreshIntervalMinutes = "AppSettings:Caches:Diagnostic:DiagnosticCacheRefreshIntervalMinutes";
        // > AppSettings:Caches:DVIRDefect
        const string ArgNameDVIRDefectCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:DVIRDefect:DVIRDefectListCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameDVIRDefectListCacheRefreshIntervalMinutes = "AppSettings:Caches:DVIRDefect:DVIRDefectListCacheRefreshIntervalMinutes";
        // > AppSettings:Caches:FailureMode
        const string ArgNameFailureModeCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:FailureMode:FailureModeCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameFailureModeCacheUpdateIntervalMinutes = "AppSettings:Caches:FailureMode:FailureModeCacheUpdateIntervalMinutes";
        const string ArgNameFailureModeCacheRefreshIntervalMinutes = "AppSettings:Caches:FailureMode:FailureModeCacheRefreshIntervalMinutes";
        // > AppSettings:Caches:Group
        const string ArgNameGroupCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:Group:GroupCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameGroupCacheUpdateIntervalMinutes = "AppSettings:Caches:Group:GroupCacheUpdateIntervalMinutes";
        const string ArgNameGroupCacheRefreshIntervalMinutes = "AppSettings:Caches:Group:GroupCacheRefreshIntervalMinutes";
        // > AppSettings:Caches:Rule
        const string ArgNameRuleCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:Rule:RuleCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameRuleCacheUpdateIntervalMinutes = "AppSettings:Caches:Rule:RuleCacheUpdateIntervalMinutes";
        const string ArgNameRuleCacheRefreshIntervalMinutes = "AppSettings:Caches:Rule:RuleCacheRefreshIntervalMinutes";
        // > AppSettings:Caches:UnitOfMeasure
        const string ArgNameUnitOfMeasureCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:UnitOfMeasure:UnitOfMeasureCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameUnitOfMeasureCacheUpdateIntervalMinutes = "AppSettings:Caches:UnitOfMeasure:UnitOfMeasureCacheUpdateIntervalMinutes";
        const string ArgNameUnitOfMeasureCacheRefreshIntervalMinutes = "AppSettings:Caches:UnitOfMeasure:UnitOfMeasureCacheRefreshIntervalMinutes";
        // > AppSettings:Caches:User
        const string ArgNameUserCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:User:UserCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameUserCacheRefreshIntervalMinutes = "AppSettings:Caches:User:UserCacheRefreshIntervalMinutes";
        const string ArgNameUserCacheUpdateIntervalMinutes = "AppSettings:Caches:User:UserCacheUpdateIntervalMinutes";
        // > AppSettings:Caches:Zone
        const string ArgNameZoneCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:Zone:ZoneCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameZoneCacheRefreshIntervalMinutes = "AppSettings:Caches:Zone:ZoneCacheRefreshIntervalMinutes";
        const string ArgNameZoneCacheUpdateIntervalMinutes = "AppSettings:Caches:Zone:ZoneCacheUpdateIntervalMinutes";
        // > AppSettings:Caches:ZoneType
        const string ArgNameZoneTypeCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:ZoneType:ZoneTypeCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameZoneTypeCacheRefreshIntervalMinutes = "AppSettings:Caches:ZoneType:ZoneTypeCacheRefreshIntervalMinutes";
        const string ArgNameZoneTypeCacheUpdateIntervalMinutes = "AppSettings:Caches:ZoneType:ZoneTypeCacheUpdateIntervalMinutes";
        // > AppSettings:GeneralFeedSettings
        const string ArgNameFeedStartOption = "AppSettings:GeneralFeedSettings:FeedStartOption";
        const string ArgNameFeedStartSpecificTimeUTC = "AppSettings:GeneralFeedSettings:FeedStartSpecificTimeUTC";
        public readonly string ArgNameDevicesToTrack = "AppSettings:GeneralFeedSettings:DevicesToTrack";
        public readonly string ArgNameDiagnosticsToTrack = "AppSettings:GeneralFeedSettings:DiagnosticsToTrack";
        // > AppSettings:Feeds:DutyStatusAvailability
        const string ArgNameEnableDutyStatusAvailabilityFeed = "AppSettings:Feeds:DutyStatusAvailability:EnableDutyStatusAvailabilityFeed";
        const string ArgNameDutyStatusAvailabilityFeedIntervalSeconds = "AppSettings:Feeds:DutyStatusAvailability:DutyStatusAvailabilityFeedIntervalSeconds";
        const string ArgNameDutyStatusAvailabilityFeedLastAccessDateCutoffDays = "AppSettings:Feeds:DutyStatusAvailability:DutyStatusAvailabilityFeedLastAccessDateCutoffDays";
        // > AppSettings:Feeds:DVIRLog
        const string ArgNameEnableDVIRLogFeed = "AppSettings:Feeds:DVIRLog:EnableDVIRLogFeed";
        const string ArgNameDVIRLogFeedIntervalSeconds = "AppSettings:Feeds:DVIRLog:DVIRLogFeedIntervalSeconds";
        // > AppSettings:Feeds:ExceptionEvent
        const string ArgNameEnableExceptionEventFeed = "AppSettings:Feeds:ExceptionEvent:EnableExceptionEventFeed";
        const string ArgNameExceptionEventFeedIntervalSeconds = "AppSettings:Feeds:ExceptionEvent:ExceptionEventFeedIntervalSeconds";
        const string ArgNameTrackZoneStops = "AppSettings:Feeds:ExceptionEvent:TrackZoneStops";
        // > AppSettings:Feeds:FaultData
        const string ArgNameEnableFaultDataFeed = "AppSettings:Feeds:FaultData:EnableFaultDataFeed";
        const string ArgNameFaultDataFeedIntervalSeconds = "AppSettings:Feeds:FaultData:FaultDataFeedIntervalSeconds";
        // > AppSettings:Feeds:LogRecord
        const string ArgNameEnableLogRecordFeed = "AppSettings:Feeds:LogRecord:EnableLogRecordFeed";
        const string ArgNameLogRecordFeedIntervalSeconds = "AppSettings:Feeds:LogRecord:LogRecordFeedIntervalSeconds";
        // > AppSettings:Feeds:StatusData
        const string ArgNameEnableStatusDataFeed = "AppSettings:Feeds:StatusData:EnableStatusDataFeed";
        const string ArgNameStatusDataFeedIntervalSeconds = "AppSettings:Feeds:StatusData:StatusDataFeedIntervalSeconds";
        // > AppSettings:Feeds:Trip
        const string ArgNameEnableTripFeed = "AppSettings:Feeds:Trip:EnableTripFeed";
        const string ArgNameTripFeedIntervalSeconds = "AppSettings:Feeds:Trip:TripFeedIntervalSeconds";
        // > AppSettings:Manipulators:DVIRLog
        const string ArgNameEnableDVIRLogManipulator = "AppSettings:Manipulators:DVIRLog:EnableDVIRLogManipulator";
        const string ArgNameDVIRLogManipulatorIntervalSeconds = "AppSettings:Manipulators:DVIRLog:DVIRLogManipulatorIntervalSeconds";


        // Arbitrary limits to prevent API abuse:
        // > Cache refresh
        const int MinCacheRefreshIntervalMinutes = 60;
        const int MaxCacheRefreshIntervalMinutes = 10080; // 10080 min = 1 wk
        const int DefaultCacheRefreshIntervalMinutes = 10080;
        // > Cache update
        const int MinCacheUpdateIntervalMinutes = 1;
        const int MaxCacheUpdateIntervalMinutes = 10080; // 10080 min = 1 wk
        const int DefaultCacheUpdateIntervalMinutes = 1440; // 1440 min = 1 day
        // > Feeds
        const int MinFeedIntervalSeconds = 2;
        const int MaxFeedIntervalSeconds = 604800; // 604800 sec = 1 wk
        const int DefaultFeedIntervalSeconds = 30;
        const int MinDutyStatusAvailabilityFeedLastAccessDateCutoffDays = 14;
        const int MaxDutyStatusAvailabilityFeedLastAccessDateCutoffDays = 60;
        const int DefaultDutyStatusAvailabilityFeedLastAccessDateCutoffDays = 30;

        // Arbitrary timeout limits:
        const int DefaultTimeoutSeconds = 30;
        const int MaxTimeoutSeconds = 120;
        const int MinTimeoutSeconds = 10;

        // Database table names:
        const string TableNameDbConfigFeedVersions = "ConfigFeedVersions";
        const string TableNameDbDevice = "Devices";
        const string TableNameDbDiagnostic = "Diagnostics";
        const string TableNameDbDutyStatusAvailability = "DutyStatusAvailability";
        const string TableNameDbDVIRDefectRemark = "DVIRDefectRemarks";
        const string TableNameDbDVIRDefectUpdates = "DVIRDefectUpdates";
        const string TableNameDbDVIRDefect = "DVIRDefects";
        const string TableNameDbDVIRLog = "DVIRLogs";
        const string TableNameDbExceptionEvent = "ExceptionEvents";
        const string TableNameDbFailedDVIRDefectUpdates = "FailedDVIRDefectUpdates";
        const string TableNameDbFaultData = "FaultData";
        const string TableNameDbLogRecord = "LogRecords";
        const string TableNameDbRule = "Rules";
        const string TableNameDbStatusData = "StatusData";
        const string TableNameDbTrip = "Trips";
        const string TableNameDbUser = "Users";
        const string TableNameDbZone = "Zones";
        const string TableNameDbZoneType = "ZoneTypes";

        // Miscellaneous:
        const string AllString = "*";
        const string MaskString = "************";

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        DateTime controllerCacheIntervalDailyReferenceStartTimeUTC;
        int controllerCacheRefreshIntervalMinutes;
        int controllerCacheUpdateIntervalMinutes;
        string databaseConnectionString;
        string databaseProviderType;
        DateTime deviceCacheIntervalDailyReferenceStartTimeUTC;
        int deviceCacheRefreshIntervalMinutes;
        int deviceCacheUpdateIntervalMinutes;
        string devicesToTrackList;
        DateTime diagnosticCacheIntervalDailyReferenceStartTimeUTC;
        int diagnosticCacheRefreshIntervalMinutes;
        int diagnosticCacheUpdateIntervalMinutes;
        string diagnosticsToTrackList;
        int dutyStatusAvailabilityFeedIntervalSeconds;
        int dutyStatusAvailabilityFeedLastAccessDateCutoffDays;
        DateTime dvirDefectCacheIntervalDailyReferenceStartTimeUTC;
        int dvirDefectListCacheRefreshIntervalMinutes;
        int dvirLogDataFeedIntervalSeconds;
        int dvirLogManipulatorIntervalSeconds;
        bool enableDutyStatusAvailabilityDataFeed;
        bool enableDVIRLogDataFeed;
        bool enableDVIRLogManipulator;
        bool enableExceptionEventFeed;
        bool enableFaultDataFeed;
        bool enableLogRecordFeed;
        bool enableStatusDataFeed;
        bool enableTripFeed;
        int exceptionEventFeedIntervalSeconds;
        DateTime failureModeCacheIntervalDailyReferenceStartTimeUTC;
        int failureModeCacheRefreshIntervalMinutes;
        int failureModeCacheUpdateIntervalMinutes;
        int faultDataFeedIntervalSeconds;
        Globals.FeedStartOption feedStartOption;
        DateTime feedStartSpecificTimeUTC;
        DateTime groupCacheIntervalDailyReferenceStartTimeUTC;
        int groupCacheRefreshIntervalMinutes;
        int groupCacheUpdateIntervalMinutes;
        int logRecordFeedIntervalSeconds;
        string myGeotabDatabase;
        string myGeotabPassword;
        string myGeotabServer;
        string myGeotabUser;
        DateTime ruleCacheIntervalDailyReferenceStartTimeUTC;
        int ruleCacheRefreshIntervalMinutes;
        int ruleCacheUpdateIntervalMinutes;
        int statusDataFeedIntervalSeconds;
        int timeoutSecondsForDatabaseTasks;
        int timeoutSecondsForMyGeotabTasks;
        bool trackZoneStops;
        int tripFeedIntervalSeconds;
        DateTime unitOfMeasureCacheIntervalDailyReferenceStartTimeUTC;
        int unitOfMeasureCacheRefreshIntervalMinutes;
        int unitOfMeasureCacheUpdateIntervalMinutes;
        DateTime userCacheIntervalDailyReferenceStartTimeUTC;
        int userCacheRefreshIntervalMinutes;
        int userCacheUpdateIntervalMinutes;
        DateTime zoneCacheIntervalDailyReferenceStartTimeUTC;
        int zoneCacheRefreshIntervalMinutes;
        int zoneCacheUpdateIntervalMinutes;
        DateTime zoneTypeCacheIntervalDailyReferenceStartTimeUTC;
        int zoneTypeCacheRefreshIntervalMinutes;
        int zoneTypeCacheUpdateIntervalMinutes;

        /// <summary>
        /// Creates a new <see cref="ConfigurationManager"/> instance.
        /// </summary>
        public ConfigurationManager()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Creates a new <see cref="ConfigurationManager"/> instance using the supplied <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
        public ConfigurationManager(IConfiguration configuration)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");
            Configuration = configuration;
            ProcessConfigItems();
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Add any clean-up code here.
            }
        }

        /// <summary>
        /// The <see cref="IConfiguration"/> used by this <see cref="ConfigurationManager"/> instance.
        /// </summary>
        public static IConfiguration Configuration { get => _configuration; set => _configuration = value; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="Controller"/> cache.
        /// </summary>
        public DateTime ControllerCacheIntervalDailyReferenceStartTimeUTC
        {
            get => controllerCacheIntervalDailyReferenceStartTimeUTC;
        }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="Controller"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        public int ControllerCacheRefreshIntervalMinutes
        {
            get => controllerCacheRefreshIntervalMinutes;
        }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="Controller"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        public int ControllerCacheUpdateIntervalMinutes
        {
            get => controllerCacheUpdateIntervalMinutes;
        }

        /// <summary>
        /// The connection string used to access the MyGeotabAPIAdapter database.
        /// </summary>
        public string DatabaseConnectionString
        {
            get => databaseConnectionString;
        }

        /// <summary>
        /// A string representation of the <see cref="MyGeotabAPIAdapter.Database.ConnectionInfo.DataAccessProviderType"/> to be used when creating <see cref="System.Data.IDbConnection"/> instances.
        /// </summary>
        public string DatabaseProviderType
        {
            get => databaseProviderType;
        }

        /// <summary>
        /// The name of the database table for data feed ToVersion information.
        /// </summary>
        public static string DbConfigFeedVersionsTableName
        {
            get => TableNameDbConfigFeedVersions;
        }

        /// <summary>
        /// The name of the database table for <see cref="Device"/> information.
        /// </summary>
        public static string DbDeviceTableName
        {
            get => TableNameDbDevice;
        }

        /// <summary>
        /// The name of the database table for <see cref="Diagnostic"/> information.
        /// </summary>
        public static string DbDiagnosticTableName
        {
            get => TableNameDbDiagnostic;
        }

        /// <summary>
        /// The name of the database table for <see cref="DutyStatusAvailability"/> information.
        /// </summary>
        public static string DbDutyStatusAvailabilityTableName
        {
            get => TableNameDbDutyStatusAvailability;
        }

        /// <summary>
        /// The name of the database table for <see cref="DVIRDefect"/> information.
        /// </summary>
        public static string DbDVIRDefectTableName
        {
            get => TableNameDbDVIRDefect;
        }

        /// <summary>
        /// The name of the database table for <see cref="DefectRemark"/> information.
        /// </summary>
        public static string DbDVIRDefectRemarkTableName
        {
            get => TableNameDbDVIRDefectRemark;
        }

        /// <summary>
        /// The name of the database table for <see cref="DVIRDefect"/> updates information.
        /// </summary>
        public static string DbDVIRDefectUpdatesTableName
        {
            get => TableNameDbDVIRDefectUpdates;
        }

        /// <summary>
        /// The name of the database table for <see cref="DVIRLog"/> information.
        /// </summary>
        public static string DbDVIRLogTableName
        {
            get => TableNameDbDVIRLog;
        }

        /// <summary>
        /// The name of the database table for <see cref="ExceptionEvent"/> information.
        /// </summary>
        public static string DbExceptionEventTableName
        {
            get => TableNameDbExceptionEvent;
        }

        /// <summary>
        /// The name of the database table for failed <see cref="DVIRDefect"/> updates information.
        /// </summary>
        public static string DbFailedDVIRDefectUpdatesTableName
        {
            get => TableNameDbFailedDVIRDefectUpdates;
        }

        /// <summary>
        /// The name of the database table for <see cref="FaultData"/> information.
        /// </summary>
        public static string DbFaultDataTableName
        {
            get => TableNameDbFaultData;
        }

        /// <summary>
        /// The name of the database table for <see cref="LogRecord"/> information.
        /// </summary>
        public static string DbLogRecordTableName
        {
            get => TableNameDbLogRecord;
        }

        /// <summary>
        /// The name of the database table for <see cref="Rule"/> information.
        /// </summary>
        public static string DbRuleTableName
        {
            get => TableNameDbRule;
        }

        /// <summary>
        /// The name of the database table for <see cref="StatusData"/> information.
        /// </summary>
        public static string DbStatusDataTableName
        {
            get => TableNameDbStatusData;
        }

        /// <summary>
        /// The name of the database table for <see cref="User"/> information.
        /// </summary>
        public static string DbTripTableName
        {
            get => TableNameDbTrip;
        }

        /// <summary>
        /// The name of the database table for <see cref="User"/> information.
        /// </summary>
        public static string DbUserTableName
        {
            get => TableNameDbUser;
        }

        /// <summary>
        /// The name of the database table for <see cref="Zone"/> information.
        /// </summary>
        public static string DbZoneTableName
        {
            get => TableNameDbZone;
        }

        /// <summary>
        /// The name of the database table for <see cref="ZoneType"/> information.
        /// </summary>
        public static string DbZoneTypeTableName
        {
            get => TableNameDbZoneType;
        }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="Device"/> cache.
        /// </summary>
        public DateTime DeviceCacheIntervalDailyReferenceStartTimeUTC
        {
            get => deviceCacheIntervalDailyReferenceStartTimeUTC;
        }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="Device"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        public int DeviceCacheRefreshIntervalMinutes
        {
            get => deviceCacheRefreshIntervalMinutes;
        }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="Device"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        public int DeviceCacheUpdateIntervalMinutes
        {
            get => deviceCacheUpdateIntervalMinutes;
        }

        /// <summary>
        /// The comma-separated list of <see cref="Id"/>s of of specific <see cref="Device"/> entities to track when utilizing data feeds.
        /// </summary>
        public string DevicesToTrackList
        {
            get => devicesToTrackList;
        }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="Diagnostic"/> cache.
        /// </summary>
        public DateTime DiagnosticCacheIntervalDailyReferenceStartTimeUTC
        {
            get => diagnosticCacheIntervalDailyReferenceStartTimeUTC;
        }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="Diagnostic"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        public int DiagnosticCacheRefreshIntervalMinutes
        {
            get => diagnosticCacheRefreshIntervalMinutes;
        }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="Diagnostic"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        public int DiagnosticCacheUpdateIntervalMinutes
        {
            get => diagnosticCacheUpdateIntervalMinutes;
        }

        /// <summary>
        /// The comma-separated list of <see cref="Id"/>s of of specific <see cref="Diagnostic"/> entities to track when utilizing data feeds.
        /// </summary>
        public string DiagnosticsToTrackList
        {
            get => diagnosticsToTrackList;
        }

        /// <summary>
        /// The minimum number of seconds to wait after retrieving <see cref="DutyStatusAvailability"/> information for all <see cref="Driver"/>s before starting the retrieval process again.
        /// </summary>
        public int DutyStatusAvailabilityFeedIntervalSeconds
        {
            get => dutyStatusAvailabilityFeedIntervalSeconds;
        }

        /// <summary>
        /// Used to reduce the number of unnecessary Get calls when retrieving <see cref="DutyStatusAvailability"/> information for all <see cref="Driver"/>s. Data is not queried for Drivers with a <see cref="User.LastAccessDate"/> greater than this many days in the past. This value should be set to approximately twice the longest possible cycle for a HOS ruleset.
        /// </summary>
        public int DutyStatusAvailabilityFeedLastAccessDateCutoffDays
        {
            get => dutyStatusAvailabilityFeedLastAccessDateCutoffDays; 
        }

        /// <summary>
        /// The minimum number of seconds to wait between iterations of the process that propagates <see cref="DVIRLog"/> changes from tables in the adapter database to the associated MyGeotab database.
        /// </summary>
        public int DVIRLogManipulatorIntervalSeconds
        {
            get => dvirLogManipulatorIntervalSeconds;
        }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="DVIRDefect"/> cache.
        /// </summary>
        public DateTime DVIRDefectCacheIntervalDailyReferenceStartTimeUTC
        {
            get => dvirDefectCacheIntervalDailyReferenceStartTimeUTC;
        }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="DVIRLog.DefectList"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        public int DVIRDefectListCacheRefreshIntervalMinutes
        {
            get => dvirDefectListCacheRefreshIntervalMinutes;
        }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="DVIRLog"/> objects.
        /// </summary>
        public int DVIRLogDataFeedIntervalSeconds
        {
            get => dvirLogDataFeedIntervalSeconds;
        }

        /// <summary>
        /// Indicates whether a <see cref="DutyStatusAvailability"/> data feed should be enabled. 
        /// </summary>
        public bool EnableDutyStatusAvailabilityDataFeed
        {
            get => enableDutyStatusAvailabilityDataFeed;
        }

        /// <summary>
        /// Indicates whether a <see cref="DVIRLog"/> data feed should be enabled. 
        /// </summary>
        public bool EnableDVIRLogDataFeed
        {
            get => enableDVIRLogDataFeed;
        }

        /// <summary>
        /// Indicates whether the <see cref="DVIRLogManipulator"/> worker service should be enabled. 
        /// </summary>
        public bool EnableDVIRLogManipulator
        {
            get => enableDVIRLogManipulator;
        }

        /// <summary>
        /// Indicates whether a <see cref="ExceptionEvent"/> data feed should be enabled. 
        /// </summary>
        public bool EnableExceptionEventFeed
        {
            get => enableExceptionEventFeed;
        }

        /// <summary>
        /// Indicates whether a <see cref="FaultData"/> data feed should be enabled. 
        /// </summary>
        public bool EnableFaultDataFeed
        {
            get => enableFaultDataFeed;
        }

        /// <summary>
        /// Indicates whether a <see cref="LogRecord"/> data feed should be enabled. 
        /// </summary>
        public bool EnableLogRecordFeed
        {
            get => enableLogRecordFeed;
        }

        /// <summary>
        /// Indicates whether a <see cref="StatusData"/> data feed should be enabled. 
        /// </summary>
        public bool EnableStatusDataFeed
        {
            get => enableStatusDataFeed;
        }

        /// <summary>
        /// Indicates whether a <see cref="Trip"/> data feed should be enabled. 
        /// </summary>
        public bool EnableTripFeed
        {
            get => enableTripFeed;
        }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="ExceptionEvent"/> objects.
        /// </summary>
        public int ExceptionEventFeedIntervalSeconds
        {
            get => exceptionEventFeedIntervalSeconds;
        }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="FailureMode"/> cache.
        /// </summary>
        public DateTime FailureModeCacheIntervalDailyReferenceStartTimeUTC
        {
            get => failureModeCacheIntervalDailyReferenceStartTimeUTC;
        }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="FailureMode"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        public int FailureModeCacheRefreshIntervalMinutes
        {
            get => failureModeCacheRefreshIntervalMinutes;
        }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="FailureMode"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        public int FailureModeCacheUpdateIntervalMinutes
        {
            get => failureModeCacheUpdateIntervalMinutes;
        }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="FaultData"/> objects.
        /// </summary>
        public int FaultDataFeedIntervalSeconds
        {
            get => faultDataFeedIntervalSeconds;
        }

        /// <summary>
        /// The <see cref="Common.FeedStartOption"/> to use for the initial GetFeed call for each data feed.
        /// </summary>
        public Globals.FeedStartOption FeedStartOption
        {
            get => feedStartOption;
            set => feedStartOption = value;
        }

        /// <summary>
        /// The specific <see cref="DateTime"/> to use for the initial GetFeed call for each data feed. Only used if <see cref="FeedStartOption"/> is set to <see cref="Common.FeedStartOption.SpecificTime"/>.
        /// </summary>
        public DateTime FeedStartSpecificTimeUTC
        {
            get => feedStartSpecificTimeUTC;        
        }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="Group"/> cache.
        /// </summary>
        public DateTime GroupCacheIntervalDailyReferenceStartTimeUTC
        {
            get => groupCacheIntervalDailyReferenceStartTimeUTC;
        }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="Group"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        public int GroupCacheRefreshIntervalMinutes
        {
            get => groupCacheRefreshIntervalMinutes;
        }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="Group"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        public int GroupCacheUpdateIntervalMinutes
        {
            get => groupCacheUpdateIntervalMinutes;
        }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="LogRecord"/> objects.
        /// </summary>
        public int LogRecordFeedIntervalSeconds
        { 
            get => logRecordFeedIntervalSeconds;
        }

        /// <summary>
        /// The MyGeotab Database to use with the <see cref="Geotab.Checkmate.API.AuthenticateAsync(System.Threading.CancellationToken)"> method. 
        /// </summary>
        public string MyGeotabDatabase
        {
            get => myGeotabDatabase;
        }

        /// <summary>
        /// The MyGeotab Password to use with the <see cref="Geotab.Checkmate.API.AuthenticateAsync(System.Threading.CancellationToken)"> method. 
        /// </summary>
        public string MyGeotabPassword
        {
            get => myGeotabPassword;
        }

        /// <summary>
        /// The MyGeotab Server to use with the <see cref="Geotab.Checkmate.API.AuthenticateAsync(System.Threading.CancellationToken)"> method. 
        /// </summary>
        public string MyGeotabServer
        {
            get => myGeotabServer;
        }

        /// <summary>
        /// The MyGeotab User to use with the <see cref="Geotab.Checkmate.API.AuthenticateAsync(System.Threading.CancellationToken)"> method. 
        /// </summary>
        public string MyGeotabUser
        {
            get => myGeotabUser;
        }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="Rule"/> cache.
        /// </summary>
        public DateTime RuleCacheIntervalDailyReferenceStartTimeUTC
        {
            get => ruleCacheIntervalDailyReferenceStartTimeUTC;
        }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="Rule"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        public int RuleCacheRefreshIntervalMinutes
        {
            get => ruleCacheRefreshIntervalMinutes;
        }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="Rule"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        public int RuleCacheUpdateIntervalMinutes
        {
            get => ruleCacheUpdateIntervalMinutes;
        }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="StatusData"/> objects.
        /// </summary>
        public int StatusDataFeedIntervalSeconds
        {
            get => statusDataFeedIntervalSeconds;
        }

        /// <summary>
        /// The maximum number of milliseconds that a database <see cref="System.Threading.Tasks.Task"/> or batch thereof can take to be completed before it is deemed that there is a database connectivity issue and a <see cref="Database.DatabaseConnectionException"/> will be thrown.
        /// </summary>
        public int TimeoutMillisecondsForDatabaseTasks
        {
            get => timeoutSecondsForDatabaseTasks * 1000;
        }

        /// <summary>
        /// The maximum number of milliseconds that a MyGeotab API call <see cref="System.Threading.Tasks.Task"/> or batch thereof can take to be completed before it is deemed that there is a MyGeotab connectivity issue and a <see cref="MyGeotabConnectionException"/> will be thrown.
        /// </summary>
        public int TimeoutMillisecondsForMyGeotabTasks
        {
            get => timeoutSecondsForMyGeotabTasks * 1000;
        }

        /// <summary>
        /// The maximum number of seconds that a database <see cref="System.Threading.Tasks.Task"/> or batch thereof can take to be completed before it is deemed that there is a database connectivity issue and a <see cref="Database.DatabaseConnectionException"/> will be thrown.
        /// </summary>
        public int TimeoutSecondsForDatabaseTasks
        {
            get => timeoutSecondsForDatabaseTasks;
        }

        /// <summary>
        /// The maximum number of seconds that a MyGeotab API call <see cref="System.Threading.Tasks.Task"/> or batch thereof can take to be completed before it is deemed that there is a MyGeotab connectivity issue and a <see cref="MyGeotabConnectionException"/> will be thrown.
        /// </summary>
        public int TimeoutSecondsForMyGeotabTasks
        {
            get => timeoutSecondsForMyGeotabTasks;
        }

        /// <summary>
        /// Indicates whether zone stops are to be tracked. If <c>false</c>, <see cref="Zone"/> objects will not be cached and <see cref="ExceptionEvent"/>s of the <see cref="ExceptionRuleBaseType.ZoneStop"/> type will be excluded when processing results of the <see cref="ExceptionEvent"/> feed.
        /// </summary>
        public bool TrackZoneStops
        {
            get => trackZoneStops;
        }

        /// <summary>
        /// Indicates whether all <see cref="Device"/> types are being tracked with respect to <see cref="StatusData"/> and <see cref="FaultData"/> feeds.
        /// </summary>
        public bool TrackAllDevices
        {
            get
            {
                if (devicesToTrackList == AllString)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Indicates whether all <see cref="Diagnostic"/> types are being tracked with respect to <see cref="StatusData"/> and <see cref="FaultData"/> feeds.
        /// </summary>
        public bool TrackAllDiagnostics
        {
            get 
            {
                if (diagnosticsToTrackList == AllString)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="Trip"/> objects.
        /// </summary>
        public int TripFeedIntervalSeconds
        {
            get => tripFeedIntervalSeconds;
        }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="UnitOfMeasure"/> cache.
        /// </summary>
        public DateTime UnitOfMeasureCacheIntervalDailyReferenceStartTimeUTC
        {
            get => unitOfMeasureCacheIntervalDailyReferenceStartTimeUTC;
        }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="UnitOfMeasure"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        public int UnitOfMeasureCacheRefreshIntervalMinutes
        {
            get => unitOfMeasureCacheRefreshIntervalMinutes;
        }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="UnitOfMeasure"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        public int UnitOfMeasureCacheUpdateIntervalMinutes
        {
            get => unitOfMeasureCacheUpdateIntervalMinutes;
        }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="User"/> cache.
        /// </summary>
        public DateTime UserCacheIntervalDailyReferenceStartTimeUTC
        {
            get => userCacheIntervalDailyReferenceStartTimeUTC;
        }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="User"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        public int UserCacheRefreshIntervalMinutes
        {
            get => userCacheRefreshIntervalMinutes;
        }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="User"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        public int UserCacheUpdateIntervalMinutes
        {
            get => userCacheUpdateIntervalMinutes;
        }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="Zone"/> cache.
        /// </summary>
        public DateTime ZoneCacheIntervalDailyReferenceStartTimeUTC
        {
            get => zoneCacheIntervalDailyReferenceStartTimeUTC;
        }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="Zone"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        public int ZoneCacheRefreshIntervalMinutes
        {
            get => zoneCacheRefreshIntervalMinutes;
        }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="Zone"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        public int ZoneCacheUpdateIntervalMinutes
        {
            get => zoneCacheUpdateIntervalMinutes;
        }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="ZoneType"/> cache.
        /// </summary>
        public DateTime ZoneTypeCacheIntervalDailyReferenceStartTimeUTC
        {
            get => zoneTypeCacheIntervalDailyReferenceStartTimeUTC;
        }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="ZoneType"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        public int ZoneTypeCacheRefreshIntervalMinutes
        {
            get => zoneTypeCacheRefreshIntervalMinutes;
        }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="ZoneType"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        public int ZoneTypeCacheUpdateIntervalMinutes
        {
            get => zoneTypeCacheUpdateIntervalMinutes;
        }

        /// <summary>
        /// Parses the appsettings.json configuration file for the boolean value associated with the key and section provided. 
        /// If no section is provided then the key is searched at the root level.
        /// </summary>
        /// <param name="keyString">Configuration key value</param>
        /// <param name="sectionString">Configuration section value</param>
        /// <param name="isMasked">Whether the return value should be masked or not in the log file output</param>
        /// <returns>Boolean value associated with the key & section submitted.</returns>
        public static bool GetConfigKeyValueBoolean(string keyString, string sectionString = "", bool isMasked = false)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            string valueString = GetConfigKeyValueString(keyString, sectionString, false);
            if (bool.TryParse(valueString, out bool configItemValueBool))
            {
                if (isMasked == true)
                {
                    logger.Info($">{keyString}: {MaskString}");
                }
                else
                {
                    logger.Info($">{keyString}: {configItemValueBool}");
                }
            }
            else
            {
                string errorMessage = $"The value of '{valueString}' provided for the '{keyString}' configuration item is not valid.";
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return configItemValueBool;
        }

        /// <summary>
        /// Parses the appsettings.json configuration file for the DateTime value associated with the key and section provided. 
        /// If no section is provided then the key is searched at the root level.
        /// </summary>
        /// <param name="keyString">Configuration key value</param>
        /// <param name="sectionString">Configuration section value</param>
        /// <param name="isMasked">Whether the return value should be masked or not in the log file output</param>
        /// <returns>DateTime value associated with the key & section submitted.</returns>
        public static DateTime GetConfigKeyValueDateTime(string keyString, string sectionString = "", bool isMasked = false)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            string configItemValueString = GetConfigKeyValueString(keyString, sectionString, false);
            if (DateTime.TryParse(configItemValueString, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime configItemValueDateTime))
            {
                if (isMasked == true)
                {
                    logger.Info($">{keyString}: {MaskString}");
                }
                else
                {
                    logger.Info($">{keyString}: {configItemValueString}");
                }
            }
            else
            {
                string errorMessage = $"The value of '{configItemValueString}' provided for the '{keyString}' configuration item is not valid.";
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return configItemValueDateTime;
        }

        /// <summary>
        /// Parses the appsettings.json configuration file for the integer value associated with the key and section provided. 
        /// If no section is provided then the key is searched at the root level.
        /// </summary>
        /// <param name="keyString">Configuration key value</param>
        /// <param name="sectionString">Configuration section value</param>
        /// <param name="isMasked">Whether the return value should be masked or not in the log file output</param>
        /// <param name="minimumAllowedValue">Minimum value allowed</param>
        /// <param name="maximumAllowedValue">Maximum value allowed</param>
        /// <param name="defaultValueIfOutsideRange">Value to return if the minimum and maximum boundaries are exceeded</param>
        /// <returns>Integer value associated with the key & section submitted.</returns>
        public static int GetConfigKeyValueInt(string keyString, string sectionString = "", bool isMasked = false, int minimumAllowedValue = int.MinValue, int maximumAllowedValue = int.MaxValue, int defaultValueIfOutsideRange = int.MinValue)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            string valueString = GetConfigKeyValueString(keyString, sectionString, false);

            if (int.TryParse(valueString, out int output))
            {
                // Perform validation.
                if (output < minimumAllowedValue || output > maximumAllowedValue)
                {
                    // Throw an ArgumentException if the supplied minimumAllowedValue is greater than the supplied maximumAllowedValue.
                    if (minimumAllowedValue > maximumAllowedValue)
                    {
                        string errorMessage = $"The value of '{minimumAllowedValue}' provided for the 'minimumAllowedValue' parameter is greater than the value of '{maximumAllowedValue}' provided for the 'maximumAllowedValue' parameter.";
                        logger.Error(errorMessage);
                        throw new ArgumentException(errorMessage);
                    }
                    // Throw an ArgumentException if the supplied defaultValueIfOutsideRange is less than the supplied minimumAllowedValue or greater than the supplied maximumAllowedValue.
                    if (defaultValueIfOutsideRange < minimumAllowedValue || defaultValueIfOutsideRange > maximumAllowedValue)
                    {
                        string errorMessage = $"The value of '{defaultValueIfOutsideRange}' provided for the 'defaultValueIfOutsideRange' is not between the value of '{minimumAllowedValue}' provided for the 'minimumAllowedValue' parameter and the value of '{maximumAllowedValue}' provided for the 'maximumAllowedValue' parameter.";
                        logger.Error(errorMessage);
                        throw new ArgumentException(errorMessage);
                    }
                    // If the value of the subject ConfigItem falls outside the allowed range, use the default value for the ConfigItem instead and log a warning message.
                    if (output < minimumAllowedValue || output > maximumAllowedValue)
                    {
                        string errorMessage = $"The value of '{output}' provided for the '{keyString}' configuration item is is not between the minimum allowed value of '{minimumAllowedValue}' and the maximum allowed value of '{maximumAllowedValue}'. {keyString} will be set to '{defaultValueIfOutsideRange}'.";
                        output = defaultValueIfOutsideRange;
                        logger.Warn(errorMessage);
                    }
                }
                // Log the ConfigItem value.
                if (isMasked == true)
                {
                    logger.Info($">{keyString}: {MaskString}");
                }
                else
                {
                    logger.Info($">{keyString}: {output}");
                }
            }
            else
            {
                string errorMessage = $"The value of '{valueString}' provided for the '{keyString}' configuration item is not valid.";
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return output;
        }

        /// <summary>
        /// Parses the appsettings.json configuration file for the string value associated with the key and section provided. 
        /// If no section is provided then the key is searched at the root level.
        /// </summary>
        /// <param name="keyString">Configuration key value</param>
        /// <param name="sectionString">Configuration section value</param>
        /// <param name="isLogged">Enable/Disable logging</param>
        /// <param name="isMasked">Whether the return value should be masked or not in the log file output</param>
        /// <returns>String value associated with the key and section submitted.</returns>
        public static string GetConfigKeyValueString(string keyString, string sectionString = "", bool isLogged = true, bool isMasked = false)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");
            string output;
            if (string.IsNullOrEmpty(sectionString))
            {
                // No section defined.
                output = Configuration[keyString];
            }
            else
            {
                // Key defined in a section.
                output = Configuration.GetSection(sectionString)[keyString];
            }
            if (string.IsNullOrEmpty(output))
            {
                string errorMessage = $"A required configuration item named '{keyString}' was not found in the configuration file.";
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
            if (isLogged == true)
            {
                if (isMasked == true)
                {
                    logger.Info($">{keyString}: {MaskString}");
                }
                else
                {
                    logger.Info($">{keyString}: {output}");
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return output;
        }

        /// <summary>
        /// Validates the configuration object values and uses those values to set properties of the <see cref="ConfigurationManager"/>.
        /// </summary>
        public void ProcessConfigItems()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");
            logger.Info($"Processing configuration items.");

            // Database:
            databaseConnectionString = GetConfigKeyValueString(ArgNameDatabaseConnectionString, null, true, true);
            databaseProviderType = GetConfigKeyValueString(ArgNameDatabaseProviderType);

            // MyGeotab:
            myGeotabServer = GetConfigKeyValueString(ArgNameMyGeotabServer);
            myGeotabDatabase = GetConfigKeyValueString(ArgNameMyGeotabDatabase);
            myGeotabUser = GetConfigKeyValueString(ArgNameMyGeotabUser);
            myGeotabPassword = GetConfigKeyValueString(ArgNameMyGeotabPassword, null, true, true);

            // General Feed-Related:
            string feedStartOptionString = GetConfigKeyValueString(ArgNameFeedStartOption);
            string errorMessage;
            switch (feedStartOptionString)
            {
                case nameof(Globals.FeedStartOption.CurrentTime):
                    feedStartOption = Globals.FeedStartOption.CurrentTime;
                    break;
                case nameof(Globals.FeedStartOption.FeedVersion):
                    feedStartOption = Globals.FeedStartOption.FeedVersion;
                    break;
                case nameof(Globals.FeedStartOption.SpecificTime):
                    string feedStartSpecificTimeUTCString = GetConfigKeyValueString(ArgNameFeedStartSpecificTimeUTC);
                    if (DateTime.TryParse(feedStartSpecificTimeUTCString, null, System.Globalization.DateTimeStyles.RoundtripKind, out feedStartSpecificTimeUTC) == false)
                    {
                        errorMessage = $"The value of '{feedStartSpecificTimeUTCString}' provided for the '{ArgNameFeedStartSpecificTimeUTC}' configuration item is not valid.";
                        logger.Error(errorMessage);
                        throw new Exception(errorMessage);
                    }
                    else
                    {
                        feedStartOption = Globals.FeedStartOption.SpecificTime;
                    }
                    break;
                default:
                    errorMessage = $"The value of '{feedStartOptionString}' provided for the '{ArgNameFeedStartOption}' configuration item is not valid.";
                    logger.Error(errorMessage);
                    throw new Exception(errorMessage);
            }
            devicesToTrackList = GetConfigKeyValueString(ArgNameDevicesToTrack);
            diagnosticsToTrackList = GetConfigKeyValueString(ArgNameDiagnosticsToTrack);

            // Cache:
            controllerCacheIntervalDailyReferenceStartTimeUTC
             = GetConfigKeyValueDateTime(ArgNameControllerCacheIntervalDailyReferenceStartTimeUTC);
            controllerCacheRefreshIntervalMinutes = GetConfigKeyValueInt(ArgNameControllerCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);
            controllerCacheUpdateIntervalMinutes = GetConfigKeyValueInt(ArgNameControllerCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);

            deviceCacheIntervalDailyReferenceStartTimeUTC = GetConfigKeyValueDateTime(ArgNameDeviceCacheIntervalDailyReferenceStartTimeUTC);
            deviceCacheRefreshIntervalMinutes = GetConfigKeyValueInt(ArgNameDeviceCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);
            deviceCacheUpdateIntervalMinutes = GetConfigKeyValueInt(ArgNameDeviceCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);

            diagnosticCacheIntervalDailyReferenceStartTimeUTC = GetConfigKeyValueDateTime(ArgNameDiagnosticCacheIntervalDailyReferenceStartTimeUTC);
            diagnosticCacheRefreshIntervalMinutes = GetConfigKeyValueInt(ArgNameDiagnosticCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);
            diagnosticCacheUpdateIntervalMinutes = GetConfigKeyValueInt(ArgNameDiagnosticCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);

            dvirDefectCacheIntervalDailyReferenceStartTimeUTC = GetConfigKeyValueDateTime(ArgNameDVIRDefectCacheIntervalDailyReferenceStartTimeUTC);
            dvirDefectListCacheRefreshIntervalMinutes = GetConfigKeyValueInt(ArgNameDVIRDefectListCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes);

            failureModeCacheIntervalDailyReferenceStartTimeUTC = GetConfigKeyValueDateTime(ArgNameFailureModeCacheIntervalDailyReferenceStartTimeUTC);
            failureModeCacheRefreshIntervalMinutes = GetConfigKeyValueInt(ArgNameFailureModeCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);
            failureModeCacheUpdateIntervalMinutes = GetConfigKeyValueInt(ArgNameFailureModeCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);

            groupCacheIntervalDailyReferenceStartTimeUTC = GetConfigKeyValueDateTime(ArgNameGroupCacheIntervalDailyReferenceStartTimeUTC);
            groupCacheRefreshIntervalMinutes = GetConfigKeyValueInt(ArgNameGroupCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);
            groupCacheUpdateIntervalMinutes = GetConfigKeyValueInt(ArgNameGroupCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);

            ruleCacheIntervalDailyReferenceStartTimeUTC = GetConfigKeyValueDateTime(ArgNameRuleCacheIntervalDailyReferenceStartTimeUTC);
            ruleCacheRefreshIntervalMinutes = GetConfigKeyValueInt(ArgNameRuleCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);
            ruleCacheUpdateIntervalMinutes = GetConfigKeyValueInt(ArgNameRuleCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);

            unitOfMeasureCacheIntervalDailyReferenceStartTimeUTC = GetConfigKeyValueDateTime(ArgNameUnitOfMeasureCacheIntervalDailyReferenceStartTimeUTC);
            unitOfMeasureCacheRefreshIntervalMinutes = GetConfigKeyValueInt(ArgNameUnitOfMeasureCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);
            unitOfMeasureCacheUpdateIntervalMinutes = GetConfigKeyValueInt(ArgNameUnitOfMeasureCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);

            userCacheIntervalDailyReferenceStartTimeUTC = GetConfigKeyValueDateTime(ArgNameUserCacheIntervalDailyReferenceStartTimeUTC);
            userCacheRefreshIntervalMinutes = GetConfigKeyValueInt(ArgNameUserCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);
            userCacheUpdateIntervalMinutes = GetConfigKeyValueInt(ArgNameUserCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);

            zoneCacheIntervalDailyReferenceStartTimeUTC = GetConfigKeyValueDateTime(ArgNameZoneCacheIntervalDailyReferenceStartTimeUTC);
            zoneCacheRefreshIntervalMinutes = GetConfigKeyValueInt(ArgNameZoneCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);
            zoneCacheUpdateIntervalMinutes = GetConfigKeyValueInt(ArgNameZoneCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);

            zoneTypeCacheIntervalDailyReferenceStartTimeUTC = GetConfigKeyValueDateTime(ArgNameZoneTypeCacheIntervalDailyReferenceStartTimeUTC);
            zoneTypeCacheRefreshIntervalMinutes = GetConfigKeyValueInt(ArgNameZoneTypeCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);
            zoneTypeCacheUpdateIntervalMinutes = GetConfigKeyValueInt(ArgNameZoneTypeCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);

            // Feed:
            enableDutyStatusAvailabilityDataFeed = GetConfigKeyValueBoolean(ArgNameEnableDutyStatusAvailabilityFeed);
            dutyStatusAvailabilityFeedIntervalSeconds = GetConfigKeyValueInt(ArgNameDutyStatusAvailabilityFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);
            dutyStatusAvailabilityFeedLastAccessDateCutoffDays = GetConfigKeyValueInt(ArgNameDutyStatusAvailabilityFeedLastAccessDateCutoffDays, null, false, MinDutyStatusAvailabilityFeedLastAccessDateCutoffDays, MaxDutyStatusAvailabilityFeedLastAccessDateCutoffDays, DefaultDutyStatusAvailabilityFeedLastAccessDateCutoffDays);

            enableDVIRLogDataFeed = GetConfigKeyValueBoolean(ArgNameEnableDVIRLogFeed);
            dvirLogDataFeedIntervalSeconds = GetConfigKeyValueInt(ArgNameDVIRLogFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            enableExceptionEventFeed = GetConfigKeyValueBoolean(ArgNameEnableExceptionEventFeed);
            exceptionEventFeedIntervalSeconds = GetConfigKeyValueInt(ArgNameExceptionEventFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            enableFaultDataFeed = GetConfigKeyValueBoolean(ArgNameEnableFaultDataFeed);
            faultDataFeedIntervalSeconds = GetConfigKeyValueInt(ArgNameFaultDataFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            enableLogRecordFeed = GetConfigKeyValueBoolean(ArgNameEnableLogRecordFeed);
            logRecordFeedIntervalSeconds = GetConfigKeyValueInt(ArgNameLogRecordFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            enableStatusDataFeed = GetConfigKeyValueBoolean(ArgNameEnableStatusDataFeed);
            statusDataFeedIntervalSeconds = GetConfigKeyValueInt(ArgNameStatusDataFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            enableTripFeed = GetConfigKeyValueBoolean(ArgNameEnableTripFeed);
            tripFeedIntervalSeconds = GetConfigKeyValueInt(ArgNameTripFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            // ZoneStop tracking:
            trackZoneStops = GetConfigKeyValueBoolean(ArgNameTrackZoneStops);

            // Manipulators:
            enableDVIRLogManipulator = GetConfigKeyValueBoolean(ArgNameEnableDVIRLogManipulator);
            dvirLogManipulatorIntervalSeconds = GetConfigKeyValueInt(ArgNameDVIRLogManipulatorIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            // Timeouts:
            timeoutSecondsForDatabaseTasks = GetConfigKeyValueInt(ArgNameTimeoutSecondsForDatabaseTasks, null, false, MinTimeoutSeconds, MaxTimeoutSeconds, DefaultTimeoutSeconds);
            timeoutSecondsForMyGeotabTasks = GetConfigKeyValueInt(ArgNameTimeoutSecondsForMyGeotabTasks, null, false, MinTimeoutSeconds, MaxTimeoutSeconds, DefaultTimeoutSeconds);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
