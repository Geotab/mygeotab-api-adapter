using Microsoft.Extensions.Configuration;
using System;

namespace MyGeotabAPIAdapter.Configuration
{
    /// <summary>
    /// Interface for a helper class that assists in reading/processing application configuration settings (from appsettings.json).
    /// </summary>
    public interface IConfigurationHelper
    {
        /// <summary>
        /// Parses the appsettings.json configuration file for the boolean value associated with the key and section provided. 
        /// If no section is provided then the key is searched at the root level.
        /// </summary>
        /// <param name="keyString">Configuration key value</param>
        /// <param name="sectionString">Configuration section value</param>
        /// <param name="isMasked">Whether the return value should be masked or not in the log file output</param>
        /// <returns>Boolean value associated with the key & section submitted.</returns>
        bool GetConfigKeyValueBoolean(string keyString, string sectionString = "", bool isMasked = false);

        /// <summary>
        /// Parses the appsettings.json configuration file for the DateTime value associated with the key and section provided. 
        /// If no section is provided then the key is searched at the root level.
        /// </summary>
        /// <param name="keyString">Configuration key value</param>
        /// <param name="sectionString">Configuration section value</param>
        /// <param name="isMasked">Whether the return value should be masked or not in the log file output</param>
        /// <returns>DateTime value associated with the key & section submitted.</returns>
        DateTime GetConfigKeyValueDateTime(string keyString, string sectionString = "", bool isMasked = false);

        /// <summary>
        /// Parses the appsettings.json configuration file for the integer value associated with the key and section provided. 
        /// If no section is provided then the key is searched at the root level.
        /// </summary>
        /// <param name="keyString">Configuration key value</param>
        /// <param name="sectionString">Configuration section value</param>
        /// <param name="isMasked">Whether the return value should be masked or not in the log file output</param>
        /// <param name="minimumAllowedValue">Minimum value allowed</param>
        /// <param name="maximumAllowedValue">Maximum value allowed</param>
        /// <param name="defaultValueIfOutsideRange">Value to return if the minimum and maximum boundaries are exceeded</param>
        /// <returns>Integer value associated with the key & section submitted.</returns>
        int GetConfigKeyValueInt(string keyString, string sectionString = "", bool isMasked = false, int minimumAllowedValue = int.MinValue, int maximumAllowedValue = int.MaxValue, int defaultValueIfOutsideRange = int.MinValue);

        /// <summary>
        /// Parses the appsettings.json configuration file for the string value associated with the key and section provided. 
        /// If no section is provided then the key is searched at the root level.
        /// </summary>
        /// <param name="keyString">Configuration key value</param>
        /// <param name="sectionString">Configuration section value</param>
        /// <param name="isLogged">Enable/Disable logging</param>
        /// <param name="isMasked">Whether the return value should be masked or not in the log file output</param>
        /// <returns>String value associated with the key and section submitted.</returns>
        string GetConfigKeyValueString(string keyString, string sectionString = "", bool isLogged = true, bool isMasked = false);

        /// <summary>
        /// Parses the appsettings.json configuration file for the boolean value associated with the key and section provided,
        /// returning <paramref name="defaultValue"/> if the key is absent instead of throwing. Intended for keys introduced
        /// after a release, where an existing user's configuration file will not yet contain them. A key that is present but
        /// cannot be parsed - an empty value included - is still treated as an error.
        /// </summary>
        /// <param name="keyString">Configuration key value</param>
        /// <param name="defaultValue">Value to return if the key is not present in the configuration file</param>
        /// <param name="sectionString">Configuration section value</param>
        /// <param name="isMasked">Whether the return value should be masked or not in the log file output</param>
        /// <returns>Boolean value associated with the key &amp; section submitted, or <paramref name="defaultValue"/> if absent.</returns>
        bool GetConfigKeyValueBooleanOrDefault(string keyString, bool defaultValue, string sectionString = "", bool isMasked = false);

        /// <summary>
        /// Parses the appsettings.json configuration file for the integer value associated with the key and section provided,
        /// returning <paramref name="defaultValue"/> if the key is absent instead of throwing. Intended for keys introduced
        /// after a release, where an existing user's configuration file will not yet contain them. A key that is present but
        /// cannot be parsed - an empty value included - is still treated as an error; one that falls outside the allowed range is replaced with
        /// <paramref name="defaultValue"/> and a warning is logged.
        /// </summary>
        /// <param name="keyString">Configuration key value</param>
        /// <param name="defaultValue">Value to return if the key is not present in the configuration file, or if its value falls outside the allowed range</param>
        /// <param name="sectionString">Configuration section value</param>
        /// <param name="isMasked">Whether the return value should be masked or not in the log file output</param>
        /// <param name="minimumAllowedValue">Minimum value allowed</param>
        /// <param name="maximumAllowedValue">Maximum value allowed</param>
        /// <returns>Integer value associated with the key &amp; section submitted, or <paramref name="defaultValue"/> if absent.</returns>
        int GetConfigKeyValueIntOrDefault(string keyString, int defaultValue, string sectionString = "", bool isMasked = false, int minimumAllowedValue = int.MinValue, int maximumAllowedValue = int.MaxValue);

        /// <summary>
        /// Parses the appsettings.json configuration file for the string value associated with the key and section provided,
        /// returning <paramref name="defaultValue"/> if the key is absent or empty instead of throwing. Intended for keys
        /// introduced after a release, where an existing user's configuration file will not yet contain them.
        /// </summary>
        /// <param name="keyString">Configuration key value</param>
        /// <param name="defaultValue">Value to return if the key is not present in the configuration file</param>
        /// <param name="sectionString">Configuration section value</param>
        /// <param name="isLogged">Enable/Disable logging</param>
        /// <param name="isMasked">Whether the return value should be masked or not in the log file output</param>
        /// <returns>String value associated with the key and section submitted, or <paramref name="defaultValue"/> if absent.</returns>
        string GetConfigKeyValueStringOrDefault(string keyString, string defaultValue, string sectionString = "", bool isLogged = true, bool isMasked = false);

        /// <summary>
        /// Parses the appsettings.json configuration file for the section provided.
        /// </summary>
        /// <param name="sectionString">Value of the configuration section to be returned.</param>
        /// <returns></returns>
        IConfigurationSection GetConfigSection(string sectionString);

        /// <summary>
        /// Replaces the default <see cref="IConfiguration"/> used by the current <see cref="IConfigurationHelper"/> implementation with the supplied <paramref name="configuration"/>.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/> to be used by the current <see cref="IConfigurationHelper"/> implementation.</param>
        void SetConfiguration(IConfiguration configuration);
    }
}
