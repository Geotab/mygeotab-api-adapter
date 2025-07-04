using NLog;
using System;
using System.Reflection;

namespace MyGeotabAPIAdapter.Configuration
{
    /// <summary>
    /// A class that reads (from appsettings.json) and stores application configuration settings. Intended for association with the MyGeotabAPIAdapter.DataOptimizer project.
    /// </summary>
    public class DataOptimizerConfiguration : IDataOptimizerConfiguration
    {
        // Argument Names for appsettings:
        // > OverrideSettings
        const string ArgNameDisableMachineNameValidation = "OverrideSettings:DisableMachineNameValidation";
        // > DatabaseSettings:AdapterDatabase
        const string ArgNameAdapterDatabaseProviderType = "DatabaseSettings:AdapterDatabase:AdapterDatabaseProviderType";
        const string ArgNameAdapterDatabaseConnectionString = "DatabaseSettings:AdapterDatabase:AdapterDatabaseConnectionString";
        // > DatabaseSettings:OptimizerDatabase
        const string ArgNameOptimizerDatabaseProviderType = "DatabaseSettings:OptimizerDatabase:OptimizerDatabaseProviderType";
        const string ArgNameOptimizerDatabaseConnectionString = "DatabaseSettings:OptimizerDatabase:OptimizerDatabaseConnectionString";
        // > AppSettings:GeneralSettings
        const string ArgNameTimeoutSecondsForDatabaseTasks = "AppSettings:GeneralSettings:TimeoutSecondsForDatabaseTasks";
        // > AppSettings:Processors:BinaryData
        const string ArgNameEnableBinaryDataProcessor = "AppSettings:Processors:BinaryData:EnableBinaryDataProcessor";
        const string ArgNameBinaryDataProcessorOperationMode = "AppSettings:Processors:BinaryData:BinaryDataProcessorOperationMode";
        const string ArgNameBinaryDataProcessorDailyStartTimeUTC = "AppSettings:Processors:BinaryData:BinaryDataProcessorDailyStartTimeUTC";
        const string ArgNameBinaryDataProcessorDailyRunTimeSeconds = "AppSettings:Processors:BinaryData:BinaryDataProcessorDailyRunTimeSeconds";
        const string ArgNameBinaryDataProcessorBatchSize = "AppSettings:Processors:BinaryData:BinaryDataProcessorBatchSize";
        const string ArgNameBinaryDataProcessorExecutionIntervalSeconds = "AppSettings:Processors:BinaryData:BinaryDataProcessorExecutionIntervalSeconds";
        // > AppSettings:Processors:Device
        const string ArgNameEnableDeviceProcessor = "AppSettings:Processors:Device:EnableDeviceProcessor";
        const string ArgNameDeviceProcessorOperationMode = "AppSettings:Processors:Device:DeviceProcessorOperationMode";
        const string ArgNameDeviceProcessorDailyStartTimeUTC = "AppSettings:Processors:Device:DeviceProcessorDailyStartTimeUTC";
        const string ArgNameDeviceProcessorDailyRunTimeSeconds = "AppSettings:Processors:Device:DeviceProcessorDailyRunTimeSeconds";
        const string ArgNameDeviceProcessorExecutionIntervalSeconds = "AppSettings:Processors:Device:DeviceProcessorExecutionIntervalSeconds";
        // > AppSettings:Processors:Diagnostic
        const string ArgNameEnableDiagnosticProcessor = "AppSettings:Processors:Diagnostic:EnableDiagnosticProcessor";
        const string ArgNameDiagnosticProcessorOperationMode = "AppSettings:Processors:Diagnostic:DiagnosticProcessorOperationMode";
        const string ArgNameDiagnosticProcessorDailyStartTimeUTC = "AppSettings:Processors:Diagnostic:DiagnosticProcessorDailyStartTimeUTC";
        const string ArgNameDiagnosticProcessorDailyRunTimeSeconds = "AppSettings:Processors:Diagnostic:DiagnosticProcessorDailyRunTimeSeconds";
        const string ArgNameDiagnosticProcessorExecutionIntervalSeconds = "AppSettings:Processors:Diagnostic:DiagnosticProcessorExecutionIntervalSeconds";
        // > AppSettings:Processors:DriverChange
        const string ArgNameEnableDriverChangeProcessor = "AppSettings:Processors:DriverChange:EnableDriverChangeProcessor";
        const string ArgNameDriverChangeProcessorOperationMode = "AppSettings:Processors:DriverChange:DriverChangeProcessorOperationMode";
        const string ArgNameDriverChangeProcessorDailyStartTimeUTC = "AppSettings:Processors:DriverChange:DriverChangeProcessorDailyStartTimeUTC";
        const string ArgNameDriverChangeProcessorDailyRunTimeSeconds = "AppSettings:Processors:DriverChange:DriverChangeProcessorDailyRunTimeSeconds";
        const string ArgNameDriverChangeProcessorBatchSize = "AppSettings:Processors:DriverChange:DriverChangeProcessorBatchSize";
        const string ArgNameDriverChangeProcessorExecutionIntervalSeconds = "AppSettings:Processors:DriverChange:DriverChangeProcessorExecutionIntervalSeconds";
        // > AppSettings:Processors:FaultData
        const string ArgNameEnableFaultDataProcessor = "AppSettings:Processors:FaultData:EnableFaultDataProcessor";
        const string ArgNameFaultDataProcessorOperationMode = "AppSettings:Processors:FaultData:FaultDataProcessorOperationMode";
        const string ArgNameFaultDataProcessorDailyStartTimeUTC = "AppSettings:Processors:FaultData:FaultDataProcessorDailyStartTimeUTC";
        const string ArgNameFaultDataProcessorDailyRunTimeSeconds = "AppSettings:Processors:FaultData:FaultDataProcessorDailyRunTimeSeconds";
        const string ArgNameFaultDataProcessorBatchSize = "AppSettings:Processors:FaultData:FaultDataProcessorBatchSize";
        const string ArgNameFaultDataProcessorExecutionIntervalSeconds = "AppSettings:Processors:FaultData:FaultDataProcessorExecutionIntervalSeconds";
        // > AppSettings:Processors:LogRecord
        const string ArgNameEnableLogRecordProcessor = "AppSettings:Processors:LogRecord:EnableLogRecordProcessor";
        const string ArgNameLogRecordProcessorOperationMode = "AppSettings:Processors:LogRecord:LogRecordProcessorOperationMode";
        const string ArgNameLogRecordProcessorDailyStartTimeUTC = "AppSettings:Processors:LogRecord:LogRecordProcessorDailyStartTimeUTC";
        const string ArgNameLogRecordProcessorDailyRunTimeSeconds = "AppSettings:Processors:LogRecord:LogRecordProcessorDailyRunTimeSeconds";
        const string ArgNameLogRecordProcessorBatchSize = "AppSettings:Processors:LogRecord:LogRecordProcessorBatchSize";
        const string ArgNameLogRecordProcessorExecutionIntervalSeconds = "AppSettings:Processors:LogRecord:LogRecordProcessorExecutionIntervalSeconds";
        // > AppSettings:Processors:StatusData
        const string ArgNameEnableStatusDataProcessor = "AppSettings:Processors:StatusData:EnableStatusDataProcessor";
        const string ArgNameStatusDataProcessorOperationMode = "AppSettings:Processors:StatusData:StatusDataProcessorOperationMode";
        const string ArgNameStatusDataProcessorDailyStartTimeUTC = "AppSettings:Processors:StatusData:StatusDataProcessorDailyStartTimeUTC";
        const string ArgNameStatusDataProcessorDailyRunTimeSeconds = "AppSettings:Processors:StatusData:StatusDataProcessorDailyRunTimeSeconds";
        const string ArgNameStatusDataProcessorBatchSize = "AppSettings:Processors:StatusData:StatusDataProcessorBatchSize";
        const string ArgNameStatusDataProcessorExecutionIntervalSeconds = "AppSettings:Processors:StatusData:StatusDataProcessorExecutionIntervalSeconds";
        // > AppSettings:Processors:User
        const string ArgNameEnableUserProcessor = "AppSettings:Processors:User:EnableUserProcessor";
        const string ArgNameUserProcessorOperationMode = "AppSettings:Processors:User:UserProcessorOperationMode";
        const string ArgNameUserProcessorDailyStartTimeUTC = "AppSettings:Processors:User:UserProcessorDailyStartTimeUTC";
        const string ArgNameUserProcessorDailyRunTimeSeconds = "AppSettings:Processors:User:UserProcessorDailyRunTimeSeconds";
        const string ArgNameUserProcessorExecutionIntervalSeconds = "AppSettings:Processors:User:UserProcessorExecutionIntervalSeconds";
        // > AppSettings:Optimizers:FaultData
        const string ArgNameEnableFaultDataOptimizer = "AppSettings:Optimizers:FaultData:EnableFaultDataOptimizer";
        const string ArgNameFaultDataOptimizerOperationMode = "AppSettings:Optimizers:FaultData:FaultDataOptimizerOperationMode";
        const string ArgNameFaultDataOptimizerDailyStartTimeUTC = "AppSettings:Optimizers:FaultData:FaultDataOptimizerDailyStartTimeUTC";
        const string ArgNameFaultDataOptimizerDailyRunTimeSeconds = "AppSettings:Optimizers:FaultData:FaultDataOptimizerDailyRunTimeSeconds";
        const string ArgNameFaultDataOptimizerExecutionIntervalSeconds = "AppSettings:Optimizers:FaultData:FaultDataOptimizerExecutionIntervalSeconds";
        const string ArgNameFaultDataOptimizerPopulateLongitudeLatitude = "AppSettings:Optimizers:FaultData:FaultDataOptimizerPopulateLongitudeLatitude";
        const string ArgNameFaultDataOptimizerPopulateSpeed = "AppSettings:Optimizers:FaultData:FaultDataOptimizerPopulateSpeed";
        const string ArgNameFaultDataOptimizerPopulateBearing = "AppSettings:Optimizers:FaultData:FaultDataOptimizerPopulateBearing";
        const string ArgNameFaultDataOptimizerPopulateDirection = "AppSettings:Optimizers:FaultData:FaultDataOptimizerPopulateDirection";
        const string ArgNameFaultDataOptimizerNumberOfCompassDirections = "AppSettings:Optimizers:FaultData:FaultDataOptimizerNumberOfCompassDirections";
        const string ArgNameFaultDataOptimizerPopulateDriverId = "AppSettings:Optimizers:FaultData:FaultDataOptimizerPopulateDriverId";
        // > AppSettings:Optimizers:StatusData
        const string ArgNameEnableStatusDataOptimizer = "AppSettings:Optimizers:StatusData:EnableStatusDataOptimizer";
        const string ArgNameStatusDataOptimizerOperationMode = "AppSettings:Optimizers:StatusData:StatusDataOptimizerOperationMode";
        const string ArgNameStatusDataOptimizerDailyStartTimeUTC = "AppSettings:Optimizers:StatusData:StatusDataOptimizerDailyStartTimeUTC";
        const string ArgNameStatusDataOptimizerDailyRunTimeSeconds = "AppSettings:Optimizers:StatusData:StatusDataOptimizerDailyRunTimeSeconds";
        const string ArgNameStatusDataOptimizerExecutionIntervalSeconds = "AppSettings:Optimizers:StatusData:StatusDataOptimizerExecutionIntervalSeconds";
        const string ArgNameStatusDataOptimizerPopulateLongitudeLatitude = "AppSettings:Optimizers:StatusData:StatusDataOptimizerPopulateLongitudeLatitude";
        const string ArgNameStatusDataOptimizerPopulateSpeed = "AppSettings:Optimizers:StatusData:StatusDataOptimizerPopulateSpeed";
        const string ArgNameStatusDataOptimizerPopulateBearing = "AppSettings:Optimizers:StatusData:StatusDataOptimizerPopulateBearing";
        const string ArgNameStatusDataOptimizerPopulateDirection = "AppSettings:Optimizers:StatusData:StatusDataOptimizerPopulateDirection";
        const string ArgNameStatusDataOptimizerNumberOfCompassDirections = "AppSettings:Optimizers:StatusData:StatusDataOptimizerNumberOfCompassDirections";
        const string ArgNameStatusDataOptimizerPopulateDriverId = "AppSettings:Optimizers:StatusData:StatusDataOptimizerPopulateDriverId";

