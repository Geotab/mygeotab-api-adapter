namespace MyGeotabAPIAdapter.DIGAPI
{
    /// <summary>
    /// Represents the result of a DIG API authentication or token refresh operation.
    /// </summary>
    public class DIGAuthenticationResult
    {
        /// <summary>
        /// Indicates whether the authentication was successful.
        /// </summary>
        public bool Authenticated { get; set; }

        /// <summary>
        /// The user ID of the authenticated user.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// The bearer token for API access.
        /// </summary>
        public DIGToken? BearerToken { get; set; }

        /// <summary>
        /// The refresh token for obtaining new tokens.
        /// </summary>
        public DIGToken? RefreshToken { get; set; }
    }
}