using MyGeotabAPIAdapter.Database.DataAccess;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// Interface for a class that helps to keep track of overall application state with respect to database connectivity.
    /// </summary>
    interface IStateMachine
    {
        /// <summary>
        /// The current <see cref="State"/> of the <see cref="StateMachine"/> instance.
        /// </summary>
        State CurrentState { get; }

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
        /// Indicates whether the optimizer database is accessible.
        /// </summary>
        /// <param name="context">The <see cref="OptimizerDatabaseUnitOfWorkContext"/> to use.</param>
        /// <returns></returns>
        Task<bool> IsOptimizerDatabaseAccessibleAsync(IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> context);

        /// <summary>
        /// Sets the <see cref="State"/> and <see cref="StateReason"/> properties of the current <see cref="IStateMachine"/> instance.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="stateReason"></param>
        void SetState(State state, StateReason stateReason);
    }

    /// <summary>
    /// A list of possible application states for use by the <see cref="IStateMachine"/>.
    /// </summary>
    public enum State { Normal, Waiting }

    /// <summary>
    /// A list of possible reasons for the current <see cref="State"/> of the <see cref="IStateMachine"/>.
    /// </summary>
    public enum StateReason { ApplicationNotInitialized, AdapterDatabaseNotAvailable, OptimizerDatabaseNotAvailable, NoReason }
}
