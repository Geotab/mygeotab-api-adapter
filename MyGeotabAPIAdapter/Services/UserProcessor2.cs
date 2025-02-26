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
using System.Linq;
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
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericGenericDbObjectCache<DbUser2, AdapterGenericDbObjectCache<DbUser2>> dbUser2ObjectCache;
        readonly IGenericEntityPersister<DbUser2> dbUser2EntityPersister;
        readonly IGenericGeotabObjectCacher<User> userGeotabObjectCacher;
        readonly IGeotabIdConverter geotabIdConverter;
        readonly IGeotabUserDbUser2ObjectMapper geotabUserDbUser2ObjectMapper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserProcessor2"/> class.
        /// </summary>
        public UserProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<UserProcessor2> awaiter, IExceptionHelper exceptionHelper, IGenericGenericDbObjectCache<DbUser2, AdapterGenericDbObjectCache<DbUser2>> dbUser2ObjectCache, IGenericEntityPersister<DbUser2> dbUser2EntityPersister, IGeotabUserDbUser2ObjectMapper geotabUserDbUser2ObjectMapper, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericGeotabObjectCacher<User> userGeotabObjectCacher, IGeotabIdConverter geotabIdConverter, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.exceptionHelper = exceptionHelper;
            this.dbUser2ObjectCache = dbUser2ObjectCache;
            this.dbUser2EntityPersister = dbUser2EntityPersister;
            this.geotabUserDbUser2ObjectMapper = geotabUserDbUser2ObjectMapper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.userGeotabObjectCacher = userGeotabObjectCacher;
            this.geotabIdConverter = geotabIdConverter;

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
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            var delayTimeSpan = TimeSpan.FromMinutes(adapterConfiguration.UserCacheUpdateIntervalMinutes);

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

                        var dbUser2sToPersist = new List<DbUser2>();
                        var newDbUser2sToPersistDictionary = new Dictionary<long, Common.DatabaseWriteOperationType>();
                        // Only propagate the cache to database if the cache has been updated since the last time it was propagated to database.
                        if (userGeotabObjectCacher.LastUpdatedTimeUTC > userGeotabObjectCacher.LastPropagatedToDatabaseTimeUtc)
                        {
                            DateTime recordChangedTimestampUtc = DateTime.UtcNow;

                            // Find any users that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                            var dbUser2s = await dbUser2ObjectCache.GetObjectsAsync();
                            foreach (var dbUser2 in dbUser2s)
                            {
                                if (dbUser2.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                                {
                                    bool userExistsInCache = userGeotabObjectCacher.GeotabObjectCache.ContainsKey(Id.Create(dbUser2.GeotabId));
                                    if (!userExistsInCache)
                                    {
                                        logger.Debug($"User '{dbUser2.GeotabId}' no longer exists in MyGeotab and is being marked as deleted.");
                                        dbUser2.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                        dbUser2.RecordLastChangedUtc = recordChangedTimestampUtc;
                                        dbUser2.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                        dbUser2sToPersist.Add(dbUser2);
                                    }
                                }
                            }

                            // Iterate through the in-memory cache of Geotab User objects.
                            foreach (var user in userGeotabObjectCacher.GeotabObjectCache.Values)
                            {
                                // Try to find the existing database record for the cached user.
                                long userId = geotabIdConverter.ToLong(user.Id);
                                var dbUser2 = await dbUser2ObjectCache.GetObjectAsync(userId);
                                if (dbUser2 != null)
                                {
                                    // The user has already been added to the database.
                                    if (geotabUserDbUser2ObjectMapper.EntityRequiresUpdate(dbUser2, user))
                                    {
                                        DbUser2 updatedDbUser2 = geotabUserDbUser2ObjectMapper.UpdateEntity(dbUser2, user);
                                        dbUser2sToPersist.Add(updatedDbUser2);
                                    }
                                }
                                else
                                {
                                    // The user has not yet been added to the database. Create a DbUser2, set its properties and add it to the cache.
                                    var newDbUser2 = geotabUserDbUser2ObjectMapper.CreateEntity(user);

                                    // There may be multiple records for the same entity in the batch of entities retrieved from Geotab. If there are, make sure that duplicates are set to be updated instead of inserted.
                                    if (newDbUser2sToPersistDictionary.ContainsKey(newDbUser2.id))
                                    {
                                        newDbUser2.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                    }

                                    dbUser2sToPersist.Add(newDbUser2);
                                    if (newDbUser2.DatabaseWriteOperationType == Common.DatabaseWriteOperationType.Insert)
                                    {
                                        newDbUser2sToPersistDictionary.Add(newDbUser2.id, newDbUser2.DatabaseWriteOperationType);
                                    }
                                }
                            }

                            stoppingToken.ThrowIfCancellationRequested();
                        }
                        else
                        {
                            logger.Debug($"User cache in database is up-to-date.");
                        }

                        // Persist changes to database.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    // DbUser2:
                                    await dbUser2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbUser2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

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
                        if (dbUser2sToPersist.Any())
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