        // Daily run time seconds limits:
        const int MinDailyRunTimeSeconds = 300; // 300 sec = 5 mins
        const int MaxDailyRunTimeSeconds = 82800; // 82800 sec = 23 hrs

        // Arbitrary batch size limits:
        const int DefaultBatchSize = 10000;
        const int MinBatchSize = 100;
        const int MaxBatchSize = 100000;

        // Arbitrary throttling limits:
        const int DefaultExecutionIntervalSeconds = 60;
        const int MinExecutionIntervalSeconds = 10;
        const int MaxExecutionIntervalSeconds = 86400; // 86400 sec = 1 day

        // Arbitrary timeout limits:
        const int DefaultTimeoutSeconds = 30;
        const int MinTimeoutSeconds = 10;
        const int MaxTimeoutSeconds = 3600;

        /// <inheritdoc/>
        public string AdapterDatabaseProviderType { get; private set; }

        /// <inheritdoc/>
        public string AdapterDatabaseConnectionString { get; private set; }

        /// <inheritdoc/>
        public int BinaryDataProcessorBatchSize { get; private set; }

        /// <inheritdoc/>
        public DateTime BinaryDataProcessorDailyStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int BinaryDataProcessorDailyRunTimeSeconds { get; private set; }

        /// <inheritdoc/>
        public int BinaryDataProcessorExecutionIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public OperationMode BinaryDataProcessorOperationMode { get; private set; }

