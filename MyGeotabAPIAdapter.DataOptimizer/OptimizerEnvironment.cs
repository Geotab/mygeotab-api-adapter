using MyGeotabAPIAdapter.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// A class for obtaining environment information related to the <see cref="DataOptimizer"/>. 
    /// </summary>
    public class OptimizerEnvironment : IOptimizerEnvironment
    {
        readonly IOptimizerEnvironmentValidator optimizerEnvironmentValidator;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public string OptimizerAssemblyName { get; }

        /// <inheritdoc/>
        public string OptimizerMachineName { get; }

        /// <inheritdoc/>
        public Version OptimizerVersion { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizerEnvironment"/> class.
        /// </summary>
        public OptimizerEnvironment(IOptimizerEnvironmentValidator optimizerEnvironmentValidator)
        {
            this.optimizerEnvironmentValidator = optimizerEnvironmentValidator;
            OptimizerAssemblyName = GetType().Assembly.GetName().Name;
            OptimizerMachineName = Environment.MachineName;
            OptimizerVersion = GetType().Assembly.GetName().Version;
        }

        /// <inheritdoc/>
        public void ValidateOptimizerEnvironment(List<DbOProcessorTracking> dbOProcessorTrackings, DataOptimizerProcessor dataOptimizerProcessor, bool disableMachineNameValidation)
        {
            optimizerEnvironmentValidator.ValidateOptimizerVersion(this, dbOProcessorTrackings, dataOptimizerProcessor);
            if (disableMachineNameValidation == false)
            {
                optimizerEnvironmentValidator.ValidateOptimizerMachineName(this, dbOProcessorTrackings, dataOptimizerProcessor);
            }
        }
    }
}
