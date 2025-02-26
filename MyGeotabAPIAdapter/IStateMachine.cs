using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a class that helps to keep track of overall application state with respect to MyGeotab API and database connectivity.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IDbMyGeotabVersionInfo"/> implementation to be used.</typeparam>
    interface IStateMachine<T> where T : class, IDbMyGeotabVersionInfo
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
        /// The <see cref="StateReason"/> for the current <see cref="State"/> of the <see cref="StateMachine"/> instance.
        /// </summary>
        StateReason Reason { get; }

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
        /// Sets the <see cref="State"/> and <see cref="StateReason"/> properties of the current <see cref="IStateMachine"/> instance.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="stateReason"></param>
        void SetState(State state, StateReason stateReason);
    }
}