        /// <inheritdoc/>
        public DateTime DeviceProcessorDailyStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int DeviceProcessorDailyRunTimeSeconds { get; private set; }

        /// <inheritdoc/>
        public int DeviceProcessorExecutionIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public OperationMode DeviceProcessorOperationMode { get; private set; }

        /// <inheritdoc/>
        public DateTime DiagnosticProcessorDailyStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int DiagnosticProcessorDailyRunTimeSeconds { get; private set; }

        /// <inheritdoc/>
        public int DiagnosticProcessorExecutionIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public OperationMode DiagnosticProcessorOperationMode { get; private set; }

        /// <inheritdoc/>
        public bool DisableMachineNameValidation { get; private set; }

        /// <inheritdoc/>
        public int DriverChangeProcessorBatchSize { get; private set; }

        /// <inheritdoc/>
        public DateTime DriverChangeProcessorDailyStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int DriverChangeProcessorDailyRunTimeSeconds { get; private set; }

        /// <inheritdoc/>
        public int DriverChangeProcessorExecutionIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public OperationMode DriverChangeProcessorOperationMode { get; private set; }

        /// <inheritdoc/>
        public bool EnableBinaryDataProcessor { get; private set; }

        /// <inheritdoc/>
        public bool EnableDeviceProcessor { get; private set; }

