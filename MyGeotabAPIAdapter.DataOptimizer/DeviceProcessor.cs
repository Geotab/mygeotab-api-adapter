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
    /// A <see cref="BackgroundService"/> that handles ETL processing of Device data from the Adapter database to the Optimizer database. 
    /// </summary>
    class DeviceProcessor : BackgroundService
    {
        string AssemblyName { get => GetType().Assembly.GetName().Name; }
        string AssemblyVersion { get => GetType().Assembly.GetName().Version.ToString(); }
        static string CurrentClassName { get => nameof(DeviceProcessor); }
        static string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }
        static int ThrottleEngagingBatchRecordCount { get => 1; }

        readonly IAdapterDatabaseObjectNames adapterDatabaseObjectNames;
        readonly IConnectionInfoContainer connectionInfoContainer;
        readonly IDataOptimizerConfiguration dataOptimizerConfiguration;
        readonly IDateTimeHelper dateTimeHelper;
        readonly IDbDeviceDbDeviceTEntityMapper dbDeviceDbDeviceTEntityMapper;
        readonly IGenericEntityPersister<DbDeviceT> dbDeviceTEntityPersister;
        readonly IGenericDbObjectCache<DbDevice> dbDeviceObjectCache;
        readonly IGenericDbObjectCache<DbDeviceT> dbDeviceTObjectCache;
        readonly IExceptionHelper exceptionHelper;
        readonly IMessageLogger messageLogger;
        readonly IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames;
        readonly IOptimizerEnvironment optimizerEnvironment;
        readonly IProcessorTracker processorTracker;
        readonly IStateMachine stateMachine;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly UnitOfWorkContext adapterContext;
        readonly UnitOfWorkContext optimizerContext;

        /// <summary>
        /// The last time a call was initiated to retrieve records from the DbDevices table in the Adapter database.
        /// </summary>
        DateTime DbDevicesLastQueriedUtc { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceProcessor"/> class.
        /// </summary>
        public DeviceProcessor(IDataOptimizerConfiguration dataOptimizerConfiguration, IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames, IAdapterDatabaseObjectNames adapterDatabaseObjectNames, IDateTimeHelper dateTimeHelper, IExceptionHelper exceptionHelper, IMessageLogger messageLogger, IOptimizerEnvironment optimizerEnvironment, IStateMachine stateMachine, IConnectionInfoContainer connectionInfoContainer, IProcessorTracker processorTracker, IDbDeviceDbDeviceTEntityMapper dbDeviceDbDeviceTEntityMapper, IGenericEntityPersister<DbDeviceT> dbDeviceTEntityPersister, IGenericDbObjectCache<DbDevice> dbDeviceObjectCache, IGenericDbObjectCache<DbDeviceT> dbDeviceTObjectCache, UnitOfWorkContext adapterContext, UnitOfWorkContext optimizerContext)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.dataOptimizerConfiguration = dataOptimizerConfiguration;
            this.optimizerDatabaseObjectNames = optimizerDatabaseObjectNames;
            this.adapterDatabaseObjectNames = adapterDatabaseObjectNames;
            this.exceptionHelper = exceptionHelper;
            this.messageLogger = messageLogger;
            this.optimizerEnvironment = optimizerEnvironment;
            this.dateTimeHelper = dateTimeHelper;
            this.stateMachine = stateMachine;
            this.connectionInfoContainer = connectionInfoContainer;
            this.processorTracker = processorTracker;
            this.dbDeviceDbDeviceTEntityMapper = dbDeviceDbDeviceTEntityMapper;
            this.dbDeviceTEntityPersister = dbDeviceTEntityPersister;
            this.dbDeviceObjectCache = dbDeviceObjectCache;
            this.dbDeviceTObjectCache = dbDeviceTObjectCache;

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
                    continue;
                }

                // Abort if the configured execution interval has not elapsed since the last time this method was executed.
                var deviceProcessorInfo = await processorTracker.GetDeviceProcessorInfoAsync();
                if (deviceProcessorInfo.EntitiesHaveBeenProcessed && !dateTimeHelper.TimeIntervalHasElapsed((DateTime)deviceProcessorInfo.EntitiesLastProcessedUtc, DateTimeIntervalType.Seconds, dataOptimizerConfiguration.DeviceProcessorExecutionIntervalSeconds))
                {
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

                        // Initialize object caches.
                        if (dbDeviceObjectCache.IsInitialized == false)
                        {
                            await dbDeviceObjectCache.InitializeAsync(Databases.AdapterDatabase);
                        }
                        if (dbDeviceTObjectCache.IsInitialized == false)
                        {
                            await dbDeviceTObjectCache.InitializeAsync(Databases.OptimizerDatabase);
                        }

                        if (dbDeviceObjectCache.Any())
                        {
#nullable enable
                            long? adapterDbLastId = null;
                            string? adapterDbLastGeotabId = null;
                            DateTime? adapterDbLastRecordCreationTimeUtc = null;
#nullable disable
                            // Get the subset of DbDevices that were added or changed since the last time DbDevices were processed.
                            var changedSince = (DateTime)deviceProcessorInfo.EntitiesLastProcessedUtc;
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
                                        adapterDbLastRecordCreationTimeUtc = changedDbDevice.RecordLastChangedUtc;
                                    }
                                    else
                                    {
                                        // Update the existing DbDeviceT.
                                        dbDeviceDbDeviceTEntityMapper.UpdateEntity(dbDeviceT, changedDbDevice);
                                        dbDeviceTsToPersist.Add(dbDeviceT);
                                    }
                                }

                                // Persist changes to database.
                                using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                {
                                    try
                                    {
                                        // DbDeviceT:
                                        await dbDeviceTEntityPersister.PersistEntitiesToDatabaseAsync(optimizerContext, dbDeviceTsToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                        // DbOProcessorTracking:
                                        await processorTracker.UpdateDbOProcessorTrackingRecord(optimizerContext, DataOptimizerProcessor.DeviceProcessor, DbDevicesLastQueriedUtc, adapterDbLastId, adapterDbLastRecordCreationTimeUtc, adapterDbLastGeotabId);

                                        // Commit transactions:
                                        await optimizerUOW.CommitAsync();
                                        processorTrackingInfoUpdated = true;
                                    }
                                    catch (Exception)
                                    {
                                        await optimizerUOW.RollBackAsync();
                                        throw;
                                    }
                                }
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
                            using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                            {
                                try
                                {
                                    await processorTracker.UpdateDbOProcessorTrackingRecord(optimizerContext, DataOptimizerProcessor.DeviceProcessor, DbDevicesLastQueriedUtc, null, null, null);
                                    await optimizerUOW.CommitAsync();
                                }
                                catch (Exception)
                                {
                                    await optimizerUOW.RollBackAsync();
                                    throw;
                                }
                            }
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
        /// Starts the current <see cref="DeviceProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var dbOProcessorTrackings = await processorTracker.GetDbOProcessorTrackingListAsync();
            optimizerEnvironment.ValidateOptimizerEnvironment(dbOProcessorTrackings, DataOptimizerProcessor.DeviceProcessor);
            using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
            {
                try
                {
                    await processorTracker.UpdateDbOProcessorTrackingRecord(optimizerContext, DataOptimizerProcessor.DeviceProcessor, optimizerEnvironment.OptimizerVersion.ToString(), optimizerEnvironment.OptimizerMachineName);
                    await optimizerUOW.CommitAsync();
                }
                catch (Exception)
                {
                    await optimizerUOW.RollBackAsync();
                    throw;
                }
            }

            // Only start this service if it has been configured to be enabled.
            if (dataOptimizerConfiguration.EnableDeviceProcessor == true)
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
        /// Stops the current <see cref="DeviceProcessor"/> instance.
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
    }
}
