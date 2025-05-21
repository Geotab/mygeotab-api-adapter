using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a class that handles waiting for various reasons on behalf of a <see cref="BackgroundService"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="BackgroundService"/> represented by this <see cref="IBackgroundServiceAwaiter{T}"/> implementation.</typeparam>
    interface IBackgroundServiceAwaiter<T> where T : BackgroundService
    {
        /// <summary>
        /// Indicates whether the service is paused for database maintenance.
        /// </summary>
        bool ServiceIsPausedForDatabaseMaintenance { get; }

        /// <summary>
        /// The name of the service.
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Adds a delay equivalent to the <paramref name="delayTimeSpan"/>. The delay will be interrupted if StateMachine State changes to <see cref="State.Waiting"/> for any reason or if cancellation is requested.
        /// </summary>
        /// <param name="delayTimeSpan">The <see cref="TimeSpan"/> representing the delay interval.</param>
        /// <param name="intervalType">The <see cref="DelayIntervalType"/> for use in log messages.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        Task WaitForConfiguredIntervalAsync(TimeSpan delayTimeSpan, DelayIntervalType intervalType, CancellationToken cancellationToken);

        /// <summary>
        /// Waits for the restoration of connectivity to the MyGeotab API or to the adapter database if connecticity to either is lost, effectively pausing the subject service, <see cref="T"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> if wait was needed; otherwise, <c>false</c></returns>
        Task<bool> WaitForConnectivityRestorationIfNeededAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Waits for the completion of database maintenance (if underway), effectively pausing the subject service, <see cref="T"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        Task WaitForDatabaseMaintenanceCompletionIfNeededAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Checks whether any prerequisite services have been run and are currently running. If any of prerequisite services have not yet been run or are not currently running, details will be logged and the subject service, <see cref="T"/>, will pause operation, repeating this check intermittently until all prerequisite services are running.
        /// </summary>
        /// <param name="prerequisiteServices">A list of services upon which the dependent service depends.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        Task WaitForPrerequisiteServicesIfNeededAsync(List<AdapterService> prerequisiteServices, CancellationToken cancellationToken);

        /// <summary>
        /// Waits for the <paramref name="prerequisiteService"/> to log its next activity from the DateTime at which this method is called. Checks every minute until the service has logged its next activity.
        /// </summary>
        /// <param name="prerequisiteService">The prerequisite service upon which to wait.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        Task WaitForPrerequisiteServiceToProcessEntitiesAsync(AdapterService prerequisiteService, CancellationToken cancellationToken);
    }
}