        /// <inheritdoc/>
        public bool EnableDiagnosticProcessor { get; private set; }

        /// <inheritdoc/>
        public bool EnableDriverChangeProcessor { get; private set; }

        /// <inheritdoc/>
        public bool EnableFaultDataOptimizer { get; private set; }

        /// <inheritdoc/>
        public bool EnableFaultDataProcessor { get; private set; }

        /// <inheritdoc/>
        public bool EnableLogRecordProcessor { get; private set; }

        /// <inheritdoc/>
        public bool EnableStatusDataOptimizer { get; private set; }

        /// <inheritdoc/>
        public bool EnableStatusDataProcessor { get; private set; }

        /// <inheritdoc/>
        public bool EnableUserProcessor { get; private set; }

        /// <inheritdoc/>
        public DateTime FaultDataOptimizerDailyStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int FaultDataOptimizerDailyRunTimeSeconds { get; private set; }

        /// <inheritdoc/>
        public int FaultDataOptimizerExecutionIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public int FaultDataOptimizerNumberOfCompassDirections { get; private set; }

        /// <inheritdoc/>
        public OperationMode FaultDataOptimizerOperationMode { get; private set; }

        /// <inheritdoc/>
        public bool FaultDataOptimizerPopulateBearing { get; private set; }

        /// <inheritdoc/>
        public bool FaultDataOptimizerPopulateDirection { get; private set; }

        /// <inheritdoc/>
        public bool FaultDataOptimizerPopulateDriverId { get; private set; }

        /// <inheritdoc/>
        public bool FaultDataOptimizerPopulateLongitudeLatitude { get; private set; }

        /// <inheritdoc/>
        public bool FaultDataOptimizerPopulateSpeed { get; private set; }

        /// <inheritdoc/>
        public int FaultDataProcessorBatchSize { get; private set; }

        /// <inheritdoc/>
        public DateTime FaultDataProcessorDailyStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int FaultDataProcessorDailyRunTimeSeconds { get; private set; }

        /// <inheritdoc/>
        public int FaultDataProcessorExecutionIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public OperationMode FaultDataProcessorOperationMode { get; private set; }

        /// <inheritdoc/>
        public int LogRecordProcessorBatchSize { get; private set; }

        /// <inheritdoc/>
        public DateTime LogRecordProcessorDailyStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int LogRecordProcessorDailyRunTimeSeconds { get; private set; }

        /// <inheritdoc/>
        public int LogRecordProcessorExecutionIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public OperationMode LogRecordProcessorOperationMode { get; private set; }

        /// <inheritdoc/>
        public string OptimizerDatabaseConnectionString { get; private set; }

        /// <inheritdoc/>
        public string OptimizerDatabaseProviderType { get; private set; }

        /// <inheritdoc/>
        public DateTime StatusDataOptimizerDailyStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int StatusDataOptimizerDailyRunTimeSeconds { get; private set; }

        /// <inheritdoc/>
        public int StatusDataOptimizerExecutionIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public int StatusDataOptimizerNumberOfCompassDirections { get; private set; }

        /// <inheritdoc/>
        public OperationMode StatusDataOptimizerOperationMode { get; private set; }

        /// <inheritdoc/>
        public bool StatusDataOptimizerPopulateBearing { get; private set; }

        /// <inheritdoc/>
        public bool StatusDataOptimizerPopulateDirection { get; private set; }

        /// <inheritdoc/>
        public bool StatusDataOptimizerPopulateDriverId { get; private set; }

        /// <inheritdoc/>
        public bool StatusDataOptimizerPopulateLongitudeLatitude { get; private set; }

        /// <inheritdoc/>
        public bool StatusDataOptimizerPopulateSpeed { get; private set; }

        /// <inheritdoc/>
        public int StatusDataProcessorBatchSize { get; private set; }

        /// <inheritdoc/>
        public DateTime StatusDataProcessorDailyStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int StatusDataProcessorDailyRunTimeSeconds { get; private set; }

        /// <inheritdoc/>
        public int StatusDataProcessorExecutionIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public OperationMode StatusDataProcessorOperationMode { get; private set; }

        /// <inheritdoc/>
        public int TimeoutSecondsForDatabaseTasks { get; private set; }

        /// <inheritdoc/>
        public DateTime UserProcessorDailyStartTimeUTC { get; private set; }

        /// <inheritdoc/>
        public int UserProcessorDailyRunTimeSeconds { get; private set; }

        /// <inheritdoc/>
        public int UserProcessorExecutionIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public OperationMode UserProcessorOperationMode { get; private set; }

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IConfigurationHelper configurationHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataOptimizerConfiguration"/> class.
        /// </summary>
        public DataOptimizerConfiguration(IConfigurationHelper configurationHelper)
        {
            this.configurationHelper = configurationHelper;
            ProcessConfigItems();
        }

