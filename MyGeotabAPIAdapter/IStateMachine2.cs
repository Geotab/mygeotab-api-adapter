using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a class that helps to keep track of overall application state with respect to MyGeotab API and database connectivity, pauses for database maintenance, etc.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IDbMyGeotabVersionInfo"/> implementation to be used.</typeparam>
    interface IStateMachine2<T> where T : class, IDbMyGeotabVersionInfo
    {
        /// <summary>
        /// The current <see cref="State"/> of the <see cref="StateMachine"/> instance.
        /// </summary>
        State CurrentState { get; }

        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// A list of any and all active <see cref="StateReason"/>s for the <see cref="CurrentState"/>.
        /// </summary>
        /// <returns></returns>
        IReadOnlyCollection<StateReason> GetActiveStateReasons();

        /// <summary>
        /// Gets a comma-separated list of services that have registered, are participating in pauses for DB maintenancem, but are not paused. If all services that are participating in pauses for DB maintenance have registered and are paused, an empty string is returned.
        /// </summary>
        /// <returns></returns>
        string GetServicesNotYetPausedForDbMaintenance();

        /// <summary>
        /// Gets a comma-separated list of services that have not yet been registered. If all services have been registered, an empty string is returned.
        /// </summary>
        /// <returns></returns>
        string GetServicesNotYetRegistered();

        /// <summary>
        /// Gets the pause statuses of all services.
        /// </summary>
        /// <returns>A dictionary containing the pause statuses of all services.</returns>
        IReadOnlyDictionary<string, bool> GetServicePauseStatuses();

        /// <summary>
        /// If the <paramref name="exception"/> is connectivity-related (i.e. <see cref="Exceptions.AdapterDatabaseConnectionException"/> or <see cref="Exceptions.MyGeotabConnectionException"/>), changes the <see cref="StateReason"/> of the current <see cref="IStateMachine{T}"/> instance to reflect the connectivity issue. The orchestrator service should then assume responsibility for re-establishing connectivity while othwr services should pause and wait for connectivity restortaion. If the <paramref name="exception"/> is <see cref="NLogLogLevelName.Fatal"/> (i.e. unhandled exception), the application will be killed.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to be handled.</param>
        /// <param name="logLevel">The log level associated with the <paramref name="exception"/>.</param>
        void HandleException(Exception exception, NLogLogLevelName logLevel);

        /// <summary>
        /// Indicates whether the adapter database is accessible.
        /// </summary>
        /// <param name="context">The <see cref="AdapterDatabaseUnitOfWorkContext"/> to use.</param>
        /// <returns></returns>
        Task<bool> IsAdapterDatabaseAccessibleAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context);

        /// <summary>
        /// Indicates whether the MyGeotab API is accessible.
        /// </summary>
        /// <returns></returns>
        Task<bool> IsMyGeotabAccessibleAsync();

        /// <summary>
        /// Indicates whether the specified <see cref="StateReason"/> is currently active.
        /// </summary>
        /// <param name="stateReason">The <see cref="StateReason"/> to check.</param>
        /// <returns><c>true</c> if the specified reason is active; otherwise, <c>false</c>.</returns>
        public bool IsStateReasonActive(StateReason stateReason);

        /// <summary>
        /// Registers a service with the StateMachine and indicates whether the service will pause for database maintenance. If the service was already registered, the participation in database maintenance pauses is updated.
        /// </summary>
        /// <param name="serviceName">The name of the service to be registered.</param>
        /// <param name="participateInDatabaseMaintenancePauses">Indicates whether the service will pause for database maintenance.</param>
        void RegisterService(string serviceName, bool participateInDatabaseMaintenancePauses);

        /// <summary>
        /// Updates the state reason and its active status in the state machine.
        /// </summary>
        /// <param name="stateReason">The <see cref="StateReason"/> to activate or deactivate.</param>
        /// <param name="isActive">
        /// Indincates whether the specified <paramref name="stateReason"/> should be active or inactive. 
        /// If <c>true</c>, the reason is added to the active state reasons; if <c>false</c>, it is removed.
        /// </param>
        /// <remarks>
        /// This method updates the internal collection of active state reasons. If a state reason is set to active, 
        /// it is added to the <c>activeStateReasons</c> collection. If it is set to inactive, it is removed from the collection.
        /// After modifying the collection, the <see cref="UpdateState"/> method is called to recalculate the overall state 
        /// based on the current active state reasons.
        /// </remarks>
        /// <example>
        /// Activating a state reason:
        /// <code>
        /// stateMachine.SetStateReason(StateReason.ApplicationNotInitialized, true);
        /// </code>
        /// Deactivating a state reason:
        /// <code>
        /// stateMachine.SetStateReason(StateReason.ApplicationNotInitialized, false);
        /// </code>
        /// </example>
        void SetStateReason(StateReason stateReason, bool isActive);

        /// <summary>
        /// Updates the pause status of a service.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="isPaused">Indicates whether the service is paused.</param>
        void UpdateServicePauseStatus(string serviceName, bool isPaused);
    }
}


