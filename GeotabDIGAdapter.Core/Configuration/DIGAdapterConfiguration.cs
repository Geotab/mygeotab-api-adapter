using NLog;

namespace MyGeotabAPIAdapter.Configuration
{
    /// <summary>
    /// A class that reads (from appsettings.json) and stores application configuration settings. Intended for association with the GeotabDIGAdapter project.
    /// </summary>
    public class DIGAdapterConfiguration : IDIGAdapterConfiguration
    {
        // Argument Names for appsettings:
        // > OverrideSettings
        const string ArgNameDisableMachineNameValidation = "OverrideSettings:DisableMachineNameValidation";
        // > DatabaseSettings
        const string ArgNameDatabaseProviderType = "DatabaseSettings:DatabaseProviderType";
        const string ArgNameDatabaseConnectionString = "DatabaseSettings:DatabaseConnectionString";
        // > MyAdminSettings
        const string ArgNameMyAdminAPIEndpoint = "MyAdminSettings:MyAdminAPIEndpoint";
        const string ArgNameMyAdminUser = "MyAdminSettings:MyAdminUser";
        const string ArgNameMyAdminPassword = "MyAdminSettings:MyAdminPassword";
        const string ArgNamePromoCode = "MyAdminSettings:PromoCode";
        // > DIGSettings
        const string ArgNameOperatingMode = "DIGSettings:OperatingMode";
        const string ArgNameDIGAPIEndpoint = "DIGSettings:DIGAPIEndpoint";
        const string ArgNameDIGUser = "DIGSettings:DIGUser";
        const string ArgNameDIGPassword = "DIGSettings:DIGPassword";
        // > AppSettings:GeneralSettings
        const string ArgNameTimeoutSecondsForDatabaseTasks = "AppSettings:GeneralSettings:TimeoutSecondsForDatabaseTasks";
        const string ArgNameTimeoutSecondsForDIGTasks = "AppSettings:GeneralSettings:TimeoutSecondsForDIGTasks";
        // > DIGSettings:Services:DeviceProvisioningService
        const string ArgNameEnableDeviceProvisioningService = "DIGSettings:Services:DeviceProvisioningService:EnableDeviceProvisioningService";
        const string ArgNameDeviceProvisioningServiceMaxClaimAttempts = "DIGSettings:Services:DeviceProvisioningService:MaxClaimAttempts";
        // > DIGSettings:Services:DeviceDIGReadinessService
        const string ArgNameEnableDeviceDIGReadinessService = "DIGSettings:Services:DeviceDIGReadinessService:EnableDeviceDIGReadinessService";
        // > DIGSettings:Services:DeviceSubscriptionService
        const string ArgNameEnableDeviceSubscriptionService = "DIGSettings:Services:DeviceSubscriptionService:EnableDeviceSubscriptionService";
        const string ArgNameDeviceSubscriptionServiceIntervalSeconds = "DIGSettings:Services:DeviceSubscriptionService:IntervalSeconds";
        const string ArgNameDeviceSubscriptionServiceBatchSize = "DIGSettings:Services:DeviceSubscriptionService:BatchSize";
        const string ArgNameDeviceSubscriptionServiceMaxClaimAttempts = "DIGSettings:Services:DeviceSubscriptionService:MaxClaimAttempts";
        const string ArgNameExpectedIdentifierTypes = "DIGSettings:Services:DeviceSubscriptionService:ExpectedIdentifierTypes";
        const string ArgNameMaxTerminationsPerWindow = "DIGSettings:Services:DeviceSubscriptionService:MaxTerminationsPerWindow";
        const string ArgNameTerminationWindowMinutes = "DIGSettings:Services:DeviceSubscriptionService:TerminationWindowMinutes";
        const string ArgNameStaleStagingBatchMinutes = "DIGSettings:Services:DeviceSubscriptionService:StaleStagingBatchMinutes";
        const string ArgNameMinimumExpectedDeviceCount = "DIGSettings:Services:DeviceSubscriptionService:MinimumExpectedDeviceCount";
        // > DIGSettings:Services:TelemetryDataService
        const string ArgNameEnableTelemetryDataService = "DIGSettings:Services:TelemetryDataService:EnableTelemetryDataService";
        const string ArgNameTelemetryDataServiceBatchSizePerType = "DIGSettings:Services:TelemetryDataService:BatchSizePerType";
        const string ArgNameTelemetryDataServiceMaxClaimAttempts = "DIGSettings:Services:TelemetryDataService:MaxClaimAttempts";
        const string ArgNameTelemetryDataServiceIntervalSeconds = "DIGSettings:Services:TelemetryDataService:IntervalSeconds";
        const string ArgNameEnableAccelerationRecords = "DIGSettings:Services:TelemetryDataService:EnableAccelerationRecords";
        const string ArgNameEnableBinaryRecords = "DIGSettings:Services:TelemetryDataService:EnableBinaryRecords";
        const string ArgNameEnableBluetoothRecords = "DIGSettings:Services:TelemetryDataService:EnableBluetoothRecords";
        const string ArgNameEnableDriverChangeRecords = "DIGSettings:Services:TelemetryDataService:EnableDriverChangeRecords";
        const string ArgNameEnableGenericFaultRecords = "DIGSettings:Services:TelemetryDataService:EnableGenericFaultRecords";
        const string ArgNameEnableGenericStatusRecords = "DIGSettings:Services:TelemetryDataService:EnableGenericStatusRecords";
        const string ArgNameEnableGpsRecords = "DIGSettings:Services:TelemetryDataService:EnableGpsRecords";
        const string ArgNameEnableJ1708FaultRecords = "DIGSettings:Services:TelemetryDataService:EnableJ1708FaultRecords";
        const string ArgNameEnableJ1939FaultRecords = "DIGSettings:Services:TelemetryDataService:EnableJ1939FaultRecords";
        const string ArgNameEnableObdiiFaultRecords = "DIGSettings:Services:TelemetryDataService:EnableObdiiFaultRecords";
        const string ArgNameEnableVinRecords = "DIGSettings:Services:TelemetryDataService:EnableVinRecords";
        const string ArgNameProvisionedDeviceCacheRefreshIntervalSeconds = "DIGSettings:Services:TelemetryDataService:ProvisionedDeviceCacheRefreshIntervalSeconds";
        // > DIGSettings:Services:InvalidRecordRetrievalService
        const string ArgNameEnableInvalidRecordRetrievalService = "DIGSettings:Services:InvalidRecordRetrievalService:EnableInvalidRecordRetrievalService";
        const string ArgNameInvalidRecordRetrievalServiceIntervalSeconds = "DIGSettings:Services:InvalidRecordRetrievalService:IntervalSeconds";
        const string ArgNameInvalidRecordRetrievalServiceBatchSize = "DIGSettings:Services:InvalidRecordRetrievalService:BatchSize";
        // > DIGSettings:Services:DatabaseMaintenanceService
        const string ArgNameEnableDatabaseMaintenanceService = "DIGSettings:Services:DatabaseMaintenanceService:EnableDatabaseMaintenanceService";
        const string ArgNameDatabaseMaintenanceServiceIntervalMinutes = "DIGSettings:Services:DatabaseMaintenanceService:IntervalMinutes";
        const string ArgNamePartitionInterval = "DIGSettings:Services:DatabaseMaintenanceService:PartitionInterval";
        const string ArgNameEnableAutomaticRetention = "DIGSettings:Services:DatabaseMaintenanceService:EnableAutomaticRetention";
        const string ArgNameFailTableRetentionDays = "DIGSettings:Services:DatabaseMaintenanceService:FailTableRetentionDays";
        const string ArgNameQueueDepthWarningThreshold = "DIGSettings:Services:DatabaseMaintenanceService:QueueDepthWarningThreshold";
        const string ArgNameEnableLevel1DigMaintenance = "DIGSettings:Services:DatabaseMaintenanceService:EnableLevel1DigMaintenance";
        const string ArgNameLevel1DigMaintenanceIntervalMinutes = "DIGSettings:Services:DatabaseMaintenanceService:Level1DigMaintenanceIntervalMinutes";
        const string ArgNameEnableLevel2DigMaintenance = "DIGSettings:Services:DatabaseMaintenanceService:EnableLevel2DigMaintenance";
        const string ArgNameLevel2DigMaintenanceIntervalMinutes = "DIGSettings:Services:DatabaseMaintenanceService:Level2DigMaintenanceIntervalMinutes";
        const string ArgNameEnableLevel2DigMaintenanceWindow = "DIGSettings:Services:DatabaseMaintenanceService:EnableLevel2DigMaintenanceWindow";
        const string ArgNameLevel2DigMaintenanceWindowStartTimeUtc = "DIGSettings:Services:DatabaseMaintenanceService:Level2DigMaintenanceWindowStartTimeUTC";
        const string ArgNameLevel2DigMaintenanceWindowMaxMinutes = "DIGSettings:Services:DatabaseMaintenanceService:Level2DigMaintenanceWindowMaxMinutes";

