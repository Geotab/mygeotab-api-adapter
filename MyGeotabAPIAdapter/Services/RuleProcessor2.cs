using Geotab.Checkmate.ObjectModel.Exceptions;
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
    /// A <see cref="BackgroundService"/> that extracts <see cref="Rule"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class RuleProcessor2 : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<RuleProcessor2> awaiter;
        readonly IBaseRepository<DbStgRule2> dbStgRule2Repo;
        readonly IExceptionHelper exceptionHelper;        
        readonly IGenericGenericDbObjectCache<DbRule2, AdapterGenericDbObjectCache<DbRule2>> dbRule2ObjectCache;
        readonly IGenericEntityPersister<DbStgRule2> dbStgRule2EntityPersister;
        readonly IGenericGeotabObjectCacher<Rule> ruleGeotabObjectCacher;
        readonly IGeotabRuleDbStgRule2ObjectMapper geotabRuleDbStgRule2ObjectMapper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="RuleProcessor2"/> class.
        /// </summary>
        public RuleProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<RuleProcessor2> awaiter, IExceptionHelper exceptionHelper, IGenericGenericDbObjectCache<DbRule2, AdapterGenericDbObjectCache<DbRule2>> dbRule2ObjectCache, IGenericEntityPersister<DbStgRule2> dbStgRule2EntityPersister, IGeotabRuleDbStgRule2ObjectMapper geotabRuleDbStgRule2ObjectMapper, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericGeotabObjectCacher<Rule> ruleGeotabObjectCacher, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.exceptionHelper = exceptionHelper;
            this.dbRule2ObjectCache = dbRule2ObjectCache;
            this.dbStgRule2EntityPersister = dbStgRule2EntityPersister;
            this.geotabRuleDbStgRule2ObjectMapper = geotabRuleDbStgRule2ObjectMapper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.ruleGeotabObjectCacher = ruleGeotabObjectCacher;

            dbStgRule2Repo = new BaseRepository<DbStgRule2>(adapterContext);

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
            const string MergeFunctionSQL_Postgres = @"SELECT public.""spMerge_stg_Rules2""(@SetEntityStatusDeletedForMissingRules::boolean);";
            const string MergeProcedureSQL_SQLServer = @"EXEC [dbo].[spMerge_stg_Rules2] @SetEntityStatusDeletedForMissingRules = @SetEntityStatusDeletedForMissingRules;";
            const string TruncateStagingTableSQL_Postgres = @"TRUNCATE TABLE public.""stg_Rules2"";";
            const string TruncateStagingTableSQL_SQLServer = @"TRUNCATE TABLE [dbo].[stg_Rules2];";

            MethodBase methodBase = MethodBase.GetCurrentMethod();
            var delayTimeSpan = TimeSpan.FromMinutes(adapterConfiguration.RuleCacheUpdateIntervalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait if necessary.
                var prerequisiteServices = new List<AdapterService> { };
                await awaiter.WaitForPrerequisiteServicesIfNeededAsync(prerequisiteServices, stoppingToken);
                await awaiter.WaitForDatabaseMaintenanceCompletionIfNeededAsync(stoppingToken);
                await awaiter.WaitForConnectivityRestorationIfNeededAsync(stoppingToken);

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        await InitializeOrUpdateCachesAsync(cancellationTokenSource);

                        var dbStgRule2sToPersist = new List<DbStgRule2>();
                        
                        // Only propagate the cache to database if the cache has been updated since the last time it was propagated to database.
                        if (ruleGeotabObjectCacher.LastUpdatedTimeUTC > ruleGeotabObjectCacher.LastPropagatedToDatabaseTimeUtc)
                        {
                            // Iterate through the in-memory cache of Geotab Rule objects that were added or updated in the last cache update, mapping them to corresponding DbStgRule2 entities and adding them to the list of DbStgRule2 objects to persist. Note that deduplication logic is contained in the database.
                            foreach (var rule in ruleGeotabObjectCacher.GeotabObjectsChangedInLastUpdate)
                            {
                                var newdbStgRule2 = geotabRuleDbStgRule2ObjectMapper.CreateEntity(rule);
                                dbStgRule2sToPersist.Add(newdbStgRule2);
                            }

                            stoppingToken.ThrowIfCancellationRequested();
                        }
                        else
                        {
                            logger.Debug($"Rule cache in database is up-to-date.");
                        }
                        
                        // Persist changes to database. Step 1: Persist the DbStgRule2 entities.
                        if (dbStgRule2sToPersist.Count != 0)
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
                                        await dbStgRule2Repo.ExecuteAsync(sql, null, cancellationTokenSource, true, adapterContext);

                                        // DbStgRules2:
                                        await dbStgRule2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbStgRule2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);
                                        
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

                        // Persist changes to database. Step 2: Merge the DbStgRule2 entities into the DbRule2 table and update the DbOServiceTracking table.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    if (dbStgRule2sToPersist.Count != 0)
                                    {
                                        // Define parameters for the merge procedure.
                                        var setEntityStatusDeletedForMissingRules = false;

                                        if (ruleGeotabObjectCacher.LastCacheOperationType == CacheOperationType.Refresh)
                                        {
                                            setEntityStatusDeletedForMissingRules = true;
                                        }

                                        var parameters = new[]
                                        {
                                            new { SetEntityStatusDeletedForMissingRules = setEntityStatusDeletedForMissingRules }
                                        };

                                        // Build the SQL statement to execute the merge procedure.
                                        var sql = adapterContext.ProviderType switch
                                        {
                                            ConnectionInfo.DataAccessProviderType.PostgreSQL => MergeFunctionSQL_Postgres,
                                            ConnectionInfo.DataAccessProviderType.SQLServer => MergeProcedureSQL_SQLServer,
                                            _ => throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.")
                                        };

                                        // Execute the merge procedure.
                                        await dbStgRule2Repo.ExecuteAsync(sql, parameters, cancellationTokenSource);
                                    }

                                    // DbOServiceTracking:
                                    await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.RuleProcessor2, DateTime.UtcNow);

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
						
                        // If there were any changes, force the DbRule2 and/or DbCondition caches to be updated so that the changes are immediately available to other consumers. Run the associated tasks in parallel.
                        
                        if (dbStgRule2sToPersist.Count != 0)
                        {
                            await dbRule2ObjectCache.UpdateAsync(true);
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
                    //HandleException(databaseConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    exceptionHelper.LogException(databaseConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix); 
                    stateMachine.HandleException(databaseConnectionException, NLogLogLevelName.Error);
                }
                catch (MyGeotabConnectionException myGeotabConnectionException)
                {
                    //HandleException(myGeotabConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
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
            if (ruleGeotabObjectCacher.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(ruleGeotabObjectCacher.InitializeAsync(cancellationTokenSource, adapterConfiguration.RuleCacheIntervalDailyReferenceStartTimeUTC, adapterConfiguration.RuleCacheUpdateIntervalMinutes, adapterConfiguration.RuleCacheRefreshIntervalMinutes, myGeotabAPIHelper.GetFeedResultLimitRule, true));
            }
            else
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(ruleGeotabObjectCacher.UpdateGeotabObjectCacheAsync(cancellationTokenSource));
            }

            // Update the in-memory caches of database objects that correspond with Geotab objects. Run tasks in parallel.
            if (dbRule2ObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbRule2ObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }
            
            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);
        }

        /// <summary>
        /// Starts the current <see cref="RuleProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.RuleProcessor2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.RuleProcessor2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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

            // Register this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false as this service does not need to participate in pauses for database maintenance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(RuleProcessor2), false);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableRuleCache == true)
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
        /// Stops the current <see cref="RuleProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(RuleProcessor2), false);
            }

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}