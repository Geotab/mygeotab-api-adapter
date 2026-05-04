using MyGeotabAPIAdapter;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Logging;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace GeotabDIGAdapter
{
    /// <summary>
    /// A class that helps to keep track of overall application state with respect to API and database connectivity, pauses for database maintenance, etc.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IDbGdaMiddlewareVersionInfo"/> implementation to be used.</typeparam>
    class StateMachine<T> : IStateMachine<T> where T : class, IDbGdaMiddlewareVersionInfo
    {
        readonly ConcurrentDictionary<StateReason, bool> activeStateReasons = new();
        readonly ConcurrentDictionary<string, bool> serviceDbMaintenancePauseParticipation;
        readonly ConcurrentDictionary<string, bool> serviceDbMaintenancePauseStatuses;
        readonly ConcurrentDictionary<string, bool> serviceRegistrations;
        string id;
        readonly ReaderWriterLockSlim _lock = new();
        State currentState = State.Normal;

        readonly IDIGAdapterConfiguration adapterConfiguration;

        /// <inheritdoc/>
        public State CurrentState
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return currentState;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            private set
            {
                // Write lock not needed here as this property is only set internally.
                currentState = value;
            }
        }

        /// <inheritdoc/>
        public string Id
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return id;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            private set
            {
                _lock.EnterWriteLock();
                try
                {
                    id = value;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachine{T}"/> class.
        /// </summary>
        public StateMachine(IDIGAdapterConfiguration adapterConfiguration, IEnumerable<ServiceOptions> serviceOptionsList)
        {
            Id = Guid.NewGuid().ToString();
            this.adapterConfiguration = adapterConfiguration;

            serviceDbMaintenancePauseParticipation = new ConcurrentDictionary<string, bool>();
            serviceDbMaintenancePauseStatuses = new ConcurrentDictionary<string, bool>();
            serviceRegistrations = new ConcurrentDictionary<string, bool>();
            foreach (var serviceOptions in serviceOptionsList)
            {
                // Set the registration and DB maintenance pause status for each service to false initially. All services must register, even if some are disabled and subsequently shut-down. This is to ensure that DB maintenance won't start before all services are ready.
                serviceRegistrations[serviceOptions.ServiceName] = false;
                serviceDbMaintenancePauseStatuses[serviceOptions.ServiceName] = false;

                // Set the service's participation in DB maintenance pauses based on the ServiceOptions configured in Program.cs.
                serviceDbMaintenancePauseParticipation[serviceOptions.ServiceName] = serviceOptions.PauseForDatabaseMaintenance;
            }

            SetStateReason(StateReason.ApplicationNotInitialized, true);
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<StateReason> GetActiveStateReasons()
        {
            return activeStateReasons.Keys.ToList().AsReadOnly();
        }

        /// <inheritdoc/>
        public string GetServicesNotYetPausedForDbMaintenance()
        {
            return string.Join(", ", serviceRegistrations
                .Where(kvp => kvp.Value == true
                    && serviceDbMaintenancePauseParticipation.TryGetValue(kvp.Key, out bool participating)
                        && participating == true
                    && serviceDbMaintenancePauseStatuses.TryGetValue(kvp.Key, out bool paused)
                        && paused == false)
                .Select(kvp => kvp.Key));
        }

        /// <inheritdoc/>
        public string GetServicesNotYetRegistered()
        {
            return string.Join(", ", serviceRegistrations
                .Where(kvp => kvp.Value == false)
                .Select(kvp => kvp.Key));
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, bool> GetServicePauseStatuses()
        {
            return new ReadOnlyDictionary<string, bool>(serviceDbMaintenancePauseStatuses);
        }

        /// <inheritdoc/>
        public void HandleException(Exception exception, NLogLogLevelName logLevel)
        {
            if (exception is AdapterDatabaseConnectionException)
            {
                SetStateReason(StateReason.AdapterDatabaseNotAvailable, true);
            }
            else if (exception is MyGeotabConnectionException)
            {
                SetStateReason(StateReason.MyGeotabNotAvailable, true);
            }
            else if (exception is MyAdminConnectionException)
            {
                SetStateReason(StateReason.MyAdminNotAvailable, true);
            }
            else if (exception is DIGConnectionException)
            {
                SetStateReason(StateReason.DIGNotAvailable, true);
            }

            if (logLevel == NLogLogLevelName.Fatal)
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsAdapterDatabaseAccessibleAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context)
        {
            try
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    // Attempt a call that retrieves data from the database. If successful, database is accessible. If an exception is encountered, the database will be deemed inaccessible.
                    var dbGdaMiddlewareVersionInfoRepo = new BaseRepository<T>(context);
                    var dbGdaMiddlewareVersionInfos = await dbGdaMiddlewareVersionInfoRepo.GetAllAsync(cancellationTokenSource);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public bool IsStateReasonActive(StateReason stateReason)
        {
            return activeStateReasons.ContainsKey(stateReason);
        }

        /// <inheritdoc/>
        public void RegisterService(string serviceName, bool participateInDatabaseMaintenancePauses)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentException($"{serviceName} name cannot be null or whitespace.");
            }

            // Register the service to indicate that it has started and is ready, if necessary, to participate in database maintenance pauses.
            serviceRegistrations[serviceName] = true;

            // Update the service's participation in DB maintenance pauses. Even though this is already set based on the ServiceOptions configured in Program.cs, some services may be disabled, in which case they will stop and no longer participate in DB maintenance pauses.
            serviceDbMaintenancePauseParticipation[serviceName] = participateInDatabaseMaintenancePauses;
        }

        /// <inheritdoc/>
        public void SetStateReason(StateReason stateReason, bool isActive)
        {
            if (isActive)
            {
                activeStateReasons[stateReason] = true;
            }
            else
            {
                activeStateReasons.TryRemove(stateReason, out _);
            }

            UpdateState();

            // If the state reason is "AdapterDatabaseMaintenance" and it is being activated, reset all service statuses to "not paused".
            if (CurrentState == State.Waiting && stateReason == StateReason.AdapterDatabaseMaintenance && isActive == true)
            {
                foreach (var service in serviceDbMaintenancePauseStatuses.Keys)
                {
                    serviceDbMaintenancePauseStatuses[service] = false;
                }
            }
        }

        /// <inheritdoc/>
        public void UpdateServicePauseStatus(string serviceName, bool isPaused)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentException($"{serviceName} name cannot be null or whitespace.");
            }

            serviceDbMaintenancePauseStatuses[serviceName] = isPaused;
        }

        /// <summary>
        /// Updates the current state of the state machine based on active state reasons.
        /// </summary>
        /// <remarks>
        /// This method determines the overall <see cref="CurrentState"/> of the state machine by evaluating 
        /// whether there are any active state reasons in the <c>activeStateReasons</c> collection. 
        /// If there are active state reasons, the state is set to <see cref="State.Waiting"/>; 
        /// otherwise, it is set to <see cref="State.Normal"/>.
        /// 
        /// The method uses a thread-safe approach by locking the <c>_lock</c> object during the state update 
        /// to ensure consistent state transitions in a multithreaded environment.
        /// </remarks>
        void UpdateState()
        {
            _lock.EnterWriteLock();
            try
            {
                if (activeStateReasons.IsEmpty == false)
                {
                    CurrentState = State.Waiting;
                }
                else
                {
                    CurrentState = State.Normal;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