        // Arbitrary timeout limits:
        const int DefaultTimeoutSeconds = 30;
        const int MinTimeoutSeconds = 10;
        const int MaxTimeoutSeconds = 10800;

        // TelemetryDataService limits:
        /// <summary>
        /// The number of iterations a record whose device cannot be found is held in its queue before it is moved to a fail table. Holding exists to cover the window between a feeder starting to transmit for a device and that device being authorised; it is deliberately short, because a record that never resolves is a data problem rather than a timing one.
        /// <para>
        /// It is defined here, next to <see cref="MinTelemetryDataServiceMaxClaimAttempts"/>, because the two are one invariant rather than two settings. Both count the same <c>RetryCount</c> column, at two cadences: the claim procedures increment it on a stale reclaim every 30 minutes, and the hold path increments it once per service iteration. Iterations are 10 seconds apart by default only when one processed nothing; under load they follow one another at once, so the five holds can pass in seconds - the window is a number of iterations, not a length of time. The claim procedures' exhausted-row <c>DELETE</c> runs BEFORE the claim, so a ceiling at or below this value removes a held record on the very iteration the hold logic would have classified it, and the operator is told "Exceeded N claim attempts." instead of "No device found for ThirdPartyId: ...". Only the second names the real cause and carries the remedy. The floor below is what keeps that state unreachable from configuration.
        /// </para>
        /// </summary>
        public const int MaxDeviceLookupHoldAttempts = 5;
        const int DefaultTelemetryDataServiceMaxClaimAttempts = 10;
        /// <summary>
        /// Strictly greater than <see cref="MaxDeviceLookupHoldAttempts"/>, and so deliberately not <c>1</c> like the equivalent floor on the other two services - neither of which has a hold path. See the note above before changing it; a unit test asserts the relationship.
        /// </summary>
        public const int MinTelemetryDataServiceMaxClaimAttempts = 6;
        const int MaxTelemetryDataServiceMaxClaimAttempts = 100;
        const int DefaultDeviceProvisioningServiceMaxClaimAttempts = 5;
        const int MinDeviceProvisioningServiceMaxClaimAttempts = 1;
        const int MaxDeviceProvisioningServiceMaxClaimAttempts = 100;
        const int DefaultTelemetryDataServiceBatchSizePerType = 1000;
        const int MinTelemetryDataServiceBatchSizePerType = 1;
        const int MaxTelemetryDataServiceBatchSizePerType = 5000; // DIG API limit
        const int DefaultTelemetryDataServiceIntervalSeconds = 10;
        const int MinTelemetryDataServiceIntervalSeconds = 1;
        const int MaxTelemetryDataServiceIntervalSeconds = 3600;
        const int DefaultProvisionedDeviceCacheRefreshIntervalSeconds = 300; // 5 minutes
        const int MinProvisionedDeviceCacheRefreshIntervalSeconds = 10;
        const int MaxProvisionedDeviceCacheRefreshIntervalSeconds = 86400; // 24 hours

