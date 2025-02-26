using System.Collections.Generic;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a class that provides methods to access and manage service options configurations.
    /// </summary>
    public interface IServiceOptionsProvider
    {
        /// <summary>
        /// Gets a collection of all configured service names.
        /// </summary>
        /// <returns>A collection of service names that have been configured.</returns>
        IEnumerable<string> GetAllServiceNames();

        /// <summary>
        /// Retrieves all <see cref="ServiceOptions"/> for the specified service names.
        /// </summary>
        /// <param name="serviceNames">The names of the services for which to retrieve options.</param>
        /// <returns>A collection of <see cref="ServiceOptions"/> instances.</returns>
        IEnumerable<ServiceOptions> GetAllServiceOptions(IEnumerable<string> serviceNames);

        /// <summary>
        /// Tracks a service name when its <see cref="ServiceOptions"/> is configured.
        /// </summary>
        /// <param name="serviceName">The name of the service to track.</param>
        void TrackService(string serviceName);
    }
}
