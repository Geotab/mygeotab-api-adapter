using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Logging;
using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A class that handles checking whether prerequisite services are running.
    /// </summary>
    internal class PrerequisiteServiceChecker<T> : IPrerequisiteServiceChecker<T> where T : IDbOServiceTracking
    {
        const int OrchestratorServiceInitializationCheckIntervalSeconds = 5;

        readonly IMessageLogger messageLogger;
        readonly IOrchestratorServiceTracker orchestratorServiceTracker;
        readonly IServiceTracker<T> serviceTracker;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="PrerequisiteServiceChecker"/> class.
        /// </summary>
        public PrerequisiteServiceChecker(IMessageLogger messageLogger, IOrchestratorServiceTracker orchestratorServiceTracker, IServiceTracker<T> serviceTracker)
        {
            this.messageLogger = messageLogger;
            this.orchestratorServiceTracker = orchestratorServiceTracker;
            this.serviceTracker = serviceTracker;
        }

        /// <inheritdoc/>
        public async Task WaitForPrerequisiteServicesIfNeededAsync(string dependentServiceClassName, List<AdapterService> prerequisiteServices, CancellationToken cancellationToken, bool includeCheckForWhetherServicesHaveProcessedAnyData = false)
        {
            // Perform initial check to see whether the Orchestrator service has been initialized.
            var orchestratorServiceInitialized = orchestratorServiceTracker.OrchestratorServiceInitialized;

            // Perform initial check to see whether all prerequisite services are running.
            var prerequisiteServiceOperationCheckResult = await serviceTracker.CheckOperationOfPrerequisiteServicesAsync(prerequisiteServices);
            var allPrerequisiteServicesRunning = prerequisiteServiceOperationCheckResult.AllPrerequisiteServicesRunning;
            var allPrerequisiteServicesHaveProcessedData = false;

            // If all prerequisite services are not running, keep checking until they are.
            var waitForOrchestratorMessageLogged = false;
            var waitForPrerequisiteServicesMessageLogged = false;
            while (orchestratorServiceInitialized == false || allPrerequisiteServicesRunning == false || allPrerequisiteServicesHaveProcessedData == false)
            {
                orchestratorServiceInitialized = orchestratorServiceTracker.OrchestratorServiceInitialized;
                prerequisiteServiceOperationCheckResult = await serviceTracker.CheckOperationOfPrerequisiteServicesAsync(prerequisiteServices);
                allPrerequisiteServicesRunning = prerequisiteServiceOperationCheckResult.AllPrerequisiteServicesRunning;

                if (prerequisiteServiceOperationCheckResult.ServicesWithNoDataProcessed.Count == 0)
                {
                    allPrerequisiteServicesHaveProcessedData = true;
                }

                if (orchestratorServiceInitialized == false)
                {
                    if (waitForOrchestratorMessageLogged == false)
                    {
                        messageLogger.LogWaitForOrchestratorServiceServicePause(dependentServiceClassName, TimeSpan.FromSeconds(OrchestratorServiceInitializationCheckIntervalSeconds));
                        waitForOrchestratorMessageLogged = true;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(OrchestratorServiceInitializationCheckIntervalSeconds), cancellationToken);
                }
                else
                {
                    if (allPrerequisiteServicesRunning == false || allPrerequisiteServicesHaveProcessedData == false)
                    {
                        if (waitForPrerequisiteServicesMessageLogged == false)
                        {
                            messageLogger.LogWaitForPrerequisiteServicesServicePause(dependentServiceClassName, prerequisiteServiceOperationCheckResult.ServicesNeverRunStatement, prerequisiteServiceOperationCheckResult.ServicesNotRunningStatement, prerequisiteServiceOperationCheckResult.ServicesWithNoDataProcessedStatement, prerequisiteServiceOperationCheckResult.RecommendedDelayBeforeNextCheck);
                            waitForPrerequisiteServicesMessageLogged = true;
                        }
                        
                        await Task.Delay(prerequisiteServiceOperationCheckResult.RecommendedDelayBeforeNextCheck, cancellationToken);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            // Log a message indicating that the service is resuming operation.
            if (waitForOrchestratorMessageLogged == true || waitForPrerequisiteServicesMessageLogged == true)
            {
                messageLogger.LogWaitForPrerequisiteServicesServiceResumption(dependentServiceClassName);
            }
        }
    }
}
