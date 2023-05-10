using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// Interface for a class for obtaining environment information related to the <see cref="DataOptimizer"/>. 
    /// </summary>
    public interface IOptimizerEnvironment
    {
        /// <summary>
        /// The <see cref="System.Reflection.AssemblyName"/> of the <see cref="DataOptimizer"/> assembly.
        /// </summary>
        string OptimizerAssemblyName { get; }

        /// <summary>
        /// The <see cref="Environment.MachineName"/> of the computer on which the <see cref="DataOptimizer"/> assembly is being executed.
        /// </summary>
        string OptimizerMachineName { get; }

        /// <summary>
        /// The <see cref="Version"/> of the <see cref="DataOptimizer"/> assembly.
        /// </summary>
        Version OptimizerVersion { get; }

        /// <summary>
        /// <para>
        /// Validates both the <see cref="OptimizerVersion"/> and the <see cref="IOptimizerEnvironment.OptimizerMachineName"/> of this <see cref="IOptimizerEnvironment"/> as decribed below.
        /// </para>
        /// <para>
        /// Validates the <see cref="OptimizerVersion"/> of this <see cref="IOptimizerEnvironment"/> against the <see cref="DbOProcessorTracking"/> in the <paramref name="dbOProcessorTrackings"/> identified by <paramref name="dataOptimizerProcessor"/>. Intended to help ensure that the same version of the <see cref="DataOptimizer"/> is used on all machines in a distributed deployment scenario in which copies of the <see cref="DataOptimizer"/> are installed on multiple machines with different processor/optimizer services running on each (in order to distribute load and maximize throughput).
        /// </para>
        /// <para>
        /// Validates the <see cref="OptimizerMachineName"/> of this <see cref="IOptimizerEnvironment"/> against the <see cref="DbOProcessorTracking"/> in the <paramref name="dbOProcessorTrackings"/> identified by <paramref name="dataOptimizerProcessor"/>. Intended to help ensure that only one instance of the subject <see cref="DataOptimizerProcessor"/> is running against the same optimizer database in a distributed deployment scenario in which copies of the <see cref="DataOptimizer"/> are installed on multiple machines with different processor/optimizer services running on each (in order to distribute load and maximize throughput). Running multiple instances of a processor against the same database will result in data duplication amongst other possible issues.
        /// </para>
        /// </summary>
        /// <param name="dbOProcessorTrackings">A list of <see cref="DbOProcessorTracking"/> objects to validate the <paramref name="optimizerEnvironment"/> against.</param>
        /// <param name="dataOptimizerProcessor">The specific <see cref="DataOptimizerProcessor"/> in the <paramref name="dbOProcessorTrackings"/> to be validated against.</param>
        /// <param name="disableMachineNameValidation">Indicates whether machine name validation should be disabled. NOTE: This should always be set to <c>false</c> except in scenarios where machine names in hosted environments are not static. WARNING: Improper use of this setting could result in application instability and data integrity issues.</param>
        void ValidateOptimizerEnvironment(List<DbOProcessorTracking> dbOProcessorTrackings, DataOptimizerProcessor dataOptimizerProcessor, bool disableMachineNameValidation);
    }
}
