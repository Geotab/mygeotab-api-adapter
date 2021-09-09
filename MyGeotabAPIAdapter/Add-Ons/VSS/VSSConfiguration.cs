using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Microsoft.Extensions.Configuration;
using MyGeotabAPIAdapter.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MyGeotabAPIAdapter.Add_Ons.VSS
{
    // Application configuration settings (from appsettings.json) related specifically to the VSS Add-On.
    public class VSSConfiguration : IDisposable
    {
        private bool _disposed = false;
        ~VSSConfiguration() => Dispose(false);

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

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        IDictionary<string, VSSPathMap> attributeVSSPathMaps;
        bool disableOtherAdapterFunctionality;
        bool enableVSSAddOn;
        IDictionary<string, VSSPathMap> logRecordVSSPathMaps;
        bool logUnmappedDiagnostics;
        bool outputLogRecordsToOVDS;
        bool outputStatusDataToOVDS;
        int oVDSClientWorkerIntervalSeconds;
        string oVDSServerURL;
        string oVDSSetCommandTemplate;
        string oVDSSetCommandTemplateForAttributeVSSPathMaps;
        bool sendAttributeTypeDataToOVDS;
        IDictionary<string, VSSPathMap> statusDataVSSPathMaps;
        int unmappedDiagnosticsLogIntervalMinutes;
        string vssPathMapFileURL;
        int vssPathMapUpdateIntervalMinutes;
        string vssVersion;

        /// <summary>
        /// Creates a new <see cref="VSSConfiguration"/> instance.
        /// </summary>
        public VSSConfiguration()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed state (managed objects).
            }
            _disposed = true;
        }

        /// <summary>
        /// Names of specific fields of MyGeotab objects that are used in VSS processing.
        /// </summary>
        public enum GeotabObjectFieldNames { Data, Latitude, Longitude, Speed }

        /// <summary>
        /// MyGeotab object types that can be mapped to VSS.
        /// </summary>
        public enum VSSMappableGeotabObjectTypes { LogRecord, StatusData, AttributeStatusData }

        /// <summary>
        /// The path of the "AttributeVSSPathMaps" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameAttributeVSSPathMaps
        {
            get => ArgumentNameAttributeVSSPathMaps;
        }

        /// <summary>
        /// The path of the "DisableCorrespondingAdapterOutput" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameDisableCorrespondingAdapterOutput
        {
            get => ArgumentNameDisableCorrespondingAdapterOutput;
        }

        /// <summary>
        /// The path of the "EnableVSSAddOn" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameEnableVSSAddOn
        {
            get => ArgumentNameEnableVSSAddOn;
        }

        /// <summary>
        /// The path of the "LogRecordVSSPathMaps" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameLogRecordVSSPathMaps
        {
            get => ArgumentNameLogRecordVSSPathMaps;
        }

        /// <summary>
        /// The path of the "OutputLogRecordsToOVDS" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameOutputLogRecordsToOVDS
        {
            get => ArgumentNameOutputLogRecordsToOVDS;
        }

        /// <summary>
        /// The path of the "LogUnmappedDiagnostics" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameLogUnmappedDiagnostics
        {
            get => ArgumentNameLogUnmappedDiagnostics;
        }

        /// <summary>
        /// The path of the "OutputStatusDataToOVDS" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameOutputStatusDataToOVDS
        {
            get => ArgumentNameOutputStatusDataToOVDS;
        }

        /// <summary>
        /// The path of the "OVDSClientWorkerIntervalSeconds" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameOVDSClientWorkerIntervalSeconds
        {
            get => ArgumentNameOVDSClientWorkerIntervalSeconds;
        }

        /// <summary>
        /// The path of the "OVDSServerURL" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameOVDSServerURL
        {
            get => ArgumentNameOVDSServerURL;
        }

        /// <summary>
        /// The path of the "OVDSSetCommandTemplate" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameOVDSSetCommandTemplate
        {
            get => ArgumentNameOVDSSetCommandTemplate;
        }

        /// <summary>
        /// The path of the "OVDSSetCommandTemplateForAttributeVSSPathMaps" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameOVDSSetCommandTemplateForAttributeVSSPathMaps
        {
            get => ArgumentNameOVDSSetCommandTemplateForAttributeVSSPathMaps;
        }

        /// <summary>
        /// The path of the "SendAttributeTypeDataToOVDS" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameSendAttributeTypeDataToOVDS
        {
            get => ArgumentNameSendAttributeTypeDataToOVDS;
        }

        /// <summary>
        /// The path of the "StatusDataVSSPathMaps" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameStatusDataVSSPathMaps
        {
            get => ArgumentNameStatusDataVSSPathMaps;
        }

        /// <summary>
        /// The path of the "UnmappedDiagnosticsLogIntervalMinutes" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameUnmappedDiagnosticsLogIntervalMinutes
        {
            get => ArgumentNameUnmappedDiagnosticsLogIntervalMinutes;
        }

        /// <summary>
        /// The path of the "VSSPathMapFileURL" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameVSSPathMapFileURL
        {
            get => ArgumentNameVSSPathMapFileURL;
        }

        /// <summary>
        /// The path of the "VSSPathMapUpdateIntervalMinutes" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameVSSPathMapUpdateIntervalMinutes
        {
            get => ArgumentNameVSSPathMapUpdateIntervalMinutes;
        }

        /// <summary>
        /// The path of the "VSSVersion" setting in the appsettings.json file. 
        /// </summary>
        public static string ArgNameVSSVersion
        {
            get => ArgumentNameVSSVersion;
        }

        /// <summary>
        /// The VSS path mapping information related to 'Attribute' <see cref="StatusData"/> objects. The <see cref="VSSPathMap.GeotabDiagnosticId"/> is used as the Key.
        /// </summary>
        public IDictionary<string, VSSPathMap> AttributeVSSPathMaps
        {
            get => attributeVSSPathMaps;
            set => attributeVSSPathMaps = value;
        }

        /// <summary>
        /// Indicates whether corresponding MyGeotab API Adapter functionality should be disabled. For example, if set to <c>true</c>, when outputting LogRecords to OVDS, LogRecords will not be written to the LogRecords table in the adapter database.
        /// </summary>
        public bool DisableCorrespondingAdapterOutput
        {
            get => disableOtherAdapterFunctionality;
            set => disableOtherAdapterFunctionality = value;
        }

        /// <summary>
        /// Indicates whether the VSS Add-On functionality should be enabled.
        /// </summary>
        public bool EnableVSSAddOn
        {
            get => enableVSSAddOn;
            set => enableVSSAddOn = value;
        }

        /// <summary>
        /// The feed results limit to be used to override the default for <see cref="LogRecord"/>s when <see cref="LogRecord"/>s are being output as OVDS server commands. Typically, the default is 50,000, but for each <see cref="LogRecord"/>, three OVDS server commands must be generated, resulting in 150,000 records that must be added to to the OVDSServerCommands table for every 50,000 records that must be added to the LogRecords table. Handling this many records along with those of other feeds in a single database transaction can have performance implications - so, smaller batches of <see cref="LogRecord"/>s should be processed when outputting as OVDS server commands.  
        /// </summary>
        public static int LogRecordFeedResultsLimitWhenOutputtingLogRecordsToOVDS
        {
            get => LogRecordFeedResultsLimitWhenOutputtingToOVDS;
        }

        /// <summary>
        /// The VSS path mapping information related to <see cref="LogRecord"/> objects. The <see cref="VSSPathMap.GeotabObjectPropertyName"/> is used as the Key.
        /// </summary>
        public IDictionary<string, VSSPathMap> LogRecordVSSPathMaps
        {
            get => logRecordVSSPathMaps;
            set => logRecordVSSPathMaps = value;
        }

        /// <summary>
        /// Indicates whether <see cref="UnmappedDiagnostic"/>s should be written to the log file periodically.
        /// </summary>
        public bool LogUnmappedDiagnostics
        {
            get => logUnmappedDiagnostics;
            set => logUnmappedDiagnostics = value;
        }

        /// <summary>
        /// Indicates whether <see cref="LogRecord"/> objects should be output to the OVDS server.
        /// </summary>
        public bool OutputLogRecordsToOVDS
        {
            get => outputLogRecordsToOVDS;
            set => outputLogRecordsToOVDS = value;
        }

        /// <summary>
        /// Indicates whether <see cref="StatusData"/> objects should be output to the OVDS server.
        /// </summary>
        public bool OutputStatusDataToOVDS
        {
            get => outputStatusDataToOVDS;
            set => outputStatusDataToOVDS = value;
        }

        /// <summary>
        /// The minimum number of seconds to wait between iterations of the process that retrieves batches of <see cref="DbOVDSServerCommand"/>s from the adapter database and sends the commands to the configured OVDS server.
        /// </summary>
        public int OVDSClientWorkerIntervalSeconds
        {
            get => oVDSClientWorkerIntervalSeconds;
            set => oVDSClientWorkerIntervalSeconds = value;
        }

        /// <summary>
        /// The number of OVDS server commands to retrieve from the database at a time for processing. Since the number of commands can quickly grow into the millions, they are processed in batches in order to avoid excessive memory usage and performance implications that would arise if the entire table was loaded into memory.
        /// </summary>
        public static int OVDSServerCommandBatchSize
        {
            get => OVDSServerCmdBatchSize;
        }

        /// <summary>
        /// The URL of the OVDS server to which commands are to be posted. 
        /// </summary>
        public string OVDSServerURL
        {
            get => oVDSServerURL;
            set => oVDSServerURL = value;
        }

        /// <summary>
        /// A string containing the template to be used for the SET command when posting data to the OVDS server. 
        /// </summary>
        public string OVDSSetCommandTemplate
        {
            get => oVDSSetCommandTemplate;
            set => oVDSSetCommandTemplate = value;
        }

        /// <summary>
        /// A string containing the template to be used for the SET command when posting data to the OVDS server for AttributeVSSPathMaps. 
        /// </summary>
        public string OVDSSetCommandTemplateForAttributeVSSPathMaps
        {
            get => oVDSSetCommandTemplateForAttributeVSSPathMaps;
            set => oVDSSetCommandTemplateForAttributeVSSPathMaps = value;
        }

        /// <summary>
        /// The string used in the <see cref="OVDSSetCommandTemplate"/> as the placeholder for the <c>Path</c> value.
        /// </summary>
        public static string PlaceholderStringForPathValue
        {
            get => PlaceholderStringForPath;
        }

        /// <summary>
        /// The string used in the <see cref="OVDSSetCommandTemplate"/> as the placeholder for the <c>Timestamp</c> value.
        /// </summary>
        public static string PlaceholderStringForTimestampValue
        {
            get => PlaceholderStringForTimestamp;
        }

        /// <summary>
        /// The string used in the <see cref="OVDSSetCommandTemplate"/> as the placeholder for the <c>Value</c> value.
        /// </summary>
        public static string PlaceholderStringForValueValue
        {
            get => PlaceholderStringForValue;
        }

        /// <summary>
        /// The string used in the <see cref="OVDSSetCommandTemplate"/> as the placeholder for the <c>VIN</c> value.
        /// </summary>
        public static string PlaceholderStringForVINValue
        {
            get => PlaceholderStringForVIN;
        }

        /// <summary>
        /// Indicates whether OVDS server commands should be generated for <see cref="StatusData"/> objects with <see cref="Diagnostic.Id"/> values mapped in the <see cref="AttributeVSSPathMaps"/> category.
        /// </summary>
        public bool SendAttributeTypeDataToOVDS
        {
            get => sendAttributeTypeDataToOVDS;
            set => sendAttributeTypeDataToOVDS = value;
        }

        /// <summary>
        /// The feed results limit to be used to override the default for <see cref="StatusData"/>s when <see cref="StatusData"/>s are being output as OVDS server commands. Typically, the default is 50,000, but for each <see cref="StatusData"/>, an OVDS server command must be generated, resulting in 50,000 records that must be added to to the OVDSServerCommands table for every 50,000 records that must be added to the StatusDatas table. Handling this many records along with those of other feeds in a single database transaction can have performance implications - so, smaller batches of <see cref="StatusData"/>s should be processed when outputting as OVDS server commands.  
        /// </summary>
        public static int StatusDataFeedResultsLimitWhenOutputtingStatusDataToOVDS
        {
            get => StatusDataFeedResultsLimitWhenOutputtingToOVDS;
        }

        /// <summary>
        /// The VSS path mapping information related to <see cref="StatusData"/> objects. The <see cref="VSSPathMap.GeotabDiagnosticId"/> is used as the Key.
        /// </summary>
        public IDictionary<string, VSSPathMap> StatusDataVSSPathMaps
        {
            get => statusDataVSSPathMaps;
            set => statusDataVSSPathMaps = value;
        }

        /// <summary>
        /// The number of minutes to wait, after writing <see cref="UnmappedDiagnostic"/> information to the log file, before repeating.
        /// </summary>
        public int UnmappedDiagnosticsLogIntervalMinutes
        {
            get => unmappedDiagnosticsLogIntervalMinutes;
            set => unmappedDiagnosticsLogIntervalMinutes = value;
        }

        /// <summary>
        /// The URL of the JSON file containing the mappings between Geotab <see cref="Diagnostic"/> <see cref="Id"/>s and the <see href="https://genivi.github.io/vehicle_signal_specification/">Vehicle Signal Specification</see> paths with which they are associated. 
        /// </summary>
        public string VSSPathMapFileURL
        {
            get => vssPathMapFileURL;
            set => vssPathMapFileURL = value;
        }

        /// <summary>
        /// The number of minutes to wait, after retrieving the VSS path mappings, before checking for updates.
        /// </summary>
        public int VSSPathMapUpdateIntervalMinutes
        {
            get => vssPathMapUpdateIntervalMinutes;
            set => vssPathMapUpdateIntervalMinutes = value;
        }

        /// <summary>
        /// The <see href="https://genivi.github.io/vehicle_signal_specification/">Vehicle Signal Specification</see> version to use.
        /// </summary>
        public string VSSVersion
        {
            get => vssVersion;
            set => vssVersion = value;
        }

        /// <summary>
        /// Populates <see cref="AttributeVSSPathMaps"/>.
        /// </summary>
        /// <param name="configuration">A reference to the appsettings.json file from which to read the mapping information.</param>
        public void LoadAttributeVSSPathMaps(IConfiguration configuration)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            AttributeVSSPathMaps = new Dictionary<string, VSSPathMap>();
            var attributeVSSPathMaps = configuration.GetSection(ArgNameAttributeVSSPathMaps).Get<VSSPathMap[]>();
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
        /// <param name="configuration">A reference to the appsettings.json file from which to read the mapping information.</param>
        public void LoadLogRecordVSSPathMaps(IConfiguration configuration)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            LogRecordVSSPathMaps = new Dictionary<string, VSSPathMap>();
            var logRecordVSSPathMaps = configuration.GetSection(ArgNameLogRecordVSSPathMaps).Get<VSSPathMap[]>();
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
        /// <param name="configuration">A reference to the appsettings.json file from which to read the mapping information.</param>
        public void LoadStatusDataVSSPathMaps(IConfiguration configuration)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            StatusDataVSSPathMaps = new Dictionary<string, VSSPathMap>();
            var statusDataVSSPathMaps = configuration.GetSection(ArgNameStatusDataVSSPathMaps).Get<VSSPathMap[]>();
            foreach (var statusDataVSSPathMap in statusDataVSSPathMaps)
            {
                statusDataVSSPathMap.GeotabObjectType = VSSMappableGeotabObjectTypes.StatusData.ToString();
                statusDataVSSPathMap.GeotabObjectPropertyName = nameof(GeotabObjectFieldNames.Data);
                StatusDataVSSPathMaps.Add(statusDataVSSPathMap.GeotabDiagnosticId, statusDataVSSPathMap);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
