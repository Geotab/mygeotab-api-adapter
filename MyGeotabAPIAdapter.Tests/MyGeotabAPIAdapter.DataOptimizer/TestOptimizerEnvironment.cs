using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DataOptimizer;
using System;
using System.Collections.Generic;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// A test class for simulating environment information related to the <see cref="DataOptimizer"/>. 
    /// </summary>
    class TestOptimizerEnvironment : IOptimizerEnvironment 
    {
        /// <inheritdoc/>
        public string OptimizerAssemblyName { get; }

        /// <inheritdoc/>
        public string OptimizerMachineName { get; }

        /// <inheritdoc/>
        public Version OptimizerVersion { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestOptimizerEnvironment"/> class.
        /// </summary>
        /// <param name="optimizerAssemblyName">The value to be used for <see cref="OptimizerAssemblyName"/>.</param>
        /// <param name="optimizerMachineName">The value to be used for <see cref="OptimizerMachineName"/>.</param>
        /// <param name="optimizerVersion">The value to be used for <see cref="OptimizerVersion"/>.</param>
        public TestOptimizerEnvironment(string optimizerAssemblyName, string optimizerMachineName, string optimizerVersion)
        {
            OptimizerAssemblyName = optimizerAssemblyName;
            OptimizerMachineName = optimizerMachineName;
            OptimizerVersion = Version.Parse(optimizerVersion);
        }

        /// <summary>
        /// This method doesn't do anything in this <see cref="TestOptimizerEnvironment"/> testing class and is only here due to interface requirement.
        /// </summary>
        public void ValidateOptimizerEnvironment(List<DbOProcessorTracking> dbOProcessorTrackings, DataOptimizerProcessor dataOptimizerProcessor)
        {
            throw new NotImplementedException();
        }
    }
}
