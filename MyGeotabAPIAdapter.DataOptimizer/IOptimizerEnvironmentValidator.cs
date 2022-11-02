using MyGeotabAPIAdapter.Database.Models;
using System.Collections.Generic;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// Interface for a class that includes validation logic to help ensure that only one instance of a given <see cref="DataOptimizerProcessor"/> is running against the same optimizer database in a distributed deployment scenario in which copies of the <see cref="MyGeotabAPIAdapter.DataOptimizer"/> are installed on multiple machines with different services running on each (in order to distribute load and maximize throughput) AND that the same version of the <see cref="MyGeotabAPIAdapter.DataOptimizer"/> is used on all machines involved. 
    /// </summary>
    public interface IOptimizerEnvironmentValidator
    {
        /// <summary>
        /// Validates the <see cref="IOptimizerEnvironment.OptimizerMachineName"/> against the <see cref="DbOProcessorTracking"/> in the <paramref name="dbOProcessorTrackings"/> identified by <paramref name="dataOptimizerProcessor"/>. Intended to help ensure that only one instance of the subject <see cref="DataOptimizerProcessor"/> is running against the same optimizer database in a distributed deployment scenario in which copies of the <see cref="DataOptimizer"/> are installed on multiple machines with different processor/optimizer services running on each (in order to distribute load and maximize throughput). Running multiple instances of a processor against the same database will result in data duplication amongst other possible issues.
        /// </summary>
        /// <param name="optimizerEnvironment">The <see cref="IOptimizerEnvironment"/> to be validated.</param>
        /// <param name="dbOProcessorTrackings">A list of <see cref="DbOProcessorTracking"/> objects to validate the <paramref name="optimizerEnvironment"/> against.</param>
        /// <param name="dataOptimizerProcessor">The specific <see cref="DataOptimizerProcessor"/> in the <paramref name="dbOProcessorTrackings"/> to be validated against.</param>
        void ValidateOptimizerMachineName(IOptimizerEnvironment optimizerEnvironment, List<DbOProcessorTracking> dbOProcessorTrackings, DataOptimizerProcessor dataOptimizerProcessor);

        /// <summary>
        /// Validates the <see cref="IOptimizerEnvironment.OptimizerVersion"/> against the <see cref="DbOProcessorTracking"/> in the <paramref name="dbOProcessorTrackings"/> identified by <paramref name="dataOptimizerProcessor"/>. Intended to help ensure that the same version of the <see cref="DataOptimizer"/> is used on all machines in a distributed deployment scenario in which copies of the <see cref="DataOptimizer"/> are installed on multiple machines with different processor/optimizer services running on each (in order to distribute load and maximize throughput).
        /// </summary>
        /// <param name="optimizerEnvironment">The <see cref="IOptimizerEnvironment"/> to be validated.</param>
        /// <param name="dbOProcessorTrackings">A list of <see cref="DbOProcessorTracking"/> objects to validate the <paramref name="optimizerEnvironment"/> against.</param>
        /// <param name="dataOptimizerProcessor">The specific <see cref="DataOptimizerProcessor"/> in the <paramref name="dbOProcessorTrackings"/> to be validated against.</param>
        void ValidateOptimizerVersion(IOptimizerEnvironment optimizerEnvironment, List<DbOProcessorTracking> dbOProcessorTrackings, DataOptimizerProcessor dataOptimizerProcessor);
    }
}
