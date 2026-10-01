using System;
using System.Linq;

namespace MyGeotabAPIAdapter.Configuration
{
    /// <summary>
    /// Parses a configuration value into a member of an enum.
    /// <para>
    /// Exists because <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/> alone is the wrong tool for a
    /// configuration value, in three ways that each turn a typo into a silent misconfiguration rather than a
    /// startup failure: it is case-sensitive unless asked otherwise, while nothing in the configuration
    /// documentation tells an operator which values are case-sensitive; it accepts a numeric string and casts it
    /// to the underlying type without checking that the result is a member, so '99' parses successfully into a
    /// value no switch will match; and it accepts a comma-separated list, OR-ing the members together, even for
    /// an enum that is not a <see cref="FlagsAttribute"/> enum.
    /// </para>
    /// <para>
    /// Use this for every configuration value that names an enum member, so that the same value is accepted - and
    /// the same values refused - everywhere in the solution.
    /// </para>
    /// </summary>
    public static class ConfigurationEnumParser
    {
        /// <summary>
        /// Attempts to parse the supplied configuration value into a member of <typeparamref name="TEnum"/>.
        /// Matching ignores case and surrounding whitespace; everything else is refused.
        /// </summary>
        /// <typeparam name="TEnum">The enum type the configuration value is expected to name.</typeparam>
        /// <param name="configItemValue">The configuration value, as read from the configuration file.</param>
        /// <param name="result">The matched member, or <c>default</c> if the value did not name one.</param>
        /// <returns><c>true</c> if the value named a member of <typeparamref name="TEnum"/>; otherwise <c>false</c>.</returns>
        public static bool TryParse<TEnum>(string configItemValue, out TEnum result) where TEnum : struct, Enum
        {
            result = default;

            if (string.IsNullOrWhiteSpace(configItemValue))
            {
                return false;
            }

            if (Enum.TryParse(configItemValue, true, out TEnum parsedValue) == false)
            {
                return false;
            }

            // Refuses a numeric string that is outside the enum - '99' parses to (TEnum)99 without this.
            if (Enum.IsDefined(parsedValue) == false)
            {
                return false;
            }

            // Refuses a numeric string that happens to be inside the enum ('0'), and a comma-separated list
            // ('PostgreSQL, SQLServer', which Enum.TryParse ORs into a single member). Both survive the two
            // checks above; neither is a configuration value this solution documents or intends to accept.
            if (string.Equals(parsedValue.ToString(), configItemValue.Trim(), StringComparison.OrdinalIgnoreCase) == false)
            {
                return false;
            }

            result = parsedValue;
            return true;
        }

        /// <summary>
        /// Renders the members of <typeparamref name="TEnum"/> as a quoted, comma-separated list, for use in the
        /// error message reporting a value that named none of them.
        /// </summary>
        /// <typeparam name="TEnum">The enum type whose members are to be listed.</typeparam>
        /// <param name="unsupportedValues">
        /// Members that are defined on the enum but are not valid configuration values, and so must not be
        /// advertised to an operator as though they were.
        /// </param>
        /// <returns>The members, each single-quoted, separated by commas.</returns>
        public static string DescribeValidValues<TEnum>(params TEnum[] unsupportedValues) where TEnum : struct, Enum
        {
            var validValueNames = Enum.GetValues<TEnum>()
                .Where(enumValue => unsupportedValues.Contains(enumValue) == false)
                .Select(enumValue => enumValue.ToString());
            return $"'{string.Join("', '", validValueNames)}'";
        }
    }
}
