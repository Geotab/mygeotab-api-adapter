using Geotab.Checkmate.ObjectModel;
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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that extracts <see cref="Rule"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class RuleProcessor : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        List<DbCondition> dbConditionObjectCacheList = new();

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment adapterEnvironment;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericGenericDbObjectCache<DbCondition, AdapterGenericDbObjectCache<DbCondition>> dbConditionObjectCache;
        readonly IGenericGenericDbObjectCache<DbRule, AdapterGenericDbObjectCache<DbRule>> dbRuleObjectCache;
        readonly IGenericEntityPersister<DbCondition> dbConditionEntityPersister;
        readonly IGenericEntityPersister<DbRule> dbRuleEntityPersister;
        readonly IGenericGeotabObjectCacher<Rule> ruleGeotabObjectCacher;
        readonly IGeotabConditionDbConditionObjectMapper geotabConditionDbConditionObjectMapper;
        readonly IGeotabRuleDbRuleObjectMapper geotabRuleDbRuleObjectMapper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IPrerequisiteServiceChecker prerequisiteServiceChecker;
        readonly IServiceTracker serviceTracker;
        readonly IStateMachine stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="RuleProcessor"/> class.
        /// </summary>
        public RuleProcessor(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment adapterEnvironment, IExceptionHelper exceptionHelper, IGenericGenericDbObjectCache<DbCondition, AdapterGenericDbObjectCache<DbCondition>> dbConditionObjectCache, IGenericGenericDbObjectCache<DbRule, AdapterGenericDbObjectCache<DbRule>> dbRuleObjectCache, IGenericEntityPersister<DbCondition> dbConditionEntityPersister, IGenericEntityPersister<DbRule> dbRuleEntityPersister, IGeotabConditionDbConditionObjectMapper geotabConditionDbConditionObjectMapper, IGeotabRuleDbRuleObjectMapper geotabRuleDbRuleObjectMapper, IMyGeotabAPIHelper myGeotabAPIHelper, IPrerequisiteServiceChecker prerequisiteServiceChecker, IServiceTracker serviceTracker, IStateMachine stateMachine, IGenericGeotabObjectCacher<Rule> ruleGeotabObjectCacher, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.exceptionHelper = exceptionHelper;
            this.dbConditionObjectCache = dbConditionObjectCache;
            this.dbRuleObjectCache = dbRuleObjectCache;
            this.dbConditionEntityPersister = dbConditionEntityPersister;
            this.dbRuleEntityPersister = dbRuleEntityPersister;
            this.geotabConditionDbConditionObjectMapper = geotabConditionDbConditionObjectMapper;
            this.geotabRuleDbRuleObjectMapper = geotabRuleDbRuleObjectMapper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.prerequisiteServiceChecker = prerequisiteServiceChecker;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.ruleGeotabObjectCacher = ruleGeotabObjectCacher;

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(MaxRetries, logger);

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
                await WaitForPrerequisiteServicesIfNeededAsync(stoppingToken);

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
                        await InitializeOrUpdateCachesAsync(cancellationTokenSource);

                        var dbRulesToPersist = new List<DbRule>();
                        var dbConditionsToPersist = new List<DbCondition>();
                        // Only propagate the cache to database if the cache has been updated since the last time it was propagated to database.
                        if (ruleGeotabObjectCacher.LastUpdatedTimeUTC > ruleGeotabObjectCacher.LastPropagatedToDatabaseTimeUtc)
                        {
                            DateTime recordChangedTimestampUtc = DateTime.UtcNow;

                            // Get the full list of DbConditions from the dbConditionObjectCache for use in relating DbCondititions to DbRules. This is done once here so that the list does not to be inefficiently created multiple times.
                            dbConditionObjectCacheList = await dbConditionObjectCache.GetObjectsAsync();

                            // Find any rules that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                            var dbRules = await dbRuleObjectCache.GetObjectsAsync();
                            foreach (var dbRule in dbRules)
                            {
                                if (dbRule.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                                {
                                    bool ruleExistsInCache = ruleGeotabObjectCacher.GeotabObjectCache.ContainsKey(Id.Create(dbRule.GeotabId));
                                    if (!ruleExistsInCache)
                                    {
                                        dbRule.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                        dbRule.RecordLastChangedUtc = recordChangedTimestampUtc;
                                        dbRule.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                        dbRulesToPersist.Add(dbRule);

                                        // Update all associated DbConditions so that they will also be flagged as deleted in the database.
                                        var dbConditionsToUpdate = GetAllDbConditionsAssociatedWithDbRule(dbRule);
                                        foreach (var dbCondition in dbConditionsToUpdate)
                                        {
                                            dbCondition.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                            dbCondition.RecordLastChangedUtc = recordChangedTimestampUtc;
                                            dbCondition.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                            dbConditionsToPersist.Add(dbCondition);
                                        }
                                    }
                                }
                            }

                            // Iterate through the in-memory cache of Geotab Rule objects.
                            foreach (var rule in ruleGeotabObjectCacher.GeotabObjectCache.Values)
                            {
                                // Try to find the existing database record for the cached rule.
                                var dbRule = await dbRuleObjectCache.GetObjectAsync(rule.Id.ToString());
                                if (dbRule != null)
                                {
                                    // The Rule has already been added to the database. Check whether the Rule has changed. Note that if any of the Conditions associated with the Rule have changed, then the Version of the Rule should be different - causing the update requirement check to return true. Therefore, it is not necessary to also check the individual Condtions associated with the Rule for changes.
                                    if (geotabRuleDbRuleObjectMapper.EntityRequiresUpdate(dbRule, rule))
                                    {
                                        DbRule updatedDbRule = geotabRuleDbRuleObjectMapper.UpdateEntity(dbRule, rule);
                                        dbRulesToPersist.Add(updatedDbRule);

                                        // Update all associated DbConditions so that they will be deleted from the database. A new set of DbConditions will be created to replace them.
                                        var dbConditionsToDelete = GetAllDbConditionsAssociatedWithDbRule(dbRule);
                                        foreach (var dbCondition in dbConditionsToDelete)
                                        {
                                            dbCondition.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                            dbCondition.RecordLastChangedUtc = recordChangedTimestampUtc;
                                            dbCondition.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                                            dbConditionsToPersist.Add(dbCondition);
                                        }
                                        // Create a new set of DbConditions associated with the DbRule and add them to the cache.
                                        var newDbConditions = geotabConditionDbConditionObjectMapper.CreateDbConditionEntitiesForRule(rule);
                                        dbConditionsToPersist.AddRange(newDbConditions);
                                    }
                                }
                                else
                                {
                                    // The rule has not yet been added to the database. Create a DbRule, set its properties and add it to the cache.
                                    var newDbRule = geotabRuleDbRuleObjectMapper.CreateEntity(rule);

                                    // There may be multiple records for the same entity in the batch of entities retrieved from Geotab. If there are, make sure that duplicates are set to be updated instead of inserted.
                                    if (dbRulesToPersist.Where(dbRule => dbRule.GeotabId == newDbRule.GeotabId).Any())
                                    {
                                        newDbRule.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

                                        // Update all associated DbConditions so that they will be deleted from the database. A new set of DbConditions will be created to replace them.
                                        var dbConditionsToDelete = GetAllDbConditionsAssociatedWithDbRule(dbRule);
                                        foreach (var dbCondition in dbConditionsToDelete)
                                        {
                                            dbCondition.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                            dbCondition.RecordLastChangedUtc = recordChangedTimestampUtc;
                                            dbCondition.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                                            dbConditionsToPersist.Add(dbCondition);
                                        }
                                    }

                                    dbRulesToPersist.Add(newDbRule);

                                    // Create any DbConditions associated with the DbRule and add them to the cache.
                                    var newDbConditions = geotabConditionDbConditionObjectMapper.CreateDbConditionEntitiesForRule(rule);
                                    dbConditionsToPersist.AddRange(newDbConditions);
                                }
                            }

                            stoppingToken.ThrowIfCancellationRequested();

                            // Force the DbRule and DbCondition caches to be updated so that the changes are immediately available to other consumers. Run the associated tasks in parallel.
                            var dbObjectCacheUpdateTasks = new List<Task>
                            {
                                dbRuleObjectCache.UpdateAsync(true),
                                dbConditionObjectCache.UpdateAsync(true)
                            };
                            await Task.WhenAll(dbObjectCacheUpdateTasks);
                        }
                        else
                        {
                            logger.Debug($"Rule cache in database is up-to-date.");
                        }

                        // Persist changes to database. Run tasks in parallel.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    var dbEntityPersistenceTasks = new List<Task>
                                    {
                                        // DbRules:
                                        dbRuleEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbRulesToPersist, cancellationTokenSource, Logging.LogLevel.Info),
                                        // DbConditions:
                                        dbConditionEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbConditionsToPersist, cancellationTokenSource, Logging.LogLevel.Info),
                                        // DbOServiceTracking:
                                        serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.RuleProcessor, DateTime.UtcNow)
                                    };
                                    await Task.WhenAll(dbEntityPersistenceTasks);

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
                catch (MyGeotabConnectionException myGeotabConnectionException)
                {
                    HandleException(myGeotabConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                }
                catch (Exception ex)
                {
                    // If an exception hasn't been handled to this point, log it and kill the process.
                    HandleException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
                }

                // Add a delay equivalent to the configured update interval.
                var delayTimeSpan = TimeSpan.FromMinutes(adapterConfiguration.RuleCacheUpdateIntervalMinutes);
                logger.Info($"{CurrentClassName} pausing for the configured update interval ({delayTimeSpan}).");
                await Task.Delay(delayTimeSpan, stoppingToken);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Finds all <see cref="DbCondition"/>s in <see cref="dbConditionObjectCacheList"/> associated with the <paramref name="dbRule"/> and returns them in a list. In no associated conditions are found, the list will be empty.
        /// </summary>
        /// <param name="dbRule">The <see cref="DbRule"/> for which to find associated <see cref="DbCondition"/>s to update.</param>
        List<DbCondition> GetAllDbConditionsAssociatedWithDbRule(DbRule dbRule)
        {
            var dbConditions = new List<DbCondition>();
            foreach (var dbCondition in dbConditionObjectCacheList)
            {
                if (dbCondition.RuleId == dbRule.GeotabId)
                {
                    dbConditions.Add(dbCondition);
                }
            }
            return dbConditions;
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
            else if (exception is MyGeotabConnectionException)
            {
                stateMachine.SetState(State.Waiting, StateReason.MyGeotabNotAvailable);
            }

            if (logLevel == NLogLogLevelName.Fatal)
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        /// Initializes and/or updates any caches used by this class.
        /// </summary>
        /// <returns></returns>
        async Task InitializeOrUpdateCachesAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

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
            if (dbRuleObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbRuleObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }
            if (dbConditionObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbConditionObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }
            
            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Starts the current <see cref="RuleProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.RuleProcessor);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.RuleProcessor, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.EnableRuleCache == true)
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
        /// Stops the current <see cref="RuleProcessor"/> instance.
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
        /// Checks whether any prerequisite services have been run and are currently running. If any of prerequisite services have not yet been run or are not currently running, details will be logged and this service will pause operation, repeating this check intermittently until all prerequisite services are running.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        async Task WaitForPrerequisiteServicesIfNeededAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var prerequisiteServices = new List<AdapterService>
            {
            };

            await prerequisiteServiceChecker.WaitForPrerequisiteServicesIfNeededAsync(CurrentClassName, prerequisiteServices, cancellationToken);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
