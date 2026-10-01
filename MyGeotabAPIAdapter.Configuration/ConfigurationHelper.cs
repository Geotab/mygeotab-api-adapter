using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Reflection;

namespace MyGeotabAPIAdapter.Configuration
{
    /// <summary>
    /// A helper class that assists in reading/processing application configuration settings (from appsettings.json).
    /// </summary>
    public class ConfigurationHelper : IConfigurationHelper
    {
        const string MaskString = "************";

        IConfiguration configuration;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationHelper"/> class.
        /// </summary>
        public ConfigurationHelper(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <inheritdoc/>
        public bool GetConfigKeyValueBoolean(string keyString, string sectionString = "", bool isMasked = false)
        {
            string valueString = GetConfigKeyValueString(keyString, sectionString, false);
            if (bool.TryParse(valueString, out bool configItemValueBool))
            {
                if (isMasked == true)
                {
                    logger.Info($">{keyString}: {MaskString}");
                }
                else
                {
                    logger.Info($">{keyString}: {configItemValueBool}");
                }
            }
            else
            {
                string errorMessage = $"The value of '{valueString}' provided for the '{keyString}' configuration item is not valid.";
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
            return configItemValueBool;
        }

        /// <inheritdoc/>
        public DateTime GetConfigKeyValueDateTime(string keyString, string sectionString = "", bool isMasked = false)
        {
            string configItemValueString = GetConfigKeyValueString(keyString, sectionString, false);
            if (DateTime.TryParse(configItemValueString, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime configItemValueDateTime))
            {
                if (isMasked == true)
                {
                    logger.Info($">{keyString}: {MaskString}");
                }
                else
                {
                    logger.Info($">{keyString}: {configItemValueString}");
                }
            }
            else
            {
                string errorMessage = $"The value of '{configItemValueString}' provided for the '{keyString}' configuration item is not valid.";
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
            return configItemValueDateTime;
        }

        /// <inheritdoc/>
        public int GetConfigKeyValueInt(string keyString, string sectionString = "", bool isMasked = false, int minimumAllowedValue = int.MinValue, int maximumAllowedValue = int.MaxValue, int defaultValueIfOutsideRange = int.MinValue)
        {
            string valueString = GetConfigKeyValueString(keyString, sectionString, false);

            if (int.TryParse(valueString, out int output))
            {
                // Perform validation.
                if (output < minimumAllowedValue || output > maximumAllowedValue)
                {
                    // Throw an ArgumentException if the supplied minimumAllowedValue is greater than the supplied maximumAllowedValue.
                    if (minimumAllowedValue > maximumAllowedValue)
                    {
                        string errorMessage = $"The value of '{minimumAllowedValue}' provided for the 'minimumAllowedValue' parameter is greater than the value of '{maximumAllowedValue}' provided for the 'maximumAllowedValue' parameter.";
                        logger.Error(errorMessage);
                        throw new ArgumentException(errorMessage);
                    }
                    // Throw an ArgumentException if the supplied defaultValueIfOutsideRange is less than the supplied minimumAllowedValue or greater than the supplied maximumAllowedValue.
                    if (defaultValueIfOutsideRange < minimumAllowedValue || defaultValueIfOutsideRange > maximumAllowedValue)
                    {
                        string errorMessage = $"The value of '{defaultValueIfOutsideRange}' provided for the 'defaultValueIfOutsideRange' is not between the value of '{minimumAllowedValue}' provided for the 'minimumAllowedValue' parameter and the value of '{maximumAllowedValue}' provided for the 'maximumAllowedValue' parameter.";
                        logger.Error(errorMessage);
                        throw new ArgumentException(errorMessage);
                    }
                    // If the value of the subject ConfigItem falls outside the allowed range, use the default value for the ConfigItem instead and log a warning message.
                    if (output < minimumAllowedValue || output > maximumAllowedValue)
                    {
                        string errorMessage = $"The value of '{output}' provided for the '{keyString}' configuration item is is not between the minimum allowed value of '{minimumAllowedValue}' and the maximum allowed value of '{maximumAllowedValue}'. {keyString} will be set to '{defaultValueIfOutsideRange}'.";
                        output = defaultValueIfOutsideRange;
                        logger.Warn(errorMessage);
                    }
                }
                // Log the ConfigItem value.
                if (isMasked == true)
                {
                    logger.Info($">{keyString}: {MaskString}");
                }
                else
                {
                    logger.Info($">{keyString}: {output}");
                }
            }
            else
            {
                string errorMessage = $"The value of '{valueString}' provided for the '{keyString}' configuration item is not valid.";
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
            return output;
        }

        /// <inheritdoc/>
        public string GetConfigKeyValueString(string keyString, string sectionString = "", bool isLogged = true, bool isMasked = false)
        {
            string output;
            if (string.IsNullOrEmpty(sectionString))
            {
                // No section defined.
                output = configuration[keyString];
            }
            else
            {
                // Key defined in a section.
                output = configuration.GetSection(sectionString)[keyString];
            }
            if (string.IsNullOrEmpty(output))
            {
                string errorMessage = $"A required configuration item named '{keyString}' was not found in the configuration file.";
                logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
            if (isLogged == true)
            {
                if (isMasked == true)
                {
                    logger.Info($">{keyString}: {MaskString}");
                }
                else
                {
                    logger.Info($">{keyString}: {output}");
                }
            }
            return output;
        }

        /// <inheritdoc/>
        public bool GetConfigKeyValueBooleanOrDefault(string keyString, bool defaultValue, string sectionString = "", bool isMasked = false)
        {
            string? valueString = GetRawConfigKeyValue(keyString, sectionString);
            if (valueString == null)
            {
                LogDefaultApplied(keyString, defaultValue.ToString(), isMasked);
                return defaultValue;
            }
            ThrowIfPresentButEmpty(keyString, valueString, defaultValue.ToString());
            return GetConfigKeyValueBoolean(keyString, sectionString, isMasked);
        }

        /// <inheritdoc/>
        public int GetConfigKeyValueIntOrDefault(string keyString, int defaultValue, string sectionString = "", bool isMasked = false, int minimumAllowedValue = int.MinValue, int maximumAllowedValue = int.MaxValue)
        {
            string? valueString = GetRawConfigKeyValue(keyString, sectionString);
            if (valueString == null)
            {
                LogDefaultApplied(keyString, defaultValue.ToString(), isMasked);
                return defaultValue;
            }
            ThrowIfPresentButEmpty(keyString, valueString, defaultValue.ToString());
            return GetConfigKeyValueInt(keyString, sectionString, isMasked, minimumAllowedValue, maximumAllowedValue, defaultValue);
        }

        /// <summary>
        /// Refuses a key that is present with an empty value. Only an ABSENT key takes the default: an empty value is a
        /// value that cannot be parsed, like any other. Taking the default for it was easy to misread - an operator who
        /// blanks <c>EnableAutomaticRetention</c> most likely means to turn retention off, and its default deletes
        /// data - and the startup log then said the key had not been found, which was untrue.
        /// </summary>
        static void ThrowIfPresentButEmpty(string keyString, string valueString, string defaultValue)
        {
            if (valueString.Length == 0)
            {
                throw new Exception($"The '{keyString}' setting is present but has no value. Give it a value, or remove the key to use its default ({defaultValue}).");
            }
        }

        /// <inheritdoc/>
        public string GetConfigKeyValueStringOrDefault(string keyString, string defaultValue, string sectionString = "", bool isLogged = true, bool isMasked = false)
        {
            string? valueString = GetRawConfigKeyValue(keyString, sectionString);
            if (string.IsNullOrEmpty(valueString))
            {
                if (isLogged == true)
                {
                    LogDefaultApplied(keyString, defaultValue, isMasked);
                }
                return defaultValue;
            }
            return GetConfigKeyValueString(keyString, sectionString, isLogged, isMasked);
        }

        /// <inheritdoc/>
        public IConfigurationSection GetConfigSection(string sectionString)
        {
            var output = configuration.GetSection(sectionString);
            return output;
        }

        /// <summary>
        /// Retrieves the raw string value associated with the supplied key and section without logging it and without
        /// throwing if it is absent. Used by the *OrDefault methods to distinguish "absent" from "present but invalid".
        /// </summary>
        /// <param name="keyString">Configuration key value</param>
        /// <param name="sectionString">Configuration section value</param>
        /// <returns>The raw configuration value, or <c>null</c> if the key is not present.</returns>
        string? GetRawConfigKeyValue(string keyString, string sectionString)
        {
            if (string.IsNullOrEmpty(sectionString))
            {
                // No section defined.
                return configuration[keyString];
            }
            // Key defined in a section.
            return configuration.GetSection(sectionString)[keyString];
        }

        /// <summary>
        /// Logs an absent configuration item and the default value applied in its place, using the same
        /// '&gt;{key}: {value}' format as every other setting so that a reader of the startup log sees the full
        /// effective configuration rather than a gap.
        /// </summary>
        /// <param name="keyString">Configuration key value</param>
        /// <param name="defaultValueString">The default value being applied</param>
        /// <param name="isMasked">Whether the value should be masked in the log file output</param>
        void LogDefaultApplied(string keyString, string defaultValueString, bool isMasked)
        {
            if (isMasked == true)
            {
                logger.Info($">{keyString}: {MaskString} (not found in the configuration file; default applied)");
            }
            else
            {
                logger.Info($">{keyString}: {defaultValueString} (not found in the configuration file; default applied)");
            }
        }

        /// <inheritdoc/>
        public void SetConfiguration(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
    }
}
