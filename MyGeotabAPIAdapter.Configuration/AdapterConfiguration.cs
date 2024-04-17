using NLog;
using System;
using System.Linq;
using System.Reflection;

namespace MyGeotabAPIAdapter.Configuration
{
    /// <summary>
    /// A class that reads (from appsettings.json) and stores application configuration settings. Intended for association with the MyGeotabAPIAdapter project.
    /// </summary>
    public class AdapterConfiguration : IAdapterConfiguration
    {
        const string WildcardString = "*";

        // Argument Names for appsettings:
        // > OverrideSetings
        const string ArgNameDisableMachineNameValidation = "OverrideSetings:DisableMachineNameValidation";
        // > DatabaseSettings
        const string ArgNameDatabaseProviderType = "DatabaseSettings:DatabaseProviderType";
        const string ArgNameDatabaseConnectionString = "DatabaseSettings:DatabaseConnectionString";
        // > LoginSettings
        const string ArgNameMyGeotabServer = "LoginSettings:MyGeotabServer";
        const string ArgNameMyGeotabDatabase = "LoginSettings:MyGeotabDatabase";
        const string ArgNameMyGeotabUser = "LoginSettings:MyGeotabUser";
        const string ArgNameMyGeotabPassword = "LoginSettings:MyGeotabPassword";
        // > AppSettings:GeneralSettings
        const string ArgNameTimeoutSecondsForDatabaseTasks = "AppSettings:GeneralSettings:TimeoutSecondsForDatabaseTasks";
        const string ArgNameTimeoutSecondsForMyGeotabTasks = "AppSettings:GeneralSettings:TimeoutSecondsForMyGeotabTasks";
        // > AppSettings:Caches:Controller
        const string ArgNameEnableControllerCache = "AppSettings:Caches:Controller:EnableControllerCache";
        const string ArgNameControllerCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:Controller:ControllerCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameControllerCacheUpdateIntervalMinutes = "AppSettings:Caches:Controller:ControllerCacheUpdateIntervalMinutes";
        const string ArgNameControllerCacheRefreshIntervalMinutes = "AppSettings:Caches:Controller:ControllerCacheRefreshIntervalMinutes";
        // > AppSettings:Caches:Device
        const string ArgNameEnableDeviceCache = "AppSettings:Caches:Device:EnableDeviceCache";
        const string ArgNameDeviceCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:Device:DeviceCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameDeviceCacheUpdateIntervalMinutes = "AppSettings:Caches:Device:DeviceCacheUpdateIntervalMinutes";
        const string ArgNameDeviceCacheRefreshIntervalMinutes = "AppSettings:Caches:Device:DeviceCacheRefreshIntervalMinutes";
        // > AppSettings:Caches:Diagnostic
        const string ArgNameEnableDiagnosticCache = "AppSettings:Caches:Diagnostic:EnableDiagnosticCache";
        const string ArgNameDiagnosticCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:Diagnostic:DiagnosticCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameDiagnosticCacheUpdateIntervalMinutes = "AppSettings:Caches:Diagnostic:DiagnosticCacheUpdateIntervalMinutes";
        const string ArgNameDiagnosticCacheRefreshIntervalMinutes = "AppSettings:Caches:Diagnostic:DiagnosticCacheRefreshIntervalMinutes";
        // > AppSettings:Caches:DVIRDefect
        const string ArgNameDVIRDefectListCacheRefreshIntervalMinutes = "AppSettings:Caches:DVIRDefect:DVIRDefectListCacheRefreshIntervalMinutes";
        // > AppSettings:Caches:FailureMode
        const string ArgNameEnableFailureModeCache = "AppSettings:Caches:FailureMode:EnableFailureModeCache";
        const string ArgNameFailureModeCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:FailureMode:FailureModeCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameFailureModeCacheUpdateIntervalMinutes = "AppSettings:Caches:FailureMode:FailureModeCacheUpdateIntervalMinutes";
        const string ArgNameFailureModeCacheRefreshIntervalMinutes = "AppSettings:Caches:FailureMode:FailureModeCacheRefreshIntervalMinutes";
        // > AppSettings:Caches:Group
        const string ArgNameEnableGroupCache = "AppSettings:Caches:Group:EnableGroupCache";
        const string ArgNameGroupCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:Group:GroupCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameGroupCacheUpdateIntervalMinutes = "AppSettings:Caches:Group:GroupCacheUpdateIntervalMinutes";
        const string ArgNameGroupCacheRefreshIntervalMinutes = "AppSettings:Caches:Group:GroupCacheRefreshIntervalMinutes";
        // > AppSettings:Caches:Rule
        const string ArgNameEnableRuleCache = "AppSettings:Caches:Rule:EnableRuleCache";
        const string ArgNameRuleCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:Rule:RuleCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameRuleCacheUpdateIntervalMinutes = "AppSettings:Caches:Rule:RuleCacheUpdateIntervalMinutes";
        const string ArgNameRuleCacheRefreshIntervalMinutes = "AppSettings:Caches:Rule:RuleCacheRefreshIntervalMinutes";
        // > AppSettings:Caches:UnitOfMeasure
        const string ArgNameEnableUnitOfMeasureCache = "AppSettings:Caches:UnitOfMeasure:EnableUnitOfMeasureCache";
        const string ArgNameUnitOfMeasureCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:UnitOfMeasure:UnitOfMeasureCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameUnitOfMeasureCacheUpdateIntervalMinutes = "AppSettings:Caches:UnitOfMeasure:UnitOfMeasureCacheUpdateIntervalMinutes";
        const string ArgNameUnitOfMeasureCacheRefreshIntervalMinutes = "AppSettings:Caches:UnitOfMeasure:UnitOfMeasureCacheRefreshIntervalMinutes";
        // > AppSettings:Caches:User
        const string ArgNameEnableUserCache = "AppSettings:Caches:User:EnableUserCache";
        const string ArgNameUserCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:User:UserCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameUserCacheRefreshIntervalMinutes = "AppSettings:Caches:User:UserCacheRefreshIntervalMinutes";
        const string ArgNameUserCacheUpdateIntervalMinutes = "AppSettings:Caches:User:UserCacheUpdateIntervalMinutes";
        // > AppSettings:Caches:Zone
        const string ArgNameEnableZoneCache = "AppSettings:Caches:Zone:EnableZoneCache";
        const string ArgNameZoneCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:Zone:ZoneCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameZoneCacheRefreshIntervalMinutes = "AppSettings:Caches:Zone:ZoneCacheRefreshIntervalMinutes";
        const string ArgNameZoneCacheUpdateIntervalMinutes = "AppSettings:Caches:Zone:ZoneCacheUpdateIntervalMinutes";
        // > AppSettings:Caches:ZoneType
        const string ArgNameEnableZoneTypeCache = "AppSettings:Caches:ZoneType:EnableZoneTypeCache";
        const string ArgNameZoneTypeCacheIntervalDailyReferenceStartTimeUTC = "AppSettings:Caches:ZoneType:ZoneTypeCacheIntervalDailyReferenceStartTimeUTC";
        const string ArgNameZoneTypeCacheRefreshIntervalMinutes = "AppSettings:Caches:ZoneType:ZoneTypeCacheRefreshIntervalMinutes";
        const string ArgNameZoneTypeCacheUpdateIntervalMinutes = "AppSettings:Caches:ZoneType:ZoneTypeCacheUpdateIntervalMinutes";
        // > AppSettings:GeneralFeedSettings
        const string ArgNameFeedStartOption = "AppSettings:GeneralFeedSettings:FeedStartOption";
        const string ArgNameFeedStartSpecificTimeUTC = "AppSettings:GeneralFeedSettings:FeedStartSpecificTimeUTC";
        const string ArgNameDevicesToTrack = "AppSettings:GeneralFeedSettings:DevicesToTrack";
        const string ArgNameDiagnosticsToTrack = "AppSettings:GeneralFeedSettings:DiagnosticsToTrack";
        const string ArgNameExcludeDiagnosticsToTrack = "AppSettings:GeneralFeedSettings:ExcludeDiagnosticsToTrack";
        const string ArgNameEnableMinimunIntervalSamplingForLogRecords = "AppSettings:GeneralFeedSettings:EnableMinimunIntervalSamplingForLogRecords";
        const string ArgNameEnableMinimunIntervalSamplingForStatusData = "AppSettings:GeneralFeedSettings:EnableMinimunIntervalSamplingForStatusData";
        const string ArgNameMinimumIntervalSamplingDiagnostics = "AppSettings:GeneralFeedSettings:MinimumIntervalSamplingDiagnostics";
        const string ArgNameMinimumIntervalSamplingIntervalSeconds = "AppSettings:GeneralFeedSettings:MinimumIntervalSamplingIntervalSeconds";
        // > AppSettings:Feeds:BinaryData
        const string ArgNameEnableBinaryDataFeed = "AppSettings:Feeds:BinaryData:EnableBinaryDataFeed";
        const string ArgNameBinaryDataFeedIntervalSeconds = "AppSettings:Feeds:BinaryData:BinaryDataFeedIntervalSeconds";
        // > AppSettings:Feeds:ChargeEvent
        const string ArgNameEnableChargeEventFeed = "AppSettings:Feeds:ChargeEvent:EnableChargeEventFeed";
        const string ArgNameChargeEventFeedIntervalSeconds = "AppSettings:Feeds:ChargeEvent:ChargeEventFeedIntervalSeconds";
        // > AppSettings:Feeds:DebugData
        const string ArgNameEnableDebugDataFeed = "AppSettings:Feeds:DebugData:EnableDebugDataFeed";
        const string ArgNameDebugDataFeedIntervalSeconds = "AppSettings:Feeds:DebugData:DebugDataFeedIntervalSeconds";
        // > AppSettings:Feeds:DeviceStatusInfo
        const string ArgNameEnableDeviceStatusInfoFeed = "AppSettings:Feeds:DeviceStatusInfo:EnableDeviceStatusInfoFeed";
        const string ArgNameDeviceStatusInfoFeedIntervalSeconds = "AppSettings:Feeds:DeviceStatusInfo:DeviceStatusInfoFeedIntervalSeconds";
        // > AppSettings:Feeds:DriverChange
        const string ArgNameEnableDriverChangeFeed = "AppSettings:Feeds:DriverChange:EnableDriverChangeFeed";
        const string ArgNameDriverChangeFeedIntervalSeconds = "AppSettings:Feeds:DriverChange:DriverChangeFeedIntervalSeconds";
        // > AppSettings:Feeds:DutyStatusAvailability
        const string ArgNameEnableDutyStatusAvailabilityFeed = "AppSettings:Feeds:DutyStatusAvailability:EnableDutyStatusAvailabilityFeed";
        const string ArgNameDutyStatusAvailabilityFeedIntervalSeconds = "AppSettings:Feeds:DutyStatusAvailability:DutyStatusAvailabilityFeedIntervalSeconds";
        const string ArgNameDutyStatusAvailabilityFeedLastAccessDateCutoffDays = "AppSettings:Feeds:DutyStatusAvailability:DutyStatusAvailabilityFeedLastAccessDateCutoffDays";
        // > AppSettings:Feeds:DutyStatusLog
        const string ArgNameEnableDutyStatusLogFeed = "AppSettings:Feeds:DutyStatusLog:EnableDutyStatusLogFeed";
        const string ArgNameDutyStatusLogFeedIntervalSeconds = "AppSettings:Feeds:DutyStatusLog:DutyStatusLogFeedIntervalSeconds";
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
        const int MinSamplingIntervalSeconds = 1;
        const int MaxSamplingIntervalSeconds = 3600; // 3600 sec = 1 hr
        const int DefaultSamplingIntervalSeconds = 300;

