using Geotab.Checkmate.ObjectModel;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Caches;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.GeotabObjectMappers;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.MyGeotabAPI;
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
    /// A <see cref="BackgroundService"/> that extracts <see cref="DutyStatusAvailability"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// NOTE: The Geotab API's GetFeed method does not support the <see cref="Geotab.Checkmate.ObjectModel.DutyStatusAvailability"/> entity type and the results for DutyStatusAvailability Get requests are calculated dynamically, resulting in longer response times than are typical for pre-calculated data. It is also necessary to retrieve DutyStatusAvailability on a per-driver basis using batches of Get requests wrapped in <see cref="Geotab.Checkmate.API.MultiCallAsync(object[])"/> requests (in order to support larger fleets where the number of Get<DutyStatusAvailability> requests required to cover all drivers could not be made in a single MultiCall request). The result of the combination of these factors is that it can take some time for DutyStatusAvailability to be retrieved for all drivers in a fleet.
    /// </summary>
    class DutyStatusAvailabilityProcessor2 : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        const string HosRuleSetNoneValue = "None";
        const int maxBatchSize = 100;
        DateTime lastDutyStatusAvailabilityDataRetrievalStartTimeUtc;

        // Polly-related items:
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<DutyStatusAvailabilityProcessor2> awaiter;
        readonly IBaseRepository<DbStgDutyStatusAvailability2> dbStgDutyStatusAvailability2Repo;
        readonly IDateTimeHelper dateTimeHelper;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericGenericDbObjectCache<DbUser2, AdapterGenericDbObjectCache<DbUser2>> dbUser2ObjectCache;
        readonly IGenericEntityPersister<DbStgDutyStatusAvailability2> dbStgDutyStatusAvailability2EntityPersister;
        readonly IGeotabIdConverter geotabIdConverter;
        readonly IGeotabDutyStatusAvailabilityDbStgDutyStatusAvailability2ObjectMapper geotabDutyStatusAvailabilityDbStgDutyStatusAvailability2ObjectMapper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IForeignKeyServiceDependencyMap dutyStatusAvailabilityForeignKeyServiceDependencyMap;
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DutyStatusAvailabilityProcessor2"/> class.
        /// </summary>
        public DutyStatusAvailabilityProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<DutyStatusAvailabilityProcessor2> awaiter, IDateTimeHelper dateTimeHelper, IExceptionHelper exceptionHelper, IGenericGenericDbObjectCache<DbUser2, AdapterGenericDbObjectCache<DbUser2>> dbUser2ObjectCache, IGenericEntityPersister<DbStgDutyStatusAvailability2> dbStgDutyStatusAvailability2EntityPersister, IGeotabIdConverter geotabIdConverter, IGeotabDutyStatusAvailabilityDbStgDutyStatusAvailability2ObjectMapper geotabDutyStatusAvailabilityDbStgDutyStatusAvailability2ObjectMapper, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.dateTimeHelper = dateTimeHelper;
            this.exceptionHelper = exceptionHelper;
            this.dbUser2ObjectCache = dbUser2ObjectCache;
            this.dbStgDutyStatusAvailability2EntityPersister = dbStgDutyStatusAvailability2EntityPersister;
            this.geotabIdConverter = geotabIdConverter;
            this.geotabDutyStatusAvailabilityDbStgDutyStatusAvailability2ObjectMapper = geotabDutyStatusAvailabilityDbStgDutyStatusAvailability2ObjectMapper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;

            dbStgDutyStatusAvailability2Repo = new BaseRepository<DbStgDutyStatusAvailability2>(adapterContext);

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            // Setup the foreign key service dependency map.
            dutyStatusAvailabilityForeignKeyServiceDependencyMap = new ForeignKeyServiceDependencyMap(
                [
                    //new ForeignKeyServiceDependency("FK_DutyStatusAvailabilities2_Users2", AdapterService.UserProcessor2)
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
            const string MergeFunctionSQL_Postgres = @"SELECT public.""spMerge_stg_DutyStatusAvailabilities2""();";
            const string MergeProcedureSQL_SQLServer = @"EXEC [dbo].[spMerge_stg_DutyStatusAvailabilities2];";
            const string TruncateStagingTableSQL_Postgres = @"TRUNCATE TABLE public.""stg_DutyStatusAvailabilities2"";";
            const string TruncateStagingTableSQL_SQLServer = @"TRUNCATE TABLE [dbo].[stg_DutyStatusAvailabilities2];";

            MethodBase methodBase = MethodBase.GetCurrentMethod();
            var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.DutyStatusAvailabilityFeedIntervalSeconds);

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
                await awaiter.WaitForConnectivityRestorationIfNeededAsync(stoppingToken);

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    // Only proceed with data retrieval if the DutyStatusAvailabilityFeedIntervalSeconds has elapsed since data retrieval was last initiated.
                    if (dateTimeHelper.TimeIntervalHasElapsed(lastDutyStatusAvailabilityDataRetrievalStartTimeUtc, DateTimeIntervalType.Seconds, adapterConfiguration.DutyStatusAvailabilityFeedIntervalSeconds) == true)
                    {
                        lastDutyStatusAvailabilityDataRetrievalStartTimeUtc = DateTime.UtcNow;

                        using (var cancellationTokenSource = new CancellationTokenSource())
                        {
                            try
                            {
                                await InitializeOrUpdateCachesAsync(cancellationTokenSource);

                                // Get list of active users that are drivers who have accessed the MyGeotab system within the last 30 days and have HosRuleSets assigned.
                                var dutyStatusAvailabilityFeedLastAccessDateCutoffDays = TimeSpan.FromDays(adapterConfiguration.DutyStatusAvailabilityFeedLastAccessDateCutoffDays);
                                DateTime cutoffLastAccessedTime = DateTime.UtcNow.Subtract(dutyStatusAvailabilityFeedLastAccessDateCutoffDays);
                                var allDbUsers = await dbUser2ObjectCache.GetObjectsAsync();
                                var driverDbUsers = allDbUsers.Where(dbUser => dbUser.IsDriver == true && dbUser.ActiveTo >= DateTime.UtcNow && dbUser.LastAccessDate >= cutoffLastAccessedTime && dbUser.HosRuleSet != HosRuleSetNoneValue).ToList();

                                int currentBatchSize = 0;
                                int driverDbUserCount = driverDbUsers.Count;
                                var calls = new List<object>();
                                for (int driverDbUserListIndex = 0; driverDbUserListIndex < driverDbUserCount + 1; driverDbUserListIndex++)
                                {
                                    if (currentBatchSize == maxBatchSize || driverDbUserListIndex == driverDbUserCount)
                                    {
                                        DateTime recordChangedTimestampUtc = DateTime.UtcNow;
                                        var dbStgDutyStatusAvailability2sToPersist = new List<DbStgDutyStatusAvailability2>();

                                        List<object> results;
                                        try
                                        {
                                            // Execute MultiCall.
                                            results = await myGeotabAPIHelper.MyGeotabAPI.MultiCallAsync(calls.ToArray());
                                        }
                                        catch (Exception exception)
                                        {
                                            // If the exception is related to connectivity, wrap it in a MyGeotabConnectionException. Otherwise, just pass it along.
                                            if (exceptionHelper.ExceptionIsRelatedToMyGeotabConnectivityLoss(exception))
                                            {
                                                throw new MyGeotabConnectionException("An exception occurred while attempting to get data from the Geotab API via MultiCallAsync.", exception);
                                            }
                                            else
                                            {
                                                throw;
                                            }
                                        }

                                        // Iterate through the returned DutyStatusAvailability entities.
                                        foreach (var result in results)
                                        {
                                            if (result is List<DutyStatusAvailability> resultDutyStatusAvailabilityList && resultDutyStatusAvailabilityList.Count != 0) 
                                            {
                                                // Map the DutyStatusAvailability object to a DbStgDutyStatusAvailability2 entity and add to enties to persist.
                                                var dutyStatusAvailability = resultDutyStatusAvailabilityList[0];
                                                long? dutyStatusAvailabilityDriverId = null;
                                                if (dutyStatusAvailability.Driver != null && dutyStatusAvailability.Driver.Id != null && dutyStatusAvailability.Driver.GetType() != typeof(UnknownDriver))
                                                {
                                                    dutyStatusAvailabilityDriverId = geotabIdConverter.ToLong(dutyStatusAvailability.Driver.Id);

                                                    var dbStgDutyStatusAvailability2 = geotabDutyStatusAvailabilityDbStgDutyStatusAvailability2ObjectMapper.CreateEntity(dutyStatusAvailability, (long)dutyStatusAvailabilityDriverId);
                                                    dbStgDutyStatusAvailability2sToPersist.Add(dbStgDutyStatusAvailability2);
                                                }
                                                else
                                                {
                                                    logger.Info($"Could not process a {nameof(DutyStatusAvailability)} entity with Id '{dutyStatusAvailability.Id}' because its {nameof(DutyStatusAvailability.Driver)} property is null or has a null {nameof(Driver.Id)}.");
                                                    continue;
                                                }
                                            }
                                        }

                                        stoppingToken.ThrowIfCancellationRequested();

                                        // Persist changes to database. Step 1: Persist the DbStgDutyStatusAvailability2 entities.
                                        if (dbStgDutyStatusAvailability2sToPersist.Count != 0)
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
                                                        await dbStgDutyStatusAvailability2Repo.ExecuteAsync(sql, null, cancellationTokenSource, true, adapterContext);

                                                        // DbStgDutyStatusAvailability2:
                                                        await dbStgDutyStatusAvailability2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbStgDutyStatusAvailability2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

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

                                        // Persist changes to database. Step 2: Merge the DbStgDutyStatusAvailability2 entities into the DbDutyStatusAvailability2 table and update the DbOServiceTracking table.
                                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                                        {
                                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                            {
                                                try
                                                {
                                                    if (dbStgDutyStatusAvailability2sToPersist.Count != 0)
                                                    {
                                                        // Build the SQL statement to execute the merge procedure.
                                                        var sql = adapterContext.ProviderType switch
                                                        {
                                                            ConnectionInfo.DataAccessProviderType.PostgreSQL => MergeFunctionSQL_Postgres,
                                                            ConnectionInfo.DataAccessProviderType.SQLServer => MergeProcedureSQL_SQLServer,
                                                            _ => throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.")
                                                        };

                                                        // Execute the merge procedure.
                                                        await dbStgDutyStatusAvailability2Repo.ExecuteAsync(sql, null, cancellationTokenSource);
                                                    }

                                                    // DbOServiceTracking:
                                                    await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DutyStatusAvailabilityProcessor2, DateTime.UtcNow);

                                                    // Commit transactions:
                                                    await adapterUOW.CommitAsync();
                                                }
                                                catch (Exception ex)
                                                {
                                                    await adapterUOW.RollBackAsync();
                                                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                                                    throw;
                                                }
                                            }
                                        }, new Context());

                                        // Clear calls list and reset counter.
                                        calls = new List<object>();
                                        currentBatchSize = 0;
                                    }
                                    if (driverDbUserListIndex == driverDbUserCount)
                                    {
                                        // All drivers have been processed.
                                        break;
                                    }
                                    // Generate Get<DutyStatusAvailability> call for current driver and add to list.
                                    var driverDbUserId = Id.Create(driverDbUsers[driverDbUserListIndex].GeotabId);
                                    var userSearch = new UserSearch
                                    {
                                        Id = driverDbUserId
                                    };
                                    calls.Add(new object[] { "Get", typeof(DutyStatusAvailability), new { search = new DutyStatusAvailabilitySearch { UserSearch = userSearch } }, typeof(List<DutyStatusAvailability>) });

                                    currentBatchSize++;
                                }
                            }
                            catch (TaskCanceledException taskCanceledException)
                            {
                                string errorMessage = $"Task was cancelled. TaskCanceledException: \nMESSAGE [{taskCanceledException.Message}]; \nSOURCE [{taskCanceledException.Source}]; \nSTACK TRACE [{taskCanceledException.StackTrace}]";
                                logger.Warn(errorMessage);
                            }
                            catch (Exception)
                            {
                                cancellationTokenSource.Cancel();
                                throw;
                            }
                        }
                    }
                    else
                    {
                        logger.Debug($"DutyStatusAvailability data retrieval not initiated; {adapterConfiguration.DutyStatusAvailabilityFeedIntervalSeconds} seconds have not passed since DutyStatusAvailability data retrieval was last initiated.");
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
                        if (!string.IsNullOrEmpty(violatedConstraint) && dutyStatusAvailabilityForeignKeyServiceDependencyMap.TryGetDependency(violatedConstraint, out AdapterService prerequisiteService))
                        {
                            await awaiter.WaitForPrerequisiteServiceToProcessEntitiesAsync(prerequisiteService, stoppingToken);
                            // After waiting, this iteration's attempt is considered "handled" by waiting. The next iteration will be the actual retry of the operation.
                            logger.Debug($"Iteration handling for FK violation on '{violatedConstraint}' complete (waited for {prerequisiteService}). Ready for next iteration.");
                        }
                        else
                        {
                            // FK violation occurred, but constraint name not found OR not included in the dependency map.
                            string reason = string.IsNullOrEmpty(violatedConstraint) ? "constraint name not extractable" : $"constraint '{violatedConstraint}' not included in dutyStatusAvailabilityForeignKeyServiceDependencyMap";
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

                // Add a delay equivalent to the configured interval.
                await awaiter.WaitForConfiguredIntervalAsync(delayTimeSpan, DelayIntervalType.Feed, stoppingToken);
            }
        }

        /// <summary>
        /// Initializes and/or updates any caches used by this class.
        /// </summary>
        /// <returns></returns>
        async Task InitializeOrUpdateCachesAsync(CancellationTokenSource cancellationTokenSource)
        {
            var dbObjectCacheInitializationAndUpdateTasks = new List<Task>();

            // Update the in-memory caches of database objects that correspond with Geotab objects.
            if (dbUser2ObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbUser2ObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }

            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);
        }

        /// <summary>
        /// Starts the current <see cref="DutyStatusAvailabilityProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.DutyStatusAvailabilityProcessor2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DutyStatusAvailabilityProcessor2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
                stateMachine.RegisterService(nameof(DutyStatusAvailabilityProcessor2), adapterConfiguration.EnableDutyStatusAvailabilityFeed);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableDutyStatusAvailabilityFeed == true)
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
        /// Stops the current <see cref="DutyStatusAvailabilityProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(DutyStatusAvailabilityProcessor2), false);
            }

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
