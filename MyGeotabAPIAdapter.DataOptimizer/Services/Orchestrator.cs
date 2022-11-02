using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using NLog;
using Polly;
using Polly.Retry;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.DataOptimizer.Services
{
    /// <summary>
    /// The main <see cref="BackgroundService"/>. Handles orchestration tasks related to application initialization and restoration of connectivity. 
    /// </summary>
    class Orchestrator : BackgroundService
    {
        string AssemblyName { get => GetType().Assembly.GetName().Name; }
        string AssemblyVersion { get => GetType().Assembly.GetName().Version.ToString(); }
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        const int ConnectivityRestorationCheckIntervalMilliseconds = 10000;

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IOrchestratorServiceTracker orchestratorServiceTracker;
        readonly IStateMachine stateMachine;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;
        readonly IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> optimizerContext;

        /// <summary>
        /// Instantiates a new instance of the <see cref="Orchestrator"/> class.
        /// </summary>
        public Orchestrator(IOrchestratorServiceTracker orchestratorServiceTracker, IStateMachine stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext, IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> optimizerContext)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.orchestratorServiceTracker = orchestratorServiceTracker;
            this.stateMachine = stateMachine;
            this.adapterContext = adapterContext;
            this.optimizerContext = optimizerContext;

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(MaxRetries, logger);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Iteratively executes the business logic until the application is stopped.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (stateMachine.CurrentState == State.Waiting && stateMachine.Reason == StateReason.ApplicationNotInitialized)
                {
                    PerformInitializationTasks();
                    continue;
                }

                if (stateMachine.CurrentState == State.Waiting && (stateMachine.Reason == StateReason.AdapterDatabaseNotAvailable || stateMachine.Reason == StateReason.OptimizerDatabaseNotAvailable))
                {
                    await WaitForConnectivityRestorationAsync();
                }
                await Task.Delay(10000, stoppingToken);
            }
        }

        /// <summary>
        /// Performs startup tasks.
        /// </summary>
        /// <returns></returns>
        void PerformInitializationTasks()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            try
            {
                // Log application start-up.
                logger.Info($"******** INITIALIZING APPLICATION - {AssemblyName} (v{AssemblyVersion})");

                // Set state to normal so that other worker services may proceed.
                stateMachine.SetState(State.Normal, StateReason.NoReason);

                // Let other services know that the Orchestrator has been initialized.
                orchestratorServiceTracker.OrchestratorServiceInitialized = true;

                logger.Info("Initialization completed.");
            }
            catch (Exception ex)
            {
                string errorMessage = $"{DefaultErrorMessagePrefix}\nMESSAGE [{ex.Message}]; \nSOURCE [{ex.Source}]; \nSTACK TRACE [{ex.StackTrace}]";
                logger.Error(errorMessage);
                throw new Exception(errorMessage, ex);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Starts the current <see cref="Orchestrator"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Info($"******** STARTING SERVICE: {CurrentClassName}");
            await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Stops the current <see cref="Orchestrator"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            orchestratorServiceTracker.OrchestratorServiceInitialized = false;

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Repeatedly tests for connectivity until it is restored.
        /// </summary>
        /// <returns></returns>
        async Task WaitForConnectivityRestorationAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Warn($"******** CONNECTIVITY LOST. REASON: '{stateMachine.Reason}'. WAITING FOR RESTORATION OF CONNECTIVITY...");

            while (stateMachine.CurrentState == State.Waiting)
            {
                // Wait for the prescribed interval between connectivity checks.
                await Task.Delay(ConnectivityRestorationCheckIntervalMilliseconds);

                if (stateMachine.Reason == StateReason.AdapterDatabaseNotAvailable)
                {
                    await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                    {
                        using (var uow = optimizerContext.CreateUnitOfWork(Databases.AdapterDatabase))
                        {
                            if (await stateMachine.IsAdapterDatabaseAccessibleAsync(adapterContext) == true)
                            {
                                logger.Info($"******** CONNECTIVITY RESTORED.");
                                stateMachine.SetState(State.Normal, StateReason.NoReason);
                            }
                        }
                    }, new Context());
                }
                else if (stateMachine.Reason == StateReason.OptimizerDatabaseNotAvailable)
                {
                    await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                    {
                        using (var uow = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                        {
                            if (await stateMachine.IsOptimizerDatabaseAccessibleAsync(optimizerContext) == true)
                            {
                                logger.Info($"******** CONNECTIVITY RESTORED.");
                                stateMachine.SetState(State.Normal, StateReason.NoReason);
                            }
                        }
                    }, new Context());
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