        // Arbitrary timeout limits:
        const int DefaultTimeoutSeconds = 30;
        const int MinTimeoutSeconds = 10;
        const int MaxTimeoutSeconds = 10800;
        
        /// <inheritdoc/>
        public int BinaryDataFeedIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public int ChargeEventFeedIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public DateTime ControllerCacheIntervalDailyReferenceStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int ControllerCacheRefreshIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public int ControllerCacheUpdateIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public string DatabaseConnectionString { get; private set; }

        /// <inheritdoc/>
        public string DatabaseProviderType { get; private set; }

        /// <inheritdoc/>
        public int DebugDataFeedIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public DateTime DeviceCacheIntervalDailyReferenceStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int DeviceCacheRefreshIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public int DeviceCacheUpdateIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public int DeviceStatusInfoFeedIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public string DevicesToTrackList { get; private set; }

        /// <inheritdoc/>
        public DateTime DiagnosticCacheIntervalDailyReferenceStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int DiagnosticCacheRefreshIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public int DiagnosticCacheUpdateIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public string DiagnosticsToTrackList { get; private set; }

        /// <inheritdoc/>
        public bool DisableMachineNameValidation { get; private set; }

        /// <inheritdoc/>
        public int DriverChangeFeedIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public int DutyStatusAvailabilityFeedIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public int DutyStatusAvailabilityFeedLastAccessDateCutoffDays { get; private set; }

