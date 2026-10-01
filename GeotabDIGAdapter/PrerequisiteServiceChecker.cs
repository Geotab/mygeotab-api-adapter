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

        /// <summary>
        /// How often a service that is still waiting repeats its wait message. Logging only once makes a
        /// permanent wait indistinguishable from a transient one: the log simply goes quiet, which reads as a
        /// healthy idle adapter. Repeating it keeps a wait that will never end visible.
        /// </summary>
        const int WaitMessageRepeatIntervalMinutes = 15;

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
            var waitStartedUtc = DateTime.UtcNow;
            DateTime? orchestratorMessageLastLoggedUtc = null;
            DateTime? prerequisiteServicesMessageLastLoggedUtc = null;
            var waitMessageRepeatInterval = TimeSpan.FromMinutes(WaitMessageRepeatIntervalMinutes);
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
                    if (orchestratorMessageLastLoggedUtc == null || DateTime.UtcNow - orchestratorMessageLastLoggedUtc.Value >= waitMessageRepeatInterval)
                    {
                        logger.Warn($"[{dependentServiceClassName}] Orchestrator service not yet initialized{DescribeElapsedWait(waitStartedUtc)}. Waiting {OrchestratorServiceInitializationCheckIntervalSeconds} seconds before checking again.");
                        waitForOrchestratorMessageLogged = true;
                        orchestratorMessageLastLoggedUtc = DateTime.UtcNow;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(OrchestratorServiceInitializationCheckIntervalSeconds), cancellationToken);
                }
                else
                {
                    if (allPrerequisiteServicesRunning == false || (includeCheckForWhetherServicesHaveProcessedAnyData && allPrerequisiteServicesHaveProcessedData == false))
                    {
                        if (prerequisiteServicesMessageLastLoggedUtc == null || DateTime.UtcNow - prerequisiteServicesMessageLastLoggedUtc.Value >= waitMessageRepeatInterval)
                        {
                            var message = $"[{dependentServiceClassName}] Waiting for prerequisite services{DescribeElapsedWait(waitStartedUtc)}.";
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
                            prerequisiteServicesMessageLastLoggedUtc = DateTime.UtcNow;
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
    
        /// <summary>
        /// Renders how long a service has been waiting, for inclusion in a repeated wait message. An operator
        /// reading a single line needs to be able to tell a few seconds of startup ordering from a wait that
        /// has been going on for hours and is never going to end.
        /// </summary>
        /// <param name="waitStartedUtc">When the wait began.</param>
        /// <returns>A phrase to append to the message, or an empty string for a wait too short to be worth reporting.</returns>
        static string DescribeElapsedWait(DateTime waitStartedUtc)
        {
            var elapsed = DateTime.UtcNow - waitStartedUtc;
            if (elapsed < TimeSpan.FromMinutes(1))
            {
                return string.Empty;
            }
            return elapsed < TimeSpan.FromHours(1)
                ? $" (waiting for {elapsed.TotalMinutes:N0} minute(s) so far)"
                : $" (waiting for {elapsed.TotalHours:N1} hour(s) so far)";
        }
}
}
