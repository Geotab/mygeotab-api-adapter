using MyGeotabAPIAdapter;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Logging;
using NLog;

namespace GeotabDIGAdapter
{
    /// <summary>
    /// A class that handles checking whether prerequisite services are running.
    /// </summary>
    public class PrerequisiteServiceChecker<T> : IPrerequisiteServiceChecker<T> where T : IDbOServiceTracking
    {
        const int OrchestratorServiceInitializationCheckIntervalSeconds = 5;

        readonly IMessageLogger messageLogger;
        readonly IOrchestratorServiceTracker orchestratorServiceTracker;
        readonly IServiceTracker<T> serviceTracker;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="PrerequisiteServiceChecker{T}"/> class.
        /// </summary>
        public PrerequisiteServiceChecker(IMessageLogger messageLogger, IOrchestratorServiceTracker orchestratorServiceTracker, IServiceTracker<T> serviceTracker)
        {
            this.messageLogger = messageLogger;
            this.orchestratorServiceTracker = orchestratorServiceTracker;
            this.serviceTracker = serviceTracker;
        }

        /// <inheritdoc/>
        public async Task WaitForPrerequisiteServicesIfNeededAsync(string dependentServiceClassName, List<DIGAdapterService> prerequisiteServices, CancellationToken cancellationToken, bool includeCheckForWhetherServicesHaveProcessedAnyData = false)
        {
            // Perform initial check to see whether the Orchestrator service has been initialized.
            var orchestratorServiceInitialized = orchestratorServiceTracker.OrchestratorServiceInitialized;

            // Perform initial check to see whether all prerequisite services are running.
            var prerequisiteServiceOperationCheckResult = await serviceTracker.CheckOperationOfPrerequisiteServicesAsync(prerequisiteServices, includeCheckForWhetherServicesHaveProcessedAnyData);
            var allPrerequisiteServicesRunning = prerequisiteServiceOperationCheckResult.AllPrerequisiteServicesRunning;
            var allPrerequisiteServicesHaveProcessedData = false;

            // If all prerequisite services are not running, keep checking until they are.
            var waitForOrchestratorMessageLogged = false;
            var waitForPrerequisiteServicesMessageLogged = false;
            while (orchestratorServiceInitialized == false || allPrerequisiteServicesRunning == false || (includeCheckForWhetherServicesHaveProcessedAnyData && allPrerequisiteServicesHaveProcessedData == false))
            {
                orchestratorServiceInitialized = orchestratorServiceTracker.OrchestratorServiceInitialized;
                prerequisiteServiceOperationCheckResult = await serviceTracker.CheckOperationOfPrerequisiteServicesAsync(prerequisiteServices, includeCheckForWhetherServicesHaveProcessedAnyData);
                allPrerequisiteServicesRunning = prerequisiteServiceOperationCheckResult.AllPrerequisiteServicesRunning;

                if (prerequisiteServiceOperationCheckResult.ServicesWithNoDataProcessed.Count == 0)
                {
                    allPrerequisiteServicesHaveProcessedData = true;
                }

                if (orchestratorServiceInitialized == false)
                {
                    if (waitForOrchestratorMessageLogged == false)
                    {
                        logger.Warn($"[{dependentServiceClassName}] Orchestrator service not yet initialized. Waiting {OrchestratorServiceInitializationCheckIntervalSeconds} seconds before checking again.");
                        waitForOrchestratorMessageLogged = true;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(OrchestratorServiceInitializationCheckIntervalSeconds), cancellationToken);
                }
                else
                {
                    if (allPrerequisiteServicesRunning == false || (includeCheckForWhetherServicesHaveProcessedAnyData && allPrerequisiteServicesHaveProcessedData == false))
                    {
                        if (waitForPrerequisiteServicesMessageLogged == false)
                        {
                            var message = $"[{dependentServiceClassName}] Waiting for prerequisite services.";
                            if (!string.IsNullOrEmpty(prerequisiteServiceOperationCheckResult.ServicesNeverRunStatement))
                            {
                                message += $" {prerequisiteServiceOperationCheckResult.ServicesNeverRunStatement}";
                            }
                            if (!string.IsNullOrEmpty(prerequisiteServiceOperationCheckResult.ServicesNotRunningStatement))
                            {
                                message += $" {prerequisiteServiceOperationCheckResult.ServicesNotRunningStatement}";
                            }
                            if (includeCheckForWhetherServicesHaveProcessedAnyData && !string.IsNullOrEmpty(prerequisiteServiceOperationCheckResult.ServicesWithNoDataProcessedStatement))
                            {
                                message += $" {prerequisiteServiceOperationCheckResult.ServicesWithNoDataProcessedStatement}";
                            }
                            logger.Warn(message);
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
                logger.Info($"[{dependentServiceClassName}] Prerequisite services are now running. Resuming operation.");
            }
        }

        /// <inheritdoc/>
        public async Task WaitForPrerequisiteServiceToProcessEntitiesAsync(string dependentServiceClassName, DIGAdapterService prerequisiteService, CancellationToken cancellationToken)
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
                    logger.Debug($"[{dependentServiceClassName}] Waiting {waitDuration.TotalSeconds} seconds for prerequisite service '{prerequisiteService}' to process entities.");
                    await Task.Delay(waitDuration, cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            // Log a message indicating that the service is resuming operation.
            logger.Info($"[{dependentServiceClassName}] Prerequisite service '{prerequisiteService}' has processed entities. Resuming operation.");
        }
    }
}
