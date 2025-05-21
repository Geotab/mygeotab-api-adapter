using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Logging;
using System;
using System.Collections.Generic;
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

        /// <inheritdoc/>
        public async Task WaitForPrerequisiteServiceToProcessEntitiesAsync(string dependentServiceClassName, AdapterService prerequisiteService, CancellationToken cancellationToken)
        {
            const int CheckIntervalSeconds = 10;
            var waitDuration = TimeSpan.FromSeconds(CheckIntervalSeconds);
            var waitStartTime = DateTime.UtcNow;
            var prerequisiteServiceHasProcessedEntitiesSinceWaitStartTime = false;
            while (prerequisiteServiceHasProcessedEntitiesSinceWaitStartTime == false)
            {
                prerequisiteServiceHasProcessedEntitiesSinceWaitStartTime = await serviceTracker.ServiceHasProcessedDataSinceAsync(prerequisiteService, waitStartTime);
                if (prerequisiteServiceHasProcessedEntitiesSinceWaitStartTime == false)
                {
                    messageLogger.LogWaitForPrerequisiteServiceToProcessEntitiesPause(dependentServiceClassName, prerequisiteService.ToString(), waitDuration);
                    await Task.Delay(waitDuration, cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            // Log a message indicating that the service is resuming operation.
            messageLogger.LogWaitForPrerequisiteServiceToProcessEntitiesResumption(dependentServiceClassName, prerequisiteService.ToString());
        }
    }
}
