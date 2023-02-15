using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Configuration.Add_Ons.VSS;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.DataAccess.Add_Ons.VSS;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models.Add_Ons.VSS;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.Helpers;
using NLog;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Add_Ons.VSS
{
    /// <summary>
    /// A worker service that iteratively retrieves <see cref="DbOVDSServerCommand"/> entities from the database and sends them to an Open Vehicle Data Set (OVDS) server via HTTP POST.
    /// </summary>
    class OVDSClientWorker : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }
        static int ThrottleEngagingBatchRecordCount { get => 1; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment adapterEnvironment;
        readonly IAdapterDatabaseObjectNames adapterDatabaseObjectNames;
        readonly IDateTimeHelper dateTimeHelper;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbFailedOVDSServerCommand> dbFailedOVDSServerCommandEntityPersister;
        readonly IGenericEntityPersister<DbOVDSServerCommand> dbOVDSServerCommandEntityPersister;
        readonly IHttpHelper httpHelper;
        readonly IServiceTracker serviceTracker;
        readonly IStateMachine stateMachine;
        readonly IVSSConfiguration vssConfiguration;
        readonly IVSSObjectMapper vssObjectMapper;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        readonly HttpClient httpClient = new();
        DateTime lastVSSConfigurationRefreshTime = DateTime.MinValue;
        DateTime lastIterationStartTimeUtc;
        readonly int commandTimeout = 0;

        /// <summary>
        /// Instantiates a new instance of the <see cref="OVDSClientWorker"/> class.
        /// </summary>
        public OVDSClientWorker(IAdapterConfiguration adapterConfiguration, IAdapterDatabaseObjectNames adapterDatabaseObjectNames, IAdapterEnvironment adapterEnvironment, IDateTimeHelper dateTimeHelper, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbFailedOVDSServerCommand> dbFailedOVDSServerCommandEntityPersister, IGenericEntityPersister<DbOVDSServerCommand> dbOVDSServerCommandEntityPersister, IHttpHelper httpHelper, IServiceTracker serviceTracker, IStateMachine stateMachine, IVSSConfiguration vssConfiguration, IVSSObjectMapper vssObjectMapper, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.adapterConfiguration = adapterConfiguration;
            this.adapterDatabaseObjectNames = adapterDatabaseObjectNames;
            this.adapterEnvironment = adapterEnvironment;
            this.dateTimeHelper = dateTimeHelper;
            this.exceptionHelper = exceptionHelper;
            this.dbFailedOVDSServerCommandEntityPersister = dbFailedOVDSServerCommandEntityPersister;
            this.dbOVDSServerCommandEntityPersister = dbOVDSServerCommandEntityPersister;
            this.httpHelper = httpHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.vssConfiguration = vssConfiguration;
            this.vssObjectMapper = vssObjectMapper;

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Allow longer database command timeout because the main Worker process may be inserting a batch of 150K records at the same time as one of these queries are being executed and that may exceed the timeout.
            commandTimeout = adapterConfiguration.TimeoutSecondsForDatabaseTasks * 2;

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Disposes the current <see cref="OVDSClientWorker"/> instance.
        /// </summary>
        public override void Dispose()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            base.Dispose();
        }

        /// <summary>
        /// Iteratively executes the business logic until the application is stopped.
        /// </summary>
        /// <param name="stoppingToken">The <see cref="CancellationToken"/> that can be used to stop execution of the application.</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // This is the loop containing all of the business logic that is executed iteratively throughout the lifeime of the application.
            while (!stoppingToken.IsCancellationRequested)
            {
                // Abort if waiting for connectivity restoration.
                if (stateMachine.CurrentState == State.Waiting)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                var engageExecutionThrottle = true;

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    // Only proceed if the OVDSClientWorkerIntervalSeconds has elapsed since the last iteration was initiated.
                    if (dateTimeHelper.TimeIntervalHasElapsed(lastIterationStartTimeUtc, DateTimeIntervalType.Seconds, vssConfiguration.OVDSClientWorkerIntervalSeconds))
                    {
                        lastIterationStartTimeUtc = DateTime.UtcNow;

                        using (var cancellationTokenSource = new CancellationTokenSource())
                        {
                            try
                            {
                                await vssConfiguration.UpdateVSSPathMapsAsync();

                                // Retrieve a batch of DbOVDSServerCommand entities from the database.
                                IEnumerable<DbOVDSServerCommand> dbOVDSServerCommands = null;
                                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                                {
                                    using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                    {
                                        var dbOVDSServerCommandRepo = new DbOVDSServerCommandRepository(adapterContext);
                                        dbOVDSServerCommands = await dbOVDSServerCommandRepo.GetAllAsync(cancellationTokenSource, vssConfiguration.OVDSServerCommandBatchSize);
                                    }
                                }, new Context());

                                int dbOVDSServerCommandCount = dbOVDSServerCommands.Count();
                                int processedDbOVDSServerCommandCount = 0;
                                int failedDbOVDSServerCommandCount = 0;
                                engageExecutionThrottle = dbOVDSServerCommandCount < ThrottleEngagingBatchRecordCount;

                                if (dbOVDSServerCommands.Any())
                                {
                                    logger.Info($"Retrieved {dbOVDSServerCommandCount} records from OVDSServerCommands table. Processing...");

                                    // Process each of the retrieved DbOVDSServerCommands.
                                    foreach (var dbOVDSServerCommand in dbOVDSServerCommands)
                                    {
                                        var dbOVDSServerCommandsToPersist = new List<DbOVDSServerCommand>();
                                        var dbFailedOVDSServerCommandsToPersist = new List<DbFailedOVDSServerCommand>();
                                        try
                                        {
                                            // Post the command to the OVDS server.
                                            HttpContent httpContent = new StringContent(dbOVDSServerCommand.Command);
                                            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                                            HttpResponseMessage response = await httpClient.PostAsync(vssConfiguration.OVDSServerURL, httpContent, stoppingToken);
                                            response.EnsureSuccessStatusCode();
                                            string responseBody = await response.Content.ReadAsStringAsync(stoppingToken);
                                            logger.Debug($"Command successfully POSTed to OVDS server. Command Id: '{dbOVDSServerCommand.id}'. Response body: ['{responseBody}']");

                                            // Delete the successfully processed DbOVDSServerCommand.
                                            dbOVDSServerCommand.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                                            dbOVDSServerCommandsToPersist.Add(dbOVDSServerCommand);
                                            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                                            {
                                                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                                {
                                                    try
                                                    {
                                                        // DbOVDSServerCommand
                                                        await dbOVDSServerCommandEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbOVDSServerCommandsToPersist, cancellationTokenSource, Logging.LogLevel.Info);

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
                                            processedDbOVDSServerCommandCount += 1;
                                        }
                                        catch (Exception exception)
                                        {
                                            try
                                            {
                                                failedDbOVDSServerCommandCount += 1;
                                                var failureMessage = GenerateMessageForOVDSClientWorkerException(exception);

                                                // Prepare a FailedOVDSServerCommand.
                                                DbFailedOVDSServerCommand dbFailedOVDSServerCommand = vssObjectMapper.GetDbFailedOVDSServerCommand(dbOVDSServerCommand, failureMessage);
                                                dbFailedOVDSServerCommandsToPersist.Add(dbFailedOVDSServerCommand);
                                                dbOVDSServerCommand.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                                                dbOVDSServerCommandsToPersist.Add(dbOVDSServerCommand);

                                                // Persist changes to database.
                                                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                                                {
                                                    using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                                    {
                                                        try
                                                        {
                                                            // DbFailedOVDSServerCommand:
                                                            await dbFailedOVDSServerCommandEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbFailedOVDSServerCommandsToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                                            // DbOVDSServerCommand
                                                            await dbOVDSServerCommandEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbOVDSServerCommandsToPersist, cancellationTokenSource, Logging.LogLevel.Info);

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
                                            catch (Exception ex)
                                            {
                                                throw new Exception($"Exception encountered while writing to {adapterDatabaseObjectNames.DbFailedOVDSServerCommandTableName} table or deleting from {adapterDatabaseObjectNames.DbOVDSServerCommandTableName} table.", ex);
                                            }
                                        }
                                    }

                                    logger.Info($"Of the {dbOVDSServerCommandCount} records from OVDSServerCommands table, {processedDbOVDSServerCommandCount} were successfully processed and {failedDbOVDSServerCommandCount} failed. Copies of any failed records have been inserted into the FailedOVDSServerCommands table for reference.");
                                }
                                else
                                {
                                    logger.Debug($"No records retrieved from OVDSServerCommands table.");
                                }

                                // Update the OServiceTracking record for this service to show that the service is operating.
                                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                                {
                                    using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                    {
                                        try
                                        {
                                            await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.OVDSClientWorker, DateTime.UtcNow);

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
                            catch (TaskCanceledException taskCanceledException)
                            {
                                string errorMessage = $"Task was cancelled. TaskCanceledException: \nMESSAGE [{taskCanceledException.Message}]; \nSOURCE [{taskCanceledException.Source}]; \nSTACK TRACE [{taskCanceledException.StackTrace}]";
                                logger.Warn(errorMessage);
                            }
                            catch (Exception)
                            {
                                cancellationTokenSource.Cancel();
                                throw;
                            }
                        }
                    }
                    else
                    {
                        logger.Debug($"No OVDS server commands will be processed on this iteration; {vssConfiguration.OVDSClientWorkerIntervalSeconds} seconds have not passed since the process was last initiated.");
                    }

                    logger.Trace($"Completed iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");
                }
                catch (OperationCanceledException)
                {
                    string errorMessage = $"{nameof(CurrentClassName)} process cancelled.";
                    logger.Warn(errorMessage);
                    throw new Exception(errorMessage);
                }
                catch (AdapterDatabaseConnectionException databaseConnectionException)
                {
                    HandleException(databaseConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                }
                catch (Exception ex)
                {
                    // If an exception hasn't been handled to this point, log it and kill the process.
                    HandleException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
                }

                // If necessary, add a delay to implement the configured execution interval.
                if (engageExecutionThrottle == true)
                {
                    var delayTimeSpan = TimeSpan.FromSeconds(vssConfiguration.OVDSClientWorkerIntervalSeconds);
                    logger.Info($"{CurrentClassName} pausing for the configured feed interval ({delayTimeSpan}).");
                    await Task.Delay(delayTimeSpan, stoppingToken);
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Generates the message for an <see cref="Exception"/> occurring while attempting to POST an OVDS server command. 
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <returns>The generated error message.</returns>
        string GenerateMessageForOVDSClientWorkerException(Exception exception)
        {
            string exceptionTypeName = exception.GetType().Name;
            StringBuilder messageBuilder = new();
            messageBuilder.AppendLine($"TYPE: [{exceptionTypeName}];");
            messageBuilder.AppendLine($"MESSAGE [{exception.Message}];");

            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
                exceptionTypeName = exception.GetType().Name;
                messageBuilder.AppendLine($"---------- INNER EXCEPTION ----------");
                messageBuilder.AppendLine($"TYPE: [{exceptionTypeName}];");
                messageBuilder.AppendLine($"MESSAGE [{exception.Message}];");
            }

            return messageBuilder.ToString();
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

            if (logLevel == NLogLogLevelName.Fatal)
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }

        }

        /// <summary>
        /// Starts the current <see cref="OVDSClientWorker"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.OVDSClientWorker);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.OVDSClientWorker, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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

            // Only start if the VSS Add-On is enabled.
            if (vssConfiguration.EnableVSSAddOn == true)
            {
                logger.Info($"******** STARTING SERVICE: {CurrentClassName}");

                // Initialize the VSSConfiguration.
                await vssConfiguration.InitializeAsync(AppContext.BaseDirectory, vssConfiguration.VSSPathMapFileName);

                await base.StartAsync(cancellationToken);
            }
            else
            {
                logger.Info($"******** WARNING - SERVICE DISABLED: The VSS Add-On ({CurrentClassName}) service has not been enabled and will NOT be started.");
            }
        }

        /// <summary>
        /// Stops the current <see cref="OVDSClientWorker"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
