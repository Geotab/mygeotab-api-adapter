namespace MyGeotabAPIAdapter.Configuration
{
    /// <summary>
    /// Interface for a class that reads (from appsettings.json) and stores application configuration settings. Intended for association with the GeotabDIGAdapter project.
    /// </summary>
    public interface IDIGAdapterConfiguration : IDatabaseConfiguration
    {
        /// <summary>
        /// The API endpoint for the DIG service.
        /// </summary>
        string DIGAPIEndpoint { get; }

        /// <summary>
        /// The password to use when authenticating with the DIG API.
        /// </summary>
        string DIGPassword { get; }

        /// <summary>
        /// The username to use when authenticating with the DIG API.
        /// </summary>
        string DIGUser { get; }

        /// <summary>
        /// Indicates whether machine name validation should be disabled. NOTE: This should always be set to <c>false</c> except in scenarios where machine names in hosted environments are not static. WARNING: Improper use of this setting could result in application instability and data integrity issues.
        /// </summary>
        bool DisableMachineNameValidation { get; }

        /// <summary>
        /// Indicates whether the DeviceProvisioningService should be enabled.
        /// </summary>
        bool EnableDeviceProvisioningService { get; }

        /// <summary>
        /// Indicates whether the DeviceDIGReadinessService should be enabled.
        /// </summary>
        bool EnableDeviceDIGReadinessService { get; }

        /// <summary>
        /// Indicates whether the TelemetryDataService should be enabled.
        /// </summary>
        bool EnableTelemetryDataService { get; }

        /// <summary>
        /// Indicates whether AccelerationRecord processing should be enabled in the TelemetryDataService.
        /// </summary>
        bool EnableAccelerationRecords { get; }

        /// <summary>
        /// Indicates whether BinaryRecord processing should be enabled in the TelemetryDataService.
        /// </summary>
        bool EnableBinaryRecords { get; }

        /// <summary>
        /// Indicates whether BluetoothRecord processing should be enabled in the TelemetryDataService.
        /// </summary>
        bool EnableBluetoothRecords { get; }

        /// <summary>
        /// Indicates whether DriverChangeRecord processing should be enabled in the TelemetryDataService.
        /// </summary>
        bool EnableDriverChangeRecords { get; }

        /// <summary>
        /// Indicates whether GenericFaultRecord processing should be enabled in the TelemetryDataService.
        /// </summary>
        bool EnableGenericFaultRecords { get; }

        /// <summary>
        /// Indicates whether GenericStatusRecord processing should be enabled in the TelemetryDataService.
        /// </summary>
        bool EnableGenericStatusRecords { get; }

        /// <summary>
        /// Indicates whether GpsRecord processing should be enabled in the TelemetryDataService.
        /// </summary>
        bool EnableGpsRecords { get; }

        /// <summary>
        /// Indicates whether J1708FaultRecord processing should be enabled in the TelemetryDataService.
        /// </summary>
        bool EnableJ1708FaultRecords { get; }

        /// <summary>
        /// Indicates whether J1939FaultRecord processing should be enabled in the TelemetryDataService.
        /// </summary>
        bool EnableJ1939FaultRecords { get; }

        /// <summary>
        /// Indicates whether ObdiiFaultRecord processing should be enabled in the TelemetryDataService.
        /// </summary>
        bool EnableObdiiFaultRecords { get; }

        /// <summary>
        /// Indicates whether VinRecord processing should be enabled in the TelemetryDataService.
        /// </summary>
        bool EnableVinRecords { get; }

        /// <summary>
        /// Indicates whether the InvalidRecordRetrievalService should be enabled.
        /// </summary>
        bool EnableInvalidRecordRetrievalService { get; }

        /// <summary>
        /// The number of seconds to wait between InvalidRecordRetrievalService iterations.
        /// </summary>
        int InvalidRecordRetrievalServiceIntervalSeconds { get; }

        /// <summary>
        /// The maximum number of records to retrieve per DIG API call in the InvalidRecordRetrievalService.
        /// </summary>
        int InvalidRecordRetrievalServiceBatchSize { get; }

        /// <summary>
        /// The maximum number of records to retrieve per record type in each TelemetryDataService iteration.
        /// </summary>
        int TelemetryDataServiceBatchSizePerType { get; }

        /// <summary>
        /// The number of seconds to wait between TelemetryDataService iterations.
        /// </summary>
        int TelemetryDataServiceIntervalSeconds { get; }

        /// <summary>
        /// The number of seconds between refreshes of the provisioned device cache used by the TelemetryDataService.
        /// </summary>
        int ProvisionedDeviceCacheRefreshIntervalSeconds { get; }

        /// <summary>
        /// Indicates whether "Level 1" database maintenance should be enabled. This includes non-interfering tasks that can be executed outside a maintenance window.
        /// </summary>
        bool EnableLevel1DatabaseMaintenance { get; }

        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The number of minutes to wait between executions of "Level 1" database maintenance.
        /// </summary>
        int Level1DatabaseMaintenanceIntervalMinutes { get; }

        /// <summary>
        /// The API endpoint for the MyAdmin service.
        /// </summary>
        string MyAdminAPIEndpoint { get; }

        /// <summary>
        /// The password to use when authenticating with the MyAdmin API.
        /// </summary>
        string MyAdminPassword { get; }

        /// <summary>
        /// The username to use when authenticating with the MyAdmin API.
        /// </summary>
        string MyAdminUser { get; }

        /// <summary>
        /// The promo code to use with MyAdmin.
        /// </summary>
        string PromoCode { get; }

        /// <summary>
        /// The maximum number of seconds that a DIG API call Task or batch thereof can take to be completed before it is deemed that there is a DIG connectivity issue.
        /// </summary>
        int TimeoutSecondsForDIGTasks { get; }

        /// <summary>
        /// Validates the configuration object values and uses those values to set properties of the <see cref="IDIGAdapterConfiguration"/>.
        /// </summary>
        void ProcessConfigItems();
    }
}
