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
        /// Indicates whether the DeviceSubscriptionService should be enabled. This service drives the subscription control plane, which is an alternative to the device provisioning control plane and is intended for deployments that are told which devices to transmit for rather than provisioning devices themselves.
        /// </summary>
        bool EnableDeviceSubscriptionService { get; }

        /// <summary>
        /// The number of seconds to wait between iterations of the DeviceSubscriptionService.
        /// </summary>
        int DeviceSubscriptionServiceIntervalSeconds { get; }

        /// <summary>
        /// The maximum number of Q_DeviceSubscriptions records to claim and process in a single DeviceSubscriptionService iteration.
        /// </summary>
        int DeviceSubscriptionServiceBatchSize { get; }

        /// <summary>
        /// The number of times a Q_DeviceSubscriptions record may be claimed before it is moved to Q_DeviceSubscriptionsFail. Prevents a permanently-failing record from being reclaimed indefinitely.
        /// </summary>
        int DeviceSubscriptionServiceMaxClaimAttempts { get; }

        /// <summary>
        /// A comma-separated allow-list of the identifier types that may be applied to a subscription. An instruction carrying an identifier type outside this list is rejected. A single fleet can legitimately mix identifier types, so this is a list rather than a single value; it also fails closed if the source begins emitting a different identifier type for devices it previously identified another way, which would otherwise present as a full fleet of new subscriptions and a full fleet of terminations.
        /// </summary>
        string ExpectedIdentifierTypes { get; }

        /// <summary>
        /// The maximum number of subscription terminations permitted within TerminationWindowMinutes. A batch that would exceed this is applied in its entirety or not at all: nothing is applied, the termination guard is tripped, and processing stops until an operator clears the guard. Set to 0 to disable the guard.
        /// </summary>
        int MaxTerminationsPerWindow { get; }

        /// <summary>
        /// The length, in minutes, of the rolling window over which terminations are counted against MaxTerminationsPerWindow.
        /// </summary>
        int TerminationWindowMinutes { get; }

        /// <summary>
        /// The age, in minutes, at which a stg_DeviceSubscriptions batch is considered orphaned by a crash between staging and merging, and is swept.
        /// </summary>
        int StaleStagingBatchMinutes { get; }

        /// <summary>
        /// An absolute floor on the number of devices the adapter expects to be able to transmit for. If the composed device lookup holds fewer than this, an error is logged. A relative-drop warning alone is insufficient, because it makes each new lower count the baseline for the next comparison.
        /// </summary>
        int MinimumExpectedDeviceCount { get; }

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
        /// Indicates whether the DatabaseMaintenanceService should be enabled. This service creates the period partitions that the partitioned tables need, and - if <see cref="EnableAutomaticRetention"/> is set - reclaims the space held by old failure records.
        /// </summary>
        bool EnableDatabaseMaintenanceService { get; }

        /// <summary>
        /// The number of minutes between runs of the DatabaseMaintenanceService's partition management, retention and table-depth report. Level 1 and Level 2 maintenance have intervals of their own; the service wakes at the shortest interval among those enabled, and each task runs only once its own interval has elapsed.
        /// </summary>
        int DatabaseMaintenanceServiceIntervalMinutes { get; }

        /// <summary>
        /// Indicates whether Level 1 database maintenance should be performed: UPDATE STATISTICS on SQL Server, and VACUUM and ANALYZE on PostgreSQL, on the gda tables that need them. These are online operations and the other services are not paused for them.
        /// </summary>
        bool EnableLevel1DigMaintenance { get; }

        /// <summary>
        /// The minimum number of minutes between the starts of two Level 1 database maintenance runs.
        /// </summary>
        int Level1DigMaintenanceIntervalMinutes { get; }

        /// <summary>
        /// Indicates whether Level 2 database maintenance should be performed: index REORGANIZE and REBUILD on SQL Server, and REINDEX on PostgreSQL, on the gda indexes that need them. THE OTHER SERVICES ARE PAUSED WHILE IT RUNS, so telemetry is not transmitted for its duration.
        /// </summary>
        bool EnableLevel2DigMaintenance { get; }

        /// <summary>
        /// The minimum number of minutes between the starts of two Level 2 database maintenance runs.
        /// </summary>
        int Level2DigMaintenanceIntervalMinutes { get; }

        /// <summary>
        /// Indicates whether Level 2 database maintenance is confined to a daily window. When true, a run starts only within the window and stops starting new index operations once the window ends. When false, a run may start whenever its interval has elapsed and processes every candidate index.
        /// </summary>
        bool EnableLevel2DigMaintenanceWindow { get; }

        /// <summary>
        /// The start of the daily Level 2 maintenance window. Only the time of day is used, and it is interpreted as UTC. Has no effect unless <see cref="EnableLevel2DigMaintenanceWindow"/> is true.
        /// </summary>
        DateTime Level2DigMaintenanceWindowStartTimeUtc { get; }

        /// <summary>
        /// The length of the daily Level 2 maintenance window, in minutes. Has no effect unless <see cref="EnableLevel2DigMaintenanceWindow"/> is true.
        /// </summary>
        int Level2DigMaintenanceWindowMaxMinutes { get; }

        /// <summary>
        /// The period each partition of a partitioned table covers: 'monthly', 'weekly' or 'daily'. Pinned on the first run and immutable thereafter, because two partition layouts cannot coexist on one table.
        /// </summary>
        string PartitionInterval { get; }

        /// <summary>
        /// Indicates whether failure records and invalid records older than <see cref="FailTableRetentionDays"/> should be deleted automatically. THIS DELETES DATA, and it is on by default. Set it to false to keep the failure history indefinitely.
        /// </summary>
        bool EnableAutomaticRetention { get; }

        /// <summary>
        /// How many days of failure records to keep. The partitioned fail tables give up only the partitions whose entire period precedes the cutoff, so records there can survive rather longer than this - never less. DIGInvalidRecords cannot be partitioned and is swept by a batched delete instead, so for that table the cutoff applies to each record exactly.
        /// </summary>
        int FailTableRetentionDays { get; }

        /// <summary>
        /// The number of records a single queue, fail or invalid record table may hold before the DatabaseMaintenanceService's depth report warns about it. Set it to zero to report the depths without ever warning.
        /// </summary>
        int QueueDepthWarningThreshold { get; }

        /// <summary>
        /// The maximum number of records to retrieve per record type in each TelemetryDataService iteration.
        /// </summary>
        int TelemetryDataServiceBatchSizePerType { get; }

        /// <summary>
        /// The number of times a telemetry queue record may be claimed before it is treated as poison and moved to its fail table. Applies to all eleven telemetry queues.
        /// </summary>
        int TelemetryDataServiceMaxClaimAttempts { get; }

        /// <summary>
        /// The number of times a <c>Q_ProvisionDevices</c> record may be claimed before it is treated as poison and moved to <c>Q_ProvisionDevicesFail</c>.
        /// </summary>
        int DeviceProvisioningServiceMaxClaimAttempts { get; }

        /// <summary>
        /// The number of seconds to wait between TelemetryDataService iterations.
        /// </summary>
        int TelemetryDataServiceIntervalSeconds { get; }

        /// <summary>
        /// The number of seconds between refreshes of the provisioned device cache used by the TelemetryDataService.
        /// </summary>
        int ProvisionedDeviceCacheRefreshIntervalSeconds { get; }

        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

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
        /// Whether this deployment needs a MyAdmin account. True when either of the two services that call the
        /// MyAdmin API is enabled. When false, the MyAdmin settings are neither required nor used, and the
        /// application neither validates nor authenticates them.
        /// </summary>
        bool RequiresMyAdmin { get; }

        /// <summary>
        /// Which device control plane this deployment declares it is using. Validation and documentation only -
        /// it never enables or disables a service.
        /// </summary>
        DIGAdapterOperatingMode OperatingMode { get; }

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
