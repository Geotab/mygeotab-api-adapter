using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Configuration.Add_Ons.VSS
{
    /// <summary>
    /// Interface for a class that reads (from appsettings.json) and stores application configuration settings related specifically to the VSS Add-On. Intended for association with the MyGeotabAPIAdapter project.
    /// </summary>
    public interface IVSSConfiguration
    {
        /// <summary>
        /// The path of the "AttributeVSSPathMaps" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameAttributeVSSPathMaps { get; }

        /// <summary>
        /// The path of the "DisableCorrespondingAdapterOutput" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameDisableCorrespondingAdapterOutput { get; }

        /// <summary>
        /// The path of the "EnableVSSAddOn" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameEnableVSSAddOn { get; }

        /// <summary>
        /// The path of the "LogRecordVSSPathMaps" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameLogRecordVSSPathMaps { get; }

        /// <summary>
        /// The path of the "OutputLogRecordsToOVDS" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameOutputLogRecordsToOVDS { get; }

        /// <summary>
        /// The path of the "LogUnmappedDiagnostics" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameLogUnmappedDiagnostics { get; }

        /// <summary>
        /// The path of the "OutputStatusDataToOVDS" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameOutputStatusDataToOVDS { get; }

        /// <summary>
        /// The path of the "OVDSClientWorkerIntervalSeconds" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameOVDSClientWorkerIntervalSeconds { get; }

        /// <summary>
        /// The path of the "OVDSServerURL" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameOVDSServerURL { get; }

        /// <summary>
        /// The path of the "OVDSSetCommandTemplate" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameOVDSSetCommandTemplate { get; }

        /// <summary>
        /// The path of the "OVDSSetCommandTemplateForAttributeVSSPathMaps" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameOVDSSetCommandTemplateForAttributeVSSPathMaps { get; }

        /// <summary>
        /// The path of the "SendAttributeTypeDataToOVDS" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameSendAttributeTypeDataToOVDS { get; }

        /// <summary>
        /// The path of the "StatusDataVSSPathMaps" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameStatusDataVSSPathMaps { get; }

        /// <summary>
        /// The path of the "UnmappedDiagnosticsLogIntervalMinutes" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameUnmappedDiagnosticsLogIntervalMinutes { get; }

        /// <summary>
        /// The path of the "VSSPathMapFileURL" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameVSSPathMapFileURL { get; }

        /// <summary>
        /// The path of the "VSSPathMapUpdateIntervalMinutes" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameVSSPathMapUpdateIntervalMinutes { get; }

        /// <summary>
        /// The path of the "VSSVersion" setting in the appsettings.json file. 
        /// </summary>
        string ArgNameVSSVersion { get; }

        /// <summary>
        /// The VSS path mapping information related to 'Attribute' <see cref="StatusData"/> objects. The <see cref="VSSPathMap.GeotabDiagnosticId"/> is used as the Key.
        /// </summary>
        IDictionary<string, VSSPathMap> AttributeVSSPathMaps { get; }

        /// <summary>
        /// Indicates whether corresponding MyGeotab API Adapter functionality should be disabled. For example, if set to <c>true</c>, when outputting LogRecords to OVDS, LogRecords will not be written to the LogRecords table in the adapter database.
        /// </summary>
        bool DisableCorrespondingAdapterOutput { get; }

        /// <summary>
        /// Indicates whether the VSS Add-On functionality should be enabled.
        /// </summary>
        bool EnableVSSAddOn { get; }

        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Indicates whether the <see cref="InitializeAsync(string, string)"/> method has been invoked since the current class instance was created.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// The feed results limit to be used to override the default for <see cref="LogRecord"/>s when <see cref="LogRecord"/>s are being output as OVDS server commands. Typically, the default is 50,000, but for each <see cref="LogRecord"/>, three OVDS server commands must be generated, resulting in 150,000 records that must be added to to the OVDSServerCommands table for every 50,000 records that must be added to the LogRecords table. Handling this many records along with those of other feeds in a single database transaction can have performance implications - so, smaller batches of <see cref="LogRecord"/>s should be processed when outputting as OVDS server commands.  
        /// </summary>
        int LogRecordFeedResultsLimitWhenOutputtingLogRecordsToOVDS { get; }

        /// <summary>
        /// The VSS path mapping information related to <see cref="LogRecord"/> objects. The <see cref="VSSPathMap.GeotabObjectPropertyName"/> is used as the Key.
        /// </summary>
        IDictionary<string, VSSPathMap> LogRecordVSSPathMaps { get; }

        /// <summary>
        /// Indicates whether <see cref="UnmappedDiagnostic"/>s should be written to the log file periodically.
        /// </summary>
        bool LogUnmappedDiagnostics { get; }

        /// <summary>
        /// Indicates whether <see cref="LogRecord"/> objects should be output to the OVDS server.
        /// </summary>
        bool OutputLogRecordsToOVDS { get; }

        /// <summary>
        /// Indicates whether <see cref="StatusData"/> objects should be output to the OVDS server.
        /// </summary>
        bool OutputStatusDataToOVDS { get; }

        /// <summary>
        /// The minimum number of seconds to wait between iterations of the process that retrieves batches of <see cref="DbOVDSServerCommand"/>s from the adapter database and sends the commands to the configured OVDS server.
        /// </summary>
        int OVDSClientWorkerIntervalSeconds { get; }

        /// <summary>
        /// The number of OVDS server commands to retrieve from the database at a time for processing. Since the number of commands can quickly grow into the millions, they are processed in batches in order to avoid excessive memory usage and performance implications that would arise if the entire table was loaded into memory.
        /// </summary>
        int OVDSServerCommandBatchSize { get; }

        /// <summary>
        /// The URL of the OVDS server to which commands are to be posted. 
        /// </summary>
        string OVDSServerURL { get; }

        /// <summary>
        /// A string containing the template to be used for the SET command when posting data to the OVDS server. 
        /// </summary>
        string OVDSSetCommandTemplate { get; }

        /// <summary>
        /// A string containing the template to be used for the SET command when posting data to the OVDS server for AttributeVSSPathMaps. 
        /// </summary>
        string OVDSSetCommandTemplateForAttributeVSSPathMaps { get; }

        /// <summary>
        /// The string used in the <see cref="OVDSSetCommandTemplate"/> as the placeholder for the <c>Path</c> value.
        /// </summary>
        string PlaceholderStringForPathValue { get; }

        /// <summary>
        /// The string used in the <see cref="OVDSSetCommandTemplate"/> as the placeholder for the <c>Timestamp</c> value.
        /// </summary>
        string PlaceholderStringForTimestampValue { get; }

        /// <summary>
        /// The string used in the <see cref="OVDSSetCommandTemplate"/> as the placeholder for the <c>Value</c> value.
        /// </summary>
        string PlaceholderStringForValueValue { get; }

        /// <summary>
        /// The string used in the <see cref="OVDSSetCommandTemplate"/> as the placeholder for the <c>VIN</c> value.
        /// </summary>
        string PlaceholderStringForVINValue { get; }

        /// <summary>
        /// Indicates whether OVDS server commands should be generated for <see cref="StatusData"/> objects with <see cref="Diagnostic.Id"/> values mapped in the <see cref="AttributeVSSPathMaps"/> category.
        /// </summary>
        bool SendAttributeTypeDataToOVDS { get; }

        /// <summary>
        /// The feed results limit to be used to override the default for <see cref="StatusData"/>s when <see cref="StatusData"/>s are being output as OVDS server commands. Typically, the default is 50,000, but for each <see cref="StatusData"/>, an OVDS server command must be generated, resulting in 50,000 records that must be added to to the OVDSServerCommands table for every 50,000 records that must be added to the StatusDatas table. Handling this many records along with those of other feeds in a single database transaction can have performance implications - so, smaller batches of <see cref="StatusData"/>s should be processed when outputting as OVDS server commands.  
        /// </summary>
        int StatusDataFeedResultsLimitWhenOutputtingStatusDataToOVDS { get; }

        /// <summary>
        /// The VSS path mapping information related to <see cref="StatusData"/> objects. The <see cref="VSSPathMap.GeotabDiagnosticId"/> is used as the Key.
        /// </summary>
        IDictionary<string, VSSPathMap> StatusDataVSSPathMaps { get; }

        /// <summary>
        /// The number of minutes to wait, after writing <see cref="UnmappedDiagnostic"/> information to the log file, before repeating.
        /// </summary>
        int UnmappedDiagnosticsLogIntervalMinutes { get; }

        /// <summary>
        /// A constant value indicating the file extension of the VSSPathMaps file name.
        /// </summary>
        string URLJSONFileExtension { get => ".json"; }

        /// <summary>
        /// A constant value indicating the version delimiter of the VSSPathMaps file name.
        /// </summary>
        string URLVSSVersionDelimiter { get => "-v"; }

        /// <summary>
        /// A constant value indicating the base VSSPathMaps file name (without version indicator applied).
        /// </summary>
        string VSSPathMapFileName { get => "VSSPathMaps.json"; }

        /// <summary>
        /// The URL of the JSON file containing the mappings between Geotab <see cref="Diagnostic"/> <see cref="Id"/>s and the <see href="https://genivi.github.io/vehicle_signal_specification/">Vehicle Signal Specification</see> paths with which they are associated. 
        /// </summary>
        string VSSPathMapFileURL { get; }

        /// <summary>
        /// A constant value indicating the name of the temporary VSSPathMaps file (for download purposes).
        /// </summary>
        string VSSPathMapTempFileName { get => "DOWNLOAD-VSSPathMaps.json"; }

        /// <summary>
        /// The number of minutes to wait, after retrieving the VSS path mappings, before checking for updates.
        /// </summary>
        int VSSPathMapUpdateIntervalMinutes { get; }

        /// <summary>
        /// The <see href="https://genivi.github.io/vehicle_signal_specification/">Vehicle Signal Specification</see> version to use.
        /// </summary>
        string VSSVersion { get; }

        /// <summary>
        /// Determines which of the <see cref="VSSOutputOptions"/> is to be used, in relation to LogRecords, based on the combination of configured values in appsettings.json.
        /// </summary>
        VSSOutputOptions GetVSSOutputOptionForLogRecords();

        /// <summary>
        /// Determines which of the <see cref="VSSOutputOptions"/> is to be used, in relation to StatusData, based on the combination of configured values in appsettings.json.
        /// </summary>
        VSSOutputOptions GetVSSOutputOptionForStatusData();

        /// <summary>
        /// If not already initialized, initializes the current <see cref="IVSSConfiguration"/> instance. Builds the VSS-related <see cref="IConfiguration"/> using the supplied <paramref name="vssPathMapFileDirectory"/> and <paramref name="vssPathMapFileName"/> values and then calls the <see cref="IConfigurationHelper.SetConfiguration(IConfiguration)"/> method of the internal <see cref="IConfigurationHelper"/> to supply the <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="vssPathMapFileDirectory">The directory of the VSSPathMap file.</param>
        /// <param name="vssPathMapFileName">The name of the VSSPathMap file.</param>
        /// <returns></returns>
        Task InitializeAsync(string vssPathMapFileDirectory, string vssPathMapFileName);

        /// <summary>
        /// Validates the configuration object values and uses those values to set properties of the <see cref="IVSSConfiguration"/>.
        /// </summary>
        void ProcessConfigItems();

        /// <summary>
        /// Reloads the LogRecordVSSPathMaps and StatusDataVSSPathMaps from appsettings.json and updates <see cref="vssConfiguration"/> accordingly so that changes can be made to the VSS path maps while the application is running. Only performs this activity if <see cref="VSSPathMapUpdateIntervalMinutes"/> has elapsed since the last time this activity was performed.
        /// </summary>
        Task UpdateVSSPathMapsAsync();
    }
}