        // DeviceSubscriptionService limits. Every one of these keys is read with a default-tolerant
        // accessor, because the section does not exist in a configuration file written for an earlier
        // version and a missing key would otherwise stop the adapter at boot.
        // Defaults to the subscription control plane: most deployments of this adapter are expected to be
        // OEMs told which devices to transmit for, rather than manufacturers provisioning their own through
        // MyAdmin. A configuration file that predates this setting and still runs the provisioning workflow
        // therefore has to declare that, and is refused at startup with a message naming the key until it
        // does - a loud, one-line fix, rather than a silent change of control plane.
        const DIGAdapterOperatingMode DefaultOperatingMode = DIGAdapterOperatingMode.OemSubscription;
        const bool DefaultEnableDeviceSubscriptionService = false;
        const int DefaultDeviceSubscriptionServiceIntervalSeconds = 10;
        const int MinDeviceSubscriptionServiceIntervalSeconds = 1;
        const int MaxDeviceSubscriptionServiceIntervalSeconds = 3600;
        const int DefaultDeviceSubscriptionServiceBatchSize = 1000;
        const int MinDeviceSubscriptionServiceBatchSize = 1;
        const int MaxDeviceSubscriptionServiceBatchSize = 5000;
        const int DefaultDeviceSubscriptionServiceMaxClaimAttempts = 5;
        const int MinDeviceSubscriptionServiceMaxClaimAttempts = 1;
        const int MaxDeviceSubscriptionServiceMaxClaimAttempts = 100;
        const string DefaultExpectedIdentifierTypes = "Vin";
        const int DefaultMaxTerminationsPerWindow = 50;
        const int MinMaxTerminationsPerWindow = 0; // 0 disables the termination guard.
        const int MaxMaxTerminationsPerWindow = 1000000;
        const int DefaultTerminationWindowMinutes = 60;
        const int MinTerminationWindowMinutes = 1;
        const int MaxTerminationWindowMinutes = 43200; // 43200 min = 30 days
        const int DefaultStaleStagingBatchMinutes = 30;
        const int MinStaleStagingBatchMinutes = 1;
        const int MaxStaleStagingBatchMinutes = 1440; // 1440 min = 24 hours
        const int DefaultMinimumExpectedDeviceCount = 0;
        const int MinMinimumExpectedDeviceCount = 0;
        const int MaxMinimumExpectedDeviceCount = 1000000;

        // InvalidRecordRetrievalService limits:
        const int DefaultInvalidRecordRetrievalServiceIntervalSeconds = 3600; // 1 hour
        const int MinInvalidRecordRetrievalServiceIntervalSeconds = 60; // 1 minute
        const int MaxInvalidRecordRetrievalServiceIntervalSeconds = 86400; // 24 hours
        const int DefaultInvalidRecordRetrievalServiceBatchSize = 1000;
        const int MinInvalidRecordRetrievalServiceBatchSize = 1;
        const int MaxInvalidRecordRetrievalServiceBatchSize = 50000; // DIG API limit

        // DatabaseMaintenanceService limits. Read with default-tolerant accessors, because the whole
        // section is absent from a configuration file written for an earlier version and a missing key
        // would otherwise stop the adapter at boot. Defaults are chosen so that omitting the section
        // entirely produces the intended behaviour: partitions are maintained, and failure records are
        // reclaimed after 90 days.
        const bool DefaultEnableDatabaseMaintenanceService = true;
        const int DefaultDatabaseMaintenanceServiceIntervalMinutes = 1440; // 24 hours
        const int MinDatabaseMaintenanceServiceIntervalMinutes = 1;
        const int MaxDatabaseMaintenanceServiceIntervalMinutes = 43200; // 43200 min = 30 days
        const string DefaultPartitionInterval = "monthly";
        const bool DefaultEnableAutomaticRetention = true;
        const int DefaultFailTableRetentionDays = 90;
        const int MinFailTableRetentionDays = 1;
        const int MaxFailTableRetentionDays = 36500; // 100 years, i.e. effectively "never"
        const int DefaultQueueDepthWarningThreshold = 100000;
        const int MinQueueDepthWarningThreshold = 0; // 0 = report the depths, but never warn about them
        const int MaxQueueDepthWarningThreshold = 1000000000;

        // Level 1 and Level 2 database maintenance limits. The limits match the MyGeotab API Adapter's for the
        // same settings, so that one understanding covers both adapters; the Level 2 default interval does
        // not. Level 2 pauses telemetry for its offline rebuilds, and this adapter's queue tables are the highest-churn
        // tables in the solution, so hourly - the API Adapter's default - is the wrong default for an adapter
        // whose existing maintenance cadence is daily.
        const bool DefaultEnableLevel1DigMaintenance = true;
        const int DefaultLevel1DigMaintenanceIntervalMinutes = 30;
        const bool DefaultEnableLevel2DigMaintenance = true;
        const int DefaultLevel2DigMaintenanceIntervalMinutes = 1440; // 24 hours
        const int MinDigMaintenanceLevelIntervalMinutes = 10;
        const int MaxDigMaintenanceLevelIntervalMinutes = 43200; // 43200 min = 30 days
        const bool DefaultEnableLevel2DigMaintenanceWindow = false;
        const string DefaultLevel2DigMaintenanceWindowStartTimeUtc = "2020-06-23T06:00:00Z";
        const int DefaultLevel2DigMaintenanceWindowMaxMinutes = 60;
        const int MinLevel2DigMaintenanceWindowMaxMinutes = 10;
        const int MaxLevel2DigMaintenanceWindowMaxMinutes = 720; // 720 min = 12 hours

        /// <inheritdoc/>
        public string DatabaseConnectionString { get; private set; }

        /// <inheritdoc/>
        public string DatabaseProviderType { get; private set; }

        /// <inheritdoc/>
        public string DIGAPIEndpoint { get; private set; }

        /// <inheritdoc/>
        public string DIGPassword { get; private set; }

        /// <inheritdoc/>
        public string DIGUser { get; private set; }

        /// <inheritdoc/>
        public bool DisableMachineNameValidation { get; private set; }

        /// <inheritdoc/>
        public bool EnableDeviceProvisioningService { get; private set; } = true;

        /// <inheritdoc/>
        public bool EnableDeviceDIGReadinessService { get; private set; } = true;

        /// <inheritdoc/>
        public bool EnableDeviceSubscriptionService { get; private set; }

        /// <inheritdoc/>
        public int DeviceSubscriptionServiceIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public int DeviceSubscriptionServiceBatchSize { get; private set; }

        /// <inheritdoc/>
        public int DeviceSubscriptionServiceMaxClaimAttempts { get; private set; }

        /// <inheritdoc/>
        public string ExpectedIdentifierTypes { get; private set; }

        /// <inheritdoc/>
        public int MaxTerminationsPerWindow { get; private set; }

        /// <inheritdoc/>
        public int TerminationWindowMinutes { get; private set; }

        /// <inheritdoc/>
        public int StaleStagingBatchMinutes { get; private set; }

        /// <inheritdoc/>
        public int MinimumExpectedDeviceCount { get; private set; }

