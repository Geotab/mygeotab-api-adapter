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
    /// A <see cref="BackgroundService"/> that handles optimization and interpolation to populate additional columns added to the FaultDataT table in the Optimizer database. 
    /// </summary>
    class FaultDataOptimizer : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }
        static TimeSpan DateTimeBufferForInterpolationRecordRetrieval { get => TimeSpan.FromSeconds(120); }
        static int ThrottleEngagingBatchRecordCount { get => 100; }

        int lastBatchRecordCount = 0;

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterDatabaseObjectNames adapterDatabaseObjectNames;
        readonly IDataOptimizerDatabaseConnectionInfoContainer connectionInfoContainer;
        readonly IDataOptimizerConfiguration dataOptimizerConfiguration;
        readonly IDateTimeHelper dateTimeHelper;
        readonly ILongitudeLatitudeInterpolator longitudeLatitudeInterpolator;
        readonly IDbFaultDataDbFaultDataTEntityMapper dbFaultDataDbFaultDataTEntityMapper;
        readonly IGenericEntityPersister<DbFaultDataTDriverIdUpdate> dbFaultDataTDriverIdUpdateEntityPersister;
        readonly IGenericEntityPersister<DbFaultDataTLongLatUpdate> dbFaultDataTLongLatUpdateEntityPersister;
        readonly IExceptionHelper exceptionHelper;
        readonly IMessageLogger messageLogger;
        readonly IOptimizerEnvironment optimizerEnvironment;
        readonly IPrerequisiteProcessorChecker prerequisiteProcessorChecker;
        readonly IProcessorTracker processorTracker;
        readonly IStateMachine stateMachine;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;
        readonly IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> optimizerContext;

        /// <summary>
        /// The last time a call was initiated to retrieve a batch of DbFaultDataT records for processing.
        /// </summary>
        DateTime DbFaultDataTBatchLastRetrievedUtc { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FaultDataOptimizer"/> class.
        /// </summary>
        public FaultDataOptimizer(IAdapterDatabaseObjectNames adapterDatabaseObjectNames, IDataOptimizerDatabaseConnectionInfoContainer connectionInfoContainer, IDataOptimizerConfiguration dataOptimizerConfiguration, IDateTimeHelper dateTimeHelper, IDbFaultDataDbFaultDataTEntityMapper dbFaultDataDbFaultDataTEntityMapper, IGenericEntityPersister<DbFaultDataTDriverIdUpdate> dbFaultDataTDriverIdUpdateEntityPersister, IGenericEntityPersister<DbFaultDataTLongLatUpdate> dbFaultDataTLongLatUpdateEntityPersister, IExceptionHelper exceptionHelper, IMessageLogger messageLogger, ILongitudeLatitudeInterpolator longitudeLatitudeInterpolator, IOptimizerEnvironment optimizerEnvironment, IPrerequisiteProcessorChecker prerequisiteProcessorChecker, IProcessorTracker processorTracker, IStateMachine stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext, IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> optimizerContext)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.adapterDatabaseObjectNames = adapterDatabaseObjectNames;
            this.connectionInfoContainer = connectionInfoContainer;
            this.dataOptimizerConfiguration = dataOptimizerConfiguration;
            this.dateTimeHelper = dateTimeHelper;
            this.dbFaultDataDbFaultDataTEntityMapper = dbFaultDataDbFaultDataTEntityMapper;
            this.dbFaultDataTDriverIdUpdateEntityPersister = dbFaultDataTDriverIdUpdateEntityPersister;
            this.dbFaultDataTLongLatUpdateEntityPersister = dbFaultDataTLongLatUpdateEntityPersister;
            this.exceptionHelper = exceptionHelper;
            this.messageLogger = messageLogger;
            this.longitudeLatitudeInterpolator = longitudeLatitudeInterpolator;
            this.optimizerEnvironment = optimizerEnvironment;
            this.prerequisiteProcessorChecker = prerequisiteProcessorChecker;
            this.processorTracker = processorTracker;
            this.stateMachine = stateMachine;

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(OptimizerDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {nameof(FaultDataOptimizer)}.");

            this.optimizerContext = optimizerContext;
            logger.Debug($"{nameof(OptimizerDatabaseUnitOfWorkContext)} [Id: {optimizerContext.Id}] associated with {nameof(FaultDataOptimizer)}.");

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

            var lastPollTimeForLongLatUpdates = DateTime.MinValue;
            var lastPollTimeForDriverIdUpdates = DateTime.MinValue;

            while (!stoppingToken.IsCancellationRequested)
            {
                // If configured to operate on a schedule and the present time is currently outside of an operating window, delay until the next daily start time.
                if (dataOptimizerConfiguration.FaultDataOptimizerOperationMode == OperationMode.Scheduled)
                {
                    var timeSpanToNextDailyStartTimeUTC = dateTimeHelper.GetTimeSpanToNextDailyStartTimeUTC(dataOptimizerConfiguration.FaultDataOptimizerDailyStartTimeUTC, dataOptimizerConfiguration.FaultDataOptimizerDailyRunTimeSeconds);
                    if (timeSpanToNextDailyStartTimeUTC != TimeSpan.Zero)
                    {
                        DateTime nextScheduledStartTimeUTC = DateTime.UtcNow.Add(timeSpanToNextDailyStartTimeUTC);
                        messageLogger.LogScheduledServicePause(CurrentClassName, dataOptimizerConfiguration.FaultDataOptimizerDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.FaultDataOptimizerDailyRunTimeSeconds, nextScheduledStartTimeUTC);

                        await Task.Delay(timeSpanToNextDailyStartTimeUTC, stoppingToken);

                        DateTime nextScheduledPauseTimeUTC = DateTime.UtcNow.Add(TimeSpan.FromSeconds(dataOptimizerConfiguration.FaultDataOptimizerDailyRunTimeSeconds));
                        messageLogger.LogScheduledServiceResumption(CurrentClassName, dataOptimizerConfiguration.FaultDataOptimizerDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.FaultDataOptimizerDailyRunTimeSeconds, nextScheduledPauseTimeUTC);
                    }
                }

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
                        var processorTrackingInfoUpdated = false;
                        DbFaultDataTBatchLastRetrievedUtc = DateTime.UtcNow;

                        // Populate Longitude and Latitude fields if configured to do so:
                        if (dataOptimizerConfiguration.FaultDataOptimizerPopulateLongitudeLatitude == true)
                        {
                            // Only proceed if not being throttled due to the minimum record threshold having not been met on the previous iteration.
                            if (dateTimeHelper.TimeIntervalHasElapsed(lastPollTimeForLongLatUpdates, DateTimeIntervalType.Seconds, dataOptimizerConfiguration.FaultDataOptimizerExecutionIntervalSeconds))
                            {
                                DbFaultDataTBatchLastRetrievedUtc = DateTime.UtcNow;

                                // Get a batch of DbFaultDataTWithLagLeadLongLats.
                                IEnumerable<DbFaultDataTWithLagLeadLongLat> dbFaultDataTWithLagLeadLongLats = null;
                                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                                {
                                    using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                    {
                                        var dbFaultDataTWithLagLeadLongLatRepo = new BaseRepository<DbFaultDataTWithLagLeadLongLat>(optimizerContext);
                                        dbFaultDataTWithLagLeadLongLats = await dbFaultDataTWithLagLeadLongLatRepo.GetAllAsync(cancellationTokenSource);
                                    }
                                }, new Context());

                                lastBatchRecordCount = dbFaultDataTWithLagLeadLongLats.Count();
                                
                                // Implement throttling if necessary.
                                if (!dbFaultDataTWithLagLeadLongLats.Any() || lastBatchRecordCount < ThrottleEngagingBatchRecordCount)
                                {
                                    lastPollTimeForLongLatUpdates = DateTime.UtcNow;
                                    var delayTimeSpan = TimeSpan.FromSeconds(dataOptimizerConfiguration.FaultDataOptimizerExecutionIntervalSeconds);
                                    logger.Info($"{CurrentClassName} pausing Long/Lat optimization for {delayTimeSpan} because fewer than {ThrottleEngagingBatchRecordCount} records were processed during the current execution interval.");
                                }
                                else
                                {
                                    lastPollTimeForLongLatUpdates = DateTime.MinValue;
                                }

                                if (dbFaultDataTWithLagLeadLongLats.Any())
                                {
                                    // Process the batch of DbFaultDataTWithLagLeadLongLats.
                                    List<DbFaultDataTLongLatUpdate> dbFaultDataTLongLatUpdates = new();
                                    foreach (var dbFaultDataTWithLagLeadLongLat in dbFaultDataTWithLagLeadLongLats)
                                    {
                                        // Create a DbFaultDataTLongLatUpdate object to use for updating the subject record in the FaultDataT table.
                                        DbFaultDataTLongLatUpdate dbFaultDataTLongLatUpdate = new()
                                        {
                                            id = dbFaultDataTWithLagLeadLongLat.id,
                                            GeotabId = dbFaultDataTWithLagLeadLongLat.GeotabId,
                                            LongLatProcessed = true,
                                            DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update,
                                            RecordLastChangedUtc = DateTime.UtcNow
                                        };

                                        // If the DbFaultDataTWithLagLeadLongLat has lag/lead data, perform the interpolation steps. Otherwise, determine the reason for lack of lag/lead data and update the DbFaultDataTLongLatUpdate object accordingly to avoid scenarios where the same DbFaultDataT records keep getting retrieved even though it will never be possible to obtain lag/lead LogRecords and interpolate.
                                        if (dbFaultDataTWithLagLeadLongLat.LagDateTime != null && dbFaultDataTWithLagLeadLongLat.LeadDateTime != null)
                                        {
                                            // Get the interpolated coordinates for the current DbFaultDataTWithLagLeadLongLat.
                                            LongitudeLatitudeInterpolationResult longitudeLatitudeInterpolationResult = longitudeLatitudeInterpolator.InterpolateCoordinates(dbFaultDataTWithLagLeadLongLat.FaultDataDateTime, (DateTime)dbFaultDataTWithLagLeadLongLat.LagDateTime, (double)dbFaultDataTWithLagLeadLongLat.LagLongitude, (double)dbFaultDataTWithLagLeadLongLat.LagLatitude, (DateTime)dbFaultDataTWithLagLeadLongLat.LeadDateTime, (double)dbFaultDataTWithLagLeadLongLat.LeadLongitude, (double)dbFaultDataTWithLagLeadLongLat.LeadLatitude, dataOptimizerConfiguration.FaultDataOptimizerNumberOfCompassDirections);

                                            // If interpolation was successful, capture the coordinates. Otherwise, capture the reason why interpolation was unsuccessful.
                                            if (longitudeLatitudeInterpolationResult.Success)
                                            {
                                                dbFaultDataTLongLatUpdate.Longitude = longitudeLatitudeInterpolationResult.Longitude;
                                                dbFaultDataTLongLatUpdate.Latitude = longitudeLatitudeInterpolationResult.Latitude;
                                                if (dataOptimizerConfiguration.FaultDataOptimizerPopulateSpeed == true)
                                                {
                                                    dbFaultDataTLongLatUpdate.Speed = dbFaultDataTWithLagLeadLongLat.LagSpeed;
                                                }
                                                if (dataOptimizerConfiguration.FaultDataOptimizerPopulateBearing == true)
                                                {
                                                    dbFaultDataTLongLatUpdate.Bearing = longitudeLatitudeInterpolationResult.Bearing;
                                                }
                                                if (dataOptimizerConfiguration.FaultDataOptimizerPopulateDirection == true)
                                                {
                                                    dbFaultDataTLongLatUpdate.Direction = longitudeLatitudeInterpolationResult.Direction;
                                                }
                                            }
                                            else
                                            {
                                                dbFaultDataTLongLatUpdate.LongLatReason = (byte?)longitudeLatitudeInterpolationResult.Reason;
                                            }
                                        }
                                        else
                                        {
                                            if (dbFaultDataTWithLagLeadLongLat.FaultDataDateTime < dbFaultDataTWithLagLeadLongLat.LogRecordsTMinDateTime)
                                            {
                                                // The DateTime of the subject FaultDataT record is older than the DateTime of any LogRecordT records. It is highly unlikely that new LogRecords with older dates will come-in, since the adapter only moves forward in time once started. 
                                                dbFaultDataTLongLatUpdate.LongLatReason = (byte?)LongitudeLatitudeInterpolationResultReason.TargetEntityDateTimeBelowMinDbLogRecordTDateTime;
                                            }
                                            else if (dbFaultDataTWithLagLeadLongLat.FaultDataDateTime < dbFaultDataTWithLagLeadLongLat.DeviceLogRecordsTMinDateTime)
                                            {
                                                // The DateTime of the subject FaultDataT record is older than the DateTime of any LogRecordT records for the associated Device. It is highly unlikely that new LogRecords with older dates will come-in, since the adapter only moves forward in time once started. 
                                                dbFaultDataTLongLatUpdate.LongLatReason = (byte?)LongitudeLatitudeInterpolationResultReason.TargetEntityDateTimeBelowMinDbLogRecordTDateTimeForDevice;
                                            }
                                            else
                                            {
                                                // The lag/lead DbLogRecordT info was not found for an unknown reason.
                                                dbFaultDataTLongLatUpdate.LongLatReason = (byte?)LongitudeLatitudeInterpolationResultReason.LagLeadDbLogRecordTInfoNotFound;
                                            }
                                        }
                                        dbFaultDataTLongLatUpdates.Add(dbFaultDataTLongLatUpdate);
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
                                                // DbFaultDataTLongLatUpdate:
                                                dbFaultDataTLongLatUpdateEntityPersister.PersistEntitiesToDatabaseAsync(optimizerContext, dbFaultDataTLongLatUpdates, cancellationTokenSource, Logging.LogLevel.Info),
                                                // DbOProcessorTracking:
                                                processorTracker.UpdateDbOProcessorTrackingRecordAsync(optimizerContext, DataOptimizerProcessor.FaultDataOptimizer, DbFaultDataTBatchLastRetrievedUtc, null, null, null)
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
                                }
                                else
                                {
                                    logger.Debug($"No {nameof(DbFaultDataTWithLagLeadLongLat)} entities were returned during the current ExecuteAsync iteration of the {CurrentClassName}.");
                                }
                            }
                        }

                        // Populate DriverId field if configured to do so:
                        if (dataOptimizerConfiguration.FaultDataOptimizerPopulateDriverId == true)
                        {
                            // Only proceed if not being throttled due to the minimum record threshold having not been met on the previous iteration.
                            if (dateTimeHelper.TimeIntervalHasElapsed(lastPollTimeForDriverIdUpdates, DateTimeIntervalType.Seconds, dataOptimizerConfiguration.FaultDataOptimizerExecutionIntervalSeconds))
                            {
                                DbFaultDataTBatchLastRetrievedUtc = DateTime.UtcNow;

                                // Get a batch of DbFaultDataTWithLagLeadDriverChanges.
                                IEnumerable<DbFaultDataTWithLagLeadDriverChange> dbFaultDataTWithLagLeadDriverChanges = null;
                                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                                {
                                    using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                    {
                                        var dbFaultDataTWithLagLeadDriverChangeRepo = new BaseRepository<DbFaultDataTWithLagLeadDriverChange>(optimizerContext);
                                        dbFaultDataTWithLagLeadDriverChanges = await dbFaultDataTWithLagLeadDriverChangeRepo.GetAllAsync(cancellationTokenSource);
                                    }
                                }, new Context());

                                lastBatchRecordCount = dbFaultDataTWithLagLeadDriverChanges.Count();

                                // Implement throttling if necessary.
                                if (!dbFaultDataTWithLagLeadDriverChanges.Any() || lastBatchRecordCount < ThrottleEngagingBatchRecordCount)
                                {
                                    lastPollTimeForDriverIdUpdates = DateTime.UtcNow;
                                    var delayTimeSpan = TimeSpan.FromSeconds(dataOptimizerConfiguration.FaultDataOptimizerExecutionIntervalSeconds);
                                    logger.Info($"{CurrentClassName} pausing DriverId optimization for {delayTimeSpan} because fewer than {ThrottleEngagingBatchRecordCount} records were processed during the current execution interval.");
                                }
                                else
                                {
                                    lastPollTimeForDriverIdUpdates = DateTime.MinValue;
                                }

                                if (dbFaultDataTWithLagLeadDriverChanges.Any())
                                {
                                    // Process the batch of DbFaultDataTWithLagLeadDriverChanges.
                                    List<DbFaultDataTDriverIdUpdate> dbFaultDataTDriverIdUpdates = new();
                                    foreach (var dbFaultDataTWithLagLeadDriverChange in dbFaultDataTWithLagLeadDriverChanges)
                                    {
                                        // Create a DbFaultDataTDriverIdUpdate object to use for updating the subject record in the FaultDataT table.
                                        DbFaultDataTDriverIdUpdate dbFaultDataTDriverIdUpdate = new()
                                        {
                                            id = dbFaultDataTWithLagLeadDriverChange.id,
                                            GeotabId = dbFaultDataTWithLagLeadDriverChange.GeotabId,
                                            DriverIdProcessed = true,
                                            DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update,
                                            RecordLastChangedUtc = DateTime.UtcNow
                                        };

                                        // If the DbFaultDataTWithLagLeadDriverChange has a LagDateTime, grab the DriverId. Otherwise, determine the reason for lack of LagDateTime and update the DbFaultDataTDriverIdUpdate object accordingly to avoid scenarios where the same DbFaultDataT records keep getting retrieved even though it will never be possible to obtain LagDateTime and DriverId information.
                                        if (dbFaultDataTWithLagLeadDriverChange.LagDateTime != null)
                                        {
                                            dbFaultDataTDriverIdUpdate.DriverId = dbFaultDataTWithLagLeadDriverChange.DriverId;
                                        }
                                        else
                                        {
                                            if (dbFaultDataTWithLagLeadDriverChange.FaultDataDateTime < dbFaultDataTWithLagLeadDriverChange.DriverChangesTMinDateTime)
                                            {
                                                // The DateTime of the subject FaultDataT record is older than the DateTime of any DriverChangeT records. It is highly unlikely that new DriverChanges with older dates will come-in, since the adapter only moves forward in time once started.
                                                dbFaultDataTDriverIdUpdate.DriverIdReason = (byte?)DriverIdEstimationResultReason.TargetEntityDateTimeBelowMinDbDriverChangeTDateTime;
                                            }
                                            else if (dbFaultDataTWithLagLeadDriverChange.FaultDataDateTime < dbFaultDataTWithLagLeadDriverChange.DeviceDriverChangesTMinDateTime)
                                            {
                                                // The DateTime of the subject FaultDataT record is older than the DateTime of any DriverChangeT records for the associated Device. It is highly unlikely that new DriverChanges with older dates will come-in, since the adapter only moves forward in time once started.
                                                dbFaultDataTDriverIdUpdate.DriverIdReason = (byte?)DriverIdEstimationResultReason.TargetEntityDateTimeBelowMinDbDriverChangeTDateTimeForDevice;
                                            }
                                            else
                                            {
                                                // The lag DbDriverChangeT info was not found for an unknown reason.
                                                dbFaultDataTDriverIdUpdate.DriverIdReason = (byte?)DriverIdEstimationResultReason.LagDbDriverChangeTNotFound;
                                            }
                                        }
                                        dbFaultDataTDriverIdUpdates.Add(dbFaultDataTDriverIdUpdate);
                                    }

                                    // Persist changes to database.  Run tasks in parallel.
                                    await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                                    {
                                        using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                        {
                                            try
                                            {
                                                var dbEntityPersistenceTasks = new List<Task>
                                            {
                                                // DbFaultDataTDriverIdUpdate:
                                                dbFaultDataTDriverIdUpdateEntityPersister.PersistEntitiesToDatabaseAsync(optimizerContext, dbFaultDataTDriverIdUpdates, cancellationTokenSource, Logging.LogLevel.Info),
                                                // DbOProcessorTracking:
                                                processorTracker.UpdateDbOProcessorTrackingRecordAsync(optimizerContext, DataOptimizerProcessor.FaultDataOptimizer, DbFaultDataTBatchLastRetrievedUtc, null, null, null)
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
                                }
                                else
                                {
                                    logger.Debug($"No {nameof(DbFaultDataTWithLagLeadDriverChange)} entities were returned during the current ExecuteAsync iteration of the {CurrentClassName}.");
                                }
                            }
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
                                        await processorTracker.UpdateDbOProcessorTrackingRecordAsync(optimizerContext, DataOptimizerProcessor.FaultDataOptimizer, DbFaultDataTBatchLastRetrievedUtc, null, null, null);
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

                        // If all optimization processes are configured to be throttled, add a delay to prevent unnecessary CPU usage.
                        var addDelay = false;
                        if (lastPollTimeForLongLatUpdates != DateTime.MinValue && lastPollTimeForDriverIdUpdates != DateTime.MinValue)
                        {
                            addDelay = true;
                        }
                        else if (lastPollTimeForLongLatUpdates != DateTime.MinValue && dataOptimizerConfiguration.FaultDataOptimizerPopulateDriverId == false)
                        {
                            addDelay = true;
                        }
                        else if (lastPollTimeForDriverIdUpdates != DateTime.MinValue && dataOptimizerConfiguration.FaultDataOptimizerPopulateLongitudeLatitude == false)
                        {
                            addDelay = true;
                        }
                        if (addDelay == true)
                        {
                            var delayTimeSpan = TimeSpan.FromSeconds(dataOptimizerConfiguration.FaultDataOptimizerExecutionIntervalSeconds);
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
        /// Starts the current <see cref="FaultDataOptimizer"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var dbOProcessorTrackings = await processorTracker.GetDbOProcessorTrackingListAsync();
            optimizerEnvironment.ValidateOptimizerEnvironment(dbOProcessorTrackings, DataOptimizerProcessor.FaultDataOptimizer);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                {
                    try
                    {
                        await processorTracker.UpdateDbOProcessorTrackingRecordAsync(optimizerContext, DataOptimizerProcessor.FaultDataOptimizer, optimizerEnvironment.OptimizerVersion.ToString(), optimizerEnvironment.OptimizerMachineName);
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

            // Only start this service if it has been configured to be enabled along with at least one of its optimization tasks.
            if (dataOptimizerConfiguration.EnableFaultDataOptimizer == true)
            {
                if (dataOptimizerConfiguration.FaultDataOptimizerPopulateLongitudeLatitude || dataOptimizerConfiguration.FaultDataOptimizerPopulateDriverId)
                {
                    logger.Info($"******** STARTING SERVICE: {CurrentClassName}");
                    await base.StartAsync(cancellationToken);
                }
                else
                {
                    logger.Warn($"******** WARNING - SERVICE DISABLED: The {CurrentClassName} service has been enabled, but none of its optimization tasks have been enabled. As a result, the service will NOT be started.");
                }
            }
            else
            {
                logger.Warn($"******** WARNING - SERVICE DISABLED: The {CurrentClassName} service has not been enabled and will NOT be started.");
            }
        }

        /// <summary>
        /// Stops the current <see cref="FaultDataOptimizer"/> instance.
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
            };

            await prerequisiteProcessorChecker.WaitForPrerequisiteProcessorsIfNeededAsync(CurrentClassName, prerequisiteProcessors, cancellationToken);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
