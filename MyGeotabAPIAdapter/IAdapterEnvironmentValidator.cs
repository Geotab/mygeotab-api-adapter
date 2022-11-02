using MyGeotabAPIAdapter.Database.Models;
using System.Collections.Generic;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a class that includes validation logic to help ensure that only one instance of a given <see cref="AdapterService"/> is running against the same adapter database in a distributed deployment scenario in which copies of the <see cref="MyGeotabAPIAdapter"/> are installed on multiple machines with different services running on each (in order to distribute load and maximize throughput) AND that the same version of the <see cref="MyGeotabAPIAdapter"/> is used on all machines involved. 
    /// </summary>
    public interface IAdapterEnvironmentValidator
    {
        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Validates the <see cref="IAdapterEnvironment.AdapterMachineName"/> against the <see cref="DbOServiceTracking"/> in the <paramref name="dbOServiceTrackings"/> identified by <paramref name="adapterService"/>. Intended to help ensure that only one instance of the subject <see cref="AdapterService"/> is running against the same adapter database in a distributed deployment scenario in which copies of the <see cref="MyGeotabAPIAdapter"/> are installed on multiple machines with different services running on each (in order to distribute load and maximize throughput). Running multiple instances of a service against the same database will result in data duplication amongst other possible issues.
        /// </summary>
        /// <param name="adapterEnvironment">The <see cref="IAdapterEnvironment"/> to be validated.</param>
        /// <param name="dbOServiceTrackings">A list of <see cref="DbOServiceTracking"/> objects to validate the <paramref name="adapterEnvironment"/> against.</param>
        /// <param name="adapterService">The specific <see cref="AdapterService"/> in the <paramref name="dbOServiceTrackings"/> to be validated against.</param>
        void ValidateAdapterMachineName(IAdapterEnvironment adapterEnvironment, List<DbOServiceTracking> dbOServiceTrackings, AdapterService adapterService);

        /// <summary>
        /// Validates the <see cref="IAdapterEnvironment.AdapterVersion"/> against the <see cref="DbOServiceTracking"/> in the <paramref name="dbOServiceTrackings"/> identified by <paramref name="adapterService"/>. Intended to help ensure that the same version of the <see cref="MyGeotabAPIAdapter"/> is used on all machines in a distributed deployment scenario in which copies of the <see cref="MyGeotabAPIAdapter"/> are installed on multiple machines with different services running on each (in order to distribute load and maximize throughput).
        /// </summary>
        /// <param name="adapterEnvironment">The <see cref="IAdapterEnvironment"/> to be validated.</param>
        /// <param name="dbOServiceTrackings">A list of <see cref="DbOServiceTracking"/> objects to validate the <paramref name="adapterEnvironment"/> against.</param>
        /// <param name="adapterService">The specific <see cref="AdapterService"/> in the <paramref name="dbOServiceTrackings"/> to be validated against.</param>
        void ValidateAdapterVersion(IAdapterEnvironment adapterEnvironment, List<DbOServiceTracking> dbOServiceTrackings, AdapterService adapterService);
    }
}