        /// <inheritdoc/>
        public bool EnableTelemetryDataService { get; private set; } = true;

        /// <inheritdoc/>
        public bool EnableAccelerationRecords { get; private set; } = true;

        /// <inheritdoc/>
        public bool EnableBinaryRecords { get; private set; } = true;

        /// <inheritdoc/>
        public bool EnableBluetoothRecords { get; private set; } = true;

        /// <inheritdoc/>
        public bool EnableDriverChangeRecords { get; private set; } = true;

        /// <inheritdoc/>
        public bool EnableGenericFaultRecords { get; private set; } = true;

        /// <inheritdoc/>
        public bool EnableGenericStatusRecords { get; private set; } = true;

        /// <inheritdoc/>
        public bool EnableGpsRecords { get; private set; } = true;

        /// <inheritdoc/>
        public bool EnableJ1708FaultRecords { get; private set; } = true;

        /// <inheritdoc/>
        public bool EnableJ1939FaultRecords { get; private set; } = true;

        /// <inheritdoc/>
        public bool EnableObdiiFaultRecords { get; private set; } = true;

        /// <inheritdoc/>
        public bool EnableVinRecords { get; private set; } = true;

        /// <inheritdoc/>
        public bool EnableInvalidRecordRetrievalService { get; private set; } = true;

        /// <inheritdoc/>
        public int InvalidRecordRetrievalServiceIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public int InvalidRecordRetrievalServiceBatchSize { get; private set; }

        /// <inheritdoc/>
        public bool EnableDatabaseMaintenanceService { get; private set; } = DefaultEnableDatabaseMaintenanceService;

        /// <inheritdoc/>
        public int DatabaseMaintenanceServiceIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public string PartitionInterval { get; private set; } = DefaultPartitionInterval;

        /// <inheritdoc/>
        public bool EnableAutomaticRetention { get; private set; } = DefaultEnableAutomaticRetention;

        /// <inheritdoc/>
        public int FailTableRetentionDays { get; private set; }

        /// <inheritdoc/>
        public int QueueDepthWarningThreshold { get; private set; }

        /// <inheritdoc/>
        public bool EnableLevel1DigMaintenance { get; private set; } = DefaultEnableLevel1DigMaintenance;

        /// <inheritdoc/>
        public int Level1DigMaintenanceIntervalMinutes { get; private set; } = DefaultLevel1DigMaintenanceIntervalMinutes;

        /// <inheritdoc/>
        public bool EnableLevel2DigMaintenance { get; private set; } = DefaultEnableLevel2DigMaintenance;

        /// <inheritdoc/>
        public int Level2DigMaintenanceIntervalMinutes { get; private set; } = DefaultLevel2DigMaintenanceIntervalMinutes;

        /// <inheritdoc/>
        public bool EnableLevel2DigMaintenanceWindow { get; private set; } = DefaultEnableLevel2DigMaintenanceWindow;

        /// <inheritdoc/>
        public DateTime Level2DigMaintenanceWindowStartTimeUtc { get; private set; }

        /// <inheritdoc/>
        public int Level2DigMaintenanceWindowMaxMinutes { get; private set; } = DefaultLevel2DigMaintenanceWindowMaxMinutes;

        /// <inheritdoc/>
        public int TelemetryDataServiceBatchSizePerType { get; private set; }

        /// <inheritdoc/>
        public int TelemetryDataServiceMaxClaimAttempts { get; private set; }

        /// <inheritdoc/>
        public int DeviceProvisioningServiceMaxClaimAttempts { get; private set; }

        /// <inheritdoc/>
        public int TelemetryDataServiceIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public int ProvisionedDeviceCacheRefreshIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <inheritdoc/>
        public string MyAdminAPIEndpoint { get; private set; }

        /// <inheritdoc/>
        public string MyAdminPassword { get; private set; }

        /// <inheritdoc/>
        public string MyAdminUser { get; private set; }

        /// <inheritdoc/>
        public string PromoCode { get; private set; }

        /// <inheritdoc/>
        public bool RequiresMyAdmin { get => EnableDeviceProvisioningService || EnableDeviceDIGReadinessService; }

        /// <inheritdoc/>
        public DIGAdapterOperatingMode OperatingMode { get; private set; } = DefaultOperatingMode;

        /// <inheritdoc/>
        public int TimeoutSecondsForDatabaseTasks { get; private set; }

        /// <inheritdoc/>
        public int TimeoutSecondsForDIGTasks { get; private set; }

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IConfigurationHelper configurationHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="DIGAdapterConfiguration"/> class.
        /// </summary>
        public DIGAdapterConfiguration(IConfigurationHelper configurationHelper)
        {
            this.configurationHelper = configurationHelper;
            ProcessConfigItems();

            Id = Guid.NewGuid().ToString();
            logger.Debug($"{nameof(DIGAdapterConfiguration)} [Id: {Id}] created.");
        }

        /// <inheritdoc/>
        public void ProcessConfigItems()
        {
            logger.Info($"Processing configuration items.");

            // OverrideSettings:
            DisableMachineNameValidation = configurationHelper.GetConfigKeyValueBoolean(ArgNameDisableMachineNameValidation);

            // DatabaseSettings:
            DatabaseConnectionString = configurationHelper.GetConfigKeyValueString(ArgNameDatabaseConnectionString, null, true, true);
            DatabaseProviderType = configurationHelper.GetConfigKeyValueString(ArgNameDatabaseProviderType);

            // DIGSettings:
            DIGAPIEndpoint = configurationHelper.GetConfigKeyValueString(ArgNameDIGAPIEndpoint);
            DIGUser = configurationHelper.GetConfigKeyValueString(ArgNameDIGUser);
            DIGPassword = configurationHelper.GetConfigKeyValueString(ArgNameDIGPassword, null, true, true);

            // AppSettings:GeneralSettings:
            TimeoutSecondsForDatabaseTasks = configurationHelper.GetConfigKeyValueInt(ArgNameTimeoutSecondsForDatabaseTasks, null, false, MinTimeoutSeconds, MaxTimeoutSeconds, DefaultTimeoutSeconds);
            TimeoutSecondsForDIGTasks = configurationHelper.GetConfigKeyValueInt(ArgNameTimeoutSecondsForDIGTasks, null, false, MinTimeoutSeconds, MaxTimeoutSeconds, DefaultTimeoutSeconds);

            // DIGSettings:Services:DeviceProvisioningService:
            EnableDeviceProvisioningService = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDeviceProvisioningService);
            DeviceProvisioningServiceMaxClaimAttempts = configurationHelper.GetConfigKeyValueIntOrDefault(ArgNameDeviceProvisioningServiceMaxClaimAttempts, DefaultDeviceProvisioningServiceMaxClaimAttempts, null, false, MinDeviceProvisioningServiceMaxClaimAttempts, MaxDeviceProvisioningServiceMaxClaimAttempts);

