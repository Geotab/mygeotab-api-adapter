namespace MyGeotabAPIAdapter.Helpers
{
    /// <summary>
    /// Interface for a helper class to assist in working with strings.
    /// </summary>
    public interface IStringHelper
    {
        /// <summary>
        /// Indicates whether <paramref name="s1"/> and <paramref name="s2"/> are equal. For comparison purposes, null and empty strings are considered to be equal.
        /// </summary>
        /// <param name="s1">The string to compare with <paramref name="s2"/>.</param>
        /// <param name="s2">The string to compare with <paramref name="s1"/>.</param>
        /// <returns></returns>
        bool AreEqual(string s1, string s2);

        /// <summary>
        /// Indicates whether <paramref name="identifier"/> is a valid identifier for a database object such as a schame name or a table name. A valid identifier must start with a letter (capital or lowercase) or an underscore, and may contain only letters (capital or lowercase), numbers, underscores, and dashes.
        /// </summary>
        /// <param name="identifier">The identifier to be evaluated.</param>
        /// <returns></returns>
        bool IsValidIdentifierForDatabaseObject(string identifier);
    }
}
