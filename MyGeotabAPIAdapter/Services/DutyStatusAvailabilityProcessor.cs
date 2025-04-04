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
    /// 
    /// NOTE: The Geotab API's GetFeed method does not support the <see cref="Geotab.Checkmate.ObjectModel.DutyStatusAvailability"/> entity type and the results for DutyStatusAvailability Get requests are calculated dynamically, resulting in longer response times than are typical for pre-calculated data. It is also necessary to retrieve DutyStatusAvailability on a per-driver basis using batches of Get requests wrapped in <see cref="Geotab.Checkmate.API.MultiCallAsync(object[])"/> requests (in order to support larger fleets where the number of Get<DutyStatusAvailability> requests required to cover all drivers could not be made in a single MultiCall request). The result of the combination of these factors is that it can take some time for DutyStatusAvailability to be retrieved for all drivers in a fleet.
    /// </summary>
    class DutyStatusAvailabilityProcessor : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        const string HosRuleSetNoneValue = "None";
        const int maxBatchSize = 100;
        DateTime lastDutyStatusAvailabilityDataRetrievalStartTimeUtc;

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking> adapterEnvironment;
        readonly IDateTimeHelper dateTimeHelper;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericGenericDbObjectCache<DbDutyStatusAvailability, AdapterGenericDbObjectCache<DbDutyStatusAvailability>> dbDutyStatusAvailabilityObjectCache;
        readonly IGenericGenericDbObjectCache<DbUser, AdapterGenericDbObjectCache<DbUser>> dbUserObjectCache;
        readonly IGenericEntityPersister<DbDutyStatusAvailability> dbDutyStatusAvailabilityEntityPersister;
        readonly IGeotabDeviceFilterer geotabDeviceFilterer;
        readonly IGeotabDutyStatusAvailabilityDbDutyStatusAvailabilityObjectMapper geotabDutyStatusAvailabilityDbDutyStatusAvailabilityObjectMapper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IPrerequisiteServiceChecker<DbOServiceTracking> prerequisiteServiceChecker;
        readonly IServiceTracker<DbOServiceTracking> serviceTracker;
        readonly IStateMachine<DbMyGeotabVersionInfo> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DutyStatusAvailabilityProcessor"/> class.
        /// </summary>
        public DutyStatusAvailabilityProcessor(IAdapterConfiguration adapterConfiguration, IDateTimeHelper dateTimeHelper, IAdapterEnvironment<DbOServiceTracking> adapterEnvironment, IExceptionHelper exceptionHelper, IGenericGenericDbObjectCache<DbDutyStatusAvailability, AdapterGenericDbObjectCache<DbDutyStatusAvailability>> dbDutyStatusAvailabilityObjectCache, IGenericGenericDbObjectCache<DbUser, AdapterGenericDbObjectCache<DbUser>> dbUserObjectCache, IGenericEntityPersister<DbDutyStatusAvailability> dbDutyStatusAvailabilityEntityPersister, IGeotabDeviceFilterer geotabDeviceFilterer, IGeotabDutyStatusAvailabilityDbDutyStatusAvailabilityObjectMapper geotabDutyStatusAvailabilityDbDutyStatusAvailabilityObjectMapper, IMyGeotabAPIHelper myGeotabAPIHelper, IPrerequisiteServiceChecker<DbOServiceTracking> prerequisiteServiceChecker, IServiceTracker<DbOServiceTracking> serviceTracker, IStateMachine<DbMyGeotabVersionInfo> stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.dateTimeHelper = dateTimeHelper;
            this.adapterEnvironment = adapterEnvironment;
            this.exceptionHelper = exceptionHelper;
            this.dbDutyStatusAvailabilityObjectCache = dbDutyStatusAvailabilityObjectCache;
            this.dbUserObjectCache = dbUserObjectCache;
            this.dbDutyStatusAvailabilityEntityPersister = dbDutyStatusAvailabilityEntityPersister;
            this.geotabDeviceFilterer = geotabDeviceFilterer;
            this.geotabDutyStatusAvailabilityDbDutyStatusAvailabilityObjectMapper = geotabDutyStatusAvailabilityDbDutyStatusAvailabilityObjectMapper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.prerequisiteServiceChecker = prerequisiteServiceChecker;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;

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
                                var allDbUsers = await dbUserObjectCache.GetObjectsAsync();
                                var driverDbUsers = allDbUsers.Where(dbUser => dbUser.IsDriver == true && dbUser.ActiveTo >= DateTime.UtcNow && dbUser.LastAccessDate >= cutoffLastAccessedTime && dbUser.HosRuleSet != HosRuleSetNoneValue).ToList();

                                int currentBatchSize = 0;
                                int driverDbUserCount = driverDbUsers.Count;
                                var calls = new List<object>();
                                for (int driverDbUserListIndex = 0; driverDbUserListIndex < driverDbUserCount + 1; driverDbUserListIndex++)
                                {
                                    if (currentBatchSize == maxBatchSize || driverDbUserListIndex == driverDbUserCount)
                                    {
                                        DateTime recordChangedTimestampUtc = DateTime.UtcNow;
                                        var dbDutyStatusAvailabilityEntitiesToPersist = new List<DbDutyStatusAvailability>();

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
                                                var dutyStatusAvailability = resultDutyStatusAvailabilityList[0];
                                                var dutyStatusAvailabilityDriver = dutyStatusAvailability.Driver;
                                                if (dutyStatusAvailabilityDriver == null || dutyStatusAvailabilityDriver.Id == null)
                                                {
                                                    logger.Info($"Could not process a {nameof(DutyStatusAvailability)} entity with Id '{dutyStatusAvailability.Id.ToString()}' because its {nameof(DutyStatusAvailability.Driver)} property is null or has a null {nameof(Driver.Id)}.");
                                                    continue;
                                                }

                                                // Try to find the existing database record for DutyStatusAvailability associated with the subject Driver.
                                                var existingDbDutyStatusAvailability = await dbDutyStatusAvailabilityObjectCache.GetObjectAsync(dutyStatusAvailabilityDriver.Id.ToString());
                                                if (existingDbDutyStatusAvailability != null)
                                                {
                                                    // The database already contains a DutyStatusAvailability record for the subject Driver.
                                                    if (geotabDutyStatusAvailabilityDbDutyStatusAvailabilityObjectMapper.EntityRequiresUpdate(existingDbDutyStatusAvailability, dutyStatusAvailability))
                                                    { 
                                                        DbDutyStatusAvailability updatedDbDutyStatusAvailability = geotabDutyStatusAvailabilityDbDutyStatusAvailabilityObjectMapper.UpdateEntity(existingDbDutyStatusAvailability, dutyStatusAvailability);
                                                        dbDutyStatusAvailabilityEntitiesToPersist.Add(updatedDbDutyStatusAvailability);
                                                    }
                                                }
                                                else
                                                {
                                                    // A DutyStatusAvailability record associated with the subject Driver has not yet been added to the database. Create a DbDutyStatusAvailability, set its properties and add it to the cache.
                                                    DbDutyStatusAvailability newDbDutyStatusAvailability = geotabDutyStatusAvailabilityDbDutyStatusAvailabilityObjectMapper.CreateEntity(dutyStatusAvailability);
                                                    dbDutyStatusAvailabilityEntitiesToPersist.Add(newDbDutyStatusAvailability);
                                                }
                                            }
                                        }

                                        stoppingToken.ThrowIfCancellationRequested();

                                        // Persist changes to database.
                                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                                        {
                                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                            {
                                                try
                                                {
                                                    // DbDutyStatusAvailability:
                                                    await dbDutyStatusAvailabilityEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbDutyStatusAvailabilityEntitiesToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                                    // DbOServiceTracking:
                                                    await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DutyStatusAvailabilityProcessor, DateTime.UtcNow);

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

                                        // If there were any changes, force the DutyStatusAvailability cache to be updated so that the changes are immediately available to other consumers.
                                        if (dbDutyStatusAvailabilityEntitiesToPersist.Count != 0)
                                        {
                                            await dbDutyStatusAvailabilityObjectCache.UpdateAsync(true);
                                        }

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
                                    calls.Add(new object[] { "Get", typeof(DutyStatusAvailability), new { search = new DutyStatusAvailabilitySearch { UserSearch = new UserSearch { Id = driverDbUserId } } }, typeof(List<DutyStatusAvailability>) });

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
                var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.DutyStatusAvailabilityFeedIntervalSeconds);
                logger.Info($"{CurrentClassName} pausing for the configured feed interval ({delayTimeSpan}).");
                await Task.Delay(delayTimeSpan, stoppingToken);
            }
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
            var dbObjectCacheInitializationAndUpdateTasks = new List<Task>();

            // Update the in-memory caches of database objects that correspond with Geotab objects.
            if (dbDutyStatusAvailabilityObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbDutyStatusAvailabilityObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }
            if (dbUserObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbUserObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }

            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);
        }

        /// <summary>
        /// Starts the current <see cref="DutyStatusAvailabilityProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.DutyStatusAvailabilityProcessor, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DutyStatusAvailabilityProcessor, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
            if (adapterConfiguration.UseDataModel2 == false && adapterConfiguration.EnableDutyStatusAvailabilityFeed == true)
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
        /// Stops the current <see cref="DutyStatusAvailabilityProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
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
            var prerequisiteServices = new List<AdapterService>
            {
                AdapterService.UserProcessor
            };

            await prerequisiteServiceChecker.WaitForPrerequisiteServicesIfNeededAsync(CurrentClassName, prerequisiteServices, cancellationToken);
        }
    }
}
