using MyGeotabAPIAdapter.Database.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a class that handles checking whether prerequisite services are running.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IDbOServiceTracking"/> implementation to be used.</typeparam>
    public interface IPrerequisiteServiceChecker<T> where T : IDbOServiceTracking
    {
        /// <summary>
        /// Checks whether all of the <paramref name="prerequisiteServices"/> are running and, if not, waits until they are. Intended to allow for any prerequisite services that are not running to be started and have dependent services/services then continue operation.
        /// </summary>
        /// <param name="dependentServiceClassName">The name of the dependent service class.</param>
        /// <param name="prerequisiteServices">A list of services upon which the dependent service depends.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="includeCheckForWhetherServicesHaveProcessedAnyData">If set to true, an additional check will be done to see whether each service has processed data.</param>
        /// <returns></returns>
        Task WaitForPrerequisiteServicesIfNeededAsync(string dependentServiceClassName, List<AdapterService> prerequisiteServices, CancellationToken cancellationToken, bool includeCheckForWhetherServicesHaveProcessedAnyData = false);
    }
}
