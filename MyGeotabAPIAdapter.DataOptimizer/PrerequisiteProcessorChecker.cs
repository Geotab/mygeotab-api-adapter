﻿using MyGeotabAPIAdapter.Logging;
using NLog;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// A class that handles checking whether prerequisite processors are running.
    /// </summary>
    public class PrerequisiteProcessorChecker : IPrerequisiteProcessorChecker
    {
        readonly IMessageLogger messageLogger;
        readonly IProcessorTracker processorTracker;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="PrerequisiteProcessorChecker"/> class.
        /// </summary>
        public PrerequisiteProcessorChecker(IMessageLogger messageLogger, IProcessorTracker processorTracker)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.messageLogger = messageLogger;
            this.processorTracker = processorTracker;

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task WaitForPrerequisiteProcessorsIfNeededAsync(string dependentProcessorClassName, List<DataOptimizerProcessor> prerequisiteProcessors, CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Perform initial check to see whether all prerequisite processors are running.
            var prerequisiteProcessorOperationCheckResult = await processorTracker.CheckOperationOfPrerequisiteProcessors(prerequisiteProcessors);
            var allPrerequisiteProcessorsRunning = prerequisiteProcessorOperationCheckResult.AllPrerequisiteProcessorsRunning;
            var allPrerequisiteProcessorsHaveProcessedData = false;

            // If all prerequisite processors are not running, keep checking until they are.
            while (allPrerequisiteProcessorsRunning == false || allPrerequisiteProcessorsHaveProcessedData == false)
            {
                prerequisiteProcessorOperationCheckResult = await processorTracker.CheckOperationOfPrerequisiteProcessors(prerequisiteProcessors);
                allPrerequisiteProcessorsRunning = prerequisiteProcessorOperationCheckResult.AllPrerequisiteProcessorsRunning;

                if (prerequisiteProcessorOperationCheckResult.ProcessorsWithNoDataProcessed.Count == 0)
                {
                    allPrerequisiteProcessorsHaveProcessedData = true;
                }

                if (allPrerequisiteProcessorsRunning == false || allPrerequisiteProcessorsHaveProcessedData == false)
                {
                    messageLogger.LogWaitForPrerequisiteProcessorsServicePause(dependentProcessorClassName, prerequisiteProcessorOperationCheckResult.ProcessorsNeverRunStatement, prerequisiteProcessorOperationCheckResult.ProcessorsNotRunningStatement, prerequisiteProcessorOperationCheckResult.ProcessorsWithNoDataProcessedStatement, prerequisiteProcessorOperationCheckResult.RecommendedDelayBeforeNextCheck);
                    await Task.Delay(prerequisiteProcessorOperationCheckResult.RecommendedDelayBeforeNextCheck, cancellationToken);
                    messageLogger.LogWaitForPrerequisiteProcessorsServiceResumption(dependentProcessorClassName);
                }
                cancellationToken.ThrowIfCancellationRequested();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
