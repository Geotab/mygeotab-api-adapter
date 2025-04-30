using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DataEnhancement;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Geospatial;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.Logging;
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

namespace MyGeotabAPIAdapter.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that interpolates latitude, longitude, speed, bearing and direction for <see cref="DbStatusData"/> records using <see cref="DbLogRecord2"/> records and pupulates the <see cref="DbStatusDataLocation2.DatabaseTableName"/> with the interpolated values.
    /// </summary>
    class StatusDataLocationService2 : BackgroundService
    {
        // SQL statements for the spStatusData2WithLagLeadLongLatBatch stored procedure / function.
        const string spStatusData2WithLagLeadLongLatBatchSQL_Postgres = @"SELECT * FROM public.""spStatusData2WithLagLeadLongLatBatch""(@MaxDaysPerBatch::integer, @MaxBatchSize::integer, @BufferMinutes::integer);";
        const string spStatusData2WithLagLeadLongLatBatchSQL_SQLServer = "EXEC [dbo].[spStatusData2WithLagLeadLongLatBatch] @MaxDaysPerBatch = @MaxDaysPerBatch, @MaxBatchSize = @MaxBatchSize, @BufferMinutes = @BufferMinutes;";

        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }
        static int ThrottleEngagingBatchRecordCount { get => 1; }

        int lastBatchRecordCount = 0;
        int interpolationProgressCheckIntervalMinutes = 5;
        DateTime lastInterpolationProgressCheck = DateTime.MinValue;

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IBaseRepository<DbStatusData2WithLagLeadLongLat> dbStatusData2WithLagLeadLongLatRepo;
        readonly IBaseRepository<DbvwStatForLocationInterpolationProgress> dbvwStatForLocationInterpolationProgressRepo;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterDatabaseObjectNames adapterDatabaseObjectNames;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<StatusDataLocationService2> awaiter;
        readonly IDateTimeHelper dateTimeHelper;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbStatusDataLocation2> dbStatusDataLocation2EntityPersister;
        readonly IGeospatialHelper geospatialHelper;
        readonly ILocationInterpolator locationInterpolator;
        readonly IMessageLogger messageLogger;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// The last time a call was initiated to retrieve a batch of DbStatusData2 records for processing.
        /// </summary>
        DateTime DbStatusData2BatchLastRetrievedUtc { get; set; }

        public StatusDataLocationService2(IAdapterConfiguration adapterConfiguration, IAdapterDatabaseObjectNames adapterDatabaseObjectNames, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<StatusDataLocationService2> awaiter, IDateTimeHelper dateTimeHelper, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbStatusDataLocation2> dbStatusDataLocation2EntityPersister, IGeospatialHelper geospatialHelper, ILocationInterpolator locationInterpolator, IMessageLogger messageLogger, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterDatabaseObjectNames = adapterDatabaseObjectNames;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.dateTimeHelper = dateTimeHelper;
            this.exceptionHelper = exceptionHelper;
            this.dbStatusDataLocation2EntityPersister = dbStatusDataLocation2EntityPersister;
            this.geospatialHelper = geospatialHelper;
            this.locationInterpolator = locationInterpolator;
            this.messageLogger = messageLogger;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;

            dbStatusData2WithLagLeadLongLatRepo = new BaseRepository<DbStatusData2WithLagLeadLongLat>(adapterContext);
            dbvwStatForLocationInterpolationProgressRepo = new BaseRepository<DbvwStatForLocationInterpolationProgress>(adapterContext);

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            var lastPollTimeForLongLatUpdates = DateTime.MinValue;

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait if necessary.
                var prerequisiteServices = new List<AdapterService> { };
                await awaiter.WaitForPrerequisiteServicesIfNeededAsync(prerequisiteServices, stoppingToken);
                await awaiter.WaitForDatabaseMaintenanceCompletionIfNeededAsync(stoppingToken);
                await awaiter.WaitForConnectivityRestorationIfNeededAsync(stoppingToken);

                // If configured to operate on a schedule and the present time is currently outside of an operating window, delay until the next daily start time.
                if (adapterConfiguration.StatusDataLocationServiceOperationMode == OperationMode.Scheduled)
                {
                    var timeSpanToNextDailyStartTimeUTC = dateTimeHelper.GetTimeSpanToNextDailyStartTimeUTC(adapterConfiguration.StatusDataLocationServiceDailyStartTimeUTC, adapterConfiguration.StatusDataLocationServiceDailyRunTimeSeconds);
                    if (timeSpanToNextDailyStartTimeUTC != TimeSpan.Zero)
                    {
                        DateTime nextScheduledStartTimeUTC = DateTime.UtcNow.Add(timeSpanToNextDailyStartTimeUTC);
                        messageLogger.LogScheduledServicePause(CurrentClassName, adapterConfiguration.StatusDataLocationServiceDailyStartTimeUTC.TimeOfDay, adapterConfiguration.StatusDataLocationServiceDailyRunTimeSeconds, nextScheduledStartTimeUTC);

                        await Task.Delay(timeSpanToNextDailyStartTimeUTC, stoppingToken);

                        DateTime nextScheduledPauseTimeUTC = DateTime.UtcNow.Add(TimeSpan.FromSeconds(adapterConfiguration.StatusDataLocationServiceDailyRunTimeSeconds));
                        messageLogger.LogScheduledServiceResumption(CurrentClassName, adapterConfiguration.StatusDataLocationServiceDailyStartTimeUTC.TimeOfDay, adapterConfiguration.StatusDataLocationServiceDailyRunTimeSeconds, nextScheduledPauseTimeUTC);
                    }
                }

                await awaiter.WaitForDatabaseMaintenanceCompletionIfNeededAsync(stoppingToken);

                // Abort if waiting for connectivity restoration.
                if (stateMachine.CurrentState == State.Waiting)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    // Fire-and-forget for GetInterpolationProgressUpdateAsync
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await GetInterpolationProgressUpdateAsync();
                        }
                        catch (Exception ex)
                        {
                            exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                        }
                    }, stoppingToken);

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        var processorTrackingInfoUpdated = false;
                        DbStatusData2BatchLastRetrievedUtc = DateTime.UtcNow;

                        // Only proceed if not being throttled due to the minimum record threshold having not been met on the previous iteration.
                        if (dateTimeHelper.TimeIntervalHasElapsed(lastPollTimeForLongLatUpdates, DateTimeIntervalType.Seconds, adapterConfiguration.StatusDataLocationServiceExecutionIntervalSeconds))
                        {
                            DbStatusData2BatchLastRetrievedUtc = DateTime.UtcNow;

                            // Choose the SQL statement to use based on database provider type.
                            var sql = adapterContext.ProviderType switch
                            {
                                ConnectionInfo.DataAccessProviderType.PostgreSQL => spStatusData2WithLagLeadLongLatBatchSQL_Postgres,
                                ConnectionInfo.DataAccessProviderType.SQLServer => spStatusData2WithLagLeadLongLatBatchSQL_SQLServer,
                                _ => throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.")
                            };

                            // Set the parameters for the spStatusData2WithLagLeadLongLatBatchSQL stored procedure / function.
                            var parameters = new
                            {
                                MaxDaysPerBatch = adapterConfiguration.StatusDataLocationServiceMaxDaysPerBatch,
                                MaxBatchSize = adapterConfiguration.StatusDataLocationServiceMaxBatchSize,
                                BufferMinutes = adapterConfiguration.StatusDataLocationServiceBufferMinutes
                            };

                            // Execute the stored procedure / function to get a batch of DbStatusData2WithLagLeadLongLats.
                            IEnumerable<DbStatusData2WithLagLeadLongLat> dbStatusData2WithLagLeadLongLats = null;
                            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                            {
                                try
                                {
                                    dbStatusData2WithLagLeadLongLats = await dbStatusData2WithLagLeadLongLatRepo.QueryAsync(sql, parameters, cancellationTokenSource, true, adapterContext);
                                }
                                catch (Exception ex)
                                {
                                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                                    throw;
                                }
                            }, new Context());

                            lastBatchRecordCount = dbStatusData2WithLagLeadLongLats.Count();

                            if (dbStatusData2WithLagLeadLongLats.Any())
                            {
                                // Process the batch of DbStatusData2WithLagLeadLongLats.
                                List<DbStatusDataLocation2> dbStatusDataLocation2s = new();
                                var updateDateTime = DateTime.UtcNow;
                                foreach (var dbStatusData2WithLagLeadLongLat in dbStatusData2WithLagLeadLongLats)
                                {
                                    // Create a new DbStatusDataLocation2 object.
                                    DbStatusDataLocation2 dbStatusDataLocation2 = new()
                                    {
                                        id = dbStatusData2WithLagLeadLongLat.id,
                                        DeviceId = dbStatusData2WithLagLeadLongLat.DeviceId,
                                        DateTime = dbStatusData2WithLagLeadLongLat.StatusDataDateTime,
                                        DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update,
                                        LongLatProcessed = true,
                                        RecordLastChangedUtc = updateDateTime
                                    };

                                    // If the DbStatusData2WithLagLeadLongLat has lag/lead data, perform the interpolation steps. Otherwise, determine the reason for lack of lag/lead data and update the DbStatusDataLocation2 object accordingly to avoid scenarios where the same DbStatusData2 records keep getting retrieved even though it will never be possible to obtain lag/lead LogRecords and interpolate.
                                    if (dbStatusData2WithLagLeadLongLat.LagDateTime != null && dbStatusData2WithLagLeadLongLat.LeadDateTime != null)
                                    {
                                        // Get the interpolated coordinates for the current DbStatusData2WithLagLeadLongLat.
                                        LocationInterpolationResult locationInterpolationResult = locationInterpolator.InterpolateCoordinates(dbStatusData2WithLagLeadLongLat.StatusDataDateTime, (DateTime)dbStatusData2WithLagLeadLongLat.LagDateTime, (double)dbStatusData2WithLagLeadLongLat.LagLongitude, (double)dbStatusData2WithLagLeadLongLat.LagLatitude, (DateTime)dbStatusData2WithLagLeadLongLat.LeadDateTime, (double)dbStatusData2WithLagLeadLongLat.LeadLongitude, (double)dbStatusData2WithLagLeadLongLat.LeadLatitude, adapterConfiguration.StatusDataLocationServiceNumberOfCompassDirections);

                                        // If interpolation was successful, capture the coordinates. Otherwise, capture the reason why interpolation was unsuccessful.
                                        if (locationInterpolationResult.Success)
                                        {
                                            dbStatusDataLocation2.Longitude = locationInterpolationResult.Longitude;
                                            dbStatusDataLocation2.Latitude = locationInterpolationResult.Latitude;
                                            if (adapterConfiguration.StatusDataLocationServicePopulateSpeed == true)
                                            {
                                                dbStatusDataLocation2.Speed = dbStatusData2WithLagLeadLongLat.LagSpeed;
                                            }
                                            if (adapterConfiguration.StatusDataLocationServicePopulateBearing == true)
                                            {
                                                dbStatusDataLocation2.Bearing = locationInterpolationResult.Bearing;
                                            }
                                            if (adapterConfiguration.StatusDataLocationServicePopulateDirection == true)
                                            {
                                                dbStatusDataLocation2.Direction = locationInterpolationResult.Direction;
                                            }
                                        }
                                        else
                                        {
                                            dbStatusDataLocation2.LongLatReason = (byte?)locationInterpolationResult.Reason;
                                        }
                                    }
                                    else
                                    {
                                        if (dbStatusData2WithLagLeadLongLat.StatusDataDateTime < dbStatusData2WithLagLeadLongLat.LogRecords2MinDateTime)
                                        {
                                            // The DateTime of the subject StatusData2 record is older than the DateTime of any LogRecordT records. It is highly unlikely that new LogRecords with older dates will come-in, since the adapter only moves forward in time once started. 
                                            dbStatusDataLocation2.LongLatReason = (byte?)LocationInterpolationResultReason.TargetEntityDateTimeBelowMinDbLogRecord2DateTime;
                                        }
                                        else if (dbStatusData2WithLagLeadLongLat.StatusDataDateTime < dbStatusData2WithLagLeadLongLat.DeviceLogRecords2MinDateTime)
                                        {
                                            // The DateTime of the subject StatusData2 record is older than the DateTime of any LogRecordT records for the associated Device. It is highly unlikely that new LogRecords with older dates will come-in, since the adapter only moves forward in time once started. 
                                            dbStatusDataLocation2.LongLatReason = (byte?)LocationInterpolationResultReason.TargetEntityDateTimeBelowMinDbLogRecord2DateTimeForDevice;
                                        }
                                        else
                                        {
                                            // The lag/lead DbLogRecord2 info was not found for an unknown reason.
                                            dbStatusDataLocation2.LongLatReason = (byte?)LocationInterpolationResultReason.LagLeadDbLogRecord2InfoNotFound;
                                        }
                                    }
                                    dbStatusDataLocation2s.Add(dbStatusDataLocation2);
                                }

                                // Persist dbStatusDataLocation2s without using a transaction to avoid contention with other services. There is no real risk of data inconsistency in this case, since any DbStatusData2 records that are not processed during the current iteration will be picked up during the next iteration. While a UOW (transaction) is not used, a retry policy is still used to ensure that the database operations are retried in case of transient errors such as deadlocks. 
                                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                                {
                                    try
                                    {
                                        await dbStatusDataLocation2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbStatusDataLocation2s, cancellationTokenSource, Logging.LogLevel.Info, true, true);
                                    }
                                    catch (Exception ex)
                                    {
                                        exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                                        throw;
                                    }
                                }, new Context());

                                // Persist changes to database. Run tasks in parallel.
                                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                                {
                                    using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                    {
                                        try
                                        {
                                            var dbEntityPersistenceTasks = new List<Task>
                                            {
                                                // DbOServiceTracking:
                                                serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.StatusDataLocationService2, DbStatusData2BatchLastRetrievedUtc)
                                            };
                                            await Task.WhenAll(dbEntityPersistenceTasks);

                                            // Commit transactions:
                                            await adapterUOW.CommitAsync();
                                            processorTrackingInfoUpdated = true;
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
                            else
                            {
                                logger.Debug($"No {nameof(DbStatusData2WithLagLeadLongLat)} entities were returned during the current ExecuteAsync iteration of the {CurrentClassName}.");
                            }
                        }

                        // Update processor tracking info if not already done.
                        if (processorTrackingInfoUpdated == false)
                        {
                            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                            {
                                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                {
                                    try
                                    {
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.StatusDataLocationService2, DbStatusData2BatchLastRetrievedUtc);
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

                        // Throttle if necessary.
                        if (lastBatchRecordCount < ThrottleEngagingBatchRecordCount)
                        {
                            lastPollTimeForLongLatUpdates = DateTime.UtcNow;
                            var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.StatusDataLocationServiceExecutionIntervalSeconds);
                            logger.Info($"{CurrentClassName} pausing for {delayTimeSpan} because fewer than {ThrottleEngagingBatchRecordCount} records were processed during the current execution interval.");
                            await Task.Delay(delayTimeSpan, stoppingToken);
                        }
                        else
                        {
                            lastPollTimeForLongLatUpdates = DateTime.MinValue;
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
                    exceptionHelper.LogException(databaseConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    stateMachine.HandleException(databaseConnectionException, NLogLogLevelName.Error);
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
                    stateMachine.HandleException(ex, NLogLogLevelName.Fatal);
                }
            }
        }

        /// <summary>
        /// Retrieves and logs information on the progress of the location interpolation process.
        /// </summary>
        /// <returns></returns>
        async Task GetInterpolationProgressUpdateAsync()
        {
            if (dateTimeHelper.TimeIntervalHasElapsed(lastInterpolationProgressCheck, DateTimeIntervalType.Minutes, interpolationProgressCheckIntervalMinutes) == false)
            {
                return;
            }

            // Get information on the progress of the location interpolation process.
            List<DbvwStatForLocationInterpolationProgress> dbvwStatForLocationInterpolationProgresss = new();
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(adapterConfiguration.TimeoutSecondsForDatabaseTasks));
                    dbvwStatForLocationInterpolationProgresss = (List<DbvwStatForLocationInterpolationProgress>)await dbvwStatForLocationInterpolationProgressRepo.GetAllAsync(cts, null, null, nameof(DbvwStatForLocationInterpolationProgress.Table), true, adapterContext);
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    throw;
                }
            }, new Context());

            // Log any progress information that was retrieved.
            if (dbvwStatForLocationInterpolationProgresss.Count != 0)
            {
                var sb = new StringBuilder();
                sb.Append($"Interpolation progress update: [");
                foreach (var dbvwStatForLocationInterpolationProgress in dbvwStatForLocationInterpolationProgresss)
                {
                    sb.Append($"Table: {dbvwStatForLocationInterpolationProgress.Table}; Total: {dbvwStatForLocationInterpolationProgress.Total}; LongLatProcessedTotal; {dbvwStatForLocationInterpolationProgress.LongLatProcessedTotal}; LongLatProcessedPercentage: {dbvwStatForLocationInterpolationProgress.LongLatProcessedPercentage} | ");
                }
                // Remove the last " | " and add a closing bracket.
                sb.Length -= 3;
                sb.Append("]");
                logger.Info(sb.ToString());
            }

            lastInterpolationProgressCheck = DateTime.UtcNow;
        }

        /// <summary>
        /// Starts the current <see cref="StatusDataLocationService2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.StatusDataLocationService2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.StatusDataLocationService2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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

            // Register this service with the StateMachine. Set mustPauseForDatabaseMaintenance to true if the service is enabled or false otherwise.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(StatusDataLocationService2), adapterConfiguration.EnableStatusDataLocationService);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableStatusDataLocationService == true)
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
        /// Stops the current <see cref="StatusDataLocationService2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(StatusDataLocationService2), false);
            }

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
