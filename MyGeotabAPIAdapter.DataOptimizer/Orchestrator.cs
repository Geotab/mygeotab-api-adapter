using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Logging;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// The main <see cref="BackgroundService"/>. Handles orchestration tasks related to application initialization and restoration of connectivity. 
    /// </summary>
    class Orchestrator : BackgroundService
    {
        const int ConnectivityRestorationCheckIntervalMilliseconds = 10000;

        readonly IStateMachine stateMachine;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly UnitOfWorkContext context;

        /// <summary>
        /// Instantiates a new instance of the <see cref="Orchestrator"/> class.
        /// </summary>
        public Orchestrator(IStateMachine stateMachine, UnitOfWorkContext context)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.stateMachine = stateMachine;
            this.context = context;

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
                var assemblyName = GetType().Assembly.GetName().Name;
                var assemblyVersion = GetType().Assembly.GetName().Version.ToString();
                logger.Info($"******** INITIALIZING APPLICATION - {assemblyName} (v{assemblyVersion})");

                // Set state to normal so that other worker services may proceed.
                stateMachine.SetState(State.Normal, StateReason.NoReason);

                logger.Info("Initialization completed.");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Worker process caught an exception: \nMESSAGE [{ex.Message}]; \nSOURCE [{ex.Source}]; \nSTACK TRACE [{ex.StackTrace}]";
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

            logger.Info("Worker stopped.");
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
                    if (await stateMachine.IsAdapterDatabaseAccessibleAsync() == true)
                    {
                        logger.Info($"******** CONNECTIVITY RESTORED.");
                        stateMachine.SetState(State.Normal, StateReason.NoReason);
                    }
                }
                else if (stateMachine.Reason == StateReason.OptimizerDatabaseNotAvailable)
                {
                    using (var uow = context.CreateUnitOfWork(Databases.OptimizerDatabase))
                    {
                        if (await stateMachine.IsOptimizerDatabaseAccessibleAsync(context) == true)
                        {
                            logger.Info($"******** CONNECTIVITY RESTORED.");
                            stateMachine.SetState(State.Normal, StateReason.NoReason);
                        }
                    }
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
