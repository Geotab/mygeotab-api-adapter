using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// A class that helps to keep track of overall application state with respect to database connectivity.
    /// </summary>
    class StateMachine : IStateMachine
    {
        /// <inheritdoc/>
        public State CurrentState { get; private set; }

        /// <inheritdoc/>
        public StateReason Reason { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachine"/> class.
        /// </summary>
        public StateMachine()
        {
            SetState(State.Waiting, StateReason.ApplicationNotInitialized);
        }

        /// <inheritdoc/>
        public async Task<bool> IsAdapterDatabaseAccessibleAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context)
        {
            try
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    // Attempt a call that retrieves data from the database. If successful, database is accessible. If an exception is encountered, the database will be deemed inaccessible.
                    var dbMyGeotabVersionInfoRepo = new BaseRepository<DbMyGeotabVersionInfo>(context);
                    var dbMyGeotabVersionInfos = await dbMyGeotabVersionInfoRepo.GetAllAsync(cancellationTokenSource);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsOptimizerDatabaseAccessibleAsync(IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> context)
        {
            try
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    // Attempt a call that retrieves data from the database. If successful, database is accessible. If an exception is encountered, the database will be deemed inaccessible.
                    var dbOProcessorTrackingRepo = new BaseRepository<DbOProcessorTracking>(context);
                    var returnedDbOProcessorTrackings = await dbOProcessorTrackingRepo.GetAllAsync(cancellationTokenSource);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public void SetState(State state, StateReason stateReason)
        {
            if (state == State.Normal)
            {
                stateReason = StateReason.NoReason;
            }
            Reason = stateReason;
            CurrentState = state;
        }
    }
}
