using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A class that handles waiting for various reasons on behalf of a <see cref="BackgroundService"/>.
    /// </summary>
    class BackgroundServiceAwaiter<T> : IBackgroundServiceAwaiter<T> where T : BackgroundService
    {
        const int DatabaseMaintenanceCheckIntervalMilliseconds = 5000;
        const int StateMachineCheckIntervalMilliseconds = 1000;

        readonly IPrerequisiteServiceChecker<DbOServiceTracking2> prerequisiteServiceChecker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public bool ServiceIsPausedForDatabaseMaintenance { get; private set; }

        /// <inheritdoc/>
        public string ServiceName => typeof(T).Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundServiceAwaiter<T>"/> class.
        /// </summary>
        public BackgroundServiceAwaiter(IPrerequisiteServiceChecker<DbOServiceTracking2> prerequisiteServiceChecker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine)
        {
            this.prerequisiteServiceChecker = prerequisiteServiceChecker;
            this.stateMachine = stateMachine;
        }

        /// <summary>
        /// Indicates whether connectivity is lost to either the MyGeotab API or the adapter database.
        /// </summary>
        bool IsConnectivityLost()
        {
            return stateMachine.CurrentState == State.Waiting && (stateMachine.IsStateReasonActive(StateReason.AdapterDatabaseNotAvailable) || stateMachine.IsStateReasonActive(StateReason.MyGeotabNotAvailable));
        }

        /// <summary>
        /// Indicates whether the database maintenance is in progress.
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
                // Exit the delay early if cancellation is requested or state changes to waiting for any reason.
                if (cancellationToken.IsCancellationRequested || stateMachine.CurrentState == State.Waiting)
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
                    stateMachine.UpdateServicePauseStatus(ServiceName, true);
                    ServiceIsPausedForDatabaseMaintenance = true;
                    logger.Info($"******** PAUSING SERVICE: {ServiceName} for database maintenance.");

                    while (IsDatabaseMaintenanceInProgress())
                    {
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
        public async Task WaitForPrerequisiteServicesIfNeededAsync(List<AdapterService> prerequisiteServices, CancellationToken cancellationToken)
        {
            await prerequisiteServiceChecker.WaitForPrerequisiteServicesIfNeededAsync(ServiceName, prerequisiteServices, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task WaitForPrerequisiteServiceToProcessEntitiesAsync(AdapterService prerequisiteService, CancellationToken cancellationToken)
        {
            await prerequisiteServiceChecker.WaitForPrerequisiteServiceToProcessEntitiesAsync(ServiceName, prerequisiteService, cancellationToken);
        }
    }
}
