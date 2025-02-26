using System.Text.RegularExpressions;
namespace MyGeotabAPIAdapter.Helpers
{
    /// <summary>
    /// A helper class to assist in working with strings.
    /// </summary>
    public class StringHelper : IStringHelper
    {
        /// <inheritdoc/>
        public bool AreEqual(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1))
            {
                return string.IsNullOrEmpty(s2);
            }
            return string.Equals(s1, s2);
        }

        /// <inheritdoc/>
        public bool IsValidIdentifierForDatabaseObject(string identifier)
        {
            return Regex.IsMatch(identifier, @"^[a-zA-Z_][a-zA-Z0-9_-]*$");
        }
    }
}
