using MyGeotabAPIAdapter.Logging;
using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// A class that handles checking whether prerequisite processors are running.
    /// </summary>
    internal class PrerequisiteProcessorChecker : IPrerequisiteProcessorChecker
    {
        const int OrchestratorServiceInitializationCheckIntervalSeconds = 5;

        readonly IMessageLogger messageLogger;
        readonly IOrchestratorServiceTracker orchestratorServiceTracker;
        readonly IProcessorTracker processorTracker;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="PrerequisiteProcessorChecker"/> class.
        /// </summary>
        public PrerequisiteProcessorChecker(IMessageLogger messageLogger, IOrchestratorServiceTracker orchestratorServiceTracker, IProcessorTracker processorTracker)
        {
            this.messageLogger = messageLogger;
            this.orchestratorServiceTracker = orchestratorServiceTracker;
            this.processorTracker = processorTracker;
        }

        /// <inheritdoc/>
        public async Task WaitForPrerequisiteProcessorsIfNeededAsync(string dependentProcessorClassName, List<DataOptimizerProcessor> prerequisiteProcessors, CancellationToken cancellationToken)
        {
            // Perform initial check to see whether the Orchestrator service has been initialized.
            var orchestratorServiceInitialized = orchestratorServiceTracker.OrchestratorServiceInitialized;

            // Perform initial check to see whether all prerequisite processors are running.
            var prerequisiteProcessorOperationCheckResult = await processorTracker.CheckOperationOfPrerequisiteProcessorsAsync(prerequisiteProcessors);
            var allPrerequisiteProcessorsRunning = prerequisiteProcessorOperationCheckResult.AllPrerequisiteProcessorsRunning;
            var allPrerequisiteProcessorsHaveProcessedData = false;

            // If all prerequisite processors are not running, keep checking until they are.
            while (orchestratorServiceInitialized == false || allPrerequisiteProcessorsRunning == false || allPrerequisiteProcessorsHaveProcessedData == false)
            {
                orchestratorServiceInitialized = orchestratorServiceTracker.OrchestratorServiceInitialized;
                prerequisiteProcessorOperationCheckResult = await processorTracker.CheckOperationOfPrerequisiteProcessorsAsync(prerequisiteProcessors);
                allPrerequisiteProcessorsRunning = prerequisiteProcessorOperationCheckResult.AllPrerequisiteProcessorsRunning;

                if (prerequisiteProcessorOperationCheckResult.ProcessorsWithNoDataProcessed.Count == 0)
                {
                    allPrerequisiteProcessorsHaveProcessedData = true;
                }

                if (orchestratorServiceInitialized == false)
                {
                    messageLogger.LogWaitForOrchestratorServiceServicePause(dependentProcessorClassName, TimeSpan.FromSeconds(OrchestratorServiceInitializationCheckIntervalSeconds));
                    await Task.Delay(TimeSpan.FromSeconds(OrchestratorServiceInitializationCheckIntervalSeconds), cancellationToken);
                }
                else
                {
                    if (allPrerequisiteProcessorsRunning == false || allPrerequisiteProcessorsHaveProcessedData == false)
                    {
                        messageLogger.LogWaitForPrerequisiteServicesServicePause(dependentProcessorClassName, prerequisiteProcessorOperationCheckResult.ProcessorsNeverRunStatement, prerequisiteProcessorOperationCheckResult.ProcessorsNotRunningStatement, prerequisiteProcessorOperationCheckResult.ProcessorsWithNoDataProcessedStatement, prerequisiteProcessorOperationCheckResult.RecommendedDelayBeforeNextCheck);
                        await Task.Delay(prerequisiteProcessorOperationCheckResult.RecommendedDelayBeforeNextCheck, cancellationToken);
                        messageLogger.LogWaitForPrerequisiteServicesServiceResumption(dependentProcessorClassName);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
