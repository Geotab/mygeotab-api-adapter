namespace MyGeotabAPIAdapter.DIGAPI
{
    /// <summary>
    /// Represents a token from the DIG API (either Bearer or Refresh token).
    /// </summary>
    public class DIGToken
    {
        /// <summary>
        /// The string representation of the token.
        /// </summary>
        public string TokenString { get; set; } = string.Empty;

        /// <summary>
        /// The date and time when the token expires (UTC).
        /// </summary>
        public DateTime Expires { get; set; }

        /// <summary>
        /// The date and time when the token was created (UTC).
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Indicates whether the token has expired.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= Expires;

        /// <summary>
        /// Gets the time remaining until the token expires.
        /// </summary>
        public TimeSpan TimeUntilExpiry => Expires - DateTime.UtcNow;
    }
}