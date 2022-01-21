using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Logic;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// A class that helps to keep track of overall application state with respect to MyGeotab API and database connectivity.
    /// </summary>
    class StateMachine : IStateMachine
    {
        readonly IConnectionInfoContainer connectionInfoContainer;
        readonly IDataOptimizerConfiguration dataOptimizerConfiguration;

        /// <inheritdoc/>
        public State CurrentState { get; private set; }

        /// <inheritdoc/>
        public StateReason Reason { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachine"/> class.
        /// </summary>
        public StateMachine(IDataOptimizerConfiguration dataOptimizerConfiguration, IConnectionInfoContainer connectionInfoContainer)
        {
            this.dataOptimizerConfiguration = dataOptimizerConfiguration;
            this.connectionInfoContainer = connectionInfoContainer;
            this.SetState(State.Normal, StateReason.ApplicationNotInitialized);
        }

        /// <inheritdoc/>
        public async Task<bool> IsAdapterDatabaseAccessibleAsync()
        {
            try
            {
                // Attempt a call that retrieves data from the database. If successful, database is accessible. If an exception is encountered, the database will be deemed inaccessible.
                var dbMyGeotabVersionInfos = await DbMyGeotabVersionInfoService.GetAllAsync(connectionInfoContainer.AdapterDatabaseConnectionInfo, dataOptimizerConfiguration.TimeoutSecondsForDatabaseTasks);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsOptimizerDatabaseAccessibleAsync(UnitOfWorkContext context)
        {
            try
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    // Attempt a call that retrieves data from the database. If successful, database is accessible. If an exception is encountered, the database will be deemed inaccessible.
                    var dbOProcessorTrackingRepo = new DbOProcessorTrackingRepository(context);
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
            this.Reason = stateReason;
            this.CurrentState = state;
        }
    }
}