        /// <inheritdoc/>
        public int DutyStatusLogFeedIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public DateTime DVIRDefectListCacheIntervalDailyReferenceStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int DVIRDefectListCacheRefreshIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public int DVIRLogFeedIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public int DVIRLogManipulatorIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public bool EnableBinaryDataFeed { get; private set; }

        /// <inheritdoc/>
        public bool EnableChargeEventFeed { get; private set; }

        /// <inheritdoc/>
        public bool EnableControllerCache { get; private set; }

        /// <inheritdoc/>
        public bool EnableDebugDataFeed { get; private set; }

        /// <inheritdoc/>
        public bool EnableDeviceCache { get; private set; }

        /// <inheritdoc/>
        public bool EnableDeviceStatusInfoFeed { get; private set; }

        /// <inheritdoc/>
        public bool EnableDiagnosticCache { get; private set; }

        /// <inheritdoc/>
        public bool EnableDriverChangeFeed { get; private set; }

        /// <inheritdoc/>
        public bool EnableDutyStatusAvailabilityFeed { get; private set; }

        /// <inheritdoc/>
        public bool EnableDutyStatusLogFeed { get; private set; }

        /// <inheritdoc/>
        public bool EnableDVIRDefectCache { get; private set; }

        /// <inheritdoc/>
        public bool EnableDVIRLogFeed { get; private set; }

