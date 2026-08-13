using Geotab.Checkmate.ObjectModel;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Enums;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.GeotabObjectMappers;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.MyGeotabAPI;
using NLog;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that extracts <see cref="ShipmentLog"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database.
    /// </summary>
    class ShipmentLogProcessor2 : BackgroundService
    {
        bool feedVersionRollbackRequired = false;

        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<ShipmentLogProcessor2> awaiter;
        readonly IBaseRepository<DbStgShipmentLog2> dbStgShipmentLog2Repo;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbStgShipmentLog2> dbStgShipmentLog2EntityPersister;
        readonly IGenericGeotabObjectFeeder<ShipmentLog> shipmentLogGeotabObjectFeeder;
        readonly IGeotabDeviceFilterer geotabDeviceFilterer;
        readonly IGeotabIdConverter geotabIdConverter;
        readonly IGeotabShipmentLogDbStgShipmentLog2ObjectMapper geotabShipmentLogDbStgShipmentLog2ObjectMapper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IForeignKeyServiceDependencyMap shipmentLogForeignKeyServiceDependencyMap;
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipmentLogProcessor2"/> class.
        /// </summary>
        public ShipmentLogProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<ShipmentLogProcessor2> awaiter, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbStgShipmentLog2> dbStgShipmentLog2EntityPersister, IGenericGeotabObjectFeeder<ShipmentLog> shipmentLogGeotabObjectFeeder, IGeotabDeviceFilterer geotabDeviceFilterer, IGeotabIdConverter geotabIdConverter, IGeotabShipmentLogDbStgShipmentLog2ObjectMapper geotabShipmentLogDbStgShipmentLog2ObjectMapper, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.exceptionHelper = exceptionHelper;
            this.dbStgShipmentLog2EntityPersister = dbStgShipmentLog2EntityPersister;
            this.shipmentLogGeotabObjectFeeder = shipmentLogGeotabObjectFeeder;
            this.geotabDeviceFilterer = geotabDeviceFilterer;
            this.geotabIdConverter = geotabIdConverter;
            this.geotabShipmentLogDbStgShipmentLog2ObjectMapper = geotabShipmentLogDbStgShipmentLog2ObjectMapper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;

            dbStgShipmentLog2Repo = new BaseRepository<DbStgShipmentLog2>(adapterContext);

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            // Setup the foreign key service dependency map.
            shipmentLogForeignKeyServiceDependencyMap = new ForeignKeyServiceDependencyMap(
                [
                    new ForeignKeyServiceDependency("FK_ShipmentLogs2_Devices2", AdapterService.DeviceProcessor2)
                ]
            );
        }

        /// <summary>
        /// Iteratively executes the business logic until the service is stopped.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const string MergeFunctionSQL_Postgres = @"SELECT public.""spMerge_stg_ShipmentLogs2""();";
            const string MergeProcedureSQL_SQLServer = @"EXEC [dbo].[spMerge_stg_ShipmentLogs2];";
            const string TruncateStagingTableSQL_Postgres = @"TRUNCATE TABLE public.""stg_ShipmentLogs2"";";
            const string TruncateStagingTableSQL_SQLServer = @"TRUNCATE TABLE [dbo].[stg_ShipmentLogs2];";

            MethodBase methodBase = MethodBase.GetCurrentMethod();
            var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.ShipmentLogFeedIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait if necessary.
                var prerequisiteServices = new List<AdapterService>
                {
                    AdapterService.DatabaseMaintenanceService2,
                    AdapterService.DeviceProcessor2,
                    AdapterService.UserProcessor2
                };
                await awaiter.WaitForPrerequisiteServicesIfNeededAsync(prerequisiteServices, stoppingToken);
                await awaiter.WaitForDatabaseMaintenanceCompletionIfNeededAsync(stoppingToken);
                var connectivityRestored = await awaiter.WaitForConnectivityRestorationIfNeededAsync(stoppingToken);
                if (connectivityRestored == true)
                {
                    feedVersionRollbackRequired = true;
                    connectivityRestored = false;
                }

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        var dbOServiceTracking = await serviceTracker.GetShipmentLogService2InfoAsync();

                        // Initialize the Geotab object feeder.
                        if (shipmentLogGeotabObjectFeeder.IsInitialized == false)
                        {
                            await shipmentLogGeotabObjectFeeder.InitializeAsync(cancellationTokenSource, adapterConfiguration.ShipmentLogFeedIntervalSeconds, myGeotabAPIHelper.GetFeedResultLimitDefault, (long?)dbOServiceTracking.LastProcessedFeedVersion);
                        }

                        // If this is the first iteration after a connectivity disruption, roll-back the LastFeedVersion of the GeotabObjectFeeder to the last processed feed version that was committed to the database and set the LastFeedRetrievalTimeUtc to DateTime.MinValue to start processing without further delay.
                        if (feedVersionRollbackRequired == true)
                        {
                            shipmentLogGeotabObjectFeeder.Rollback(dbOServiceTracking.LastProcessedFeedVersion);
                            feedVersionRollbackRequired = false;
                        }

                        // Get a batch of ShipmentLog objects from Geotab.
                        await shipmentLogGeotabObjectFeeder.GetFeedDataBatchAsync(cancellationTokenSource);
                        stoppingToken.ThrowIfCancellationRequested();

                        // Process any returned ShipmentLogs.
                        var shipmentLogs = shipmentLogGeotabObjectFeeder.GetFeedResultDataValuesList();
                        var dbStgShipmentLog2sToPersist = new List<DbStgShipmentLog2>();
                        if (shipmentLogs.Count != 0)
                        {
                            // Apply tracked device filter (if configured in appsettings.json).
                            var filteredShipmentLogs = await geotabDeviceFilterer.ApplyDeviceFilterAsync(cancellationTokenSource, shipmentLogs);

                            // Map the ShipmentLog objects to DbStgShipmentLog2 entities.
                            foreach (var shipmentLog in filteredShipmentLogs)
                            {
                                // Process shipmentLog.Device:
                                long? shipmentLogDeviceId = null;
                                if (shipmentLog.Device != null)
                                {
                                    if (shipmentLog.Device.GetType() == typeof(NoDevice))
                                    {
                                        shipmentLogDeviceId = AdapterDbSentinelIdsForMYGKnownIds.NoDeviceId;
                                    }
                                    else if (shipmentLog.Device.Id != null)
                                    {
                                        shipmentLogDeviceId = geotabIdConverter.ToLong(shipmentLog.Device.Id);
                                    }
                                }

                                // Process shipmentLog.Driver:
                                long? shipmentLogDriverId = null;
                                if (shipmentLog.Driver != null)
                                {
                                    if (shipmentLog.Driver.GetType() == typeof(NoDriver))
                                    {
                                        shipmentLogDriverId = AdapterDbSentinelIdsForMYGKnownIds.NoDriverId;
                                    }
                                    else if (shipmentLog.Driver.GetType() == typeof(UnknownDriver))
                                    {
                                        shipmentLogDriverId = AdapterDbSentinelIdsForMYGKnownIds.UnknownDriverId;
                                    }
                                    else if (shipmentLog.Driver.GetType() == typeof(NoUser))
                                    {
                                        shipmentLogDriverId = AdapterDbSentinelIdsForMYGKnownIds.NoUserId;
                                    }
                                    else if (shipmentLog.Driver.Id != null)
                                    {
                                        shipmentLogDriverId = geotabIdConverter.ToLong(shipmentLog.Driver.Id);
                                    }
                                }

                                var dbStgShipmentLog2 = geotabShipmentLogDbStgShipmentLog2ObjectMapper.CreateEntity(shipmentLog, shipmentLogDeviceId, shipmentLogDriverId);
                                dbStgShipmentLog2sToPersist.Add(dbStgShipmentLog2);
                            }
                        }

                        stoppingToken.ThrowIfCancellationRequested();

                        // Persist changes to database. Step 1: Persist the DbStgShipmentLog2 entities.
                        if (dbStgShipmentLog2sToPersist.Count != 0)
                        {
                            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                            {
                                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                {
                                    try
                                    {
                                        // Truncate staging table in case it contains any data:
                                        var sql = adapterContext.ProviderType switch
                                        {
                                            ConnectionInfo.DataAccessProviderType.PostgreSQL => TruncateStagingTableSQL_Postgres,
                                            ConnectionInfo.DataAccessProviderType.SQLServer => TruncateStagingTableSQL_SQLServer,
                                            _ => throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.")
                                        };
                                        await dbStgShipmentLog2Repo.ExecuteAsync(sql, null, cancellationTokenSource, true, adapterContext);

                                        // DbStgShipmentLog2:
                                        await dbStgShipmentLog2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbStgShipmentLog2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                        // Commit transactions:
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

                        // Persist changes to database. Step 2: Merge the DbStgShipmentLog2 entities into the DbShipmentLog2 table and update the DbOServiceTracking table.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    if (dbStgShipmentLog2sToPersist.Count != 0)
                                    {
                                        // Build the SQL statement to execute the merge procedure.
                                        var sql = adapterContext.ProviderType switch
                                        {
                                            ConnectionInfo.DataAccessProviderType.PostgreSQL => MergeFunctionSQL_Postgres,
                                            ConnectionInfo.DataAccessProviderType.SQLServer => MergeProcedureSQL_SQLServer,
                                            _ => throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.")
                                        };

                                        // Execute the merge procedure.
                                        await dbStgShipmentLog2Repo.ExecuteAsync(sql, null, cancellationTokenSource);
                                    }

                                    // DbOServiceTracking:
                                    if (dbStgShipmentLog2sToPersist.Count != 0)
                                    {
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.ShipmentLogProcessor2, shipmentLogGeotabObjectFeeder.LastFeedRetrievalTimeUtc, shipmentLogGeotabObjectFeeder.LastFeedVersion);
                                    }
                                    else
                                    {
                                        // No ShipmentLogs were returned, but the OServiceTracking record for this service still needs to be updated to show that the service is operating.
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.ShipmentLogProcessor2, DateTime.UtcNow);
                                    }

                                    // Commit transactions:
                                    await adapterUOW.CommitAsync();
                                }
                                catch (Exception ex)
                                {
                                    feedVersionRollbackRequired = true;
                                    await adapterUOW.RollBackAsync();
                                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                                    throw;
                                }
                            }
                        }, new Context());

                        // Clear FeedResultData.
                        shipmentLogGeotabObjectFeeder.FeedResultData.Clear();
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
                catch (MyGeotabConnectionException myGeotabConnectionException)
                {
                    exceptionHelper.LogException(myGeotabConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    stateMachine.HandleException(myGeotabConnectionException, NLogLogLevelName.Error);
                }
                catch (Exception ex)
                {
                    var exceptionToAnalyze = ex.InnerException ?? ex;
                    if (ForeignKeyExceptionHelper.IsForeignKeyViolationException(exceptionToAnalyze))
                    {
                        var violatedConstraint = ForeignKeyExceptionHelper.GetConstraintNameFromException(exceptionToAnalyze);
                        if (!string.IsNullOrEmpty(violatedConstraint) && shipmentLogForeignKeyServiceDependencyMap.TryGetDependency(violatedConstraint, out AdapterService prerequisiteService))
                        {
                            await awaiter.WaitForPrerequisiteServiceToProcessEntitiesAsync(prerequisiteService, stoppingToken);
                            // After waiting, this iteration's attempt is considered "handled" by waiting. The next iteration will be the actual retry of the operation.
                            logger.Warn($"Iteration handling for FK violation on '{violatedConstraint}' complete (waited for {prerequisiteService}). Ready for next iteration.");
                        }
                        else
                        {
                            // FK violation occurred, but constraint name not found OR not included in the dependency map.
                            string reason = string.IsNullOrEmpty(violatedConstraint) ? "constraint name not extractable" : $"constraint '{violatedConstraint}' not included in shipmentLogForeignKeyServiceDependencyMap";
                            exceptionHelper.LogException(ex, NLogLogLevelName.Fatal, $"{DefaultErrorMessagePrefix} Unhandled FK violation: {reason}.");
                            stateMachine.HandleException(ex, NLogLogLevelName.Fatal);
                        }
                    }
                    else
                    {
                        // Not an FK violation. Treat as fatal.
                        exceptionHelper.LogException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
                        stateMachine.HandleException(ex, NLogLogLevelName.Fatal);
                    }
                }

                // If the feed is up-to-date, add a delay equivalent to the configured interval.
                if (shipmentLogGeotabObjectFeeder.FeedCurrent == true)
                {
                    await awaiter.WaitForConfiguredIntervalAsync(delayTimeSpan, DelayIntervalType.Feed, stoppingToken);
                }
            }
        }

        /// <summary>
        /// Starts the current <see cref="ShipmentLogProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.ShipmentLogProcessor2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.ShipmentLogProcessor2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
            stateMachine.RegisterService(nameof(ShipmentLogProcessor2), adapterConfiguration.EnableShipmentLogFeed);

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.EnableShipmentLogFeed == true)
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
        /// Stops the current <see cref="ShipmentLogProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            stateMachine.RegisterService(nameof(ShipmentLogProcessor2), false);

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
