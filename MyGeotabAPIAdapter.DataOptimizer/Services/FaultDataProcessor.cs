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
    /// A <see cref="BackgroundService"/> that handles ETL processing of FaultData data from the Adapter database to the Optimizer database. 
    /// </summary>
    class FaultDataProcessor : BackgroundService
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
        readonly IDbFaultDataDbFaultDataTEntityMapper dbFaultDataDbFaultDataTEntityMapper;
        readonly IGenericEntityPersister<DbFaultData> dbFaultDataEntityPersister;
        readonly IGenericEntityPersister<DbFaultDataT> dbFaultDataTEntityPersister;
        readonly IGenericGenericDbObjectCache<DbDeviceT, OptimizerGenericDbObjectCache<DbDeviceT>> dbDeviceTObjectCache;
        readonly IGenericGenericDbObjectCache<DbUserT, OptimizerGenericDbObjectCache<DbUserT>> dbUserTObjectCache;
        readonly IGenericGeotabGUIDCacheableDbObjectCache<DbDiagnosticIdT> dbDiagnosticIdTObjectCache;
        readonly IGenericGeotabGUIDCacheableDbObjectCache<DbDiagnosticT> dbDiagnosticTObjectCache;
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
        /// The last time a call was initiated to retrieve records from the DbFaultData table in the Adapter database.
        /// </summary>
        DateTime DbFaultDatasLastQueriedUtc { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FaultDataProcessor"/> class.
        /// </summary>
        public FaultDataProcessor(IDataOptimizerConfiguration dataOptimizerConfiguration, IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames, IOptimizerEnvironment optimizerEnvironment, IPrerequisiteProcessorChecker prerequisiteProcessorChecker, IAdapterDatabaseObjectNames adapterDatabaseObjectNames, IDateTimeHelper dateTimeHelper, IExceptionHelper exceptionHelper, IMessageLogger messageLogger, IStateMachine stateMachine, IDataOptimizerDatabaseConnectionInfoContainer connectionInfoContainer, IProcessorTracker processorTracker, IDbFaultDataDbFaultDataTEntityMapper dbFaultDataDbFaultDataTEntityMapper, IGenericEntityPersister<DbFaultData> dbFaultDataEntityPersister, IGenericGenericDbObjectCache<DbDeviceT, OptimizerGenericDbObjectCache<DbDeviceT>> dbDeviceTObjectCache, IGenericGenericDbObjectCache<DbUserT, OptimizerGenericDbObjectCache<DbUserT>> dbUserTObjectCache, IGenericGeotabGUIDCacheableDbObjectCache<DbDiagnosticIdT> dbDiagnosticIdTObjectCache, IGenericGeotabGUIDCacheableDbObjectCache<DbDiagnosticT> dbDiagnosticTObjectCache, IGenericEntityPersister<DbFaultDataT> dbFaultDataTEntityPersister, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext, IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> optimizerContext)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.dataOptimizerConfiguration = dataOptimizerConfiguration;
            this.optimizerDatabaseObjectNames = optimizerDatabaseObjectNames;
            this.optimizerEnvironment = optimizerEnvironment;
            this.prerequisiteProcessorChecker = prerequisiteProcessorChecker;
            this.adapterDatabaseObjectNames = adapterDatabaseObjectNames;
            this.dateTimeHelper = dateTimeHelper;
            this.exceptionHelper = exceptionHelper;
            this.messageLogger = messageLogger;
            this.stateMachine = stateMachine;
            this.connectionInfoContainer = connectionInfoContainer;
            this.processorTracker = processorTracker;
            this.dbFaultDataDbFaultDataTEntityMapper = dbFaultDataDbFaultDataTEntityMapper;
            this.dbFaultDataEntityPersister = dbFaultDataEntityPersister;
            this.dbFaultDataTEntityPersister = dbFaultDataTEntityPersister;
            this.dbDeviceTObjectCache = dbDeviceTObjectCache;
            this.dbUserTObjectCache = dbUserTObjectCache;
            this.dbDiagnosticIdTObjectCache = dbDiagnosticIdTObjectCache;
            this.dbDiagnosticTObjectCache = dbDiagnosticTObjectCache;

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(OptimizerDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            this.optimizerContext = optimizerContext;
            logger.Debug($"{nameof(OptimizerDatabaseUnitOfWorkContext)} [Id: {optimizerContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Iteratively executes the business logic until the service is stopped.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            while (!stoppingToken.IsCancellationRequested)
            {
                // If configured to operate on a schedule and the present time is currently outside of an operating window, delay until the next daily start time.
                if (dataOptimizerConfiguration.FaultDataProcessorOperationMode == OperationMode.Scheduled)
                {
                    var timeSpanToNextDailyStartTimeUTC = dateTimeHelper.GetTimeSpanToNextDailyStartTimeUTC(dataOptimizerConfiguration.FaultDataProcessorDailyStartTimeUTC, dataOptimizerConfiguration.FaultDataProcessorDailyRunTimeSeconds);
                    if (timeSpanToNextDailyStartTimeUTC != TimeSpan.Zero)
                    {
                        DateTime nextScheduledStartTimeUTC = DateTime.UtcNow.Add(timeSpanToNextDailyStartTimeUTC);
                        messageLogger.LogScheduledServicePause(CurrentClassName, dataOptimizerConfiguration.FaultDataProcessorDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.FaultDataProcessorDailyRunTimeSeconds, nextScheduledStartTimeUTC);

                        await Task.Delay(timeSpanToNextDailyStartTimeUTC, stoppingToken);

                        DateTime nextScheduledPauseTimeUTC = DateTime.UtcNow.Add(TimeSpan.FromSeconds(dataOptimizerConfiguration.FaultDataProcessorDailyRunTimeSeconds));
                        messageLogger.LogScheduledServiceResumption(CurrentClassName, dataOptimizerConfiguration.FaultDataProcessorDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.FaultDataProcessorDailyRunTimeSeconds, nextScheduledPauseTimeUTC);
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
                        DbFaultDatasLastQueriedUtc = DateTime.UtcNow;

                        await InitializeOrUpdateCachesAsync(cancellationTokenSource);

                        // Get a batch of DbFaultDatas.
                        IEnumerable<DbFaultData> dbFaultDatas = null;
                        string sortColumnName = (string)nameof(DbFaultData.DateTime);
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                var dbFaultDataRepo = new BaseRepository<DbFaultData>(adapterContext);
                                dbFaultDatas = await dbFaultDataRepo.GetAllAsync(cancellationTokenSource, dataOptimizerConfiguration.FaultDataProcessorBatchSize, null, sortColumnName);
                            }
                        }, new Context());

                        lastBatchRecordCount = dbFaultDatas.Count();
                        if (dbFaultDatas.Any())
                        {
                            engageExecutionThrottle = lastBatchRecordCount < ThrottleEngagingBatchRecordCount;
                            // Process the batch of DbFaultDatas.
#nullable enable
                            long? adapterDbLastId = null;
                            string? adapterDbLastGeotabId = null;
                            DateTime? adapterDbLastRecordCreationTimeUtc = null;
#nullable disable
                            var dbFaultDataTsToPersist = new List<DbFaultDataT>();
                            foreach (var dbFaultData in dbFaultDatas)
                            {
                                var deviceId = await dbDeviceTObjectCache.GetObjectIdAsync(dbFaultData.DeviceId);
                                if (deviceId == null)
                                {
                                    logger.Warn($"Could not process {nameof(DbFaultData)} '{dbFaultData.id} (GeotabId {dbFaultData.GeotabId})' because a {nameof(DbDeviceT)} with a {nameof(DbDeviceT.GeotabId)} matching the {nameof(DbFaultData.DeviceId)} could not be found.");
                                    continue;
                                }

                                var diagnosticIdTGeotabGUID = await dbDiagnosticIdTObjectCache.GetObjectGeotabGUIDByGeotabIdAsync(dbFaultData.DiagnosticId);
                                if (diagnosticIdTGeotabGUID == null)
                                {
                                    logger.Warn($"Could not process {nameof(DbFaultData)} '{dbFaultData.id} (GeotabId {dbFaultData.GeotabId})' because a {nameof(DbDiagnosticIdT)} with a {nameof(DbDiagnosticIdT.GeotabId)} matching the {nameof(DbFaultData.DiagnosticId)} could not be found.");
                                    continue;
                                }

                                var diagnosticId = await dbDiagnosticTObjectCache.GetObjectIdByGeotabGUIDAsync(diagnosticIdTGeotabGUID);
                                if (diagnosticId == null)
                                {
                                    logger.Warn($"Could not process {nameof(DbFaultData)} '{dbFaultData.id} (GeotabId {dbFaultData.GeotabId})' because a {nameof(DbDiagnosticT)} with a {nameof(DbDiagnosticT.GeotabId)} matching the {nameof(DbFaultData.DiagnosticId)} could not be found.");
                                    continue;
                                }

                                var dismissUserId = await dbUserTObjectCache.GetObjectIdAsync(dbFaultData.DismissUserId);
                                var dbFaultDataT = dbFaultDataDbFaultDataTEntityMapper.CreateEntity(dbFaultData, (long)deviceId, (long)diagnosticId, dismissUserId);
                                dbFaultDataTsToPersist.Add(dbFaultDataT);
                                dbFaultData.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                                adapterDbLastId = dbFaultData.id;
                                adapterDbLastGeotabId = dbFaultData.GeotabId;
                                adapterDbLastRecordCreationTimeUtc = dbFaultData.RecordCreationTimeUtc;
                            }

                            // Persist changes to database using a Unit of Work for each database. Run tasks in parallel.
                            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                            {
                                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                {
                                    using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                    {
                                        try
                                        {
                                            var dbEntityPersistenceTasks = new List<Task>
                                        {
                                            // DbFaultDataT:
                                            dbFaultDataTEntityPersister.PersistEntitiesToDatabaseAsync(optimizerContext, dbFaultDataTsToPersist, cancellationTokenSource, Logging.LogLevel.Info),
                                            // DbFaultData:
                                            dbFaultDataEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbFaultDatas, cancellationTokenSource, Logging.LogLevel.Info),
                                            // DbOProcessorTracking:
                                            processorTracker.UpdateDbOProcessorTrackingRecordAsync(optimizerContext, DataOptimizerProcessor.FaultDataProcessor, DbFaultDatasLastQueriedUtc, adapterDbLastId, adapterDbLastRecordCreationTimeUtc, adapterDbLastGeotabId)
                                        };
                                            await Task.WhenAll(dbEntityPersistenceTasks);

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
                            logger.Debug($"No records were returned from the {adapterDatabaseObjectNames.DbFaultDataTableName} table in the {adapterDatabaseObjectNames.AdapterDatabaseNickname} database.");

                            // Update processor tracking info.
                            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                            {
                                using (var uow = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                {
                                    try
                                    {
                                        await processorTracker.UpdateDbOProcessorTrackingRecordAsync(optimizerContext, DataOptimizerProcessor.FaultDataProcessor, DbFaultDatasLastQueriedUtc, null, null, null);
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
                            var delayTimeSpan = TimeSpan.FromSeconds(dataOptimizerConfiguration.FaultDataProcessorExecutionIntervalSeconds);
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

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
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
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Initialize object caches. Run tasks in parallel.
            var dbObjectCacheInitializationAndUpdateTasks = new List<Task>();

            if (dbDeviceTObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbDeviceTObjectCache.InitializeAsync(Databases.OptimizerDatabase));
            }
            if (dbDiagnosticTObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbDiagnosticTObjectCache.InitializeAsync(Databases.OptimizerDatabase));
            }
            if (dbDiagnosticIdTObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbDiagnosticIdTObjectCache.InitializeAsync(Databases.OptimizerDatabase));
            }
            if (dbUserTObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbUserTObjectCache.InitializeAsync(Databases.OptimizerDatabase));
            }

            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Starts the current <see cref="FaultDataProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var dbOProcessorTrackings = await processorTracker.GetDbOProcessorTrackingListAsync();
            optimizerEnvironment.ValidateOptimizerEnvironment(dbOProcessorTrackings, DataOptimizerProcessor.FaultDataProcessor, dataOptimizerConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                {
                    try
                    {
                        await processorTracker.UpdateDbOProcessorTrackingRecordAsync(optimizerContext, DataOptimizerProcessor.FaultDataProcessor, optimizerEnvironment.OptimizerVersion.ToString(), optimizerEnvironment.OptimizerMachineName);
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
            if (dataOptimizerConfiguration.EnableFaultDataProcessor == true)
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
        /// Stops the current <see cref="FaultDataProcessor"/> instance.
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
        /// Checks whether any prerequisite processors have been run and are currently running. If any of prerequisite processors have not yet been run or are not currently running, details will be logged and this processor will pause operation, repeating this check intermittently until all prerequisite processors are running.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public async Task WaitForPrerequisiteProcessorsIfNeededAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var prerequisiteProcessors = new List<DataOptimizerProcessor>
            {
                DataOptimizerProcessor.DeviceProcessor,
                DataOptimizerProcessor.DiagnosticProcessor,
                DataOptimizerProcessor.UserProcessor
            };

            await prerequisiteProcessorChecker.WaitForPrerequisiteProcessorsIfNeededAsync(CurrentClassName, prerequisiteProcessors, cancellationToken);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
