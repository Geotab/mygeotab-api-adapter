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
    /// A <see cref="BackgroundService"/> that extracts <see cref="User"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class UserProcessor2 : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<UserProcessor2> awaiter;
        readonly IBaseRepository<DbStgUser2> dbStgUser2Repo;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericGenericDbObjectCache<DbUser2, AdapterGenericDbObjectCache<DbUser2>> dbUser2ObjectCache;
        readonly IGenericEntityPersister<DbStgUser2> dbStgUser2EntityPersister;
        readonly IGenericGeotabObjectCacher<User> userGeotabObjectCacher;
        readonly IGeotabIdConverter geotabIdConverter;
        readonly IGeotabUserDbStgUser2ObjectMapper geotabUserDbStgUser2ObjectMapper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserProcessor2"/> class.
        /// </summary>
        public UserProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<UserProcessor2> awaiter, IExceptionHelper exceptionHelper, IGenericGenericDbObjectCache<DbUser2, AdapterGenericDbObjectCache<DbUser2>> dbUser2ObjectCache, IGenericEntityPersister<DbStgUser2> dbStgUser2EntityPersister, IGeotabUserDbStgUser2ObjectMapper geotabUserDbStgUser2ObjectMapper, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericGeotabObjectCacher<User> userGeotabObjectCacher, IGeotabIdConverter geotabIdConverter, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.exceptionHelper = exceptionHelper;
            this.dbUser2ObjectCache = dbUser2ObjectCache;
            this.dbStgUser2EntityPersister = dbStgUser2EntityPersister;
            this.geotabUserDbStgUser2ObjectMapper = geotabUserDbStgUser2ObjectMapper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.userGeotabObjectCacher = userGeotabObjectCacher;
            this.geotabIdConverter = geotabIdConverter;

            dbStgUser2Repo = new BaseRepository<DbStgUser2>(adapterContext);

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

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
            const string MergeFunctionSQL_Postgres = @"SELECT public.""spMerge_stg_Users2""(@SetEntityStatusDeletedForMissingUsers::boolean);";
            const string MergeProcedureSQL_SQLServer = @"EXEC [dbo].[spMerge_stg_Users2] @SetEntityStatusDeletedForMissingUsers = @SetEntityStatusDeletedForMissingUsers;";
            const string TruncateStagingTableSQL_Postgres = @"TRUNCATE TABLE public.""stg_Users2"";";
            const string TruncateStagingTableSQL_SQLServer = @"TRUNCATE TABLE [dbo].[stg_Users2];";

            MethodBase methodBase = MethodBase.GetCurrentMethod();
            var delayTimeSpan = TimeSpan.FromMinutes(adapterConfiguration.UserCacheUpdateIntervalMinutes);

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

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        await InitializeOrUpdateCachesAsync(cancellationTokenSource);

                        var dbStgUser2sToPersist = new List<DbStgUser2>();

                        // Only propagate the cache to database if the cache has been updated since the last time it was propagated to database.
                        if (userGeotabObjectCacher.LastUpdatedTimeUTC > userGeotabObjectCacher.LastPropagatedToDatabaseTimeUtc)
                        {
                            DateTime recordChangedTimestampUtc = DateTime.UtcNow;

                            // Iterate through the in-memory cache of Geotab User objects that were added or updated in the last cache update, mapping them to corresponding DbStgUser2 entities and adding them to the list of DbStgUser2 objects to persist. Note that deduplication logic is contained in the database.
                            foreach (var user in userGeotabObjectCacher.GeotabObjectsChangedInLastUpdate)
                            {
                                var newdbStgUser2 = geotabUserDbStgUser2ObjectMapper.CreateEntity(user);
                                dbStgUser2sToPersist.Add(newdbStgUser2);
                            }

                            stoppingToken.ThrowIfCancellationRequested();
                        }
                        else
                        {
                            logger.Debug($"User cache in database is up-to-date.");
                        }

                        // Persist changes to database. Step 1: Persist the DbStgUser2 entities.
                        if (dbStgUser2sToPersist.Count != 0)
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
                                        await dbStgUser2Repo.ExecuteAsync(sql, null, cancellationTokenSource, true, adapterContext);

                                        // DbStgUser2:
                                        await dbStgUser2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbStgUser2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

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

                        // Persist changes to database. Step 2: Merge the DbStgUser2 entities into the DbUser2 table and update the DbOServiceTracking table.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    if (dbStgUser2sToPersist.Count != 0)
                                    {
                                        // Define parameters for the merge procedure.
                                        var setEntityStatusDeletedForMissingUsers = false;
                                        if (userGeotabObjectCacher.LastCacheOperationType == CacheOperationType.Refresh)
                                        {
                                            setEntityStatusDeletedForMissingUsers = true;
                                        }
                                        var parameters = new[]
                                        {
                                        new { SetEntityStatusDeletedForMissingUsers = setEntityStatusDeletedForMissingUsers }
                                    };

                                        // Build the SQL statement to execute the merge procedure.
                                        var sql = adapterContext.ProviderType switch
                                        {
                                            ConnectionInfo.DataAccessProviderType.PostgreSQL => MergeFunctionSQL_Postgres,
                                            ConnectionInfo.DataAccessProviderType.SQLServer => MergeProcedureSQL_SQLServer,
                                            _ => throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.")
                                        };

                                        // Execute the merge procedure.
                                        await dbStgUser2Repo.ExecuteAsync(sql, parameters, cancellationTokenSource);
                                    }

                                    // DbOServiceTracking:
                                    await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.UserProcessor2, DateTime.UtcNow);

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

                        // If there were any changes, force the DbUser2 cache to be updated so that the changes are immediately available to other consumers.
                        if (dbStgUser2sToPersist.Count != 0)
                        {
                            await dbUser2ObjectCache.UpdateAsync(true);
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
                catch (MyGeotabConnectionException myGeotabConnectionException)
                {
                    exceptionHelper.LogException(myGeotabConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    stateMachine.HandleException(myGeotabConnectionException, NLogLogLevelName.Error);

                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
                    stateMachine.HandleException(ex, NLogLogLevelName.Fatal);
                }

                // Add a delay equivalent to the configured interval.
                await awaiter.WaitForConfiguredIntervalAsync(delayTimeSpan, DelayIntervalType.Update, stoppingToken);
            }
        }

        /// <summary>
        /// Initializes and/or updates any caches used by this class.
        /// </summary>
        /// <returns></returns>
        async Task InitializeOrUpdateCachesAsync(CancellationTokenSource cancellationTokenSource)
        {
            var dbObjectCacheInitializationAndUpdateTasks = new List<Task>();

            // Update the in-memory cache of Geotab objects obtained via API from the MyGeotab database.
            if (userGeotabObjectCacher.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(userGeotabObjectCacher.InitializeAsync(cancellationTokenSource, adapterConfiguration.UserCacheIntervalDailyReferenceStartTimeUTC, adapterConfiguration.UserCacheUpdateIntervalMinutes, adapterConfiguration.UserCacheRefreshIntervalMinutes, myGeotabAPIHelper.GetFeedResultLimitUser, true));
            }
            else
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(userGeotabObjectCacher.UpdateGeotabObjectCacheAsync(cancellationTokenSource));
            }

            // Update the in-memory cache of database objects that correspond with Geotab objects.
            if (dbUser2ObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbUser2ObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }

            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);
        }

        /// <summary>
        /// Starts the current <see cref="UserProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.UserProcessor2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.UserProcessor2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
                stateMachine.RegisterService(nameof(UserProcessor2), adapterConfiguration.EnableUserCache);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableUserCache == true)
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
        /// Stops the current <see cref="UserProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(UserProcessor2), false);
            }

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
