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
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that interpolates latitude, longitude, speed, bearing and direction for <see cref="DbFaultData"/> records using <see cref="DbLogRecord2"/> records and pupulates the <see cref="DbFaultDataLocation2.DatabaseTableName"/> with the interpolated values.
    /// </summary>
    class FaultDataLocationService2 : BackgroundService
    {
        const string spFaultData2WithLagLeadLongLatBatchSQL_Postgres = @"SELECT * FROM public.""spFaultData2WithLagLeadLongLatBatch""(@MaxDaysPerBatch::integer, @MaxBatchSize::integer, @BufferMinutes::integer);";
        const string spFaultData2WithLagLeadLongLatBatchSQL_SQLServer = "EXEC [dbo].[spFaultData2WithLagLeadLongLatBatch] @MaxDaysPerBatch = @MaxDaysPerBatch, @MaxBatchSize = @MaxBatchSize, @BufferMinutes = @BufferMinutes;";

        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }
        static int ThrottleEngagingBatchRecordCount { get => 1; }

        int lastBatchRecordCount = 0;

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IBaseRepository<DbFaultData2WithLagLeadLongLat> dbFaultData2WithLagLeadLongLatRepo;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterDatabaseObjectNames adapterDatabaseObjectNames;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<FaultDataLocationService2> awaiter;
        readonly IDateTimeHelper dateTimeHelper;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbFaultDataLocation2> dbFaultDataLocation2EntityPersister;
        readonly IGeospatialHelper geospatialHelper;
        readonly ILocationInterpolator locationInterpolator;
        readonly IMessageLogger messageLogger;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// The last time a call was initiated to retrieve a batch of DbFaultData2 records for processing.
        /// </summary>
        DateTime DbFaultData2BatchLastRetrievedUtc { get; set; }

        public FaultDataLocationService2(IAdapterConfiguration adapterConfiguration, IAdapterDatabaseObjectNames adapterDatabaseObjectNames, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<FaultDataLocationService2> awaiter, IDateTimeHelper dateTimeHelper, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbFaultDataLocation2> dbFaultDataLocation2EntityPersister, IGeospatialHelper geospatialHelper, ILocationInterpolator locationInterpolator, IMessageLogger messageLogger, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterDatabaseObjectNames = adapterDatabaseObjectNames;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.dateTimeHelper = dateTimeHelper;
            this.exceptionHelper = exceptionHelper;
            this.dbFaultDataLocation2EntityPersister = dbFaultDataLocation2EntityPersister;
            this.geospatialHelper = geospatialHelper;
            this.locationInterpolator = locationInterpolator;
            this.messageLogger = messageLogger;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;

            dbFaultData2WithLagLeadLongLatRepo = new BaseRepository<DbFaultData2WithLagLeadLongLat>(adapterContext);

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
                var prerequisiteServices = new List<AdapterService> 
                {
                    AdapterService.DatabaseMaintenanceService2
                };
                await awaiter.WaitForPrerequisiteServicesIfNeededAsync(prerequisiteServices, stoppingToken);
                await awaiter.WaitForDatabaseMaintenanceCompletionIfNeededAsync(stoppingToken);
                await awaiter.WaitForConnectivityRestorationIfNeededAsync(stoppingToken);

                // If configured to operate on a schedule and the present time is currently outside of an operating window, delay until the next daily start time.
                if (adapterConfiguration.FaultDataLocationServiceOperationMode == OperationMode.Scheduled)
                {
                    var timeSpanToNextDailyStartTimeUTC = dateTimeHelper.GetTimeSpanToNextDailyStartTimeUTC(adapterConfiguration.FaultDataLocationServiceDailyStartTimeUTC, adapterConfiguration.FaultDataLocationServiceDailyRunTimeSeconds);
                    if (timeSpanToNextDailyStartTimeUTC != TimeSpan.Zero)
                    {
                        DateTime nextScheduledStartTimeUTC = DateTime.UtcNow.Add(timeSpanToNextDailyStartTimeUTC);
                        messageLogger.LogScheduledServicePause(CurrentClassName, adapterConfiguration.FaultDataLocationServiceDailyStartTimeUTC.TimeOfDay, adapterConfiguration.FaultDataLocationServiceDailyRunTimeSeconds, nextScheduledStartTimeUTC);

                        await Task.Delay(timeSpanToNextDailyStartTimeUTC, stoppingToken);

                        DateTime nextScheduledPauseTimeUTC = DateTime.UtcNow.Add(TimeSpan.FromSeconds(adapterConfiguration.FaultDataLocationServiceDailyRunTimeSeconds));
                        messageLogger.LogScheduledServiceResumption(CurrentClassName, adapterConfiguration.FaultDataLocationServiceDailyStartTimeUTC.TimeOfDay, adapterConfiguration.FaultDataLocationServiceDailyRunTimeSeconds, nextScheduledPauseTimeUTC);
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

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        var processorTrackingInfoUpdated = false;
                        DbFaultData2BatchLastRetrievedUtc = DateTime.UtcNow;

                        // Only proceed if not being throttled due to the minimum record threshold having not been met on the previous iteration.
                        if (dateTimeHelper.TimeIntervalHasElapsed(lastPollTimeForLongLatUpdates, DateTimeIntervalType.Seconds, adapterConfiguration.FaultDataLocationServiceExecutionIntervalSeconds))
                        {
                            DbFaultData2BatchLastRetrievedUtc = DateTime.UtcNow;

                            // Choose the SQL statement to use based on database provider type.
                            var sql = adapterContext.ProviderType switch
                            {
                                ConnectionInfo.DataAccessProviderType.PostgreSQL => spFaultData2WithLagLeadLongLatBatchSQL_Postgres,
                                ConnectionInfo.DataAccessProviderType.SQLServer => spFaultData2WithLagLeadLongLatBatchSQL_SQLServer,
                                _ => throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.")
                            };

                            // Set the parameters for the spFaultData2WithLagLeadLongLatBatchSQL stored procedure / function.
                            var parameters = new
                            {
                                MaxDaysPerBatch = adapterConfiguration.FaultDataLocationServiceMaxDaysPerBatch,
                                MaxBatchSize = adapterConfiguration.FaultDataLocationServiceMaxBatchSize,
                                BufferMinutes = adapterConfiguration.FaultDataLocationServiceBufferMinutes
                            };

                            // Execute the stored procedure / function to get a batch of DbFaultData2WithLagLeadLongLats.
                            IEnumerable<DbFaultData2WithLagLeadLongLat> dbFaultData2WithLagLeadLongLats = null;
                            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                            {
                                try
                                {
                                    dbFaultData2WithLagLeadLongLats = await dbFaultData2WithLagLeadLongLatRepo.QueryAsync(sql, parameters, cancellationTokenSource, true, adapterContext);
                                }
                                catch (Exception ex)
                                {
                                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                                    throw;
                                }
                            }, new Context());

                            lastBatchRecordCount = dbFaultData2WithLagLeadLongLats.Count();

                            if (dbFaultData2WithLagLeadLongLats.Any())
                            {
                                // Process the batch of DbFaultData2WithLagLeadLongLats.
                                List<DbFaultDataLocation2> dbFaultDataLocation2s = new();
                                var updateDateTime = DateTime.UtcNow;
                                foreach (var dbFaultData2WithLagLeadLongLat in dbFaultData2WithLagLeadLongLats)
                                {
                                    // Create new DbFaultDataLocation2 object.
                                    DbFaultDataLocation2 dbFaultDataLocation2 = new()
                                    {
                                        id = dbFaultData2WithLagLeadLongLat.id,
                                        DeviceId = dbFaultData2WithLagLeadLongLat.DeviceId,
                                        DateTime = dbFaultData2WithLagLeadLongLat.FaultDataDateTime,
                                        DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update,
                                        LongLatProcessed = true,
                                        RecordLastChangedUtc = updateDateTime
                                    };

                                    // If the DbFaultData2WithLagLeadLongLat has lag/lead data, perform the interpolation steps. Otherwise, determine the reason for lack of lag/lead data and update the DbFaultDataLocation2 object accordingly to avoid scenarios where the same DbFaultData2 records keep getting retrieved even though it will never be possible to obtain lag/lead LogRecords and interpolate.
                                    if (dbFaultData2WithLagLeadLongLat.LagDateTime != null && dbFaultData2WithLagLeadLongLat.LeadDateTime != null)
                                    {
                                        // Get the interpolated coordinates for the current DbFaultData2WithLagLeadLongLat.
                                        LocationInterpolationResult locationInterpolationResult = locationInterpolator.InterpolateCoordinates(dbFaultData2WithLagLeadLongLat.FaultDataDateTime, (DateTime)dbFaultData2WithLagLeadLongLat.LagDateTime, (double)dbFaultData2WithLagLeadLongLat.LagLongitude, (double)dbFaultData2WithLagLeadLongLat.LagLatitude, (DateTime)dbFaultData2WithLagLeadLongLat.LeadDateTime, (double)dbFaultData2WithLagLeadLongLat.LeadLongitude, (double)dbFaultData2WithLagLeadLongLat.LeadLatitude, adapterConfiguration.FaultDataLocationServiceNumberOfCompassDirections);

                                        // If interpolation was successful, capture the coordinates. Otherwise, capture the reason why interpolation was unsuccessful.
                                        if (locationInterpolationResult.Success)
                                        {
                                            dbFaultDataLocation2.Longitude = locationInterpolationResult.Longitude;
                                            dbFaultDataLocation2.Latitude = locationInterpolationResult.Latitude;
                                            if (adapterConfiguration.FaultDataLocationServicePopulateSpeed == true)
                                            {
                                                dbFaultDataLocation2.Speed = dbFaultData2WithLagLeadLongLat.LagSpeed;
                                            }
                                            if (adapterConfiguration.FaultDataLocationServicePopulateBearing == true)
                                            {
                                                dbFaultDataLocation2.Bearing = locationInterpolationResult.Bearing;
                                            }
                                            if (adapterConfiguration.FaultDataLocationServicePopulateDirection == true)
                                            {
                                                dbFaultDataLocation2.Direction = locationInterpolationResult.Direction;
                                            }
                                        }
                                        else
                                        {
                                            dbFaultDataLocation2.LongLatReason = (byte?)locationInterpolationResult.Reason;
                                        }
                                    }
                                    else
                                    {
                                        if (dbFaultData2WithLagLeadLongLat.FaultDataDateTime < dbFaultData2WithLagLeadLongLat.LogRecords2MinDateTime)
                                        {
                                            // The DateTime of the subject FaultData2 record is older than the DateTime of any LogRecordT records. It is highly unlikely that new LogRecords with older dates will come-in, since the adapter only moves forward in time once started. 
                                            dbFaultDataLocation2.LongLatReason = (byte?)LocationInterpolationResultReason.TargetEntityDateTimeBelowMinDbLogRecord2DateTime;
                                        }
                                        else if (dbFaultData2WithLagLeadLongLat.FaultDataDateTime < dbFaultData2WithLagLeadLongLat.DeviceLogRecords2MinDateTime)
                                        {
                                            // The DateTime of the subject FaultData2 record is older than the DateTime of any LogRecordT records for the associated Device. It is highly unlikely that new LogRecords with older dates will come-in, since the adapter only moves forward in time once started. 
                                            dbFaultDataLocation2.LongLatReason = (byte?)LocationInterpolationResultReason.TargetEntityDateTimeBelowMinDbLogRecord2DateTimeForDevice;
                                        }
                                        else
                                        {
                                            // The lag/lead DbLogRecord2 info was not found for an unknown reason.
                                            dbFaultDataLocation2.LongLatReason = (byte?)LocationInterpolationResultReason.LagLeadDbLogRecord2InfoNotFound;
                                        }
                                    }
                                    dbFaultDataLocation2s.Add(dbFaultDataLocation2);
                                }

                                // Persist dbFaultDataLocation2s without using a transaction to avoid contention with other services. There is no real risk of data inconsistency in this case, since any DbFaultData2 records that are not processed during the current iteration will be picked up during the next iteration. While a UOW (transaction) is not used, a retry policy is still used to ensure that the database operations are retried in case of transient errors such as deadlocks. 
                                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                                {
                                    try
                                    {
                                        await dbFaultDataLocation2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbFaultDataLocation2s, cancellationTokenSource, Logging.LogLevel.Info, true, true);
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
                                                serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.FaultDataLocationService2, DbFaultData2BatchLastRetrievedUtc)
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
                                logger.Debug($"No {nameof(DbFaultData2WithLagLeadLongLat)} entities were returned during the current ExecuteAsync iteration of the {CurrentClassName}.");
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
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.FaultDataLocationService2, DbFaultData2BatchLastRetrievedUtc);
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
                            var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.FaultDataLocationServiceExecutionIntervalSeconds);
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
        /// Starts the current <see cref="FaultDataLocationService2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.FaultDataLocationService2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.FaultDataLocationService2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
                stateMachine.RegisterService(nameof(FaultDataLocationService2), adapterConfiguration.EnableFaultDataLocationService);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableFaultDataLocationService == true)
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
        /// Stops the current <see cref="FaultDataLocationService2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(FaultDataLocationService2), false);
            }

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
