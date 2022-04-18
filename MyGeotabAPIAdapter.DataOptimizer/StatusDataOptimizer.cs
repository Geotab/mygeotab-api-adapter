using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityMappers;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DataOptimizer.EstimatorsAndInterpolators;
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
    /// A <see cref="BackgroundService"/> that handles optimization and interpolation to populate additional columns added to the StatusDataT table in the Optimizer database. 
    /// </summary>
    class StatusDataOptimizer : BackgroundService
    {
        string AssemblyName { get => GetType().Assembly.GetName().Name; }
        string AssemblyVersion { get => GetType().Assembly.GetName().Version.ToString(); }
        static string CurrentClassName { get => nameof(StatusDataOptimizer); }
        static string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }
        static TimeSpan DateTimeBufferForInterpolationRecordRetrieval { get => TimeSpan.FromSeconds(120); }
        static int ThrottleEngagingBatchRecordCount { get => 500; }

        int lastBatchRecordCount = 0;

        readonly IAdapterDatabaseObjectNames adapterDatabaseObjectNames;
        readonly IConnectionInfoContainer connectionInfoContainer;
        readonly IDataOptimizerConfiguration dataOptimizerConfiguration;
        readonly IDateTimeHelper dateTimeHelper;
        readonly IDbStatusDataDbStatusDataTEntityMapper dbStatusDataDbStatusDataTEntityMapper;
        readonly IGenericEntityPersister<DbStatusDataTDriverIdUpdate> dbStatusDataTDriverIdUpdateEntityPersister;
        readonly IGenericEntityPersister<DbStatusDataTLongLatUpdate> dbStatusDataTLongLatUpdateEntityPersister;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbStatusDataT> dbStatusDataTEntityPersister;
        readonly ILongitudeLatitudeInterpolator longitudeLatitudeInterpolator;
        readonly IMessageLogger messageLogger;
        readonly IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames;
        readonly IOptimizerEnvironment optimizerEnvironment;
        readonly IProcessorTracker processorTracker;
        readonly IStateMachine stateMachine;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly UnitOfWorkContext adapterContext;
        readonly UnitOfWorkContext optimizerContext;

        /// <summary>
        /// The last time a call was initiated to retrieve a batch of DbStatusDataT records for processing.
        /// </summary>
        DateTime DbStatusDataTBatchLastRetrievedUtc { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusDataOptimizer"/> class.
        /// </summary>
        public StatusDataOptimizer(IAdapterDatabaseObjectNames adapterDatabaseObjectNames, IConnectionInfoContainer connectionInfoContainer, IDataOptimizerConfiguration dataOptimizerConfiguration, IDateTimeHelper dateTimeHelper, IDbStatusDataDbStatusDataTEntityMapper dbStatusDataDbStatusDataTEntityMapper, IGenericEntityPersister<DbStatusDataT> dbStatusDataTEntityPersister, IGenericEntityPersister<DbStatusDataTDriverIdUpdate> dbStatusDataTDriverIdUpdateEntityPersister, IGenericEntityPersister<DbStatusDataTLongLatUpdate> dbStatusDataTLongLatUpdateEntityPersister, IExceptionHelper exceptionHelper, ILongitudeLatitudeInterpolator longitudeLatitudeInterpolator, IMessageLogger messageLogger, IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames, IOptimizerEnvironment optimizerEnvironment, IProcessorTracker processorTracker, IStateMachine stateMachine, UnitOfWorkContext adapterContext, UnitOfWorkContext optimizerContext)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(UnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {nameof(StatusDataOptimizer)}.");

            this.adapterDatabaseObjectNames = adapterDatabaseObjectNames;
            this.connectionInfoContainer = connectionInfoContainer;
            this.dataOptimizerConfiguration = dataOptimizerConfiguration;
            this.dateTimeHelper = dateTimeHelper;
            this.dbStatusDataDbStatusDataTEntityMapper = dbStatusDataDbStatusDataTEntityMapper;
            this.dbStatusDataTEntityPersister = dbStatusDataTEntityPersister;
            this.dbStatusDataTDriverIdUpdateEntityPersister = dbStatusDataTDriverIdUpdateEntityPersister;
            this.dbStatusDataTLongLatUpdateEntityPersister = dbStatusDataTLongLatUpdateEntityPersister;
            this.exceptionHelper = exceptionHelper;
            this.optimizerEnvironment = optimizerEnvironment;
            this.longitudeLatitudeInterpolator = longitudeLatitudeInterpolator;
            this.messageLogger = messageLogger;
            this.optimizerDatabaseObjectNames = optimizerDatabaseObjectNames;
            this.processorTracker = processorTracker;
            this.stateMachine = stateMachine;

            this.optimizerContext = optimizerContext;
            logger.Debug($"{nameof(UnitOfWorkContext)} [Id: {optimizerContext.Id}] associated with {nameof(StatusDataOptimizer)}.");

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

            var lastPollTimeForLongLatUpdates = DateTime.MinValue;
            var lastPollTimeForDriverIdUpdates = DateTime.MinValue;

            while (!stoppingToken.IsCancellationRequested)
            {
                // If configured to operate on a schedule and the present time is currently outside of an operating window, delay until the next daily start time.
                if (dataOptimizerConfiguration.StatusDataOptimizerOperationMode == OperationMode.Scheduled)
                {
                    var timeSpanToNextDailyStartTimeUTC = dateTimeHelper.GetTimeSpanToNextDailyStartTimeUTC(dataOptimizerConfiguration.StatusDataOptimizerDailyStartTimeUTC, dataOptimizerConfiguration.StatusDataOptimizerDailyRunTimeSeconds);
                    if (timeSpanToNextDailyStartTimeUTC != TimeSpan.Zero)
                    {
                        DateTime nextScheduledStartTimeUTC = DateTime.UtcNow.Add(timeSpanToNextDailyStartTimeUTC);
                        messageLogger.LogScheduledServicePause(CurrentClassName, dataOptimizerConfiguration.StatusDataOptimizerDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.StatusDataOptimizerDailyRunTimeSeconds, nextScheduledStartTimeUTC);

                        await Task.Delay(timeSpanToNextDailyStartTimeUTC, stoppingToken);

                        DateTime nextScheduledPauseTimeUTC = DateTime.UtcNow.Add(TimeSpan.FromSeconds(dataOptimizerConfiguration.StatusDataOptimizerDailyRunTimeSeconds));
                        messageLogger.LogScheduledServiceResumption(CurrentClassName, dataOptimizerConfiguration.StatusDataOptimizerDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.StatusDataOptimizerDailyRunTimeSeconds, nextScheduledPauseTimeUTC);
                    }
                }

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
                        var processorTrackingInfoUpdated = false;
                        DbStatusDataTBatchLastRetrievedUtc = DateTime.UtcNow;

                        // Populate Longitude and Latitude fields if configured to do so:
                        if (dataOptimizerConfiguration.StatusDataOptimizerPopulateLongitudeLatitude == true)
                        {
                            // Only proceed if not being throttled due to the minimum record threshold having not been met on the previous iteration.
                            if (dateTimeHelper.TimeIntervalHasElapsed(lastPollTimeForLongLatUpdates, DateTimeIntervalType.Seconds, dataOptimizerConfiguration.StatusDataOptimizerExecutionIntervalSeconds))
                            {
                                DbStatusDataTBatchLastRetrievedUtc = DateTime.UtcNow;

                                // Get a batch of DbStatusDataTWithLagLeadLongLats.
                                IEnumerable<DbStatusDataTWithLagLeadLongLat> dbStatusDataTWithLagLeadLongLats;
                                using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                {
                                    var dbStatusDataTWithLagLeadLongLatRepo = new DbStatusDataTWithLagLeadLongLatRepository(optimizerContext, optimizerDatabaseObjectNames);
                                    dbStatusDataTWithLagLeadLongLats = await dbStatusDataTWithLagLeadLongLatRepo.ExecuteStoredProcedureQueryAsync(optimizerDatabaseObjectNames.DbStatusDataTWithLagLeadLongLatStoredProcedureName, null, cancellationTokenSource);
                                }

                                lastBatchRecordCount = dbStatusDataTWithLagLeadLongLats.Count();
                               
                                // Implement throttling if necessary.
                                if (!dbStatusDataTWithLagLeadLongLats.Any() || lastBatchRecordCount < ThrottleEngagingBatchRecordCount)
                                {
                                    lastPollTimeForLongLatUpdates = DateTime.UtcNow;
                                    var delayTimeSpan = TimeSpan.FromSeconds(dataOptimizerConfiguration.StatusDataOptimizerExecutionIntervalSeconds);
                                    logger.Info($"{CurrentClassName} pausing Long/Lat optimization for {delayTimeSpan} because fewer than {ThrottleEngagingBatchRecordCount} records were processed during the current execution interval.");
                                }
                                else
                                {
                                    lastPollTimeForLongLatUpdates = DateTime.MinValue;
                                }

                                if (dbStatusDataTWithLagLeadLongLats.Any())
                                {
                                    // Process the batch of DbStatusDataTWithLagLeadLongLats.
                                    List<DbStatusDataTLongLatUpdate> dbStatusDataTLongLatUpdates = new();
                                    foreach (var dbStatusDataTWithLagLeadLongLat in dbStatusDataTWithLagLeadLongLats)
                                    {
                                        // Create a DbStatusDataTLongLatUpdate object to use for updating the subject record in the StatusDataT table.
                                        DbStatusDataTLongLatUpdate dbStatusDataTLongLatUpdate = new()
                                        {
                                            id = dbStatusDataTWithLagLeadLongLat.id,
                                            GeotabId = dbStatusDataTWithLagLeadLongLat.GeotabId,
                                            LongLatProcessed = true,
                                            DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update,
                                            RecordLastChangedUtc = DateTime.UtcNow
                                        };

                                        // If the DbStatusDataTWithLagLeadLongLat has lag/lead data, perform the interpolation steps. Otherwise, determine the reason for lack of lag/lead data and update the DbStatusDataTLongLatUpdate object accordingly to avoid scenarios where the same DbStatusDataT records keep getting retrieved even though it will never be possible to obtain lag/lead LogRecords and interpolate.
                                        if (dbStatusDataTWithLagLeadLongLat.LagDateTime != null && dbStatusDataTWithLagLeadLongLat.LeadDateTime != null)
                                        {
                                            // Get the interpolated coordinates for the current DbStatusDataTWithLagLeadLongLat.
                                            LongitudeLatitudeInterpolationResult longitudeLatitudeInterpolationResult = longitudeLatitudeInterpolator.InterpolateCoordinates(dbStatusDataTWithLagLeadLongLat.StatusDataDateTime, (DateTime)dbStatusDataTWithLagLeadLongLat.LagDateTime, (double)dbStatusDataTWithLagLeadLongLat.LagLongitude, (double)dbStatusDataTWithLagLeadLongLat.LagLatitude, (DateTime)dbStatusDataTWithLagLeadLongLat.LeadDateTime, (double)dbStatusDataTWithLagLeadLongLat.LeadLongitude, (double)dbStatusDataTWithLagLeadLongLat.LeadLatitude, dataOptimizerConfiguration.StatusDataOptimizerNumberOfCompassDirections);

                                            // If interpolation was successful, capture the coordinates. Otherwise, capture the reason why interpolation was unsuccessful.
                                            if (longitudeLatitudeInterpolationResult.Success)
                                            {
                                                dbStatusDataTLongLatUpdate.Longitude = longitudeLatitudeInterpolationResult.Longitude;
                                                dbStatusDataTLongLatUpdate.Latitude = longitudeLatitudeInterpolationResult.Latitude;
                                                if (dataOptimizerConfiguration.StatusDataOptimizerPopulateSpeed == true)
                                                {
                                                    dbStatusDataTLongLatUpdate.Speed = dbStatusDataTWithLagLeadLongLat.LagSpeed;
                                                }
                                                if (dataOptimizerConfiguration.StatusDataOptimizerPopulateBearing == true)
                                                {
                                                    dbStatusDataTLongLatUpdate.Bearing = longitudeLatitudeInterpolationResult.Bearing;
                                                }
                                                if (dataOptimizerConfiguration.StatusDataOptimizerPopulateDirection == true)
                                                {
                                                    dbStatusDataTLongLatUpdate.Direction = longitudeLatitudeInterpolationResult.Direction;
                                                }
                                            }
                                            else
                                            {
                                                dbStatusDataTLongLatUpdate.LongLatReason = (byte?)longitudeLatitudeInterpolationResult.Reason;
                                            }
                                        }
                                        else
                                        {
                                            if (dbStatusDataTWithLagLeadLongLat.StatusDataDateTime < dbStatusDataTWithLagLeadLongLat.LogRecordsTMinDateTime)
                                            {
                                                // The DateTime of the subject StatusDataT record is older than the DateTime of any LogRecordT records. It is highly unlikely that new LogRecords with older dates will come-in, since the adapter only moves forward in time once started. 
                                                dbStatusDataTLongLatUpdate.LongLatReason = (byte?)LongitudeLatitudeInterpolationResultReason.TargetEntityDateTimeBelowMinDbLogRecordTDateTime;
                                            }
                                            else if (dbStatusDataTWithLagLeadLongLat.StatusDataDateTime < dbStatusDataTWithLagLeadLongLat.DeviceLogRecordsTMinDateTime)
                                            {
                                                // The DateTime of the subject StatusDataT record is older than the DateTime of any LogRecordT records for the associated Device. It is highly unlikely that new LogRecords with older dates will come-in, since the adapter only moves forward in time once started. 
                                                dbStatusDataTLongLatUpdate.LongLatReason = (byte?)LongitudeLatitudeInterpolationResultReason.TargetEntityDateTimeBelowMinDbLogRecordTDateTimeForDevice;
                                            }
                                            else
                                            {
                                                // The lag/lead DbLogRecordT info was not found for an unknown reason.
                                                dbStatusDataTLongLatUpdate.LongLatReason = (byte?)LongitudeLatitudeInterpolationResultReason.LagLeadDbLogRecordTInfoNotFound;
                                            }
                                        }
                                        dbStatusDataTLongLatUpdates.Add(dbStatusDataTLongLatUpdate);
                                    }

                                    // Persist changes to database.
                                    using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                    {
                                        try
                                        {
                                            // DbStatusDataTLongLatUpdate:
                                            await dbStatusDataTLongLatUpdateEntityPersister.PersistEntitiesToDatabaseAsync(optimizerContext, dbStatusDataTLongLatUpdates, cancellationTokenSource, Logging.LogLevel.Info);

                                            // DbOProcessorTracking:
                                            await processorTracker.UpdateDbOProcessorTrackingRecord(optimizerContext, DataOptimizerProcessor.StatusDataOptimizer, DbStatusDataTBatchLastRetrievedUtc, null, null, null);

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
                                }
                                else
                                {
                                    logger.Debug($"No {nameof(DbStatusDataTWithLagLeadLongLat)} entities were returned during the current ExecuteAsync iteration of the {CurrentClassName}.");
                                }
                            }
                        }

                        // Populate DriverId field if configured to do so:
                        if (dataOptimizerConfiguration.StatusDataOptimizerPopulateDriverId == true)
                        {
                            // Only proceed if not being throttled due to the minimum record threshold having not been met on the previous iteration.
                            if (dateTimeHelper.TimeIntervalHasElapsed(lastPollTimeForDriverIdUpdates, DateTimeIntervalType.Seconds, dataOptimizerConfiguration.StatusDataOptimizerExecutionIntervalSeconds))
                            {
                                DbStatusDataTBatchLastRetrievedUtc = DateTime.UtcNow;

                                // Get a batch of DbStatusDataTWithLagLeadDriverChanges.
                                IEnumerable<DbStatusDataTWithLagLeadDriverChange> dbStatusDataTWithLagLeadDriverChanges;
                                using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                {
                                    var dbStatusDataTWithLagLeadDriverChangeRepo = new DbStatusDataTWithLagLeadDriverChangeRepository(optimizerContext);
                                    dbStatusDataTWithLagLeadDriverChanges = await dbStatusDataTWithLagLeadDriverChangeRepo.GetAllAsync(cancellationTokenSource);
                                }

                                lastBatchRecordCount = dbStatusDataTWithLagLeadDriverChanges.Count();
                                
                                // Implement throttling if necessary.
                                if (!dbStatusDataTWithLagLeadDriverChanges.Any() || lastBatchRecordCount < ThrottleEngagingBatchRecordCount)
                                {
                                    lastPollTimeForDriverIdUpdates = DateTime.UtcNow;
                                    var delayTimeSpan = TimeSpan.FromSeconds(dataOptimizerConfiguration.StatusDataOptimizerExecutionIntervalSeconds);
                                    logger.Info($"{CurrentClassName} pausing DriverId optimization for {delayTimeSpan} because fewer than {ThrottleEngagingBatchRecordCount} records were processed during the current execution interval.");
                                }
                                else
                                {
                                    lastPollTimeForDriverIdUpdates = DateTime.MinValue;
                                }

                                if (dbStatusDataTWithLagLeadDriverChanges.Any())
                                {
                                     // Process the batch of DbStatusDataTWithLagLeadDriverChanges.
                                    List<DbStatusDataTDriverIdUpdate> dbStatusDataTDriverIdUpdates = new();
                                    foreach (var dbStatusDataTWithLagLeadDriverChange in dbStatusDataTWithLagLeadDriverChanges)
                                    {
                                        // Create a DbStatusDataTDriverIdUpdate object to use for updating the subject record in the StatusDataT table.
                                        DbStatusDataTDriverIdUpdate dbStatusDataTDriverIdUpdate = new()
                                        {
                                            id = dbStatusDataTWithLagLeadDriverChange.id,
                                            GeotabId = dbStatusDataTWithLagLeadDriverChange.GeotabId,
                                            DriverIdProcessed = true,
                                            DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update,
                                            RecordLastChangedUtc = DateTime.UtcNow
                                        };

                                        // If the DbStatusDataTWithLagLeadDriverChange has a LagDateTime, grab the DriverId. Otherwise, determine the reason for lack of LagDateTime and update the DbStatusDataTDriverIdUpdate object accordingly to avoid scenarios where the same DbStatusDataT records keep getting retrieved even though it will never be possible to obtain LagDateTime and DriverId information.
                                        if (dbStatusDataTWithLagLeadDriverChange.LagDateTime != null)
                                        {
                                            dbStatusDataTDriverIdUpdate.DriverId = dbStatusDataTWithLagLeadDriverChange.DriverId;
                                        }
                                        else
                                        {
                                            if (dbStatusDataTWithLagLeadDriverChange.StatusDataDateTime < dbStatusDataTWithLagLeadDriverChange.DriverChangesTMinDateTime)
                                            {
                                                // The DateTime of the subject StatusDataT record is older than the DateTime of any DriverChangeT records. It is highly unlikely that new DriverChanges with older dates will come-in, since the adapter only moves forward in time once started.
                                                dbStatusDataTDriverIdUpdate.DriverIdReason = (byte?)DriverIdEstimationResultReason.TargetEntityDateTimeBelowMinDbDriverChangeTDateTime;
                                            }
                                            else if (dbStatusDataTWithLagLeadDriverChange.StatusDataDateTime < dbStatusDataTWithLagLeadDriverChange.DeviceDriverChangesTMinDateTime)
                                            {
                                                // The DateTime of the subject StatusDataT record is older than the DateTime of any DriverChangeT records for the associated Device. It is highly unlikely that new DriverChanges with older dates will come-in, since the adapter only moves forward in time once started.
                                                dbStatusDataTDriverIdUpdate.DriverIdReason = (byte?)DriverIdEstimationResultReason.TargetEntityDateTimeBelowMinDbDriverChangeTDateTimeForDevice;
                                            }
                                            else
                                            {
                                                // The lag DbDriverChangeT info was not found for an unknown reason.
                                                dbStatusDataTDriverIdUpdate.DriverIdReason = (byte?)DriverIdEstimationResultReason.LagDbDriverChangeTNotFound;
                                            }
                                        }
                                        dbStatusDataTDriverIdUpdates.Add(dbStatusDataTDriverIdUpdate);
                                    }

                                    // Persist changes to database.
                                    using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                    {
                                        try
                                        {
                                            // DbStatusDataTDriverIdUpdate:
                                            await dbStatusDataTDriverIdUpdateEntityPersister.PersistEntitiesToDatabaseAsync(optimizerContext, dbStatusDataTDriverIdUpdates, cancellationTokenSource, Logging.LogLevel.Info);

                                            // DbOProcessorTracking:
                                            await processorTracker.UpdateDbOProcessorTrackingRecord(optimizerContext, DataOptimizerProcessor.StatusDataOptimizer, DbStatusDataTBatchLastRetrievedUtc, null, null, null);

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
                                }
                                else
                                {
                                    logger.Debug($"No {nameof(DbStatusDataTWithLagLeadDriverChange)} entities were returned during the current ExecuteAsync iteration of the {CurrentClassName}.");
                                }
                            }
                        }

                        // Update processor tracking info if not already done.
                        if (processorTrackingInfoUpdated == false)
                        {
                            using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                            {
                                try
                                {
                                    await processorTracker.UpdateDbOProcessorTrackingRecord(optimizerContext, DataOptimizerProcessor.StatusDataOptimizer, DbStatusDataTBatchLastRetrievedUtc, null, null, null);
                                    await optimizerUOW.CommitAsync();
                                }
                                catch (Exception)
                                {
                                    await optimizerUOW.RollBackAsync();
                                    throw;
                                }
                            }
                        }

                        // If all optimization processes are configured to be throttled, add a delay to prevent unnecessary CPU usage.
                        var addDelay = false;
                        if (lastPollTimeForLongLatUpdates != DateTime.MinValue && lastPollTimeForDriverIdUpdates != DateTime.MinValue)
                        { 
                            addDelay = true;
                        }
                        else if (lastPollTimeForLongLatUpdates != DateTime.MinValue && dataOptimizerConfiguration.StatusDataOptimizerPopulateDriverId == false)
                        {
                            addDelay = true;
                        }
                        else if (lastPollTimeForDriverIdUpdates != DateTime.MinValue && dataOptimizerConfiguration.StatusDataOptimizerPopulateLongitudeLatitude == false)
                        {
                            addDelay = true;
                        }
                        if (addDelay == true)
                        {
                            var delayTimeSpan = TimeSpan.FromSeconds(dataOptimizerConfiguration.StatusDataOptimizerExecutionIntervalSeconds);
                            logger.Info($"{CurrentClassName} pausing for {delayTimeSpan} because all optimization subprocesses have been throttled during the current execution interval.");
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
        /// Starts the current <see cref="StatusDataOptimizer"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var dbOProcessorTrackings = await processorTracker.GetDbOProcessorTrackingListAsync();
            optimizerEnvironment.ValidateOptimizerEnvironment(dbOProcessorTrackings, DataOptimizerProcessor.StatusDataOptimizer);
            using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
            {
                try
                {
                    await processorTracker.UpdateDbOProcessorTrackingRecord(optimizerContext, DataOptimizerProcessor.StatusDataOptimizer, optimizerEnvironment.OptimizerVersion.ToString(), optimizerEnvironment.OptimizerMachineName);
                    await optimizerUOW.CommitAsync();
                }
                catch (Exception)
                {
                    await optimizerUOW.RollBackAsync();
                    throw;
                }
            }

            // Only start this service if it has been configured to be enabled along with at least one of its optimization tasks.
            if (dataOptimizerConfiguration.EnableStatusDataOptimizer == true)
            {
                if (dataOptimizerConfiguration.StatusDataOptimizerPopulateLongitudeLatitude || dataOptimizerConfiguration.StatusDataOptimizerPopulateDriverId)
                {
                    logger.Info($"******** STARTING SERVICE: {AssemblyName}.{CurrentClassName} (v{AssemblyVersion})");
                    await base.StartAsync(cancellationToken);
                }
                else
                {
                    logger.Warn($"******** WARNING - SERVICE DISABLED: The {AssemblyName}.{CurrentClassName} service has been enabled, but none of its optimization tasks have been enabled. As a result, the service will NOT be started.");
                }
            }
            else
            {
                logger.Warn($"******** WARNING - SERVICE DISABLED: The {AssemblyName}.{CurrentClassName} service has not been enabled and will NOT be started.");
            }
        }

        /// <summary>
        /// Stops the current <see cref="StatusDataOptimizer"/> instance.
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
