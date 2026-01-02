using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Fuel;
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
    /// A <see cref="BackgroundService"/> that extracts <see cref="FuelAndEnergyUsed"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class FuelAndEnergyUsedProcessor2 : BackgroundService
    {
        bool feedVersionRollbackRequired = false;

        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<FuelAndEnergyUsedProcessor2> awaiter;
        readonly IBaseRepository<DbStgFuelAndEnergyUsed2> dbStgFuelAndEnergyUsed2Repo;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbStgFuelAndEnergyUsed2> dbStgFuelAndEnergyUsed2EntityPersister;
        readonly IGenericGeotabObjectFeeder<FuelAndEnergyUsed> fuelAndEnergyUsedGeotabObjectFeeder;
        readonly IGeotabDeviceFilterer geotabDeviceFilterer;
        readonly IGeotabIdConverter geotabIdConverter;
        readonly IGeotabFuelAndEnergyUsedDbStgFuelAndEnergyUsed2ObjectMapper geotabFuelAndEnergyUsedDbStgFuelAndEnergyUsed2ObjectMapper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IForeignKeyServiceDependencyMap fuelAndEnergyUsedForeignKeyServiceDependencyMap;
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuelAndEnergyUsedProcessor2"/> class.
        /// </summary>
        public FuelAndEnergyUsedProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<FuelAndEnergyUsedProcessor2> awaiter, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbStgFuelAndEnergyUsed2> dbStgFuelAndEnergyUsed2EntityPersister, IGenericGeotabObjectFeeder<FuelAndEnergyUsed> fuelAndEnergyUsedGeotabObjectFeeder, IGeotabDeviceFilterer geotabDeviceFilterer, IGeotabIdConverter geotabIdConverter, IGeotabFuelAndEnergyUsedDbStgFuelAndEnergyUsed2ObjectMapper geotabFuelAndEnergyUsedDbStgFuelAndEnergyUsed2ObjectMapper, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.exceptionHelper = exceptionHelper;
            this.dbStgFuelAndEnergyUsed2EntityPersister = dbStgFuelAndEnergyUsed2EntityPersister;
            this.fuelAndEnergyUsedGeotabObjectFeeder = fuelAndEnergyUsedGeotabObjectFeeder;
            this.geotabDeviceFilterer = geotabDeviceFilterer;
            this.geotabIdConverter = geotabIdConverter;
            this.geotabFuelAndEnergyUsedDbStgFuelAndEnergyUsed2ObjectMapper = geotabFuelAndEnergyUsedDbStgFuelAndEnergyUsed2ObjectMapper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;

            dbStgFuelAndEnergyUsed2Repo = new BaseRepository<DbStgFuelAndEnergyUsed2>(adapterContext);

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            // Setup the foreign key service dependency map.
            fuelAndEnergyUsedForeignKeyServiceDependencyMap = new ForeignKeyServiceDependencyMap(
                [
                    new ForeignKeyServiceDependency("FK_FuelAndEnergyUsed2_Devices2", AdapterService.DeviceProcessor2)
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
            const string MergeFunctionSQL_Postgres = @"SELECT public.""spMerge_stg_FuelAndEnergyUsed2""();";
            const string MergeProcedureSQL_SQLServer = @"EXEC [dbo].[spMerge_stg_FuelAndEnergyUsed2];";
            const string TruncateStagingTableSQL_Postgres = @"TRUNCATE TABLE public.""stg_FuelAndEnergyUsed2"";";
            const string TruncateStagingTableSQL_SQLServer = @"TRUNCATE TABLE [dbo].[stg_FuelAndEnergyUsed2];";

            MethodBase methodBase = MethodBase.GetCurrentMethod();
            var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.FuelAndEnergyUsedFeedIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait if necessary.
                var prerequisiteServices = new List<AdapterService>
                {
                    AdapterService.DatabaseMaintenanceService2,
                    AdapterService.DeviceProcessor2
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
                        var dbOServiceTracking = await serviceTracker.GetFuelAndEnergyUsedService2InfoAsync();

                        // Initialize the Geotab object feeder.
                        if (fuelAndEnergyUsedGeotabObjectFeeder.IsInitialized == false)
                        {
                            await fuelAndEnergyUsedGeotabObjectFeeder.InitializeAsync(cancellationTokenSource, adapterConfiguration.FuelAndEnergyUsedFeedIntervalSeconds, myGeotabAPIHelper.GetFeedResultLimitDefault, (long?)dbOServiceTracking.LastProcessedFeedVersion);
                        }

                        // If this is the first iteration after a connectivity disruption, roll-back the LastFeedVersion of the GeotabObjectFeeder to the last processed feed version that was committed to the database and set the LastFeedRetrievalTimeUtc to DateTime.MinValue to start processing without further delay.
                        if (feedVersionRollbackRequired == true)
                        {
                            fuelAndEnergyUsedGeotabObjectFeeder.Rollback(dbOServiceTracking.LastProcessedFeedVersion);
                            feedVersionRollbackRequired = false;
                        }

                        // Get a batch of FuelAndEnergyUsed objects from Geotab.
                        await fuelAndEnergyUsedGeotabObjectFeeder.GetFeedDataBatchAsync(cancellationTokenSource);
                        stoppingToken.ThrowIfCancellationRequested();

                        // Process any returned FuelAndEnergyUseds.
                        var fuelAndEnergyUseds = fuelAndEnergyUsedGeotabObjectFeeder.GetFeedResultDataValuesList();
                        var dbStgFuelAndEnergyUsed2sToPersist = new List<DbStgFuelAndEnergyUsed2>();
                        if (fuelAndEnergyUseds.Count != 0)
                        {
                            // Apply tracked device filter and/or tracked diagnostic filter (if configured in appsettings.json).
                            var filteredFuelAndEnergyUseds = await geotabDeviceFilterer.ApplyDeviceFilterAsync(cancellationTokenSource, fuelAndEnergyUseds);

                            // Map the FuelAndEnergyUsed objects to DbFuelAndEnergyUsed2 entities.
                            foreach (var fuelAndEnergyUsed in filteredFuelAndEnergyUseds)
                            {
                                long? fuelAndEnergyUsedDeviceId = null;
                                if (fuelAndEnergyUsed.Device != null)
                                {
                                    if (fuelAndEnergyUsed.Device.GetType() == typeof(NoDevice))
                                    {
                                        fuelAndEnergyUsedDeviceId = AdapterDbSentinelIdsForMYGKnownIds.NoDeviceId;
                                    }
                                    else if (fuelAndEnergyUsed.Device.Id != null)
                                    {
                                        fuelAndEnergyUsedDeviceId = geotabIdConverter.ToLong(fuelAndEnergyUsed.Device.Id);
                                    }
                                }
                                if (fuelAndEnergyUsedDeviceId == null)
                                {
                                    logger.Warn($"Could not process {nameof(FuelAndEnergyUsed)} with GeotabId {fuelAndEnergyUsed.Id}' because its {nameof(FuelAndEnergyUsed.Device)} is null.");
                                    continue;
                                }

                                var dbStgFuelAndEnergyUsed2 = geotabFuelAndEnergyUsedDbStgFuelAndEnergyUsed2ObjectMapper.CreateEntity(fuelAndEnergyUsed, (long)fuelAndEnergyUsedDeviceId);
                                dbStgFuelAndEnergyUsed2sToPersist.Add(dbStgFuelAndEnergyUsed2);
                            }
                        }

                        stoppingToken.ThrowIfCancellationRequested();

                        // Persist changes to database. Step 1: Persist the DbStgFuelAndEnergyUsed2 entities.
                        if (dbStgFuelAndEnergyUsed2sToPersist.Count != 0)
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
                                        await dbStgFuelAndEnergyUsed2Repo.ExecuteAsync(sql, null, cancellationTokenSource, true, adapterContext);

                                        // DbStgFuelAndEnergyUsed2:
                                        await dbStgFuelAndEnergyUsed2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbStgFuelAndEnergyUsed2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

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

                        // Persist changes to database. Step 2: Merge the DbStgFuelAndEnergyUsed2 entities into the DbFuelAndEnergyUsed2 table and update the DbOServiceTracking table.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    if (dbStgFuelAndEnergyUsed2sToPersist.Count != 0)
                                    {
                                        // Build the SQL statement to execute the merge procedure.
                                        var sql = adapterContext.ProviderType switch
                                        {
                                            ConnectionInfo.DataAccessProviderType.PostgreSQL => MergeFunctionSQL_Postgres,
                                            ConnectionInfo.DataAccessProviderType.SQLServer => MergeProcedureSQL_SQLServer,
                                            _ => throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.")
                                        };

                                        // Execute the merge procedure.
                                        await dbStgFuelAndEnergyUsed2Repo.ExecuteAsync(sql, null, cancellationTokenSource);
                                    }

                                    // DbOServiceTracking:
                                    if (dbStgFuelAndEnergyUsed2sToPersist.Count != 0)
                                    {
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.FuelAndEnergyUsedProcessor2, fuelAndEnergyUsedGeotabObjectFeeder.LastFeedRetrievalTimeUtc, fuelAndEnergyUsedGeotabObjectFeeder.LastFeedVersion);
                                    }
                                    else
                                    {
                                        // No FuelAndEnergyUseds were returned, but the OServiceTracking record for this service still needs to be updated to show that the service is operating.
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.FuelAndEnergyUsedProcessor2, DateTime.UtcNow);
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
                        fuelAndEnergyUsedGeotabObjectFeeder.FeedResultData.Clear();
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
                        if (!string.IsNullOrEmpty(violatedConstraint) && fuelAndEnergyUsedForeignKeyServiceDependencyMap.TryGetDependency(violatedConstraint, out AdapterService prerequisiteService))
                        {
                            await awaiter.WaitForPrerequisiteServiceToProcessEntitiesAsync(prerequisiteService, stoppingToken);
                            // After waiting, this iteration's attempt is considered "handled" by waiting. The next iteration will be the actual retry of the operation.
                            logger.Debug($"Iteration handling for FK violation on '{violatedConstraint}' complete (waited for {prerequisiteService}). Ready for next iteration.");
                        }
                        else
                        {
                            // FK violation occurred, but constraint name not found OR not included in the dependency map.
                            string reason = string.IsNullOrEmpty(violatedConstraint) ? "constraint name not extractable" : $"constraint '{violatedConstraint}' not included in fuelAndEnergyUsedForeignKeyServiceDependencyMap";
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
                if (fuelAndEnergyUsedGeotabObjectFeeder.FeedCurrent == true)
                {
                    await awaiter.WaitForConfiguredIntervalAsync(delayTimeSpan, DelayIntervalType.Feed, stoppingToken);
                }
            }
        }

        /// <summary>
        /// Starts the current <see cref="FuelAndEnergyUsedProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.FuelAndEnergyUsedProcessor2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.FuelAndEnergyUsedProcessor2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
            stateMachine.RegisterService(nameof(FuelAndEnergyUsedProcessor2), adapterConfiguration.EnableFuelAndEnergyUsedFeed);

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.EnableFuelAndEnergyUsedFeed == true)
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
        /// Stops the current <see cref="FuelAndEnergyUsedProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            stateMachine.RegisterService(nameof(FuelAndEnergyUsedProcessor2), false);

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
