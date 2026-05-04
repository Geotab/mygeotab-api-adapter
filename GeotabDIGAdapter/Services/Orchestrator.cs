using MyGeotabAPIAdapter;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.MyAdminAPI;
using NLog;
using Polly;
using Polly.Retry;

namespace GeotabDIGAdapter.Services
{
    /// <summary>
    /// The main <see cref="BackgroundService"/>. Handles orchestration tasks related to application initialization and restoration of connectivity. 
    /// </summary>
    class Orchestrator : BackgroundService
    {
        string AssemblyName { get => GetType().Assembly.GetName().Name ?? string.Empty; }
        string AssemblyVersion { get => GetType().Assembly.GetName().Version?.ToString() ?? string.Empty; }
        string CurrentClassName { get => $"{AssemblyName}.{GetType().Name} (v{AssemblyVersion})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        const int ConnectivityRestorationCheckIntervalMilliseconds = 10000;

        // Polly-related items:
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IDIGAdapterConfiguration adapterConfiguration;
        readonly IDIGAPIHelper digAPIHelper;
        readonly IExceptionHelper exceptionHelper;
        readonly IMyAdminAPIHelper myAdminAPIHelper;
        readonly IOrchestratorServiceTracker orchestratorServiceTracker;
        readonly IStateMachine<DbGdaMiddlewareVersionInfo> stateMachine;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context;

        /// <summary>
        /// Initializes a new instance of the <see cref="Orchestrator"/> class.
        /// </summary>
        public Orchestrator(IDIGAdapterConfiguration adapterConfiguration, IDIGAPIHelper digAPIHelper, IExceptionHelper exceptionHelper, IMyAdminAPIHelper myAdminAPIHelper, IOrchestratorServiceTracker orchestratorServiceTracker, IStateMachine<DbGdaMiddlewareVersionInfo> stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.digAPIHelper = digAPIHelper;
            this.exceptionHelper = exceptionHelper;
            this.myAdminAPIHelper = myAdminAPIHelper;
            this.orchestratorServiceTracker = orchestratorServiceTracker;
            this.stateMachine = stateMachine;
            this.context = context;

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);
        }

        /// <summary>
        /// Iteratively executes the business logic until the application is stopped.
        /// </summary>
        /// <param name="stoppingToken">The <see cref="CancellationToken"/> that can be used to stop the service.</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Handle initialization if application is not yet initialized.
                if (stateMachine.CurrentState == State.Waiting && stateMachine.IsStateReasonActive(StateReason.ApplicationNotInitialized))
                {
                    await PerformInitializationTasksAsync();
                    continue;
                }

                // Handle connectivity restoration if database or external services are unavailable.
                if (stateMachine.CurrentState == State.Waiting && (stateMachine.IsStateReasonActive(StateReason.AdapterDatabaseNotAvailable) || stateMachine.IsStateReasonActive(StateReason.DIGNotAvailable) || stateMachine.IsStateReasonActive(StateReason.MyAdminNotAvailable) || stateMachine.IsStateReasonActive(StateReason.MyGeotabNotAvailable)))
                {
                    await WaitForConnectivityRestorationAsync();
                }

