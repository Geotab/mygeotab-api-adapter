using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Microsoft.CSharp.RuntimeBinder;
using MyGeotabAPIAdapter.Configuration.Add_Ons.VSS;
using MyGeotabAPIAdapter.Database.Models.Add_Ons.VSS;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace MyGeotabAPIAdapter.Add_Ons.VSS
{
    /// <summary>
    /// A class that maps between various <see cref="Entity"/> objects and <see cref="Database.Models.Add_Ons.VSS"/> objects related specifically to the VSS Add-On.
    /// </summary>
    internal class VSSObjectMapper : IVSSObjectMapper
    {
        const string ISO8601DateTimeFormatString = "yyyy-MM-ddThh:mm:ssZ";
        const int MinLengthForVIN = 17;

        readonly IGenericGeotabObjectHydrator<Device> deviceGeotabObjectHydrator;
        readonly IUnmappedDiagnosticManager unmappedDiagnosticManager;
        readonly IVSSConfiguration vssConfiguration;

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public int MinimumLengthForVIN
        {
            get => MinLengthForVIN;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VSSObjectMapper"/> class.
        /// </summary>
        public VSSObjectMapper(IGenericGeotabObjectHydrator<Device> deviceGeotabObjectHydrator, IUnmappedDiagnosticManager unmappedDiagnosticManager, IVSSConfiguration vssConfiguration)
        {
            this.deviceGeotabObjectHydrator = deviceGeotabObjectHydrator;
            this.unmappedDiagnosticManager = unmappedDiagnosticManager;
            this.vssConfiguration = vssConfiguration;
        }

        /// <inheritdoc/>
        public DbFailedOVDSServerCommand GetDbFailedOVDSServerCommand(DbOVDSServerCommand dbOVDSServerCommand, string failureMessage)
        {
            DbFailedOVDSServerCommand dbFailedOVDSServerCommand = new()
            {
                Command = dbOVDSServerCommand.Command,
                OVDSServerCommandId = dbOVDSServerCommand.id,
                FailureMessage = failureMessage,
                RecordCreationTimeUtc = DateTime.UtcNow,
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert
            };
            return dbFailedOVDSServerCommand;
        }

        /// <inheritdoc/>
        public List<DbOVDSServerCommand> GetDbOVDSServerSetCommands(List<LogRecord> logRecords)
        {
            var dbOVDSServerCommands = new List<DbOVDSServerCommand>();
            DbOVDSServerCommand dbOVDSServerCommand;
            foreach (var logRecord in logRecords)
            {
                // Get the VIN from the Device associated with the LogRecord. If no VIN is associated with the Device, do not generate OVDS SET command(s), since VIN is a crucial identifier.
                string deviceVIN = GetVIN(logRecord.Device);
                if (deviceVIN.Length < MinimumLengthForVIN)
                {
                    continue;
                }

                // Generate a DbOVDSServerCommand for each of the LogRecord properties that are to be sent to the OVDS server. 
                foreach (var vssPathMap in vssConfiguration.LogRecordVSSPathMaps.Values)
                {
                    string vssValue = vssPathMap.GeotabObjectPropertyName switch
                    {
                        nameof(GeotabObjectFieldNames.Latitude) => logRecord.Latitude.ToString(),
                        nameof(GeotabObjectFieldNames.Longitude) => logRecord.Longitude.ToString(),
                        nameof(GeotabObjectFieldNames.Speed) => logRecord.Speed.ToString(),
                        _ => throw new NotImplementedException($"Support for the '{vssPathMap.GeotabObjectPropertyName}' property has not been implemented."),
                    };

                    var vssValueFormatted = GetFormattedVssValue(vssValue, vssPathMap.VSSDataType);

                    // Generate a DbOVDSServerCommand and add it to the list.
                    dbOVDSServerCommand = new DbOVDSServerCommand
                    {
                        Command = GetOVDSSetCommand(vssConfiguration.OVDSSetCommandTemplate, vssConfiguration.PlaceholderStringForVINValue, vssConfiguration.PlaceholderStringForPathValue, vssConfiguration.PlaceholderStringForValueValue, vssConfiguration.PlaceholderStringForTimestampValue, deviceVIN, vssPathMap.VSSPath, vssValueFormatted, logRecord.DateTime.GetValueOrDefault().ToString(ISO8601DateTimeFormatString)),
                        DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                        RecordCreationTimeUtc = DateTime.UtcNow
                    };
                    dbOVDSServerCommands.Add(dbOVDSServerCommand);
                }
            }
            return dbOVDSServerCommands;
        }

        /// <inheritdoc/>
        public List<DbOVDSServerCommand> GetDbOVDSServerSetCommands(List<StatusData> statusDatas)
        {
            var dbOVDSServerCommands = new List<DbOVDSServerCommand>();
            DbOVDSServerCommand dbOVDSServerCommand;
            foreach (var statusData in statusDatas)
            {
                // Get the Diagnostic Id associated with the StatusData and only proceed if there is a VSS path mapped for the subject Diagnostic.
                var statusDataDiagnostic = statusData.Diagnostic;
                if (statusDataDiagnostic == null || statusDataDiagnostic is NoDiagnostic)
                {
                    continue;
                }

                // Try to get the VSSPathMap. Both 'StatusDataVSSPathMaps' and 'AttributeVSSPathMaps' represent StatusData, but the latter require that no timestamp be included in the OVDS Server Set command. Additionally, 'AttributeVSSPathMaps' should only be processed if the SendAttributeTypeDataToOVDS parameter in the VSSConfiguration is set to true.
                bool isAttributeVSSPathMap = false;
                VSSPathMap targetStatusDataVSSPathMap = null;
                if (vssConfiguration.StatusDataVSSPathMaps.TryGetValue(statusDataDiagnostic.Id.ToString(), out VSSPathMap statusDataVSSPathMap))
                {
                    targetStatusDataVSSPathMap = statusDataVSSPathMap;
                }
                else if (vssConfiguration.SendAttributeTypeDataToOVDS == true && vssConfiguration.AttributeVSSPathMaps.TryGetValue(statusDataDiagnostic.Id.ToString(), out VSSPathMap attributeVSSPathMap))
                {
                    isAttributeVSSPathMap = true;
                    targetStatusDataVSSPathMap = attributeVSSPathMap;
                }

                if (targetStatusDataVSSPathMap != null)
                {
                    // Get the VIN from the Device associated with the StatusData. If no VIN is associated with the Device, do not generate OVDS SET command(s), since VIN is a crucial identifier.
                    string deviceVIN = GetVIN(statusData.Device);
                    if (deviceVIN.Length < MinimumLengthForVIN)
                    {
                        continue;
                    }

                    // Apply the unit conversion multiplier to the StatusData Data value. If the StatusData Data value is null, do not generate an OVDS SET command. Only apply the unit conversion if the unitConversionMultiplier property is set.
                    if (statusData.Data == null)
                    {
                        continue;
                    }
                    var vssValue = Convert.ToDouble(statusData.Data, CultureInfo.CurrentCulture.NumberFormat);
                    if (targetStatusDataVSSPathMap.VSSDataType != VSSDataType.Boolean && targetStatusDataVSSPathMap.UnitConversionMultiplier != 0)
                    {
                        vssValue *= targetStatusDataVSSPathMap.UnitConversionMultiplier;
                    }

                    var vssValueFormatted = GetFormattedVssValue(vssValue.ToString(), targetStatusDataVSSPathMap.VSSDataType);

                    // Generate a DbOVDSServerCommand and add it to the list.
                    var ovdsSetCommandTemplate = vssConfiguration.OVDSSetCommandTemplate;
                    if (isAttributeVSSPathMap == true)
                    {
                        ovdsSetCommandTemplate = vssConfiguration.OVDSSetCommandTemplateForAttributeVSSPathMaps;
                    }

                    dbOVDSServerCommand = new DbOVDSServerCommand
                    {
                        Command = GetOVDSSetCommand(ovdsSetCommandTemplate, vssConfiguration.PlaceholderStringForVINValue, vssConfiguration.PlaceholderStringForPathValue, vssConfiguration.PlaceholderStringForValueValue, vssConfiguration.PlaceholderStringForTimestampValue, deviceVIN, targetStatusDataVSSPathMap.VSSPath, vssValueFormatted, statusData.DateTime.GetValueOrDefault().ToString(ISO8601DateTimeFormatString)),
                        DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                        RecordCreationTimeUtc = DateTime.UtcNow
                    };
                    dbOVDSServerCommands.Add(dbOVDSServerCommand);
                }
                else
                {
                    // The Diagnostic associated with the subject StatusData entity has not been mapped in the VSSPathMaps file. If configured to do so, log the unmapped Diagnostic. 
                    if (vssConfiguration.LogUnmappedDiagnostics == true)
                    {
                        unmappedDiagnosticManager.AddUnmappedDiagnosticToDictionary(statusData);
                    }
                }
            }
            // If logging of unmapped Diagnostics is enabled, write any that have been detected to the log.
            if (vssConfiguration.LogUnmappedDiagnostics == true)
            {
                unmappedDiagnosticManager.LogUnmappedDiagnostics();
            }
            return dbOVDSServerCommands;
        }

        /// <inheritdoc/>
        public string GetFormattedVssValue(string vssValue, VSSDataType vssDataType)
        {
            string formattedVssValue;
            switch (vssDataType)
            {
                case VSSDataType.Boolean:
                    if (vssValue == "-0" || vssValue == "0" || vssValue == "48")
                    {
                        vssValue = "false";
                    }
                    else if (vssValue == "-1" || vssValue == "1" || vssValue == "49")
                    {
                        vssValue = "true";
                    }
                    var vssValueBool = bool.Parse(vssValue);
                    formattedVssValue = vssValueBool.ToString().ToLower();
                    break;
                case VSSDataType.Double:
                    var vssValueDouble = Convert.ToDouble(vssValue, CultureInfo.CurrentCulture.NumberFormat);
                    formattedVssValue = vssValueDouble.ToString();
                    break;
                case VSSDataType.Integer:
                    var vssValDouble = Convert.ToDouble(vssValue, CultureInfo.CurrentCulture.NumberFormat);
                    var vssValueInt = Convert.ToInt32(vssValDouble, CultureInfo.CurrentCulture.NumberFormat);
                    formattedVssValue = vssValueInt.ToString();
                    break;
                default:
                    throw new NotImplementedException($"Support for '{nameof(VSSDataType)}.{vssDataType}' has not been implemented.");
            }

            return formattedVssValue;
        }


        /// <inheritdoc/>
        public string GetOVDSSetCommand(string ovdsSetCommandTemplate, string vinPlaceholder, string pathPlaceholder, string valuePlaceholder, string timestampPlaceholder, string vin, string path, string value, string timestamp)
        {
            string ovdsSetCommand = ovdsSetCommandTemplate;
            ovdsSetCommand = ovdsSetCommand.Replace(vinPlaceholder, vin);
            ovdsSetCommand = ovdsSetCommand.Replace(pathPlaceholder, path);
            ovdsSetCommand = ovdsSetCommand.Replace(valuePlaceholder, value);
            ovdsSetCommand = ovdsSetCommand.Replace(timestampPlaceholder, timestamp);
            return ovdsSetCommand;
        }

        /// <inheritdoc/>
        public string GetVIN(Device device)
        {
            string vin = "";
            var hydratedDevice = deviceGeotabObjectHydrator.HydrateEntity(device, NoDevice.Value);
            if (hydratedDevice is NoDevice)
            {
                return vin;
            }
            dynamic convertedDevice = Convert.ChangeType(hydratedDevice, hydratedDevice.GetType());
            try
            {
                vin = convertedDevice.VehicleIdentificationNumber;
            }
            catch (RuntimeBinderException)
            {
                return vin;
            }
            return vin;
        }
    }
}
