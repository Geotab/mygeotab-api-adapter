using MyGeotabAPIAdapter.Exceptions;

namespace MyGeotabAPIAdapter.MyAdminAPI
{
    /// <summary>
    /// Represents the result of a call to the MyAdmin API <see href="https://developers.geotab.com/myAdmin/apiReference/methods/ProvisionDevice/index.html">ProvisionDevice</see> method. Contains details from the returned <see href="https://developers.geotab.com/myAdmin/apiReference/objects/ProvisionResult/">ProvisionResult</see> along with additional metadata added by this middleware, if applicable.
    /// </summary>
    public class ProvisionDeviceResult
    {
        /// <summary>
        /// If provisioning failed, the error message indicating the reason for failure.
        /// This may contain errors from the MyAdmin API or from middleware exceptions.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The source of the error, if any.
        /// </summary>
        public ErrorSource ErrorSource { get; set; } = ErrorSource.None;

        /// <summary>
        /// The serial number of the provisioned device, or null/empty if provisioning failed.
        /// </summary>
        public string GeotabSerialNumber { get; set; }

        /// <summary>
        /// Indicates whether the provisioning was successful.
        /// </summary>
        public bool IsSuccess { get; set; }
    }
}