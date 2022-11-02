using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Configuration.Add_Ons.VSS;
using MyGeotabAPIAdapter.Database.Models.Add_Ons.VSS;
using System.Collections.Generic;

namespace MyGeotabAPIAdapter.Add_Ons.VSS
{
    /// <summary>
    /// Interface for a class that maps between various <see cref="Geotab.Checkmate.ObjectModel.Entity"/> objects and <see cref="MyGeotabAPIAdapter.Database.Models.Add_Ons.VSS"/> objects related specifically to the VSS Add-On.
    /// </summary>
    internal interface IVSSObjectMapper
    {
        /// <summary>
        /// The minimum length for a Vehicle Identifictaion Number (VIN).
        /// </summary>
        int MinimumLengthForVIN { get; }

        /// <summary>
        /// Converts the supplied <see cref="DbOVDSServerCommand"/> into a <see cref="DbFailedOVDSServerCommand"/>.
        /// </summary>
        /// <param name="dbOVDSServerCommand">The <see cref="DbOVDSServerCommand"/> to be converted.</param>
        /// <param name="failureMessage">A message indicating the reason why processing of the OVDS server command failed.</param>
        /// <returns></returns>
        DbFailedOVDSServerCommand GetDbFailedOVDSServerCommand(DbOVDSServerCommand dbOVDSServerCommand, string failureMessage);

        /// <summary>
        /// Converts the supplied list of <see cref="LogRecord"/> objects into a list of <see cref="DbOVDSServerCommand"/> objects.
        /// </summary>
        /// <param name="logRecords">The list of <see cref="LogRecord"/> objects to be converted.</param>
        /// <returns></returns>
        List<DbOVDSServerCommand> GetDbOVDSServerSetCommands(List<LogRecord> logRecords);

        /// <summary>
        /// Converts the supplied list of <see cref="StatusData"/> objects into a list of <see cref="DbOVDSServerCommand"/> objects.
        /// </summary>
        /// <param name="statusDatas">The list of <see cref="StatusData"/> objects to be converted.</param>
        /// <returns></returns>
        List<DbOVDSServerCommand> GetDbOVDSServerSetCommands(List<StatusData> statusDatas);

        /// <summary>
        /// Formats the <paramref name="vssValue"/> based on the <paramref name="vssDataType"/> and returns the formatted value as a string.
        /// </summary>
        /// <param name="vssValue">The value to be formatted.</param>
        /// <param name="vssDataType">The <see cref="VSSDataType"/> defining the format to be applied to <paramref name="vssValue"/>.</param>
        /// <returns></returns>
        string GetFormattedVssValue(string vssValue, VSSDataType vssDataType);

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
        string GetOVDSSetCommand(string ovdsSetCommandTemplate, string vinPlaceholder, string pathPlaceholder, string valuePlaceholder, string timestampPlaceholder, string vin, string path, string value, string timestamp);

        /// <summary>
        /// Hydrates the <paramref name="device"/>, converts it to the specific type of <see cref="Device"/> and then returns its VIN. If the VIN cannot be obtained due to <see cref="Device.DeviceType"/> or VIN not being populated, an empty string is returned.
        /// </summary>
        /// <param name="device">The <see cref="Device"/> from which to obtain the VIN.</param>
        /// <returns></returns>
        string GetVIN(Device device);
    }
}
