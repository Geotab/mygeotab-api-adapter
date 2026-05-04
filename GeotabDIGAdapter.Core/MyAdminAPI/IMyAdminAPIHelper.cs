using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.MyAdminAPI
{
    /// <summary>
    /// Interface for a helper class to assist in working with the MyAdmin API.
    /// </summary>
    public interface IMyAdminAPIHelper
    {
        /// <summary>
        /// The default timeout in seconds for MyAdmin API requests.
        /// </summary>
        const int DefaultTimeoutSeconds = 30;

        /// <summary>
        /// Indicates whether the MyAdmin API session is authenticated.
        /// </summary>
        bool MyAdminAPIIsAuthenticated { get; }

        /// <summary>
        /// Authenticates with the MyAdmin API.
        /// </summary>
        /// <param name="myAdminApiEndpoint">The MyAdmin API endpoint.</param>
        /// <param name="username">The MyAdmin username.</param>
        /// <param name="password">The MyAdmin password.</param>
        /// <param name="requestTimeoutSeconds">The timeout, in seconds, for API requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AuthenticateMyAdminAPIAsync(string myAdminApiEndpoint, string username, string password, int requestTimeoutSeconds = DefaultTimeoutSeconds);

        /// <summary>
        /// Calls the MyAdmin API <see href="https://developers.geotab.com/myAdmin/apiReference/methods/GetDeviceDatabaseNamesAsync/index.html">GetDeviceDatabaseNamesAsync</see> method.
        /// </summary>
        /// <param name="serialNumbers">The array of serial numbers for which to retrieve that list of owner and shared databases.</param>
        /// <param name="requestTimeoutSeconds">The timeout, in seconds, for API requests.</param>
        /// <returns>A <see cref="GetDeviceDatabaseNamesResult"/>.</returns>
        Task<GetDeviceDatabaseNamesResult> GetDeviceDatabaseNamesAsync(IList<string> serialNumbers, int requestTimeoutSeconds = DefaultTimeoutSeconds);

        /// <summary>
        /// Calls the MyAdmin API <see href="https://developers.geotab.com/myAdmin/apiReference/methods/ProvisionDeviceToAccount/index.html">ProvisionDeviceToAccount</see> method.
        /// </summary>
        /// <param name="dbGdaQProvisionDevice">The <see cref="DbGdaQProvisionDevice"/> entity containing device provisioning parameters.</param>
        /// <param name="requestTimeoutSeconds">The timeout, in seconds, for API requests.</param>
        /// <returns>A <see cref="ProvisionDeviceResult"/>.</returns>
        Task<ProvisionDeviceResult> ProvisionDeviceToAccountAsync(DbGdaQProvisionDevice dbGdaQProvisionDevice, int requestTimeoutSeconds = DefaultTimeoutSeconds);

        /// <summary>
        /// Test method to verify that the session expiry re-authentication policy works correctly.
        /// </summary>
        /// <param name="requestTimeoutSeconds">The timeout in seconds for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task TestSessionExpiryReauthenticationAsync(int requestTimeoutSeconds = 30);
    }
}