                await Task.Delay(10000, stoppingToken);
            }
        }

        /// <summary>
        /// Performs startup tasks.
        /// </summary>
        async Task PerformInitializationTasksAsync()
        {
            try
            {
                // Log application start-up.
                logger.Info($"******** INITIALIZING APPLICATION - {AssemblyName} (v{AssemblyVersion})");

                // Authenticate the MyAdmin API client.
                await myAdminAPIHelper.AuthenticateMyAdminAPIAsync(
                    adapterConfiguration.MyAdminAPIEndpoint,
                    adapterConfiguration.MyAdminUser,
                    adapterConfiguration.MyAdminPassword);

                //// Test the session expiry re-authentication policy
                //await myAdminAPIHelper.TestSessionExpiryReauthenticationAsync();

                // Authenticate with the DIG API.
                await digAPIHelper.AuthenticateDIGAPIAsync(
                    adapterConfiguration.DIGAPIEndpoint,
                    adapterConfiguration.DIGUser,
                    adapterConfiguration.DIGPassword,
                    adapterConfiguration.TimeoutSecondsForDIGTasks);

                // All authentication succeeded. Clear the ApplicationNotInitialized state reason so that other worker services may proceed.
                stateMachine.SetStateReason(StateReason.ApplicationNotInitialized, false);

                // Let other services know that the Orchestrator has been initialized.
                orchestratorServiceTracker.OrchestratorServiceInitialized = true;

                logger.Info($"Initialization of {AssemblyName} completed.");
            }
            catch (Exception ex)
            {
                string errorMessage = $"{DefaultErrorMessagePrefix}\nMESSAGE [{ex.Message}]; \nSOURCE [{ex.Source}]; \nSTACK TRACE [{ex.StackTrace}]";
                logger.Error(errorMessage);
                logger.Warn($"Initialization failed. Will retry in {ConnectivityRestorationCheckIntervalMilliseconds / 1000} seconds.");
                await Task.Delay(ConnectivityRestorationCheckIntervalMilliseconds);
            }
        }

        /// <summary>
        /// Starts the current <see cref="Orchestrator"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            // Register this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false as this service does not need to participate in pauses for database maintenance.
            stateMachine.RegisterService(nameof(Orchestrator), false);

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
            logger.Warn($"******** CONNECTIVITY LOST. REASON(S): '{string.Join(", ", stateMachine.GetActiveStateReasons())}'. WAITING FOR RESTORATION OF CONNECTIVITY...");

            while (stateMachine.CurrentState == State.Waiting)
            {
                // Wait for the prescribed interval between connectivity checks.
                await Task.Delay(ConnectivityRestorationCheckIntervalMilliseconds);

                if (stateMachine.IsStateReasonActive(StateReason.AdapterDatabaseNotAvailable))
                {
                    await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                    {
                        using (var uow = context.CreateUnitOfWork(Databases.AdapterDatabase))
                        {
                            if (await stateMachine.IsAdapterDatabaseAccessibleAsync(context) == true)
                            {
                                logger.Info($"******** CONNECTIVITY RESTORED.");
                                stateMachine.SetStateReason(StateReason.AdapterDatabaseNotAvailable, false);
                            }
                        }
                    }, new Context());
                }
                else if (stateMachine.IsStateReasonActive(StateReason.DIGNotAvailable))
                {
                    try
                    {
                        await digAPIHelper.AuthenticateDIGAPIAsync(
                            adapterConfiguration.DIGAPIEndpoint,
                            adapterConfiguration.DIGUser,
                            adapterConfiguration.DIGPassword,
                            adapterConfiguration.TimeoutSecondsForDIGTasks);

                        logger.Info($"******** CONNECTIVITY RESTORED.");
                        stateMachine.SetStateReason(StateReason.DIGNotAvailable, false);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"DIG API connectivity check failed: {ex.Message}. Will retry in {ConnectivityRestorationCheckIntervalMilliseconds / 1000} seconds.");
                    }
                }
                else if (stateMachine.IsStateReasonActive(StateReason.MyAdminNotAvailable))
                {
                    try
                    {
                        await myAdminAPIHelper.AuthenticateMyAdminAPIAsync(
                            adapterConfiguration.MyAdminAPIEndpoint,
                            adapterConfiguration.MyAdminUser,
                            adapterConfiguration.MyAdminPassword);

                        logger.Info($"******** CONNECTIVITY RESTORED.");
                        stateMachine.SetStateReason(StateReason.MyAdminNotAvailable, false);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"MyAdmin API connectivity check failed: {ex.Message}. Will retry in {ConnectivityRestorationCheckIntervalMilliseconds / 1000} seconds.");
                    }
                }
                else if (stateMachine.IsStateReasonActive(StateReason.MyGeotabNotAvailable))
                {
                    throw new NotSupportedException("MyGeotab API connectivity check is not implemented.");
                    // TODO (Maybe)
                    //if (await stateMachine.IsMyGeotabAccessibleAsync() == true)
                    //{
                    //    logger.Info($"******** CONNECTIVITY RESTORED.");
                    //    stateMachine.SetStateReason(StateReason.MyGeotabNotAvailable, false);
                    //}
                }
            }
        }
    }
}
