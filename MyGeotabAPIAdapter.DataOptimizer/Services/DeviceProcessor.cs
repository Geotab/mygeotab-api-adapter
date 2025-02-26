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
    /// A <see cref="BackgroundService"/> that handles ETL processing of Device data from the Adapter database to the Optimizer database. 
    /// </summary>
    class DeviceProcessor : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }
        static int ThrottleEngagingBatchRecordCount { get => 1; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterDatabaseObjectNames adapterDatabaseObjectNames;
        readonly IDataOptimizerDatabaseConnectionInfoContainer connectionInfoContainer;
        readonly IDataOptimizerConfiguration dataOptimizerConfiguration;
        readonly IDateTimeHelper dateTimeHelper;
        readonly IDbDeviceDbDeviceTEntityMapper dbDeviceDbDeviceTEntityMapper;
        readonly IGenericEntityPersister<DbDeviceT> dbDeviceTEntityPersister;
        readonly IGenericGenericDbObjectCache<DbDevice, AdapterGenericDbObjectCache<DbDevice>> dbDeviceObjectCache;
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
        /// The last time a call was initiated to retrieve records from the DbDevices table in the Adapter database.
        /// </summary>
        DateTime DbDevicesLastQueriedUtc { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceProcessor"/> class.
        /// </summary>
        public DeviceProcessor(IDataOptimizerConfiguration dataOptimizerConfiguration, IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames, IAdapterDatabaseObjectNames adapterDatabaseObjectNames, IDateTimeHelper dateTimeHelper, IExceptionHelper exceptionHelper, IMessageLogger messageLogger, IOptimizerEnvironment optimizerEnvironment, IStateMachine stateMachine, IDataOptimizerDatabaseConnectionInfoContainer connectionInfoContainer, IPrerequisiteProcessorChecker prerequisiteProcessorChecker, IProcessorTracker processorTracker, IDbDeviceDbDeviceTEntityMapper dbDeviceDbDeviceTEntityMapper, IGenericEntityPersister<DbDeviceT> dbDeviceTEntityPersister, IGenericGenericDbObjectCache<DbDevice, AdapterGenericDbObjectCache<DbDevice>> dbDeviceObjectCache, IGenericGenericDbObjectCache<DbDeviceT, OptimizerGenericDbObjectCache<DbDeviceT>> dbDeviceTObjectCache, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext, IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> optimizerContext)
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
            this.dbDeviceDbDeviceTEntityMapper = dbDeviceDbDeviceTEntityMapper;
            this.dbDeviceTEntityPersister = dbDeviceTEntityPersister;
            this.dbDeviceObjectCache = dbDeviceObjectCache;
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
                if (dataOptimizerConfiguration.DeviceProcessorOperationMode == OperationMode.Scheduled)
                {
                    var timeSpanToNextDailyStartTimeUTC = dateTimeHelper.GetTimeSpanToNextDailyStartTimeUTC(dataOptimizerConfiguration.DeviceProcessorDailyStartTimeUTC, dataOptimizerConfiguration.DeviceProcessorDailyRunTimeSeconds);
                    if (timeSpanToNextDailyStartTimeUTC != TimeSpan.Zero)
                    {
                        DateTime nextScheduledStartTimeUTC = DateTime.UtcNow.Add(timeSpanToNextDailyStartTimeUTC);
                        messageLogger.LogScheduledServicePause(CurrentClassName, dataOptimizerConfiguration.DeviceProcessorDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.DeviceProcessorDailyRunTimeSeconds, nextScheduledStartTimeUTC);

                        await Task.Delay(timeSpanToNextDailyStartTimeUTC, stoppingToken);

                        DateTime nextScheduledPauseTimeUTC = DateTime.UtcNow.Add(TimeSpan.FromSeconds(dataOptimizerConfiguration.DeviceProcessorDailyRunTimeSeconds));
                        messageLogger.LogScheduledServiceResumption(CurrentClassName, dataOptimizerConfiguration.DeviceProcessorDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.DeviceProcessorDailyRunTimeSeconds, nextScheduledPauseTimeUTC);
                    }
                }

                // Abort if waiting for connectivity restoration.
                if (stateMachine.CurrentState == State.Waiting)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                // If the configured execution interval has not elapsed since the last time this method was executed, add a delay for the remainder of the interval.
                var deviceProcessorInfo = await processorTracker.GetDeviceProcessorInfoAsync();
                if (deviceProcessorInfo.EntitiesHaveBeenProcessed && !dateTimeHelper.TimeIntervalHasElapsed((DateTime)deviceProcessorInfo.EntitiesLastProcessedUtc, DateTimeIntervalType.Seconds, dataOptimizerConfiguration.DeviceProcessorExecutionIntervalSeconds))
                {
                    var delayTimeSpan = dateTimeHelper.GetRemainingTimeSpan((DateTime)deviceProcessorInfo.EntitiesLastProcessedUtc, dataOptimizerConfiguration.DeviceProcessorExecutionIntervalSeconds);
                    logger.Info($"{CurrentClassName} pausing for {delayTimeSpan} because the configured execution interval of {dataOptimizerConfiguration.DeviceProcessorExecutionIntervalSeconds} seconds has not elapsed since the last execution interval completed.");
                    await Task.Delay(delayTimeSpan, stoppingToken);
                    continue;
                }

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        var engageExecutionThrottle = true;
                        var processorTrackingInfoUpdated = false;
                        DbDevicesLastQueriedUtc = DateTime.UtcNow;

                        await InitializeOrUpdateCachesAsync(cancellationTokenSource);

                        if (dbDeviceObjectCache.Any())
                        {
#nullable enable
                            long? adapterDbLastId = null;
                            string? adapterDbLastGeotabId = null;
                            DateTime? adapterDbLastRecordCreationTimeUtc = null;
#nullable disable
                            // Get the subset of DbDevices that were added or changed since the last time DbDevices were processed.
                            var changedSince = dateTimeHelper.GetDateTimeOrDefault(deviceProcessorInfo.AdapterDbLastRecordCreationTimeUtc);
                            var changedDbDevices = await dbDeviceObjectCache.GetObjectsAsync(changedSince);
                            if (changedDbDevices.Any())
                            {
                                engageExecutionThrottle = changedDbDevices.Count < ThrottleEngagingBatchRecordCount;
                                var dbDeviceTsToPersist = new List<DbDeviceT>();

                                // Iterate through the list of added/changed DbDevices.
                                foreach (var changedDbDevice in changedDbDevices)
                                {
                                    // Try to get the DbDeviceT that corresponds with the DbDevice.
                                    var dbDeviceT = await dbDeviceTObjectCache.GetObjectAsync(changedDbDevice.GeotabId);
                                    if (dbDeviceT == null)
                                    {
                                        // The DbDeviceT doesn't yet exist. Create a new one.
                                        dbDeviceT = dbDeviceDbDeviceTEntityMapper.CreateEntity(changedDbDevice);
                                        dbDeviceTsToPersist.Add(dbDeviceT);

                                        adapterDbLastId = changedDbDevice.id;
                                        adapterDbLastGeotabId = changedDbDevice.GeotabId;
                                    }
                                    else
                                    {
                                        // Update the existing DbDeviceT.
                                        dbDeviceDbDeviceTEntityMapper.UpdateEntity(dbDeviceT, changedDbDevice);
                                        dbDeviceTsToPersist.Add(dbDeviceT);
                                    }
                                    // Keep track of the highest RecordLastChangedUtc value of all added or changed entities. 
                                    adapterDbLastRecordCreationTimeUtc = dateTimeHelper.GetGreatestDateTime(adapterDbLastRecordCreationTimeUtc, changedDbDevice.RecordLastChangedUtc);
                                }

                                // Persist changes to database. Run tasks in parallel.
                                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                                {
                                    using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                    {
                                        try
                                        {
                                            var dbEntityPersistenceTasks = new List<Task>
                                        {
                                            // DbDeviceT:
                                            dbDeviceTEntityPersister.PersistEntitiesToDatabaseAsync(optimizerContext, dbDeviceTsToPersist, cancellationTokenSource, Logging.LogLevel.Info),
                                           // DbOProcessorTracking:
                                            processorTracker.UpdateDbOProcessorTrackingRecordAsync(optimizerContext, DataOptimizerProcessor.DeviceProcessor, DbDevicesLastQueriedUtc, adapterDbLastId, adapterDbLastRecordCreationTimeUtc, adapterDbLastGeotabId)
                                    };
                                            await Task.WhenAll(dbEntityPersistenceTasks);

                                            // Commit transactions:
                                            await optimizerUOW.CommitAsync();
                                            processorTrackingInfoUpdated = true;
                                        }
                                        catch (Exception ex)
                                        {
                                            exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                                            await optimizerUOW.RollBackAsync();
                                            throw;
                                        }
                                    }
                                }, new Context());

                                // Force the DbDeviceT cache to be updated so that the changes are immediately available to other consumers.
                                await dbDeviceTObjectCache.UpdateAsync(true);
                            }
                            else
                            {
                                logger.Debug($"There are no new or changed records in the {adapterDatabaseObjectNames.DbDeviceTableName} table in the {adapterDatabaseObjectNames.AdapterDatabaseNickname} database since the last check.");
                            }
                        }
                        else
                        {
                            logger.Debug($"No records were returned from the {adapterDatabaseObjectNames.DbDeviceTableName} table in the {adapterDatabaseObjectNames.AdapterDatabaseNickname} database.");
                        }

                        // Update processor tracking info if not already done.
                        if (processorTrackingInfoUpdated == false)
                        {
                            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                            {
                                using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                {
                                    try
                                    {
                                        await processorTracker.UpdateDbOProcessorTrackingRecordAsync(optimizerContext, DataOptimizerProcessor.DeviceProcessor, DbDevicesLastQueriedUtc, null, null, null);
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
                        }

                        // If necessary, add a delay to implement the configured execution interval.
                        if (engageExecutionThrottle == true)
                        {
                            var delayTimeSpan = TimeSpan.FromSeconds(dataOptimizerConfiguration.DeviceProcessorExecutionIntervalSeconds);
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

            if (dbDeviceObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbDeviceObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }
            if (dbDeviceTObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbDeviceTObjectCache.InitializeAsync(Databases.OptimizerDatabase));
            }

            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);
        }

        /// <summary>
        /// Starts the current <see cref="DeviceProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOProcessorTrackings = await processorTracker.GetDbOProcessorTrackingListAsync();
            optimizerEnvironment.ValidateOptimizerEnvironment(dbOProcessorTrackings, DataOptimizerProcessor.DeviceProcessor, dataOptimizerConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                {
                    try
                    {
                        await processorTracker.UpdateDbOProcessorTrackingRecordAsync(optimizerContext, DataOptimizerProcessor.DeviceProcessor, optimizerEnvironment.OptimizerVersion.ToString(), optimizerEnvironment.OptimizerMachineName);
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
            if (dataOptimizerConfiguration.EnableDeviceProcessor == true)
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
        /// Stops the current <see cref="DeviceProcessor"/> instance.
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
            };

            await prerequisiteProcessorChecker.WaitForPrerequisiteProcessorsIfNeededAsync(CurrentClassName, prerequisiteProcessors, cancellationToken);
        }
    }
}
