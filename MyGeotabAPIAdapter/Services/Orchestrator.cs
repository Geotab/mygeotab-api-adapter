using Geotab.Checkmate.ObjectModel;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.MyGeotabAPI;
using NLog;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Services
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

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbMyGeotabVersionInfo> dbMyGeotabVersionInfoEntityPersister;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IOrchestratorServiceTracker orchestratorServiceTracker;
        readonly IStateMachine stateMachine;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context;

        /// <summary>
        /// Instantiates a new instance of the <see cref="Orchestrator"/> class.
        /// </summary>
        public Orchestrator(IAdapterConfiguration adapterConfiguration, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbMyGeotabVersionInfo> dbMyGeotabVersionInfoEntityPersister, IMyGeotabAPIHelper myGeotabAPIHelper, IOrchestratorServiceTracker orchestratorServiceTracker, IStateMachine stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.adapterConfiguration = adapterConfiguration;
            this.exceptionHelper = exceptionHelper;
            this.dbMyGeotabVersionInfoEntityPersister = dbMyGeotabVersionInfoEntityPersister;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.orchestratorServiceTracker = orchestratorServiceTracker;
            this.stateMachine = stateMachine;
            this.context = context;

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

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

                if (stateMachine.CurrentState == State.Waiting && (stateMachine.Reason == StateReason.AdapterDatabaseNotAvailable || stateMachine.Reason == StateReason.MyGeotabNotAvailable))
                {
                    await WaitForConnectivityRestorationAsync();
                }
                await Task.Delay(10000, stoppingToken);
            }
        }

        /// <summary>
        /// Generates and logs an error message for the supplied <paramref name="exception"/>. If the <paramref name="exception"/> is connectivity-related, the <see cref="stateMachine"/> will have its <see cref="IStateMachine.CurrentState"/> and <see cref="IStateMachine.Reason"/> set accordingly. If the value supplied for <paramref name="logLevel"/> is <see cref="NLogLogLevelName.Fatal"/>, the current process will be killed.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <param name="logLevel">The <see cref="LogLevel"/> to be used when logging the error message.</param>
        /// <param name="errorMessagePrefix">The start of the error message, which will be followed by the <see cref="Exception.Message"/>, <see cref="Exception.Source"/> and <see cref="Exception.StackTrace"/>.</param>
        /// <returns></returns>
        void HandleException(Exception exception, NLogLogLevelName logLevel, string errorMessagePrefix)
        {
            exceptionHelper.LogException(exception, logLevel, errorMessagePrefix);
            if (exception is AdapterDatabaseConnectionException)
            {
                stateMachine.SetState(State.Waiting, StateReason.AdapterDatabaseNotAvailable);
            }
            else if (exception is MyGeotabConnectionException)
            {
                stateMachine.SetState(State.Waiting, StateReason.MyGeotabNotAvailable);
            }

            if (logLevel == NLogLogLevelName.Fatal)
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        /// Performs startup tasks.
        /// </summary>
        /// <returns></returns>
        async void PerformInitializationTasks()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            try
            {
                // Log application start-up.
                logger.Info($"******** INITIALIZING APPLICATION - {AssemblyName} (v{AssemblyVersion})");

                // Set state to normal so that other worker services may proceed.
                stateMachine.SetState(State.Normal, StateReason.NoReason);

                // Authenticate the MyGeotab API client and validate MyGeotab database.
                await myGeotabAPIHelper.AuthenticateMyGeotabApiAsync(adapterConfiguration.MyGeotabUser, adapterConfiguration.MyGeotabPassword, adapterConfiguration.MyGeotabDatabase, adapterConfiguration.MyGeotabServer, adapterConfiguration.TimeoutSecondsForMyGeotabTasks);
                await ValidateMyGeotabVersionInformationAsync();

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
        /// Obtains the <see cref="VersionInformation"/> for the current MyGeotab database, logs this information and performs validation to prevent mixing if data from multiple MyGeotab database in a single MyGeotab API Adapter database.
        /// </summary>
        /// <returns></returns>
        async Task ValidateMyGeotabVersionInformationAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var versionInformation = await myGeotabAPIHelper.GetVersionInformationAsync(adapterConfiguration.TimeoutSecondsForMyGeotabTasks);

            logger.Info($"Version information for MyGeotab database '{myGeotabAPIHelper.MyGeotabAPI.Database}' on server '{myGeotabAPIHelper.MyGeotabAPI.Server}': Server='{versionInformation.Application.Build}-{versionInformation.Application.Branch}-{versionInformation.Application.Commit}', Database='{versionInformation.Database}', GoTalk='{versionInformation.GoTalk}'");

            try
            {
                // Create a new DbMyGeotabVersionInfo entity with info from the VersionInformation of the current MyGeotab database.
                var newDbMyGeotabVersionInfo = new DbMyGeotabVersionInfo
                {
                    ApplicationBranch = versionInformation.Application.Branch,
                    ApplicationBuild = versionInformation.Application.Build,
                    ApplicationCommit = versionInformation.Application.Commit,
                    DatabaseName = myGeotabAPIHelper.MyGeotabAPI.Database,
                    DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                    DatabaseVersion = versionInformation.Database,
                    GoTalkVersion = versionInformation.GoTalk,
                    RecordCreationTimeUtc = DateTime.UtcNow,
                    Server = myGeotabAPIHelper.MyGeotabAPI.Server
                };

                // Get any existing DbMyGeotabVersionInfo entities.
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var dbMyGeotabVersionInfoRepo = new BaseRepository<DbMyGeotabVersionInfo>(context);
                    var dbMyGeotabVersionInfosToPersist = new List<DbMyGeotabVersionInfo>();

                    await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                    {
                        using (var uow = context.CreateUnitOfWork(context.Database))
                        {
                            var dbMyGeotabVersionInfos = await dbMyGeotabVersionInfoRepo.GetAllAsync(cancellationTokenSource);
                            if (!dbMyGeotabVersionInfos.Any())
                            {
                                // No existing DbMyGeotabVersionInfo entities were found; the new DbMyGeotabVersionInfo entity must be inserted into the database.
                                dbMyGeotabVersionInfosToPersist.Add(newDbMyGeotabVersionInfo);
                            }
                            else
                            {
                                // Get the latest DbMyGeotabVersionInfo from the database.
                                var latestDbMyGeotabVersionInfo = dbMyGeotabVersionInfos.Last();

                                // Throw an exception if name of the current MyGeotab database doesn't match that for which data has been written to the database.
                                if (newDbMyGeotabVersionInfo.DatabaseName != latestDbMyGeotabVersionInfo.DatabaseName)
                                {
                                    string errorMessage = $"The MyGeotab database to which this application is currently connected ('{newDbMyGeotabVersionInfo.DatabaseName}') does not match that for which data has been stored in the MyGeotab API Adapter database ('{latestDbMyGeotabVersionInfo.DatabaseName}'). Data from multiple MyGeotab databases cannot be stored in a single MyGeotab API Adapter database.";
                                    logger.Error(errorMessage);
                                    throw new Exception(errorMessage);
                                }

                                // If any of the properties other than DatabaseName have changed, insert the new DbMyGeotabVersionInfo entity into the database.
                                if (newDbMyGeotabVersionInfo.ApplicationBranch != latestDbMyGeotabVersionInfo.ApplicationBranch || newDbMyGeotabVersionInfo.ApplicationBuild != latestDbMyGeotabVersionInfo.ApplicationBuild || newDbMyGeotabVersionInfo.ApplicationCommit != latestDbMyGeotabVersionInfo.ApplicationCommit || newDbMyGeotabVersionInfo.DatabaseVersion != latestDbMyGeotabVersionInfo.DatabaseVersion || newDbMyGeotabVersionInfo.GoTalkVersion != latestDbMyGeotabVersionInfo.GoTalkVersion || newDbMyGeotabVersionInfo.Server != latestDbMyGeotabVersionInfo.Server)
                                {
                                    dbMyGeotabVersionInfosToPersist.Add(newDbMyGeotabVersionInfo);
                                }
                            }
                        }
                    }, new Context());

                    // Persist changes to database.
                    await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                    {
                        using (var adapterUOW = context.CreateUnitOfWork(Databases.AdapterDatabase))
                        {
                            try
                            {
                                // DbMyGeotabVersionInfo:
                                await dbMyGeotabVersionInfoEntityPersister.PersistEntitiesToDatabaseAsync(context, dbMyGeotabVersionInfosToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                // Commit transaction:
                                await adapterUOW.CommitAsync();
                            }
                            catch (Exception ex)
                            {
                                exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                                await adapterUOW.RollBackAsync();
                                throw;
                            }
                        }
                    }, new Context());
                }
            }
            catch (AdapterDatabaseConnectionException databaseConnectionException)
            {
                HandleException(databaseConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
            }
            catch (Exception)
            {
                throw;
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
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
                        using (var uow = context.CreateUnitOfWork(Databases.AdapterDatabase))
                        {
                            if (await stateMachine.IsAdapterDatabaseAccessibleAsync(context) == true)
                            {
                                logger.Info($"******** CONNECTIVITY RESTORED.");
                                stateMachine.SetState(State.Normal, StateReason.NoReason);
                            }
                        }
                    }, new Context());
                }
                else if (stateMachine.Reason == StateReason.MyGeotabNotAvailable)
                {
                    if (await stateMachine.IsMyGeotabAccessibleAsync() == true)
                    {
                        logger.Info($"******** CONNECTIVITY RESTORED.");
                        stateMachine.SetState(State.Normal, StateReason.NoReason);
                    }
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
