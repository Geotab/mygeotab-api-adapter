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
    }
}