#region ORIGINAL IStateMachine.cs
//using MyGeotabAPIAdapter.Database.DataAccess;
//using MyGeotabAPIAdapter.Database.Models;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace MyGeotabAPIAdapter
//{
//    /// <summary>
//    /// Interface for a class that helps to keep track of overall application state with respect to MyGeotab API and database connectivity.
//    /// </summary>
//    /// <typeparam name="T">The type of <see cref="IDbMyGeotabVersionInfo"/> implementation to be used.</typeparam>
//    interface IStateMachine<T> where T : class, IDbMyGeotabVersionInfo
//    {
//        /// <summary>
//        /// The current <see cref="State"/> of the <see cref="StateMachine"/> instance.
//        /// </summary>
//        State CurrentState { get; }

//        /// <summary>
//        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
//        /// </summary>
//        string Id { get; }

//        /// <summary>
//        /// The <see cref="StateReason"/> for the current <see cref="State"/> of the <see cref="StateMachine"/> instance.
//        /// </summary>
//        StateReason Reason { get; }

//        /// <summary>
//        /// Gets a comma-separated list of services that have registered, are participating in pauses for DB maintenancem, but are not paused. If all services that are participating in pauses for DB maintenance have registered and are paused, an empty string is returned.
//        /// </summary>
//        /// <returns></returns>
//        string GetServicesNotYetPausedForDbMaintenance();

//        /// <summary>
//        /// Gets a comma-separated list of services that have not yet been registered. If all services have been registered, an empty string is returned.
//        /// </summary>
//        /// <returns></returns>
//        string GetServicesNotYetRegistered();

//        /// <summary>
//        /// Gets the pause statuses of all services.
//        /// </summary>
//        /// <returns>A dictionary containing the pause statuses of all services.</returns>
//        IReadOnlyDictionary<string, bool> GetServicePauseStatuses();

//        /// <summary>
//        /// Indicates whether the adapter database is accessible.
//        /// </summary>
//        /// <param name="context">The <see cref="AdapterDatabaseUnitOfWorkContext"/> to use.</param>
//        /// <returns></returns>
//        Task<bool> IsAdapterDatabaseAccessibleAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context);

//        /// <summary>
//        /// Indicates whether the MyGeotab API is accessible.
//        /// </summary>
//        /// <returns></returns>
//        Task<bool> IsMyGeotabAccessibleAsync();

//        /// <summary>
//        /// Registers a service with the StateMachine and indicates whether the service will pause for database maintenance. If the service was already registered, the participation in database maintenance pauses is updated.
//        /// </summary>
//        /// <param name="serviceName">The name of the service to be registered.</param>
//        /// <param name="participateInDatabaseMaintenancePauses">Indicates whether the service will pause for database maintenance.</param>
//        void RegisterService(string serviceName, bool participateInDatabaseMaintenancePauses);

//        /// <summary>
//        /// Sets the <see cref="State"/> and <see cref="StateReason"/> properties of the current <see cref="IStateMachine"/> instance.
//        /// </summary>
//        /// <param name="newState"></param>
//        /// <param name="stateReason"></param>
//        void SetState(State newState, StateReason stateReason);

//        /// <summary>
//        /// Updates the pause status of a service.
//        /// </summary>
//        /// <param name="serviceName">The name of the service.</param>
//        /// <param name="isPaused">Indicates whether the service is paused.</param>
//        void UpdateServicePauseStatus(string serviceName, bool isPaused);
//    }

//    /// <summary>
//    /// A list of possible application states for use by the <see cref="IStateMachine"/>.
//    /// </summary>
//    public enum State { Normal, Waiting }

//    /// <summary>
//    /// A list of possible reasons for the current <see cref="State"/> of the <see cref="IStateMachine"/>.
//    /// </summary>
//    public enum StateReason { ApplicationNotInitialized, AdapterDatabaseMaintenance, AdapterDatabaseNotAvailable, MyGeotabNotAvailable, NoReason }
//}
#endregion
