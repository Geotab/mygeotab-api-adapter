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
    }
}