        /// <inheritdoc/>
        public void ProcessConfigItems()
        {
            logger.Info ($"Processing configuration items.");

            string errorMessage;

            // OverrideSettings:
            DisableMachineNameValidation = configurationHelper.GetConfigKeyValueBoolean(ArgNameDisableMachineNameValidation);
            if (DisableMachineNameValidation == true)
            {
                logger.Warn($"WARNING: Machine name validation has been disabled. This should only be done in cases where the application is installed in hosted environments where machine names are not static. Improper use of this setting may lead to application instability and data integrity issues.");
            }

            // DatabaseSettings:AdapterDatabase:
            AdapterDatabaseConnectionString = configurationHelper.GetConfigKeyValueString(ArgNameAdapterDatabaseConnectionString, null, true, true);
            AdapterDatabaseProviderType = configurationHelper.GetConfigKeyValueString(ArgNameAdapterDatabaseProviderType);

            // DatabaseSettings:OptimizerDatabase:
            OptimizerDatabaseConnectionString = configurationHelper.GetConfigKeyValueString(ArgNameOptimizerDatabaseConnectionString, null, true, true);
            OptimizerDatabaseProviderType = configurationHelper.GetConfigKeyValueString(ArgNameOptimizerDatabaseProviderType);

            // AppSettings:GeneralSettings:
            TimeoutSecondsForDatabaseTasks = configurationHelper.GetConfigKeyValueInt(ArgNameTimeoutSecondsForDatabaseTasks, null, false, MinTimeoutSeconds, MaxTimeoutSeconds, DefaultTimeoutSeconds);

            // AppSettings:Processors:BinaryData:
            EnableBinaryDataProcessor = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableBinaryDataProcessor);
            BinaryDataProcessorBatchSize = configurationHelper.GetConfigKeyValueInt(ArgNameBinaryDataProcessorBatchSize, null, false, MinBatchSize, MaxBatchSize, DefaultBatchSize);
            BinaryDataProcessorExecutionIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameBinaryDataProcessorExecutionIntervalSeconds, null, false, MinExecutionIntervalSeconds, MaxExecutionIntervalSeconds, DefaultExecutionIntervalSeconds);
            string BinaryDataProcessorOperationModeString = configurationHelper.GetConfigKeyValueString(ArgNameBinaryDataProcessorOperationMode);
            switch (BinaryDataProcessorOperationModeString)
            {
                case nameof(OperationMode.Continuous):
                    BinaryDataProcessorOperationMode = OperationMode.Continuous;
                    break;
                case nameof(OperationMode.Scheduled):
                    BinaryDataProcessorOperationMode = OperationMode.Scheduled;
                    BinaryDataProcessorDailyStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameBinaryDataProcessorDailyStartTimeUTC);
                    BinaryDataProcessorDailyRunTimeSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameBinaryDataProcessorDailyRunTimeSeconds, null, false, MinDailyRunTimeSeconds, MaxDailyRunTimeSeconds, MinDailyRunTimeSeconds);
                    break;
                default:
                    errorMessage = $"The value of '{BinaryDataProcessorOperationModeString}' provided for the '{ArgNameBinaryDataProcessorOperationMode}' configuration item is not valid.";
                    logger.Error(errorMessage);
                    throw new Exception(errorMessage);
            }

            // AppSettings:Processors:Device:
            EnableDeviceProcessor = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDeviceProcessor);
            DeviceProcessorExecutionIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameDeviceProcessorExecutionIntervalSeconds, null, false, MinExecutionIntervalSeconds, MaxExecutionIntervalSeconds, DefaultExecutionIntervalSeconds);
            string deviceProcessorOperationModeString = configurationHelper.GetConfigKeyValueString(ArgNameDeviceProcessorOperationMode);
            switch (deviceProcessorOperationModeString)
            {
                case nameof(OperationMode.Continuous):
                    DeviceProcessorOperationMode = OperationMode.Continuous;
                    break;
                case nameof(OperationMode.Scheduled):
                    DeviceProcessorOperationMode = OperationMode.Scheduled;
                    DeviceProcessorDailyStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameDeviceProcessorDailyStartTimeUTC);
                    DeviceProcessorDailyRunTimeSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameDeviceProcessorDailyRunTimeSeconds, null, false, MinDailyRunTimeSeconds, MaxDailyRunTimeSeconds, MinDailyRunTimeSeconds);
                    break;
                default:
                    errorMessage = $"The value of '{deviceProcessorOperationModeString}' provided for the '{ArgNameDeviceProcessorOperationMode}' configuration item is not valid.";
                    logger.Error(errorMessage);
                    throw new Exception(errorMessage);
            }

            // AppSettings:Processors:Diagnostic:
            EnableDiagnosticProcessor = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDiagnosticProcessor);
            DiagnosticProcessorExecutionIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameDiagnosticProcessorExecutionIntervalSeconds, null, false, MinExecutionIntervalSeconds, MaxExecutionIntervalSeconds, DefaultExecutionIntervalSeconds);
            string DiagnosticProcessorOperationModeString = configurationHelper.GetConfigKeyValueString(ArgNameDiagnosticProcessorOperationMode);
            switch (DiagnosticProcessorOperationModeString)
            {
                case nameof(OperationMode.Continuous):
                    DiagnosticProcessorOperationMode = OperationMode.Continuous;
                    break;
                case nameof(OperationMode.Scheduled):
                    DiagnosticProcessorOperationMode = OperationMode.Scheduled;
                    DiagnosticProcessorDailyStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameDiagnosticProcessorDailyStartTimeUTC);
                    DiagnosticProcessorDailyRunTimeSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameDiagnosticProcessorDailyRunTimeSeconds, null, false, MinDailyRunTimeSeconds, MaxDailyRunTimeSeconds, MinDailyRunTimeSeconds);
                    break;
                default:
                    errorMessage = $"The value of '{DiagnosticProcessorOperationModeString}' provided for the '{ArgNameDiagnosticProcessorOperationMode}' configuration item is not valid.";
                    logger.Error(errorMessage);
                    throw new Exception(errorMessage);
            }

            // AppSettings:Processors:DriverChange:
            EnableDriverChangeProcessor = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableDriverChangeProcessor);
            DriverChangeProcessorBatchSize = configurationHelper.GetConfigKeyValueInt(ArgNameDriverChangeProcessorBatchSize, null, false, MinBatchSize, MaxBatchSize, DefaultBatchSize);
            DriverChangeProcessorExecutionIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameDriverChangeProcessorExecutionIntervalSeconds, null, false, MinExecutionIntervalSeconds, MaxExecutionIntervalSeconds, DefaultExecutionIntervalSeconds);
            string driverChangeProcessorOperationModeString = configurationHelper.GetConfigKeyValueString(ArgNameDriverChangeProcessorOperationMode);
            switch (driverChangeProcessorOperationModeString)
            {
                case nameof(OperationMode.Continuous):
                    DriverChangeProcessorOperationMode = OperationMode.Continuous;
                    break;
                case nameof(OperationMode.Scheduled):
                    DriverChangeProcessorOperationMode = OperationMode.Scheduled;
                    DriverChangeProcessorDailyStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameDriverChangeProcessorDailyStartTimeUTC);
                    DriverChangeProcessorDailyRunTimeSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameDriverChangeProcessorDailyRunTimeSeconds, null, false, MinDailyRunTimeSeconds, MaxDailyRunTimeSeconds, MinDailyRunTimeSeconds);
                    break;
                default:
                    errorMessage = $"The value of '{driverChangeProcessorOperationModeString}' provided for the '{ArgNameDriverChangeProcessorOperationMode}' configuration item is not valid.";
                    logger.Error(errorMessage);
                    throw new Exception(errorMessage);
            }

            // AppSettings:Processors:FaultData:
            EnableFaultDataProcessor = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableFaultDataProcessor);
            FaultDataProcessorBatchSize = configurationHelper.GetConfigKeyValueInt(ArgNameFaultDataProcessorBatchSize, null, false, MinBatchSize, MaxBatchSize, DefaultBatchSize);
            FaultDataProcessorExecutionIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameFaultDataProcessorExecutionIntervalSeconds, null, false, MinExecutionIntervalSeconds, MaxExecutionIntervalSeconds, DefaultExecutionIntervalSeconds);
            string faultDataProcessorOperationModeString = configurationHelper.GetConfigKeyValueString(ArgNameFaultDataProcessorOperationMode);
            switch (faultDataProcessorOperationModeString)
            {
                case nameof(OperationMode.Continuous):
                    FaultDataProcessorOperationMode = OperationMode.Continuous;
                    break;
                case nameof(OperationMode.Scheduled):
                    FaultDataProcessorOperationMode = OperationMode.Scheduled;
                    FaultDataProcessorDailyStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameFaultDataProcessorDailyStartTimeUTC);
                    FaultDataProcessorDailyRunTimeSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameFaultDataProcessorDailyRunTimeSeconds, null, false, MinDailyRunTimeSeconds, MaxDailyRunTimeSeconds, MinDailyRunTimeSeconds);
                    break;
                default:
                    errorMessage = $"The value of '{faultDataProcessorOperationModeString}' provided for the '{ArgNameFaultDataProcessorOperationMode}' configuration item is not valid.";
                    logger.Error(errorMessage);
                    throw new Exception(errorMessage);
            }

            // AppSettings:Processors:LogRecord:
            EnableLogRecordProcessor = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableLogRecordProcessor);
            LogRecordProcessorBatchSize = configurationHelper.GetConfigKeyValueInt(ArgNameLogRecordProcessorBatchSize, null, false, MinBatchSize, MaxBatchSize, DefaultBatchSize);
            LogRecordProcessorExecutionIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameLogRecordProcessorExecutionIntervalSeconds, null, false, MinExecutionIntervalSeconds, MaxExecutionIntervalSeconds, DefaultExecutionIntervalSeconds);
            string logRecordProcessorOperationModeString = configurationHelper.GetConfigKeyValueString(ArgNameLogRecordProcessorOperationMode);
            switch (logRecordProcessorOperationModeString)
            {
                case nameof(OperationMode.Continuous):
                    LogRecordProcessorOperationMode = OperationMode.Continuous;
                    break;
                case nameof(OperationMode.Scheduled):
                    LogRecordProcessorOperationMode = OperationMode.Scheduled;
                    LogRecordProcessorDailyStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameLogRecordProcessorDailyStartTimeUTC);
                    LogRecordProcessorDailyRunTimeSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameLogRecordProcessorDailyRunTimeSeconds, null, false, MinDailyRunTimeSeconds, MaxDailyRunTimeSeconds, MinDailyRunTimeSeconds);
                    break;
                default:
                    errorMessage = $"The value of '{logRecordProcessorOperationModeString}' provided for the '{ArgNameLogRecordProcessorOperationMode}' configuration item is not valid.";
                    logger.Error(errorMessage);
                    throw new Exception(errorMessage);
            }

            // AppSettings:Processors:StatusData:
            EnableStatusDataProcessor = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableStatusDataProcessor);
            StatusDataProcessorBatchSize = configurationHelper.GetConfigKeyValueInt(ArgNameStatusDataProcessorBatchSize, null, false, MinBatchSize, MaxBatchSize, DefaultBatchSize);
            StatusDataProcessorExecutionIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameStatusDataProcessorExecutionIntervalSeconds, null, false, MinExecutionIntervalSeconds, MaxExecutionIntervalSeconds, DefaultExecutionIntervalSeconds);
            string statusDataProcessorOperationModeString = configurationHelper.GetConfigKeyValueString(ArgNameStatusDataProcessorOperationMode);
            switch (statusDataProcessorOperationModeString)
            {
                case nameof(OperationMode.Continuous):
                    StatusDataProcessorOperationMode = OperationMode.Continuous;
                    break;
                case nameof(OperationMode.Scheduled):
                    StatusDataProcessorOperationMode = OperationMode.Scheduled;
                    StatusDataProcessorDailyStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameStatusDataProcessorDailyStartTimeUTC);
                    StatusDataProcessorDailyRunTimeSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameStatusDataProcessorDailyRunTimeSeconds, null, false, MinDailyRunTimeSeconds, MaxDailyRunTimeSeconds, MinDailyRunTimeSeconds);
                    break;
                default:
                    errorMessage = $"The value of '{statusDataProcessorOperationModeString}' provided for the '{ArgNameStatusDataProcessorOperationMode}' configuration item is not valid.";
                    logger.Error(errorMessage);
                    throw new Exception(errorMessage);
            }

            // AppSettings:Processors:User:
            EnableUserProcessor = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableUserProcessor);
            UserProcessorExecutionIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameUserProcessorExecutionIntervalSeconds, null, false, MinExecutionIntervalSeconds, MaxExecutionIntervalSeconds, DefaultExecutionIntervalSeconds);
            string userProcessorOperationModeString = configurationHelper.GetConfigKeyValueString(ArgNameUserProcessorOperationMode);
            switch (userProcessorOperationModeString)
            {
                case nameof(OperationMode.Continuous):
                    UserProcessorOperationMode = OperationMode.Continuous;
                    break;
                case nameof(OperationMode.Scheduled):
                    UserProcessorOperationMode = OperationMode.Scheduled;
                    UserProcessorDailyStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameUserProcessorDailyStartTimeUTC);
                    UserProcessorDailyRunTimeSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameUserProcessorDailyRunTimeSeconds, null, false, MinDailyRunTimeSeconds, MaxDailyRunTimeSeconds, MinDailyRunTimeSeconds);
                    break;
                default:
                    errorMessage = $"The value of '{userProcessorOperationModeString}' provided for the '{ArgNameUserProcessorOperationMode}' configuration item is not valid.";
                    logger.Error(errorMessage);
                    throw new Exception(errorMessage);
            }

            // AppSettings:Optimizers:FaultData:
            EnableFaultDataOptimizer = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableFaultDataOptimizer);
            FaultDataOptimizerExecutionIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameFaultDataOptimizerExecutionIntervalSeconds, null, false, MinExecutionIntervalSeconds, MaxExecutionIntervalSeconds, DefaultExecutionIntervalSeconds);
            string faultDataOptimizerOperationModeString = configurationHelper.GetConfigKeyValueString(ArgNameFaultDataOptimizerOperationMode);
            switch (faultDataOptimizerOperationModeString)
            {
                case nameof(OperationMode.Continuous):
                    FaultDataOptimizerOperationMode = OperationMode.Continuous;
                    break;
                case nameof(OperationMode.Scheduled):
                    FaultDataOptimizerOperationMode = OperationMode.Scheduled;
                    FaultDataOptimizerDailyStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameFaultDataOptimizerDailyStartTimeUTC);
                    FaultDataOptimizerDailyRunTimeSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameFaultDataOptimizerDailyRunTimeSeconds, null, false, MinDailyRunTimeSeconds, MaxDailyRunTimeSeconds, MinDailyRunTimeSeconds);
                    break;
                default:
                    errorMessage = $"The value of '{faultDataOptimizerOperationModeString}' provided for the '{ArgNameFaultDataOptimizerOperationMode}' configuration item is not valid.";
                    logger.Error(errorMessage);
                    throw new Exception(errorMessage);
            }
            FaultDataOptimizerPopulateLongitudeLatitude = configurationHelper.GetConfigKeyValueBoolean(ArgNameFaultDataOptimizerPopulateLongitudeLatitude);
            FaultDataOptimizerPopulateSpeed = configurationHelper.GetConfigKeyValueBoolean(ArgNameFaultDataOptimizerPopulateSpeed);
            FaultDataOptimizerPopulateBearing = configurationHelper.GetConfigKeyValueBoolean(ArgNameFaultDataOptimizerPopulateBearing);
            FaultDataOptimizerPopulateDirection = configurationHelper.GetConfigKeyValueBoolean(ArgNameFaultDataOptimizerPopulateDirection);
            FaultDataOptimizerNumberOfCompassDirections = configurationHelper.GetConfigKeyValueInt(ArgNameFaultDataOptimizerNumberOfCompassDirections, null, false, 4, 16, 16);
            if (FaultDataOptimizerNumberOfCompassDirections != 4 && FaultDataOptimizerNumberOfCompassDirections != 8 && FaultDataOptimizerNumberOfCompassDirections != 16)
            {
                errorMessage = $"The value of '{FaultDataOptimizerNumberOfCompassDirections}' provided for the '{ArgNameFaultDataOptimizerNumberOfCompassDirections}' configuration item is not valid. Value must be one of 4, 8 or 16.";
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
            FaultDataOptimizerPopulateDriverId = configurationHelper.GetConfigKeyValueBoolean(ArgNameFaultDataOptimizerPopulateDriverId);

            // AppSettings:Optimizers:StatusData:
            EnableStatusDataOptimizer = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableStatusDataOptimizer);
            StatusDataOptimizerExecutionIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameStatusDataOptimizerExecutionIntervalSeconds, null, false, MinExecutionIntervalSeconds, MaxExecutionIntervalSeconds, DefaultExecutionIntervalSeconds);
            string statusDataOptimizerOperationModeString = configurationHelper.GetConfigKeyValueString(ArgNameStatusDataOptimizerOperationMode);
            switch (statusDataOptimizerOperationModeString)
            {
                case nameof(OperationMode.Continuous):
                    StatusDataOptimizerOperationMode = OperationMode.Continuous;
                    break;
                case nameof(OperationMode.Scheduled):
                    StatusDataOptimizerOperationMode = OperationMode.Scheduled;
                    StatusDataOptimizerDailyStartTimeUTC = configurationHelper.GetConfigKeyValueDateTime(ArgNameStatusDataOptimizerDailyStartTimeUTC);
                    StatusDataOptimizerDailyRunTimeSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameStatusDataOptimizerDailyRunTimeSeconds, null, false, MinDailyRunTimeSeconds, MaxDailyRunTimeSeconds, MinDailyRunTimeSeconds);
                    break;
                default:
                    errorMessage = $"The value of '{statusDataOptimizerOperationModeString}' provided for the '{ArgNameStatusDataOptimizerOperationMode}' configuration item is not valid.";
                    logger.Error(errorMessage);
                    throw new Exception(errorMessage);
            }
            StatusDataOptimizerPopulateLongitudeLatitude = configurationHelper.GetConfigKeyValueBoolean(ArgNameStatusDataOptimizerPopulateLongitudeLatitude);
            StatusDataOptimizerPopulateSpeed = configurationHelper.GetConfigKeyValueBoolean(ArgNameStatusDataOptimizerPopulateSpeed);
            StatusDataOptimizerPopulateBearing = configurationHelper.GetConfigKeyValueBoolean(ArgNameStatusDataOptimizerPopulateBearing);
            StatusDataOptimizerPopulateDirection = configurationHelper.GetConfigKeyValueBoolean(ArgNameStatusDataOptimizerPopulateDirection);
            StatusDataOptimizerNumberOfCompassDirections = configurationHelper.GetConfigKeyValueInt(ArgNameStatusDataOptimizerNumberOfCompassDirections, null, false, 4, 16, 16);
            if (StatusDataOptimizerNumberOfCompassDirections != 4 && StatusDataOptimizerNumberOfCompassDirections != 8 && StatusDataOptimizerNumberOfCompassDirections != 16)
            {
                errorMessage = $"The value of '{StatusDataOptimizerNumberOfCompassDirections}' provided for the '{ArgNameStatusDataOptimizerNumberOfCompassDirections}' configuration item is not valid. Value must be one of 4, 8 or 16.";
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
            StatusDataOptimizerPopulateDriverId = configurationHelper.GetConfigKeyValueBoolean(ArgNameStatusDataOptimizerPopulateDriverId);
        }
    }
}
