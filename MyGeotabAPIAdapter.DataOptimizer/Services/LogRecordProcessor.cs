using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Caches;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityMappers;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.Logging;
using NLog;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.DataOptimizer.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that handles ETL processing of LogRecord data from the Adapter database to the Optimizer database. 
    /// </summary>
    class LogRecordProcessor : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }
        static int ThrottleEngagingBatchRecordCount { get => 1000; }

        int lastBatchRecordCount = 0;

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterDatabaseObjectNames adapterDatabaseObjectNames;
        readonly IDataOptimizerDatabaseConnectionInfoContainer connectionInfoContainer;
        readonly IDataOptimizerConfiguration dataOptimizerConfiguration;
        readonly IDateTimeHelper dateTimeHelper;
        readonly IDbLogRecordDbLogRecordTEntityMapper dbLogRecordDbLogRecordTEntityMapper;
        readonly IGenericEntityPersister<DbLogRecord> dbLogRecordEntityPersister;
        readonly IGenericEntityPersister<DbLogRecordT> dbLogRecordTEntityPersister;
        readonly IGenericGenericDbObjectCache<DbDeviceT, OptimizerGenericDbObjectCache<DbDeviceT>> dbDeviceTObjectCache;
        readonly IExceptionHelper exceptionHelper;
        readonly IMessageLogger messageLogger;
        readonly IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames;
        readonly IOptimizerEnvironment optimizerEnvironment;
        readonly IPrerequisiteProcessorChecker prerequisiteProcessorChecker;
        readonly IProcessorTracker processorTracker;
        readonly IStateMachine stateMachine;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;
        readonly IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> optimizerContext;
        
        /// <summary>
        /// The last time a call was initiated to retrieve records from the DbLogRecords table in the Adapter database.
        /// </summary>
        DateTime DbLogRecordsLastQueriedUtc { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogRecordProcessor"/> class.
        /// </summary>
        public LogRecordProcessor(IDataOptimizerConfiguration dataOptimizerConfiguration, IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames, IAdapterDatabaseObjectNames adapterDatabaseObjectNames, IDateTimeHelper dateTimeHelper, IExceptionHelper exceptionHelper, IMessageLogger messageLogger, IOptimizerEnvironment optimizerEnvironment, IStateMachine stateMachine, IDataOptimizerDatabaseConnectionInfoContainer connectionInfoContainer, IPrerequisiteProcessorChecker prerequisiteProcessorChecker, IProcessorTracker processorTracker, IDbLogRecordDbLogRecordTEntityMapper dbLogRecordDbLogRecordTEntityMapper, IGenericEntityPersister<DbLogRecord> dbLogRecordEntityPersister, IGenericEntityPersister<DbLogRecordT> dbLogRecordTEntityPersister, IGenericGenericDbObjectCache<DbDeviceT, OptimizerGenericDbObjectCache<DbDeviceT>> dbDeviceTObjectCache, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext, IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> optimizerContext, IGenericIdCache<DbDeviceT> dbDeviceTIdCache)
        {
            this.dataOptimizerConfiguration = dataOptimizerConfiguration;
            this.optimizerDatabaseObjectNames = optimizerDatabaseObjectNames;
            this.adapterDatabaseObjectNames = adapterDatabaseObjectNames;
            this.exceptionHelper = exceptionHelper;
            this.messageLogger = messageLogger;
            this.optimizerEnvironment = optimizerEnvironment;
            this.dateTimeHelper = dateTimeHelper;
            this.stateMachine = stateMachine;
            this.connectionInfoContainer = connectionInfoContainer;
            this.prerequisiteProcessorChecker = prerequisiteProcessorChecker;
            this.processorTracker = processorTracker;
            this.dbLogRecordDbLogRecordTEntityMapper = dbLogRecordDbLogRecordTEntityMapper;
            this.dbLogRecordEntityPersister = dbLogRecordEntityPersister;
            this.dbLogRecordTEntityPersister = dbLogRecordTEntityPersister;
            this.dbDeviceTObjectCache = dbDeviceTObjectCache;

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(OptimizerDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");
            
            this.optimizerContext = optimizerContext;
            logger.Debug($"{nameof(OptimizerDatabaseUnitOfWorkContext)} [Id: {optimizerContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);
        }

        /// <summary>
        /// Iteratively executes the business logic until the service is stopped.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();

            while (!stoppingToken.IsCancellationRequested)
            {
                // If configured to operate on a schedule and the present time is currently outside of an operating window, delay until the next daily start time.
                if (dataOptimizerConfiguration.LogRecordProcessorOperationMode == OperationMode.Scheduled)
                {
                    var timeSpanToNextDailyStartTimeUTC = dateTimeHelper.GetTimeSpanToNextDailyStartTimeUTC(dataOptimizerConfiguration.LogRecordProcessorDailyStartTimeUTC, dataOptimizerConfiguration.LogRecordProcessorDailyRunTimeSeconds);
                    if (timeSpanToNextDailyStartTimeUTC != TimeSpan.Zero)
                    {
                        DateTime nextScheduledStartTimeUTC = DateTime.UtcNow.Add(timeSpanToNextDailyStartTimeUTC);
                        messageLogger.LogScheduledServicePause(CurrentClassName, dataOptimizerConfiguration.LogRecordProcessorDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.LogRecordProcessorDailyRunTimeSeconds, nextScheduledStartTimeUTC);
                       
                        await Task.Delay(timeSpanToNextDailyStartTimeUTC, stoppingToken);

                        DateTime nextScheduledPauseTimeUTC = DateTime.UtcNow.Add(TimeSpan.FromSeconds(dataOptimizerConfiguration.LogRecordProcessorDailyRunTimeSeconds));
                        messageLogger.LogScheduledServiceResumption(CurrentClassName, dataOptimizerConfiguration.LogRecordProcessorDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.LogRecordProcessorDailyRunTimeSeconds, nextScheduledPauseTimeUTC);
                    }
                }

                await WaitForPrerequisiteProcessorsIfNeededAsync(stoppingToken);

                // Abort if waiting for connectivity restoration.
                if (stateMachine.CurrentState == State.Waiting)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        var engageExecutionThrottle = true;
                        DbLogRecordsLastQueriedUtc = DateTime.UtcNow;

                        await InitializeOrUpdateCachesAsync(cancellationTokenSource);

                        // Get a batch of DbLogRecords.
                        IEnumerable<DbLogRecord> dbLogRecords = null;
                        string sortColumnName = (string)nameof(DbLogRecord.DateTime);
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                var dbLogRecordRepo = new BaseRepository<DbLogRecord>(adapterContext);
                                dbLogRecords = await dbLogRecordRepo.GetAllAsync(cancellationTokenSource, dataOptimizerConfiguration.LogRecordProcessorBatchSize, null, sortColumnName);
                            }
                        }, new Context());

                        lastBatchRecordCount = dbLogRecords.Count();
                        if (dbLogRecords.Any())
                        {
                            engageExecutionThrottle = lastBatchRecordCount < ThrottleEngagingBatchRecordCount;
                            // Process the batch of DbLogRecords.
#nullable enable
                            long? adapterDbLastId = null;
                            string? adapterDbLastGeotabId = null;
                            DateTime? adapterDbLastRecordCreationTimeUtc = null;
#nullable disable
                            var dbLogRecordTsToPersist = new List<DbLogRecordT>();
                            foreach (var dbLogRecord in dbLogRecords)
                            {
                                var deviceId = await dbDeviceTObjectCache.GetObjectIdAsync(dbLogRecord.DeviceId);
                                if (deviceId != null)
                                {
                                    var dbLogRecordT = dbLogRecordDbLogRecordTEntityMapper.CreateEntity(dbLogRecord, (long)deviceId);
                                    dbLogRecordTsToPersist.Add(dbLogRecordT);
                                    dbLogRecord.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                                    adapterDbLastId = dbLogRecord.id;
                                    adapterDbLastGeotabId = dbLogRecord.GeotabId;
                                    adapterDbLastRecordCreationTimeUtc = dbLogRecord.RecordCreationTimeUtc;
                                }
                                else
                                {
                                    logger.Warn($"Could not process {nameof(DbLogRecord)} '{dbLogRecord.id} (GeotabId {dbLogRecord.GeotabId})' because a {nameof(DbDeviceT)} with a {nameof(DbDeviceT.GeotabId)} matching the {nameof(DbLogRecord.DeviceId)} could not be found.");
                                }
                            }

                            // Persist changes to database using a Unit of Work for each database.
                            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                            {
                                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                {
                                    using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                    {
                                        try
                                        {
                                            // DbLogRecordT:
                                            await dbLogRecordTEntityPersister.PersistEntitiesToDatabaseAsync(optimizerContext, dbLogRecordTsToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                            // DbOProcessorTracking:
                                            await processorTracker.UpdateDbOProcessorTrackingRecordAsync(optimizerContext, DataOptimizerProcessor.LogRecordProcessor, DbLogRecordsLastQueriedUtc, adapterDbLastId, adapterDbLastRecordCreationTimeUtc, adapterDbLastGeotabId);

                                            // DbLogRecord:
                                            await dbLogRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbLogRecords, cancellationTokenSource, Logging.LogLevel.Info);

                                            // Commit transactions:
                                            await optimizerUOW.CommitAsync();
                                            await adapterUOW.CommitAsync();
                                        }
                                        catch (Exception ex)
                                        {
                                            exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                                            await optimizerUOW.RollBackAsync();
                                            await adapterUOW.RollBackAsync();
                                            throw;
                                        }
                                    }
                                }
                            }, new Context());
                        }
                        else
                        {
                            logger.Debug($"No records were returned from the {adapterDatabaseObjectNames.DbLogRecordTableName} table in the {adapterDatabaseObjectNames.AdapterDatabaseNickname} database.");

                            // Update processor tracking info.
                            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                            {
                                using (var uow = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                {
                                    try
                                    {
                                        await processorTracker.UpdateDbOProcessorTrackingRecordAsync(optimizerContext, DataOptimizerProcessor.LogRecordProcessor, DbLogRecordsLastQueriedUtc, null, null, null);
                                        await uow.CommitAsync();
                                    }
                                    catch (Exception ex)
                                    {
                                        exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                                        await uow.RollBackAsync();
                                        throw;
                                    }
                                }
                            }, new Context());
                        }

                        // If necessary, add a delay to implement the configured execution interval.
                        if (engageExecutionThrottle == true)
                        {
                            var delayTimeSpan = TimeSpan.FromSeconds(dataOptimizerConfiguration.LogRecordProcessorExecutionIntervalSeconds);
                            logger.Info($"{CurrentClassName} pausing for {delayTimeSpan} because fewer than {ThrottleEngagingBatchRecordCount} records were processed during the current execution interval.");
                            await Task.Delay(delayTimeSpan, stoppingToken);
                        }
                    }

                    logger.Trace($"Completed iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");
                }
                catch (OperationCanceledException)
                {
                    string errorMessage = $"{CurrentClassName} process cancelled.";
                    logger.Warn(errorMessage);
                    throw new Exception(errorMessage);
                }
                catch (AdapterDatabaseConnectionException databaseConnectionException)
                {
                    HandleException(databaseConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                }
                catch (OptimizerDatabaseConnectionException optimizerDatabaseConnectionException)
                {
                    HandleException(optimizerDatabaseConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                }
                catch (Exception ex)
                {
                    // If an exception hasn't been handled to this point, log it and kill the process.
                    HandleException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
                }
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
            else if (exception is OptimizerDatabaseConnectionException)
            {
                stateMachine.SetState(State.Waiting, StateReason.OptimizerDatabaseNotAvailable);
            }

            if (logLevel == NLogLogLevelName.Fatal)
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        /// Initializes and/or updates any caches used by this class.
        /// </summary>
        /// <returns></returns>
        async Task InitializeOrUpdateCachesAsync(CancellationTokenSource cancellationTokenSource)
        {
            // Initialize object caches. Run tasks in parallel.
            var dbObjectCacheInitializationAndUpdateTasks = new List<Task>();

            // Initialize object caches.
            if (dbDeviceTObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbDeviceTObjectCache.InitializeAsync(Databases.OptimizerDatabase));
            }

            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);
        }

        /// <summary>
        /// Starts the current <see cref="LogRecordProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOProcessorTrackings = await processorTracker.GetDbOProcessorTrackingListAsync();
            optimizerEnvironment.ValidateOptimizerEnvironment(dbOProcessorTrackings, DataOptimizerProcessor.LogRecordProcessor, dataOptimizerConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                {
                    try
                    {
                        await processorTracker.UpdateDbOProcessorTrackingRecordAsync(optimizerContext, DataOptimizerProcessor.LogRecordProcessor, optimizerEnvironment.OptimizerVersion.ToString(), optimizerEnvironment.OptimizerMachineName);
                        await optimizerUOW.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                        await optimizerUOW.RollBackAsync();
                        throw;
                    }
                }
            }, new Context());

            // Only start this service if it has been configured to be enabled.
            if (dataOptimizerConfiguration.EnableLogRecordProcessor == true)
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
        /// Stops the current <see cref="LogRecordProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Checks whether any prerequisite processors have been run and are currently running. If any of prerequisite processors have not yet been run or are not currently running, details will be logged and this processor will pause operation, repeating this check intermittently until all prerequisite processors are running.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public async Task WaitForPrerequisiteProcessorsIfNeededAsync(CancellationToken cancellationToken)
        {
            var prerequisiteProcessors = new List<DataOptimizerProcessor>
            {
                DataOptimizerProcessor.DeviceProcessor
            };

            await prerequisiteProcessorChecker.WaitForPrerequisiteProcessorsIfNeededAsync(CurrentClassName, prerequisiteProcessors, cancellationToken);
        }
    }
}
