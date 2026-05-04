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
        const string ArgNameEnableLevel1DatabaseMaintenance = "DatabaseSettings:EnableLevel1DatabaseMaintenance";
        const string ArgNameLevel1DatabaseMaintenanceIntervalMinutes = "DatabaseSettings:Level1DatabaseMaintenanceIntervalMinutes";
        const string ArgNameDatabaseProviderType = "DatabaseSettings:DatabaseProviderType";
        const string ArgNameDatabaseConnectionString = "DatabaseSettings:DatabaseConnectionString";
        // > MyAdminSettings
        const string ArgNameMyAdminAPIEndpoint = "MyAdminSettings:MyAdminAPIEndpoint";
        const string ArgNameMyAdminUser = "MyAdminSettings:MyAdminUser";
        const string ArgNameMyAdminPassword = "MyAdminSettings:MyAdminPassword";
        const string ArgNamePromoCode = "MyAdminSettings:PromoCode";
        // > DIGSettings
        const string ArgNameDIGAPIEndpoint = "DIGSettings:DIGAPIEndpoint";
        const string ArgNameDIGUser = "DIGSettings:DIGUser";
        const string ArgNameDIGPassword = "DIGSettings:DIGPassword";
        // > AppSettings:GeneralSettings
        const string ArgNameTimeoutSecondsForDatabaseTasks = "AppSettings:GeneralSettings:TimeoutSecondsForDatabaseTasks";
        const string ArgNameTimeoutSecondsForDIGTasks = "AppSettings:GeneralSettings:TimeoutSecondsForDIGTasks";
        // > DIGSettings:Services:DeviceProvisioningService
        const string ArgNameEnableDeviceProvisioningService = "DIGSettings:Services:DeviceProvisioningService:EnableDeviceProvisioningService";
        // > DIGSettings:Services:DeviceDIGReadinessService
        const string ArgNameEnableDeviceDIGReadinessService = "DIGSettings:Services:DeviceDIGReadinessService:EnableDeviceDIGReadinessService";
        // > DIGSettings:Services:TelemetryDataService
        const string ArgNameEnableTelemetryDataService = "DIGSettings:Services:TelemetryDataService:EnableTelemetryDataService";
        const string ArgNameTelemetryDataServiceBatchSizePerType = "DIGSettings:Services:TelemetryDataService:BatchSizePerType";
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

        // Database maintenance limits:
        const int MinDatabaseMaintenanceIntervalMinutes = 10;
        const int MaxDatabaseMaintenanceIntervalMinutes = 43200; // 43200 min = 30 days
        const int DefaultLevel1DatabaseMaintenanceIntervalMinutes = 30;

        // Arbitrary timeout limits:
        const int DefaultTimeoutSeconds = 30;
        const int MinTimeoutSeconds = 10;
        const int MaxTimeoutSeconds = 10800;

        // TelemetryDataService limits:
        const int DefaultTelemetryDataServiceBatchSizePerType = 1000;
        const int MinTelemetryDataServiceBatchSizePerType = 1;
        const int MaxTelemetryDataServiceBatchSizePerType = 5000; // DIG API limit
        const int DefaultTelemetryDataServiceIntervalSeconds = 10;
        const int MinTelemetryDataServiceIntervalSeconds = 1;
        const int MaxTelemetryDataServiceIntervalSeconds = 3600;
        const int DefaultProvisionedDeviceCacheRefreshIntervalSeconds = 300; // 5 minutes
        const int MinProvisionedDeviceCacheRefreshIntervalSeconds = 10;
        const int MaxProvisionedDeviceCacheRefreshIntervalSeconds = 86400; // 24 hours

        // InvalidRecordRetrievalService limits:
        const int DefaultInvalidRecordRetrievalServiceIntervalSeconds = 3600; // 1 hour
        const int MinInvalidRecordRetrievalServiceIntervalSeconds = 60; // 1 minute
        const int MaxInvalidRecordRetrievalServiceIntervalSeconds = 86400; // 24 hours
        const int DefaultInvalidRecordRetrievalServiceBatchSize = 1000;
        const int MinInvalidRecordRetrievalServiceBatchSize = 1;
        const int MaxInvalidRecordRetrievalServiceBatchSize = 50000; // DIG API limit

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
        public int TelemetryDataServiceBatchSizePerType { get; private set; }

        /// <inheritdoc/>
        public int TelemetryDataServiceIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public int ProvisionedDeviceCacheRefreshIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public bool EnableLevel1DatabaseMaintenance { get; private set; }

        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <inheritdoc/>
        public int Level1DatabaseMaintenanceIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public string MyAdminAPIEndpoint { get; private set; }

        /// <inheritdoc/>
        public string MyAdminPassword { get; private set; }

        /// <inheritdoc/>
        public string MyAdminUser { get; private set; }

        /// <inheritdoc/>
        public string PromoCode { get; private set; }

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
            EnableLevel1DatabaseMaintenance = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableLevel1DatabaseMaintenance);
            Level1DatabaseMaintenanceIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameLevel1DatabaseMaintenanceIntervalMinutes, null, false, MinDatabaseMaintenanceIntervalMinutes, MaxDatabaseMaintenanceIntervalMinutes, DefaultLevel1DatabaseMaintenanceIntervalMinutes);
            DatabaseConnectionString = configurationHelper.GetConfigKeyValueString(ArgNameDatabaseConnectionString, null, true, true);
            DatabaseProviderType = configurationHelper.GetConfigKeyValueString(ArgNameDatabaseProviderType);

            // MyAdminSettings:
            MyAdminAPIEndpoint = configurationHelper.GetConfigKeyValueString(ArgNameMyAdminAPIEndpoint);
            MyAdminUser = configurationHelper.GetConfigKeyValueString(ArgNameMyAdminUser);
            MyAdminPassword = configurationHelper.GetConfigKeyValueString(ArgNameMyAdminPassword, null, true, true);
            PromoCode = configurationHelper.GetConfigKeyValueString(ArgNamePromoCode, null, true, true);

            // DIGSettings:
            DIGAPIEndpoint = configurationHelper.GetConfigKeyValueString(ArgNameDIGAPIEndpoint);
            DIGUser = configurationHelper.GetConfigKeyValueString(ArgNameDIGUser);
            DIGPassword = configurationHelper.GetConfigKeyValueString(ArgNameDIGPassword, null, true, true);

            // AppSettings:GeneralSettings:
            TimeoutSecondsForDatabaseTasks = configurationHelper.GetConfigKeyValueInt(ArgNameTimeoutSecondsForDatabaseTasks, null, false, MinTimeoutSeconds, MaxTimeoutSeconds, DefaultTimeoutSeconds);
            TimeoutSecondsForDIGTasks = configurationHelper.GetConfigKeyValueInt(ArgNameTimeoutSecondsForDIGTasks, null, false, MinTimeoutSeconds, MaxTimeoutSeconds, DefaultTimeoutSeconds);

            // DIGSettings:Services:DeviceProvisioningService:
            EnableDeviceProvisioningService = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDeviceProvisioningService);

            // DIGSettings:Services:DeviceDIGReadinessService:
            EnableDeviceDIGReadinessService = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDeviceDIGReadinessService);

            // DIGSettings:Services:TelemetryDataService:
            EnableTelemetryDataService = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableTelemetryDataService);
            TelemetryDataServiceBatchSizePerType = configurationHelper.GetConfigKeyValueInt(ArgNameTelemetryDataServiceBatchSizePerType, null, false, MinTelemetryDataServiceBatchSizePerType, MaxTelemetryDataServiceBatchSizePerType, DefaultTelemetryDataServiceBatchSizePerType);
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
        }
    }
}