            // DIGSettings:Services:DeviceDIGReadinessService:
            EnableDeviceDIGReadinessService = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDeviceDIGReadinessService);

            // MyAdminSettings:
            // Deliberately read AFTER the two enable flags above, because whether these settings are required
            // depends on them. They use default-tolerant accessors so that a deployment with no MyAdmin account
            // can omit them entirely; ValidateMyAdminSettings then re-imposes the requirement on the deployments
            // that actually call MyAdmin. Reading them as unconditionally-required is what previously made a
            // MyAdmin account mandatory to start the application at all, regardless of which services were on.
            MyAdminAPIEndpoint = configurationHelper.GetConfigKeyValueStringOrDefault(ArgNameMyAdminAPIEndpoint, string.Empty);
            MyAdminUser = configurationHelper.GetConfigKeyValueStringOrDefault(ArgNameMyAdminUser, string.Empty);
            MyAdminPassword = configurationHelper.GetConfigKeyValueStringOrDefault(ArgNameMyAdminPassword, string.Empty, null, true, true);
            // PromoCode is never validated: nothing in the solution reads this property. The promo code that
            // reaches MyAdmin is the per-row value carried on the Q_ProvisionDevices record.
            PromoCode = configurationHelper.GetConfigKeyValueStringOrDefault(ArgNamePromoCode, string.Empty, null, true, true);
            ValidateMyAdminSettings();

            // DIGSettings:Services:DeviceSubscriptionService:
            EnableDeviceSubscriptionService = configurationHelper.GetConfigKeyValueBooleanOrDefault(ArgNameEnableDeviceSubscriptionService, DefaultEnableDeviceSubscriptionService);
            DeviceSubscriptionServiceIntervalSeconds = configurationHelper.GetConfigKeyValueIntOrDefault(ArgNameDeviceSubscriptionServiceIntervalSeconds, DefaultDeviceSubscriptionServiceIntervalSeconds, null, false, MinDeviceSubscriptionServiceIntervalSeconds, MaxDeviceSubscriptionServiceIntervalSeconds);
            DeviceSubscriptionServiceBatchSize = configurationHelper.GetConfigKeyValueIntOrDefault(ArgNameDeviceSubscriptionServiceBatchSize, DefaultDeviceSubscriptionServiceBatchSize, null, false, MinDeviceSubscriptionServiceBatchSize, MaxDeviceSubscriptionServiceBatchSize);
            DeviceSubscriptionServiceMaxClaimAttempts = configurationHelper.GetConfigKeyValueIntOrDefault(ArgNameDeviceSubscriptionServiceMaxClaimAttempts, DefaultDeviceSubscriptionServiceMaxClaimAttempts, null, false, MinDeviceSubscriptionServiceMaxClaimAttempts, MaxDeviceSubscriptionServiceMaxClaimAttempts);
            ExpectedIdentifierTypes = configurationHelper.GetConfigKeyValueStringOrDefault(ArgNameExpectedIdentifierTypes, DefaultExpectedIdentifierTypes);
            MaxTerminationsPerWindow = configurationHelper.GetConfigKeyValueIntOrDefault(ArgNameMaxTerminationsPerWindow, DefaultMaxTerminationsPerWindow, null, false, MinMaxTerminationsPerWindow, MaxMaxTerminationsPerWindow);
            TerminationWindowMinutes = configurationHelper.GetConfigKeyValueIntOrDefault(ArgNameTerminationWindowMinutes, DefaultTerminationWindowMinutes, null, false, MinTerminationWindowMinutes, MaxTerminationWindowMinutes);
            StaleStagingBatchMinutes = configurationHelper.GetConfigKeyValueIntOrDefault(ArgNameStaleStagingBatchMinutes, DefaultStaleStagingBatchMinutes, null, false, MinStaleStagingBatchMinutes, MaxStaleStagingBatchMinutes);
            MinimumExpectedDeviceCount = configurationHelper.GetConfigKeyValueIntOrDefault(ArgNameMinimumExpectedDeviceCount, DefaultMinimumExpectedDeviceCount, null, false, MinMinimumExpectedDeviceCount, MaxMinimumExpectedDeviceCount);

