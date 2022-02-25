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
    /// A <see cref="BackgroundService"/> that handles ETL processing of BinaryData data from the Adapter database to the Optimizer database. 
    /// </summary>
    class BinaryDataProcessor : BackgroundService
    {
        string AssemblyName { get => GetType().Assembly.GetName().Name; }
        string AssemblyVersion { get => GetType().Assembly.GetName().Version.ToString(); }
        static string CurrentClassName { get => nameof(BinaryDataProcessor); }
        static string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }
        static int ThrottleEngagingBatchRecordCount { get => 1000; }

        int lastBatchRecordCount = 0;

        readonly IAdapterDatabaseObjectNames adapterDatabaseObjectNames;
        readonly IConnectionInfoContainer connectionInfoContainer;
        readonly IDataOptimizerConfiguration dataOptimizerConfiguration;
        readonly IDateTimeHelper dateTimeHelper;
        readonly IDbBinaryDataDbBinaryDataTEntityMapper dbBinaryDataDbBinaryDataTEntityMapper;
        readonly IGenericEntityPersister<DbBinaryData> dbBinaryDataEntityPersister;
        readonly IGenericEntityPersister<DbBinaryDataT> dbBinaryDataTEntityPersister;
        readonly IGenericEntityPersister<DbBinaryTypeT> dbBinaryTypeTEntityPersister;
        readonly IGenericEntityPersister<DbControllerT> dbControllerTEntityPersister;
        readonly IGenericDbObjectCache<DbDeviceT> dbDeviceTObjectCache;
        readonly IGenericDbObjectCache<DbBinaryTypeT> dbBinaryTypeTObjectCache;
        readonly IGenericDbObjectCache<DbControllerT> dbControllerTObjectCache;
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
        /// The last time a call was initiated to retrieve records from the DbBinaryData table in the Adapter database.
        /// </summary>
        DateTime DbBinaryDatasLastQueriedUtc { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryDataProcessor"/> class.
        /// </summary>
        public BinaryDataProcessor(IDataOptimizerConfiguration dataOptimizerConfiguration, IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames, IAdapterDatabaseObjectNames adapterDatabaseObjectNames, IDateTimeHelper dateTimeHelper, IExceptionHelper exceptionHelper, IMessageLogger messageLogger, IOptimizerEnvironment optimizerEnvironment, IPrerequisiteProcessorChecker prerequisiteProcessorChecker, IStateMachine stateMachine, IConnectionInfoContainer connectionInfoContainer, IProcessorTracker processorTracker, IDbBinaryDataDbBinaryDataTEntityMapper dbBinaryDataDbBinaryDataTEntityMapper, IGenericEntityPersister<DbBinaryData> dbBinaryDataEntityPersister, IGenericEntityPersister<DbBinaryDataT> dbBinaryDataTEntityPersister, IGenericEntityPersister<DbBinaryTypeT> dbBinaryTypeTEntityPersister, IGenericEntityPersister<DbControllerT> dbControllerTEntityPersister, IGenericDbObjectCache<DbDeviceT> dbDeviceTObjectCache, IGenericDbObjectCache<DbBinaryTypeT> dbBinaryTypeTObjectCache, IGenericDbObjectCache<DbControllerT> dbControllerTObjectCache, UnitOfWorkContext adapterContext, UnitOfWorkContext optimizerContext)
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
            this.dbBinaryDataDbBinaryDataTEntityMapper = dbBinaryDataDbBinaryDataTEntityMapper;
            this.dbBinaryDataEntityPersister = dbBinaryDataEntityPersister;
            this.dbBinaryDataTEntityPersister = dbBinaryDataTEntityPersister;
            this.dbBinaryTypeTEntityPersister = dbBinaryTypeTEntityPersister;
            this.dbControllerTEntityPersister = dbControllerTEntityPersister;
            this.dbDeviceTObjectCache = dbDeviceTObjectCache;
            this.dbBinaryTypeTObjectCache = dbBinaryTypeTObjectCache;
            this.dbControllerTObjectCache = dbControllerTObjectCache;

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
                if (dataOptimizerConfiguration.BinaryDataProcessorOperationMode == OperationMode.Scheduled)
                {
                    var timeSpanToNextDailyStartTimeUTC = dateTimeHelper.GetTimeSpanToNextDailyStartTimeUTC(dataOptimizerConfiguration.BinaryDataProcessorDailyStartTimeUTC, dataOptimizerConfiguration.BinaryDataProcessorDailyRunTimeSeconds);
                    if (timeSpanToNextDailyStartTimeUTC != TimeSpan.Zero)
                    {
                        DateTime nextScheduledStartTimeUTC = DateTime.UtcNow.Add(timeSpanToNextDailyStartTimeUTC);
                        messageLogger.LogScheduledServicePause(CurrentClassName, dataOptimizerConfiguration.BinaryDataProcessorDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.BinaryDataProcessorDailyRunTimeSeconds, nextScheduledStartTimeUTC);

                        await Task.Delay(timeSpanToNextDailyStartTimeUTC, stoppingToken);

                        DateTime nextScheduledPauseTimeUTC = DateTime.UtcNow.Add(TimeSpan.FromSeconds(dataOptimizerConfiguration.BinaryDataProcessorDailyRunTimeSeconds));
                        messageLogger.LogScheduledServiceResumption(CurrentClassName, dataOptimizerConfiguration.BinaryDataProcessorDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.BinaryDataProcessorDailyRunTimeSeconds, nextScheduledPauseTimeUTC);
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
                        DbBinaryDatasLastQueriedUtc = DateTime.UtcNow;

                        // Initialize object caches.
                        if (dbDeviceTObjectCache.IsInitialized == false)
                        {
                            await dbDeviceTObjectCache.InitializeAsync(optimizerContext, Databases.OptimizerDatabase);
                        }
                        if (dbBinaryTypeTObjectCache.IsInitialized == false)
                        {
                            await dbBinaryTypeTObjectCache.InitializeAsync(optimizerContext, Databases.OptimizerDatabase);
                        }
                        if (dbControllerTObjectCache.IsInitialized == false)
                        {
                            await dbControllerTObjectCache.InitializeAsync(optimizerContext, Databases.OptimizerDatabase);
                        }

                        // Get a batch of DbBinaryDatas.
                        IEnumerable<DbBinaryData> dbBinaryDatas;
                        string sortColumnName = (string)nameof(DbBinaryData.DateTime);
                        using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                        {
                            var dbBinaryDataRepo = new DbBinaryDataRepository2(adapterContext);
                            dbBinaryDatas = await dbBinaryDataRepo.GetAllAsync(cancellationTokenSource, dataOptimizerConfiguration.BinaryDataProcessorBatchSize, null, sortColumnName);
                        }

                        lastBatchRecordCount = dbBinaryDatas.Count();
                        if (dbBinaryDatas.Any())
                        {
                            engageExecutionThrottle = lastBatchRecordCount < ThrottleEngagingBatchRecordCount;
                            // Process the batch of DbBinaryDatas.
#nullable enable
                            long? adapterDbLastId = null;
                            string? adapterDbLastGeotabId = null;
                            DateTime? adapterDbLastRecordCreationTimeUtc = null;
#nullable disable
                            var dbBinaryDataTsToPersist = new List<DbBinaryDataT>();
                            foreach (var dbBinaryData in dbBinaryDatas)
                            {
                                var deviceId = await dbDeviceTObjectCache.GetObjectIdAsync(dbBinaryData.DeviceId);
                                if (deviceId == null)
                                {
                                    logger.Warn($"Could not process {nameof(DbBinaryData)} '{dbBinaryData.id} (GeotabId {dbBinaryData.GeotabId})' because a {nameof(DbDeviceT)} with a {nameof(DbDeviceT.GeotabId)} matching the {nameof(DbBinaryData.DeviceId)} could not be found.");
                                    continue;
                                }
                                var binaryTypeId = await GetBinaryTypeIdAsync(dbBinaryData.BinaryType, cancellationTokenSource);
                                var controllerId = await GetControllerIdAsync(dbBinaryData.ControllerId, cancellationTokenSource);
                                var dbBinaryDataT = dbBinaryDataDbBinaryDataTEntityMapper.CreateEntity(dbBinaryData, (long)binaryTypeId, (long)controllerId, (long)deviceId);
                                dbBinaryDataTsToPersist.Add(dbBinaryDataT);
                                dbBinaryData.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                                adapterDbLastId = dbBinaryData.id;
                                adapterDbLastGeotabId = dbBinaryData.GeotabId;
                                adapterDbLastRecordCreationTimeUtc = dbBinaryData.RecordCreationTimeUtc;
                            }

                            // Persist changes to database using a Unit of Work for each database.
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                {
                                    try
                                    {
                                        // DbBinaryDataT:
                                        await dbBinaryDataTEntityPersister.PersistEntitiesToDatabaseAsync(optimizerContext, dbBinaryDataTsToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                        // DbOProcessorTracking:
                                        await processorTracker.UpdateDbOProcessorTrackingRecord(optimizerContext, DataOptimizerProcessor.BinaryDataProcessor, DbBinaryDatasLastQueriedUtc, adapterDbLastId, adapterDbLastRecordCreationTimeUtc, adapterDbLastGeotabId);

                                        // DbBinaryData:
                                        await dbBinaryDataEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbBinaryDatas, cancellationTokenSource, Logging.LogLevel.Info);

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
                            logger.Debug($"No records were returned from the {adapterDatabaseObjectNames.DbBinaryDataTableName} table in the {adapterDatabaseObjectNames.AdapterDatabaseNickname} database.");

                            // Update processor tracking info.
                            using (var uow = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                            {
                                try
                                {
                                    await processorTracker.UpdateDbOProcessorTrackingRecord(optimizerContext, DataOptimizerProcessor.BinaryDataProcessor, DbBinaryDatasLastQueriedUtc, null, null, null);
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
                            var delayTimeSpan = TimeSpan.FromSeconds(dataOptimizerConfiguration.BinaryDataProcessorExecutionIntervalSeconds);
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
        /// Retrieves the <see cref="DbBinaryTypeT.id"/> of the <see cref="DbBinaryTypeT"/> associated with the <paramref name="geotabId"/> from the <see cref="dbBinaryTypeTIdCache"/>. If the <see cref="DbBinaryTypeT"/> is not found in the <see cref="dbBinaryTypeTIdCache"/>, a new <see cref="DbBinaryTypeT"/> is added to the database for the subject <see cref="DbBinaryTypeT.id"/>, the <see cref="dbBinaryTypeTIdCache"/> is refreshed and the <see cref="DbBinaryTypeT.id"/> is then retrieved.
        /// </summary>
        /// <param name="geotabId">The <see cref="DbBinaryTypeT.GeotabId"/> for which to return the corresponding <see cref="DbBinaryTypeT.id"/>.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task<long> GetBinaryTypeIdAsync(string geotabId, CancellationTokenSource cancellationTokenSource)
        {
            var binaryTypeId = await dbBinaryTypeTObjectCache.GetObjectIdAsync(geotabId);
            if (binaryTypeId == null)
            {
                // DbBinaryTypeT not found in cache. Create a new one and persist it to database. 
                var dbBinaryTypeTsToPersist = new List<DbBinaryTypeT>();
                DbBinaryTypeT dbBinaryTypeT = new()
                {
                    DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                    GeotabId = geotabId,
                    RecordLastChangedUtc = DateTime.UtcNow
                };
                dbBinaryTypeTsToPersist.Add(dbBinaryTypeT);

                using (var uow = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                {
                    try
                    {
                        await dbBinaryTypeTEntityPersister.PersistEntitiesToDatabaseAsync(optimizerContext, dbBinaryTypeTsToPersist, cancellationTokenSource, Logging.LogLevel.Info);
                        await uow.CommitAsync();
                    }
                    catch (Exception)
                    {
                        await uow.RollBackAsync();
                        throw;
                    }
                }

                // Force the BinaryTypeT cache to be updated so that the changes are immediately available to other consumers.
                await dbBinaryTypeTObjectCache.UpdateAsync(true);
                binaryTypeId = await dbBinaryTypeTObjectCache.GetObjectIdAsync(geotabId);
            }
            return (long)binaryTypeId;
        }

        /// <summary>
        /// Retrieves the <see cref="DbControllerT.id"/> of the <see cref="DbControllerT"/> associated with the <paramref name="geotabId"/> from the <see cref="dbControllerTIdCache"/>. If the <see cref="DbControllerT"/> is not found in the <see cref="dbControllerTIdCache"/>, a new <see cref="DbControllerT"/> is added to the database for the subject <see cref="DbControllerT.id"/>, the <see cref="dbControllerTIdCache"/> is refreshed and the <see cref="DbControllerT.id"/> is then retrieved.
        /// </summary>
        /// <param name="geotabId">The <see cref="DbControllerT.GeotabId"/> for which to return the corresponding <see cref="DbControllerT.id"/>.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task<long> GetControllerIdAsync(string geotabId, CancellationTokenSource cancellationTokenSource)
        {
            var controllerId = await dbControllerTObjectCache.GetObjectIdAsync(geotabId);
            if (controllerId == null)
            {
                // DbControllerT not found in cache. Create a new one and persist it to database. 
                var dbControllerTsToPersist = new List<DbControllerT>();
                DbControllerT dbControllerT = new()
                {
                    DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                    GeotabId = geotabId,
                    RecordLastChangedUtc = DateTime.UtcNow
                };
                dbControllerTsToPersist.Add(dbControllerT);

                using (var uow = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                {
                    try
                    {
                        await dbControllerTEntityPersister.PersistEntitiesToDatabaseAsync(optimizerContext, dbControllerTsToPersist, cancellationTokenSource, Logging.LogLevel.Info);
                        await uow.CommitAsync();
                    }
                    catch (Exception)
                    {
                        await uow.RollBackAsync();
                        throw;
                    }
                }

                // Force the ControllerT cache to be updated so that the changes are immediately available to other consumers.
                await dbControllerTObjectCache.UpdateAsync(true);
                controllerId = await dbControllerTObjectCache.GetObjectIdAsync(geotabId);
            }
            return (long)controllerId;
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
        /// Starts the current <see cref="BinaryDataProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var dbOProcessorTrackings = await processorTracker.GetDbOProcessorTrackingListAsync();
            optimizerEnvironment.ValidateOptimizerEnvironment(dbOProcessorTrackings, DataOptimizerProcessor.BinaryDataProcessor);
            using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
            {
                try
                {
                    await processorTracker.UpdateDbOProcessorTrackingRecord(optimizerContext, DataOptimizerProcessor.BinaryDataProcessor, optimizerEnvironment.OptimizerVersion.ToString(), optimizerEnvironment.OptimizerMachineName);
                    await optimizerUOW.CommitAsync();
                }
                catch (Exception)
                {
                    await optimizerUOW.RollBackAsync();
                    throw;
                }
            }

            // Only start this service if it has been configured to be enabled.
            if (dataOptimizerConfiguration.EnableBinaryDataProcessor == true)
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
        /// Stops the current <see cref="BinaryDataProcessor"/> instance.
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
                DataOptimizerProcessor.DeviceProcessor
            };

            await prerequisiteProcessorChecker.WaitForPrerequisiteProcessorsIfNeededAsync(CurrentClassName, prerequisiteProcessors, cancellationToken);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
