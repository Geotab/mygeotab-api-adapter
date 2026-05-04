using MyAdminApiLib.Geotab.MyAdmin.MyAdminApi.ObjectModel;
using MyGeotabAPIAdapter.Exceptions;

namespace MyGeotabAPIAdapter.MyAdminAPI
{
    /// <summary>
    /// Represents the result of a call to the MyAdmin API <see href="https://developers.geotab.com/myAdmin/apiReference/methods/GetDeviceDatabaseNamesAsync/index.html">GetDeviceDatabaseNamesAsync</see> method. Contains details from the returned <see href="https://developers.geotab.com/myAdmin/apiReference/objects/ApiDeviceDatabaseOwnerShared/">ApiDeviceDatabaseOwnerShared</see> along with additional metadata added by this middleware, if applicable.
    /// </summary>
    public class GetDeviceDatabaseNamesResult
    {
        /// <summary>
        /// The array of <see cref="ApiDeviceDatabaseOwnerShared"/> entities returned by the API.
        /// May be null if the API returns no results or an error occurred.
        /// </summary>
        public ApiDeviceDatabaseOwnerShared[]? ApiDeviceDatabaseOwnerShareds { get; set; }

        /// <summary>
        /// If an exception was encountered, the error message indicating the reason for failure.
        /// This may contain errors from the MyAdmin API or from middleware exceptions.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// The source of the error, if any.
        /// </summary>
        public ErrorSource ErrorSource { get; set; } = ErrorSource.None;

        /// <summary>
        /// Indicates whether any device database owner shared results were returned.
        /// </summary>
        public bool HasResults => ApiDeviceDatabaseOwnerShareds?.Length > 0;
    }
}
