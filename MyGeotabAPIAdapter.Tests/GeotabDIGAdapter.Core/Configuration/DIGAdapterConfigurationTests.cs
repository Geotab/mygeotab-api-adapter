using Microsoft.Extensions.Configuration;
using MyGeotabAPIAdapter.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// Unit tests covering when <see cref="DIGAdapterConfiguration"/> requires MyAdmin settings.
    /// <para>
    /// A deployment driven by device subscriptions rather than by device provisioning has no MyAdmin account.
    /// These settings were previously read as unconditionally required, which made a MyAdmin account mandatory
    /// to start the application at all - regardless of whether any service would ever call MyAdmin.
    /// </para>
    /// </summary>
    public class DIGAdapterConfigurationTests
    {
        const string ArgNameMyAdminAPIEndpoint = "MyAdminSettings:MyAdminAPIEndpoint";
        const string ArgNameMyAdminUser = "MyAdminSettings:MyAdminUser";
        const string ArgNameMyAdminPassword = "MyAdminSettings:MyAdminPassword";
        const string ArgNamePromoCode = "MyAdminSettings:PromoCode";
        const string ArgNameEnableDeviceProvisioningService = "DIGSettings:Services:DeviceProvisioningService:EnableDeviceProvisioningService";
        const string ArgNameEnableDeviceDIGReadinessService = "DIGSettings:Services:DeviceDIGReadinessService:EnableDeviceDIGReadinessService";
        const string ArgNameEnableDeviceSubscriptionService = "DIGSettings:Services:DeviceSubscriptionService:EnableDeviceSubscriptionService";
        const string ArgNameEnableTelemetryDataService = "DIGSettings:Services:TelemetryDataService:EnableTelemetryDataService";
        const string ArgNameOperatingMode = "DIGSettings:OperatingMode";
        const string ArgNameEnableDatabaseMaintenanceService = "DIGSettings:Services:DatabaseMaintenanceService:EnableDatabaseMaintenanceService";
        const string ArgNamePartitionInterval = "DIGSettings:Services:DatabaseMaintenanceService:PartitionInterval";
        const string ArgNameEnableAutomaticRetention = "DIGSettings:Services:DatabaseMaintenanceService:EnableAutomaticRetention";
        const string ArgNameFailTableRetentionDays = "DIGSettings:Services:DatabaseMaintenanceService:FailTableRetentionDays";
        const string ArgNameQueueDepthWarningThreshold = "DIGSettings:Services:DatabaseMaintenanceService:QueueDepthWarningThreshold";
        const string ArgNameEnableLevel1DigMaintenance = "DIGSettings:Services:DatabaseMaintenanceService:EnableLevel1DigMaintenance";
        const string ArgNameLevel1DigMaintenanceIntervalMinutes = "DIGSettings:Services:DatabaseMaintenanceService:Level1DigMaintenanceIntervalMinutes";
        const string ArgNameLevel2DigMaintenanceIntervalMinutes = "DIGSettings:Services:DatabaseMaintenanceService:Level2DigMaintenanceIntervalMinutes";
        const string ArgNameEnableLevel2DigMaintenanceWindow = "DIGSettings:Services:DatabaseMaintenanceService:EnableLevel2DigMaintenanceWindow";
        const string ArgNameLevel2DigMaintenanceWindowStartTimeUtc = "DIGSettings:Services:DatabaseMaintenanceService:Level2DigMaintenanceWindowStartTimeUTC";
        const string ArgNameLevel2DigMaintenanceWindowMaxMinutes = "DIGSettings:Services:DatabaseMaintenanceService:Level2DigMaintenanceWindowMaxMinutes";

        /// <summary>
        /// A complete and internally consistent pure-OEM configuration: every setting read with a
        /// non-default-tolerant accessor is present, and the declared operating mode agrees with the enable
        /// flags. Tests vary one axis from here.
        /// </summary>
        static Dictionary<string, string?> BuildRequiredSettings()
        {
            return new Dictionary<string, string?>
            {
                { "OverrideSettings:DisableMachineNameValidation", "true" },
                // These two keys are DELIBERATE, and deliberately dead. This adapter no longer reads either of
                // them - they were removed from appsettings.json and from the configuration reader, because an
                // operator hunting for the retention off switch would find them first. The CHANGELOG and the
                // upgrade guide both promise that a carried-forward appsettings.json still containing them
                // starts normally, and seeding them in the baseline here is what holds every test in this class
                // to that promise. Do not "tidy" them away; the MyGeotab API Adapter's own copies are live and
                // are a different thing entirely.
                { "DatabaseSettings:EnableLevel1DatabaseMaintenance", "false" },
                { "DatabaseSettings:Level1DatabaseMaintenanceIntervalMinutes", "30" },
                { "DatabaseSettings:DatabaseConnectionString", "Server=test;Database=geotabadapterdb" },
                { "DatabaseSettings:DatabaseProviderType", "PostgreSQL" },
                { "DIGSettings:DIGAPIEndpoint", "https://dig.test.local/" },
                { "DIGSettings:DIGUser", "dig-user" },
                { "DIGSettings:DIGPassword", "dig-password" },
                { "AppSettings:GeneralSettings:TimeoutSecondsForDatabaseTasks", "600" },
                { "AppSettings:GeneralSettings:TimeoutSecondsForDIGTasks", "3600" },
                { ArgNameOperatingMode, "OemSubscription" },
                { ArgNameEnableDeviceProvisioningService, "false" },
                { ArgNameEnableDeviceDIGReadinessService, "false" },
                { ArgNameEnableDeviceSubscriptionService, "true" },
                { "DIGSettings:Services:TelemetryDataService:EnableTelemetryDataService", "true" },
                { "DIGSettings:Services:TelemetryDataService:BatchSizePerType", "1000" },
                { "DIGSettings:Services:TelemetryDataService:IntervalSeconds", "10" },
                { "DIGSettings:Services:TelemetryDataService:ProvisionedDeviceCacheRefreshIntervalSeconds", "300" },
                { "DIGSettings:Services:TelemetryDataService:EnableAccelerationRecords", "true" },
                { "DIGSettings:Services:TelemetryDataService:EnableBinaryRecords", "true" },
                { "DIGSettings:Services:TelemetryDataService:EnableBluetoothRecords", "true" },
                { "DIGSettings:Services:TelemetryDataService:EnableDriverChangeRecords", "true" },
                { "DIGSettings:Services:TelemetryDataService:EnableGenericFaultRecords", "true" },
                { "DIGSettings:Services:TelemetryDataService:EnableGenericStatusRecords", "true" },
                { "DIGSettings:Services:TelemetryDataService:EnableGpsRecords", "true" },
                { "DIGSettings:Services:TelemetryDataService:EnableJ1708FaultRecords", "true" },
                { "DIGSettings:Services:TelemetryDataService:EnableJ1939FaultRecords", "true" },
                { "DIGSettings:Services:TelemetryDataService:EnableObdiiFaultRecords", "true" },
                { "DIGSettings:Services:TelemetryDataService:EnableVinRecords", "true" },
                { "DIGSettings:Services:InvalidRecordRetrievalService:EnableInvalidRecordRetrievalService", "false" },
                { "DIGSettings:Services:InvalidRecordRetrievalService:IntervalSeconds", "3600" },
                { "DIGSettings:Services:InvalidRecordRetrievalService:BatchSize", "1000" }
            };
        }

        static DIGAdapterConfiguration BuildConfiguration(Dictionary<string, string?> settings)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            return new DIGAdapterConfiguration(new ConfigurationHelper(configuration));
        }

        [Fact]
        public void NoMyAdminSettings_WithBothMyAdminDependentServicesDisabled_IsAccepted()
        {
            var settings = BuildRequiredSettings();

            var adapterConfiguration = BuildConfiguration(settings);

            Assert.False(adapterConfiguration.RequiresMyAdmin);
            Assert.Equal(string.Empty, adapterConfiguration.MyAdminUser);
            Assert.Equal(string.Empty, adapterConfiguration.MyAdminPassword);
            Assert.Equal(string.Empty, adapterConfiguration.MyAdminAPIEndpoint);
        }

        [Theory]
        [InlineData(ArgNameEnableDeviceProvisioningService)]
        [InlineData(ArgNameEnableDeviceDIGReadinessService)]
        public void NoMyAdminSettings_WithAMyAdminDependentServiceEnabled_Throws(string enableKey)
        {
            var settings = BuildRequiredSettings();
            settings[ArgNameOperatingMode] = "Both";
            settings[enableKey] = "true";

            var exception = Assert.Throws<Exception>(() => BuildConfiguration(settings));

            Assert.Contains(ArgNameMyAdminAPIEndpoint, exception.Message);
            Assert.Contains(ArgNameMyAdminUser, exception.Message);
            Assert.Contains(ArgNameMyAdminPassword, exception.Message);
        }

        [Fact]
        public void BlankMyAdminSettings_WithAMyAdminDependentServiceEnabled_Throws()
        {
            // A blank value must fail exactly as an absent one does; otherwise "present but empty" would look
            // like a deliberate opt-out and the failure would surface later, as an authentication error.
            var settings = BuildRequiredSettings();
            settings[ArgNameOperatingMode] = "Both";
            settings[ArgNameEnableDeviceProvisioningService] = "true";
            settings[ArgNameMyAdminAPIEndpoint] = "https://myadmin.test.local/";
            settings[ArgNameMyAdminUser] = "   ";
            settings[ArgNameMyAdminPassword] = "";

            var exception = Assert.Throws<Exception>(() => BuildConfiguration(settings));

            Assert.Contains(ArgNameMyAdminUser, exception.Message);
            Assert.Contains(ArgNameMyAdminPassword, exception.Message);
            Assert.DoesNotContain(ArgNameMyAdminAPIEndpoint, exception.Message);
        }

        [Fact]
        public void CompleteMyAdminSettings_WithAMyAdminDependentServiceEnabled_IsAccepted()
        {
            var settings = BuildRequiredSettings();
            settings[ArgNameOperatingMode] = "Both";
            settings[ArgNameEnableDeviceProvisioningService] = "true";
            settings[ArgNameMyAdminAPIEndpoint] = "https://myadmin.test.local/";
            settings[ArgNameMyAdminUser] = "myadmin-user";
            settings[ArgNameMyAdminPassword] = "myadmin-password";

            var adapterConfiguration = BuildConfiguration(settings);

            Assert.True(adapterConfiguration.RequiresMyAdmin);
            Assert.Equal("myadmin-user", adapterConfiguration.MyAdminUser);
        }

        [Fact]
        public void MissingOperatingMode_DefaultsToOemSubscription()
        {
            // Most deployments are expected to be OEMs, so the subscription control plane is the default.
            var settings = BuildRequiredSettings();
            settings.Remove(ArgNameOperatingMode);

            var adapterConfiguration = BuildConfiguration(settings);

            Assert.Equal(DIGAdapterOperatingMode.OemSubscription, adapterConfiguration.OperatingMode);
        }

        [Fact]
        public void MissingOperatingMode_OnAProvisioningConfiguration_ThrowsNamingTheKey()
        {
            // The cost of defaulting to OemSubscription: a configuration file that predates the setting and
            // still runs the provisioning workflow must declare that. It must fail loudly and name the key,
            // never silently switch control planes.
            var settings = BuildRequiredSettings();
            settings.Remove(ArgNameOperatingMode);
            settings[ArgNameEnableDeviceSubscriptionService] = "false";
            settings[ArgNameEnableDeviceProvisioningService] = "true";
            settings[ArgNameMyAdminAPIEndpoint] = "https://myadmin.test.local/";
            settings[ArgNameMyAdminUser] = "myadmin-user";
            settings[ArgNameMyAdminPassword] = "myadmin-password";

            var exception = Assert.Throws<Exception>(() => BuildConfiguration(settings));

            Assert.Contains(ArgNameOperatingMode, exception.Message);
            Assert.Contains(ArgNameEnableDeviceProvisioningService, exception.Message);
        }

        [Fact]
        public void UnrecognizedOperatingMode_Throws()
        {
            var settings = BuildRequiredSettings();
            settings[ArgNameOperatingMode] = "Whatever";

            var exception = Assert.Throws<Exception>(() => BuildConfiguration(settings));

            Assert.Contains(ArgNameOperatingMode, exception.Message);
            Assert.Contains(nameof(DIGAdapterOperatingMode.OemSubscription), exception.Message);
        }

        [Fact]
        public void OemSubscriptionMode_WithProvisioningEnabled_Throws()
        {
            // Would silently keep a MyAdmin account mandatory, defeating the point of declaring the mode.
            var settings = BuildRequiredSettings();
            settings[ArgNameEnableDeviceProvisioningService] = "true";
            settings[ArgNameMyAdminAPIEndpoint] = "https://myadmin.test.local/";
            settings[ArgNameMyAdminUser] = "myadmin-user";
            settings[ArgNameMyAdminPassword] = "myadmin-password";

            var exception = Assert.Throws<Exception>(() => BuildConfiguration(settings));

            Assert.Contains(ArgNameEnableDeviceProvisioningService, exception.Message);
        }

        [Fact]
        public void OemSubscriptionMode_WithNoSubscriptionSource_Throws()
        {
            var settings = BuildRequiredSettings();
            settings[ArgNameEnableDeviceSubscriptionService] = "false";

            var exception = Assert.Throws<Exception>(() => BuildConfiguration(settings));

            Assert.Contains(ArgNameEnableDeviceSubscriptionService, exception.Message);
        }

        [Fact]
        public void DeviceProvisioningMode_WithSubscriptionsEnabled_Throws()
        {
            var settings = BuildRequiredSettings();
            settings[ArgNameOperatingMode] = "DeviceProvisioning";
            settings[ArgNameEnableDeviceProvisioningService] = "true";
            settings[ArgNameMyAdminAPIEndpoint] = "https://myadmin.test.local/";
            settings[ArgNameMyAdminUser] = "myadmin-user";
            settings[ArgNameMyAdminPassword] = "myadmin-password";

            var exception = Assert.Throws<Exception>(() => BuildConfiguration(settings));

            Assert.Contains(ArgNameEnableDeviceSubscriptionService, exception.Message);
            Assert.Contains(nameof(DIGAdapterOperatingMode.Both), exception.Message);
        }

        [Fact]
        public void TelemetryEnabled_WithNoDeviceSource_Throws()
        {
            // The device lookup would be empty, so nothing could ever be transmitted.
            var settings = BuildRequiredSettings();
            settings[ArgNameOperatingMode] = "Both";
            settings[ArgNameEnableDeviceSubscriptionService] = "false";
            settings[ArgNameEnableTelemetryDataService] = "true";

            var exception = Assert.Throws<Exception>(() => BuildConfiguration(settings));

            Assert.Contains(ArgNameEnableTelemetryDataService, exception.Message);
        }

        [Fact]
        public void MissingPromoCode_WithAMyAdminDependentServiceEnabled_IsAccepted()
        {
            // PromoCode has no consumer anywhere in the solution - the value MyAdmin receives is the per-row one
            // carried on the Q_ProvisionDevices record - so it must never be able to stop the application.
            var settings = BuildRequiredSettings();
            settings[ArgNameOperatingMode] = "Both";
            settings[ArgNameEnableDeviceProvisioningService] = "true";
            settings[ArgNameMyAdminAPIEndpoint] = "https://myadmin.test.local/";
            settings[ArgNameMyAdminUser] = "myadmin-user";
            settings[ArgNameMyAdminPassword] = "myadmin-password";
            Assert.False(settings.ContainsKey(ArgNamePromoCode));

            var adapterConfiguration = BuildConfiguration(settings);

            Assert.Equal(string.Empty, adapterConfiguration.PromoCode);
        }

        [Fact]
        public void MissingDatabaseMaintenanceSettings_MaintainPartitionsAndReclaimAfterNinetyDays()
        {
            // A configuration file written for an earlier version has no DatabaseMaintenanceService section at
            // all. Omitting it must produce the intended behaviour rather than stop the application, and the
            // intended behaviour is that partitions are maintained and old failure records are reclaimed.
            var settings = BuildRequiredSettings();
            Assert.False(settings.ContainsKey(ArgNameEnableDatabaseMaintenanceService));

            var adapterConfiguration = BuildConfiguration(settings);

            Assert.True(adapterConfiguration.EnableDatabaseMaintenanceService);
            Assert.Equal(1440, adapterConfiguration.DatabaseMaintenanceServiceIntervalMinutes);
            Assert.Equal("monthly", adapterConfiguration.PartitionInterval);
            Assert.True(adapterConfiguration.EnableAutomaticRetention);
            Assert.Equal(90, adapterConfiguration.FailTableRetentionDays);
            Assert.Equal(100000, adapterConfiguration.QueueDepthWarningThreshold);
        }

        [Fact]
        public void MissingLevelMaintenanceSettings_RunLevel1HalfHourlyAndLevel2DailyWithNoWindow()
        {
            // A configuration file written for an earlier version has none of these keys, and must start with the
            // intended behaviour. Level 2 is daily rather than the MyGeotab API Adapter's hourly, because it pauses
            // telemetry while it runs.
            var settings = BuildRequiredSettings();
            Assert.False(settings.ContainsKey(ArgNameEnableLevel1DigMaintenance));

            var adapterConfiguration = BuildConfiguration(settings);

            Assert.True(adapterConfiguration.EnableLevel1DigMaintenance);
            Assert.Equal(30, adapterConfiguration.Level1DigMaintenanceIntervalMinutes);
            Assert.True(adapterConfiguration.EnableLevel2DigMaintenance);
            Assert.Equal(1440, adapterConfiguration.Level2DigMaintenanceIntervalMinutes);
            Assert.False(adapterConfiguration.EnableLevel2DigMaintenanceWindow);
            Assert.Equal(new TimeSpan(6, 0, 0), adapterConfiguration.Level2DigMaintenanceWindowStartTimeUtc.TimeOfDay);
            Assert.Equal(60, adapterConfiguration.Level2DigMaintenanceWindowMaxMinutes);
        }

        [Theory]
        [InlineData(ArgNameLevel1DigMaintenanceIntervalMinutes, "5", 30)]
        [InlineData(ArgNameLevel2DigMaintenanceIntervalMinutes, "50000", 1440)]
        [InlineData(ArgNameLevel2DigMaintenanceWindowMaxMinutes, "721", 60)]
        public void LevelMaintenanceSettingOutsideTheAllowedRange_FallsBackToTheDefault(string key, string configuredValue, int expectedValue)
        {
            var settings = BuildRequiredSettings();
            settings[key] = configuredValue;

            var adapterConfiguration = BuildConfiguration(settings);

            var actualValue = key switch
            {
                ArgNameLevel1DigMaintenanceIntervalMinutes => adapterConfiguration.Level1DigMaintenanceIntervalMinutes,
                ArgNameLevel2DigMaintenanceIntervalMinutes => adapterConfiguration.Level2DigMaintenanceIntervalMinutes,
                _ => adapterConfiguration.Level2DigMaintenanceWindowMaxMinutes
            };
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Level2WindowStartTime_IsReadForItsTimeOfDayInUtc()
        {
            var settings = BuildRequiredSettings();
            settings[ArgNameEnableLevel2DigMaintenanceWindow] = "true";
            settings[ArgNameLevel2DigMaintenanceWindowStartTimeUtc] = "2024-01-01T03:30:00Z";

            var adapterConfiguration = BuildConfiguration(settings);

            Assert.True(adapterConfiguration.EnableLevel2DigMaintenanceWindow);
            Assert.Equal(new TimeSpan(3, 30, 0), adapterConfiguration.Level2DigMaintenanceWindowStartTimeUtc.TimeOfDay);
        }

        [Theory]
        [InlineData("2024-01-01T06:00:00+03:00", 3, 0)]
        [InlineData("2024-01-01T04:15:00", 4, 15)]
        public void Level2WindowStartTime_IsAlwaysUtc_WhateverTheHostTimeZone(string configured, int hour, int minute)
        {
            // 06:00 at UTC+03:00 is 03:00 UTC, and a value with no offset is taken to be UTC. Parsed as a round trip, the first
            // became the HOST's local time (Kind Local) - so on a host not running UTC the window moved by the host's offset.
            var settings = BuildRequiredSettings();
            settings[ArgNameEnableLevel2DigMaintenanceWindow] = "true";
            settings[ArgNameLevel2DigMaintenanceWindowStartTimeUtc] = configured;

            var adapterConfiguration = BuildConfiguration(settings);

            Assert.Equal(DateTimeKind.Utc, adapterConfiguration.Level2DigMaintenanceWindowStartTimeUtc.Kind);
            Assert.Equal(new TimeSpan(hour, minute, 0), adapterConfiguration.Level2DigMaintenanceWindowStartTimeUtc.TimeOfDay);
        }

        [Fact]
        public void UnreadableLevel2WindowStartTime_ThrowsNamingTheKey()
        {
            var settings = BuildRequiredSettings();
            settings[ArgNameLevel2DigMaintenanceWindowStartTimeUtc] = "six in the morning";

            var exception = Assert.Throws<Exception>(() => BuildConfiguration(settings));

            Assert.Contains(ArgNameLevel2DigMaintenanceWindowStartTimeUtc, exception.Message);
        }

        [Fact]
        public void RetiredLevel1Keys_DoNotSwitchLevel1MaintenanceOff()
        {
            // The baseline carries DatabaseSettings:EnableLevel1DatabaseMaintenance = false - a key this adapter copied from
            // the MyGeotab API Adapter and never read. Now that this adapter HAS Level 1 maintenance, that key must still
            // do nothing: the adapter warns that it is not read, and Level 1 follows its own setting.
            var settings = BuildRequiredSettings();
            Assert.Equal("false", settings["DatabaseSettings:EnableLevel1DatabaseMaintenance"]);

            var adapterConfiguration = BuildConfiguration(settings);

            Assert.True(adapterConfiguration.EnableLevel1DigMaintenance);
        }

        [Fact]
        public void QueueDepthWarningThresholdOfZero_IsAcceptedAsTheWayToSilenceTheWarnings()
        {
            // Zero is the documented opt-out. Treating it as out of range - as every other numeric setting here
            // does with zero - would silently restore the warnings a deployment had deliberately turned off.
            var settings = BuildRequiredSettings();
            settings[ArgNameQueueDepthWarningThreshold] = "0";

            var adapterConfiguration = BuildConfiguration(settings);

            Assert.Equal(0, adapterConfiguration.QueueDepthWarningThreshold);
        }

        [Fact]
        public void QueueDepthWarningThresholdOutsideTheAllowedRange_FallsBackToTheDefault()
        {
            var settings = BuildRequiredSettings();
            settings[ArgNameQueueDepthWarningThreshold] = "-1";

            var adapterConfiguration = BuildConfiguration(settings);

            Assert.Equal(100000, adapterConfiguration.QueueDepthWarningThreshold);
        }

        [Fact]
        public void UnrecognizedPartitionInterval_ThrowsNamingTheKey()
        {
            var settings = BuildRequiredSettings();
            settings[ArgNamePartitionInterval] = "quarterly";

            var exception = Assert.Throws<Exception>(() => BuildConfiguration(settings));

            Assert.Contains(ArgNamePartitionInterval, exception.Message);
            Assert.Contains("monthly", exception.Message);
        }

        [Theory]
        [InlineData("Monthly", "monthly")]
        [InlineData(" WEEKLY ", "weekly")]
        [InlineData("Daily", "daily")]
        public void PartitionInterval_IsNormalizedForTheProcedures(string configuredValue, string expectedValue)
        {
            // The partitioning procedures compare the value literally, so 'Monthly' would reach the database and
            // be rejected there - turning a harmless typo into a maintenance service that fails every iteration
            // instead of a configuration that is refused at startup.
            var settings = BuildRequiredSettings();
            settings[ArgNamePartitionInterval] = configuredValue;

            var adapterConfiguration = BuildConfiguration(settings);

            Assert.Equal(expectedValue, adapterConfiguration.PartitionInterval);
        }

        [Fact]
        public void FailTableRetentionDaysOutsideTheAllowedRange_FallsBackToTheDefault()
        {
            // Zero would ask for everything before today to be reclaimed. Falling back to the default is what
            // every other out-of-range numeric setting does, and it keeps a typo from being destructive.
            var settings = BuildRequiredSettings();
            settings[ArgNameFailTableRetentionDays] = "0";

            var adapterConfiguration = BuildConfiguration(settings);

            Assert.Equal(90, adapterConfiguration.FailTableRetentionDays);
        }

        [Fact]
        public void RetentionSettings_DoNotParticipateInOperatingModeValidation()
        {
            // OperatingMode's contract is which device control plane a deployment runs. Retention is orthogonal
            // to both of them, so no combination of these settings may make a valid mode invalid.
            var settings = BuildRequiredSettings();
            settings[ArgNameEnableDatabaseMaintenanceService] = "false";
            settings[ArgNameEnableAutomaticRetention] = "false";

            var adapterConfiguration = BuildConfiguration(settings);

            Assert.Equal(DIGAdapterOperatingMode.OemSubscription, adapterConfiguration.OperatingMode);
            Assert.False(adapterConfiguration.EnableDatabaseMaintenanceService);
            Assert.False(adapterConfiguration.EnableAutomaticRetention);
        }

        [Theory]
        [InlineData("oemsubscription", DIGAdapterOperatingMode.OemSubscription)]
        [InlineData("OEMSUBSCRIPTION", DIGAdapterOperatingMode.OemSubscription)]
        [InlineData("  OemSubscription  ", DIGAdapterOperatingMode.OemSubscription)]
        public void OperatingMode_DifferingOnlyInCaseOrSpacing_IsAccepted(string configuredValue, DIGAdapterOperatingMode expectedValue)
        {
            var settings = BuildRequiredSettings();
            settings[ArgNameOperatingMode] = configuredValue;

            var adapterConfiguration = BuildConfiguration(settings);

            Assert.Equal(expectedValue, adapterConfiguration.OperatingMode);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("99")]
        [InlineData("OemSubscription, Both")]
        public void OperatingModeGivenANumberOrAList_IsRefused(string configuredValue)
        {
            // Enum.TryParse accepts all of these: a number is cast straight to the enum type whether or not it
            // names a member, and a comma-separated list is OR-ed together even though this enum carries no
            // FlagsAttribute. A value that names no mode reached ValidateOperatingMode, matched none of its
            // mode-specific blocks, and so passed every contradiction check the setting exists to perform - the
            // one failure mode this setting was added to prevent, re-entered through its own parser.
            var settings = BuildRequiredSettings();
            settings[ArgNameOperatingMode] = configuredValue;

            var exception = Assert.Throws<Exception>(() => BuildConfiguration(settings));

            Assert.Contains(ArgNameOperatingMode, exception.Message);
            Assert.Contains(configuredValue, exception.Message);
        }

        [Fact]
        public void OperatingModeNamingNoMode_DoesNotSilentlySkipTheContradictionChecks()
        {
            // The same defect stated as its consequence, on a configuration that nothing but the mode check
            // refuses: no control plane is running, so under OemSubscription the mode check refuses it (asserted
            // immediately below as the control), while every other check - MyAdmin, and the mode-independent
            // telemetry check - is satisfied. Before the mode was parsed strictly, '99' matched no mode-specific
            // block and this configuration started.
            var settings = BuildRequiredSettings();
            settings[ArgNameEnableDeviceSubscriptionService] = "false";
            settings[ArgNameEnableTelemetryDataService] = "false";

            settings[ArgNameOperatingMode] = nameof(DIGAdapterOperatingMode.OemSubscription);
            var controlException = Assert.Throws<Exception>(() => BuildConfiguration(settings));
            Assert.Contains(ArgNameEnableDeviceSubscriptionService, controlException.Message);

            settings[ArgNameOperatingMode] = "99";
            Assert.Throws<Exception>(() => BuildConfiguration(settings));
        }

        /// <summary>
        /// The telemetry claim ceiling must stay out of the device-lookup hold's way.
        /// </summary>
        [Fact]
        public void TelemetryMaxClaimAttemptsFloor_StaysAboveTheDeviceLookupHoldLimit()
        {
            // These two limits count the SAME RetryCount column at two cadences. The claim procedures
            // increment it on a stale reclaim, at a fixed 30 minutes; the device-lookup hold increments it
            // once per service iteration, 10 seconds by default. The claim procedures' exhausted-row DELETE
            // runs BEFORE the claim, so a ceiling at or below the hold limit removes a held record on the
            // very iteration the hold logic would have classified it: the operator is then told
            // "Exceeded N claim attempts." instead of "No device found for ThirdPartyId: ...", and only the
            // second names the real cause and carries the remedy. Lowering this floor back to 1 like its two
            // siblings, or raising the hold limit to meet it, silently reinstates that - which is how it
            // shipped, and why this assertion exists rather than a comment.
            Assert.True(
                DIGAdapterConfiguration.MinTelemetryDataServiceMaxClaimAttempts > DIGAdapterConfiguration.MaxDeviceLookupHoldAttempts,
                $"MinTelemetryDataServiceMaxClaimAttempts ({DIGAdapterConfiguration.MinTelemetryDataServiceMaxClaimAttempts}) must be strictly greater than MaxDeviceLookupHoldAttempts ({DIGAdapterConfiguration.MaxDeviceLookupHoldAttempts}).");
        }

        /// <summary>
        /// No claim ceiling that configuration can produce may exceed what the <c>RetryCount</c> column can hold.
        /// </summary>
        [Fact]
        public void EveryMaxClaimAttemptsBoundAndDefault_FitsInRetryCount()
        {
            // RetryCount is tinyint on SQL Server and byte in the C# models (smallint on PostgreSQL), so it holds
            // 0 to 255. Only the ceiling stops it growing: the claim procedures' exhausted-row DELETE removes a
            // row once RetryCount reaches the ceiling, so the ceiling is the highest value the column ever takes.
            // A ceiling above 255 would let a permanently failing row overflow the column - on SQL Server inside
            // the claim procedure, on PostgreSQL when the claimed row is mapped to byte - and either one ends the
            // process. The column type was left as it is on the strength of this bound, so the bound is asserted
            // here rather than remembered. The defaults are included because a key that is absent takes its
            // default without the range check. The constants are private, so they are read by name; a rename
            // fails the first loop instead of silently shrinking what the second one checks.
            var ceilings = typeof(DIGAdapterConfiguration)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && Regex.IsMatch(field.Name, "^(Max|Default)[A-Za-z]+ServiceMaxClaimAttempts$"))
                .ToDictionary(field => field.Name, field => (int)field.GetRawConstantValue());

            foreach (var expected in new[]
            {
                "MaxTelemetryDataServiceMaxClaimAttempts", "DefaultTelemetryDataServiceMaxClaimAttempts",
                "MaxDeviceProvisioningServiceMaxClaimAttempts", "DefaultDeviceProvisioningServiceMaxClaimAttempts",
                "MaxDeviceSubscriptionServiceMaxClaimAttempts", "DefaultDeviceSubscriptionServiceMaxClaimAttempts",
            })
            {
                Assert.True(ceilings.ContainsKey(expected), $"{expected} was not found on DIGAdapterConfiguration. If it was renamed, this test must still see every claim ceiling.");
            }

            foreach (var ceiling in ceilings)
            {
                Assert.True(ceiling.Value <= byte.MaxValue, $"{ceiling.Key} ({ceiling.Value}) exceeds {byte.MaxValue}, the largest value the RetryCount column can hold. Widen RetryCount on every queue table and model first.");
            }
        }
    }
}