        /// <inheritdoc/>
        public bool EnableDVIRLogManipulator { get; private set; }

        /// <inheritdoc/>
        public bool EnableExceptionEventFeed { get; private set; }

        /// <inheritdoc/>
        public bool EnableFailureModeCache { get; private set; }

        /// <inheritdoc/>
        public bool EnableFaultDataFeed { get; private set; }

        /// <inheritdoc/>
        public bool EnableGroupCache { get; private set; }

        /// <inheritdoc/>
        public bool EnableLogRecordFeed { get; private set; }

        /// <inheritdoc/>
        public bool EnableMinimunIntervalSamplingForLogRecords { get; private set; }

        /// <inheritdoc/>
        public bool EnableMinimunIntervalSamplingForStatusData { get; private set; }

        /// <inheritdoc/>
        public bool EnableRuleCache { get; private set; }

        /// <inheritdoc/>
        public bool EnableStatusDataFeed { get; private set; }

        /// <inheritdoc/>
        public bool EnableTripFeed { get; private set; }

        /// <inheritdoc/>
        public bool EnableUnitOfMeasureCache { get; private set; }

        /// <inheritdoc/>
        public bool EnableUserCache { get; private set; }

        /// <inheritdoc/>
        public bool EnableZoneCache { get; private set; }

        /// <inheritdoc/>
        public bool EnableZoneTypeCache { get; private set; }

        /// <inheritdoc/>
        public int ExceptionEventFeedIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public bool ExcludeDiagnosticsToTrack { get; private set; }

        /// <inheritdoc/>
        public DateTime FailureModeCacheIntervalDailyReferenceStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int FailureModeCacheRefreshIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public int FailureModeCacheUpdateIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public int FaultDataFeedIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public FeedStartOption FeedStartOption { get; private set; }

        /// <inheritdoc/>
        public DateTime FeedStartSpecificTimeUTC { get; private set; }

        /// <inheritdoc/>
        public DateTime GroupCacheIntervalDailyReferenceStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int GroupCacheRefreshIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public int GroupCacheUpdateIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <inheritdoc/>
        public int LogRecordFeedIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public string MinimumIntervalSamplingDiagnosticsList { get; private set; }

        /// <inheritdoc/>
        public int MinimumIntervalSamplingIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public string MyGeotabDatabase { get; private set; }

        /// <inheritdoc/>
        public string MyGeotabPassword { get; private set; }

        /// <inheritdoc/>
        public string MyGeotabServer { get; private set; }

        /// <inheritdoc/>
        public string MyGeotabUser { get; private set; }

        /// <inheritdoc/>
        public DateTime RuleCacheIntervalDailyReferenceStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int RuleCacheRefreshIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public int RuleCacheUpdateIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public int StatusDataFeedIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public int TimeoutSecondsForDatabaseTasks { get; private set; }

