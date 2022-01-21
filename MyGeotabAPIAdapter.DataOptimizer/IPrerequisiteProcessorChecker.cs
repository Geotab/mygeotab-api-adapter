using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// Interface for a class that handles checking whether prerequisite processors are running.
    /// </summary>
    public interface IPrerequisiteProcessorChecker
    {
        /// <summary>
        /// Checks whether all of the <paramref name="prerequisiteProcessors"/> are running and, if not, waits until they are. Intended to allow for any prerequisite processors that are not running to be started and have dependent processors/services then continue operation.
        /// </summary>
        /// <param name="dependentProcessorClassName">The name of the dependent processor class.</param>
        /// <param name="prerequisiteProcessors">A list of processors upon which the dependent processor depends.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        Task WaitForPrerequisiteProcessorsIfNeededAsync(string dependentProcessorClassName, List<DataOptimizerProcessor> prerequisiteProcessors, CancellationToken cancellationToken);
    }
}
