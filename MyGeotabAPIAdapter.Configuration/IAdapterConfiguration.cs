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
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="DutyStatusLog"/> objects.
        /// </summary>
        int DutyStatusLogFeedIntervalSeconds { get; }

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
        /// Indicates whether a <see cref="DutyStatusLog"/> data feed should be enabled. 
        /// </summary>
        bool EnableDutyStatusLogFeed { get; }

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
        /// Indicates whether the <see cref="MyGeotabAPIAdapter.Service.FaultDataLocationService"/> should be enabled. 
        /// </summary>
        bool EnableFaultDataLocationService { get; }

        /// <summary>
        /// Indicates whether a <see cref="FuelAndEnergyUsed"/> data feed should be enabled. 
        /// </summary>
        bool EnableFuelAndEnergyUsedFeed { get; }

        /// <summary>
        /// Indicates whether a <see cref="Group"/> cache should be enabled. 
        /// </summary>
        bool EnableGroupCache { get; }

        /// <summary>
        /// Indicates whether "Level 1" database maintenance should be enabled. This includes non-interfering tasks that can be executed outside a maintenance window.  
        /// </summary>
        bool EnableLevel1DatabaseMaintenance { get; }

        /// <summary>
        /// Indicates whether "Level 2" database maintenance should be enabled. This includes tasks that can interfere with application functionality and must be executed within a maintenance window.  
        /// </summary>
        bool EnableLevel2DatabaseMaintenance { get; }

        /// <summary>
        /// Indicates whether "Level 2" database maintenance should only occur during the daily maintenance window, the start of which is defined by <see cref="Level2DatabaseMaintenanceWindowStartTimeUTC"/> and the end of which is determined by adding <see cref="Level2DatabaseMaintenanceWindowMaxMinutes"/> to the start time. This setting is only used if <see cref="EnableLevel2DatabaseMaintenance"/> is <c>true</c>.  
        /// </summary>
        bool EnableLevel2DatabaseMaintenanceWindow { get; }

        /// <summary>
        /// Indicates whether a <see cref="LogRecord"/> data feed should be enabled. 
        /// </summary>
        bool EnableLogRecordFeed { get; }

        /// <summary>
        /// Indicates whether minimum interval sampling should be enabled for the <see cref="LogRecord"/> feed.
        /// </summary>
        bool EnableMinimunIntervalSamplingForLogRecords { get; }

        /// <summary>
        /// Indicates whether minimum interval sampling should be enabled for the <see cref="StatusData"/> feed. Applies only to StatusData with Diagnostics in the <see cref="MinimumIntervalSamplingDiagnosticsList"/> list.
        /// </summary>
        bool EnableMinimunIntervalSamplingForStatusData { get; }

        /// <summary>
        /// Indicates whether a <see cref="Rule"/> cache should be enabled. 
        /// </summary>
        bool EnableRuleCache { get; }

        /// <summary>
        /// Indicates whether a <see cref="StatusData"/> data feed should be enabled. 
        /// </summary>
        bool EnableStatusDataFeed { get; }

        /// <summary>
        /// Indicates whether the <see cref="MyGeotabAPIAdapter.Service.StatusDataLocationService"/> should be enabled. 
        /// </summary>
        bool EnableStatusDataLocationService { get; }

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
        /// When getting the DateTime range of a batch of unprocessed FaultData records, this buffer is applied to either end of the DateTime range when selecting LogRecords to use for interpolation such that lag LogRecords can be obtained for records that are “early” in the batch and lead LogRecords can be obtained for records that are “late” in the batch.
        /// </summary>
        int FaultDataLocationServiceBufferMinutes { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the daily start time for the FaultDataLocationService. Only used if <see cref="FaultDataLocationServiceOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        DateTime FaultDataLocationServiceDailyStartTimeUTC { get; }

        /// <summary>
        /// The number of seconds to add to the <see cref="DateTime.TimeOfDay"/> of the <see cref="FaultDataLocationServiceDailyStartTimeUTC"/> in order to calculate the daily stop time for the FaultDataLocationService. Only used if <see cref="FaultDataLocationServiceOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        int FaultDataLocationServiceDailyRunTimeSeconds { get; }

        /// <summary>
        /// The minimum number of seconds to wait between the start of one execution iteration of the  FaultDataLocationService and the next.
        /// </summary>
        int FaultDataLocationServiceExecutionIntervalSeconds { get; }

        /// <summary>
        /// The maximum number of unprocessed FaultData records to retrieve for interpolation per batch.
        /// </summary>
        int FaultDataLocationServiceMaxBatchSize { get; }

        /// <summary>
        /// The maximum number of days over which unprocessed FaultData records in a batch can span.
        /// </summary>
        int FaultDataLocationServiceMaxDaysPerBatch { get; }

        /// <summary>
        /// Indicates the number of directions on the <see href="https://en.wikipedia.org/wiki/Compass_rose">compass rose</see> to use when returning compass directions associated with specific bearing values. Determines the <see cref="MyGeotabAPIAdapter.Geospatial.CompassRoseType"/> that will be used. This property is only used if <see cref="FaultDataLocationServicePopulateDirection"/> is set to <c>true</c>.
        /// </summary>
        int FaultDataLocationServiceNumberOfCompassDirections { get; }

        /// <summary>
        /// The operation mode to be used by the FaultDataLocationService.
        /// </summary>
        OperationMode FaultDataLocationServiceOperationMode { get; }

        /// <summary>
        /// Indicates whether the <c>Bearing</c> column in the <c>FaultDataLocations2</c> table should be populated.
        /// </summary>
        bool FaultDataLocationServicePopulateBearing { get; }

        /// <summary>
        /// Indicates whether the <c>Direction</c> column in the <c>FaultDataLocations2</c> table should be populated. This property is only used if <see cref="FaultDataLocationServicePopulateBearing"/> is set to <c>true</c>.
        /// </summary>
        bool FaultDataLocationServicePopulateDirection { get; }

        /// <summary>
        /// Indicates whether the <c>Speed</c> column in the <c>FaultDataLocations2</c> table should be populated.
        /// </summary>
        bool FaultDataLocationServicePopulateSpeed { get; }

        /// <summary>
        /// The <see cref="Common.FeedStartOption"/> to use for the initial GetFeed call for each data feed.
        /// </summary>
        FeedStartOption FeedStartOption { get; }

        /// <summary>
        /// The specific <see cref="DateTime"/> to use for the initial GetFeed call for each data feed. Only used if <see cref="FeedStartOption"/> is set to <see cref="FeedStartOption.SpecificTime"/>.
        /// </summary>
        DateTime FeedStartSpecificTimeUTC { get; }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="FuelAndEnergyUsed"/> objects.
        /// </summary>
        int FuelAndEnergyUsedFeedIntervalSeconds { get; }

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
        /// The number of minutes to wait between executions of "Level1" database maintenance.
        /// </summary>
        int Level1DatabaseMaintenanceIntervalMinutes { get; }

        /// <summary>
        /// The number of minutes to wait between executions of "Level2" database maintenance.
        /// </summary>
        int Level2DatabaseMaintenanceIntervalMinutes { get; }

        /// <summary>
        /// The maximum number of minutes to allow for execution of "Level2" database maintenance once it starts (based on the <see cref="Level2DatabaseMaintenanceWindowStartTimeUTC"/>).
        /// </summary>
        int Level2DatabaseMaintenanceWindowMaxMinutes { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the basis for determining the start of the maintenance window for "Level 2" maintenance.
        /// </summary>
        DateTime Level2DatabaseMaintenanceWindowStartTimeUTC { get; }

        /// <summary>
        /// The minimum number of seconds to wait between GetFeed() calls for <see cref="LogRecord"/> objects.
        /// </summary>
        int LogRecordFeedIntervalSeconds { get; }

        /// <summary>
        /// The comma-separated list of <see cref="Id"/>s of of specific <see cref="Diagnostic"/> entities for which minimum interval sampling of <see cref="StatusData"/> and/or <see cref="LogRecord"/> entities should be applied. <see cref="ExcludeDiagnosticsToTrack"/> must be false. Items in this list must also be included in the <see cref="DiagnosticsToTrackList"/> list and the wildcard value (*) cannot be used. There can be more Diagnostic Ids listed in the <see cref="DiagnosticsToTrackList"/> list than the <see cref="MinimumIntervalSamplingDiagnosticsList"/> list (i.e. minimum interval sampling can be applied to a subset of the list of Diagnostics being tracked with all StatusData being pulled for the rest of the items in the <see cref="DiagnosticsToTrackList"/> list.
        /// </summary>
        string MinimumIntervalSamplingDiagnosticsList {  get; }

        /// <summary>
        /// The minimum number of seconds that must have elapsed since the DateTime of the last captured record for the subject <see cref="Device"/> (in the case of <see cref="LogRecord"/>s) or <see cref="Device"/> + <see cref="Diagnostic"/> combination (in the case of <see cref="StatusData"/>) before the next record is captured. Note that data points are not captured or interpolated at this exact interval. Rather, the next actual data point at or after the elapsed interval is captured. 
        /// </summary>
        int MinimumIntervalSamplingIntervalSeconds { get; }

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
        /// If <see cref="EnableFaultDataFeed"/> is <c>true</c>, indicates whether the EffectOnComponent and Recommendation columns in the associated adapter database table will be populated. Setting this property to <c>false</c> will result in the EffectOnComponent and Recommendation columns being set to <c>null</c> for all records in the associated adapter database table, thereby potentially saving on disk space if these property values are not of interest. WARNING: There is no way to update these columns for records that have already been downloaded.
        /// </summary>
        bool PopulateEffectOnComponentAndRecommendation { get; }

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
        /// When getting the DateTime range of a batch of unprocessed StatusData records, this buffer is applied to either end of the DateTime range when selecting LogRecords to use for interpolation such that lag LogRecords can be obtained for records that are “early” in the batch and lead LogRecords can be obtained for records that are “late” in the batch.
        /// </summary>
        int StatusDataLocationServiceBufferMinutes { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the daily start time for the StatusDataLocationService. Only used if <see cref="StatusDataLocationServiceOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        DateTime StatusDataLocationServiceDailyStartTimeUTC { get; }

        /// <summary>
        /// The number of seconds to add to the <see cref="DateTime.TimeOfDay"/> of the <see cref="StatusDataLocationServiceDailyStartTimeUTC"/> in order to calculate the daily stop time for the StatusDataLocationService. Only used if <see cref="StatusDataLocationServiceOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        int StatusDataLocationServiceDailyRunTimeSeconds { get; }

        /// <summary>
        /// The minimum number of seconds to wait between the start of one execution iteration of the  StatusDataLocationService and the next.
        /// </summary>
        int StatusDataLocationServiceExecutionIntervalSeconds { get; }

        /// <summary>
        /// The maximum number of unprocessed StatusData records to retrieve for interpolation per batch.
        /// </summary>
        int StatusDataLocationServiceMaxBatchSize { get; }

        /// <summary>
        /// The maximum number of days over which unprocessed StatusData records in a batch can span.
        /// </summary>
        int StatusDataLocationServiceMaxDaysPerBatch { get; }

        /// <summary>
        /// Indicates the number of directions on the <see href="https://en.wikipedia.org/wiki/Compass_rose">compass rose</see> to use when returning compass directions associated with specific bearing values. Determines the <see cref="MyGeotabAPIAdapter.Geospatial.CompassRoseType"/> that will be used. This property is only used if <see cref="StatusDataLocationServicePopulateDirection"/> is set to <c>true</c>.
        /// </summary>
        int StatusDataLocationServiceNumberOfCompassDirections { get; }

        /// <summary>
        /// The operation mode to be used by the StatusDataLocationService.
        /// </summary>
        OperationMode StatusDataLocationServiceOperationMode { get; }

        /// <summary>
        /// Indicates whether the <c>Bearing</c> column in the <c>StatusDataLocations2</c> table should be populated.
        /// </summary>
        bool StatusDataLocationServicePopulateBearing { get; }

        /// <summary>
        /// Indicates whether the <c>Direction</c> column in the <c>StatusDataLocations2</c> table should be populated. This property is only used if <see cref="StatusDataLocationServicePopulateBearing"/> is set to <c>true</c>.
        /// </summary>
        bool StatusDataLocationServicePopulateDirection { get; }

        /// <summary>
        /// Indicates whether the <c>Speed</c> column in the <c>StatusDataLocations2</c> table should be populated.
        /// </summary>
        bool StatusDataLocationServicePopulateSpeed { get; }

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
        /// Indicates whether the adapter database with which the application is paired is using version 2 of the data model. Services will be configured to use the appropriate data model based on the value provided here. 
        /// </summary>
        bool UseDataModel2 { get; }

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