        /// <inheritdoc/>
        public int TimeoutSecondsForMyGeotabTasks { get; private set; }

        /// <inheritdoc/>
        public bool TrackZoneStops { get; private set; }

        /// <inheritdoc/>
        public int TripFeedIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public DateTime UnitOfMeasureCacheIntervalDailyReferenceStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int UnitOfMeasureCacheRefreshIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public int UnitOfMeasureCacheUpdateIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public DateTime UserCacheIntervalDailyReferenceStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int UserCacheRefreshIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public int UserCacheUpdateIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public DateTime ZoneCacheIntervalDailyReferenceStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int ZoneCacheRefreshIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public int ZoneCacheUpdateIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public DateTime ZoneTypeCacheIntervalDailyReferenceStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int ZoneTypeCacheRefreshIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public int ZoneTypeCacheUpdateIntervalMinutes { get; private set; }

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IConfigurationHelper configurationHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterConfiguration"/> class.
        /// </summary>
        public AdapterConfiguration(IConfigurationHelper configurationHelper)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.configurationHelper = configurationHelper;
            ProcessConfigItems();

            Id = Guid.NewGuid().ToString();
            logger.Debug($"{nameof(AdapterConfiguration)} [Id: {Id}] created.");
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public void ProcessConfigItems()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");
            logger.Info($"Processing configuration items.");

            // OverrideSetings:
            DisableMachineNameValidation = configurationHelper.GetConfigKeyValueBoolean(ArgNameDisableMachineNameValidation);
            if (DisableMachineNameValidation == true)
            {
                logger.Warn($"WARNING: Machine name validation has been disabled. This should only be done in cases where the application is installed in hosted environments where machine names are not static. Improper use of this setting may lead to application instability and data integrity issues.");
            }

            // DatabaseSettings:AdapterDatabase:
            DatabaseConnectionString = configurationHelper.GetConfigKeyValueString(ArgNameDatabaseConnectionString, null, true, true);
            DatabaseProviderType = configurationHelper.GetConfigKeyValueString(ArgNameDatabaseProviderType);

            // LoginSettings:
            MyGeotabServer = configurationHelper.GetConfigKeyValueString(ArgNameMyGeotabServer);
            MyGeotabDatabase = configurationHelper.GetConfigKeyValueString(ArgNameMyGeotabDatabase);
            MyGeotabUser = configurationHelper.GetConfigKeyValueString(ArgNameMyGeotabUser);
            MyGeotabPassword = configurationHelper.GetConfigKeyValueString(ArgNameMyGeotabPassword, null, true, true);

            // AppSettings:GeneralSettings:
            TimeoutSecondsForDatabaseTasks = configurationHelper.GetConfigKeyValueInt(ArgNameTimeoutSecondsForDatabaseTasks, null, false, MinTimeoutSeconds, MaxTimeoutSeconds, DefaultTimeoutSeconds);
            TimeoutSecondsForMyGeotabTasks = configurationHelper.GetConfigKeyValueInt(ArgNameTimeoutSecondsForMyGeotabTasks, null, false, MinTimeoutSeconds, MaxTimeoutSeconds, DefaultTimeoutSeconds);

