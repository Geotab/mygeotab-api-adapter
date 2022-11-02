using Microsoft.Extensions.Configuration;
using MyGeotabAPIAdapter.Helpers;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Configuration.Add_Ons.VSS
{
    /// <summary>
    /// A class that reads (from appsettings.json) and stores application configuration settings related specifically to the VSS Add-On. Intended for association with the MyGeotabAPIAdapter project.
    /// </summary>
    public class VSSConfiguration : IVSSConfiguration
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }

        // Argument Names for appsettings:
        const string ArgumentNameAttributeVSSPathMaps = "VSSPathMaps:AttributeVSSPathMaps";
        const string ArgumentNameDisableCorrespondingAdapterOutput = "AppSettings:AddOns:VSS:DisableCorrespondingAdapterOutput";
        const string ArgumentNameEnableVSSAddOn = "AppSettings:AddOns:VSS:EnableVSSAddOn";
        const string ArgumentNameLogRecordVSSPathMaps = "VSSPathMaps:LogRecordVSSPathMaps";
        const string ArgumentNameLogUnmappedDiagnostics = "AppSettings:AddOns:VSS:LogUnmappedDiagnostics";
        const string ArgumentNameOutputLogRecordsToOVDS = "AppSettings:AddOns:VSS:OutputLogRecordsToOVDS";
        const string ArgumentNameOutputStatusDataToOVDS = "AppSettings:AddOns:VSS:OutputStatusDataToOVDS";
        const string ArgumentNameOVDSClientWorkerIntervalSeconds = "AppSettings:AddOns:VSS:OVDSClientWorkerIntervalSeconds";
        const string ArgumentNameOVDSServerURL = "AppSettings:AddOns:VSS:OVDSServerURL";
        const string ArgumentNameOVDSSetCommandTemplate = "AppSettings:AddOns:VSS:OVDSSetCommandTemplate";
        const string ArgumentNameOVDSSetCommandTemplateForAttributeVSSPathMaps = "AppSettings:AddOns:VSS:OVDSSetCommandTemplateForAttributeVSSPathMaps";
        const string ArgumentNameSendAttributeTypeDataToOVDS = "AppSettings:AddOns:VSS:SendAttributeTypeDataToOVDS";
        const string ArgumentNameStatusDataVSSPathMaps = "VSSPathMaps:StatusDataVSSPathMaps";
        const string ArgumentNameUnmappedDiagnosticsLogIntervalMinutes = "AppSettings:AddOns:VSS:UnmappedDiagnosticsLogIntervalMinutes";
        const string ArgumentNameVSSPathMapFileURL = "AppSettings:AddOns:VSS:VSSPathMapFileURL";
        const string ArgumentNameVSSVersion = "AppSettings:AddOns:VSS:VSSVersion";
        const string ArgumentNameVSSPathMapUpdateIntervalMinutes = "AppSettings:AddOns:VSS:VSSPathMapUpdateIntervalMinutes";
                
        const string PlaceholderStringForPath = "PLACEHOLDER_PATH";
        const string PlaceholderStringForTimestamp = "PLACEHOLDER_TIMESTAMP";
        const string PlaceholderStringForValue = "PLACEHOLDER_VALUE";
        const string PlaceholderStringForVIN = "PLACEHOLDER_VIN";
        const int OVDSServerCmdBatchSize = 5000;
        const int LogRecordFeedResultsLimitWhenOutputtingToOVDS = 5000;
        const int StatusDataFeedResultsLimitWhenOutputtingToOVDS = 10000;

        /// <inheritdoc/>
        public string URLJSONFileExtension { get => ".json"; }

        /// <inheritdoc/>
        public string URLVSSVersionDelimiter { get => "-v"; }

        /// <inheritdoc/>
        public string VSSPathMapFileName { get => "VSSPathMaps.json"; }

        /// <inheritdoc/>
        public string VSSPathMapTempFileName { get => "DOWNLOAD-VSSPathMaps.json"; }

        IDictionary<string, VSSPathMap> attributeVSSPathMaps;
        IDictionary<string, VSSPathMap> logRecordVSSPathMaps;
        IDictionary<string, VSSPathMap> statusDataVSSPathMaps;

        readonly SemaphoreSlim initializationLock = new(1, 1);
        bool isInitialized;
        bool isInternalUpdate;
        DateTime lastVSSConfigurationRefreshTime = DateTime.MinValue;

        readonly IConfigurationHelper configurationHelper;
        readonly IDateTimeHelper dateTimeHelper;
        readonly IHttpHelper httpHelper;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public string ArgNameAttributeVSSPathMaps
        {
            get => ArgumentNameAttributeVSSPathMaps;
        }

        /// <inheritdoc/>
        public string ArgNameDisableCorrespondingAdapterOutput
        {
            get => ArgumentNameDisableCorrespondingAdapterOutput;
        }

        /// <inheritdoc/>
        public string ArgNameEnableVSSAddOn
        {
            get => ArgumentNameEnableVSSAddOn;
        }

        /// <inheritdoc/>
        public string ArgNameLogRecordVSSPathMaps
        {
            get => ArgumentNameLogRecordVSSPathMaps;
        }

        /// <inheritdoc/>
        public string ArgNameOutputLogRecordsToOVDS
        {
            get => ArgumentNameOutputLogRecordsToOVDS;
        }

        /// <inheritdoc/>
        public string ArgNameLogUnmappedDiagnostics
        {
            get => ArgumentNameLogUnmappedDiagnostics;
        }

        /// <inheritdoc/>
        public string ArgNameOutputStatusDataToOVDS
        {
            get => ArgumentNameOutputStatusDataToOVDS;
        }

        /// <inheritdoc/>
        public string ArgNameOVDSClientWorkerIntervalSeconds
        {
            get => ArgumentNameOVDSClientWorkerIntervalSeconds;
        }

        /// <inheritdoc/>
        public string ArgNameOVDSServerURL
        {
            get => ArgumentNameOVDSServerURL;
        }

        /// <inheritdoc/>
        public string ArgNameOVDSSetCommandTemplate
        {
            get => ArgumentNameOVDSSetCommandTemplate;
        }

        /// <inheritdoc/>
        public string ArgNameOVDSSetCommandTemplateForAttributeVSSPathMaps
        {
            get => ArgumentNameOVDSSetCommandTemplateForAttributeVSSPathMaps;
        }

        /// <inheritdoc/>
        public string ArgNameSendAttributeTypeDataToOVDS
        {
            get => ArgumentNameSendAttributeTypeDataToOVDS;
        }

        /// <inheritdoc/>
        public string ArgNameStatusDataVSSPathMaps
        {
            get => ArgumentNameStatusDataVSSPathMaps;
        }

        /// <inheritdoc/>
        public string ArgNameUnmappedDiagnosticsLogIntervalMinutes
        {
            get => ArgumentNameUnmappedDiagnosticsLogIntervalMinutes;
        }

        /// <inheritdoc/>
        public string ArgNameVSSPathMapFileURL
        {
            get => ArgumentNameVSSPathMapFileURL;
        }

        /// <inheritdoc/>
        public string ArgNameVSSPathMapUpdateIntervalMinutes
        {
            get => ArgumentNameVSSPathMapUpdateIntervalMinutes;
        }

        /// <inheritdoc/>
        public string ArgNameVSSVersion
        {
            get => ArgumentNameVSSVersion;
        }

        /// <inheritdoc/>
        public IDictionary<string, VSSPathMap> AttributeVSSPathMaps
        {
            get
            {
                ValidateInitialized();
                return attributeVSSPathMaps;
            }
            private set
            {
                attributeVSSPathMaps = value;
            }
        }

        /// <inheritdoc/>
        public bool DisableCorrespondingAdapterOutput { get; private set; }

        /// <inheritdoc/>
        public bool EnableVSSAddOn { get; private set; }

        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <inheritdoc/>
        public bool IsInitialized { get => isInitialized; }

        /// <inheritdoc/>
        public int LogRecordFeedResultsLimitWhenOutputtingLogRecordsToOVDS
        {
            get => LogRecordFeedResultsLimitWhenOutputtingToOVDS;
        }

        /// <inheritdoc/>
        public IDictionary<string, VSSPathMap> LogRecordVSSPathMaps 
        {
            get
            { 
                ValidateInitialized();
                return logRecordVSSPathMaps;
            }
            private set
            { 
                logRecordVSSPathMaps = value;
            }
        }

        /// <inheritdoc/>
        public bool LogUnmappedDiagnostics { get; private set; }

        /// <inheritdoc/>
        public bool OutputLogRecordsToOVDS { get; private set; }

        /// <inheritdoc/>
        public bool OutputStatusDataToOVDS { get; private set; }

        /// <inheritdoc/>
        public int OVDSClientWorkerIntervalSeconds { get; private set; }

        /// <inheritdoc/>
        public int OVDSServerCommandBatchSize
        {
            get => OVDSServerCmdBatchSize;
        }

        /// <inheritdoc/>
        public string OVDSServerURL { get; private set; }

        /// <inheritdoc/>
        public string OVDSSetCommandTemplate { get; private set; }

        /// <inheritdoc/>
        public string OVDSSetCommandTemplateForAttributeVSSPathMaps { get; private set; }

        /// <inheritdoc/>
        public string PlaceholderStringForPathValue
        {
            get => PlaceholderStringForPath;
        }

        /// <inheritdoc/>
        public string PlaceholderStringForTimestampValue
        {
            get => PlaceholderStringForTimestamp;
        }

        /// <inheritdoc/>
        public string PlaceholderStringForValueValue
        {
            get => PlaceholderStringForValue;
        }

        /// <inheritdoc/>
        public string PlaceholderStringForVINValue
        {
            get => PlaceholderStringForVIN;
        }

        /// <inheritdoc/>
        public bool SendAttributeTypeDataToOVDS { get; private set; }

        /// <inheritdoc/>
        public int StatusDataFeedResultsLimitWhenOutputtingStatusDataToOVDS
        {
            get => StatusDataFeedResultsLimitWhenOutputtingToOVDS;
        }

        /// <inheritdoc/>
        public IDictionary<string, VSSPathMap> StatusDataVSSPathMaps
        {
            get
            {
                ValidateInitialized();
                return statusDataVSSPathMaps;
            }
            private set
            {
                statusDataVSSPathMaps = value;
            }
        }

        /// <inheritdoc/>
        public int UnmappedDiagnosticsLogIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public string VSSPathMapFileURL { get; private set; }

        /// <inheritdoc/>
        public int VSSPathMapUpdateIntervalMinutes { get; private set; }

        /// <inheritdoc/>
        public string VSSVersion { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VSSConfiguration"/> class.
        /// </summary>
        public VSSConfiguration(IConfigurationHelper configurationHelper, IDateTimeHelper dateTimeHelper, IHttpHelper httpHelper)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.configurationHelper = configurationHelper;
            this.dateTimeHelper = dateTimeHelper;
            this.httpHelper = httpHelper;
            ProcessConfigItems();

            Id = Guid.NewGuid().ToString();
            logger.Debug($"{nameof(VSSConfiguration)} [Id: {Id}] created.");
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Generates the message for an <see cref="Exception"/> occurring while attempting to POST an OVDS server command. 
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <returns>The generated error message.</returns>
        static string GenerateMessageForOVDSClientWorkerException(Exception exception)
        {
            string exceptionTypeName = exception.GetType().Name;
            StringBuilder messageBuilder = new();
            messageBuilder.AppendLine($"TYPE: [{exceptionTypeName}];");
            messageBuilder.AppendLine($"MESSAGE [{exception.Message}];");

            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
                exceptionTypeName = exception.GetType().Name;
                messageBuilder.AppendLine($"---------- INNER EXCEPTION ----------");
                messageBuilder.AppendLine($"TYPE: [{exceptionTypeName}];");
                messageBuilder.AppendLine($"MESSAGE [{exception.Message}];");
            }

            return messageBuilder.ToString();
        }

        /// <inheritdoc/>
        public VSSOutputOptions GetVSSOutputOptionForLogRecords()
        {
            VSSOutputOptions logRecordOutputOption = VSSOutputOptions.None;
            if (EnableVSSAddOn == true)
            {
                if (DisableCorrespondingAdapterOutput == true)
                {
                    if (OutputLogRecordsToOVDS == true)
                    {
                        logRecordOutputOption = VSSOutputOptions.DbOVDSServerCommandOnly;
                    }
                }
                else
                {
                    if (OutputLogRecordsToOVDS == true)
                    {
                        logRecordOutputOption = VSSOutputOptions.AdapterRecordAndDbOVDSServerCommand;
                    }
                    else
                    {
                        logRecordOutputOption = VSSOutputOptions.AdapterRecordOnly;
                    }
                }
            }
            else
            {
                logRecordOutputOption = VSSOutputOptions.AdapterRecordOnly;
            }
            if (logRecordOutputOption == VSSOutputOptions.None)
            {
                logger.Warn($"A data feed is configured to stream LogRecords from the MyGeotab system, but the LogRecords are not configured to be output in any form to the adapter database. This will result in unnecessary processing of LogRecords. Consider modifying the appsettings.json file to remedy the situation.");
            }
            return logRecordOutputOption;
        }

        /// <inheritdoc/>
        public VSSOutputOptions GetVSSOutputOptionForStatusData()
        {
            VSSOutputOptions statusDataOutputOption = VSSOutputOptions.None;
            if (EnableVSSAddOn == true)
            {
                if (DisableCorrespondingAdapterOutput == true)
                {
                    if (OutputStatusDataToOVDS == true)
                    {
                        statusDataOutputOption = VSSOutputOptions.DbOVDSServerCommandOnly;
                    }
                }
                else
                {
                    if (OutputStatusDataToOVDS == true)
                    {
                        statusDataOutputOption = VSSOutputOptions.AdapterRecordAndDbOVDSServerCommand;
                    }
                    else
                    {
                        statusDataOutputOption = VSSOutputOptions.AdapterRecordOnly;
                    }
                }
            }
            else
            {
                statusDataOutputOption = VSSOutputOptions.AdapterRecordOnly;
            }
            if (statusDataOutputOption == VSSOutputOptions.None)
            {
                logger.Warn($"A data feed is configured to stream StatusData records from the MyGeotab system, but the StatusData records are not configured to be output in any form to the adapter database. This will result in unnecessary processing of StatusData records. Consider modifying the appsettings.json file to remedy the situation.");
            }
            return statusDataOutputOption;
        }

        /// <inheritdoc/>
        public async Task InitializeAsync(string vssPathMapFileDirectory, string vssPathMapFileName)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await initializationLock.WaitAsync();
            try
            {
                if (isInitialized == false)
                {
                    isInternalUpdate = true;
                    await DownloadVSSPathMapsFileAsync();
                    var vssConfig = new ConfigurationBuilder()
                        .SetBasePath(vssPathMapFileDirectory)
                        .AddJsonFile(vssPathMapFileName, optional: false)
                        .Build();
                    configurationHelper.SetConfiguration(vssConfig);
                    LoadVSSPathMaps();
                    isInitialized = true;
                }
                else
                {
                    logger.Debug($"The current {CurrentClassName} has already been initialized.");
                }
            }
            finally
            {
                isInternalUpdate = false;
                initializationLock.Release();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Populates <see cref="AttributeVSSPathMaps"/>.
        /// </summary>
        void LoadAttributeVSSPathMaps()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            AttributeVSSPathMaps = new Dictionary<string, VSSPathMap>();
            var attributeVSSPathMapsSection = configurationHelper.GetConfigSection(ArgNameAttributeVSSPathMaps);
            var attributeVSSPathMaps = attributeVSSPathMapsSection.Get<VSSPathMap[]>();
            foreach (var attributeVSSPathMap in attributeVSSPathMaps)
            {
                attributeVSSPathMap.GeotabObjectType = VSSMappableGeotabObjectTypes.AttributeStatusData.ToString();
                attributeVSSPathMap.GeotabObjectPropertyName = nameof(GeotabObjectFieldNames.Data);
                AttributeVSSPathMaps.Add(attributeVSSPathMap.GeotabDiagnosticId, attributeVSSPathMap);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Populates <see cref="LogRecordVSSPathMaps"/>.
        /// </summary>
        void LoadLogRecordVSSPathMaps()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            LogRecordVSSPathMaps = new Dictionary<string, VSSPathMap>();
            var logRecordVSSPathMapsSection = configurationHelper.GetConfigSection(ArgNameLogRecordVSSPathMaps);
            var logRecordVSSPathMaps = logRecordVSSPathMapsSection.Get<VSSPathMap[]>();
            foreach (var logRecordVSSPathMap in logRecordVSSPathMaps)
            {
                logRecordVSSPathMap.GeotabObjectType = VSSMappableGeotabObjectTypes.LogRecord.ToString();
                LogRecordVSSPathMaps.Add(logRecordVSSPathMap.GeotabObjectPropertyName, logRecordVSSPathMap);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Populates <see cref="StatusDataVSSPathMaps"/>.
        /// </summary>
        void LoadStatusDataVSSPathMaps()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            StatusDataVSSPathMaps = new Dictionary<string, VSSPathMap>();
            var statusDataVSSPathMapsSection = configurationHelper.GetConfigSection(ArgNameStatusDataVSSPathMaps);
            var statusDataVSSPathMaps = statusDataVSSPathMapsSection.Get<VSSPathMap[]>();
            foreach (var statusDataVSSPathMap in statusDataVSSPathMaps)
            {
                statusDataVSSPathMap.GeotabObjectType = VSSMappableGeotabObjectTypes.StatusData.ToString();
                statusDataVSSPathMap.GeotabObjectPropertyName = nameof(GeotabObjectFieldNames.Data);
                StatusDataVSSPathMaps.Add(statusDataVSSPathMap.GeotabDiagnosticId, statusDataVSSPathMap);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Populates <see cref="AttributeVSSPathMaps"/>, <see cref="LogRecordVSSPathMaps"/> and <see cref="StatusDataVSSPathMaps"/>.
        /// </summary>
        void LoadVSSPathMaps()
        {
            ValidateInitialized();
            LoadAttributeVSSPathMaps();
            LoadLogRecordVSSPathMaps();
            LoadStatusDataVSSPathMaps();
        }

        /// <inheritdoc/>
        public void ProcessConfigItems()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");
            logger.Info($"Processing configuration items.");

            // AppSettings:AddOns:VSS:
            EnableVSSAddOn = configurationHelper.GetConfigKeyValueBoolean(ArgNameEnableVSSAddOn);
            DisableCorrespondingAdapterOutput = configurationHelper.GetConfigKeyValueBoolean(ArgNameDisableCorrespondingAdapterOutput);
            OutputLogRecordsToOVDS = configurationHelper.GetConfigKeyValueBoolean(ArgNameOutputLogRecordsToOVDS);
            OutputStatusDataToOVDS = configurationHelper.GetConfigKeyValueBoolean(ArgNameOutputStatusDataToOVDS);
            LogUnmappedDiagnostics = configurationHelper.GetConfigKeyValueBoolean(ArgNameLogUnmappedDiagnostics);
            UnmappedDiagnosticsLogIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameUnmappedDiagnosticsLogIntervalMinutes);
            VSSPathMapFileURL = configurationHelper.GetConfigKeyValueString(ArgNameVSSPathMapFileURL);
            VSSVersion = configurationHelper.GetConfigKeyValueString(ArgNameVSSVersion);
            VSSPathMapUpdateIntervalMinutes = configurationHelper.GetConfigKeyValueInt(ArgNameVSSPathMapUpdateIntervalMinutes);
            SendAttributeTypeDataToOVDS = configurationHelper.GetConfigKeyValueBoolean(ArgNameSendAttributeTypeDataToOVDS);
            OVDSClientWorkerIntervalSeconds = configurationHelper.GetConfigKeyValueInt(ArgNameOVDSClientWorkerIntervalSeconds);
            OVDSServerURL = configurationHelper.GetConfigKeyValueString(ArgNameOVDSServerURL);
            OVDSSetCommandTemplate = configurationHelper.GetConfigKeyValueString(ArgNameOVDSSetCommandTemplate);
            OVDSSetCommandTemplateForAttributeVSSPathMaps = configurationHelper.GetConfigKeyValueString(ArgNameOVDSSetCommandTemplateForAttributeVSSPathMaps);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Downloads the VSSPathMaps file.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        async Task DownloadVSSPathMapsFileAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Construct URL of VSS path map file - incorporating the VSS version.
            StringBuilder urlBuilder = new();
            urlBuilder.Append(VSSPathMapFileURL);
            urlBuilder.Replace(URLJSONFileExtension, null);
            urlBuilder.Append(URLVSSVersionDelimiter);
            urlBuilder.Append(VSSVersion);
            urlBuilder.Append(URLJSONFileExtension);

            // Validate the URL.
            bool urlIsValid = Uri.TryCreate(urlBuilder.ToString(), UriKind.Absolute, out Uri uri);
            if (urlIsValid == false)
            {
                throw new Exception($"Unable to construct valid URL using '{ArgNameVSSPathMapFileURL}' and '{ArgNameVSSVersion}' values from appsettings.json.");
            }

            // Download the VSS path map file.
            var vssFilePath = $"{AppContext.BaseDirectory}{VSSPathMapFileName}";
            var downloadTempFilePath = $"{AppContext.BaseDirectory}{VSSPathMapTempFileName}";
            try
            {
                await httpHelper.DownloadFileAsync(uri.AbsoluteUri, downloadTempFilePath);
                File.Move(downloadTempFilePath, vssFilePath, true);
            }
            catch (Exception exception)
            {
                // Log the exception. If a previously-downloaded copy of the VSS path map file exists, use that one; otherwise, throw the exception.
                var exceptionMessage = GenerateMessageForOVDSClientWorkerException(exception);
                StringBuilder exceptionMessageBuilder = new();
                exceptionMessageBuilder.Append($"An exception was encountered while attempting to download the VSS path map file '{uri.AbsoluteUri}' to '{vssFilePath}'.");
                if (File.Exists(vssFilePath))
                {
                    exceptionMessageBuilder.Append($"The previously-downloaded copy of the VSS path map file will be used this time. Exception details: {exceptionMessage}");
                    logger.Warn(exceptionMessageBuilder.ToString());
                }
                else
                {
                    exceptionMessageBuilder.Append($"Exception details: {exceptionMessage}");
                    logger.Warn(exceptionMessageBuilder.ToString());
                    throw;
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Reloads the LogRecordVSSPathMaps and StatusDataVSSPathMaps from appsettings.json and updates <see cref="vssConfiguration"/> accordingly so that changes can be made to the VSS path maps while the application is running. Only performs this activity if <see cref="vssConfiguration.VSSPathMapUpdateIntervalMinutes"/> has elapsed since the last time this activity was performed.
        /// </summary>
        public async Task UpdateVSSPathMapsAsync()
        {
            if (dateTimeHelper.TimeIntervalHasElapsed(lastVSSConfigurationRefreshTime, DateTimeIntervalType.Minutes, VSSPathMapUpdateIntervalMinutes))
            {
                // Load the VSS path map file and process its contents.
                await DownloadVSSPathMapsFileAsync();
                LoadVSSPathMaps();

                lastVSSConfigurationRefreshTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Checks whether the <see cref="InitializeAsync(string, string)"/> method has been invoked since the current class instance was created and throws an <see cref="InvalidOperationException"/> if initialization has not been performed.
        /// </summary>
        void ValidateInitialized()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (isInitialized == false && isInternalUpdate == false)
            {
                throw new InvalidOperationException($"The current {CurrentClassName} has not been initialized. The {nameof(InitializeAsync)} method must be called before other methods can be invoked.");
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