            // DIGSettings:Services:TelemetryDataService:
            EnableTelemetryDataService = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableTelemetryDataService);
            TelemetryDataServiceBatchSizePerType = configurationHelper.GetConfigKeyValueInt(ArgNameTelemetryDataServiceBatchSizePerType, null, false, MinTelemetryDataServiceBatchSizePerType, MaxTelemetryDataServiceBatchSizePerType, DefaultTelemetryDataServiceBatchSizePerType);
            TelemetryDataServiceMaxClaimAttempts = configurationHelper.GetConfigKeyValueIntOrDefault(ArgNameTelemetryDataServiceMaxClaimAttempts, DefaultTelemetryDataServiceMaxClaimAttempts, null, false, MinTelemetryDataServiceMaxClaimAttempts, MaxTelemetryDataServiceMaxClaimAttempts);
            TelemetryDataServiceIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameTelemetryDataServiceIntervalSeconds, null, false, MinTelemetryDataServiceIntervalSeconds, MaxTelemetryDataServiceIntervalSeconds, DefaultTelemetryDataServiceIntervalSeconds);
            ProvisionedDeviceCacheRefreshIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameProvisionedDeviceCacheRefreshIntervalSeconds, null, false, MinProvisionedDeviceCacheRefreshIntervalSeconds, MaxProvisionedDeviceCacheRefreshIntervalSeconds, DefaultProvisionedDeviceCacheRefreshIntervalSeconds);
            EnableAccelerationRecords = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableAccelerationRecords);
            EnableBinaryRecords = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableBinaryRecords);
            EnableBluetoothRecords = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableBluetoothRecords);
            EnableDriverChangeRecords = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDriverChangeRecords);
            EnableGenericFaultRecords = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableGenericFaultRecords);
            EnableGenericStatusRecords = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableGenericStatusRecords);
            EnableGpsRecords = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableGpsRecords);
            EnableJ1708FaultRecords = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableJ1708FaultRecords);
            EnableJ1939FaultRecords = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableJ1939FaultRecords);
            EnableObdiiFaultRecords = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableObdiiFaultRecords);
            EnableVinRecords = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableVinRecords);

            // DIGSettings:Services:InvalidRecordRetrievalService:
            EnableInvalidRecordRetrievalService = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableInvalidRecordRetrievalService);
            InvalidRecordRetrievalServiceIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameInvalidRecordRetrievalServiceIntervalSeconds, null, false, MinInvalidRecordRetrievalServiceIntervalSeconds, MaxInvalidRecordRetrievalServiceIntervalSeconds, DefaultInvalidRecordRetrievalServiceIntervalSeconds);
            InvalidRecordRetrievalServiceBatchSize = configurationHelper.GetConfigKeyValueInt(ArgNameInvalidRecordRetrievalServiceBatchSize, null, false, MinInvalidRecordRetrievalServiceBatchSize, MaxInvalidRecordRetrievalServiceBatchSize, DefaultInvalidRecordRetrievalServiceBatchSize);

            // DIGSettings:Services:DatabaseMaintenanceService:
            // Deliberately NOT part of ValidateOperatingMode. That setting's contract is which device control
            // plane a deployment runs, and retention is orthogonal to both of them.
            EnableDatabaseMaintenanceService = configurationHelper.GetConfigKeyValueBooleanOrDefault(ArgNameEnableDatabaseMaintenanceService, DefaultEnableDatabaseMaintenanceService);
            DatabaseMaintenanceServiceIntervalMinutes = configurationHelper.GetConfigKeyValueIntOrDefault(ArgNameDatabaseMaintenanceServiceIntervalMinutes, DefaultDatabaseMaintenanceServiceIntervalMinutes, null, false, MinDatabaseMaintenanceServiceIntervalMinutes, MaxDatabaseMaintenanceServiceIntervalMinutes);
            PartitionInterval = ReadPartitionInterval();
            EnableAutomaticRetention = configurationHelper.GetConfigKeyValueBooleanOrDefault(ArgNameEnableAutomaticRetention, DefaultEnableAutomaticRetention);
            FailTableRetentionDays = configurationHelper.GetConfigKeyValueIntOrDefault(ArgNameFailTableRetentionDays, DefaultFailTableRetentionDays, null, false, MinFailTableRetentionDays, MaxFailTableRetentionDays);
            QueueDepthWarningThreshold = configurationHelper.GetConfigKeyValueIntOrDefault(ArgNameQueueDepthWarningThreshold, DefaultQueueDepthWarningThreshold, null, false, MinQueueDepthWarningThreshold, MaxQueueDepthWarningThreshold);
            EnableLevel1DigMaintenance = configurationHelper.GetConfigKeyValueBooleanOrDefault(ArgNameEnableLevel1DigMaintenance, DefaultEnableLevel1DigMaintenance);
            Level1DigMaintenanceIntervalMinutes = configurationHelper.GetConfigKeyValueIntOrDefault(ArgNameLevel1DigMaintenanceIntervalMinutes, DefaultLevel1DigMaintenanceIntervalMinutes, null, false, MinDigMaintenanceLevelIntervalMinutes, MaxDigMaintenanceLevelIntervalMinutes);
            EnableLevel2DigMaintenance = configurationHelper.GetConfigKeyValueBooleanOrDefault(ArgNameEnableLevel2DigMaintenance, DefaultEnableLevel2DigMaintenance);
            Level2DigMaintenanceIntervalMinutes = configurationHelper.GetConfigKeyValueIntOrDefault(ArgNameLevel2DigMaintenanceIntervalMinutes, DefaultLevel2DigMaintenanceIntervalMinutes, null, false, MinDigMaintenanceLevelIntervalMinutes, MaxDigMaintenanceLevelIntervalMinutes);
            EnableLevel2DigMaintenanceWindow = configurationHelper.GetConfigKeyValueBooleanOrDefault(ArgNameEnableLevel2DigMaintenanceWindow, DefaultEnableLevel2DigMaintenanceWindow);
            Level2DigMaintenanceWindowStartTimeUtc = ReadLevel2DigMaintenanceWindowStartTimeUtc();
            Level2DigMaintenanceWindowMaxMinutes = configurationHelper.GetConfigKeyValueIntOrDefault(ArgNameLevel2DigMaintenanceWindowMaxMinutes, DefaultLevel2DigMaintenanceWindowMaxMinutes, null, false, MinLevel2DigMaintenanceWindowMaxMinutes, MaxLevel2DigMaintenanceWindowMaxMinutes);
            WarnAboutRetiredLevel1DatabaseMaintenanceKeys();
            if (EnableDatabaseMaintenanceService == true && EnableLevel2DigMaintenance == true)
            {
                // Info rather than Warn: this is the shipped default, and a warning on every startup for a
                // setting nobody changed is one an operator learns to skip. The consequence is still stated,
                // because it is the one thing about Level 2 that surprises people.
                var window = EnableLevel2DigMaintenanceWindow == true
                    ? $"within a daily window starting at {Level2DigMaintenanceWindowStartTimeUtc.TimeOfDay:hh\\:mm} UTC and lasting {Level2DigMaintenanceWindowMaxMinutes} minute(s)"
                    : $"whenever its interval has elapsed, at any hour - set '{ArgNameEnableLevel2DigMaintenanceWindow}' to true to confine it to a daily window";
                logger.Info($"Level 2 database maintenance is enabled, every {Level2DigMaintenanceIntervalMinutes} minute(s), {window}. The other services are PAUSED for its offline rebuilds, so telemetry is not transmitted while they run.");
            }
            if (EnableDatabaseMaintenanceService == true && EnableAutomaticRetention == true)
            {
                logger.Warn($"AUTOMATIC RETENTION IS ENABLED. Failure records and invalid records older than {FailTableRetentionDays} day(s) will be DELETED and cannot be recovered. Set '{ArgNameEnableAutomaticRetention}' to false to retain them indefinitely.");
            }
            if (EnableDatabaseMaintenanceService == false)
            {
                // Said plainly because the alternative is silence. An operator who turns maintenance off - or who
                // turns off something they believe is maintenance - otherwise gets no signal at all that partition
                // management, retention and queue-depth monitoring are all inert, and the queue and fail tables
                // then grow without bound. Warned unconditionally rather than only for keys the operator set:
                // the ...OrDefault accessors cannot tell a missing key from one defaulted to the same value.
                logger.Warn($"The DatabaseMaintenanceService is DISABLED ('{ArgNameEnableDatabaseMaintenanceService}' is false). Partition management, automatic retention, queue-depth monitoring and Level 1 and Level 2 database maintenance will NOT run, and the other DatabaseMaintenanceService settings have no effect. The queue and fail tables will grow without bound.");
            }

            // DIGSettings:OperatingMode - read last, because validating it needs every enable flag above.
            OperatingMode = ReadOperatingMode();
            ValidateOperatingMode();

            // Deliberately a warning and not a ValidateOperatingMode() contradiction. A deployment that
            // manages ProvisionedDevices by hand - setting IsOkayToSendDataToGeotab itself rather than
            // letting DeviceDIGReadinessService set it - is a legitimate configuration, so refusing to
            // start would break it. The commoner case by far is an operator who turned the service off
            // without realising nothing else ever flips that flag: provisioning then succeeds, telemetry
            // never flows, and no signal is emitted anywhere. Every other route to a deployment that can
            // never transmit is already refused at startup; this was the one that stayed silent.
            if ((OperatingMode == DIGAdapterOperatingMode.DeviceProvisioning || OperatingMode == DIGAdapterOperatingMode.Both)
                && EnableDeviceProvisioningService == true && EnableDeviceDIGReadinessService == false)
            {
                logger.Warn($"The DeviceDIGReadinessService is DISABLED ('{ArgNameEnableDeviceDIGReadinessService}' is false) while device provisioning is enabled. Newly provisioned devices are inserted with 'IsOkayToSendDataToGeotab' set to false and nothing will ever set it to true, so no telemetry will be transmitted for them. Set '{ArgNameEnableDeviceDIGReadinessService}' to true, or set 'IsOkayToSendDataToGeotab' yourself once each device has been added to a MyGeotab database.");
            }
        }

        /// <summary>
        /// Reads the declared operating mode, defaulting to <see cref="DIGAdapterOperatingMode.OemSubscription"/>.
        /// <para>
        /// A configuration file that predates this setting therefore does NOT keep its current behaviour: with the
        /// device provisioning service still enabled and the mode defaulting to OemSubscription,
        /// <see cref="ValidateOperatingMode"/> refuses to start until the file declares
        /// <see cref="DIGAdapterOperatingMode.DeviceProvisioning"/>. That refusal is deliberate, and the 5.2.0.0
        /// upgrade guide leads with it.
        /// </para>
        /// </summary>
        /// <exception cref="Exception">The configured value is not a recognized mode.</exception>
        DIGAdapterOperatingMode ReadOperatingMode()
        {
            var operatingModeString = configurationHelper.GetConfigKeyValueStringOrDefault(ArgNameOperatingMode, DefaultOperatingMode.ToString());
            if (ConfigurationEnumParser.TryParse(operatingModeString, out DIGAdapterOperatingMode operatingMode) == true)
            {
                return operatingMode;
            }

            var errorMessage = $"The configuration item '{ArgNameOperatingMode}' has the value '{operatingModeString}', which is not a recognized operating mode. Valid values are: {ConfigurationEnumParser.DescribeValidValues<DIGAdapterOperatingMode>()}.";
            logger.Error(errorMessage);
            throw new Exception(errorMessage);
        }

        /// <summary>
        /// Reads the partition interval, rejecting anything the partitioning procedures do not accept.
        /// <para>
        /// Returned in lower case because the procedures compare the value literally: 'Monthly' reaches the
        /// database, fails its own validation there, and turns a typo into a maintenance service that logs an
        /// error on every iteration instead of one that refuses to start.
        /// </para>
        /// </summary>
        /// <exception cref="Exception">The configured value is not one the partitioning procedures accept.</exception>
        string ReadPartitionInterval()
        {
            // The configured value is kept as supplied for the error message below. Reporting the trimmed,
            // lower-cased value instead tells an operator their file says something it does not - they cannot
            // find 'Montly ' by searching for 'montly' - and ReadOperatingMode already quotes the raw value.
            var configuredPartitionInterval = configurationHelper.GetConfigKeyValueStringOrDefault(ArgNamePartitionInterval, DefaultPartitionInterval);
            var partitionInterval = configuredPartitionInterval.Trim().ToLowerInvariant();
            if (partitionInterval == "monthly" || partitionInterval == "weekly" || partitionInterval == "daily")
            {
                return partitionInterval;
            }

            var errorMessage = $"The configuration item '{ArgNamePartitionInterval}' has the value '{configuredPartitionInterval}', which is not a supported partition interval. Valid values are: 'monthly', 'weekly', 'daily'.";
            logger.Error(errorMessage);
            throw new Exception(errorMessage);
        }

        /// <summary>
        /// Warns when a configuration file still carries the two Level 1 keys this adapter once copied from the MyGeotab
        /// API Adapter and never read.
        /// <para>
        /// They were harmless while this adapter had no Level 1 maintenance: they did nothing, as they always had. Now that
        /// it has, an operator who set one of them to false expecting to switch Level 1 off would get Level 1 anyway, with
        /// nothing to say why. The keys that do control it live in the DatabaseMaintenanceService section. A warning, not a
        /// refusal: a carried-forward file containing them must still start.
        /// </para>
        /// </summary>
        void WarnAboutRetiredLevel1DatabaseMaintenanceKeys()
        {
            var databaseSettings = configurationHelper.GetConfigSection("DatabaseSettings");
            var retiredKeysPresent = new[] { "EnableLevel1DatabaseMaintenance", "Level1DatabaseMaintenanceIntervalMinutes" }
                .Where(key => databaseSettings.GetSection(key).Value != null)
                .Select(key => $"DatabaseSettings:{key}")
                .ToList();
            if (retiredKeysPresent.Count > 0)
            {
                logger.Warn($"The configuration contains {string.Join(" and ", retiredKeysPresent.Select(key => $"'{key}'"))}, which this adapter does NOT read. Level 1 database maintenance is controlled by '{ArgNameEnableLevel1DigMaintenance}' and '{ArgNameLevel1DigMaintenanceIntervalMinutes}' instead, and is currently {(EnableLevel1DigMaintenance == true ? "ENABLED" : "disabled")}. Remove the retired key(s) to silence this warning.");
            }
        }

        /// <summary>
        /// Reads the start of the daily Level 2 maintenance window, supplying the default when the key is absent.
        /// <para>
        /// Parsed exactly as the MyGeotab API Adapter parses its equivalent setting, so that one value means the
        /// same thing in both adapters. Only its time of day is ever used. It is read with a default-tolerant
        /// accessor because a configuration file written for an earlier version has no such key, and a missing
        /// key must not stop the adapter at boot; a value that is present but is not a date and time still does.
        /// </para>
        /// </summary>
        /// <exception cref="Exception">The configured value is not a date and time.</exception>
        DateTime ReadLevel2DigMaintenanceWindowStartTimeUtc()
        {
            var configuredStartTime = configurationHelper.GetConfigKeyValueStringOrDefault(ArgNameLevel2DigMaintenanceWindowStartTimeUtc, DefaultLevel2DigMaintenanceWindowStartTimeUtc);
            // Always UTC, whatever the host's time zone: a value with an offset is converted to UTC, and one without is taken to
            // be UTC already. Parsed as a round trip, a value with an offset became the host's LOCAL time - and only the time of
            // day is used - so on a host not running UTC the window moved by the host's own offset.
            if (DateTime.TryParse(configuredStartTime, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var startTime))
            {
                return startTime;
            }

            var errorMessage = $"The configuration item '{ArgNameLevel2DigMaintenanceWindowStartTimeUtc}' has the value '{configuredStartTime}', which is not a date and time. Only its time of day is used, and it is read as UTC - for example '{DefaultLevel2DigMaintenanceWindowStartTimeUtc}' starts the window at 06:00 UTC.";
            logger.Error(errorMessage);
            throw new Exception(errorMessage);
        }

        /// <summary>
        /// Rejects a configuration whose declared operating mode contradicts its enable flags.
        /// <para>
        /// Every combination below either cannot work or does not do what the mode says it does, and each one
        /// otherwise fails later and quietly - as a service that waits forever, or as an adapter that transmits
        /// for nothing. Failing at startup, naming the key, is the whole point of the setting.
        /// </para>
        /// </summary>
        /// <exception cref="Exception">The declared mode and the enable flags contradict each other.</exception>
        void ValidateOperatingMode()
        {
            var contradictions = new List<string>();

            if (OperatingMode == DIGAdapterOperatingMode.OemSubscription)
            {
                if (EnableDeviceProvisioningService == true)
                {
                    contradictions.Add($"'{ArgNameEnableDeviceProvisioningService}' is true, which keeps a MyAdmin account mandatory and contradicts the declared mode.");
                }
                if (EnableDeviceDIGReadinessService == true)
                {
                    contradictions.Add($"'{ArgNameEnableDeviceDIGReadinessService}' is true, which keeps a MyAdmin account mandatory and contradicts the declared mode.");
                }
                if (EnableDeviceSubscriptionService == false)
                {
                    contradictions.Add($"'{ArgNameEnableDeviceSubscriptionService}' is false, so nothing would apply subscription instructions and no device would ever become transmittable.");
                }
            }

            if (OperatingMode == DIGAdapterOperatingMode.DeviceProvisioning && EnableDeviceSubscriptionService == true)
            {
                contradictions.Add($"'{ArgNameEnableDeviceSubscriptionService}' is true, which is the other control plane and contradicts the declared mode. Use '{nameof(DIGAdapterOperatingMode.Both)}' to run both.");
            }

            if (OperatingMode == DIGAdapterOperatingMode.Both && EnableDeviceProvisioningService == false && EnableDeviceSubscriptionService == false)
            {
                contradictions.Add($"neither '{ArgNameEnableDeviceProvisioningService}' nor '{ArgNameEnableDeviceSubscriptionService}' is true, so no control plane is running.");
            }

            // Independent of the declared mode: transmitting requires a source of devices to transmit for.
            if (EnableTelemetryDataService == true && EnableDeviceProvisioningService == false && EnableDeviceSubscriptionService == false)
            {
                contradictions.Add($"'{ArgNameEnableTelemetryDataService}' is true but neither '{ArgNameEnableDeviceProvisioningService}' nor '{ArgNameEnableDeviceSubscriptionService}' is, so the device lookup would be empty and no telemetry could be transmitted.");
            }

            if (contradictions.Count > 0)
            {
                var errorMessage = $"The configuration is contradictory. '{ArgNameOperatingMode}' is '{OperatingMode}', but: {string.Join(" ", contradictions)}";
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            logger.Info($">{ArgNameOperatingMode}: {OperatingMode}");
        }

        /// <summary>
        /// Throws if a MyAdmin setting is missing that an enabled service will need. Only the services that
        /// actually call MyAdmin make these settings mandatory, so a deployment running neither of them starts
        /// without a MyAdmin account. PromoCode is excluded because nothing reads it.
        /// </summary>
        /// <exception cref="Exception">A required MyAdmin setting is absent or blank.</exception>
        void ValidateMyAdminSettings()
        {
            if (RequiresMyAdmin == false)
            {
                logger.Info($"Neither {nameof(EnableDeviceProvisioningService)} nor {nameof(EnableDeviceDIGReadinessService)} is enabled, so no MyAdmin settings are required. MyAdmin will not be contacted.");
                return;
            }

            var missingSettingNames = new List<string>();
            if (string.IsNullOrWhiteSpace(MyAdminAPIEndpoint))
            {
                missingSettingNames.Add(ArgNameMyAdminAPIEndpoint);
            }
            if (string.IsNullOrWhiteSpace(MyAdminUser))
            {
                missingSettingNames.Add(ArgNameMyAdminUser);
            }
            if (string.IsNullOrWhiteSpace(MyAdminPassword))
            {
                missingSettingNames.Add(ArgNameMyAdminPassword);
            }

            if (missingSettingNames.Count > 0)
            {
                var errorMessage = $"The following required configuration item(s) were not found in the configuration file: '{string.Join("', '", missingSettingNames)}'. These are required because {nameof(EnableDeviceProvisioningService)} or {nameof(EnableDeviceDIGReadinessService)} is enabled and those services call the MyAdmin API. Either supply the MyAdmin settings, or disable both of those services if this deployment does not use the device provisioning workflow.";
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
        }
    }
}