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
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

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

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return configItemValueBool;
        }

        /// <inheritdoc/>
        public DateTime GetConfigKeyValueDateTime(string keyString, string sectionString = "", bool isMasked = false)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

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

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return configItemValueDateTime;
        }

        /// <inheritdoc/>
        public int GetConfigKeyValueInt(string keyString, string sectionString = "", bool isMasked = false, int minimumAllowedValue = int.MinValue, int maximumAllowedValue = int.MaxValue, int defaultValueIfOutsideRange = int.MinValue)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

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

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return output;
        }

        /// <inheritdoc/>
        public string GetConfigKeyValueString(string keyString, string sectionString = "", bool isLogged = true, bool isMasked = false)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");
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

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return output;
        }

        /// <inheritdoc/>
        public IConfigurationSection GetConfigSection(string sectionString)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var output = configuration.GetSection(sectionString);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return output;
        }

        /// <inheritdoc/>
        public void SetConfiguration(IConfiguration configuration)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.configuration = configuration;

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
