using Geotab.Checkmate.ObjectModel;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityMappers;
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that propagates <see cref="DVIRLog"/> changes from tables in the adapter database to the associated MyGeotab database. 
    /// </summary>
    class DVIRLogManipulator : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterDatabaseObjectNames adapterDatabaseObjectNames;
        readonly IAdapterEnvironment adapterEnvironment;
        readonly IDbDVIRDefectUpdateDbFailedDVIRDefectUpdateEntityMapper dbDVIRDefectUpdateDbFailedDVIRDefectUpdateEntityMapper;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbDVIRDefectUpdate> dbDVIRDefectUpdateEntityPersister;
        readonly IGenericEntityPersister<DbFailedDVIRDefectUpdate> dbFailedDVIRDefectUpdateEntityPersister;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IPrerequisiteServiceChecker prerequisiteServiceChecker;
        readonly IServiceTracker serviceTracker;
        readonly IStateMachine stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        const int DVIRDefectUpdateBatchSize = 5000;
        const int MyGeotabAPIAuthenticationCheckIntervalMilliseconds = 1000;
        const int ExecutionThrottleEngagingRecordCount = 10;

        DateTime lastIterationStartTimeUtc;
        readonly int commandTimeout = 0;

        /// <summary>
        /// Instantiates a new instance of the <see cref="DVIRLogManipulator"/> class.
        /// </summary>
        public DVIRLogManipulator(IAdapterConfiguration adapterConfiguration, IAdapterDatabaseObjectNames adapterDatabaseObjectNames, IAdapterEnvironment adapterEnvironment, IDbDVIRDefectUpdateDbFailedDVIRDefectUpdateEntityMapper dbDVIRDefectUpdateDbFailedDVIRDefectUpdateEntityMapper, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbDVIRDefectUpdate> dbDVIRDefectUpdateEntityPersister, IGenericEntityPersister<DbFailedDVIRDefectUpdate> dbFailedDVIRDefectUpdateEntityPersister, IMyGeotabAPIHelper myGeotabAPIHelper, IPrerequisiteServiceChecker prerequisiteServiceChecker, IServiceTracker serviceTracker, IStateMachine stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.adapterConfiguration = adapterConfiguration;
            this.adapterDatabaseObjectNames = adapterDatabaseObjectNames;
            this.adapterEnvironment = adapterEnvironment;
            this.dbDVIRDefectUpdateDbFailedDVIRDefectUpdateEntityMapper = dbDVIRDefectUpdateDbFailedDVIRDefectUpdateEntityMapper;
            this.exceptionHelper = exceptionHelper;
            this.dbDVIRDefectUpdateEntityPersister = dbDVIRDefectUpdateEntityPersister;
            this.dbFailedDVIRDefectUpdateEntityPersister = dbFailedDVIRDefectUpdateEntityPersister;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.prerequisiteServiceChecker = prerequisiteServiceChecker;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Allow longer database command timeout because another process could be writing a batch of records to the DVIRDefectUpdates table and this could cause a bit of delay.
            commandTimeout = adapterConfiguration.TimeoutSecondsForDatabaseTasks * 2;

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Adds a <see cref="DefectRemark"/> to the <paramref name="dvirDefect"/> using properties of the <paramref name="dbDVIRDefectUpdate"/>.
        /// </summary>
        /// <param name="dvirDefect">The <see cref="DVIRDefect"/> to which a <see cref="DefectRemark"/> is to be added.</param>
        /// <param name="dbDVIRDefectUpdate">The <see cref="DbDVIRDefectUpdate"/> from which to obtain the property values for the <see cref="DefectRemark"/> that is to be added to the <paramref name="dvirDefect"/>.</param>
        /// <returns></returns>
        bool AddDefectRemark(DVIRDefect dvirDefect, DbDVIRDefectUpdate dbDVIRDefectUpdate)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Check whether the DbDVIRDefectUpdate includes a remark to be added.
            if (dbDVIRDefectUpdate.Remark == null && dbDVIRDefectUpdate.RemarkDateTime == null && dbDVIRDefectUpdate.RemarkUserId == null)
            {
                // No remark to add.
                return false;
            }

            // Ensure all remark-related properties have values.
            if (dbDVIRDefectUpdate.Remark == null || dbDVIRDefectUpdate.RemarkDateTime == null || dbDVIRDefectUpdate.RemarkUserId == null)
            {
                throw new ArgumentException($"Cannot add DefectRemark to DVIRDefect because one or more DefectRemark-related properties (Remark, RemarkDateTime, or RemarkUserId) of the DbDVIRDefectUpdate object are null.");
            }

            // Add the remark.
            DefectRemark defectRemark = new()
            {
                User = new User { Id = Id.Create(dbDVIRDefectUpdate.RemarkUserId) },
                DateTime = dbDVIRDefectUpdate.RemarkDateTime,
                Remark = dbDVIRDefectUpdate.Remark
            };
            if (dvirDefect.DefectRemarks == null)
            {
                dvirDefect.DefectRemarks = new List<DefectRemark>();
            }
            dvirDefect.DefectRemarks.Add(defectRemark);
            
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return true;
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

            while (!stoppingToken.IsCancellationRequested)
            {
                await WaitForPrerequisiteServicesIfNeededAsync(stoppingToken);

                // Abort if waiting for connectivity restoration.
                if (stateMachine.CurrentState == State.Waiting)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                var engageExecutionThrottle = false;

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    lastIterationStartTimeUtc = DateTime.UtcNow;

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        try
                        {
                            // Retrieve a batch of DbDVIRDefectUpdate entities from the database.
                            IEnumerable<DbDVIRDefectUpdate> dbDVIRDefectUpdates = null;
                            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                            {
                                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                {
                                    var dbDVIRDefectUpdateRepo = new BaseRepository<DbDVIRDefectUpdate>(adapterContext);
                                    dbDVIRDefectUpdates = await dbDVIRDefectUpdateRepo.GetAllAsync(cancellationTokenSource, DVIRDefectUpdateBatchSize);
                                }
                            }, new Context());

                            int dbDVIRDefectUpdateCount = dbDVIRDefectUpdates.Count();
                            int processedDbDVIRDefectUpdateCount = 0;
                            int failedDbDVIRDefectUpdateCount = 0;

                            // Flag the execution throttle to be applied at the end of this iteration.
                            if (dbDVIRDefectUpdateCount < ExecutionThrottleEngagingRecordCount)
                            {
                                engageExecutionThrottle = true;
                            }

                            if (dbDVIRDefectUpdates.Any())
                            {
                                logger.Info($"Retrieved {dbDVIRDefectUpdateCount} records from DVIRDefectUpdates table. Processing...");
                            }
                            else
                            {
                                logger.Debug($"No records retrieved from DVIRDefectUpdates table.");
                                continue;
                            }

                            // Process each of the retrieved DbDVIRDefectUpdate entities.
                            foreach (var dbDVIRDefectUpdate in dbDVIRDefectUpdates)
                            {
                                var dbDVIRDefectUpdatesToPersist = new List<DbDVIRDefectUpdate>();
                                var dbFailedDVIRDefectUpdatesToPersist = new List<DbFailedDVIRDefectUpdate>();
                                try
                                {
                                    // Get the subject DVIRLog from the MyGeotab database.
                                    var dvirLogResultList = await myGeotabAPIHelper.GetAsync<DVIRLog>(new DVIRLogSearch { Id = Id.Create(dbDVIRDefectUpdate.DVIRLogId) }, adapterConfiguration.TimeoutSecondsForMyGeotabTasks);
                                    if (dvirLogResultList.Any())
                                    {
                                        var dvirLogToUpdate = dvirLogResultList.First();
                                        // Get the subject DVIRDefect to be updated.
                                        var dvirDefectToUpdate = dvirLogToUpdate.DVIRDefects.Where(dvirDefect => dvirDefect.Id == Id.Create(dbDVIRDefectUpdate.DVIRDefectId)).FirstOrDefault();
                                        if (dvirDefectToUpdate == null)
                                        {
                                            throw new ArgumentException($"No DVIRDefect with the sepecified Id is associated with the subject DVIRLog in the MyGeotab database.");
                                        }
                                        else
                                        {
                                            bool defectRemarkAddedToDVIRDefectToUpdate = AddDefectRemark(dvirDefectToUpdate, dbDVIRDefectUpdate);
                                            bool repairStatusUpdatedOnDVIRDefectToUpdate = UpdateDVIRDefectRepairStatus(dvirDefectToUpdate, dbDVIRDefectUpdate);

                                            if (defectRemarkAddedToDVIRDefectToUpdate == false && repairStatusUpdatedOnDVIRDefectToUpdate == false)
                                            {
                                                throw new ArgumentException($"No action to take because insufficent data was provided to either add a DefectRemark or update the RepairStatus of the subject DVIRDefect.");
                                            }

                                            var deleteCurrentDbDVIRDefectUpdateRecord = false;
                                            try
                                            {
                                                // Update the DVIRLog in the MyGeotab database.
                                                var result = await myGeotabAPIHelper.SetAsync<DVIRLog>(dvirLogToUpdate, adapterConfiguration.TimeoutSecondsForMyGeotabTasks);
                                                deleteCurrentDbDVIRDefectUpdateRecord = true;
                                                processedDbDVIRDefectUpdateCount += 1;
                                            }
                                            catch (MyGeotabConnectionException)
                                            {
                                                // Pass the MyGeotabConnectionException along so that the connectivity loss can be addressed and the subject DbDVIRDefectUpdate can be re-tried once connectivity is restored.
                                                throw;
                                            }
                                            catch (Exception exception)
                                            {
                                                throw new Exception($"MyGeotab Exception encountered on Set<DVIRLog> call.", exception);
                                            }
                                            finally
                                            {
                                                if (deleteCurrentDbDVIRDefectUpdateRecord == true)
                                                {
                                                    // Delete the current DbDVIRDefectUpdate from the DVIRDefectUpdates table.
                                                    dbDVIRDefectUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                                                    dbDVIRDefectUpdatesToPersist.Add(dbDVIRDefectUpdate);
                                                    await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                                                    {
                                                        using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                                        {
                                                            try
                                                            {
                                                                // DbDVIRDefectUpdate:
                                                                await dbDVIRDefectUpdateEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbDVIRDefectUpdatesToPersist, cancellationTokenSource, Logging.LogLevel.Info);

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
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception($"No DVIRLog with the specified Id exists in the MyGeotab database.");
                                    }
                                }
                                catch (Exception dvirLogUpdateException)
                                {
                                    try
                                    {
                                        failedDbDVIRDefectUpdateCount += 1;
                                        var failureMessage = GenerateMessageForDVIRLogUpdateException(dvirLogUpdateException);

                                        // Prepare a DbFailedDVIRDefectUpdate.
                                        DbFailedDVIRDefectUpdate dbFailedDVIRDefectUpdate = dbDVIRDefectUpdateDbFailedDVIRDefectUpdateEntityMapper.CreateEntity(dbDVIRDefectUpdate, failureMessage);
                                        dbFailedDVIRDefectUpdatesToPersist.Add(dbFailedDVIRDefectUpdate);
                                        dbDVIRDefectUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                                        dbDVIRDefectUpdatesToPersist.Add(dbDVIRDefectUpdate);

                                        // Persist changes to database.
                                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                                        {
                                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                            {
                                                try
                                                {
                                                    // DbFailedDVIRDefectUpdate:
                                                    await dbFailedDVIRDefectUpdateEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbFailedDVIRDefectUpdatesToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                                    // DbDVIRDefectUpdate:
                                                    await dbDVIRDefectUpdateEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbDVIRDefectUpdatesToPersist, cancellationTokenSource, Logging.LogLevel.Info);

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
                                        throw new Exception($"Exception encountered while writing to {adapterDatabaseObjectNames.DbFailedDVIRDefectUpdatesTableName} table or deleting from {adapterDatabaseObjectNames.DbDVIRDefectUpdatesTableName} table.", ex);
                                    }
                                }
                            }

                            logger.Info($"Of the {dbDVIRDefectUpdateCount} records from DVIRDefectUpdates table, {processedDbDVIRDefectUpdateCount} were successfully processed and {failedDbDVIRDefectUpdateCount} failed. Copies of any failed records have been inserted into the FailedDVIRDefectUpdates table for reference.");
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

                    logger.Trace($"Completed iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");
                }
                catch (OperationCanceledException)
                {
                    string errorMessage = $"{nameof(CurrentClassName)} process cancelled.";
                    logger.Warn(errorMessage);
                    throw new Exception(errorMessage);
                }
                catch (DatabaseConnectionException databaseConnectionException)
                {
                    HandleException(databaseConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                }
                catch (MyGeotabConnectionException myGeotabConnectionException)
                {
                    HandleException(myGeotabConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                }
                catch (Exception ex)
                {
                    // If an exception hasn't been handled to this point, log it and kill the process.
                    HandleException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
                }

                // If fewer than the ExecutionThrottleEngagingRecordCount records were processed during the current iteration, add a delay equivalent to the configured execution interval.
                if (engageExecutionThrottle == true)
                {
                    var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.DVIRLogManipulatorIntervalSeconds);
                    logger.Info($"{CurrentClassName} pausing for the configured feed interval ({delayTimeSpan}).");
                    await Task.Delay(delayTimeSpan, stoppingToken);
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Generates the message for an <see cref="Exception"/> related to <see cref="DVIRLog"/> updates. 
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <returns>The generated error message.</returns>
        static string GenerateMessageForDVIRLogUpdateException(Exception exception)
        {
            string exceptionTypeName = exception.GetType().Name;
            StringBuilder messageBuilder = new();
            messageBuilder.Append($"TYPE: [{exceptionTypeName}];");
            messageBuilder.Append($" MESSAGE [{exception.Message}];");
            
            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
                exceptionTypeName = exception.GetType().Name;

                messageBuilder.Append($" > INNER EXCEPTION:");
                messageBuilder.Append($" TYPE: [{exceptionTypeName}];");
                messageBuilder.Append($" MESSAGE [{exception.Message}];");
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
        /// Starts the current <see cref="DVIRLogManipulator"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.DVIRLogManipulator, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DVIRLogManipulator, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.EnableDVIRLogManipulator == true)
            {
                logger.Info($"******** STARTING SERVICE: {CurrentClassName}");
                await base.StartAsync(cancellationToken);
            }
            else
            {
                logger.Warn($"******** WARNING - SERVICE DISABLED: The {CurrentClassName} service has not been enabled and will NOT be started.");
            }
        }

        /// <summary>
        /// Stops the current <see cref="DVIRLogManipulator"/> instance.
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

        /// <summary>
        /// Updates the repair status properties of the <paramref name="dvirDefect"/> using properties of the <paramref name="dbDVIRDefectUpdate"/>.
        /// </summary>
        /// <param name="dvirDefect">The <see cref="DVIRDefect"/> to be updated.</param>
        /// <param name="dbDVIRDefectUpdate">The <see cref="DbDVIRDefectUpdate"/> from which to obtain the property values for the <paramref name="dvirDefect"/> update.</param>
        /// <returns></returns>
        bool UpdateDVIRDefectRepairStatus(DVIRDefect dvirDefect, DbDVIRDefectUpdate dbDVIRDefectUpdate)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Check whether the DbDVIRDefectUpdate repair status properties are populated.
            if (dbDVIRDefectUpdate.RepairDateTime == null && dbDVIRDefectUpdate.RepairStatus == null && dbDVIRDefectUpdate.RepairUserId == null)
            {
                // No repair status update.
                return false;
            }

            // Proceed only if the DVIRDefect's RepairStatus has not already been set.
            if (dvirDefect.RepairStatus != null && dvirDefect.RepairStatus != RepairStatusType.NotRepaired)
            {
                throw new ArgumentException($"RepairStatus of the DVIRDefect has already been set and cannot be changed.");
            }

            // Ensure all repair status properties have values.
            if (dbDVIRDefectUpdate.RepairDateTime == null || dbDVIRDefectUpdate.RepairStatus == null || dbDVIRDefectUpdate.RepairUserId == null)
            {
                throw new ArgumentException($"One or more related properties (RepairDateTime, RepairStatus, or RepairUserId) of the DbDVIRDefectUpdate object are null.");
            }

            var updateRepairStatusType = dbDVIRDefectUpdate.RepairStatus switch
            {
                nameof(RepairStatusType.NotNecessary) => RepairStatusType.NotNecessary,
                nameof(RepairStatusType.Repaired) => RepairStatusType.Repaired,
                _ => throw new ArgumentException($"Invalid RepairStatus value. RepairStatus must be set to either 'Repaired' or 'NotNecessary'."),
            };

            // Update the repair status.
            var updateRepairUser = new User { Id = Id.Create(dbDVIRDefectUpdate.RepairUserId) };
            dvirDefect.RepairDateTime = dbDVIRDefectUpdate.RepairDateTime;
            dvirDefect.RepairStatus = updateRepairStatusType;
            dvirDefect.RepairUser = updateRepairUser;

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return true;
        }

        /// <summary>
        /// Checks whether any prerequisite services have been run and are currently running. If any of prerequisite services have not yet been run or are not currently running, details will be logged and this service will pause operation, repeating this check intermittently until all prerequisite services are running.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        async Task WaitForPrerequisiteServicesIfNeededAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var prerequisiteServices = new List<AdapterService>
            {
                AdapterService.DVIRLogProcessor
            };

            await prerequisiteServiceChecker.WaitForPrerequisiteServicesIfNeededAsync(CurrentClassName, prerequisiteServices, cancellationToken);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
