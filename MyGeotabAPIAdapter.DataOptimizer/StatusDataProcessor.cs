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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that handles ETL processing of StatusData data from the Adapter database to the Optimizer database. 
    /// </summary>
    class StatusDataProcessor : BackgroundService
    {
        string AssemblyName { get => GetType().Assembly.GetName().Name; }
        string AssemblyVersion { get => GetType().Assembly.GetName().Version.ToString(); }
        static string CurrentClassName { get => nameof(StatusDataProcessor); }
        static string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }
        static int ThrottleEngagingBatchRecordCount { get => 1000; }

        int lastBatchRecordCount = 0;

        readonly IAdapterDatabaseObjectNames adapterDatabaseObjectNames;
        readonly IConnectionInfoContainer connectionInfoContainer;
        readonly IDataOptimizerConfiguration dataOptimizerConfiguration;
        readonly IDateTimeHelper dateTimeHelper;
        readonly IDbStatusDataDbStatusDataTEntityMapper dbStatusDataDbStatusDataTEntityMapper;
        readonly IGenericEntityPersister<DbStatusData> dbStatusDataEntityPersister;
        readonly IGenericEntityPersister<DbStatusDataT> dbStatusDataTEntityPersister;
        readonly IGenericDbObjectCache<DbDeviceT> dbDeviceTObjectCache;
        readonly IGenericDbObjectCache<DbDiagnosticT> dbDiagnosticTObjectCache;
        readonly IExceptionHelper exceptionHelper;
        readonly IMessageLogger messageLogger;
        readonly IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames;
        readonly IOptimizerEnvironment optimizerEnvironment;
        readonly IPrerequisiteProcessorChecker prerequisiteProcessorChecker;
        readonly IProcessorTracker processorTracker;
        readonly IStateMachine stateMachine;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly UnitOfWorkContext adapterContext;
        readonly UnitOfWorkContext optimizerContext;

        /// <summary>
        /// The last time a call was initiated to retrieve records from the DbStatusData table in the Adapter database.
        /// </summary>
        DateTime DbStatusDatasLastQueriedUtc { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusDataProcessor"/> class.
        /// </summary>
        public StatusDataProcessor(IDataOptimizerConfiguration dataOptimizerConfiguration, IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames, IAdapterDatabaseObjectNames adapterDatabaseObjectNames, IDateTimeHelper dateTimeHelper, IExceptionHelper exceptionHelper, IMessageLogger messageLogger, IOptimizerEnvironment optimizerEnvironment, IPrerequisiteProcessorChecker prerequisiteProcessorChecker, IStateMachine stateMachine, IConnectionInfoContainer connectionInfoContainer, IProcessorTracker processorTracker, IDbStatusDataDbStatusDataTEntityMapper dbStatusDataDbStatusDataTEntityMapper, IGenericEntityPersister<DbStatusData> dbStatusDataEntityPersister, IGenericEntityPersister<DbStatusDataT> dbStatusDataTEntityPersister, IGenericDbObjectCache<DbDeviceT> dbDeviceTObjectCache, IGenericDbObjectCache<DbDiagnosticT> dbDiagnosticTObjectCache, UnitOfWorkContext adapterContext, UnitOfWorkContext optimizerContext)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.dataOptimizerConfiguration = dataOptimizerConfiguration;
            this.optimizerDatabaseObjectNames = optimizerDatabaseObjectNames;
            this.adapterDatabaseObjectNames = adapterDatabaseObjectNames;
            this.exceptionHelper = exceptionHelper;
            this.messageLogger = messageLogger;
            this.optimizerEnvironment = optimizerEnvironment;
            this.prerequisiteProcessorChecker = prerequisiteProcessorChecker;
            this.dateTimeHelper = dateTimeHelper;
            this.stateMachine = stateMachine;
            this.connectionInfoContainer = connectionInfoContainer;
            this.processorTracker = processorTracker;
            this.dbStatusDataDbStatusDataTEntityMapper = dbStatusDataDbStatusDataTEntityMapper;
            this.dbStatusDataEntityPersister = dbStatusDataEntityPersister;
            this.dbStatusDataTEntityPersister = dbStatusDataTEntityPersister;
            this.dbDeviceTObjectCache = dbDeviceTObjectCache;
            this.dbDiagnosticTObjectCache = dbDiagnosticTObjectCache;

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(UnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            this.optimizerContext = optimizerContext;
            logger.Debug($"{nameof(UnitOfWorkContext)} [Id: {optimizerContext.Id}] associated with {CurrentClassName}.");

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
                if (dataOptimizerConfiguration.StatusDataProcessorOperationMode == OperationMode.Scheduled)
                {
                    var timeSpanToNextDailyStartTimeUTC = dateTimeHelper.GetTimeSpanToNextDailyStartTimeUTC(dataOptimizerConfiguration.StatusDataProcessorDailyStartTimeUTC, dataOptimizerConfiguration.StatusDataProcessorDailyRunTimeSeconds);
                    if (timeSpanToNextDailyStartTimeUTC != TimeSpan.Zero)
                    {
                        DateTime nextScheduledStartTimeUTC = DateTime.UtcNow.Add(timeSpanToNextDailyStartTimeUTC);
                        messageLogger.LogScheduledServicePause(CurrentClassName, dataOptimizerConfiguration.StatusDataProcessorDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.StatusDataProcessorDailyRunTimeSeconds, nextScheduledStartTimeUTC);

                        await Task.Delay(timeSpanToNextDailyStartTimeUTC, stoppingToken);

                        DateTime nextScheduledPauseTimeUTC = DateTime.UtcNow.Add(TimeSpan.FromSeconds(dataOptimizerConfiguration.StatusDataProcessorDailyRunTimeSeconds));
                        messageLogger.LogScheduledServiceResumption(CurrentClassName, dataOptimizerConfiguration.StatusDataProcessorDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.StatusDataProcessorDailyRunTimeSeconds, nextScheduledPauseTimeUTC);
                    }
                }

                await WaitForPrerequisiteProcessorsIfNeededAsync(stoppingToken);

                // Abort if waiting for connectivity restoration.
                if (stateMachine.CurrentState == State.Waiting)
                {
                    continue;
                }

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        var engageExecutionThrottle = true;
                        DbStatusDatasLastQueriedUtc = DateTime.UtcNow;

                        // Initialize object caches.
                        if (dbDeviceTObjectCache.IsInitialized == false)
                        {
                            await dbDeviceTObjectCache.InitializeAsync(optimizerContext, Databases.OptimizerDatabase);
                        }
                        if (dbDiagnosticTObjectCache.IsInitialized == false)
                        {
                            await dbDiagnosticTObjectCache.InitializeAsync(optimizerContext, Databases.OptimizerDatabase);
                        }

                        // Get a batch of DbStatusDatas.
                        IEnumerable<DbStatusData> dbStatusDatas;
                        using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                        {
                            var dbStatusDataRepo = new DbStatusDataRepository2(adapterContext);
                            dbStatusDatas = await dbStatusDataRepo.GetAllAsync(cancellationTokenSource, dataOptimizerConfiguration.StatusDataProcessorBatchSize);
                        }

                        lastBatchRecordCount = dbStatusDatas.Count();
                        if (dbStatusDatas.Any())
                        {
                            engageExecutionThrottle = lastBatchRecordCount < ThrottleEngagingBatchRecordCount;
                            // Process the batch of DbStatusDatas.
#nullable enable
                            long? adapterDbLastId = null;
                            string? adapterDbLastGeotabId = null;
                            DateTime? adapterDbLastRecordCreationTimeUtc = null;
#nullable disable
                            var dbStatusDataTsToPersist = new List<DbStatusDataT>();
                            foreach (var dbStatusData in dbStatusDatas)
                            {
                                var deviceId = await dbDeviceTObjectCache.GetObjectIdAsync(dbStatusData.DeviceId);
                                var diagnosticId = await dbDiagnosticTObjectCache.GetObjectIdAsync(dbStatusData.DiagnosticId);
                                if (deviceId == null)
                                {
                                    logger.Warn($"Could not process {nameof(DbStatusData)} '{dbStatusData.id} (GeotabId {dbStatusData.GeotabId})' because a {nameof(DbDeviceT)} with a {nameof(DbDeviceT.GeotabId)} matching the {nameof(DbStatusData.DeviceId)} could not be found.");
                                    continue;
                                }
                                if (diagnosticId == null)
                                {
                                    logger.Warn($"Could not process {nameof(DbStatusData)} '{dbStatusData.id} (GeotabId {dbStatusData.GeotabId})' because a {nameof(DbDiagnosticT)} with a {nameof(DbDiagnosticT.GeotabId)} matching the {nameof(DbStatusData.DiagnosticId)} could not be found.");
                                    continue;
                                }
                                var dbStatusDataT = dbStatusDataDbStatusDataTEntityMapper.CreateEntity(dbStatusData, (long)deviceId, (long)diagnosticId);
                                dbStatusDataTsToPersist.Add(dbStatusDataT);
                                dbStatusData.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                                adapterDbLastId = dbStatusData.id;
                                adapterDbLastGeotabId = dbStatusData.GeotabId;
                                adapterDbLastRecordCreationTimeUtc = dbStatusData.RecordCreationTimeUtc;
                            }

                            // Persist changes to database using a Unit of Work for each database.
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                {
                                    try
                                    {
                                        // DbStatusDataT:
                                        await dbStatusDataTEntityPersister.PersistEntitiesToDatabaseAsync(optimizerContext, dbStatusDataTsToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                        // DbOProcessorTracking:
                                        await processorTracker.UpdateDbOProcessorTrackingRecord(optimizerContext, DataOptimizerProcessor.StatusDataProcessor, DbStatusDatasLastQueriedUtc, adapterDbLastId, adapterDbLastRecordCreationTimeUtc, adapterDbLastGeotabId);

                                        // DbStatusData:
                                        await dbStatusDataEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbStatusDatas, cancellationTokenSource, Logging.LogLevel.Info);

                                        // Commit transactions:
                                        await optimizerUOW.CommitAsync();
                                        await adapterUOW.CommitAsync();
                                    }
                                    catch (Exception)
                                    {
                                        await optimizerUOW.RollBackAsync();
                                        await adapterUOW.RollBackAsync();
                                        throw;
                                    }
                                }
                            }
                        }
                        else
                        {
                            logger.Debug($"No records were returned from the {adapterDatabaseObjectNames.DbStatusDataTableName} table in the {adapterDatabaseObjectNames.AdapterDatabaseNickname} database.");

                            // Update processor tracking info.
                            using (var uow = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                            {
                                try
                                {
                                    await processorTracker.UpdateDbOProcessorTrackingRecord(optimizerContext, DataOptimizerProcessor.StatusDataProcessor, DbStatusDatasLastQueriedUtc, null, null, null);
                                    await uow.CommitAsync();
                                }
                                catch (Exception)
                                {
                                    await uow.RollBackAsync();
                                    throw;
                                }
                            }
                        }

                        // If necessary, add a delay to implement the configured execution interval.
                        if (engageExecutionThrottle == true)
                        {
                            var delayTimeSpan = TimeSpan.FromSeconds(dataOptimizerConfiguration.StatusDataProcessorExecutionIntervalSeconds);
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
        /// Starts the current <see cref="StatusDataProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var dbOProcessorTrackings = await processorTracker.GetDbOProcessorTrackingListAsync();
            optimizerEnvironment.ValidateOptimizerEnvironment(dbOProcessorTrackings, DataOptimizerProcessor.StatusDataProcessor);
            using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
            {
                try
                {
                    await processorTracker.UpdateDbOProcessorTrackingRecord(optimizerContext, DataOptimizerProcessor.StatusDataProcessor, optimizerEnvironment.OptimizerVersion.ToString(), optimizerEnvironment.OptimizerMachineName);
                    await optimizerUOW.CommitAsync();
                }
                catch (Exception)
                {
                    await optimizerUOW.RollBackAsync();
                    throw;
                }
            }

            // Only start this service if it has been configured to be enabled.
            if (dataOptimizerConfiguration.EnableStatusDataProcessor == true)
            {
                logger.Info($"******** STARTING SERVICE: {AssemblyName}.{CurrentClassName} (v{AssemblyVersion})");
                await base.StartAsync(cancellationToken);
            }
            else
            {
                logger.Warn($"******** WARNING - SERVICE DISABLED: The {AssemblyName}.{CurrentClassName} service has not been enabled and will NOT be started.");
            }
        }

        /// <summary>
        /// Stops the current <see cref="StatusDataProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Info($"******** STOPPED SERVICE: {AssemblyName}.{CurrentClassName} (v{AssemblyVersion}) ********");
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
                DataOptimizerProcessor.DiagnosticProcessor
            };

            await prerequisiteProcessorChecker.WaitForPrerequisiteProcessorsIfNeededAsync(CurrentClassName, prerequisiteProcessors, cancellationToken);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
