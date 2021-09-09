using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Microsoft.CSharp.RuntimeBinder;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MyGeotabAPIAdapter.Add_Ons.VSS
{
    /// <summary>
    /// Maps between various <see cref="Geotab.Checkmate.ObjectModel.Entity"/> objects and <see cref="MyGeotabAPIAdapter.Database.Models"/> objects related specifically to the VSS Add-On.
    /// </summary>
    static class VSSObjectMapper
    {
        const string ISO8601DateTimeFormatString = "yyyy-MM-ddThh:mm:ssZ";
        public const int MinimumLengthForVIN = 17;

        /// <summary>
        /// Converts the supplied <see cref="DbOVDSServerCommand"/> into a <see cref="DbFailedOVDSServerCommand"/>.
        /// </summary>
        /// <param name="dbOVDSServerCommand">The <see cref="DbOVDSServerCommand"/> to be converted.</param>
        /// <param name="failureMessage">A message indicating the reason why processing of the OVDS server command failed.</param>
        /// <returns></returns>
        public static DbFailedOVDSServerCommand GetDbFailedOVDSServerCommand(DbOVDSServerCommand dbOVDSServerCommand, string failureMessage)
        {
            DbFailedOVDSServerCommand dbFailedOVDSServerCommand = new()
            {
                Command = dbOVDSServerCommand.Command,
                OVDSServerCommandId = dbOVDSServerCommand.id,
                FailureMessage = failureMessage
            };
            return dbFailedOVDSServerCommand;
        }

        /// <summary>
        /// Converts the supplied list of <see cref="LogRecord"/> objects into a list of <see cref="DbOVDSServerCommand"/> objects.
        /// </summary>
        /// <param name="logRecords">The list of <see cref="LogRecord"/> objects to be converted.</param>
        /// <param name="vssConfiguration">The <see cref="VSSConfiguration"/> from which to obtain needed configuration settings.</param>
        /// <param name="cacheManager">The <see cref="CacheManager"/> to leverage for obtaining VIN information for <see cref="Device"/>s.</param>
        /// <returns></returns>
        public static List<DbOVDSServerCommand> GetDbOVDSServerSetCommands(IList<LogRecord> logRecords, VSSConfiguration vssConfiguration, CacheManager cacheManager)
        {
            var dbOVDSServerCommands = new List<DbOVDSServerCommand>();
            DbOVDSServerCommand dbOVDSServerCommand;
            foreach (var logRecord in logRecords)
            {
                // Get the VIN from the Device associated with the LogRecord. If no VIN is associated with the Device, do not generate OVDS SET command(s), since VIN is a crucial identifier.
                string deviceVIN = GetVIN(cacheManager, logRecord.Device);
                if (deviceVIN.Length < MinimumLengthForVIN)
                {
                    continue;
                }

                // Generate a DbOVDSServerCommand for each of the LogRecord properties that are to be sent to the OVDS server. 
                foreach (var vssPathMap in vssConfiguration.LogRecordVSSPathMaps.Values)
                {
                    string vssValue = vssPathMap.GeotabObjectPropertyName switch
                    {
                        nameof(VSSConfiguration.GeotabObjectFieldNames.Latitude) => logRecord.Latitude.ToString(),
                        nameof(VSSConfiguration.GeotabObjectFieldNames.Longitude) => logRecord.Longitude.ToString(),
                        nameof(VSSConfiguration.GeotabObjectFieldNames.Speed) => logRecord.Speed.ToString(),
                        _ => throw new NotImplementedException($"Support for the '{vssPathMap.GeotabObjectPropertyName}' property has not been implemented."),
                    };

                    var vssValueFormatted = GetFormattedVssValue(vssValue, vssPathMap.VSSDataType);

                    // Generate a DbOVDSServerCommand and add it to the list.
                    dbOVDSServerCommand = new DbOVDSServerCommand
                    {
                        Command = GetOVDSSetCommand(vssConfiguration.OVDSSetCommandTemplate, VSSConfiguration.PlaceholderStringForVINValue, VSSConfiguration.PlaceholderStringForPathValue, VSSConfiguration.PlaceholderStringForValueValue, VSSConfiguration.PlaceholderStringForTimestampValue, deviceVIN, vssPathMap.VSSPath, vssValueFormatted, logRecord.DateTime.GetValueOrDefault().ToString(ISO8601DateTimeFormatString)),
                        RecordCreationTimeUtc = DateTime.UtcNow
                    };
                    dbOVDSServerCommands.Add(dbOVDSServerCommand);
                }
            }
            return dbOVDSServerCommands;
        }

        /// <summary>
        /// Converts the supplied list of <see cref="StatusData"/> objects into a list of <see cref="DbOVDSServerCommand"/> objects.
        /// </summary>
        /// <param name="statusDatas">The list of <see cref="StatusData"/> objects to be converted.</param>
        /// <param name="vssConfiguration">The <see cref="VSSConfiguration"/> from which to obtain needed configuration settings.</param>
        /// <param name="cacheManager">The <see cref="CacheManager"/> to leverage for obtaining VIN information for <see cref="Device"/>s.</param>
        /// <param name="unmappedDiagnosticManager">The <see cref="UnmappedDiagnosticManager"/> to use if <see cref="UnmappedDiagnostic"/>s are being logged.</param>
        /// <returns></returns>
        public static List<DbOVDSServerCommand> GetDbOVDSServerSetCommands(IList<StatusData> statusDatas, VSSConfiguration vssConfiguration, CacheManager cacheManager, UnmappedDiagnosticManager unmappedDiagnosticManager = null)
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
                    string deviceVIN = GetVIN(cacheManager, statusData.Device);
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
                        Command = GetOVDSSetCommand(ovdsSetCommandTemplate, VSSConfiguration.PlaceholderStringForVINValue, VSSConfiguration.PlaceholderStringForPathValue, VSSConfiguration.PlaceholderStringForValueValue, VSSConfiguration.PlaceholderStringForTimestampValue, deviceVIN, targetStatusDataVSSPathMap.VSSPath, vssValueFormatted, statusData.DateTime.GetValueOrDefault().ToString(ISO8601DateTimeFormatString)),
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

        /// <summary>
        /// Formats the <paramref name="vssValue"/> based on the <paramref name="vssDataType"/> and returns the formatted value as a string.
        /// </summary>
        /// <param name="vssValue">The value to be formatted.</param>
        /// <param name="vssDataType">The <see cref="VSSDataType"/> defining the format to be applied to <paramref name="vssValue"/>.</param>
        /// <returns></returns>
        static string GetFormattedVssValue(string vssValue, VSSDataType vssDataType)
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
                    formattedVssValue = vssValueBool.ToString();
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


        /// <summary>
        /// Generates a fully-populated OVDS server SET command using the supplied inputs. 
        /// </summary>
        /// <param name="ovdsSetCommandTemplate">The OVDS server SET command template.</param>
        /// <param name="vinPlaceholder">The placeholder value used in the <paramref name="ovdsSetCommandTemplate"/> to mark the <c>VIN</c> property.</param>
        /// <param name="pathPlaceholder">The placeholder value used in the <paramref name="ovdsSetCommandTemplate"/> to mark the <c>Path</c> property.</param>
        /// <param name="valuePlaceholder">The placeholder value used in the <paramref name="ovdsSetCommandTemplate"/> to mark the <c>Value</c> property.</param>
        /// <param name="timestampPlaceholder">The placeholder value used in the <paramref name="ovdsSetCommandTemplate"/> to mark the <c>Timestamp</c> property.</param>
        /// <param name="vin">The value to be used to replace the <paramref name="vinPlaceholder"/> in the <paramref name="ovdsSetCommandTemplate"/> when generating the OVDS server SET command.</param>
        /// <param name="path">The value to be used to replace the <paramref name="pathPlaceholder"/> in the <paramref name="ovdsSetCommandTemplate"/> when generating the OVDS server SET command.</param>
        /// <param name="value">The value to be used to replace the <paramref name="valuePlaceholder"/> in the <paramref name="ovdsSetCommandTemplate"/> when generating the OVDS server SET command.</param>
        /// <param name="timestamp">The value to be used to replace the <paramref name="timestampPlaceholder"/> in the <paramref name="ovdsSetCommandTemplate"/> when generating the OVDS server SET command.</param>
        /// <returns></returns>
        public static string GetOVDSSetCommand(string ovdsSetCommandTemplate, string vinPlaceholder, string pathPlaceholder, string valuePlaceholder, string timestampPlaceholder, string vin, string path, string value, string timestamp)
        {
            string ovdsSetCommand = ovdsSetCommandTemplate;
            ovdsSetCommand = ovdsSetCommand.Replace(vinPlaceholder, vin);
            ovdsSetCommand = ovdsSetCommand.Replace(pathPlaceholder, path);
            ovdsSetCommand = ovdsSetCommand.Replace(valuePlaceholder, value);
            ovdsSetCommand = ovdsSetCommand.Replace(timestampPlaceholder, timestamp);
            return ovdsSetCommand;
        }

        /// <summary>
        /// Uses the <paramref name="cacheManager"/> to hydrate the <paramref name="device"/>, then converts the <paramref name="device"/> to the specific type of <see cref="Device"/> and returns its VIN. If the VIN cannot be obtained due to <see cref="Device.DeviceType"/> or VIN not being populated, an empty string is returned.
        /// </summary>
        /// <param name="cacheManager">The <see cref="CacheManager"/> to be used to hydrate the <paramref name="device"/>.</param>
        /// <param name="device">The <see cref="Device"/> from which to obtain the VIN.</param>
        /// <returns></returns>
        public static string GetVIN(CacheManager cacheManager, Device device)
        {
            string vin = "";
            var hydratedDevice = CacheManager.HydrateDevice(device);
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
