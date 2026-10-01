using MyGeotabAPIAdapter.Database.Models;
using NLog;

namespace GeotabDIGAdapter
{
    /// <summary>
    /// A class that handles waiting for various reasons on behalf of a <see cref="BackgroundService"/>.
    /// </summary>
    class BackgroundServiceAwaiter<T> : IBackgroundServiceAwaiter<T> where T : BackgroundService
    {
        const int DatabaseMaintenanceCheckIntervalMilliseconds = 5000;
        const int StateMachineCheckIntervalMilliseconds = 1000;

        readonly IPrerequisiteServiceChecker<DbGdaOServiceTracking> prerequisiteServiceChecker;
        readonly IStateMachine<DbGdaMiddlewareVersionInfo> stateMachine;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public bool ServiceIsPausedForDatabaseMaintenance { get; private set; }

        /// <inheritdoc/>
        public string ServiceName => typeof(T).Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundServiceAwaiter{T}"/> class.
        /// </summary>
        public BackgroundServiceAwaiter(IPrerequisiteServiceChecker<DbGdaOServiceTracking> prerequisiteServiceChecker, IStateMachine<DbGdaMiddlewareVersionInfo> stateMachine)
        {
            this.prerequisiteServiceChecker = prerequisiteServiceChecker;
            this.stateMachine = stateMachine;
        }

        /// <summary>
        /// Indicates whether connectivity is lost in a way that should pause this service.
        /// <para>
        /// Deliberately narrower than "the state machine is Waiting". MyAdminNotAvailable and DIGNotAvailable
        /// are excluded on purpose: neither stops a service from draining its queues, and pausing on them would
        /// turn a MyAdmin outage into a telemetry outage. The Orchestrator owns recovering from those two.
        /// </para>
        /// </summary>
        bool IsConnectivityLost()
        {
            return stateMachine.CurrentState == State.Waiting && (stateMachine.IsStateReasonActive(StateReason.AdapterDatabaseNotAvailable) || stateMachine.IsStateReasonActive(StateReason.MyGeotabNotAvailable));
        }

        /// <summary>
        /// Indicates whether database maintenance is in progress.
        /// </summary>
        bool IsDatabaseMaintenanceInProgress()
        {
            return stateMachine.CurrentState == State.Waiting && stateMachine.IsStateReasonActive(StateReason.AdapterDatabaseMaintenance);
        }

        /// <inheritdoc/>
        public async Task WaitForConfiguredIntervalAsync(TimeSpan delayTimeSpan, DelayIntervalType intervalType, CancellationToken cancellationToken)
        {
            var intervalTypeString = intervalType.ToString().ToLower();
            var stateMachineCheckInterval = TimeSpan.FromMilliseconds(StateMachineCheckIntervalMilliseconds);
            var elapsedTimeSpan = TimeSpan.Zero;

            if (delayTimeSpan < TimeSpan.FromSeconds(30))
            {
                logger.Debug($"{ServiceName} pausing for the configured {intervalTypeString} interval ({delayTimeSpan}).");
            }
            else 
            {
                logger.Info($"{ServiceName} pausing for the configured {intervalTypeString} interval ({delayTimeSpan}).");
            }

            while (elapsedTimeSpan < delayTimeSpan)
            {
                // Exit the delay early only for a state the service will actually act on. Breaking out on any
                // Waiting reason meant abandoning the delay for reasons that do not pause the service - see
                // IsConnectivityLost - so it immediately looped round and abandoned the next delay too,
                // spinning at zero delay with one log line per pass for as long as the reason stayed active.
                if (cancellationToken.IsCancellationRequested || IsConnectivityLost() || IsDatabaseMaintenanceInProgress())
                {
                    logger.Info($"{ServiceName} exiting {intervalTypeString} interval delay early due to state change or cancellation.");
                    break;
                }

                await Task.Delay(stateMachineCheckInterval, cancellationToken);
                elapsedTimeSpan += stateMachineCheckInterval;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> WaitForConnectivityRestorationIfNeededAsync(CancellationToken cancellationToken)
        {
            if (IsConnectivityLost())
            {
                try
                { 
                    logger.Info($"******** PAUSING SERVICE: {ServiceName} while waiting for connectivity restoration.");

                    while (IsConnectivityLost())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Delay(StateMachineCheckIntervalMilliseconds, cancellationToken);
                    }
                    return true;
                }
                catch (OperationCanceledException)
                {
                    logger.Warn($"******** CANCELLATION REQUESTED while {ServiceName} is waiting for connectivity restoration.");
                }
                finally
                {
                    logger.Info($"******** RESUMING SERVICE: {ServiceName} after connectivity restoration.");
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public async Task WaitForDatabaseMaintenanceCompletionIfNeededAsync(CancellationToken cancellationToken)
        {
            if (IsDatabaseMaintenanceInProgress())
            {
                try
                {
                    // Report the pause to the state machine, not just to this instance. The service that
                    // requested the pause waits on GetServicesNotYetPausedForDbMaintenance, so a service that
                    // pauses without saying so is indistinguishable from one that ignored the request: the
                    // handshake could never complete and every maintenance run would time out and be skipped.
                    stateMachine.UpdateServicePauseStatus(ServiceName, true);
                    ServiceIsPausedForDatabaseMaintenance = true;
                    logger.Info($"******** PAUSING SERVICE: {ServiceName} for database maintenance.");

                    while (IsDatabaseMaintenanceInProgress())
                    {
                        // Confirmed again at every check, having just seen that maintenance is still in progress - not only on
                        // entering the pause. Each new pause request resets every service's status to "not paused", and one
                        // that follows the last so closely that this service never saw the release in between - as when
                        // Level 2 pauses for its offline rebuilds straight after partition management and retention - would
                        // otherwise wait out its whole timeout for a confirmation this service had no reason to send again.
                        stateMachine.UpdateServicePauseStatus(ServiceName, true);
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Delay(DatabaseMaintenanceCheckIntervalMilliseconds, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.Warn($"******** CANCELLATION REQUESTED while {ServiceName} is paused for database maintenance.");
                }
                finally
                {
                    stateMachine.UpdateServicePauseStatus(ServiceName, false);
                    ServiceIsPausedForDatabaseMaintenance = false;
                    logger.Info($"******** RESUMING SERVICE: {ServiceName} after database maintenance.");
                }
            }
        }

        /// <inheritdoc/>
        public async Task WaitForPrerequisiteServicesIfNeededAsync(List<DIGAdapterService> prerequisiteServices, CancellationToken cancellationToken)
        {
            await prerequisiteServiceChecker.WaitForPrerequisiteServicesIfNeededAsync(ServiceName, prerequisiteServices, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task WaitForPrerequisiteServiceToProcessEntitiesAsync(DIGAdapterService prerequisiteService, CancellationToken cancellationToken)
        {
            await prerequisiteServiceChecker.WaitForPrerequisiteServiceToProcessEntitiesAsync(ServiceName, prerequisiteService, cancellationToken);
        }
    }
}