            // AppSettings:Caches:Controller:
            EnableControllerCache = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableControllerCache);
            ControllerCacheIntervalDailyReferenceStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameControllerCacheIntervalDailyReferenceStartTimeUTC);
            ControllerCacheUpdateIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameControllerCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);
            ControllerCacheRefreshIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameControllerCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);

            // AppSettings:Caches:Device:
            EnableDeviceCache = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDeviceCache);
            DeviceCacheIntervalDailyReferenceStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameDeviceCacheIntervalDailyReferenceStartTimeUTC);
            DeviceCacheUpdateIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameDeviceCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);
            DeviceCacheRefreshIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameDeviceCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);

            // AppSettings:Caches:Diagnostic:
            EnableDiagnosticCache = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDiagnosticCache);
            DiagnosticCacheIntervalDailyReferenceStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameDiagnosticCacheIntervalDailyReferenceStartTimeUTC);
            DiagnosticCacheUpdateIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameDiagnosticCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);
            DiagnosticCacheRefreshIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameDiagnosticCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);

            // AppSettings:Caches:DVIRDefect:
            DVIRDefectListCacheRefreshIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameDVIRDefectListCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);

            // AppSettings:Caches:FailureMode:
            EnableFailureModeCache = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableFailureModeCache);
            FailureModeCacheIntervalDailyReferenceStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameFailureModeCacheIntervalDailyReferenceStartTimeUTC);
            FailureModeCacheUpdateIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameFailureModeCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);
            FailureModeCacheRefreshIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameFailureModeCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);

            // AppSettings:Caches:Group:
            EnableGroupCache = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableGroupCache);
            GroupCacheIntervalDailyReferenceStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameGroupCacheIntervalDailyReferenceStartTimeUTC);
            GroupCacheUpdateIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameGroupCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);
            GroupCacheRefreshIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameGroupCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);

            // AppSettings:Caches:Rule:
            EnableRuleCache = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableRuleCache);
            RuleCacheIntervalDailyReferenceStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameRuleCacheIntervalDailyReferenceStartTimeUTC);
            RuleCacheUpdateIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameRuleCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);
            RuleCacheRefreshIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameRuleCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);

            // AppSettings:Caches:UnitOfMeasure:
            EnableUnitOfMeasureCache = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableUnitOfMeasureCache);
            UnitOfMeasureCacheIntervalDailyReferenceStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameUnitOfMeasureCacheIntervalDailyReferenceStartTimeUTC);
            UnitOfMeasureCacheUpdateIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameUnitOfMeasureCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);
            UnitOfMeasureCacheRefreshIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameUnitOfMeasureCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);

            // AppSettings:Caches:User:
            EnableUserCache = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableUserCache);
            UserCacheIntervalDailyReferenceStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameUserCacheIntervalDailyReferenceStartTimeUTC);
            UserCacheUpdateIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameUserCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);
            UserCacheRefreshIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameUserCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);

            // AppSettings:Caches:Zone:
            EnableZoneCache = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableZoneCache);
            ZoneCacheIntervalDailyReferenceStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameZoneCacheIntervalDailyReferenceStartTimeUTC);
            ZoneCacheUpdateIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameZoneCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);
            ZoneCacheRefreshIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameZoneCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);

            // AppSettings:Caches:ZoneType:
            EnableZoneTypeCache = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableZoneTypeCache);
            ZoneTypeCacheIntervalDailyReferenceStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameZoneTypeCacheIntervalDailyReferenceStartTimeUTC);
            ZoneTypeCacheUpdateIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameZoneTypeCacheUpdateIntervalMinutes, null, false, MinCacheUpdateIntervalMinutes, MaxCacheUpdateIntervalMinutes, DefaultCacheUpdateIntervalMinutes);
            ZoneTypeCacheRefreshIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameZoneTypeCacheRefreshIntervalMinutes, null, false, MinCacheRefreshIntervalMinutes, MaxCacheRefreshIntervalMinutes, DefaultCacheRefreshIntervalMinutes);

            // AppSettings:GeneralFeedSettings:
            string feedStartOptionString = configurationHelper.GetConfigKeyValueString(ArgNameFeedStartOption);
            string errorMessage;
            switch (feedStartOptionString)
            {
                case nameof(FeedStartOption.CurrentTime):
                    FeedStartOption = FeedStartOption.CurrentTime;
                    break;
                case nameof(FeedStartOption.FeedVersion):
                    FeedStartOption = FeedStartOption.FeedVersion;
                    break;
                case nameof(FeedStartOption.SpecificTime):
                    string feedStartSpecificTimeUTCString = configurationHelper.GetConfigKeyValueString(ArgNameFeedStartSpecificTimeUTC);
                    DateTime feedStartSpecificTimeUTC;
                    if (DateTime.TryParse(feedStartSpecificTimeUTCString, null, System.Globalization.DateTimeStyles.RoundtripKind, out feedStartSpecificTimeUTC) == false)
                    {
                        errorMessage = $"The value of '{feedStartSpecificTimeUTCString}' provided for the '{ArgNameFeedStartSpecificTimeUTC}' configuration item is not valid.";
                        logger.Error(errorMessage);
                        throw new Exception(errorMessage);
                    }
                    else
                    {
                        FeedStartSpecificTimeUTC = feedStartSpecificTimeUTC;
                        FeedStartOption = FeedStartOption.SpecificTime;
                    }
                    break;
                default:
                    errorMessage = $"The value of '{feedStartOptionString}' provided for the '{ArgNameFeedStartOption}' configuration item is not valid.";
                    logger.Error(errorMessage);
                    throw new Exception(errorMessage);
            }
            DevicesToTrackList = configurationHelper.GetConfigKeyValueString(ArgNameDevicesToTrack);
            DiagnosticsToTrackList = configurationHelper.GetConfigKeyValueString(ArgNameDiagnosticsToTrack);
            ExcludeDiagnosticsToTrack = configurationHelper.GetConfigKeyValueBoolean(ArgNameExcludeDiagnosticsToTrack);
            EnableMinimunIntervalSamplingForLogRecords = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableMinimunIntervalSamplingForLogRecords);
            EnableMinimunIntervalSamplingForStatusData = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableMinimunIntervalSamplingForStatusData);
            MinimumIntervalSamplingDiagnosticsList = configurationHelper.GetConfigKeyValueString(ArgNameMinimumIntervalSamplingDiagnostics);
            MinimumIntervalSamplingIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameMinimumIntervalSamplingIntervalSeconds, null, false, MinSamplingIntervalSeconds, MaxSamplingIntervalSeconds, DefaultSamplingIntervalSeconds);
            ValidateMinimumIntervalSamplingDiagnosticsList();

            // AppSettings:Feeds:BinaryData:
            EnableBinaryDataFeed = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableBinaryDataFeed);
            BinaryDataFeedIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameBinaryDataFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            // AppSettings:Feeds:ChargeEvent:
            EnableChargeEventFeed = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableChargeEventFeed);
            ChargeEventFeedIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameChargeEventFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            // AppSettings:Feeds:DebugData:
            EnableDebugDataFeed = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDebugDataFeed);
            DebugDataFeedIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameDebugDataFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            // AppSettings:Feeds:DeviceStatusInfo:
            EnableDeviceStatusInfoFeed = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDeviceStatusInfoFeed);
            DeviceStatusInfoFeedIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameDeviceStatusInfoFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            // AppSettings:Feeds:DriverChange:
            EnableDriverChangeFeed = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDriverChangeFeed);
            DriverChangeFeedIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameDriverChangeFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            // AppSettings:Feeds:DutyStatusAvailability:
            EnableDutyStatusAvailabilityFeed = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDutyStatusAvailabilityFeed);
            DutyStatusAvailabilityFeedIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameDutyStatusAvailabilityFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);
            DutyStatusAvailabilityFeedLastAccessDateCutoffDays = configurationHelper.GetConfigKeyValueInt(ArgNameDutyStatusAvailabilityFeedLastAccessDateCutoffDays, null, false, MinDutyStatusAvailabilityFeedLastAccessDateCutoffDays, MaxDutyStatusAvailabilityFeedLastAccessDateCutoffDays, DefaultDutyStatusAvailabilityFeedLastAccessDateCutoffDays);

            // AppSettings:Feeds:DutyStatusLog:
            EnableDutyStatusLogFeed = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDutyStatusLogFeed);
            DutyStatusLogFeedIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameDutyStatusLogFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            // AppSettings:Feeds:DVIRLog:
            EnableDVIRLogFeed = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDVIRLogFeed);
            DVIRLogFeedIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameDVIRLogFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            // AppSettings:Feeds:ExceptionEvent:
            EnableExceptionEventFeed = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableExceptionEventFeed);
            ExceptionEventFeedIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameExceptionEventFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);
            TrackZoneStops = configurationHelper.GetConfigKeyValueBoolean(ArgNameTrackZoneStops);

            // AppSettings:Feeds:FaultData:
            EnableFaultDataFeed = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableFaultDataFeed);
            FaultDataFeedIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameFaultDataFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            // AppSettings:Feeds:LogRecord:
            EnableLogRecordFeed = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableLogRecordFeed);
            LogRecordFeedIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameLogRecordFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            // AppSettings:Feeds:StatusData:
            EnableStatusDataFeed = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableStatusDataFeed);
            StatusDataFeedIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameStatusDataFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            // AppSettings:Feeds:Trip:
            EnableTripFeed = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableTripFeed);
            TripFeedIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameTripFeedIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            // AppSettings:Manipulators:DVIRLog:
            EnableDVIRLogManipulator = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDVIRLogManipulator);
            DVIRLogManipulatorIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameDVIRLogManipulatorIntervalSeconds, null, false, MinFeedIntervalSeconds, MaxFeedIntervalSeconds, DefaultFeedIntervalSeconds);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Validated the MinimumIntervalSamplingDiagnosticsList configuration item.
        /// </summary>
        void ValidateMinimumIntervalSamplingDiagnosticsList()
        {
            string errorMessage;

            // If both EnableMinimunIntervalSamplingForLogRecords and EnableMinimunIntervalSamplingForStatusData are false, then there is no need to validate the MinimumIntervalSamplingDiagnosticsList.
            if (EnableMinimunIntervalSamplingForLogRecords == false && EnableMinimunIntervalSamplingForStatusData == false)
            {
                return;
            }

            // If EnableMinimunIntervalSamplingForStatusData is true, then ExcludeDiagnosticsToTrack must be false.
            if (EnableMinimunIntervalSamplingForStatusData == true && ExcludeDiagnosticsToTrack == true)
            {
                errorMessage = $"'{ArgNameEnableMinimunIntervalSamplingForStatusData}' is set to true, but so is '{ArgNameExcludeDiagnosticsToTrack}'. Minimum interval sanpling cannot be enabled for StatusData unless '{ArgNameExcludeDiagnosticsToTrack}' is set to false.";
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            // If EnableMinimunIntervalSamplingForStatusData is true, then DiagnosticsToTrack must be populated with a list of specific Diagnostic Ids.
            if (EnableMinimunIntervalSamplingForStatusData == true && (DiagnosticsToTrackList == string.Empty || DiagnosticsToTrackList == WildcardString))
            {
                errorMessage = $"'{ArgNameEnableMinimunIntervalSamplingForStatusData}' is set to true, but '{ArgNameDiagnosticsToTrack}' is set to '{DiagnosticsToTrackList}'. Minimum interval sanpling cannot be enabled for StatusData unless '{ArgNameDiagnosticsToTrack}' is populated with a list of specific Diagnostic Ids.";
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            // If EnableMinimunIntervalSamplingForStatusData is true, then MinimumIntervalSamplingDiagnostics must be populated with a list of specific Diagnostic Ids that is the same as that provided in DiagnosticsToTrack or a subset thereof.
            if (EnableMinimunIntervalSamplingForStatusData == true && (MinimumIntervalSamplingDiagnosticsList == string.Empty || (MinimumIntervalSamplingDiagnosticsList == WildcardString)))
            {
                errorMessage = $"'{ArgNameEnableMinimunIntervalSamplingForStatusData}' is set to true. As such, '{ArgNameMinimumIntervalSamplingDiagnostics}' must be populated with the same list of specific Diagnostic Ids specified in '{ArgNameDiagnosticsToTrack}' or a subset thereof. The wildcard ('{WildcardString}' cannot be used.)";
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            // If EnableMinimunIntervalSamplingForStatusData is true, then MinimumIntervalSamplingDiagnostics must be populated with a list of specific Diagnostic Ids that is the same as that provided in DiagnosticsToTrack or a subset thereof.
            string[] diagnosticsToTrackArray = DiagnosticsToTrackList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] minimumIntervalSamplingDiagnosticsArray = MinimumIntervalSamplingDiagnosticsList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            bool allMinimumIntervalSamplingDiagnosticsContainedInDiagnosticsToTrack = minimumIntervalSamplingDiagnosticsArray.All(minimumIntervalSamplingDiagnostic => diagnosticsToTrackArray.Contains(minimumIntervalSamplingDiagnostic));
            if (EnableMinimunIntervalSamplingForStatusData == true && allMinimumIntervalSamplingDiagnosticsContainedInDiagnosticsToTrack == false)
            {
                errorMessage = $"'{ArgNameEnableMinimunIntervalSamplingForStatusData}' is set to true. As such, '{ArgNameMinimumIntervalSamplingDiagnostics}' must be populated with the same list of specific Diagnostic Ids specified in '{ArgNameDiagnosticsToTrack}' or a subset thereof. The list provided for '{ArgNameMinimumIntervalSamplingDiagnostics}' contains at least one item that is not in the list provided for '{ArgNameDiagnosticsToTrack}'.";
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
        }
    }
}
