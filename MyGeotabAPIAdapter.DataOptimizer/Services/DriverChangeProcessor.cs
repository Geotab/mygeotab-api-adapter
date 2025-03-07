﻿using Microsoft.Extensions.Hosting;
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
    /// A <see cref="BackgroundService"/> that handles ETL processing of DriverChange data from the Adapter database to the Optimizer database. 
    /// </summary>
    class DriverChangeProcessor : BackgroundService
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
        readonly IDbDriverChangeDbDriverChangeTEntityMapper dbDriverChangeDbDriverChangeTEntityMapper;
        readonly IGenericEntityPersister<DbDriverChange> dbDriverChangeEntityPersister;
        readonly IGenericEntityPersister<DbDriverChangeT> dbDriverChangeTEntityPersister;
        readonly IGenericEntityPersister<DbDriverChangeTypeT> dbDriverChangeTypeTEntityPersister;
        readonly IGenericGenericDbObjectCache<DbDeviceT, OptimizerGenericDbObjectCache<DbDeviceT>> dbDeviceTObjectCache;
        readonly IGenericGenericDbObjectCache<DbDriverChangeTypeT, OptimizerGenericDbObjectCache<DbDriverChangeTypeT>> dbDriverChangeTypeTObjectCache;
        readonly IGenericGenericDbObjectCache<DbUserT, OptimizerGenericDbObjectCache<DbUserT>> dbUserTObjectCache;
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
        /// The last time a call was initiated to retrieve records from the DbDriverChange table in the Adapter database.
        /// </summary>
        DateTime DbDriverChangesLastQueriedUtc { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DriverChangeProcessor"/> class.
        /// </summary>
        public DriverChangeProcessor(IDataOptimizerConfiguration dataOptimizerConfiguration, IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames, IAdapterDatabaseObjectNames adapterDatabaseObjectNames, IDateTimeHelper dateTimeHelper, IExceptionHelper exceptionHelper, IMessageLogger messageLogger, IOptimizerEnvironment optimizerEnvironment, IPrerequisiteProcessorChecker prerequisiteProcessorChecker, IStateMachine stateMachine, IDataOptimizerDatabaseConnectionInfoContainer connectionInfoContainer, IProcessorTracker processorTracker, IDbDriverChangeDbDriverChangeTEntityMapper dbDriverChangeDbDriverChangeTEntityMapper, IGenericEntityPersister<DbDriverChange> dbDriverChangeEntityPersister, IGenericEntityPersister<DbDriverChangeT> dbDriverChangeTEntityPersister, IGenericEntityPersister<DbDriverChangeTypeT> dbDriverChangeTypeTEntityPersister, IGenericGenericDbObjectCache<DbDeviceT, OptimizerGenericDbObjectCache<DbDeviceT>> dbDeviceTObjectCache, IGenericGenericDbObjectCache<DbDriverChangeTypeT, OptimizerGenericDbObjectCache<DbDriverChangeTypeT>> dbDriverChangeTypeTObjectCache, IGenericGenericDbObjectCache<DbUserT, OptimizerGenericDbObjectCache<DbUserT>> dbUserTObjectCache, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext, IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> optimizerContext)
        {
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
            this.dbDriverChangeDbDriverChangeTEntityMapper = dbDriverChangeDbDriverChangeTEntityMapper;
            this.dbDriverChangeEntityPersister = dbDriverChangeEntityPersister;
            this.dbDriverChangeTEntityPersister = dbDriverChangeTEntityPersister;
            this.dbDriverChangeTypeTEntityPersister = dbDriverChangeTypeTEntityPersister;
            this.dbDeviceTObjectCache = dbDeviceTObjectCache;
            this.dbDriverChangeTypeTObjectCache = dbDriverChangeTypeTObjectCache;
            this.dbUserTObjectCache = dbUserTObjectCache;

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
                if (dataOptimizerConfiguration.DriverChangeProcessorOperationMode == OperationMode.Scheduled)
                {
                    var timeSpanToNextDailyStartTimeUTC = dateTimeHelper.GetTimeSpanToNextDailyStartTimeUTC(dataOptimizerConfiguration.DriverChangeProcessorDailyStartTimeUTC, dataOptimizerConfiguration.DriverChangeProcessorDailyRunTimeSeconds);
                    if (timeSpanToNextDailyStartTimeUTC != TimeSpan.Zero)
                    {
                        DateTime nextScheduledStartTimeUTC = DateTime.UtcNow.Add(timeSpanToNextDailyStartTimeUTC);
                        messageLogger.LogScheduledServicePause(CurrentClassName, dataOptimizerConfiguration.DriverChangeProcessorDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.DriverChangeProcessorDailyRunTimeSeconds, nextScheduledStartTimeUTC);

                        await Task.Delay(timeSpanToNextDailyStartTimeUTC, stoppingToken);

                        DateTime nextScheduledPauseTimeUTC = DateTime.UtcNow.Add(TimeSpan.FromSeconds(dataOptimizerConfiguration.DriverChangeProcessorDailyRunTimeSeconds));
                        messageLogger.LogScheduledServiceResumption(CurrentClassName, dataOptimizerConfiguration.DriverChangeProcessorDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.DriverChangeProcessorDailyRunTimeSeconds, nextScheduledPauseTimeUTC);
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
                        DbDriverChangesLastQueriedUtc = DateTime.UtcNow;

                        await InitializeOrUpdateCachesAsync(cancellationTokenSource);

                        // Get a batch of DbDriverChanges.
                        IEnumerable<DbDriverChange> dbDriverChanges = null;
                        string sortColumnName = (string)nameof(DbDriverChange.DateTime);
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                var dbDriverChangeRepo = new BaseRepository<DbDriverChange>(adapterContext);
                                dbDriverChanges = await dbDriverChangeRepo.GetAllAsync(cancellationTokenSource, dataOptimizerConfiguration.DriverChangeProcessorBatchSize, null, sortColumnName);
                            }
                        }, new Context());

                        lastBatchRecordCount = dbDriverChanges.Count();
                        if (dbDriverChanges.Any())
                        {
                            engageExecutionThrottle = lastBatchRecordCount < ThrottleEngagingBatchRecordCount;
                            // Process the batch of DbDriverChanges.
#nullable enable
                            long? adapterDbLastId = null;
                            string? adapterDbLastGeotabId = null;
                            DateTime? adapterDbLastRecordCreationTimeUtc = null;
#nullable disable
                            var dbDriverChangeTsToPersist = new List<DbDriverChangeT>();
                            foreach (var dbDriverChange in dbDriverChanges)
                            {
                                var deviceId = await dbDeviceTObjectCache.GetObjectIdAsync(dbDriverChange.DeviceId);
                                long driverChangeTypeId = await GetDriverChangeTypeIdAsync(dbDriverChange.Type, cancellationTokenSource);
                                var driverId = await dbUserTObjectCache.GetObjectIdAsync(dbDriverChange.DriverId);
                                if (deviceId != null && driverId != null)
                                {
                                    var dbDriverChangeT = dbDriverChangeDbDriverChangeTEntityMapper.CreateEntity(dbDriverChange, driverChangeTypeId, (long)deviceId, (long)driverId);
                                    dbDriverChangeTsToPersist.Add(dbDriverChangeT);
                                    dbDriverChange.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                                    adapterDbLastId = dbDriverChange.id;
                                    adapterDbLastGeotabId = dbDriverChange.GeotabId;
                                    adapterDbLastRecordCreationTimeUtc = dbDriverChange.RecordCreationTimeUtc;
                                }
                                else
                                {
                                    if (deviceId == null && driverId == null)
                                    {
                                        logger.Debug($"Could not process {nameof(DbDriverChange)} '{dbDriverChange.id} (GeotabId {dbDriverChange.GeotabId})' because a {nameof(DbDeviceT)} with a {nameof(DbDeviceT.GeotabId)} matching the {nameof(DbDriverChange.DeviceId)} could not be found and a {nameof(DbUserT)} with a {nameof(DbUserT.GeotabId)} matching the {nameof(DbDriverChange.DriverId)} could not be found.");
                                    }
                                    else if (deviceId == null)
                                    {
                                        logger.Debug($"Could not process {nameof(DbDriverChange)} '{dbDriverChange.id} (GeotabId {dbDriverChange.GeotabId})' because a {nameof(DbDeviceT)} with a {nameof(DbDeviceT.GeotabId)} matching the {nameof(DbDriverChange.DeviceId)} could not be found.");
                                    }
                                    else if (driverId == null)
                                    {
                                        logger.Debug($"Could not process {nameof(DbDriverChange)} '{dbDriverChange.id} (GeotabId {dbDriverChange.GeotabId})' because a {nameof(DbUserT)} with a {nameof(DbUserT.GeotabId)} matching the {nameof(DbDriverChange.DriverId)} could not be found.");
                                    }
                                }
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
                                            // DbDriverChangeT:
                                            dbDriverChangeTEntityPersister.PersistEntitiesToDatabaseAsync(optimizerContext, dbDriverChangeTsToPersist, cancellationTokenSource, Logging.LogLevel.Info),
                                            // DbDriverChange:
                                            dbDriverChangeEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbDriverChanges, cancellationTokenSource, Logging.LogLevel.Info),
                                            // DbOProcessorTracking:
                                            processorTracker.UpdateDbOProcessorTrackingRecordAsync(optimizerContext, DataOptimizerProcessor.DriverChangeProcessor, DbDriverChangesLastQueriedUtc, adapterDbLastId, adapterDbLastRecordCreationTimeUtc, adapterDbLastGeotabId)
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
                            logger.Debug($"No records were returned from the {adapterDatabaseObjectNames.DbDriverChangeTableName} table in the {adapterDatabaseObjectNames.AdapterDatabaseNickname} database.");

                            // Update processor tracking info.
                            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                            {
                                using (var uow = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                {
                                    try
                                    {
                                        await processorTracker.UpdateDbOProcessorTrackingRecordAsync(optimizerContext, DataOptimizerProcessor.DriverChangeProcessor, DbDriverChangesLastQueriedUtc, null, null, null);
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
                            var delayTimeSpan = TimeSpan.FromSeconds(dataOptimizerConfiguration.DriverChangeProcessorExecutionIntervalSeconds);
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
        /// Retrieves the <see cref="DbDriverChangeTypeT.id"/> of the <see cref="DbDriverChangeTypeT"/> associated with the <paramref name="geotabId"/> from the <see cref="dbDriverChangeTypeTIdCache"/>. If the <see cref="DbDriverChangeTypeT"/> is not found in the <see cref="dbDriverChangeTypeTIdCache"/>, a new <see cref="DbDriverChangeTypeT"/> is added to the database for the subject <see cref="DbDriverChangeTypeT.id"/>, the <see cref="dbDriverChangeTypeTIdCache"/> is refreshed and the <see cref="DbDriverChangeTypeT.id"/> is then retrieved.
        /// </summary>
        /// <param name="geotabId">The <see cref="DbDriverChangeTypeT.GeotabId"/> for which to return the corresponding <see cref="DbDriverChangeTypeT.id"/>.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task<long> GetDriverChangeTypeIdAsync(string geotabId, CancellationTokenSource cancellationTokenSource)
        {
            var driverChangeTypeId = await dbDriverChangeTypeTObjectCache.GetObjectIdAsync(geotabId);
            if (driverChangeTypeId == null)
            {
                // DbDriverChangeTypeT not found in cache. Create a new one and persist it to database. 
                var dbDriverChangeTypeTsToPersist = new List<DbDriverChangeTypeT>();
                DbDriverChangeTypeT dbDriverChangeTypeT = new()
                {
                    DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                    GeotabId = geotabId,
                    RecordLastChangedUtc = DateTime.UtcNow
                };
                dbDriverChangeTypeTsToPersist.Add(dbDriverChangeTypeT);

                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                {
                    using (var uow = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                    {
                        try
                        {
                            await dbDriverChangeTypeTEntityPersister.PersistEntitiesToDatabaseAsync(optimizerContext, dbDriverChangeTypeTsToPersist, cancellationTokenSource, Logging.LogLevel.Info);
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

                // Force the DriverChangeTypeT cache to be updated so that the changes are immediately available to other consumers.
                await dbDriverChangeTypeTObjectCache.UpdateAsync(true);
                driverChangeTypeId = await dbDriverChangeTypeTObjectCache.GetObjectIdAsync(geotabId);
            }
            return (long)driverChangeTypeId;
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

            if (dbDeviceTObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbDeviceTObjectCache.InitializeAsync(Databases.OptimizerDatabase));
            }
            if (dbDriverChangeTypeTObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbDriverChangeTypeTObjectCache.InitializeAsync(Databases.OptimizerDatabase));
            }
            if (dbUserTObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbUserTObjectCache.InitializeAsync(Databases.OptimizerDatabase));
            }

            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);
        }

        /// <summary>
        /// Starts the current <see cref="DriverChangeProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOProcessorTrackings = await processorTracker.GetDbOProcessorTrackingListAsync();
            optimizerEnvironment.ValidateOptimizerEnvironment(dbOProcessorTrackings, DataOptimizerProcessor.DriverChangeProcessor, dataOptimizerConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                {
                    try
                    {
                        await processorTracker.UpdateDbOProcessorTrackingRecordAsync(optimizerContext, DataOptimizerProcessor.DriverChangeProcessor, optimizerEnvironment.OptimizerVersion.ToString(), optimizerEnvironment.OptimizerMachineName);
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
            if (dataOptimizerConfiguration.EnableDriverChangeProcessor == true)
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
        /// Stops the current <see cref="DriverChangeProcessor"/> instance.
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
                DataOptimizerProcessor.DeviceProcessor,
                DataOptimizerProcessor.UserProcessor
            };

            await prerequisiteProcessorChecker.WaitForPrerequisiteProcessorsIfNeededAsync(CurrentClassName, prerequisiteProcessors, cancellationToken);
        }
    }
}
