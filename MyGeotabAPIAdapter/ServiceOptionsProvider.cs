using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A class that provides methods to access and manage service options configurations.
    /// </summary>
    public class ServiceOptionsProvider : IServiceOptionsProvider
    {
        private readonly IOptionsMonitor<ServiceOptions> optionsMonitor;
        private readonly List<string> configuredServiceNames = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceOptionsProvider"/> class.
        /// </summary>
        /// <param name="optionsMonitor">An <see cref="IOptionsMonitor{TOptions}"/> for accessing service options.</param>
        public ServiceOptionsProvider(IOptionsMonitor<ServiceOptions> optionsMonitor)
        {
            this.optionsMonitor = optionsMonitor;
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetAllServiceNames()
        {
            return configuredServiceNames;
        }

        /// <inheritdoc/>
        public IEnumerable<ServiceOptions> GetAllServiceOptions(IEnumerable<string> serviceNames)
        {
            return serviceNames.Select(name => optionsMonitor.Get(name));
        }

        /// <inheritdoc/>
        public void TrackService(string serviceName)
        {
            if (!configuredServiceNames.Contains(serviceName))
            {
                configuredServiceNames.Add(serviceName);
            }
        }
    }
}
