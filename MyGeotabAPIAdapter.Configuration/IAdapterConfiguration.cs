using System;

namespace MyGeotabAPIAdapter.Configuration
{
    /// <summary>
    /// Interface for a class that reads (from appsettings.json) and stores application configuration settings. Intended for association with the MyGeotabAPIAdapter project.
    /// </summary>
    public interface IAdapterConfiguration
    {
        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="BinaryData"/> objects.
        /// </summary>
        int BinaryDataFeedIntervalSeconds { get; }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="ChargeEvent"/> objects.
        /// </summary>
        int ChargeEventFeedIntervalSeconds { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="Controller"/> cache.
        /// </summary>
        DateTime ControllerCacheIntervalDailyReferenceStartTimeUTC { get; }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="Controller"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        int ControllerCacheRefreshIntervalMinutes { get; }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="Controller"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        int ControllerCacheUpdateIntervalMinutes { get; }

        /// <summary>
        /// The connection string used to access the MyGeotabAPIAdapter database.
        /// </summary>
        string DatabaseConnectionString { get; }

        /// <summary>
        /// A string representation of the <see cref="MyGeotabAPIAdapter.Database.ConnectionInfo.DataAccessProviderType"/> to be used when creating <see cref="System.Data.IDbConnection"/> instances for the MyGeotabAPIAdapter database.
        /// </summary>
        string DatabaseProviderType { get; }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="DebugData"/> objects.
        /// </summary>
        int DebugDataFeedIntervalSeconds { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="Device"/> cache.
        /// </summary>
        DateTime DeviceCacheIntervalDailyReferenceStartTimeUTC { get; }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="Device"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        int DeviceCacheRefreshIntervalMinutes { get; }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="Device"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        int DeviceCacheUpdateIntervalMinutes { get; }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="DeviceStatusInfo"/> objects.
        /// </summary>
        int DeviceStatusInfoFeedIntervalSeconds { get; }

        /// <summary>
        /// The comma-separated list of <see cref="Id"/>s of of specific <see cref="Device"/> entities to track when utilizing data feeds.
        /// </summary>
        string DevicesToTrackList { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="Diagnostic"/> cache.
        /// </summary>
        DateTime DiagnosticCacheIntervalDailyReferenceStartTimeUTC { get; }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="Diagnostic"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        int DiagnosticCacheRefreshIntervalMinutes { get; }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="Diagnostic"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        int DiagnosticCacheUpdateIntervalMinutes { get; }

        /// <summary>
        /// The comma-separated list of <see cref="Id"/>s of of specific <see cref="Diagnostic"/> entities to track when utilizing data feeds.
        /// </summary>
        string DiagnosticsToTrackList { get; }

        /// <summary>
        /// Indicates whether machine name validation should be disabled. 
        /// </summary>
        bool DisableMachineNameValidation { get; }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="DriverChange"/> objects.
        /// </summary>
        int DriverChangeFeedIntervalSeconds { get; }

        /// <summary>
        /// The minimum number of seconds to wait after retrieving <see cref="DutyStatusAvailability"/> information for all <see cref="Driver"/>s before starting the retrieval process again.
        /// </summary>
        int DutyStatusAvailabilityFeedIntervalSeconds { get; }

        /// <summary>
        /// Used to reduce the number of unnecessary Get calls when retrieving <see cref="DutyStatusAvailability"/> information for all <see cref="Driver"/>s. Data is not queried for Drivers with a <see cref="User.LastAccessDate"/> greater than this many days in the past. This value should be set to approximately twice the longest possible cycle for a HOS ruleset.
        /// </summary>
        int DutyStatusAvailabilityFeedLastAccessDateCutoffDays { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="DVIRDefect"/> cache.
        /// </summary>
        DateTime DVIRDefectListCacheIntervalDailyReferenceStartTimeUTC { get; }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="DVIRLog.DefectList"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        int DVIRDefectListCacheRefreshIntervalMinutes { get; }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="DVIRLog"/> objects.
        /// </summary>
        int DVIRLogFeedIntervalSeconds { get; }

        /// <summary>
        /// The minimum number of seconds to wait between iterations of the process that propagates <see cref="DVIRLog"/> changes from tables in the adapter database to the associated MyGeotab database.
        /// </summary>
        int DVIRLogManipulatorIntervalSeconds { get; }

        /// <summary>
        /// Indicates whether a <see cref="BinaryData"/> data feed should be enabled. 
        /// </summary>
        bool EnableBinaryDataFeed { get; }

        /// <summary>
        /// Indicates whether a <see cref="ChargeEvent"/> data feed should be enabled. 
        /// </summary>
        bool EnableChargeEventFeed { get; }

        /// <summary>
        /// Indicates whether a <see cref="Controller"/> cache should be enabled. 
        /// </summary>
        bool EnableControllerCache { get; }

        /// <summary>
        /// Indicates whether a <see cref="DebugData"/> data feed should be enabled. 
        /// </summary>
        bool EnableDebugDataFeed { get; }

        /// <summary>
        /// Indicates whether a <see cref="Device"/> cache should be enabled. 
        /// </summary>
        bool EnableDeviceCache { get; }

        /// <summary>
        /// Indicates whether a <see cref="DeviceStatusInfo"/> data feed should be enabled. 
        /// </summary>
        bool EnableDeviceStatusInfoFeed { get; }

        /// <summary>
        /// Indicates whether a <see cref="Diagnostic"/> cache should be enabled. 
        /// </summary>
        bool EnableDiagnosticCache { get; }

        /// <summary>
        /// Indicates whether a <see cref="DriverChange"/> data feed should be enabled. 
        /// </summary>
        bool EnableDriverChangeFeed { get; }

        /// <summary>
        /// Indicates whether a <see cref="DutyStatusAvailability"/> data feed should be enabled. 
        /// </summary>
        bool EnableDutyStatusAvailabilityFeed { get; }

        /// <summary>
        /// Indicates whether a <see cref="DVIRDefect"/> cache should be enabled. 
        /// </summary>
        bool EnableDVIRDefectCache { get; }

        /// <summary>
        /// Indicates whether a <see cref="DVIRLog"/> data feed should be enabled. 
        /// </summary>
        bool EnableDVIRLogFeed { get; }

        /// <summary>
        /// Indicates whether the <see cref="DVIRLogManipulator"/> worker service should be enabled. 
        /// </summary>
        bool EnableDVIRLogManipulator { get; }

        /// <summary>
        /// Indicates whether a <see cref="ExceptionEvent"/> data feed should be enabled. 
        /// </summary>
        bool EnableExceptionEventFeed { get; }

        /// <summary>
        /// Indicates whether a <see cref="FailureMode"/> cache should be enabled. 
        /// </summary>
        bool EnableFailureModeCache { get; }

        /// <summary>
        /// Indicates whether a <see cref="FaultData"/> data feed should be enabled. 
        /// </summary>
        bool EnableFaultDataFeed { get; }

        /// <summary>
        /// Indicates whether a <see cref="Group"/> cache should be enabled. 
        /// </summary>
        bool EnableGroupCache { get; }

        /// <summary>
        /// Indicates whether a <see cref="LogRecord"/> data feed should be enabled. 
        /// </summary>
        bool EnableLogRecordFeed { get; }

        /// <summary>
        /// Indicates whether a <see cref="Rule"/> cache should be enabled. 
        /// </summary>
        bool EnableRuleCache { get; }

        /// <summary>
        /// Indicates whether a <see cref="StatusData"/> data feed should be enabled. 
        /// </summary>
        bool EnableStatusDataFeed { get; }

        /// <summary>
        /// Indicates whether a <see cref="Trip"/> data feed should be enabled. 
        /// </summary>
        bool EnableTripFeed { get; }

        /// <summary>
        /// Indicates whether a <see cref="UnitOfMeasure"/> cache should be enabled. 
        /// </summary>
        bool EnableUnitOfMeasureCache { get; }

        /// <summary>
        /// Indicates whether a <see cref="User"/> cache should be enabled. 
        /// </summary>
        bool EnableUserCache { get; }

        /// <summary>
        /// Indicates whether a <see cref="Zone"/> cache should be enabled. 
        /// </summary>
        bool EnableZoneCache { get; }

        /// <summary>
        /// Indicates whether a <see cref="ZoneType"/> cache should be enabled. 
        /// </summary>
        bool EnableZoneTypeCache { get; }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="ExceptionEvent"/> objects.
        /// </summary>
        int ExceptionEventFeedIntervalSeconds { get; }

        /// <summary>
        /// Indicates whether the <see cref="DiagnosticsToTrackList"/> should be excluded, effectively inverting functionality such that entities with any <see cref="Diagnostic"/> <see cref="Id"/>s EXCEPT those in the <see cref="DiagnosticsToTrackList"/> will be included when utilizing data feeds.
        /// </summary>
        bool ExcludeDiagnosticsToTrack { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="FailureMode"/> cache.
        /// </summary>
        DateTime FailureModeCacheIntervalDailyReferenceStartTimeUTC { get; }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="FailureMode"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        int FailureModeCacheRefreshIntervalMinutes { get; }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="FailureMode"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        int FailureModeCacheUpdateIntervalMinutes { get; }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="FaultData"/> objects.
        /// </summary>
        int FaultDataFeedIntervalSeconds { get; }

        /// <summary>
        /// The <see cref="Common.FeedStartOption"/> to use for the initial GetFeed call for each data feed.
        /// </summary>
        FeedStartOption FeedStartOption { get; }

        /// <summary>
        /// The specific <see cref="DateTime"/> to use for the initial GetFeed call for each data feed. Only used if <see cref="FeedStartOption"/> is set to <see cref="FeedStartOption.SpecificTime"/>.
        /// </summary>
        DateTime FeedStartSpecificTimeUTC { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="Group"/> cache.
        /// </summary>
        DateTime GroupCacheIntervalDailyReferenceStartTimeUTC { get; }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="Group"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        int GroupCacheRefreshIntervalMinutes { get; }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="Group"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        int GroupCacheUpdateIntervalMinutes { get; }

        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="LogRecord"/> objects.
        /// </summary>
        int LogRecordFeedIntervalSeconds { get; }

        /// <summary>
        /// The value to use for the <c>Database</c> parameter when authenticating the <see cref="MyGeotabAPIAdapter.Checkmate.API"/>.
        /// </summary>
        string MyGeotabDatabase { get; }

        /// <summary>
        /// The value to use for the <c>Password</c> parameter when authenticating the <see cref="MyGeotabAPIAdapter.Checkmate.API"/>.
        /// </summary>
        string MyGeotabPassword { get; }

        /// <summary>
        /// The value to use for the <c>Server</c> parameter when authenticating the <see cref="MyGeotabAPIAdapter.Checkmate.API"/>.
        /// </summary>
        string MyGeotabServer { get; }

        /// <summary>
        /// The value to use for the <c>User</c> parameter when authenticating the <see cref="MyGeotabAPIAdapter.Checkmate.API"/>.
        /// </summary>
        string MyGeotabUser { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="Rule"/> cache.
        /// </summary>
        DateTime RuleCacheIntervalDailyReferenceStartTimeUTC { get; }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="Rule"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        int RuleCacheRefreshIntervalMinutes { get; }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="Rule"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        int RuleCacheUpdateIntervalMinutes { get; }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="StatusData"/> objects.
        /// </summary>
        int StatusDataFeedIntervalSeconds { get; }

        /// <summary>
        /// The maximum number of seconds that a database <see cref="System.Threading.Tasks.Task"/> or batch thereof can take to be completed before it is deemed that there is a database connectivity issue and a <see cref="Database.DatabaseConnectionException"/> will be thrown.
        /// </summary>
        int TimeoutSecondsForDatabaseTasks { get; }

        /// <summary>
        /// The maximum number of seconds that a MyGeotab API call <see cref="System.Threading.Tasks.Task"/> or batch thereof can take to be completed before it is deemed that there is a MyGeotab connectivity issue and a <see cref="MyGeotabConnectionException"/> will be thrown.
        /// </summary>
        int TimeoutSecondsForMyGeotabTasks { get; }

        /// <summary>
        /// Indicates whether zone stops are to be tracked. If <c>false</c>, <see cref="Zone"/> objects will not be cached and <see cref="ExceptionEvent"/>s of the <see cref="ExceptionRuleBaseType.ZoneStop"/> type will be excluded when processing results of the <see cref="ExceptionEvent"/> feed.
        /// </summary>
        bool TrackZoneStops { get; }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="Trip"/> objects.
        /// </summary>
        int TripFeedIntervalSeconds { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="UnitOfMeasure"/> cache.
        /// </summary>
        DateTime UnitOfMeasureCacheIntervalDailyReferenceStartTimeUTC { get; }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="UnitOfMeasure"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        int UnitOfMeasureCacheRefreshIntervalMinutes { get; }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="UnitOfMeasure"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        int UnitOfMeasureCacheUpdateIntervalMinutes { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="User"/> cache.
        /// </summary>
        DateTime UserCacheIntervalDailyReferenceStartTimeUTC { get; }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="User"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        int UserCacheRefreshIntervalMinutes { get; }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="User"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        int UserCacheUpdateIntervalMinutes { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="Zone"/> cache.
        /// </summary>
        DateTime ZoneCacheIntervalDailyReferenceStartTimeUTC { get; }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="Zone"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        int ZoneCacheRefreshIntervalMinutes { get; }

        /// <summary>
        /// The number of minutes to wait, after updating the <see cref="Zone"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        int ZoneCacheUpdateIntervalMinutes { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the <see cref="ZoneType"/> cache.
        /// </summary>
        DateTime ZoneTypeCacheIntervalDailyReferenceStartTimeUTC { get; }

        /// <summary>
        /// The number of minutes to wait, after refreshing the <see cref="ZoneType"/> cache, before initiating the next refresh of the subject cache.
        /// </summary>
        int ZoneTypeCacheRefreshIntervalMinutes { get; }

        /// <summary>
        /// The number of seconds to wait, after updating the <see cref="ZoneType"/> cache, before initiating the next update of the subject cache.
        /// </summary>
        int ZoneTypeCacheUpdateIntervalMinutes { get; }

        /// <summary>
        /// Validates the configuration object values and uses those values to set properties of the <see cref="IAdapterConfiguration"/>.
        /// </summary>
        void ProcessConfigItems();
    }
}
